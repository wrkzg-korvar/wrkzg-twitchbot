using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Core.Services;

namespace Wrkzg.Api.Endpoints;

/// <summary>
/// REST endpoints for raffles. Used by the dashboard to create, view, draw, and manage raffles.
/// </summary>
public static class RaffleEndpoints
{
    /// <summary>Registers raffle creation, drawing, and template management API endpoints.</summary>
    public static void MapRaffleEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/raffles").WithTags("Raffles");

        // GET /api/raffles/active
        group.MapGet("/active", async (RaffleService service, CancellationToken ct) =>
        {
            Raffle? raffle = await service.GetActiveAsync(ct);
            if (raffle is null)
            {
                return Results.NotFound();
            }
            return Results.Ok(MapToDto(raffle));
        });

        // GET /api/raffles/history
        group.MapGet("/history", async (RaffleService service, CancellationToken ct) =>
        {
            IReadOnlyList<Raffle> raffles = await service.GetRecentAsync(10, ct);
            return Results.Ok(raffles.Select(MapToSummaryDto));
        });

        // GET /api/raffles/{id}
        group.MapGet("/{id:int}", async (int id, RaffleService service, CancellationToken ct) =>
        {
            Raffle? raffle = await service.GetByIdAsync(id, ct);
            return raffle is not null ? Results.Ok(MapToDto(raffle)) : Results.NotFound();
        });

        // POST /api/raffles
        group.MapPost("/", async (CreateRaffleRequest request, RaffleService service, CancellationToken ct) =>
        {
            RaffleResult result = await service.CreateAsync(
                request.Title,
                request.Keyword,
                request.DurationSeconds,
                request.MaxEntries,
                request.CreatedBy ?? "Dashboard",
                ct);

            return result.Success
                ? Results.Created($"/api/raffles/{result.Raffle!.Id}", MapToDto(result.Raffle))
                : Results.BadRequest(new { error = result.Error });
        });

        // POST /api/raffles/draw
        group.MapPost("/draw", async (RaffleService service, CancellationToken ct) =>
        {
            DrawResult result = await service.DrawAsync(ct);
            return result.Success
                ? Results.Ok(new { winner = result.WinnerName, totalEntries = result.TotalEntries })
                : Results.BadRequest(new { error = result.Error });
        });

        // POST /api/raffles/cancel
        group.MapPost("/cancel", async (RaffleService service, CancellationToken ct) =>
        {
            RaffleResult result = await service.CancelAsync(ct);
            return result.Success ? Results.Ok() : Results.BadRequest(new { error = result.Error });
        });

        // POST /api/raffles/end — close the raffle (after accepting winners)
        group.MapPost("/end", async (RaffleService service, CancellationToken ct) =>
        {
            RaffleResult result = await service.EndRaffleAsync(ct);
            return result.Success ? Results.Ok() : Results.BadRequest(new { error = result.Error });
        });

        // POST /api/raffles/accept — confirm the pending winner
        group.MapPost("/accept", async (RaffleService service, CancellationToken ct) =>
        {
            DrawResult result = await service.AcceptWinnerAsync(ct);
            return result.Success
                ? Results.Ok(new { winner = result.WinnerName, totalEntries = result.TotalEntries })
                : Results.BadRequest(new { error = result.Error });
        });

        // POST /api/raffles/redraw — reject pending winner, draw new one
        group.MapPost("/redraw", async (RedrawRequest? request, RaffleService service, CancellationToken ct) =>
        {
            DrawResult result = await service.RedrawAsync(request?.Reason, ct);
            return result.Success
                ? Results.Ok(new { winner = result.WinnerName, totalEntries = result.TotalEntries })
                : Results.BadRequest(new { error = result.Error });
        });

        // GET /api/raffles/{id}/draws — draw history for a raffle
        group.MapGet("/{id:int}/draws", async (int id, IRaffleRepository repo, CancellationToken ct) =>
        {
            IReadOnlyList<RaffleDraw> draws = await repo.GetDrawsAsync(id, ct);
            return Results.Ok(draws.Select(d => new
            {
                d.DrawNumber,
                username = d.User?.DisplayName ?? "Unknown",
                twitchId = d.User?.TwitchId ?? "",
                d.IsAccepted,
                d.RedrawReason,
                d.DrawnAt
            }));
        });

        // GET /api/raffles/templates
        group.MapGet("/templates", async (ISettingsRepository settings, CancellationToken ct) =>
        {
            IDictionary<string, string> allSettings = await settings.GetAllAsync(ct);
            List<object> templates = new();
            foreach (KeyValuePair<string, string> kvp in RaffleTemplates.Defaults)
            {
                templates.Add(new
                {
                    key = kvp.Key,
                    @default = kvp.Value,
                    current = allSettings.TryGetValue(kvp.Key, out string? val) ? val : null,
                    description = RaffleTemplates.Descriptions.GetValueOrDefault(kvp.Key, ""),
                    variables = RaffleTemplates.Variables.GetValueOrDefault(kvp.Key, Array.Empty<string>())
                });
            }
            return Results.Ok(templates);
        });

        // POST /api/raffles/templates/reset/{key}
        group.MapPost("/templates/reset/{key}", async (string key, ISettingsRepository settings, CancellationToken ct) =>
        {
            if (!RaffleTemplates.Defaults.ContainsKey(key))
            {
                return Results.NotFound(new { error = "Unknown template key" });
            }
            await settings.DeleteAsync(key, ct);
            return Results.Ok();
        });
    }

    // ─── DTO Mapping ─────────────────────────────────────

    private static object MapToDto(Raffle r) => new
    {
        r.Id,
        r.Title,
        r.Keyword,
        r.IsOpen,
        r.DurationSeconds,
        r.EntriesCloseAt,
        r.MaxEntries,
        r.CreatedBy,
        r.CreatedAt,
        r.ClosedAt,
        endReason = r.EndReason.ToString(),
        winner = r.Winner is not null ? new { r.Winner.DisplayName, r.Winner.TwitchId } : null,
        pendingWinner = r.PendingWinner is not null ? new { r.PendingWinner.DisplayName, r.PendingWinner.TwitchId } : null,
        entries = r.Entries.Select(e => new
        {
            username = e.User?.DisplayName ?? "Unknown",
            twitchId = e.User?.TwitchId ?? "",
            e.TicketCount
        }),
        entryCount = r.Entries.Count,
        draws = r.Draws.OrderBy(d => d.DrawNumber).Select(d => new
        {
            d.DrawNumber,
            username = d.User?.DisplayName ?? "Unknown",
            d.IsAccepted,
            d.RedrawReason,
            d.DrawnAt
        })
    };

    private static object MapToSummaryDto(Raffle r) => new
    {
        r.Id,
        r.Title,
        r.Keyword,
        r.IsOpen,
        r.CreatedAt,
        r.ClosedAt,
        endReason = r.EndReason.ToString(),
        winnerName = r.Winner?.DisplayName,
        entryCount = r.Entries.Count
    };
}

/// <summary>Request payload for creating a new raffle.</summary>
public record CreateRaffleRequest(
    string Title,
    string? Keyword,
    int? DurationSeconds,
    int? MaxEntries,
    string? CreatedBy);

/// <summary>Request payload for redrawing a raffle winner with an optional rejection reason.</summary>
public record RedrawRequest(string? Reason);

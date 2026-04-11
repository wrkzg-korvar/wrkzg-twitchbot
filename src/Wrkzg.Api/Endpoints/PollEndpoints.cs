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
/// REST endpoints for polls. Used by the dashboard to create, view, and manage polls.
/// </summary>
public static class PollEndpoints
{
    /// <summary>Registers poll creation, voting, and template management API endpoints.</summary>
    public static void MapPollEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/polls").WithTags("Polls");

        // GET /api/polls/active
        group.MapGet("/active", async (PollService pollService, CancellationToken ct) =>
        {
            PollResultsDto? results = await pollService.GetResultsAsync(ct: ct);
            return Results.Ok(results);
        });

        // GET /api/polls/history
        group.MapGet("/history", async (PollService pollService, CancellationToken ct) =>
        {
            IReadOnlyList<Poll> polls = await pollService.GetRecentAsync(10, ct);
            return Results.Ok(polls.Select(p => new
            {
                id = p.Id,
                question = p.Question,
                options = p.Options,
                isActive = p.IsActive,
                source = p.Source.ToString(),
                createdBy = p.CreatedBy,
                createdAt = p.CreatedAt,
                endsAt = p.EndsAt,
                durationSeconds = p.DurationSeconds,
                endReason = p.EndReason.ToString(),
                totalVotes = p.Votes.Count,
                winnerIndex = p.Votes.Count > 0
                    ? p.Options
                        .Select((_, idx) => new { idx, count = p.Votes.Count(v => v.OptionIndex == idx) })
                        .OrderByDescending(x => x.count)
                        .First().idx
                    : (int?)null
            }));
        });

        // GET /api/polls/{id}
        group.MapGet("/{id:int}", async (int id, PollService pollService, CancellationToken ct) =>
        {
            PollResultsDto? results = await pollService.GetResultsAsync(id, ct);
            return results is not null ? Results.Ok(results) : TypedResults.Problem(title: "Not Found", statusCode: StatusCodes.Status404NotFound, type: "https://wrkzg.app/problems/not-found");
        });

        // POST /api/polls
        group.MapPost("/", async (CreatePollRequest request, PollService pollService, CancellationToken ct) =>
        {
            PollResult result = await pollService.CreateBotPollAsync(
                request.Question,
                request.Options,
                request.DurationSeconds,
                request.CreatedBy ?? "Dashboard",
                ct);

            return result.Success
                ? Results.Created($"/api/polls/{result.Poll!.Id}", result.Poll)
                : TypedResults.Problem(detail: result.Error, title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
        });

        // POST /api/polls/end
        group.MapPost("/end", async (PollService pollService, CancellationToken ct) =>
        {
            PollResult result = await pollService.EndPollAsync(PollEndReason.ManuallyClosed, ct);
            return result.Success ? Results.Ok() : TypedResults.Problem(detail: result.Error, title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
        });

        // POST /api/polls/cancel
        group.MapPost("/cancel", async (PollService pollService, CancellationToken ct) =>
        {
            PollResult result = await pollService.EndPollAsync(PollEndReason.Cancelled, ct);
            return result.Success ? Results.Ok() : TypedResults.Problem(detail: result.Error, title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
        });

        // GET /api/polls/templates — all template definitions with current overrides
        group.MapGet("/templates", async (ISettingsRepository settings, CancellationToken ct) =>
        {
            IDictionary<string, string> allSettings = await settings.GetAllAsync(ct);
            var templates = PollTemplates.All.Select(t => new
            {
                t.Key,
                t.Default,
                t.Description,
                t.Variables,
                current = allSettings.TryGetValue(t.Key, out string? val) ? val : null
            });
            return Results.Ok(templates);
        });

        // POST /api/polls/templates/reset/{key} — remove a template override
        group.MapPost("/templates/reset/{key}", async (string key, ISettingsRepository settings, CancellationToken ct) =>
        {
            // Validate the key is a known template
            bool isKnown = PollTemplates.All.Any(t => t.Key == key);
            if (!isKnown)
            {
                return TypedResults.Problem(detail: $"Unknown template key: {key}", title: "Not Found", statusCode: StatusCodes.Status404NotFound, type: "https://wrkzg.app/problems/not-found");
            }

            await settings.DeleteAsync(key, ct);
            return Results.Ok();
        });

        // POST /api/polls/twitch (future — Twitch-native polls via Helix)
        group.MapPost("/twitch", async (
            CreateTwitchPollRequest request,
            PollService pollService,
            IBroadcasterHelixClient helix,
            ISecureStorage storage,
            CancellationToken ct) =>
        {
            return Results.StatusCode(501); // Not Implemented yet
        });
    }
}

/// <summary>Request payload for creating a new bot-managed poll.</summary>
public record CreatePollRequest(
    string Question,
    string[] Options,
    int DurationSeconds,
    string? CreatedBy);

/// <summary>Request payload for creating a Twitch-native poll via the Helix API.</summary>
public record CreateTwitchPollRequest(
    string Question,
    string[] Options,
    int DurationSeconds);

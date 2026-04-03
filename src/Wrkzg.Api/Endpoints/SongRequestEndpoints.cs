using System.Collections.Generic;
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
/// REST endpoints for song request queue management.
/// </summary>
public static class SongRequestEndpoints
{
    public static void MapSongRequestEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/song-requests").WithTags("SongRequests");

        group.MapGet("/queue", async (ISongRequestRepository repo, CancellationToken ct) =>
        {
            IReadOnlyList<SongRequest> queue = await repo.GetQueueAsync(ct);
            return Results.Ok(queue);
        });

        group.MapGet("/current", async (ISongRequestRepository repo, CancellationToken ct) =>
        {
            SongRequest? current = await repo.GetCurrentlyPlayingAsync(ct);
            return current is not null ? Results.Ok(current) : Results.Ok((object?)null);
        });

        group.MapGet("/history", async (ISongRequestRepository repo, CancellationToken ct) =>
        {
            IReadOnlyList<SongRequest> history = await repo.GetHistoryAsync(20, ct);
            return Results.Ok(history);
        });

        group.MapPost("/skip", async (SongRequestService service, CancellationToken ct) =>
        {
            string result = await service.SkipCurrentAsync(ct);
            return Results.Ok(new { message = result });
        });

        group.MapPost("/next", async (SongRequestService service, CancellationToken ct) =>
        {
            SongRequest? next = await service.PlayNextAsync(ct);
            return Results.Ok(next);
        });

        group.MapDelete("/{id:int}", async (int id, ISongRequestRepository repo, IChatEventBroadcaster broadcaster, CancellationToken ct) =>
        {
            await repo.DeleteAsync(id, ct);
            await broadcaster.BroadcastSongQueueUpdatedAsync(ct);
            return Results.NoContent();
        });

        group.MapPost("/clear", async (ISongRequestRepository repo, IChatEventBroadcaster broadcaster, CancellationToken ct) =>
        {
            await repo.ClearQueueAsync(ct);
            await broadcaster.BroadcastSongQueueUpdatedAsync(ct);
            return Results.Ok(new { message = "Queue cleared." });
        });

        group.MapGet("/status", async (SongRequestService service, ISongRequestRepository repo, CancellationToken ct) =>
        {
            await service.LoadSettingsAsync(ct);
            int queueCount = await repo.GetQueueCountAsync(ct);
            return Results.Ok(new
            {
                queueOpen = service.IsQueueOpen,
                queueCount,
                maxPerUser = service.MaxPerUser,
                maxDuration = service.MaxDuration,
                pointsCost = service.PointsCost
            });
        });

        group.MapPost("/toggle", async (SongRequestService service, CancellationToken ct) =>
        {
            bool newState = !service.IsQueueOpen;
            await service.SetQueueOpenAsync(newState, ct);
            return Results.Ok(new { queueOpen = newState });
        });

        // Message templates
        group.MapGet("/messages", async (SongRequestService service, ISettingsRepository settings, CancellationToken ct) =>
        {
            Dictionary<string, string> defaults = service.GetDefaultMessageTemplates();
            Dictionary<string, string> current = new();

            foreach (KeyValuePair<string, string> kvp in defaults)
            {
                string settingsKey = $"Games.SongRequest.Msg.{kvp.Key}";
                string? custom = await settings.GetAsync(settingsKey, ct);
                current[kvp.Key] = !string.IsNullOrWhiteSpace(custom) ? custom : kvp.Value;
            }

            return Results.Ok(new
            {
                name = "SongRequest",
                messages = current,
                defaults
            });
        });

        group.MapPut("/messages", async (UpdateSongRequestMessagesRequest request,
            ISettingsRepository settings, CancellationToken ct) =>
        {
            if (request.Messages is not null)
            {
                foreach (KeyValuePair<string, string> kvp in request.Messages)
                {
                    string key = $"Games.SongRequest.Msg.{kvp.Key}";
                    await settings.SetAsync(key, kvp.Value, ct);
                }
            }
            return Results.Ok(new { updated = request.Messages?.Count ?? 0 });
        });

        group.MapPost("/messages/{messageKey}/reset", async (string messageKey,
            SongRequestService service, ISettingsRepository settings, CancellationToken ct) =>
        {
            Dictionary<string, string> defaults = service.GetDefaultMessageTemplates();
            if (!defaults.ContainsKey(messageKey))
            {
                return Results.NotFound(new { error = $"Message key '{messageKey}' not found." });
            }

            string key = $"Games.SongRequest.Msg.{messageKey}";
            await settings.DeleteAsync(key, ct);
            return Results.Ok(new { key = messageKey, value = defaults[messageKey] });
        });

        // Settings update
        group.MapPut("/settings", async (UpdateSongRequestSettingsRequest request,
            ISettingsRepository settings, CancellationToken ct) =>
        {
            if (request.MaxDuration.HasValue)
            {
                await settings.SetAsync("SongRequest.MaxDuration", request.MaxDuration.Value.ToString(), ct);
            }
            if (request.MaxPerUser.HasValue)
            {
                await settings.SetAsync("SongRequest.MaxPerUser", request.MaxPerUser.Value.ToString(), ct);
            }
            if (request.PointsCost.HasValue)
            {
                await settings.SetAsync("SongRequest.PointsCost", request.PointsCost.Value.ToString(), ct);
            }
            return Results.Ok();
        });
    }
}

public record UpdateSongRequestMessagesRequest(Dictionary<string, string>? Messages);

public record UpdateSongRequestSettingsRequest(int? MaxDuration, int? MaxPerUser, int? PointsCost);

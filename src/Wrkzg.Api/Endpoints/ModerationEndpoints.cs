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

namespace Wrkzg.Api.Endpoints;

/// <summary>
/// REST endpoints for Twitch moderation actions and the moderation event log.
/// All Twitch actions are logged to the ModerationEvent table with their result.
/// </summary>
public static class ModerationEndpoints
{
    /// <summary>Registers moderation API endpoints.</summary>
    public static void MapModerationEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/moderation").WithTags("Moderation");

        // ─── Twitch Actions ──────────────────────────────────────

        group.MapPost("/timeout", async (
            TimeoutRequest request,
            IBotHelixClient botHelix,
            ISecureStorage storage,
            ITwitchOAuthService oauth,
            IModerationEventRepository repo,
            IChatEventBroadcaster broadcaster,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.TwitchUserId))
            {
                return TypedResults.Problem(detail: "TwitchUserId is required.", title: "Validation Error",
                    statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
            }

            if (request.DurationSeconds < 1 || request.DurationSeconds > 1_209_600)
            {
                return TypedResults.Problem(detail: "Duration must be between 1 and 1209600 seconds (14 days).",
                    title: "Validation Error", statusCode: StatusCodes.Status400BadRequest,
                    type: "https://wrkzg.app/problems/validation-error");
            }

            string? broadcasterId = await ResolveBroadcasterIdAsync(storage, oauth, ct);
            if (broadcasterId is null)
            {
                return TypedResults.Problem(detail: "Broadcaster token not available.",
                    title: "Auth Error", statusCode: StatusCodes.Status401Unauthorized,
                    type: "https://wrkzg.app/problems/auth-error");
            }

            bool success = await botHelix.TimeoutUserAsync(
                broadcasterId, request.TwitchUserId, request.DurationSeconds, request.Reason ?? "", ct);

            ModerationEvent evt = await repo.CreateAsync(new ModerationEvent
            {
                TwitchUserId = request.TwitchUserId,
                DisplayName = request.DisplayName ?? request.TwitchUserId,
                EventType = ModerationEventType.TwitchTimeout,
                Actor = "Dashboard",
                Reason = request.Reason,
                DurationSeconds = request.DurationSeconds,
                TwitchSuccess = success,
            }, ct);

            await broadcaster.BroadcastModerationActionAsync(evt, ct);

            return success
                ? Results.Ok(new { success = true, eventId = evt.Id })
                : TypedResults.Problem(detail: "Twitch API rejected the timeout. The bot may not be a moderator.",
                    title: "Twitch Error", statusCode: StatusCodes.Status502BadGateway,
                    type: "https://wrkzg.app/problems/twitch-error");
        });

        group.MapPost("/ban", async (
            BanRequest request,
            IBotHelixClient botHelix,
            ISecureStorage storage,
            ITwitchOAuthService oauth,
            IModerationEventRepository repo,
            IUserRepository users,
            IChatEventBroadcaster broadcaster,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.TwitchUserId))
            {
                return TypedResults.Problem(detail: "TwitchUserId is required.", title: "Validation Error",
                    statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
            }

            string? broadcasterId = await ResolveBroadcasterIdAsync(storage, oauth, ct);
            if (broadcasterId is null)
            {
                return TypedResults.Problem(detail: "Broadcaster token not available.",
                    title: "Auth Error", statusCode: StatusCodes.Status401Unauthorized,
                    type: "https://wrkzg.app/problems/auth-error");
            }

            bool success = await botHelix.BanUserAsync(
                broadcasterId, request.TwitchUserId, request.Reason ?? "", ct);

            ModerationEvent evt = await repo.CreateAsync(new ModerationEvent
            {
                TwitchUserId = request.TwitchUserId,
                DisplayName = request.DisplayName ?? request.TwitchUserId,
                EventType = ModerationEventType.TwitchBan,
                Actor = "Dashboard",
                Reason = request.Reason,
                TwitchSuccess = success,
            }, ct);

            await broadcaster.BroadcastModerationActionAsync(evt, ct);

            if (success)
            {
                User? targetUser = await users.GetByTwitchIdAsync(request.TwitchUserId, ct);
                if (targetUser is not null)
                {
                    targetUser.IsTwitchBanned = true;
                    await users.UpdateAsync(targetUser, ct);
                }
            }

            return success
                ? Results.Ok(new { success = true, eventId = evt.Id })
                : TypedResults.Problem(detail: "Twitch API rejected the ban. The bot may not be a moderator.",
                    title: "Twitch Error", statusCode: StatusCodes.Status502BadGateway,
                    type: "https://wrkzg.app/problems/twitch-error");
        });

        group.MapDelete("/ban/{twitchUserId}", async (
            string twitchUserId,
            IBotHelixClient botHelix,
            ISecureStorage storage,
            ITwitchOAuthService oauth,
            IModerationEventRepository repo,
            IUserRepository users,
            IChatEventBroadcaster broadcaster,
            CancellationToken ct) =>
        {
            string? broadcasterId = await ResolveBroadcasterIdAsync(storage, oauth, ct);
            if (broadcasterId is null)
            {
                return TypedResults.Problem(detail: "Broadcaster token not available.",
                    title: "Auth Error", statusCode: StatusCodes.Status401Unauthorized,
                    type: "https://wrkzg.app/problems/auth-error");
            }

            bool success = await botHelix.UnbanUserAsync(broadcasterId, twitchUserId, ct);

            ModerationEvent evt = await repo.CreateAsync(new ModerationEvent
            {
                TwitchUserId = twitchUserId,
                DisplayName = twitchUserId,
                EventType = ModerationEventType.TwitchUnban,
                Actor = "Dashboard",
                TwitchSuccess = success,
            }, ct);

            await broadcaster.BroadcastModerationActionAsync(evt, ct);

            if (success)
            {
                User? targetUser = await users.GetByTwitchIdAsync(twitchUserId, ct);
                if (targetUser is not null)
                {
                    targetUser.IsTwitchBanned = false;
                    await users.UpdateAsync(targetUser, ct);
                }
            }

            return success
                ? Results.Ok(new { success = true, eventId = evt.Id })
                : TypedResults.Problem(detail: "Twitch API rejected the unban.",
                    title: "Twitch Error", statusCode: StatusCodes.Status502BadGateway,
                    type: "https://wrkzg.app/problems/twitch-error");
        });

        group.MapPost("/shoutout", async (
            ShoutoutRequest request,
            IBroadcasterHelixClient broadcasterHelix,
            ISecureStorage storage,
            ITwitchOAuthService oauth,
            IModerationEventRepository repo,
            IChatEventBroadcaster broadcaster,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.TwitchUserId))
            {
                return TypedResults.Problem(detail: "TwitchUserId is required.", title: "Validation Error",
                    statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
            }

            string? broadcasterId = await ResolveBroadcasterIdAsync(storage, oauth, ct);
            if (broadcasterId is null)
            {
                return TypedResults.Problem(detail: "Broadcaster token not available.",
                    title: "Auth Error", statusCode: StatusCodes.Status401Unauthorized,
                    type: "https://wrkzg.app/problems/auth-error");
            }

            bool success = await broadcasterHelix.SendShoutoutAsync(broadcasterId, request.TwitchUserId, ct);

            ModerationEvent evt = await repo.CreateAsync(new ModerationEvent
            {
                TwitchUserId = request.TwitchUserId,
                DisplayName = request.DisplayName ?? request.TwitchUserId,
                EventType = ModerationEventType.TwitchShoutout,
                Actor = "Dashboard",
                TwitchSuccess = success,
            }, ct);

            await broadcaster.BroadcastModerationActionAsync(evt, ct);

            return success
                ? Results.Ok(new { success = true, eventId = evt.Id })
                : TypedResults.Problem(
                    detail: "Shoutout failed — rate limit or missing permissions.",
                    title: "Twitch Error", statusCode: StatusCodes.Status429TooManyRequests,
                    type: "https://wrkzg.app/problems/twitch-rate-limit");
        });

        // ─── Moderation Log ──────────────────────────────────────

        group.MapGet("/log", async (
            IModerationEventRepository repo,
            int? limit,
            int? days,
            CancellationToken ct) =>
        {
            IReadOnlyList<ModerationEvent> events = await repo.GetRecentAsync(limit ?? 100, ct);

            // Client-side date filter (SQLite DateTimeOffset limitation).
            if (days.HasValue && days.Value > 0)
            {
                DateTimeOffset since = DateTimeOffset.UtcNow.AddDays(-days.Value);
                events = events.Where(e => e.CreatedAt >= since).ToList();
            }

            return Results.Ok(events.Select(MapEvent));
        });

        group.MapGet("/log/{twitchUserId}", async (
            string twitchUserId,
            IModerationEventRepository repo,
            int? limit,
            int? days,
            CancellationToken ct) =>
        {
            IReadOnlyList<ModerationEvent> events = await repo.GetByUserAsync(twitchUserId, limit ?? 100, ct);

            // Client-side date filter (SQLite DateTimeOffset limitation).
            if (days.HasValue && days.Value > 0)
            {
                DateTimeOffset since = DateTimeOffset.UtcNow.AddDays(-days.Value);
                events = events.Where(e => e.CreatedAt >= since).ToList();
            }

            return Results.Ok(events.Select(MapEvent));
        });

        group.MapDelete("/log/cleanup", async (
            IModerationEventRepository repo,
            CancellationToken ct) =>
        {
            DateTimeOffset cutoff = DateTimeOffset.UtcNow.AddYears(-1);
            int deleted = await repo.DeleteOlderThanAsync(cutoff, ct);
            return Results.Ok(new { deleted, cutoff });
        });

        // ─── Live Viewers ────────────────────────────────────────

        group.MapGet("/viewers", async (
            IUserTrackingService tracking,
            IUserRepository users,
            CancellationToken ct) =>
        {
            IReadOnlyList<string> activeIds = tracking.GetActiveUserIds();

            if (activeIds.Count == 0)
            {
                return Results.Ok(Array.Empty<object>());
            }

            List<object> viewers = new();
            foreach (string twitchId in activeIds)
            {
                User? user = await users.GetByTwitchIdAsync(twitchId, ct);
                if (user is not null)
                {
                    viewers.Add(new
                    {
                        twitchId = user.TwitchId,
                        username = user.Username,
                        displayName = user.DisplayName,
                        isMod = user.IsMod,
                        isSubscriber = user.IsSubscriber,
                        isBroadcaster = user.IsBroadcaster,
                        isBanned = user.IsBanned,
                        isTwitchBanned = user.IsTwitchBanned,
                    });
                }
            }

            return Results.Ok(viewers);
        });
    }

    // ─── Helpers ──────────────────────────────────────────────

    private static object MapEvent(ModerationEvent e) => new
    {
        id = e.Id,
        twitchUserId = e.TwitchUserId,
        displayName = e.DisplayName,
        eventType = e.EventType.ToString(),
        actor = e.Actor,
        reason = e.Reason,
        durationSeconds = e.DurationSeconds,
        twitchSuccess = e.TwitchSuccess,
        createdAt = e.CreatedAt,
    };

    private static async Task<string?> ResolveBroadcasterIdAsync(
        ISecureStorage storage, ITwitchOAuthService oauth, CancellationToken ct)
    {
        TwitchTokens? tokens = await storage.LoadTokensAsync(TokenType.Broadcaster, ct);
        if (tokens is null)
        {
            return null;
        }

        TwitchTokenValidation? validation = await oauth.ValidateTokenAsync(tokens.AccessToken, ct);
        if (validation is null)
        {
            try
            {
                TwitchTokens refreshed = await oauth.RefreshTokenAsync(tokens.RefreshToken, ct);
                await storage.SaveTokensAsync(TokenType.Broadcaster, refreshed, ct);
                validation = await oauth.ValidateTokenAsync(refreshed.AccessToken, ct);
            }
            catch
            {
                return null;
            }
        }

        return validation?.UserId;
    }
}

// ─── Request DTOs ────────────────────────────────────────

/// <summary>Request for timing out a user.</summary>
public record TimeoutRequest(string TwitchUserId, int DurationSeconds, string? DisplayName = null, string? Reason = null);

/// <summary>Request for banning a user.</summary>
public record BanRequest(string TwitchUserId, string? DisplayName = null, string? Reason = null);

/// <summary>Request for sending a shoutout.</summary>
public record ShoutoutRequest(string TwitchUserId, string? DisplayName = null);

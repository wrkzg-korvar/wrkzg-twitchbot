using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wrkzg.Core.Interfaces;

namespace Wrkzg.Api.Endpoints;

/// <summary>
/// REST endpoints for managing event notification settings (templates, toggles).
/// </summary>
public static class NotificationEndpoints
{
    private static readonly string[] EventTypes = { "follow", "subscribe", "gift", "resub", "raid" };

    private static readonly Dictionary<string, string> DefaultTemplates = new()
    {
        ["follow"] = "Welcome {user}! Thanks for the follow!",
        ["subscribe"] = "{user} just subscribed (Tier {tier})! Thank you!",
        ["gift"] = "{user} gifted {count} Tier {tier} subs! Amazing!",
        ["resub"] = "{user} resubscribed for {months} months (Tier {tier})! {message}",
        ["raid"] = "{user} is raiding with {viewers} viewers! Welcome raiders!"
    };

    private static readonly Dictionary<string, string[]> EventVariables = new()
    {
        ["follow"] = new[] { "user" },
        ["subscribe"] = new[] { "user", "tier" },
        ["gift"] = new[] { "user", "count", "tier" },
        ["resub"] = new[] { "user", "months", "tier", "message" },
        ["raid"] = new[] { "user", "viewers" }
    };

    public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/notifications").WithTags("Notifications");

        // GET /api/notifications/settings — all notification settings
        group.MapGet("/settings", async (ISettingsRepository repo, CancellationToken ct) =>
        {
            Dictionary<string, object> result = new();

            foreach (string eventType in EventTypes)
            {
                string? enabled = await repo.GetAsync($"Notifications.{eventType}.Enabled", ct);
                string? template = await repo.GetAsync($"Notifications.{eventType}.Template", ct);

                Dictionary<string, object?> entry = new()
                {
                    ["enabled"] = !string.Equals(enabled, "false", StringComparison.OrdinalIgnoreCase),
                    ["template"] = template ?? DefaultTemplates.GetValueOrDefault(eventType, ""),
                    ["variables"] = EventVariables.GetValueOrDefault(eventType, Array.Empty<string>())
                };

                if (eventType == "raid")
                {
                    string? autoSo = await repo.GetAsync("Notifications.raid.AutoShoutout", ct);
                    entry["autoShoutout"] = string.Equals(autoSo, "true", StringComparison.OrdinalIgnoreCase);
                }

                result[eventType] = entry;
            }

            return Results.Ok(result);
        });

        // PUT /api/notifications/settings/{type} — update one event type
        group.MapPut("/settings/{type}", async (
            string type,
            UpdateNotificationRequest request,
            ISettingsRepository repo,
            CancellationToken ct) =>
        {
            if (Array.IndexOf(EventTypes, type.ToLowerInvariant()) < 0)
            {
                string safeType = type.Length > 50 ? type[..50] : type;
                return Results.BadRequest(new { error = $"Unknown event type: '{safeType}'." });
            }

            string normalizedType = type.ToLowerInvariant();

            if (request.Enabled.HasValue)
            {
                await repo.SetAsync($"Notifications.{normalizedType}.Enabled",
                    request.Enabled.Value ? "true" : "false", ct);
            }

            if (request.Template is not null)
            {
                if (request.Template.Length > 500)
                {
                    return Results.BadRequest(new { error = "Template must be 500 characters or less." });
                }

                await repo.SetAsync($"Notifications.{normalizedType}.Template",
                    request.Template, ct);
            }

            if (normalizedType == "raid" && request.AutoShoutout.HasValue)
            {
                await repo.SetAsync("Notifications.raid.AutoShoutout",
                    request.AutoShoutout.Value ? "true" : "false", ct);
            }

            return Results.Ok(new { saved = true });
        });

        // POST /api/notifications/test/{type} — send a test notification
        group.MapPost("/test/{type}", async (
            string type,
            ITwitchChatClient chatClient,
            ISettingsRepository repo,
            CancellationToken ct) =>
        {
            if (Array.IndexOf(EventTypes, type.ToLowerInvariant()) < 0)
            {
                string safeType = type.Length > 50 ? type[..50] : type;
                return Results.BadRequest(new { error = $"Unknown event type: '{safeType}'." });
            }

            string normalizedType = type.ToLowerInvariant();
            string? template = await repo.GetAsync($"Notifications.{normalizedType}.Template", ct);

            if (string.IsNullOrWhiteSpace(template))
            {
                template = DefaultTemplates.GetValueOrDefault(normalizedType, "");
            }

            // Replace with test values
            string message = template
                .Replace("{user}", "TestUser123")
                .Replace("{tier}", "1")
                .Replace("{count}", "5")
                .Replace("{months}", "12")
                .Replace("{message}", "Love this stream!")
                .Replace("{viewers}", "42");

            await chatClient.SendMessageAsync(message, ct);

            return Results.Ok(new { sent = true, message });
        });
    }
}

/// <summary>Request body for updating notification settings.</summary>
public sealed record UpdateNotificationRequest(
    bool? Enabled = null,
    string? Template = null,
    bool? AutoShoutout = null
);

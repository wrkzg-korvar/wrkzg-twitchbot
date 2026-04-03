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
/// REST endpoints for OBS overlay configuration.
/// </summary>
public static class OverlayEndpoints
{
    private static readonly string[] OverlayTypes = { "alerts", "chat", "poll", "raffle", "counter", "events" };

    private static readonly Dictionary<string, Dictionary<string, string>> Defaults = new()
    {
        ["alerts"] = new()
        {
            ["enabled"] = "true",
            ["duration"] = "5000",
            ["fontSize"] = "24",
            ["fontFamily"] = "system-ui",
            ["textColor"] = "#ffffff",
            ["accentColor"] = "#8BBF4C",
            ["animation"] = "slideDown",
            ["showFollows"] = "true",
            ["showSubs"] = "true",
            ["showGiftSubs"] = "true",
            ["showRaids"] = "true",
            ["followMessage"] = "{user} just followed!",
            ["subMessage"] = "{user} subscribed (Tier {tier})!",
            ["giftMessage"] = "{user} gifted {count} subs!",
            ["raidMessage"] = "{user} is raiding with {viewers} viewers!",
            // Per-event overrides (empty = use global value)
            ["follow.enabled"] = "true",
            ["follow.image"] = "",
            ["follow.sound"] = "",
            ["follow.soundVolume"] = "80",
            ["follow.message"] = "{user} just followed!",
            ["follow.animation"] = "",
            ["follow.duration"] = "",
            ["subscribe.enabled"] = "true",
            ["subscribe.image"] = "",
            ["subscribe.sound"] = "",
            ["subscribe.soundVolume"] = "80",
            ["subscribe.message"] = "{user} subscribed (Tier {tier})!",
            ["subscribe.animation"] = "",
            ["subscribe.duration"] = "",
            ["giftsub.enabled"] = "true",
            ["giftsub.image"] = "",
            ["giftsub.sound"] = "",
            ["giftsub.soundVolume"] = "80",
            ["giftsub.message"] = "{user} gifted {count} subs!",
            ["giftsub.animation"] = "",
            ["giftsub.duration"] = "",
            ["resub.enabled"] = "true",
            ["resub.image"] = "",
            ["resub.sound"] = "",
            ["resub.soundVolume"] = "80",
            ["resub.message"] = "{user} resubscribed for {months} months!",
            ["resub.animation"] = "",
            ["resub.duration"] = "",
            ["raid.enabled"] = "true",
            ["raid.image"] = "",
            ["raid.sound"] = "",
            ["raid.soundVolume"] = "80",
            ["raid.message"] = "{user} is raiding with {viewers} viewers!",
            ["raid.animation"] = "",
            ["raid.duration"] = "",
            ["channelpoint.enabled"] = "true",
            ["channelpoint.image"] = "",
            ["channelpoint.sound"] = "",
            ["channelpoint.soundVolume"] = "80",
            ["channelpoint.message"] = "{user} redeemed {rewardTitle}!",
            ["channelpoint.animation"] = "",
            ["channelpoint.duration"] = "",
            // Custom CSS
            ["customCSS"] = "",
        },
        ["chat"] = new()
        {
            ["enabled"] = "true",
            ["maxMessages"] = "15",
            ["fontSize"] = "14",
            ["fontFamily"] = "system-ui",
            ["textColor"] = "#ffffff",
            ["showBadges"] = "true",
            ["fadeAfter"] = "30",
            ["direction"] = "bottomUp",
            ["customCSS"] = "",
        },
        ["poll"] = new()
        {
            ["enabled"] = "true",
            ["fontSize"] = "18",
            ["barColor"] = "#8BBF4C",
            ["showPercentage"] = "true",
            ["animation"] = "growBar",
            ["customCSS"] = "",
        },
        ["raffle"] = new()
        {
            ["enabled"] = "true",
            ["fontSize"] = "20",
            ["drawAnimation"] = "spin",
            ["confetti"] = "true",
            ["customCSS"] = "",
        },
        ["counter"] = new()
        {
            ["enabled"] = "true",
            ["fontSize"] = "32",
            ["fontFamily"] = "system-ui",
            ["textColor"] = "#ffffff",
            ["label"] = "{name}: {value}",
            ["animateChange"] = "true",
            ["counterId"] = "",
            ["customCSS"] = "",
        },
        ["events"] = new()
        {
            ["enabled"] = "true",
            ["maxItems"] = "5",
            ["fontSize"] = "14",
            ["fadeAfter"] = "60",
            ["showFollows"] = "true",
            ["showSubs"] = "true",
            ["showRaids"] = "true",
            ["customCSS"] = "",
        },
    };

    private static readonly Dictionary<string, (int width, int height)> RecommendedSizes = new()
    {
        ["alerts"] = (800, 200),
        ["chat"] = (400, 600),
        ["poll"] = (600, 300),
        ["raffle"] = (600, 300),
        ["counter"] = (300, 80),
        ["events"] = (350, 400),
    };

    public static void MapOverlayEndpoints(this IEndpointRouteBuilder app)
    {
        // Health-check endpoint under /overlay/ prefix — exempt from both auth and CORS.
        // Used by overlay reconnect polling to detect when the backend is back.
        app.MapGet("/overlay/health", () => Results.Ok());

        RouteGroupBuilder group = app.MapGroup("/api/overlays").WithTags("Overlays");

        // GET /api/overlays/settings — all overlay settings
        group.MapGet("/settings", async (ISettingsRepository repo, CancellationToken ct) =>
        {
            IDictionary<string, string> allSettings = await repo.GetAllAsync(ct);
            Dictionary<string, Dictionary<string, string>> result = new();

            foreach (string type in OverlayTypes)
            {
                Dictionary<string, string> typeSettings = new();
                string prefix = $"Overlay.{type}.";

                if (Defaults.TryGetValue(type, out Dictionary<string, string>? defaults))
                {
                    foreach (KeyValuePair<string, string> kvp in defaults)
                    {
                        string settingKey = prefix + kvp.Key;
                        typeSettings[kvp.Key] = allSettings.TryGetValue(settingKey, out string? value)
                            ? value
                            : kvp.Value;
                    }
                }

                result[type] = typeSettings;
            }

            return Results.Ok(result);
        });

        // GET /api/overlays/settings/{type} — settings for one overlay type
        group.MapGet("/settings/{type}", async (string type, ISettingsRepository repo, CancellationToken ct) =>
        {
            string normalizedType = type.ToLowerInvariant();
            if (!Defaults.ContainsKey(normalizedType))
            {
                return Results.BadRequest(new { error = $"Unknown overlay type: '{type}'." });
            }

            IDictionary<string, string> allSettings = await repo.GetAllAsync(ct);
            Dictionary<string, string> result = new();
            string prefix = $"Overlay.{normalizedType}.";

            foreach (KeyValuePair<string, string> kvp in Defaults[normalizedType])
            {
                string settingKey = prefix + kvp.Key;
                result[kvp.Key] = allSettings.TryGetValue(settingKey, out string? value)
                    ? value
                    : kvp.Value;
            }

            return Results.Ok(result);
        });

        // GET /api/overlays/defaults/{type} — get built-in defaults (for editor reset)
        group.MapGet("/defaults/{type}", (string type) =>
        {
            string normalizedType = type.ToLowerInvariant();
            if (!Defaults.ContainsKey(normalizedType))
            {
                return Results.BadRequest(new { error = $"Unknown overlay type: '{type}'." });
            }

            return Results.Ok(Defaults[normalizedType]);
        });

        // PUT /api/overlays/settings/{type} — update settings for one overlay type
        group.MapPut("/settings/{type}", async (
            string type,
            Dictionary<string, string> settings,
            ISettingsRepository repo,
            CancellationToken ct) =>
        {
            string normalizedType = type.ToLowerInvariant();
            if (!Defaults.ContainsKey(normalizedType))
            {
                return Results.BadRequest(new { error = $"Unknown overlay type: '{type}'." });
            }

            string prefix = $"Overlay.{normalizedType}.";
            Dictionary<string, string> toSave = new();

            foreach (KeyValuePair<string, string> kvp in settings)
            {
                int maxLen = kvp.Key == "customCSS" ? 10000 : 500;
                if (kvp.Value.Length > maxLen)
                {
                    return Results.BadRequest(new { error = $"Value for '{kvp.Key}' exceeds {maxLen} characters." });
                }

                toSave[prefix + kvp.Key] = kvp.Value;
            }

            if (toSave.Count > 0)
            {
                await repo.SetManyAsync(toSave, ct);
            }

            return Results.Ok(new { saved = true });
        });

        // GET /api/overlays/url/{type} — OBS-ready URL info
        group.MapGet("/url/{type}", (string type, HttpContext context) =>
        {
            string normalizedType = type.ToLowerInvariant();
            if (!RecommendedSizes.ContainsKey(normalizedType))
            {
                return Results.BadRequest(new { error = $"Unknown overlay type: '{type}'." });
            }

            (int width, int height) = RecommendedSizes[normalizedType];
            string host = context.Request.Host.ToString();

            return Results.Ok(new
            {
                url = $"http://{host}/overlay/{normalizedType}",
                width,
                height,
                instructions = $"Add as Browser Source in OBS. Set width to {width} and height to {height}."
            });
        });

        // GET /api/overlays/data/poll/active — active poll data for overlay (no auth required)
        group.MapGet("/data/poll/active", async (IPollRepository repo, CancellationToken ct) =>
        {
            Poll? poll = await repo.GetActiveAsync(ct);
            if (poll is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new
            {
                poll.Id,
                poll.Question,
                poll.Options,
                poll.EndsAt,
                poll.IsActive
            });
        });

        // GET /api/overlays/data/counter/{id} — counter data for overlay (no auth required)
        group.MapGet("/data/counter/{id:int}", async (int id, ICounterRepository repo, CancellationToken ct) =>
        {
            Counter? counter = await repo.GetByIdAsync(id, ct);
            return counter is not null
                ? Results.Ok(new { id = counter.Id, name = counter.Name, value = counter.Value })
                : Results.NotFound();
        });

        // GET /api/overlays/data/counters — all counters for dropdown (no auth required)
        group.MapGet("/data/counters", async (ICounterRepository repo, CancellationToken ct) =>
        {
            System.Collections.Generic.IReadOnlyList<Counter> counters = await repo.GetAllAsync(ct);
            return Results.Ok(counters.Select(c => new { id = c.Id, name = c.Name, value = c.Value, trigger = c.Trigger }));
        });

        // POST /api/overlays/test/{event} — fire a test SignalR event for overlay preview
        group.MapPost("/test/{eventType}", async (
            string eventType,
            IChatEventBroadcaster broadcaster,
            CancellationToken ct) =>
        {
            string normalized = eventType.ToLowerInvariant();

            switch (normalized)
            {
                case "follow":
                    await broadcaster.BroadcastFollowEventAsync("TestViewer42", ct);
                    return Results.Ok(new { sent = true, eventType = "follow" });

                case "subscribe":
                    await broadcaster.BroadcastSubscribeEventAsync("LoyalSub99", 1, ct);
                    return Results.Ok(new { sent = true, eventType = "subscribe" });

                case "giftsub":
                    await broadcaster.BroadcastGiftSubEventAsync("GenerousGifter", 5, 1, ct);
                    return Results.Ok(new { sent = true, eventType = "giftsub" });

                case "resub":
                    await broadcaster.BroadcastResubEventAsync("OGViewer", 24, 1, "Love this stream!", ct);
                    return Results.Ok(new { sent = true, eventType = "resub" });

                case "raid":
                    await broadcaster.BroadcastRaidEventAsync("BigStreamer", 150, ct);
                    return Results.Ok(new { sent = true, eventType = "raid" });

                case "counter":
                    await broadcaster.BroadcastCounterUpdatedAsync(1, "Deaths", 42, ct);
                    return Results.Ok(new { sent = true, eventType = "counter" });

                default:
                    string safeType = normalized.Length > 50 ? normalized[..50] : normalized;
                    return Results.BadRequest(new { error = $"Unknown test event: '{safeType}'. Use: follow, subscribe, giftsub, resub, raid, counter." });
            }
        });

        // Song request queue for overlay (no auth needed)
        group.MapGet("/data/song-queue", async (ISongRequestRepository repo, CancellationToken ct) =>
        {
            IReadOnlyList<SongRequest> queue = await repo.GetQueueAsync(ct);
            return Results.Ok(queue);
        });
    }
}

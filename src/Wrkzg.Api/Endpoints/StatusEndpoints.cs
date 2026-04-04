using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wrkzg.Core.Interfaces;

namespace Wrkzg.Api.Endpoints;

/// <summary>
/// Status endpoint for the dashboard overview.
/// Returns bot connection status, stream live status, auth state, and app version.
/// </summary>
public static class StatusEndpoints
{
    private static string? _cachedVersion;

    /// <summary>Registers the dashboard status overview API endpoint.</summary>
    public static void MapStatusEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/status").WithTags("Status");

        group.MapGet("/", async (
            ITwitchChatClient chatClient,
            ITwitchHelixClient helix,
            ISecureStorage storage,
            CancellationToken ct) =>
        {
            // Bot connection info
            object bot = new
            {
                isConnected = chatClient.IsConnected,
                channel = chatClient.JoinedChannel
            };

            // Stream info (if bot is connected and we have a channel)
            object? stream = null;
            if (chatClient.JoinedChannel is not null)
            {
                StreamInfo? streamInfo = await helix.GetStreamAsync(chatClient.JoinedChannel, ct);
                if (streamInfo is not null)
                {
                    stream = new
                    {
                        isLive = true,
                        viewerCount = streamInfo.ViewerCount,
                        title = streamInfo.Title,
                        game = streamInfo.GameName,
                        startedAt = streamInfo.StartedAt
                    };
                }
            }

            // Auth state
            bool hasBot = await storage.LoadTokensAsync(Core.Models.TokenType.Bot, ct) is not null;
            bool hasBroadcaster = await storage.LoadTokensAsync(Core.Models.TokenType.Broadcaster, ct) is not null;

            // OS platform (Photino's UA doesn't include OS info)
            string platform = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macos"
                : RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "windows"
                : "linux";

            // App version from version.json
            string version = GetAppVersion();

            return Results.Ok(new
            {
                bot,
                stream = stream ?? new { isLive = false, viewerCount = 0, title = (string?)null, game = (string?)null, startedAt = (string?)null },
                auth = new
                {
                    botTokenPresent = hasBot,
                    broadcasterTokenPresent = hasBroadcaster
                },
                platform,
                version
            });
        });
    }

    /// <summary>
    /// Reads the app version from version.json. Cached after first read.
    /// Searches next to the executable and in common development paths.
    /// </summary>
    private static string GetAppVersion()
    {
        if (_cachedVersion is not null)
        {
            return _cachedVersion;
        }

        string[] candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "version.json"),
            Path.GetFullPath("version.json"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "version.json")),
        };

        foreach (string path in candidates)
        {
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    using JsonDocument doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("version", out JsonElement versionElement))
                    {
                        _cachedVersion = versionElement.GetString() ?? "0.0.0";
                        return _cachedVersion;
                    }
                }
                catch
                {
                    // Ignore parse errors, try next candidate
                }
            }
        }

        _cachedVersion = "0.0.0";
        return _cachedVersion;
    }
}

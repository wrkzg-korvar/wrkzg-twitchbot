using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wrkzg.Core.Interfaces;

namespace Wrkzg.Api.Endpoints;

/// <summary>
/// Status endpoint for the dashboard overview.
/// Returns bot connection status, stream live status, and auth state.
/// </summary>
public static class StatusEndpoints
{
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

            return Results.Ok(new
            {
                bot,
                stream = stream ?? new { isLive = false, viewerCount = 0, title = (string?)null, game = (string?)null, startedAt = (string?)null },
                auth = new
                {
                    botTokenPresent = hasBot,
                    broadcasterTokenPresent = hasBroadcaster
                },
                platform
            });
        });
    }
}

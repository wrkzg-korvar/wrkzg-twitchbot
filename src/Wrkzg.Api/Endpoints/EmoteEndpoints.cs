using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wrkzg.Core.Interfaces;

namespace Wrkzg.Api.Endpoints;

/// <summary>
/// REST endpoints for Twitch emote data (cached global + channel emotes).
/// </summary>
public static class EmoteEndpoints
{
    /// <summary>Registers emote list and refresh API endpoints.</summary>
    public static void MapEmoteEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/emotes").WithTags("Emotes");

        group.MapGet("/", (IEmoteService emoteService) =>
        {
            IReadOnlyList<EmoteDto> emotes = emoteService.GetCachedEmotes();
            return Results.Ok(emotes);
        });

        group.MapPost("/refresh", async (IEmoteService emoteService, CancellationToken ct) =>
        {
            await emoteService.RefreshAsync(ct);
            IReadOnlyList<EmoteDto> emotes = emoteService.GetCachedEmotes();
            return Results.Ok(new { count = emotes.Count });
        });
    }
}

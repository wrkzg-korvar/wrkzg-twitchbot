using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wrkzg.Core.Interfaces;

namespace Wrkzg.Api.Endpoints;

/// <summary>
/// Bot control endpoints — connect/disconnect the IRC bot.
/// </summary>
public static class BotEndpoints
{
    /// <summary>Registers bot connection control API endpoints.</summary>
    public static void MapBotEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/bot").WithTags("Bot");

        group.MapPost("/connect", async (IBotConnectionService botService, CancellationToken ct) =>
        {
            bool connected = await botService.TryConnectAsync(ct);
            return Results.Ok(new { connected });
        });
    }
}

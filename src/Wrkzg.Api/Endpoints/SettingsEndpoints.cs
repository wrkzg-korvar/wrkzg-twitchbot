using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wrkzg.Core.Interfaces;

namespace Wrkzg.Api.Endpoints;

public static class SettingsEndpoints
{
    public static void MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/settings").WithTags("Settings");

        // GET /api/settings — returns all settings as key-value map
        group.MapGet("/", async (ISettingsRepository repo, CancellationToken ct) =>
        {
            IDictionary<string, string> settings = await repo.GetAllAsync(ct);
            return Results.Ok(settings);
        });

        // PUT /api/settings — update one or more settings
        group.MapPut("/", async (Dictionary<string, string> updates, ISettingsRepository repo, CancellationToken ct) =>
        {
            await repo.SetManyAsync(updates, ct);
            IDictionary<string, string> all = await repo.GetAllAsync(ct);
            return Results.Ok(all);
        });
    }
}

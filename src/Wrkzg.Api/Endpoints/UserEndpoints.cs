using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Api.Endpoints;

/// <summary>
/// REST endpoints for viewer/user data management.
/// </summary>
public static class UserEndpoints
{
    /// <summary>Registers user listing and update API endpoints.</summary>
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/users").WithTags("Users");

        // GET /api/users?sortBy=points&order=desc&limit=50
        group.MapGet("/", async (
            string? sortBy,
            string? order,
            int? limit,
            CancellationToken ct,
            IUserRepository repo) =>
        {
            int take = limit is > 0 and <= 500 ? limit.Value : 50;
            string sort = sortBy?.ToLowerInvariant() ?? "points";

            IReadOnlyList<User> users = sort switch
            {
                "watchtime" => await repo.GetTopByWatchTimeAsync(take, ct),
                _ => await repo.GetTopByPointsAsync(take, ct),
            };

            return Results.Ok(users);
        });

        // GET /api/users/{id}
        group.MapGet("/{id:int}", async (int id, IUserRepository repo, CancellationToken ct) =>
        {
            User? user = await repo.GetByIdAsync(id, ct);
            return user is null ? Results.NotFound() : Results.Ok(user);
        });

        // PUT /api/users/{id} — update points or ban status
        group.MapPut("/{id:int}", async (int id, UpdateUserRequest request, IUserRepository repo, CancellationToken ct) =>
        {
            User? user = await repo.GetByIdAsync(id, ct);
            if (user is null)
            {
                return Results.NotFound();
            }

            if (request.Points.HasValue)
            {
                user.Points = request.Points.Value;
            }

            if (request.IsBanned.HasValue)
            {
                user.IsBanned = request.IsBanned.Value;
            }

            await repo.UpdateAsync(user, ct);
            return Results.Ok(user);
        });
    }
}

/// <summary>Request payload for updating a user's points or ban status.</summary>
public sealed record UpdateUserRequest(
    long? Points = null,
    bool? IsBanned = null
);

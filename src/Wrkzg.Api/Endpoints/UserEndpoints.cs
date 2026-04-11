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

        // GET /api/users?search=korvar&sortBy=points&order=desc&page=1&pageSize=50
        group.MapGet("/", async (
            string? search,
            string? sortBy,
            string? order,
            int? page,
            int? pageSize,
            int? limit,
            CancellationToken ct,
            IUserRepository repo) =>
        {
            int p = page is > 0 ? page.Value : 1;
            int ps = pageSize is > 0 and <= 200 ? pageSize.Value
                   : limit is > 0 and <= 200 ? limit.Value
                   : 50;
            string sort = sortBy ?? "points";
            string dir = order ?? "desc";

            PaginatedResult<User> result = await repo.GetPaginatedAsync(search, sort, dir, p, ps, ct);
            return Results.Ok(result);
        });

        // GET /api/users/count -- total tracked users
        group.MapGet("/count", async (IUserRepository repo, CancellationToken ct) =>
        {
            int count = await repo.CountAsync(ct);
            return Results.Ok(new { count });
        });

        // GET /api/users/{id}
        group.MapGet("/{id:int}", async (int id, IUserRepository repo, CancellationToken ct) =>
        {
            User? user = await repo.GetByIdAsync(id, ct);
            return user is null ? TypedResults.Problem(title: "Not Found", statusCode: StatusCodes.Status404NotFound, type: "https://wrkzg.app/problems/not-found") : Results.Ok(user);
        });

        // PUT /api/users/{id} — update points or ban status
        group.MapPut("/{id:int}", async (int id, UpdateUserRequest request, IUserRepository repo, CancellationToken ct) =>
        {
            User? user = await repo.GetByIdAsync(id, ct);
            if (user is null)
            {
                return TypedResults.Problem(title: "Not Found", statusCode: StatusCodes.Status404NotFound, type: "https://wrkzg.app/problems/not-found");
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

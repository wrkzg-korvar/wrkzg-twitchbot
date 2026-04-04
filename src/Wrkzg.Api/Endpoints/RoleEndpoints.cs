using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Core.Services;

namespace Wrkzg.Api.Endpoints;

/// <summary>
/// REST endpoints for the Roles and Ranks system.
/// </summary>
public static class RoleEndpoints
{
    /// <summary>Registers role CRUD, assignment, and evaluation API endpoints.</summary>
    public static void MapRoleEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/roles").WithTags("Roles");

        group.MapGet("/", async (IRoleRepository repo, CancellationToken ct) =>
        {
            IReadOnlyList<Role> roles = await repo.GetAllAsync(ct);

            List<object> result = new();
            foreach (Role role in roles)
            {
                int userCount = await repo.GetUserCountForRoleAsync(role.Id, ct);
                result.Add(new
                {
                    role.Id,
                    role.Name,
                    role.Priority,
                    role.Color,
                    role.Icon,
                    role.AutoAssign,
                    role.CreatedAt,
                    userCount
                });
            }

            return Results.Ok(result);
        });

        group.MapPost("/", async (CreateRoleRequest request, IRoleRepository repo, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Results.BadRequest(new { error = "Role name is required." });
            }

            Role role = new()
            {
                Name = request.Name.Trim(),
                Priority = request.Priority ?? 0,
                Color = request.Color,
                Icon = request.Icon,
                AutoAssign = request.AutoAssign
            };

            role = await repo.CreateAsync(role, ct);
            return Results.Created($"/api/roles/{role.Id}", role);
        });

        group.MapPut("/{id:int}", async (int id, UpdateRoleRequest request, IRoleRepository repo, CancellationToken ct) =>
        {
            Role? role = await repo.GetByIdAsync(id, ct);
            if (role is null)
            {
                return Results.NotFound();
            }

            if (request.Name is not null)
            {
                role.Name = request.Name;
            }
            if (request.Priority.HasValue)
            {
                role.Priority = request.Priority.Value;
            }
            if (request.Color is not null)
            {
                role.Color = request.Color;
            }
            if (request.Icon is not null)
            {
                role.Icon = request.Icon;
            }
            if (request.HasAutoAssign)
            {
                role.AutoAssign = request.AutoAssign;
            }

            await repo.UpdateAsync(role, ct);
            return Results.Ok(role);
        });

        group.MapDelete("/{id:int}", async (int id, IRoleRepository repo, CancellationToken ct) =>
        {
            await repo.DeleteAsync(id, ct);
            return Results.NoContent();
        });

        group.MapGet("/{id:int}/users", async (int id, IRoleRepository repo, CancellationToken ct) =>
        {
            IReadOnlyList<User> users = await repo.GetUsersWithRoleAsync(id, ct);
            return Results.Ok(users.Select(u => new
            {
                u.Id,
                u.TwitchId,
                u.Username,
                u.DisplayName,
                u.Points,
                u.WatchedMinutes,
                u.MessageCount
            }));
        });

        group.MapPost("/assign", async (AssignRoleRequest request, IRoleRepository repo, CancellationToken ct) =>
        {
            await repo.AssignRoleAsync(request.UserId, request.RoleId, isAutoAssigned: false, ct);
            return Results.Ok();
        });

        group.MapDelete("/assign/{userId:int}/{roleId:int}", async (int userId, int roleId, IRoleRepository repo, CancellationToken ct) =>
        {
            await repo.RemoveRoleAsync(userId, roleId, ct);
            return Results.NoContent();
        });

        group.MapPost("/evaluate", async (RoleEvaluationService evaluationService, CancellationToken ct) =>
        {
            int changed = await evaluationService.EvaluateAllUsersAsync(ct);
            return Results.Ok(new { usersUpdated = changed });
        });

        // User roles endpoint (placed under /api/users path but registered here)
        app.MapGet("/api/users/{id:int}/roles", async (int id, IRoleRepository repo, CancellationToken ct) =>
        {
            IReadOnlyList<Role> roles = await repo.GetUserRolesAsync(id, ct);
            return Results.Ok(roles);
        }).WithTags("Roles");
    }
}

/// <summary>Request payload for creating a new role.</summary>
public record CreateRoleRequest(
    string Name,
    int? Priority,
    string? Color,
    string? Icon,
    RoleAutoAssignCriteria? AutoAssign);

/// <summary>Request payload for updating an existing role.</summary>
public record UpdateRoleRequest(
    string? Name,
    int? Priority,
    string? Color,
    string? Icon,
    RoleAutoAssignCriteria? AutoAssign)
{
    /// <summary>Indicates whether AutoAssign was explicitly provided in the request.</summary>
    public bool HasAutoAssign => AutoAssign is not null;
}

/// <summary>Request payload for assigning a role to a user.</summary>
public record AssignRoleRequest(int UserId, int RoleId);

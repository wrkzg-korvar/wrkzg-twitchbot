using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Api.Endpoints;

/// <summary>
/// REST endpoints for managing custom chat commands.
/// Used by the dashboard to create, edit, toggle, and delete commands.
/// </summary>
public static class CommandEndpoints
{
    public static void MapCommandEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/commands").WithTags("Commands");

        // GET /api/commands — list all commands
        group.MapGet("/", async (ICommandRepository repo, CancellationToken ct) =>
        {
            IReadOnlyList<Command> commands = await repo.GetAllAsync(ct);
            return Results.Ok(commands);
        });

        // GET /api/commands/{id} — get single command
        group.MapGet("/{id:int}", async (int id, ICommandRepository repo, CancellationToken ct) =>
        {
            Command? command = await repo.GetByIdAsync(id, ct);
            return command is null ? Results.NotFound() : Results.Ok(command);
        });

        // POST /api/commands — create new command
        group.MapPost("/", async (CreateCommandRequest request, ICommandRepository repo, CancellationToken ct) =>
        {
            // Validate
            if (string.IsNullOrWhiteSpace(request.Trigger) || !request.Trigger.StartsWith('!'))
            {
                return Results.BadRequest(new { error = "Trigger must start with '!' and be non-empty." });
            }

            if (string.IsNullOrWhiteSpace(request.ResponseTemplate))
            {
                return Results.BadRequest(new { error = "ResponseTemplate is required." });
            }

            if (request.ResponseTemplate.Length > 500)
            {
                return Results.BadRequest(new { error = "ResponseTemplate must be 500 characters or less." });
            }

            // Check for duplicate trigger
            Command? existing = await repo.GetByTriggerOrAliasAsync(request.Trigger.ToLowerInvariant(), ct);
            if (existing is not null)
            {
                return Results.BadRequest(new { error = $"A command with trigger '{request.Trigger}' already exists." });
            }

            Command command = new()
            {
                Trigger = request.Trigger.ToLowerInvariant(),
                Aliases = request.Aliases ?? System.Array.Empty<string>(),
                ResponseTemplate = request.ResponseTemplate,
                PermissionLevel = request.PermissionLevel,
                GlobalCooldownSeconds = request.GlobalCooldownSeconds,
                UserCooldownSeconds = request.UserCooldownSeconds
            };

            Command created = await repo.CreateAsync(command, ct);
            return Results.Created($"/api/commands/{created.Id}", created);
        });

        // PUT /api/commands/{id} — update command (partial update)
        group.MapPut("/{id:int}", async (int id, UpdateCommandRequest request, ICommandRepository repo, CancellationToken ct) =>
        {
            Command? command = await repo.GetByIdAsync(id, ct);
            if (command is null)
            {
                return Results.NotFound();
            }

            // Apply partial updates
            if (request.Trigger is not null)
            {
                command.Trigger = request.Trigger.ToLowerInvariant();
            }

            if (request.Aliases is not null)
            {
                command.Aliases = request.Aliases;
            }

            if (request.ResponseTemplate is not null)
            {
                if (request.ResponseTemplate.Length > 500)
                {
                    return Results.BadRequest(new { error = "ResponseTemplate must be 500 characters or less." });
                }

                command.ResponseTemplate = request.ResponseTemplate;
            }

            if (request.PermissionLevel.HasValue)
            {
                command.PermissionLevel = request.PermissionLevel.Value;
            }

            if (request.GlobalCooldownSeconds.HasValue)
            {
                command.GlobalCooldownSeconds = request.GlobalCooldownSeconds.Value;
            }

            if (request.UserCooldownSeconds.HasValue)
            {
                command.UserCooldownSeconds = request.UserCooldownSeconds.Value;
            }

            if (request.IsEnabled.HasValue)
            {
                command.IsEnabled = request.IsEnabled.Value;
            }

            await repo.UpdateAsync(command, ct);
            return Results.Ok(command);
        });

        // DELETE /api/commands/{id}
        group.MapDelete("/{id:int}", async (int id, ICommandRepository repo, CancellationToken ct) =>
        {
            Command? command = await repo.GetByIdAsync(id, ct);
            if (command is null)
            {
                return Results.NotFound();
            }

            await repo.DeleteAsync(id, ct);
            return Results.NoContent();
        });
    }
}

/// <summary>Request body for creating a new command.</summary>
public sealed record CreateCommandRequest(
    string Trigger,
    string[]? Aliases,
    string ResponseTemplate,
    PermissionLevel PermissionLevel = PermissionLevel.Everyone,
    int GlobalCooldownSeconds = 0,
    int UserCooldownSeconds = 0
);

/// <summary>Request body for updating a command. All fields optional (partial update).</summary>
public sealed record UpdateCommandRequest(
    string? Trigger = null,
    string[]? Aliases = null,
    string? ResponseTemplate = null,
    PermissionLevel? PermissionLevel = null,
    int? GlobalCooldownSeconds = null,
    int? UserCooldownSeconds = null,
    bool? IsEnabled = null
);

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
/// REST endpoints for managing custom chat commands.
/// Used by the dashboard to create, edit, toggle, and delete commands.
/// </summary>
public static class CommandEndpoints
{
    /// <summary>Registers custom and system chat command API endpoints.</summary>
    public static void MapCommandEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/commands").WithTags("Commands");

        // GET /api/commands/system — returns built-in system commands with overrides
        group.MapGet("/system", async (
            IEnumerable<ISystemCommand> systemCommands,
            ISystemCommandOverrideRepository overrideRepo,
            CancellationToken ct) =>
        {
            IReadOnlyList<SystemCommandOverride> allOverrides = await overrideRepo.GetAllAsync(ct);
            Dictionary<string, SystemCommandOverride> overrideMap = allOverrides.ToDictionary(o => o.Trigger);

            var result = systemCommands.Select(cmd =>
            {
                overrideMap.TryGetValue(cmd.Trigger, out SystemCommandOverride? ovr);
                return new
                {
                    trigger = cmd.Trigger,
                    aliases = cmd.Aliases,
                    description = cmd.Description,
                    defaultResponseTemplate = cmd.DefaultResponseTemplate,
                    customResponseTemplate = ovr?.CustomResponseTemplate,
                    isEnabled = ovr?.IsEnabled ?? true,
                    isSystem = true
                };
            });
            return Results.Ok(result);
        });

        // PUT /api/commands/system/{trigger} — update system command override
        group.MapPut("/system/{trigger}", async (
            string trigger,
            UpdateSystemCommandRequest request,
            IEnumerable<ISystemCommand> systemCommands,
            ISystemCommandOverrideRepository overrideRepo,
            CancellationToken ct) =>
        {
            if (string.Equals(trigger, "!editcmd", System.StringComparison.OrdinalIgnoreCase)
                && request.CustomResponseTemplate is not null)
            {
                return TypedResults.Problem(detail: "The !editcmd response cannot be customized.", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
            }

            // Verify system command exists
            bool exists = systemCommands.Any(c =>
                string.Equals(c.Trigger, trigger, System.StringComparison.OrdinalIgnoreCase));
            if (!exists)
            {
                return TypedResults.Problem(detail: $"System command '{trigger}' not found.", title: "Not Found", statusCode: StatusCodes.Status404NotFound, type: "https://wrkzg.app/problems/not-found");
            }

            SystemCommandOverride entity = new()
            {
                Trigger = trigger.ToLowerInvariant(),
                CustomResponseTemplate = request.CustomResponseTemplate,
                IsEnabled = request.IsEnabled
            };

            await overrideRepo.SaveAsync(entity, ct);
            return Results.Ok(entity);
        });

        // POST /api/commands/system/{trigger}/reset — reset system command to default
        group.MapPost("/system/{trigger}/reset", async (
            string trigger,
            ISystemCommandOverrideRepository overrideRepo,
            CancellationToken ct) =>
        {
            await overrideRepo.DeleteAsync(trigger.ToLowerInvariant(), ct);
            return Results.Ok();
        });

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
            return command is null ? TypedResults.Problem(title: "Not Found", statusCode: StatusCodes.Status404NotFound, type: "https://wrkzg.app/problems/not-found") : Results.Ok(command);
        });

        // POST /api/commands — create new command
        group.MapPost("/", async (CreateCommandRequest request, ICommandRepository repo, CancellationToken ct) =>
        {
            // Validate
            if (string.IsNullOrWhiteSpace(request.Trigger) || !request.Trigger.StartsWith('!'))
            {
                return TypedResults.Problem(detail: "Trigger must start with '!' and be non-empty.", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
            }

            if (string.IsNullOrWhiteSpace(request.ResponseTemplate))
            {
                return TypedResults.Problem(detail: "ResponseTemplate is required.", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
            }

            if (request.ResponseTemplate.Length > 500)
            {
                return TypedResults.Problem(detail: "ResponseTemplate must be 500 characters or less.", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
            }

            // Check for duplicate trigger
            Command? existing = await repo.GetByTriggerOrAliasAsync(request.Trigger.ToLowerInvariant(), ct);
            if (existing is not null)
            {
                return TypedResults.Problem(detail: $"A command with trigger '{request.Trigger}' already exists.", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
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
                return TypedResults.Problem(title: "Not Found", statusCode: StatusCodes.Status404NotFound, type: "https://wrkzg.app/problems/not-found");
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
                    return TypedResults.Problem(detail: "ResponseTemplate must be 500 characters or less.", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
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
                return TypedResults.Problem(title: "Not Found", statusCode: StatusCodes.Status404NotFound, type: "https://wrkzg.app/problems/not-found");
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

/// <summary>Request body for updating a system command override.</summary>
public sealed record UpdateSystemCommandRequest(string? CustomResponseTemplate, bool IsEnabled);

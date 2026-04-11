using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wrkzg.Core.Effects;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Api.Endpoints;

/// <summary>
/// REST endpoints for the Effect System.
/// </summary>
public static class EffectEndpoints
{
    /// <summary>Registers effect list CRUD and trigger testing API endpoints.</summary>
    public static void MapEffectEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/effects").WithTags("Effects");

        // List all effect lists
        group.MapGet("/", async (IEffectListRepository repo, CancellationToken ct) =>
        {
            IReadOnlyList<EffectList> lists = await repo.GetAllAsync(ct);
            return Results.Ok(lists);
        });

        // Get single effect list
        group.MapGet("/{id:int}", async (int id, IEffectListRepository repo, CancellationToken ct) =>
        {
            EffectList? effectList = await repo.GetByIdAsync(id, ct);
            return effectList is not null ? Results.Ok(effectList) : TypedResults.Problem(title: "Not Found", statusCode: StatusCodes.Status404NotFound, type: "https://wrkzg.app/problems/not-found");
        });

        // Create effect list
        group.MapPost("/", async (CreateEffectListRequest request, IEffectListRepository repo, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.TriggerTypeId))
            {
                return TypedResults.Problem(detail: "Name and trigger type are required.", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
            }

            EffectList effectList = new()
            {
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                TriggerTypeId = request.TriggerTypeId.Trim(),
                TriggerConfig = request.TriggerConfig ?? "{}",
                ConditionsConfig = request.ConditionsConfig ?? "[]",
                EffectsConfig = request.EffectsConfig ?? "[]",
                Cooldown = request.Cooldown ?? 0,
                IsEnabled = true
            };

            effectList = await repo.CreateAsync(effectList, ct);
            return Results.Created($"/api/effects/{effectList.Id}", effectList);
        });

        // Update effect list
        group.MapPut("/{id:int}", async (int id, UpdateEffectListRequest request,
            IEffectListRepository repo, CancellationToken ct) =>
        {
            EffectList? effectList = await repo.GetByIdAsync(id, ct);
            if (effectList is null)
            {
                return TypedResults.Problem(title: "Not Found", statusCode: StatusCodes.Status404NotFound, type: "https://wrkzg.app/problems/not-found");
            }

            if (request.Name is not null) { effectList.Name = request.Name; }
            if (request.Description is not null) { effectList.Description = request.Description; }
            if (request.TriggerTypeId is not null) { effectList.TriggerTypeId = request.TriggerTypeId; }
            if (request.TriggerConfig is not null) { effectList.TriggerConfig = request.TriggerConfig; }
            if (request.ConditionsConfig is not null) { effectList.ConditionsConfig = request.ConditionsConfig; }
            if (request.EffectsConfig is not null) { effectList.EffectsConfig = request.EffectsConfig; }
            if (request.Cooldown.HasValue) { effectList.Cooldown = request.Cooldown.Value; }
            if (request.IsEnabled.HasValue) { effectList.IsEnabled = request.IsEnabled.Value; }

            await repo.UpdateAsync(effectList, ct);
            return Results.Ok(effectList);
        });

        // Delete effect list
        group.MapDelete("/{id:int}", async (int id, IEffectListRepository repo, CancellationToken ct) =>
        {
            await repo.DeleteAsync(id, ct);
            return Results.NoContent();
        });

        // Get available types (for the editor dropdowns)
        group.MapGet("/types", (EffectEngine engine) =>
        {
            return Results.Ok(new
            {
                triggers = engine.GetTriggerTypes().Select(t => new
                {
                    id = t.Id,
                    displayName = t.DisplayName,
                    parameterKeys = t.ParameterKeys
                }),
                conditions = engine.GetConditionTypes().Select(c => new
                {
                    id = c.Id,
                    displayName = c.DisplayName,
                    parameterKeys = c.ParameterKeys
                }),
                effects = engine.GetEffectTypes().Select(e => new
                {
                    id = e.Id,
                    displayName = e.DisplayName,
                    parameterKeys = e.ParameterKeys
                })
            });
        });

        // Test trigger — directly executes THIS effect chain only (bypasses trigger matching)
        group.MapPost("/{id:int}/test", async (int id, IEffectListRepository repo,
            EffectEngine engine, CancellationToken ct) =>
        {
            EffectList? effectList = await repo.GetByIdAsync(id, ct);
            if (effectList is null)
            {
                return TypedResults.Problem(title: "Not Found", statusCode: StatusCodes.Status404NotFound, type: "https://wrkzg.app/problems/not-found");
            }

            EffectTriggerContext testContext = new()
            {
                EventType = effectList.TriggerTypeId,
                Username = "TestUser",
                UserId = "0",
                MessageContent = "test",
                Data = new Dictionary<string, string>
                {
                    ["test"] = "true",
                    ["user"] = "TestUser",
                    ["args"] = "test arguments",
                    ["target"] = "@TestTarget",
                    ["viewers"] = "42",
                    ["tier"] = "1",
                    ["months"] = "6",
                    ["count"] = "5",
                    ["message"] = "Test message",
                    ["reward"] = "Test Reward",
                    ["input"] = "test input",
                    ["broadcaster"] = "TestBroadcaster",
                    ["amount"] = "100",
                    ["points"] = "500",
                    ["hours"] = "24",
                }
            };

            await engine.ExecuteSingleAsync(id, testContext, ct);
            return Results.Ok(new { tested = true, name = effectList.Name });
        });
    }
}

/// <summary>Request payload for creating a new effect list.</summary>
public record CreateEffectListRequest(
    string Name,
    string? Description,
    string TriggerTypeId,
    string? TriggerConfig,
    string? ConditionsConfig,
    string? EffectsConfig,
    int? Cooldown);

/// <summary>Request payload for updating an existing effect list.</summary>
public record UpdateEffectListRequest(
    string? Name,
    string? Description,
    string? TriggerTypeId,
    string? TriggerConfig,
    string? ConditionsConfig,
    string? EffectsConfig,
    int? Cooldown,
    bool? IsEnabled);

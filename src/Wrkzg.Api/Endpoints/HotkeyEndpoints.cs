using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Core.Services;
using Wrkzg.Infrastructure.Hotkeys;

namespace Wrkzg.Api.Endpoints;

/// <summary>
/// REST endpoints for hotkey binding management.
/// </summary>
public static class HotkeyEndpoints
{
    /// <summary>Registers hotkey binding CRUD and trigger API endpoints.</summary>
    public static void MapHotkeyEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/hotkeys").WithTags("Hotkeys");

        group.MapGet("/", async (IHotkeyBindingRepository repo, CancellationToken ct) =>
        {
            IReadOnlyList<HotkeyBinding> bindings = await repo.GetAllAsync(ct);
            return Results.Ok(bindings);
        });

        group.MapGet("/permission", (IHotkeyListener listener) =>
        {
            return Results.Ok(new
            {
                globalHotkeySupported = listener.IsGlobalHotkeySupported,
                hasPermission = listener.HasPermission,
                platform = OperatingSystem.IsMacOS() ? "macos" : OperatingSystem.IsWindows() ? "windows" : "other"
            });
        });

        group.MapPost("/permission/request", (IHotkeyListener listener) =>
        {
            listener.RequestPermission();
            return Results.Ok(new
            {
                hasPermission = listener.HasPermission
            });
        });

        group.MapPost("/", async (CreateHotkeyRequest request, IHotkeyBindingRepository repo,
            HotkeyListenerService listenerService, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.KeyCombination) || string.IsNullOrWhiteSpace(request.ActionType))
            {
                return TypedResults.Problem(detail: "Key combination and action type are required.", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
            }

            HotkeyBinding binding = new()
            {
                KeyCombination = request.KeyCombination.Trim(),
                ActionType = request.ActionType.Trim(),
                ActionPayload = request.ActionPayload?.Trim() ?? "",
                Description = request.Description?.Trim(),
                IsEnabled = true
            };

            binding = await repo.CreateAsync(binding, ct);
            await listenerService.RefreshBindingsAsync(ct);
            return Results.Created($"/api/hotkeys/{binding.Id}", binding);
        });

        group.MapPut("/{id:int}", async (int id, UpdateHotkeyRequest request,
            IHotkeyBindingRepository repo, HotkeyListenerService listenerService, CancellationToken ct) =>
        {
            HotkeyBinding? binding = await repo.GetByIdAsync(id, ct);
            if (binding is null)
            {
                return TypedResults.Problem(title: "Not Found", statusCode: StatusCodes.Status404NotFound, type: "https://wrkzg.app/problems/not-found");
            }

            if (request.KeyCombination is not null) { binding.KeyCombination = request.KeyCombination; }
            if (request.ActionType is not null) { binding.ActionType = request.ActionType; }
            if (request.ActionPayload is not null) { binding.ActionPayload = request.ActionPayload; }
            if (request.Description is not null) { binding.Description = request.Description; }
            if (request.IsEnabled.HasValue) { binding.IsEnabled = request.IsEnabled.Value; }

            await repo.UpdateAsync(binding, ct);
            await listenerService.RefreshBindingsAsync(ct);
            return Results.Ok(binding);
        });

        group.MapDelete("/{id:int}", async (int id, IHotkeyBindingRepository repo,
            HotkeyListenerService listenerService, CancellationToken ct) =>
        {
            await repo.DeleteAsync(id, ct);
            await listenerService.RefreshBindingsAsync(ct);
            return Results.NoContent();
        });

        // Direct trigger — loads binding from DB and executes the action immediately
        group.MapPost("/{id:int}/trigger", async (int id, IHotkeyBindingRepository repo,
            HotkeyActionExecutor executor, CancellationToken ct) =>
        {
            HotkeyBinding? binding = await repo.GetByIdAsync(id, ct);
            if (binding is null)
            {
                return TypedResults.Problem(detail: "Hotkey binding not found.", title: "Not Found", statusCode: StatusCodes.Status404NotFound, type: "https://wrkzg.app/problems/not-found");
            }

            await executor.ExecuteAsync(binding, ct);
            return Results.Ok(new { triggered = true, action = binding.ActionType, payload = binding.ActionPayload });
        });
    }
}

/// <summary>Request payload for creating a new hotkey binding.</summary>
public record CreateHotkeyRequest(
    string KeyCombination,
    string ActionType,
    string? ActionPayload,
    string? Description);

/// <summary>Request payload for updating an existing hotkey binding.</summary>
public record UpdateHotkeyRequest(
    string? KeyCombination,
    string? ActionType,
    string? ActionPayload,
    string? Description,
    bool? IsEnabled);

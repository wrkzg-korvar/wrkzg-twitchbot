using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wrkzg.Core.Interfaces;

namespace Wrkzg.Api.Endpoints;

/// <summary>
/// Window control endpoints called by the custom title bar.
/// </summary>
public static class WindowEndpoints
{
    /// <summary>Registers window control (minimize, maximize, close, drag, resize) API endpoints.</summary>
    public static void MapWindowEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/window").WithTags("Window");

        group.MapPost("/minimize", (IWindowController controller) =>
        {
            controller.Minimize();
            return Results.Ok();
        });

        group.MapPost("/maximize", (IWindowController controller) =>
        {
            controller.ToggleMaximize();
            return Results.Ok();
        });

        group.MapPost("/close", (IWindowController controller) =>
        {
            controller.Close();
            return Results.Ok();
        });

        group.MapPost("/drag-start", (DragRequest request, IWindowController controller) =>
        {
            controller.DragStart(request.ScreenX, request.ScreenY);
            return Results.Ok();
        });

        group.MapPost("/drag-move", (DragRequest request, IWindowController controller) =>
        {
            controller.DragMove(request.ScreenX, request.ScreenY);
            return Results.Ok();
        });

        group.MapPost("/start-resize", (ResizeRequest request, IWindowController controller) =>
        {
            if (string.IsNullOrEmpty(request.Direction))
            {
                return Results.BadRequest();
            }

            controller.StartResize(request.Direction);
            return Results.Ok();
        });
    }
}

/// <summary>Request body for drag operations with screen coordinates.</summary>
public sealed record DragRequest(int ScreenX, int ScreenY);

/// <summary>Request body for resize operations with direction.</summary>
public sealed record ResizeRequest(string Direction);

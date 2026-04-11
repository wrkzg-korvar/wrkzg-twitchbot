using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Api.Endpoints;

/// <summary>
/// REST endpoints for custom user-created overlays (Developer Mode).
/// </summary>
public static class CustomOverlayEndpoints
{
    /// <summary>Registers custom overlay CRUD and rendering API endpoints.</summary>
    public static void MapCustomOverlayEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/custom-overlays").WithTags("Custom Overlays");

        // List all
        group.MapGet("/", async (ICustomOverlayRepository repo, CancellationToken ct) =>
        {
            IReadOnlyList<CustomOverlay> overlays = await repo.GetAllAsync(ct);
            return Results.Ok(overlays);
        });

        // Get by ID
        group.MapGet("/{id:int}", async (int id, ICustomOverlayRepository repo, CancellationToken ct) =>
        {
            CustomOverlay? overlay = await repo.GetByIdAsync(id, ct);
            return overlay is not null ? Results.Ok(overlay) : TypedResults.Problem(title: "Not Found", statusCode: StatusCodes.Status404NotFound, type: "https://wrkzg.app/problems/not-found");
        });

        // Create
        group.MapPost("/", async (CreateCustomOverlayRequest request,
            ICustomOverlayRepository repo, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return TypedResults.Problem(detail: "Name is required.", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
            }

            CustomOverlay overlay = new()
            {
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                Html = request.Html ?? "",
                Css = request.Css ?? "",
                JavaScript = request.JavaScript ?? "",
                FieldDefinitions = request.FieldDefinitions ?? "{}",
                FieldValues = request.FieldValues ?? "{}",
                Width = request.Width ?? 800,
                Height = request.Height ?? 600,
                IsEnabled = true
            };

            overlay = await repo.CreateAsync(overlay, ct);
            return Results.Created($"/api/custom-overlays/{overlay.Id}", overlay);
        });

        // Update
        group.MapPut("/{id:int}", async (int id, UpdateCustomOverlayRequest request,
            ICustomOverlayRepository repo, CancellationToken ct) =>
        {
            CustomOverlay? overlay = await repo.GetByIdAsync(id, ct);
            if (overlay is null)
            {
                return TypedResults.Problem(title: "Not Found", statusCode: StatusCodes.Status404NotFound, type: "https://wrkzg.app/problems/not-found");
            }

            if (request.Name is not null) { overlay.Name = request.Name.Trim(); }
            if (request.Description is not null) { overlay.Description = request.Description.Trim(); }
            if (request.Html is not null) { overlay.Html = request.Html; }
            if (request.Css is not null) { overlay.Css = request.Css; }
            if (request.JavaScript is not null) { overlay.JavaScript = request.JavaScript; }
            if (request.FieldDefinitions is not null) { overlay.FieldDefinitions = request.FieldDefinitions; }
            if (request.FieldValues is not null) { overlay.FieldValues = request.FieldValues; }
            if (request.Width.HasValue) { overlay.Width = request.Width.Value; }
            if (request.Height.HasValue) { overlay.Height = request.Height.Value; }
            if (request.IsEnabled.HasValue) { overlay.IsEnabled = request.IsEnabled.Value; }

            await repo.UpdateAsync(overlay, ct);
            return Results.Ok(overlay);
        });

        // Update field values only
        group.MapPut("/{id:int}/fields", async (int id, FieldValuesRequest request,
            ICustomOverlayRepository repo, CancellationToken ct) =>
        {
            CustomOverlay? overlay = await repo.GetByIdAsync(id, ct);
            if (overlay is null)
            {
                return TypedResults.Problem(title: "Not Found", statusCode: StatusCodes.Status404NotFound, type: "https://wrkzg.app/problems/not-found");
            }

            overlay.FieldValues = request.FieldValues ?? "{}";
            await repo.UpdateAsync(overlay, ct);
            return Results.Ok(overlay);
        });

        // Delete
        group.MapDelete("/{id:int}", async (int id, ICustomOverlayRepository repo, CancellationToken ct) =>
        {
            await repo.DeleteAsync(id, ct);
            return Results.NoContent();
        });

        // Render as full HTML page (for OBS Browser Source)
        app.MapGet("/overlay/custom/{id:int}", async (
            int id,
            ICustomOverlayRepository repo,
            HttpContext context,
            CancellationToken ct) =>
        {
            CustomOverlay? overlay = await repo.GetByIdAsync(id, ct);
            if (overlay is null || !overlay.IsEnabled)
            {
                return TypedResults.Problem(detail: "Overlay not found or disabled.", title: "Not Found", statusCode: StatusCodes.Status404NotFound, type: "https://wrkzg.app/problems/not-found");
            }

            string host = context.Request.Host.ToString();
            string signalRUrl = $"http://{host}/hubs/chat";

            // When ?preview=true, show checkerboard to indicate transparency.
            // In OBS, Browser Sources render transparent backgrounds correctly.
            bool isPreview = context.Request.Query.ContainsKey("preview");
            string bodyBg = isPreview
                ? "background: repeating-conic-gradient(#222 0% 25%, #2a2a2a 0% 50%) 50% / 20px 20px;"
                : "background: transparent;";

            string html = "<!DOCTYPE html><html><head><meta charset=\"utf-8\"><style>"
                + "* { margin: 0; padding: 0; box-sizing: border-box; } "
                + "body { " + bodyBg + " overflow: hidden; } "
                + overlay.Css
                + "</style></head><body>"
                + overlay.Html
                + "<script src=\"https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.0/signalr.min.js\"></script>"
                + "<script>"
                + "const Wrkzg = {"
                + "  signalR: null,"
                + "  config: " + overlay.FieldValues + ","
                + "  fields: " + overlay.FieldDefinitions + ","
                + "  connect: async function() {"
                + "    this.signalR = new signalR.HubConnectionBuilder()"
                + "      .withUrl('" + signalRUrl + "?source=overlay')"
                + "      .withAutomaticReconnect().build();"
                + "    await this.signalR.start();"
                + "    console.log('[Wrkzg] Connected to SignalR');"
                + "  },"
                + "  on: function(n, cb) { if (this.signalR) { this.signalR.on(n, cb); } },"
                + "  getField: function(k) { return this.config[k] ?? (this.fields[k] ? this.fields[k].value : null); }"
                + "};"
                + "Wrkzg.connect().then(function() {"
                + "  try { " + overlay.JavaScript + " } catch(e) { console.error('[Wrkzg Custom]', e); }"
                + "});"
                + "</script></body></html>";

            return Results.Content(html, "text/html");
        });
    }
}

/// <summary>Request payload for creating a new custom overlay.</summary>
public record CreateCustomOverlayRequest(
    string Name,
    string? Description,
    string? Html,
    string? Css,
    string? JavaScript,
    string? FieldDefinitions,
    string? FieldValues,
    int? Width,
    int? Height);

/// <summary>Request payload for updating an existing custom overlay.</summary>
public record UpdateCustomOverlayRequest(
    string? Name,
    string? Description,
    string? Html,
    string? Css,
    string? JavaScript,
    string? FieldDefinitions,
    string? FieldValues,
    int? Width,
    int? Height,
    bool? IsEnabled);

/// <summary>Request payload for updating custom overlay field values only.</summary>
public record FieldValuesRequest(string? FieldValues);

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wrkzg.Core.Interfaces;

namespace Wrkzg.Api.Endpoints;

/// <summary>
/// REST endpoints for third-party integration settings.
/// </summary>
public static class IntegrationEndpoints
{
    /// <summary>Registers third-party integration settings API endpoints (Discord webhooks).</summary>
    public static void MapIntegrationEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/integrations").WithTags("Integrations");

        // Discord
        group.MapGet("/discord", async (ISettingsRepository settings, CancellationToken ct) =>
        {
            string? webhookUrl = await settings.GetAsync("Integration.Discord.WebhookUrl", ct);
            return Results.Ok(new
            {
                configured = !string.IsNullOrWhiteSpace(webhookUrl),
                webhookUrlSet = !string.IsNullOrWhiteSpace(webhookUrl)
            });
        });

        group.MapPut("/discord", async (UpdateDiscordRequest request, ISettingsRepository settings, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.WebhookUrl))
            {
                return TypedResults.Problem(detail: "Webhook URL is required.", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
            }

            // Basic validation: Discord webhook URLs start with https://discord.com/api/webhooks/
            if (!request.WebhookUrl.StartsWith("https://discord.com/api/webhooks/") &&
                !request.WebhookUrl.StartsWith("https://discordapp.com/api/webhooks/"))
            {
                return TypedResults.Problem(detail: "Invalid Discord webhook URL. Must start with https://discord.com/api/webhooks/", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
            }

            await settings.SetAsync("Integration.Discord.WebhookUrl", request.WebhookUrl.Trim(), ct);
            return Results.Ok(new { configured = true });
        });

        group.MapDelete("/discord", async (ISettingsRepository settings, CancellationToken ct) =>
        {
            await settings.DeleteAsync("Integration.Discord.WebhookUrl", ct);
            return Results.Ok(new { configured = false });
        });

        group.MapPost("/discord/test", async (ISettingsRepository settings, CancellationToken ct) =>
        {
            string? webhookUrl = await settings.GetAsync("Integration.Discord.WebhookUrl", ct);
            if (string.IsNullOrWhiteSpace(webhookUrl))
            {
                return TypedResults.Problem(detail: "Discord webhook not configured.", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
            }

            try
            {
                using System.Net.Http.HttpClient http = new() { Timeout = System.TimeSpan.FromSeconds(10) };
                string json = System.Text.Json.JsonSerializer.Serialize(new
                {
                    content = "Test message from Wrkzg Bot! If you see this, your Discord integration is working."
                });

                System.Net.Http.HttpResponseMessage response = await http.PostAsync(
                    webhookUrl,
                    new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json"),
                    ct);

                if (response.IsSuccessStatusCode)
                {
                    return Results.Ok(new { success = true, message = "Test message sent to Discord!" });
                }

                return TypedResults.Problem(detail: $"Discord returned {response.StatusCode}. Check your webhook URL.", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
            }
            catch (System.Exception ex)
            {
                return TypedResults.Problem(detail: $"Failed to reach Discord: {ex.Message}", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
            }
        });
        // ─── OBS WebSocket ───────────────────────────────────────

        // GET /api/integrations/obs
        group.MapGet("/obs", (IObsWebSocketService obs) =>
        {
            return Results.Ok(obs.GetStatus());
        });

        // PUT /api/integrations/obs — Save connection settings
        group.MapPut("/obs", async (
            UpdateObsRequest request,
            ISettingsRepository settings,
            ISecureStorage secureStorage,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Host))
            {
                return TypedResults.Problem(
                    detail: "Host is required.",
                    title: "Validation Error",
                    statusCode: StatusCodes.Status400BadRequest,
                    type: "https://wrkzg.app/problems/validation-error");
            }

            await settings.SetAsync("Integration.Obs.Host", request.Host.Trim(), ct);
            await settings.SetAsync("Integration.Obs.Port", request.Port.ToString(), ct);

            if (request.Password is not null)
            {
                if (string.IsNullOrWhiteSpace(request.Password))
                {
                    await secureStorage.DeleteSecretAsync("obs-websocket-password", ct);
                }
                else
                {
                    await secureStorage.SaveSecretAsync("obs-websocket-password", request.Password, ct);
                }
            }

            return Results.Ok(new { configured = true });
        });

        // DELETE /api/integrations/obs — Remove settings
        group.MapDelete("/obs", async (
            ISettingsRepository settings,
            ISecureStorage secureStorage,
            IObsWebSocketService obs,
            CancellationToken ct) =>
        {
            await obs.DisconnectAsync(ct);
            await settings.DeleteAsync("Integration.Obs.Host", ct);
            await settings.DeleteAsync("Integration.Obs.Port", ct);
            await secureStorage.DeleteSecretAsync("obs-websocket-password", ct);
            return Results.Ok(new { removed = true });
        });

        // POST /api/integrations/obs/connect
        group.MapPost("/obs/connect", async (IObsWebSocketService obs, CancellationToken ct) =>
        {
            bool connected = await obs.ConnectAsync(ct);
            return Results.Ok(new { connected });
        });

        // POST /api/integrations/obs/disconnect
        group.MapPost("/obs/disconnect", async (IObsWebSocketService obs, CancellationToken ct) =>
        {
            await obs.DisconnectAsync(ct);
            return Results.Ok(new { disconnected = true });
        });

        // GET /api/integrations/obs/scenes
        group.MapGet("/obs/scenes", async (IObsWebSocketService obs, CancellationToken ct) =>
        {
            if (!obs.IsConnected)
            {
                return Results.BadRequest(new { error = "Not connected to OBS" });
            }

            IReadOnlyList<string> scenes = await obs.GetScenesAsync(ct);
            return Results.Ok(scenes);
        });

        // POST /api/integrations/obs/scenes/switch
        group.MapPost("/obs/scenes/switch", async (
            SwitchSceneRequest request,
            IObsWebSocketService obs,
            CancellationToken ct) =>
        {
            if (!obs.IsConnected)
            {
                return Results.BadRequest(new { error = "Not connected to OBS" });
            }

            bool success = await obs.SwitchSceneAsync(request.SceneName, ct);
            return success
                ? Results.Ok(new { switched = true })
                : Results.BadRequest(new { error = "Scene not found" });
        });

        // GET /api/integrations/obs/sources?scene=SceneName
        group.MapGet("/obs/sources", async (
            string? scene,
            IObsWebSocketService obs,
            CancellationToken ct) =>
        {
            if (!obs.IsConnected)
            {
                return Results.BadRequest(new { error = "Not connected to OBS" });
            }

            IReadOnlyList<ObsSourceInfo> sources = await obs.GetSourcesAsync(scene, ct);
            return Results.Ok(sources);
        });
    }
}

/// <summary>Request payload for updating the Discord webhook URL.</summary>
public record UpdateDiscordRequest(string WebhookUrl);

/// <summary>Request payload for updating OBS WebSocket connection settings.</summary>
public sealed record UpdateObsRequest(string Host, int Port = 4455, string? Password = null);

/// <summary>Request payload for switching OBS scenes.</summary>
public sealed record SwitchSceneRequest(string SceneName);

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
                return Results.BadRequest(new { error = "Webhook URL is required." });
            }

            // Basic validation: Discord webhook URLs start with https://discord.com/api/webhooks/
            if (!request.WebhookUrl.StartsWith("https://discord.com/api/webhooks/") &&
                !request.WebhookUrl.StartsWith("https://discordapp.com/api/webhooks/"))
            {
                return Results.BadRequest(new { error = "Invalid Discord webhook URL. Must start with https://discord.com/api/webhooks/" });
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
                return Results.BadRequest(new { error = "Discord webhook not configured." });
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

                return Results.BadRequest(new { error = $"Discord returned {response.StatusCode}. Check your webhook URL." });
            }
            catch (System.Exception ex)
            {
                return Results.BadRequest(new { error = $"Failed to reach Discord: {ex.Message}" });
            }
        });
    }
}

public record UpdateDiscordRequest(string WebhookUrl);

using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Interfaces;

namespace Wrkzg.Core.Effects.EffectTypes;

/// <summary>
/// Sends a plain text message to a Discord channel via Webhook.
/// No Discord bot token or OAuth needed — just a Webhook URL.
/// </summary>
public class DiscordSendMessageEffect : IEffectType, IDisposable
{
    private readonly HttpClient _http;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DiscordSendMessageEffect> _logger;

    public string Id => "discord.send_message";
    public string DisplayName => "Send Discord Message";
    public string[] ParameterKeys => new[] { "message" };

    public DiscordSendMessageEffect(
        IServiceScopeFactory scopeFactory,
        ILogger<DiscordSendMessageEffect> logger)
    {
        _scopeFactory = scopeFactory;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        _logger = logger;
    }

    public async Task ExecuteAsync(EffectExecutionContext context, CancellationToken ct = default)
    {
        string? webhookUrl = await GetWebhookUrlAsync(ct);
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            _logger.LogWarning("Discord webhook URL not configured. Set it in Settings > Integrations.");
            return;
        }

        string template = context.GetParameter("message");
        string message = context.ResolveVariables(template);

        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        try
        {
            string json = JsonSerializer.Serialize(new { content = message });
            HttpResponseMessage response = await _http.PostAsync(
                webhookUrl,
                new StringContent(json, Encoding.UTF8, "application/json"),
                ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Discord webhook failed: {Status}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send Discord message");
        }
    }

    private async Task<string?> GetWebhookUrlAsync(CancellationToken ct)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        ISettingsRepository settings = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();
        return await settings.GetAsync("Integration.Discord.WebhookUrl", ct);
    }

    public void Dispose() { _http.Dispose(); GC.SuppressFinalize(this); }
}

/// <summary>
/// Sends a rich embed to a Discord channel via Webhook.
/// Supports title, description, color, and footer.
/// </summary>
public class DiscordSendEmbedEffect : IEffectType, IDisposable
{
    private readonly HttpClient _http;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DiscordSendEmbedEffect> _logger;

    public string Id => "discord.send_embed";
    public string DisplayName => "Send Discord Embed";
    public string[] ParameterKeys => new[] { "title", "description", "color" };

    public DiscordSendEmbedEffect(
        IServiceScopeFactory scopeFactory,
        ILogger<DiscordSendEmbedEffect> logger)
    {
        _scopeFactory = scopeFactory;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        _logger = logger;
    }

    public async Task ExecuteAsync(EffectExecutionContext context, CancellationToken ct = default)
    {
        string? webhookUrl = await GetWebhookUrlAsync(ct);
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            _logger.LogWarning("Discord webhook URL not configured.");
            return;
        }

        string title = context.ResolveVariables(context.GetParameter("title"));
        string description = context.ResolveVariables(context.GetParameter("description"));
        string colorHex = context.GetParameter("color");

        int color = 0x5865F2; // Discord blurple default
        if (!string.IsNullOrWhiteSpace(colorHex))
        {
            colorHex = colorHex.TrimStart('#');
            if (int.TryParse(colorHex, System.Globalization.NumberStyles.HexNumber, null, out int parsed))
            {
                color = parsed;
            }
        }

        try
        {
            object payload = new
            {
                embeds = new[]
                {
                    new
                    {
                        title,
                        description,
                        color,
                        footer = new { text = "Wrkzg Bot" }
                    }
                }
            };

            string json = JsonSerializer.Serialize(payload);
            HttpResponseMessage response = await _http.PostAsync(
                webhookUrl,
                new StringContent(json, Encoding.UTF8, "application/json"),
                ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Discord embed webhook failed: {Status}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send Discord embed");
        }
    }

    private async Task<string?> GetWebhookUrlAsync(CancellationToken ct)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        ISettingsRepository settings = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();
        return await settings.GetAsync("Integration.Discord.WebhookUrl", ct);
    }

    public void Dispose() { _http.Dispose(); GC.SuppressFinalize(this); }
}

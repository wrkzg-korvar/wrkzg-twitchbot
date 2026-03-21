using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Interfaces;

namespace Wrkzg.Infrastructure.Twitch;

/// <summary>
/// Twitch Helix REST API client.
/// Uses a typed HttpClient with TwitchAuthHandler in the pipeline
/// for automatic Bearer token injection and refresh.
/// </summary>
public class TwitchHelixClient : ITwitchHelixClient
{
    private readonly HttpClient _http;
    private readonly ILogger<TwitchHelixClient> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public TwitchHelixClient(HttpClient http, ILogger<TwitchHelixClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<StreamInfo?> GetStreamAsync(string channelLogin, CancellationToken ct = default)
    {
        try
        {
            HelixResponse<StreamInfo>? response = await _http.GetFromJsonAsync<HelixResponse<StreamInfo>>(
                $"streams?user_login={Uri.EscapeDataString(channelLogin)}", _jsonOptions, ct);

            return response?.Data?.FirstOrDefault();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to get stream info for {Channel}", channelLogin);
            return null;
        }
    }

    public async Task<HelixUserInfo?> GetUserAsync(string login, CancellationToken ct = default)
    {
        try
        {
            HelixResponse<HelixUserInfo>? response = await _http.GetFromJsonAsync<HelixResponse<HelixUserInfo>>(
                $"users?login={Uri.EscapeDataString(login)}", _jsonOptions, ct);

            return response?.Data?.FirstOrDefault();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to get user info for {Login}", login);
            return null;
        }
    }

    public async Task<bool> SendChatMessageAsync(string broadcasterId, string senderId, string message, CancellationToken ct = default)
    {
        try
        {
            HttpResponseMessage response = await _http.PostAsJsonAsync(
                "chat/messages",
                new { broadcaster_id = broadcasterId, sender_id = senderId, message },
                ct);

            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Failed to send chat message via Helix: {Status} {Body}",
                    response.StatusCode, body);
                return false;
            }

            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to send chat message via Helix API");
            return false;
        }
    }

    /// <summary>
    /// Generic Helix API response wrapper. Twitch wraps all responses in { "data": [...] }.
    /// </summary>
    private sealed class HelixResponse<T>
    {
        [JsonPropertyName("data")]
        public T[]? Data { get; init; }
    }
}

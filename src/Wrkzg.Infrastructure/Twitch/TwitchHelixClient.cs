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

    public async Task<bool> TimeoutUserAsync(string userId, int durationSeconds, string reason, CancellationToken ct = default)
    {
        try
        {
            // This endpoint requires broadcaster_id and moderator_id (the bot)
            // For now, log a warning — full implementation requires token resolution
            _logger.LogWarning("TimeoutUserAsync called for user {UserId} ({Duration}s, {Reason}) — requires moderator token setup",
                userId, durationSeconds, reason);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to timeout user {UserId}", userId);
            return false;
        }
    }

    public async Task<TwitchPollResponse?> CreateTwitchPollAsync(
        string broadcasterId,
        string question,
        string[] options,
        int durationSeconds,
        CancellationToken ct = default)
    {
        try
        {
            object body = new
            {
                broadcaster_id = broadcasterId,
                title = question,
                choices = options.Select(o => new { title = o }).ToArray(),
                duration = durationSeconds
            };

            HttpResponseMessage response = await _http.PostAsJsonAsync("polls", body, ct);

            if (!response.IsSuccessStatusCode)
            {
                string err = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Failed to create Twitch poll: {Status} {Body}", response.StatusCode, err);
                return null;
            }

            HelixResponse<TwitchPollResponse>? data = await response.Content.ReadFromJsonAsync<HelixResponse<TwitchPollResponse>>(_jsonOptions, ct);
            return data?.Data?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create Twitch poll");
            return null;
        }
    }

    public async Task<bool> EndTwitchPollAsync(
        string broadcasterId,
        string pollId,
        string status,
        CancellationToken ct = default)
    {
        try
        {
            object body = new
            {
                broadcaster_id = broadcasterId,
                id = pollId,
                status
            };

            HttpResponseMessage response = await _http.PatchAsJsonAsync("polls", body, ct);

            if (!response.IsSuccessStatusCode)
            {
                string err = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Failed to end Twitch poll: {Status} {Body}", response.StatusCode, err);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to end Twitch poll");
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

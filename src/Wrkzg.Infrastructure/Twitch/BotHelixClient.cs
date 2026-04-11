using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Twitch;

/// <summary>
/// Twitch Helix REST API client authenticated with the Bot token.
/// Used for moderation actions where the bot acts as a moderator in the channel.
/// Resolves the bot's own user ID internally from the Bot OAuth token.
/// </summary>
public class BotHelixClient : IBotHelixClient
{
    private readonly HttpClient _http;
    private readonly ISecureStorage _storage;
    private readonly ITwitchOAuthService _oauth;
    private readonly ILogger<BotHelixClient> _logger;

    private string? _cachedBotUserId;
    private readonly SemaphoreSlim _botUserIdLock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="BotHelixClient"/> class.
    /// </summary>
    /// <param name="http">The typed HTTP client with TwitchAuthHandler configured for the Bot token.</param>
    /// <param name="storage">Secure storage for loading Bot tokens.</param>
    /// <param name="oauth">OAuth service for token validation and refresh.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public BotHelixClient(
        HttpClient http,
        ISecureStorage storage,
        ITwitchOAuthService oauth,
        ILogger<BotHelixClient> logger)
    {
        _http = http;
        _storage = storage;
        _oauth = oauth;
        _logger = logger;
    }

    /// <summary>Sends a chat announcement via the Helix API using the Bot token.</summary>
    public async Task<bool> SendAnnouncementAsync(string broadcasterId, string message, string color = "primary", CancellationToken ct = default)
    {
        string? botUserId = await ResolveBotUserIdAsync(ct);
        if (botUserId is null)
        {
            _logger.LogWarning("Cannot send announcement — failed to resolve bot user ID");
            return false;
        }

        try
        {
            HttpResponseMessage response = await _http.PostAsJsonAsync(
                $"chat/announcements?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&moderator_id={Uri.EscapeDataString(botUserId)}",
                new { message, color },
                ct);

            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Failed to send announcement via Helix: {Status} {Body}",
                    response.StatusCode, body);
                return false;
            }

            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to send announcement via Helix API");
            return false;
        }
    }

    /// <summary>Times out a user in the channel via the Helix API using the Bot token.</summary>
    public async Task<bool> TimeoutUserAsync(string broadcasterId, string userId, int durationSeconds, string reason, CancellationToken ct = default)
    {
        string? botUserId = await ResolveBotUserIdAsync(ct);
        if (botUserId is null)
        {
            _logger.LogWarning("Cannot timeout user — failed to resolve bot user ID");
            return false;
        }

        try
        {
            HttpResponseMessage response = await _http.PostAsJsonAsync(
                $"moderation/bans?broadcaster_id={Uri.EscapeDataString(broadcasterId)}&moderator_id={Uri.EscapeDataString(botUserId)}",
                new
                {
                    data = new
                    {
                        user_id = userId,
                        duration = durationSeconds,
                        reason
                    }
                },
                ct);

            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Failed to timeout user {UserId} via Helix: {Status} {Body}",
                    userId, response.StatusCode, body);
                return false;
            }

            _logger.LogInformation("Timed out user {UserId} for {Duration}s: {Reason}",
                userId, durationSeconds, reason);
            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to timeout user {UserId} via Helix API", userId);
            return false;
        }
    }

    /// <summary>
    /// Resolves the bot's Twitch user ID from the Bot OAuth token.
    /// Thread-safe with caching — only validates the token once until invalidated.
    /// </summary>
    private async Task<string?> ResolveBotUserIdAsync(CancellationToken ct)
    {
        if (_cachedBotUserId is not null)
        {
            return _cachedBotUserId;
        }

        await _botUserIdLock.WaitAsync(ct);
        try
        {
            // Double-check after acquiring lock
            if (_cachedBotUserId is not null)
            {
                return _cachedBotUserId;
            }

            TwitchTokens? botToken = await _storage.LoadTokensAsync(TokenType.Bot, ct);
            if (botToken is null)
            {
                _logger.LogWarning("No Bot token available — cannot resolve bot user ID");
                return null;
            }

            TwitchTokenValidation? validation = await _oauth.ValidateTokenAsync(botToken.AccessToken, ct);
            if (validation is null)
            {
                _logger.LogInformation("Bot token expired — refreshing for user ID resolution");
                TwitchTokens refreshed = await _oauth.RefreshTokenAsync(botToken.RefreshToken, ct);
                await _storage.SaveTokensAsync(TokenType.Bot, refreshed, ct);
                validation = await _oauth.ValidateTokenAsync(refreshed.AccessToken, ct);
            }

            if (validation is null)
            {
                _logger.LogWarning("Failed to validate Bot token — cannot resolve bot user ID");
                return null;
            }

            _cachedBotUserId = validation.UserId;
            _logger.LogDebug("Resolved bot user ID: {BotUserId}", _cachedBotUserId);
            return _cachedBotUserId;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve bot user ID");
            return null;
        }
        finally
        {
            _botUserIdLock.Release();
        }
    }

    /// <summary>Gets global Twitch emotes available to all users.</summary>
    public async Task<IReadOnlyList<TwitchEmote>> GetGlobalEmotesAsync(CancellationToken ct = default)
    {
        try
        {
            JsonSerializerOptions opts = new()
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                PropertyNameCaseInsensitive = true,
            };

            HelixDataResponse<TwitchEmote>? response = await _http.GetFromJsonAsync<HelixDataResponse<TwitchEmote>>(
                "chat/emotes/global", opts, ct);

            return response?.Data ?? Array.Empty<TwitchEmote>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to get global emotes via Bot client");
            return Array.Empty<TwitchEmote>();
        }
    }

    /// <summary>Gets all emotes available to the bot user, including subscribed channel emotes.</summary>
    public async Task<IReadOnlyList<TwitchEmote>> GetUserEmotesAsync(string userId, CancellationToken ct = default)
    {
        try
        {
            List<TwitchEmote> allEmotes = new();
            string? cursor = null;

            do
            {
                string url = $"chat/emotes/user?user_id={Uri.EscapeDataString(userId)}";
                if (cursor is not null)
                {
                    url += $"&after={Uri.EscapeDataString(cursor)}";
                }

                HelixPaginatedResponse<TwitchEmote>? response = await _http.GetFromJsonAsync<HelixPaginatedResponse<TwitchEmote>>(
                    url, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                        PropertyNameCaseInsensitive = true,
                    }, ct);

                if (response?.Data is not null)
                {
                    allEmotes.AddRange(response.Data);
                }

                cursor = response?.Pagination?.Cursor;
            }
            while (!string.IsNullOrEmpty(cursor));

            return allEmotes;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to get user emotes for {UserId}", userId);
            return Array.Empty<TwitchEmote>();
        }
    }

    private sealed class HelixDataResponse<T>
    {
        [JsonPropertyName("data")]
        public T[]? Data { get; init; }
    }

    private sealed class HelixPaginatedResponse<T>
    {
        [JsonPropertyName("data")]
        public T[]? Data { get; init; }

        [JsonPropertyName("pagination")]
        public HelixPaginationInfo? Pagination { get; init; }
    }

    private sealed class HelixPaginationInfo
    {
        [JsonPropertyName("cursor")]
        public string? Cursor { get; init; }
    }
}

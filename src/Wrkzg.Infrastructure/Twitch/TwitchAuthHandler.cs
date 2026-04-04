using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Twitch;

/// <summary>
/// HTTP message handler that automatically injects the Broadcaster access token
/// into outgoing Twitch Helix API requests and handles token refresh on 401.
/// </summary>
public class TwitchAuthHandler : DelegatingHandler
{
    private readonly ISecureStorage _storage;
    private readonly ITwitchOAuthService _oauth;
    private readonly IAuthStateNotifier _authNotifier;
    private readonly ILogger<TwitchAuthHandler> _logger;

    private static readonly SemaphoreSlim _refreshLock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="TwitchAuthHandler"/> class.
    /// </summary>
    public TwitchAuthHandler(
        ISecureStorage storage,
        ITwitchOAuthService oauth,
        IAuthStateNotifier authNotifier,
        ILogger<TwitchAuthHandler> logger)
    {
        _storage = storage;
        _oauth = oauth;
        _authNotifier = authNotifier;
        _logger = logger;
    }

    /// <summary>
    /// Intercepts outgoing HTTP requests to inject the Bearer token and Client-Id header,
    /// and automatically refreshes the token on 401 Unauthorized responses.
    /// </summary>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        TwitchTokens? tokens = await _storage.LoadTokensAsync(TokenType.Broadcaster, ct);

        if (tokens is null)
        {
            _logger.LogWarning("No Broadcaster token available — Helix API request will likely fail");
            return await base.SendAsync(request, ct);
        }

        // Proactive refresh if token is likely expired
        if (tokens.IsLikelyExpired)
        {
            _logger.LogInformation("Broadcaster token is likely expired — refreshing proactively");
            tokens = await TryRefreshAsync(tokens, ct);
            if (tokens is null)
            {
                return await base.SendAsync(request, ct);
            }
        }

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

        // Set Client-Id header required by Helix API
        string? clientId = await _storage.LoadClientIdAsync(ct);
        if (clientId is not null)
        {
            request.Headers.TryAddWithoutValidation("Client-Id", clientId);
        }

        HttpResponseMessage response = await base.SendAsync(request, ct);

        // 401 → refresh and retry once
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogInformation("Received 401 from Twitch — attempting token refresh");

            tokens = await TryRefreshAsync(tokens, ct);
            if (tokens is null)
            {
                return response;
            }

            using HttpRequestMessage retry = await CloneRequestAsync(request);
            retry.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

            response.Dispose();
            return await base.SendAsync(retry, ct);
        }

        return response;
    }

    private async Task<TwitchTokens?> TryRefreshAsync(TwitchTokens currentTokens, CancellationToken ct)
    {
        bool lockTaken = await _refreshLock.WaitAsync(TimeSpan.FromSeconds(10), ct);
        if (!lockTaken)
        {
            _logger.LogWarning("Timed out waiting for refresh lock — skipping refresh");
            return null;
        }

        try
        {
            // Double-check: another thread may have already refreshed
            TwitchTokens? stored = await _storage.LoadTokensAsync(TokenType.Broadcaster, ct);
            if (stored is not null && stored.AccessToken != currentTokens.AccessToken)
            {
                _logger.LogDebug("Another thread already refreshed the token — using new token");
                return stored;
            }

            TwitchTokens newTokens = await _oauth.RefreshTokenAsync(currentTokens.RefreshToken, ct);
            await _storage.SaveTokensAsync(TokenType.Broadcaster, newTokens, ct);

            _logger.LogInformation("Broadcaster token refreshed successfully");
            return newTokens;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Token refresh failed — the user may need to re-authorize. Notifying frontend.");

            await _authNotifier.NotifyAuthStateChangedAsync(new AuthState
            {
                TokenType = TokenType.Broadcaster,
                IsAuthenticated = false,
                TwitchUsername = null,
                TwitchDisplayName = null,
                TwitchUserId = null,
                Scopes = null
            }, ct);

            await _storage.DeleteTokensAsync(TokenType.Broadcaster, ct);
            return null;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage original)
    {
        HttpRequestMessage clone = new(original.Method, original.RequestUri);

        foreach (KeyValuePair<string, IEnumerable<string>> header in original.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (original.Content is not null)
        {
            byte[] content = await original.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(content);

            foreach (KeyValuePair<string, IEnumerable<string>> header in original.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        foreach (KeyValuePair<string, object?> option in original.Options)
        {
            clone.Options.TryAdd(option.Key, option.Value);
        }

        return clone;
    }
}

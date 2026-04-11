using System;
using System.Collections.Concurrent;
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
/// HTTP message handler that automatically injects the access token for a specific
/// <see cref="TokenType"/> into outgoing Twitch Helix API requests and handles token refresh on 401.
/// </summary>
public class TwitchAuthHandler : DelegatingHandler
{
    private readonly TokenType _tokenType;
    private readonly ISecureStorage _storage;
    private readonly ITwitchOAuthService _oauth;
    private readonly IAuthStateNotifier _authNotifier;
    private readonly ILogger<TwitchAuthHandler> _logger;

    private static readonly ConcurrentDictionary<TokenType, SemaphoreSlim> _refreshLocks = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TwitchAuthHandler"/> class
    /// for the specified token type.
    /// </summary>
    /// <param name="tokenType">Which token (Bot or Broadcaster) this handler manages.</param>
    /// <param name="storage">Secure storage for loading and saving tokens.</param>
    /// <param name="oauth">OAuth service for refreshing expired tokens.</param>
    /// <param name="authNotifier">Notifier for broadcasting auth state changes to the frontend.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public TwitchAuthHandler(
        TokenType tokenType,
        ISecureStorage storage,
        ITwitchOAuthService oauth,
        IAuthStateNotifier authNotifier,
        ILogger<TwitchAuthHandler> logger)
    {
        _tokenType = tokenType;
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
        TwitchTokens? tokens = await _storage.LoadTokensAsync(_tokenType, ct);

        if (tokens is null)
        {
            _logger.LogWarning("No {TokenType} token available — Helix API request will likely fail", _tokenType);
            return await base.SendAsync(request, ct);
        }

        // Proactive refresh if token is likely expired
        if (tokens.IsLikelyExpired)
        {
            _logger.LogInformation("{TokenType} token is likely expired — refreshing proactively", _tokenType);
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

        // 401 -> refresh and retry once
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogInformation("Received 401 from Twitch for {TokenType} — attempting token refresh", _tokenType);

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
        SemaphoreSlim refreshLock = _refreshLocks.GetOrAdd(_tokenType, _ => new SemaphoreSlim(1, 1));

        bool lockTaken = await refreshLock.WaitAsync(TimeSpan.FromSeconds(10), ct);
        if (!lockTaken)
        {
            _logger.LogWarning("Timed out waiting for {TokenType} refresh lock — skipping refresh", _tokenType);
            return null;
        }

        try
        {
            // Double-check: another thread may have already refreshed
            TwitchTokens? stored = await _storage.LoadTokensAsync(_tokenType, ct);
            if (stored is not null && stored.AccessToken != currentTokens.AccessToken)
            {
                _logger.LogDebug("Another thread already refreshed the {TokenType} token — using new token", _tokenType);
                return stored;
            }

            TwitchTokens newTokens = await _oauth.RefreshTokenAsync(currentTokens.RefreshToken, ct);
            await _storage.SaveTokensAsync(_tokenType, newTokens, ct);

            _logger.LogInformation("{TokenType} token refreshed successfully", _tokenType);
            return newTokens;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "{TokenType} token refresh failed — the user may need to re-authorize. Notifying frontend.",
                _tokenType);

            await _authNotifier.NotifyAuthStateChangedAsync(new AuthState
            {
                TokenType = _tokenType,
                IsAuthenticated = false,
                TwitchUsername = null,
                TwitchDisplayName = null,
                TwitchUserId = null,
                Scopes = null
            }, ct);

            await _storage.DeleteTokensAsync(_tokenType, ct);
            return null;
        }
        finally
        {
            refreshLock.Release();
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

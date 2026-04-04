using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Twitch;

/// <summary>
/// Implements Twitch OAuth 2.0 Authorization Code Grant Flow.
///
/// Credential resolution order (for both Client ID and Client Secret):
///   1. ISecureStorage (OS Keystore — DPAPI / Keychain) ← production path
///   2. IConfiguration (appsettings.Development.json)   ← dev fallback only
///   3. Exception if neither is available
///
/// The Setup Wizard (Schritt 8) stores credentials exclusively in ISecureStorage.
/// appsettings.Development.json is only used by contributors during local development.
/// </summary>
public class TwitchOAuthService : ITwitchOAuthService
{
    private readonly HttpClient _http;
    private readonly ISecureStorage _storage;
    private readonly IConfiguration _config;
    private readonly ILogger<TwitchOAuthService> _logger;

    private const string BotScopes = "chat:read chat:edit user:write:chat";

    private const string BroadcasterScopes =
        "moderator:read:followers " +
        "channel:read:polls " +
        "channel:manage:polls " +
        "bits:read " +
        "channel:read:subscriptions " +
        "moderator:manage:shoutouts " +
        "user:write:chat " +
        "channel:read:redemptions " +
        "channel:manage:redemptions";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="TwitchOAuthService"/> class.
    /// </summary>
    public TwitchOAuthService(
        HttpClient http,
        ISecureStorage storage,
        IConfiguration config,
        ILogger<TwitchOAuthService> logger)
    {
        _http = http;
        _storage = storage;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Builds a Twitch OAuth authorization URL with PKCE state parameter
    /// for the specified token type (Bot or Broadcaster).
    /// </summary>
    public (string url, string state) BuildAuthorizationUrl(TokenType tokenType)
    {
        // GetClientIdAsync is async but BuildAuthorizationUrl is sync in the interface.
        // We use .GetAwaiter().GetResult() here — acceptable because this is called
        // once per OAuth flow initiation, not in a hot path.
        string clientId = GetClientIdAsync(CancellationToken.None).GetAwaiter().GetResult();
        string redirectUri = GetRedirectUri();
        string scopes = tokenType == TokenType.Bot ? BotScopes : BroadcasterScopes;

        string state = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        string compositeState = $"{tokenType.ToString().ToLowerInvariant()}:{state}";

        string url =
            $"https://id.twitch.tv/oauth2/authorize" +
            $"?client_id={Uri.EscapeDataString(clientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&response_type=code" +
            $"&scope={Uri.EscapeDataString(scopes)}" +
            $"&state={Uri.EscapeDataString(compositeState)}" +
            $"&force_verify=true";

        _logger.LogDebug("Built auth URL for {TokenType}", tokenType);

        return (url, compositeState);
    }

    /// <summary>Exchanges an authorization code for access and refresh tokens.</summary>
    public async Task<TwitchTokens> ExchangeCodeAsync(string code, CancellationToken ct = default)
    {
        string clientId = await GetClientIdAsync(ct);
        string clientSecret = await GetClientSecretAsync(ct);
        string redirectUri = GetRedirectUri();

        _logger.LogInformation("Exchanging authorization code for tokens");

        HttpResponseMessage response = await _http.PostAsync(
            "https://id.twitch.tv/oauth2/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["code"] = code,
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = redirectUri
            }), ct);

        if (!response.IsSuccessStatusCode)
        {
            string errorBody = await response.Content.ReadAsStringAsync(ct);
            string sanitizedBody = errorBody.Length > 200 ? errorBody[..200] + "…" : errorBody;
            _logger.LogError("Token exchange failed: {StatusCode} — {Body}",
                response.StatusCode, sanitizedBody);
            throw new HttpRequestException(
                $"Twitch token exchange failed ({response.StatusCode})");
        }

        TwitchTokens? tokens = await response.Content.ReadFromJsonAsync<TwitchTokens>(
            _jsonOptions, ct);

        if (tokens is null)
        {
            throw new InvalidOperationException("Twitch returned an empty token response.");
        }

        _logger.LogInformation("Token exchange successful — scopes: {Scopes}",
            string.Join(", ", tokens.Scope));

        return tokens with { ObtainedAt = DateTimeOffset.UtcNow };
    }

    /// <summary>Refreshes an expired access token using a refresh token.</summary>
    public async Task<TwitchTokens> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        string clientId = await GetClientIdAsync(ct);
        string clientSecret = await GetClientSecretAsync(ct);

        _logger.LogInformation("Refreshing access token");

        HttpResponseMessage response = await _http.PostAsync(
            "https://id.twitch.tv/oauth2/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken
            }), ct);

        if (!response.IsSuccessStatusCode)
        {
            string errorBody = await response.Content.ReadAsStringAsync(ct);
            string sanitizedBody = errorBody.Length > 200 ? errorBody[..200] + "…" : errorBody;
            _logger.LogWarning("Token refresh failed: {StatusCode} — {Body}",
                response.StatusCode, sanitizedBody);
            throw new HttpRequestException(
                $"Twitch token refresh failed ({response.StatusCode})");
        }

        TwitchTokens? tokens = await response.Content.ReadFromJsonAsync<TwitchTokens>(
            _jsonOptions, ct);

        if (tokens is null)
        {
            throw new InvalidOperationException("Twitch returned an empty refresh response.");
        }

        _logger.LogInformation("Token refresh successful");

        return tokens with { ObtainedAt = DateTimeOffset.UtcNow };
    }

    /// <summary>Revokes an access token with the Twitch OAuth revocation endpoint.</summary>
    public async Task RevokeTokenAsync(string accessToken, CancellationToken ct = default)
    {
        string clientId = await GetClientIdAsync(ct);

        HttpResponseMessage response = await _http.PostAsync(
            "https://id.twitch.tv/oauth2/revoke",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["token"] = accessToken
            }), ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Token revocation returned {StatusCode}", response.StatusCode);
        }
        else
        {
            _logger.LogInformation("Token revoked successfully");
        }
    }

    /// <summary>Validates an access token against the Twitch validation endpoint, returning user info if valid.</summary>
    public async Task<TwitchTokenValidation?> ValidateTokenAsync(
        string accessToken, CancellationToken ct = default)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "https://id.twitch.tv/oauth2/validate");
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("OAuth", accessToken);

        HttpResponseMessage response = await _http.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogDebug("Token validation returned {StatusCode} — token is invalid/expired",
                response.StatusCode);
            return null;
        }

        return await response.Content.ReadFromJsonAsync<TwitchTokenValidation>(_jsonOptions, ct);
    }

    // ─── Credential Resolution ────────────────────────────────────────

    /// <summary>
    /// Resolves the Client ID: Keystore first, then appsettings fallback (dev only).
    /// </summary>
    private async Task<string> GetClientIdAsync(CancellationToken ct)
    {
        // 1. OS Keystore (production path — set by Setup Wizard)
        string? clientId = await _storage.LoadClientIdAsync(ct);
        if (!string.IsNullOrWhiteSpace(clientId))
        {
            return clientId;
        }

        // 2. appsettings fallback (dev only)
        clientId = _config["Twitch:ClientId"];
        if (!string.IsNullOrWhiteSpace(clientId))
        {
            _logger.LogDebug("Using Client ID from appsettings (dev fallback)");
            return clientId;
        }

        throw new InvalidOperationException(
            "Twitch Client ID is not configured. " +
            "Please complete the Setup Wizard or set Twitch:ClientId in appsettings.Development.json.");
    }

    /// <summary>
    /// Resolves the Client Secret: Keystore first, then appsettings fallback (dev only).
    /// </summary>
    private async Task<string> GetClientSecretAsync(CancellationToken ct)
    {
        // 1. OS Keystore (production path — set by Setup Wizard)
        string? secret = await _storage.LoadClientSecretAsync(ct);
        if (!string.IsNullOrWhiteSpace(secret))
        {
            return secret;
        }

        // 2. appsettings fallback (dev only)
        secret = _config["Twitch:ClientSecret"];
        if (!string.IsNullOrWhiteSpace(secret))
        {
            _logger.LogDebug("Using Client Secret from appsettings (dev fallback)");
            return secret;
        }

        throw new InvalidOperationException(
            "Twitch Client Secret is not configured. " +
            "Please complete the Setup Wizard or set Twitch:ClientSecret in appsettings.Development.json.");
    }

    private string GetRedirectUri()
    {
        string port = _config["Bot:Port"] ?? "5050";
        return $"http://localhost:{port}/auth/callback";
    }
}

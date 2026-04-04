using System;
using System.Threading;
using System.Threading.Tasks;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Handles Twitch OAuth 2.0 Authorization Code Flow.
/// Implemented in Wrkzg.Infrastructure.
/// </summary>
public interface ITwitchOAuthService
{
    /// <summary>
    /// Builds the Twitch authorization URL for the given token type.
    /// Includes a cryptographically random state parameter for CSRF protection.
    /// </summary>
    (string url, string state) BuildAuthorizationUrl(TokenType tokenType);

    /// <summary>
    /// Exchanges an authorization code for an access/refresh token pair.
    /// </summary>
    Task<TwitchTokens> ExchangeCodeAsync(string code, CancellationToken ct = default);

    /// <summary>
    /// Uses the refresh token to obtain a new access/refresh token pair.
    /// </summary>
    Task<TwitchTokens> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>
    /// Revokes an access token at Twitch's token revocation endpoint.
    /// </summary>
    Task RevokeTokenAsync(string accessToken, CancellationToken ct = default);

    /// <summary>
    /// Validates an access token at Twitch's /validate endpoint.
    /// Returns user info if valid, null if expired/revoked.
    /// </summary>
    Task<TwitchTokenValidation?> ValidateTokenAsync(string accessToken, CancellationToken ct = default);
}

/// <summary>
/// Result of the Twitch /validate endpoint.
/// </summary>
public sealed class TwitchTokenValidation
{
    /// <summary>The client identifier the token was issued to.</summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>The login name of the authenticated user.</summary>
    public string Login { get; init; } = string.Empty;

    /// <summary>The Twitch-assigned user identifier of the authenticated user.</summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>The OAuth scopes granted to this token.</summary>
    public string[] Scopes { get; init; } = Array.Empty<string>();

    /// <summary>The number of seconds until the token expires.</summary>
    public int ExpiresIn { get; init; }
}

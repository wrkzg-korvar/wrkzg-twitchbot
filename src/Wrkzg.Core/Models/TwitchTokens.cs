using System;
using System.Text.Json.Serialization;

namespace Wrkzg.Core.Models;

/// <summary>
/// Holds the OAuth token pair received from Twitch.
/// Serialized to JSON for encrypted storage via <see cref="Wrkzg.Core.Interfaces.ISecureStorage"/>.
/// </summary>
public sealed record TwitchTokens
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; } = string.Empty;

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; init; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }

    [JsonPropertyName("scope")]
    public string[] Scope { get; init; } = Array.Empty<string>();

    [JsonPropertyName("token_type")]
    public string TokenType { get; init; } = "bearer";

    /// <summary>
    /// UTC timestamp when this token was obtained. Set by the application, not by Twitch.
    /// Used to calculate whether the token is likely expired before making an API call.
    /// </summary>
    [JsonPropertyName("obtained_at")]
    public DateTimeOffset ObtainedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Returns true if the access token is likely expired (with a 5-minute safety margin).
    /// </summary>
    [JsonIgnore]
    public bool IsLikelyExpired =>
        DateTimeOffset.UtcNow >= ObtainedAt.AddSeconds(ExpiresIn).AddMinutes(-5);
}

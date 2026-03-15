namespace Wrkzg.Core.Models;

/// <summary>
/// The Twitch Developer Application credentials.
/// Each user registers their own app at dev.twitch.tv/console.
/// Bound from configuration section "Twitch".
/// </summary>
public sealed class TwitchAppCredentials
{
    /// <summary>
    /// Client ID from the Twitch Developer Console.
    /// </summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>
    /// Client Secret from the Twitch Developer Console.
    /// Required because Twitch does not support PKCE on the Authorization Code Flow.
    /// Stored encrypted via ISecureStorage, not in appsettings in production.
    /// </summary>
    public string ClientSecret { get; init; } = string.Empty;
}

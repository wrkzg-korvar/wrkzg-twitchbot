namespace Wrkzg.Core.Models;

/// <summary>
/// Represents the authentication state for one token type.
/// Pushed to the frontend via SignalR whenever the state changes.
/// </summary>
public sealed class AuthState
{
    /// <summary>Identifies whether this state belongs to the Bot or Broadcaster token.</summary>
    public TokenType TokenType { get; init; }

    /// <summary>Whether a valid OAuth token exists for this token type.</summary>
    public bool IsAuthenticated { get; init; }

    /// <summary>Twitch login name (lowercase) of the authenticated account.</summary>
    public string? TwitchUsername { get; init; }

    /// <summary>Twitch display name (may contain casing/unicode) of the authenticated account.</summary>
    public string? TwitchDisplayName { get; init; }

    /// <summary>Twitch user ID of the authenticated account.</summary>
    public string? TwitchUserId { get; init; }

    /// <summary>OAuth scopes granted for this token.</summary>
    public string[]? Scopes { get; init; }
}

namespace Wrkzg.Core.Models;

/// <summary>
/// Identifies which Twitch account a token belongs to.
/// The bot requires two separate OAuth tokens for full functionality.
/// </summary>
public enum TokenType
{
    /// <summary>
    /// Bot account token — used for IRC chat (read + write).
    /// Scopes: chat:read, chat:edit
    /// </summary>
    Bot = 0,

    /// <summary>
    /// Broadcaster account token — used for Helix API, EventSub, polls.
    /// Scopes: moderator:read:followers, channel:read:polls, channel:manage:polls,
    ///         bits:read, channel:read:subscriptions
    /// </summary>
    Broadcaster = 1
}

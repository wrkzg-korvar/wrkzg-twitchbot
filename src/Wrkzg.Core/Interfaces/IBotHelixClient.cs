using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Twitch Helix API client authenticated with the Bot token.
/// Used for moderation actions (announcements, timeouts) where the bot acts as a moderator.
/// The bot's own moderator_id is resolved internally from the Bot OAuth token.
/// </summary>
public interface IBotHelixClient
{
    /// <summary>
    /// Sends a chat announcement via the Helix API (POST /chat/announcements).
    /// Requires moderator:manage:announcements scope on the Bot token.
    /// The bot's moderator_id is resolved internally.
    /// </summary>
    /// <param name="broadcasterId">The broadcaster's Twitch user ID.</param>
    /// <param name="message">The announcement text.</param>
    /// <param name="color">Announcement color: primary, blue, green, orange, purple.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the announcement was sent successfully.</returns>
    Task<bool> SendAnnouncementAsync(string broadcasterId, string message, string color = "primary", CancellationToken ct = default);

    /// <summary>
    /// Times out a user in the channel via Helix API (POST /moderation/bans).
    /// Requires moderator:manage:banned_users scope on the Bot token.
    /// The bot's moderator_id is resolved internally.
    /// </summary>
    /// <param name="broadcasterId">The broadcaster's Twitch user ID.</param>
    /// <param name="userId">The Twitch user ID of the user to timeout.</param>
    /// <param name="durationSeconds">Duration of the timeout in seconds.</param>
    /// <param name="reason">Reason for the timeout.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the timeout was applied successfully.</returns>
    Task<bool> TimeoutUserAsync(string broadcasterId, string userId, int durationSeconds, string reason, CancellationToken ct = default);

    /// <summary>Gets global Twitch emotes. Only requires a valid app/user token + Client-Id.</summary>
    Task<IReadOnlyList<TwitchEmote>> GetGlobalEmotesAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets all emotes available to the bot user (global, subscribed channels, bits, follower).
    /// Requires the user:read:emotes scope. Helix API: GET /chat/emotes/user?user_id={id}
    /// </summary>
    Task<IReadOnlyList<TwitchEmote>> GetUserEmotesAsync(string userId, CancellationToken ct = default);
}

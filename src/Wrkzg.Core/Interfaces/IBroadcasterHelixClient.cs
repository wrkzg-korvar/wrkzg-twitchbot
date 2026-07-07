using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Twitch Helix API client authenticated with the Broadcaster token.
/// Used for stream status polling, user info, chat messages, polls, and channel point rewards.
/// </summary>
public interface IBroadcasterHelixClient
{
    /// <summary>
    /// Gets the current stream info for a channel. Returns null if offline.
    /// </summary>
    Task<StreamInfo?> GetStreamAsync(string channelLogin, CancellationToken ct = default);

    /// <summary>
    /// Gets user info by login name.
    /// </summary>
    Task<HelixUserInfo?> GetUserAsync(string login, CancellationToken ct = default);

    /// <summary>
    /// Sends a chat message via the Helix API (POST /chat/messages).
    /// Uses the Broadcaster token. Returns true if sent successfully.
    /// </summary>
    Task<bool> SendChatMessageAsync(string broadcasterId, string senderId, string message, CancellationToken ct = default);

    /// <summary>
    /// Gets channel information including the game/category.
    /// Helix API: GET /channels?broadcaster_id={id}
    /// </summary>
    Task<ChannelInfo?> GetChannelInfoAsync(string broadcasterId, CancellationToken ct = default);

    /// <summary>Gets custom channel point rewards for the broadcaster.</summary>
    Task<IReadOnlyList<TwitchCustomReward>> GetCustomRewardsAsync(CancellationToken ct = default);

    /// <summary>Gets global Twitch emotes available to all users.</summary>
    Task<IReadOnlyList<TwitchEmote>> GetGlobalEmotesAsync(CancellationToken ct = default);

    /// <summary>Gets channel-specific emotes for the given broadcaster.</summary>
    Task<IReadOnlyList<TwitchEmote>> GetChannelEmotesAsync(string broadcasterId, CancellationToken ct = default);

    /// <summary>
    /// Gets all emotes available to a specific user (global, subscribed channels, bits, follower).
    /// Requires the user:read:emotes scope. Helix API: GET /chat/emotes/user?user_id={id}
    /// </summary>
    Task<IReadOnlyList<TwitchEmote>> GetUserEmotesAsync(string userId, CancellationToken ct = default);

    /// <summary>Updates the channel title and/or game for the broadcaster.</summary>
    Task<bool> ModifyChannelInfoAsync(string broadcasterId, string? title, string? gameId, CancellationToken ct = default);

    /// <summary>Searches for a game/category by name. Returns the first match or null.</summary>
    Task<TwitchGameInfo?> GetGameByNameAsync(string gameName, CancellationToken ct = default);

    /// <summary>Creates a Twitch-native poll via Helix API.</summary>
    Task<TwitchPollResponse?> CreateTwitchPollAsync(
        string broadcasterId,
        string question,
        string[] options,
        int durationSeconds,
        CancellationToken ct = default);

    /// <summary>Ends a Twitch-native poll.</summary>
    Task<bool> EndTwitchPollAsync(
        string broadcasterId,
        string pollId,
        string status,
        CancellationToken ct = default);

    /// <summary>
    /// Checks whether a specific user follows the broadcaster's channel.
    /// Helix API: GET /channels/followers?broadcaster_id={broadcasterId}&amp;user_id={userId}
    /// Requires the moderator:read:followers scope on the Broadcaster token.
    /// </summary>
    /// <param name="broadcasterId">The broadcaster's Twitch user ID.</param>
    /// <param name="userId">The user ID to check for follow status.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the user follows the broadcaster; false otherwise (or on error).</returns>
    Task<bool> IsUserFollowingAsync(string broadcasterId, string userId, CancellationToken ct = default);

    /// <summary>
    /// Returns the date on which the specified user started following the broadcaster,
    /// or null if the user does not follow the channel (or the request was not authorized).
    /// Helix API: GET /channels/followers?broadcaster_id={broadcasterId}&amp;user_id={userId}
    /// Requires the moderator:read:followers scope on the Broadcaster token.
    /// </summary>
    /// <param name="broadcasterId">The broadcaster's Twitch user ID.</param>
    /// <param name="userId">The user ID whose follow date should be retrieved.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The follow date, or null if not following / not authorized.</returns>
    Task<DateTimeOffset?> GetFollowedAtAsync(string broadcasterId, string userId, CancellationToken ct = default);

    /// <summary>
    /// Sends a shoutout to another channel via Helix API (POST /chat/shoutouts).
    /// Requires moderator:manage:shoutouts scope on the Broadcaster token.
    /// Rate limit: 1 per target per 60 minutes, 3 total per 120 seconds.
    /// </summary>
    /// <param name="fromBroadcasterId">The broadcaster's own Twitch user ID.</param>
    /// <param name="targetUserId">The Twitch user ID of the channel to shout out.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the shoutout was sent successfully.</returns>
    Task<bool> SendShoutoutAsync(string fromBroadcasterId, string targetUserId, CancellationToken ct = default);
}

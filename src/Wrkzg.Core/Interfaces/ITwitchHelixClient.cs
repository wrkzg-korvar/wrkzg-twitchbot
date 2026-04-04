using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Abstraction over the Twitch Helix REST API.
/// Used for stream status polling, user info, and future poll/EventSub management.
/// </summary>
public interface ITwitchHelixClient
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

    /// <summary>Timeouts a user in the channel via Helix API (POST /moderation/bans).</summary>
    Task<bool> TimeoutUserAsync(string userId, int durationSeconds, string reason, CancellationToken ct = default);

    /// <summary>Creates a Twitch-native poll via Helix API.</summary>
    Task<TwitchPollResponse?> CreateTwitchPollAsync(
        string broadcasterId,
        string question,
        string[] options,
        int durationSeconds,
        CancellationToken ct = default);

    /// <summary>Gets custom channel point rewards for the broadcaster.</summary>
    Task<IReadOnlyList<TwitchCustomReward>> GetCustomRewardsAsync(CancellationToken ct = default);

    /// <summary>Ends a Twitch-native poll.</summary>
    Task<bool> EndTwitchPollAsync(
        string broadcasterId,
        string pollId,
        string status,
        CancellationToken ct = default);
}

/// <summary>
/// Response from the Twitch Helix "Create Poll" endpoint.
/// </summary>
public sealed class TwitchPollResponse
{
    /// <summary>The Twitch-assigned poll identifier.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>The poll question/title text.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>The current poll status (e.g. "ACTIVE", "COMPLETED", "TERMINATED").</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>The poll duration in seconds.</summary>
    public int Duration { get; init; }
}

/// <summary>
/// Stream information from the Helix "Get Streams" endpoint.
/// </summary>
public sealed class StreamInfo
{
    /// <summary>The Twitch-assigned stream identifier.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>The broadcaster's login name (lowercase).</summary>
    public string UserLogin { get; init; } = string.Empty;

    /// <summary>The current stream title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>The name of the current game/category being streamed.</summary>
    public string GameName { get; init; } = string.Empty;

    /// <summary>The Twitch-assigned identifier of the current game/category.</summary>
    public string GameId { get; init; } = string.Empty;

    /// <summary>The current number of viewers watching the stream.</summary>
    public int ViewerCount { get; init; }

    /// <summary>The ISO 8601 timestamp of when the stream started.</summary>
    public string StartedAt { get; init; } = string.Empty;
}

/// <summary>
/// User information from the Helix "Get Users" endpoint.
/// </summary>
public sealed class HelixUserInfo
{
    /// <summary>The Twitch-assigned user identifier.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>The user's login name (lowercase).</summary>
    public string Login { get; init; } = string.Empty;

    /// <summary>The user's display name (preserves capitalization).</summary>
    public string DisplayName { get; init; } = string.Empty;
}

/// <summary>
/// Channel information from the Helix "Get Channel Information" endpoint.
/// </summary>
public sealed class ChannelInfo
{
    /// <summary>The Twitch-assigned broadcaster identifier.</summary>
    public string BroadcasterId { get; init; } = string.Empty;

    /// <summary>The broadcaster's display name.</summary>
    public string BroadcasterName { get; init; } = string.Empty;

    /// <summary>The name of the current game/category set on the channel.</summary>
    public string GameName { get; init; } = string.Empty;

    /// <summary>The current channel title.</summary>
    public string Title { get; init; } = string.Empty;
}

/// <summary>
/// Custom Channel Point Reward from the Helix API.
/// </summary>
public sealed class TwitchCustomReward
{
    /// <summary>The Twitch-assigned reward identifier.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>The display title of the reward.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>The channel point cost to redeem this reward.</summary>
    public int Cost { get; init; }

    /// <summary>Whether the reward is currently enabled and redeemable.</summary>
    public bool IsEnabled { get; init; }

    /// <summary>The prompt text shown to viewers when redeeming, if any.</summary>
    public string? Prompt { get; init; }

    /// <summary>Whether the reward requires the user to enter text when redeeming.</summary>
    public bool IsUserInputRequired { get; init; }
}

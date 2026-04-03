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
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int Duration { get; init; }
}

/// <summary>
/// Stream information from the Helix "Get Streams" endpoint.
/// </summary>
public sealed class StreamInfo
{
    public string Id { get; init; } = string.Empty;
    public string UserLogin { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string GameName { get; init; } = string.Empty;
    public string GameId { get; init; } = string.Empty;
    public int ViewerCount { get; init; }
    public string StartedAt { get; init; } = string.Empty;
}

/// <summary>
/// User information from the Helix "Get Users" endpoint.
/// </summary>
public sealed class HelixUserInfo
{
    public string Id { get; init; } = string.Empty;
    public string Login { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
}

/// <summary>
/// Channel information from the Helix "Get Channel Information" endpoint.
/// </summary>
public sealed class ChannelInfo
{
    public string BroadcasterId { get; init; } = string.Empty;
    public string BroadcasterName { get; init; } = string.Empty;
    public string GameName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
}

/// <summary>
/// Custom Channel Point Reward from the Helix API.
/// </summary>
public sealed class TwitchCustomReward
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public int Cost { get; init; }
    public bool IsEnabled { get; init; }
    public string? Prompt { get; init; }
    public bool IsUserInputRequired { get; init; }
}

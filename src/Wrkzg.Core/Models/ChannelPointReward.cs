using System;

namespace Wrkzg.Core.Models;

/// <summary>
/// Maps a Twitch Channel Point Reward to a bot action.
/// The RewardId comes from Twitch — the bot doesn't create the reward itself,
/// it only reacts when a configured reward is redeemed.
/// </summary>
public class ChannelPointReward
{
    public int Id { get; set; }

    /// <summary>Twitch Reward ID (UUID from Twitch API).</summary>
    public string TwitchRewardId { get; set; } = string.Empty;

    /// <summary>Display name of the reward (synced from Twitch).</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Cost in channel points (synced from Twitch).</summary>
    public int Cost { get; set; }

    /// <summary>What action to perform when redeemed.</summary>
    public RewardActionType ActionType { get; set; }

    /// <summary>
    /// Action payload — depends on ActionType:
    /// - ChatMessage: the message template (supports {user}, {input})
    /// - CounterIncrement/Decrement: the Counter ID
    /// - Timeout: duration in seconds
    /// - SoundAlert: sound file identifier (future)
    /// </summary>
    public string ActionPayload { get; set; } = string.Empty;

    /// <summary>Whether to auto-fulfill the redemption after action.</summary>
    public bool AutoFulfill { get; set; } = true;

    /// <summary>Whether this handler is active.</summary>
    public bool IsEnabled { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public enum RewardActionType
{
    ChatMessage = 0,
    CounterIncrement = 1,
    CounterDecrement = 2,
    Timeout = 3,
    Highlight = 4,
    SoundAlert = 5
}

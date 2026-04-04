using System;

namespace Wrkzg.Core.Models;

/// <summary>
/// Maps a Twitch Channel Point Reward to a bot action.
/// The RewardId comes from Twitch — the bot doesn't create the reward itself,
/// it only reacts when a configured reward is redeemed.
/// </summary>
public class ChannelPointReward
{
    /// <summary>Primary key.</summary>
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

    /// <summary>When this reward handler was created.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Defines the action the bot performs when a channel point reward is redeemed.
/// </summary>
public enum RewardActionType
{
    /// <summary>Send a templated message to chat.</summary>
    ChatMessage = 0,

    /// <summary>Increment a named counter by one.</summary>
    CounterIncrement = 1,

    /// <summary>Decrement a named counter by one.</summary>
    CounterDecrement = 2,

    /// <summary>Timeout the redeeming user for the configured duration.</summary>
    Timeout = 3,

    /// <summary>Highlight the redemption in the dashboard overlay.</summary>
    Highlight = 4,

    /// <summary>Play a sound alert (reserved for future implementation).</summary>
    SoundAlert = 5
}

using System.Collections.Generic;

namespace Wrkzg.Core.Interfaces;

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

/// <summary>Twitch emote information from the Helix API.</summary>
public sealed class TwitchEmote
{
    /// <summary>The Twitch-assigned emote identifier.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>The emote name that users type in chat.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>The emote type: 'globals', 'subscriptions', 'bitstier', 'follower', etc.</summary>
    public string EmoteType { get; init; } = string.Empty;
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

/// <summary>
/// Game/category information from the Helix API.
/// </summary>
public sealed class TwitchGameInfo
{
    /// <summary>The Twitch-assigned game identifier.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>The game/category name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>URL to the box art image template.</summary>
    public string BoxArtUrl { get; init; } = string.Empty;
}

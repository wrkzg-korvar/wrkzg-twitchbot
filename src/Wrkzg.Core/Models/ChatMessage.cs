using System;
using System.Collections.Generic;

namespace Wrkzg.Core.Models;

/// <summary>
/// Immutable representation of an incoming Twitch chat message.
/// Passed through the processing pipeline: TwitchChatClient → CommandProcessor → ChatGameManager.
/// </summary>
/// <param name="UserId">Twitch user ID of the message author.</param>
/// <param name="Username">Twitch login name (lowercase) of the message author.</param>
/// <param name="DisplayName">Twitch display name (may contain casing/unicode) of the message author.</param>
/// <param name="Content">Raw message text as received from IRC.</param>
/// <param name="IsModerator">Whether the author has moderator privileges in the channel.</param>
/// <param name="IsSubscriber">Whether the author is a subscriber of the channel.</param>
/// <param name="IsBroadcaster">Whether the author is the broadcaster (channel owner).</param>
/// <param name="Timestamp">UTC timestamp when the message was received.</param>
public sealed record ChatMessage(
    string UserId,
    string Username,
    string DisplayName,
    string Content,
    bool IsModerator,
    bool IsSubscriber,
    bool IsBroadcaster,
    DateTimeOffset Timestamp
)
{
    /// <summary>The channel the message was received in.</summary>
    public string Channel { get; init; } = string.Empty;

    /// <summary>
    /// Emotes in this message. Key: emote ID, Value: list of "startIndex-endIndex" positions.
    /// Used by overlays to render Twitch emote images.
    /// </summary>
    public Dictionary<string, List<string>> Emotes { get; init; } = new();
}

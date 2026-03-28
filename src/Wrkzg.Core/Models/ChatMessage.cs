using System;
using System.Collections.Generic;

namespace Wrkzg.Core.Models;

/// <summary>
/// Immutable representation of an incoming Twitch chat message.
/// Passed through the processing pipeline: TwitchChatClient → CommandProcessor → ChatGameManager.
/// </summary>
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

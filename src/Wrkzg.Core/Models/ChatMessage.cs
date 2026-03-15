using System;

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
}

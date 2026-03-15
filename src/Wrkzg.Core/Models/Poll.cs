using System;
using System.Collections.Generic;

namespace Wrkzg.Core.Models;

/// <summary>
/// A poll — either bot-native (chat-based) or Twitch-native (Helix API).
/// </summary>
public class Poll
{
    public int Id { get; set; }
    public string Question { get; set; } = string.Empty;

    /// <summary>Poll options stored as JSON array in SQLite.</summary>
    public string[] Options { get; set; } = Array.Empty<string>();

    public bool IsActive { get; set; } = true;
    public DateTimeOffset EndsAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public PollSource Source { get; set; } = PollSource.BotNative;
    public List<PollVote> Votes { get; set; } = new();
}

/// <summary>
/// Where the poll originates from.
/// </summary>
public enum PollSource
{
    BotNative = 0,
    TwitchNative = 1
}

/// <summary>
/// A single user's vote in a poll. One vote per user per poll.
/// </summary>
public class PollVote
{
    public int Id { get; set; }
    public int PollId { get; set; }
    public Poll Poll { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Zero-based index into Poll.Options.</summary>
    public int OptionIndex { get; set; }
}

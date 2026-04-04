using System;
using System.Collections.Generic;

namespace Wrkzg.Core.Models;

/// <summary>
/// A poll — either bot-native (chat-based) or Twitch-native (Helix API).
/// </summary>
public class Poll
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>The poll question displayed to viewers.</summary>
    public string Question { get; set; } = string.Empty;

    /// <summary>Poll options stored as JSON array in SQLite.</summary>
    public string[] Options { get; set; } = Array.Empty<string>();

    /// <summary>Whether the poll is currently accepting votes.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>When the poll will automatically close.</summary>
    public DateTimeOffset EndsAt { get; set; }

    /// <summary>When the poll was created.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Whether this poll was created via the bot or via Twitch's native poll system.</summary>
    public PollSource Source { get; set; } = PollSource.BotNative;

    /// <summary>All votes cast in this poll.</summary>
    public List<PollVote> Votes { get; set; } = new();

    /// <summary>Duration in seconds. Used to calculate EndsAt and for display.</summary>
    public int DurationSeconds { get; set; } = 60;

    /// <summary>Twitch Poll ID (only for TwitchNative polls). Null for BotNative.</summary>
    public string? TwitchPollId { get; set; }

    /// <summary>Who created the poll (username for display).</summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>How the poll ended.</summary>
    public PollEndReason EndReason { get; set; } = PollEndReason.NotEnded;
}

/// <summary>
/// Where the poll originates from.
/// </summary>
public enum PollSource
{
    /// <summary>Poll created and managed by the bot via chat commands.</summary>
    BotNative = 0,

    /// <summary>Poll created via Twitch's native poll API (Helix).</summary>
    TwitchNative = 1
}

/// <summary>
/// How a poll was ended.
/// </summary>
public enum PollEndReason
{
    /// <summary>Poll is still active.</summary>
    NotEnded = 0,

    /// <summary>Poll ended because the timer expired.</summary>
    TimerExpired = 1,

    /// <summary>Poll was manually closed by a moderator or the broadcaster.</summary>
    ManuallyClosed = 2,

    /// <summary>Poll was cancelled without counting results.</summary>
    Cancelled = 3
}

/// <summary>
/// A single user's vote in a poll. One vote per user per poll.
/// </summary>
public class PollVote
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Foreign key to the poll this vote belongs to.</summary>
    public int PollId { get; set; }

    /// <summary>Navigation property to the parent poll.</summary>
    public Poll Poll { get; set; } = null!;

    /// <summary>Foreign key to the user who cast this vote.</summary>
    public int UserId { get; set; }

    /// <summary>Navigation property to the voting user.</summary>
    public User User { get; set; } = null!;

    /// <summary>Zero-based index into Poll.Options.</summary>
    public int OptionIndex { get; set; }
}

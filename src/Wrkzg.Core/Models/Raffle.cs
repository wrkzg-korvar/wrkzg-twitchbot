using System;
using System.Collections.Generic;

namespace Wrkzg.Core.Models;

/// <summary>
/// A chat raffle/giveaway. Only one can be open at a time.
/// </summary>
public class Raffle
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsOpen { get; set; } = true;
    public int? WinnerId { get; set; }
    public User? Winner { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ClosedAt { get; set; }
    public List<RaffleEntry> Entries { get; set; } = new();

    /// <summary>Keyword users type in chat to enter. Null = use !join instead.</summary>
    public string? Keyword { get; set; }

    /// <summary>Who created the raffle (display name).</summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>How the raffle ended.</summary>
    public RaffleEndReason EndReason { get; set; } = RaffleEndReason.NotEnded;

    /// <summary>Optional max participant count. Null = unlimited.</summary>
    public int? MaxEntries { get; set; }

    /// <summary>Optional duration in seconds. Null = manual close via !draw.</summary>
    public int? DurationSeconds { get; set; }

    /// <summary>When entries close (calculated from DurationSeconds). Null = no timer.</summary>
    public DateTimeOffset? EntriesCloseAt { get; set; }

    /// <summary>All draw attempts for this raffle.</summary>
    public List<RaffleDraw> Draws { get; set; } = new();

    /// <summary>The currently pending (unverified) winner user ID. Null when no draw is pending.</summary>
    public int? PendingWinnerId { get; set; }
    public User? PendingWinner { get; set; }
}

/// <summary>How a raffle was ended.</summary>
public enum RaffleEndReason
{
    NotEnded = 0,
    Drawn = 1,
    Cancelled = 2
}

/// <summary>
/// A user's entry in a raffle. TicketCount determines weighted probability.
/// </summary>
public class RaffleEntry
{
    public int Id { get; set; }
    public int RaffleId { get; set; }
    public Raffle Raffle { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int TicketCount { get; set; } = 1;
}

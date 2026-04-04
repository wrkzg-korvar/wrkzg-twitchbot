using System;
using System.Collections.Generic;

namespace Wrkzg.Core.Models;

/// <summary>
/// A chat raffle/giveaway. Only one can be open at a time.
/// </summary>
public class Raffle
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Display title for the raffle (e.g. "Steam Key Giveaway").</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Whether the raffle is currently accepting entries.</summary>
    public bool IsOpen { get; set; } = true;

    /// <summary>Foreign key to the confirmed winner. Null until a draw is accepted.</summary>
    public int? WinnerId { get; set; }

    /// <summary>Navigation property to the confirmed winner.</summary>
    public User? Winner { get; set; }

    /// <summary>When this raffle was created.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>When the raffle was closed. Null if still open.</summary>
    public DateTimeOffset? ClosedAt { get; set; }

    /// <summary>All participant entries in this raffle.</summary>
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
    /// <summary>Foreign key to the currently pending (unverified) winner. Null when no draw is pending.</summary>
    public int? PendingWinnerId { get; set; }

    /// <summary>Navigation property to the pending winner awaiting confirmation.</summary>
    public User? PendingWinner { get; set; }
}

/// <summary>How a raffle was ended.</summary>
public enum RaffleEndReason
{
    /// <summary>Raffle is still open.</summary>
    NotEnded = 0,

    /// <summary>A winner was drawn and confirmed.</summary>
    Drawn = 1,

    /// <summary>Raffle was cancelled without selecting a winner.</summary>
    Cancelled = 2
}

/// <summary>
/// A user's entry in a raffle. TicketCount determines weighted probability.
/// </summary>
public class RaffleEntry
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Foreign key to the raffle this entry belongs to.</summary>
    public int RaffleId { get; set; }

    /// <summary>Navigation property to the parent raffle.</summary>
    public Raffle Raffle { get; set; } = null!;

    /// <summary>Foreign key to the participating user.</summary>
    public int UserId { get; set; }

    /// <summary>Navigation property to the participating user.</summary>
    public User User { get; set; } = null!;

    /// <summary>Number of tickets for weighted probability. Default is 1.</summary>
    public int TicketCount { get; set; } = 1;
}

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

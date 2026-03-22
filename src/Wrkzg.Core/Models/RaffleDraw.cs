using System;

namespace Wrkzg.Core.Models;

/// <summary>
/// Tracks each draw attempt in a raffle.
/// A raffle can have multiple draws if the streamer redraws.
/// </summary>
public class RaffleDraw
{
    public int Id { get; set; }
    public int RaffleId { get; set; }
    public Raffle Raffle { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Sequential draw number (1 = first draw, 2 = redraw, etc.)</summary>
    public int DrawNumber { get; set; }

    /// <summary>Whether this draw was accepted as the final winner.</summary>
    public bool IsAccepted { get; set; }

    /// <summary>Reason for redraw if not accepted.</summary>
    public string? RedrawReason { get; set; }

    public DateTimeOffset DrawnAt { get; set; } = DateTimeOffset.UtcNow;
}

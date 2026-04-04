using System;

namespace Wrkzg.Core.Models;

/// <summary>
/// Tracks each draw attempt in a raffle.
/// A raffle can have multiple draws if the streamer redraws.
/// </summary>
public class RaffleDraw
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Foreign key to the raffle this draw belongs to.</summary>
    public int RaffleId { get; set; }

    /// <summary>Navigation property to the parent raffle.</summary>
    public Raffle Raffle { get; set; } = null!;

    /// <summary>Foreign key to the user who was drawn.</summary>
    public int UserId { get; set; }

    /// <summary>Navigation property to the drawn user.</summary>
    public User User { get; set; } = null!;

    /// <summary>Sequential draw number (1 = first draw, 2 = redraw, etc.)</summary>
    public int DrawNumber { get; set; }

    /// <summary>Whether this draw was accepted as the final winner.</summary>
    public bool IsAccepted { get; set; }

    /// <summary>Reason for redraw if not accepted.</summary>
    public string? RedrawReason { get; set; }

    /// <summary>When this draw was performed.</summary>
    public DateTimeOffset DrawnAt { get; set; } = DateTimeOffset.UtcNow;
}

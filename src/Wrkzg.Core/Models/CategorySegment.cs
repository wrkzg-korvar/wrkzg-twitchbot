using System;

namespace Wrkzg.Core.Models;

/// <summary>
/// A time span during which the streamer played a specific game/category.
/// Multiple segments per StreamSession (one per category switch).
/// </summary>
public class CategorySegment
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Foreign key to the parent stream session.</summary>
    public int StreamSessionId { get; set; }

    /// <summary>Navigation property to the parent stream session.</summary>
    public StreamSession StreamSession { get; set; } = null!;

    /// <summary>Twitch Category/Game ID.</summary>
    public string? TwitchCategoryId { get; set; }

    /// <summary>Category/Game display name.</summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>When the streamer switched to this category.</summary>
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>When the streamer switched away from this category, or null if still active.</summary>
    public DateTimeOffset? EndedAt { get; set; }

    /// <summary>Duration in minutes. Calculated on close.</summary>
    public int? DurationMinutes { get; set; }

    /// <summary>Average viewers during this category segment.</summary>
    public double? AverageViewers { get; set; }

    /// <summary>Peak viewers during this category segment.</summary>
    public int? PeakViewers { get; set; }
}

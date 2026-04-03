using System;

namespace Wrkzg.Core.Models;

/// <summary>
/// A time span during which the streamer played a specific game/category.
/// Multiple segments per StreamSession (one per category switch).
/// </summary>
public class CategorySegment
{
    public int Id { get; set; }
    public int StreamSessionId { get; set; }
    public StreamSession StreamSession { get; set; } = null!;

    /// <summary>Twitch Category/Game ID.</summary>
    public string? TwitchCategoryId { get; set; }

    /// <summary>Category/Game display name.</summary>
    public string CategoryName { get; set; } = string.Empty;

    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }

    /// <summary>Duration in minutes. Calculated on close.</summary>
    public int? DurationMinutes { get; set; }

    /// <summary>Average viewers during this category segment.</summary>
    public double? AverageViewers { get; set; }

    /// <summary>Peak viewers during this category segment.</summary>
    public int? PeakViewers { get; set; }
}

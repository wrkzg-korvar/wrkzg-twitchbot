using System;
using System.Collections.Generic;

namespace Wrkzg.Core.Models;

/// <summary>
/// Represents a single stream session from go-live to going offline.
/// </summary>
public class StreamSession
{
    public int Id { get; set; }

    /// <summary>Twitch Stream ID (unique per stream).</summary>
    public string? TwitchStreamId { get; set; }

    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }

    /// <summary>Duration in minutes. Calculated on close.</summary>
    public int? DurationMinutes { get; set; }

    /// <summary>Peak concurrent viewers during this session.</summary>
    public int PeakViewers { get; set; }

    /// <summary>Average viewers (calculated on close from snapshots).</summary>
    public double? AverageViewers { get; set; }

    /// <summary>Total unique chatters during this session.</summary>
    public int? UniqueChatters { get; set; }

    /// <summary>Total chat messages during this session.</summary>
    public int? TotalMessages { get; set; }

    /// <summary>New followers gained during this session.</summary>
    public int? NewFollowers { get; set; }

    /// <summary>New subscribers gained during this session.</summary>
    public int? NewSubscribers { get; set; }

    /// <summary>Title at stream start.</summary>
    public string? Title { get; set; }

    /// <summary>Category segments within this session.</summary>
    public List<CategorySegment> CategorySegments { get; set; } = new();

    /// <summary>Viewer snapshots for chart rendering.</summary>
    public List<ViewerSnapshot> ViewerSnapshots { get; set; } = new();
}

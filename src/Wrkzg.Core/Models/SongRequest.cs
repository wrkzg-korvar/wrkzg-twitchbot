using System;

namespace Wrkzg.Core.Models;

/// <summary>
/// A song request in the queue. Tracks YouTube video metadata,
/// who requested it, and its playback status.
/// </summary>
public class SongRequest
{
    public int Id { get; set; }

    /// <summary>YouTube video ID (e.g. "dQw4w9WgXcQ").</summary>
    public string VideoId { get; set; } = string.Empty;

    /// <summary>Video title from YouTube oEmbed.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Thumbnail URL from YouTube oEmbed.</summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>Video duration in seconds.</summary>
    public int DurationSeconds { get; set; }

    /// <summary>Twitch display name of the requester.</summary>
    public string RequestedBy { get; set; } = string.Empty;

    /// <summary>Points spent for this request (null = free).</summary>
    public int? PointsCost { get; set; }

    /// <summary>Current status in the queue.</summary>
    public SongRequestStatus Status { get; set; } = SongRequestStatus.Queued;

    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PlayedAt { get; set; }
}

public enum SongRequestStatus
{
    Queued = 0,
    Playing = 1,
    Played = 2,
    Skipped = 3
}

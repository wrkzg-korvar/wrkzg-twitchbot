using System;

namespace Wrkzg.Core.Models;

/// <summary>
/// A song request in the queue. Tracks YouTube video metadata,
/// who requested it, and its playback status.
/// </summary>
public class SongRequest
{
    /// <summary>Primary key.</summary>
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

    /// <summary>When the song was requested.</summary>
    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>When the song started playing. Null if not yet played.</summary>
    public DateTimeOffset? PlayedAt { get; set; }
}

/// <summary>
/// Lifecycle status of a song request in the playback queue.
/// </summary>
public enum SongRequestStatus
{
    /// <summary>Waiting in the queue to be played.</summary>
    Queued = 0,

    /// <summary>Currently playing.</summary>
    Playing = 1,

    /// <summary>Finished playing successfully.</summary>
    Played = 2,

    /// <summary>Skipped by a moderator or the broadcaster.</summary>
    Skipped = 3
}

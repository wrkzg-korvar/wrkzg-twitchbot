using System;

namespace Wrkzg.Core.Models;

/// <summary>
/// A point-in-time viewer count measurement.
/// One per minute while stream is live. Used for time-series charts.
/// </summary>
public class ViewerSnapshot
{
    public int Id { get; set; }
    public int StreamSessionId { get; set; }

    /// <summary>Viewer count at this moment.</summary>
    public int ViewerCount { get; set; }

    /// <summary>Exact timestamp of the measurement.</summary>
    public DateTimeOffset Timestamp { get; set; }
}

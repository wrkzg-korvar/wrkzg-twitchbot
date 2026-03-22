using System;

namespace Wrkzg.Core.Models;

/// <summary>
/// A recurring timed message that the bot sends at specified intervals.
/// Multiple messages per timer are cycled through (round-robin).
/// </summary>
public class TimedMessage
{
    public int Id { get; set; }

    /// <summary>Display name for the timer (shown in dashboard).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Messages to cycle through. Stored as JSON array in SQLite.</summary>
    public string[] Messages { get; set; } = Array.Empty<string>();

    /// <summary>Index of the next message to send (cycles through Messages array).</summary>
    public int NextMessageIndex { get; set; }

    /// <summary>Interval in minutes between messages.</summary>
    public int IntervalMinutes { get; set; } = 10;

    /// <summary>Minimum chat messages since last fire. 0 = no minimum.</summary>
    public int MinChatLines { get; set; } = 5;

    /// <summary>Whether this timer is active.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Whether this timer fires when the stream is online.</summary>
    public bool RunWhenOnline { get; set; } = true;

    /// <summary>Whether this timer fires when the stream is offline.</summary>
    public bool RunWhenOffline { get; set; }

    /// <summary>When this timer last fired.</summary>
    public DateTimeOffset? LastFiredAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

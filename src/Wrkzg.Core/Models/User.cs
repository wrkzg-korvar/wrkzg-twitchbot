// ─────────────────────────────────────────────────────────
// FILE: src/Wrkzg.Core/Models/User.cs
// ─────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;

namespace Wrkzg.Core.Models;

/// <summary>
/// Represents a tracked Twitch viewer. Created on first chat message
/// or first EventSub event, then updated continuously while the stream is live.
/// </summary>
public class User
{
    public int Id { get; set; }
    public string TwitchId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public long Points { get; set; }
    public int WatchedMinutes { get; set; }
    public int MessageCount { get; set; }
    public DateTimeOffset? FollowDate { get; set; }
    public bool IsSubscriber { get; set; }

    /// <summary>0 = no sub, 1/2/3 = tier</summary>
    public int SubscriberTier { get; set; }

    public bool IsBanned { get; set; }
    public bool IsMod { get; set; }
    public DateTimeOffset FirstSeenAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public List<RaffleEntry> RaffleEntries { get; set; } = new();
    public List<PollVote> PollVotes { get; set; } = new();
}

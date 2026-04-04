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
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Twitch user ID (numeric string from Twitch API).</summary>
    public string TwitchId { get; set; } = string.Empty;

    /// <summary>Twitch login name (lowercase, unique).</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Twitch display name (may contain casing and unicode characters).</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Accumulated loyalty points earned by watching and chatting.</summary>
    public long Points { get; set; }

    /// <summary>Total minutes this user has been present in the stream.</summary>
    public int WatchedMinutes { get; set; }

    /// <summary>Total chat messages sent by this user.</summary>
    public int MessageCount { get; set; }

    /// <summary>When the user followed the channel. Null if not a follower.</summary>
    public DateTimeOffset? FollowDate { get; set; }

    /// <summary>Whether the user is currently subscribed to the channel.</summary>
    public bool IsSubscriber { get; set; }

    /// <summary>0 = no sub, 1/2/3 = tier</summary>
    public int SubscriberTier { get; set; }

    /// <summary>Whether this user is banned from the bot (excluded from points, commands, etc.).</summary>
    public bool IsBanned { get; set; }

    /// <summary>Whether this user is a moderator in the channel.</summary>
    public bool IsMod { get; set; }

    /// <summary>Whether this user is the broadcaster (channel owner).</summary>
    public bool IsBroadcaster { get; set; }

    /// <summary>When the user was first seen in chat or via EventSub.</summary>
    public DateTimeOffset FirstSeenAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>When the user was last active in chat or detected via EventSub.</summary>
    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Raffle entries this user has participated in.</summary>
    public List<RaffleEntry> RaffleEntries { get; set; } = new();

    /// <summary>Poll votes this user has cast.</summary>
    public List<PollVote> PollVotes { get; set; } = new();

    /// <summary>Roles assigned to this user.</summary>
    public List<UserRole> UserRoles { get; set; } = new();
}

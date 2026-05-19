using System;

namespace Wrkzg.Core.Models;

/// <summary>
/// Immutable log entry for a moderation action or significant user event.
/// Append-only — events are never updated or deleted (except bulk cleanup).
/// </summary>
public class ModerationEvent
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>The Twitch user ID this event relates to.</summary>
    public string TwitchUserId { get; set; } = string.Empty;

    /// <summary>Display name of the target user at the time of the event.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Event type.</summary>
    public ModerationEventType EventType { get; set; }

    /// <summary>Who initiated the action (bot name, broadcaster name, or "system" for EventSub events).</summary>
    public string Actor { get; set; } = string.Empty;

    /// <summary>Optional reason or additional detail (e.g. timeout reason, sub tier).</summary>
    public string? Reason { get; set; }

    /// <summary>Duration in seconds (for Timeout events). Null for all other event types.</summary>
    public int? DurationSeconds { get; set; }

    /// <summary>Whether the Twitch API call succeeded. Null for non-Twitch events (follow, sub).</summary>
    public bool? TwitchSuccess { get; set; }

    /// <summary>When the event occurred.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Types of events logged in the ModerationEvent table.
/// Numeric values are grouped by category for clarity and future extensibility.
/// </summary>
public enum ModerationEventType
{
    // ─── Twitch Moderation (via Helix API) ───
    TwitchTimeout = 10,
    TwitchBan = 11,
    TwitchUnban = 12,
    TwitchShoutout = 13,

    // ─── Bot Internal ───
    BotBan = 20,
    BotUnban = 21,

    // ─── User Events (from EventSub) ───
    Follow = 30,
    Subscribe = 31,
    GiftSub = 32,
    Resub = 33,
    Raid = 34,
}

using System;

namespace Wrkzg.Core.Models;

/// <summary>
/// A custom chat command (e.g. !discord, !socials).
/// </summary>
public class Command
{
    public int Id { get; set; }

    /// <summary>Chat trigger including the ! prefix (e.g. "!discord"). Unique.</summary>
    public string Trigger { get; set; } = string.Empty;

    /// <summary>Alternative triggers stored as JSON array in SQLite.</summary>
    public string[] Aliases { get; set; } = Array.Empty<string>();

    /// <summary>Response template with variables: {user}, {count}, {uptime}, {random:min:max}.</summary>
    public string ResponseTemplate { get; set; } = string.Empty;

    public PermissionLevel PermissionLevel { get; set; } = PermissionLevel.Everyone;
    public int GlobalCooldownSeconds { get; set; }
    public int UserCooldownSeconds { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int UseCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Permission levels for chat commands, ordered from least to most restrictive.
/// </summary>
public enum PermissionLevel
{
    Everyone = 0,
    Follower = 1,
    Subscriber = 2,
    Moderator = 3,
    Broadcaster = 4
}

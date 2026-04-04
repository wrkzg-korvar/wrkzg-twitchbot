using System;

namespace Wrkzg.Core.Models;

/// <summary>
/// A custom chat command (e.g. !discord, !socials).
/// </summary>
public class Command
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Chat trigger including the ! prefix (e.g. "!discord"). Unique.</summary>
    public string Trigger { get; set; } = string.Empty;

    /// <summary>Alternative triggers stored as JSON array in SQLite.</summary>
    public string[] Aliases { get; set; } = Array.Empty<string>();

    /// <summary>Response template with variables: {user}, {count}, {uptime}, {random:min:max}.</summary>
    public string ResponseTemplate { get; set; } = string.Empty;

    /// <summary>Minimum permission level required to use this command.</summary>
    public PermissionLevel PermissionLevel { get; set; } = PermissionLevel.Everyone;

    /// <summary>Cooldown in seconds that applies to all users globally.</summary>
    public int GlobalCooldownSeconds { get; set; }

    /// <summary>Cooldown in seconds that applies per individual user.</summary>
    public int UserCooldownSeconds { get; set; }

    /// <summary>Whether this command is active and responds to triggers.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Total number of times this command has been invoked.</summary>
    public int UseCount { get; set; }

    /// <summary>When this command was created.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Permission levels for chat commands, ordered from least to most restrictive.
/// </summary>
public enum PermissionLevel
{
    /// <summary>Any viewer can use the command.</summary>
    Everyone = 0,

    /// <summary>Only followers and above can use the command.</summary>
    Follower = 1,

    /// <summary>Only subscribers and above can use the command.</summary>
    Subscriber = 2,

    /// <summary>Only moderators and the broadcaster can use the command.</summary>
    Moderator = 3,

    /// <summary>Only the broadcaster (channel owner) can use the command.</summary>
    Broadcaster = 4
}

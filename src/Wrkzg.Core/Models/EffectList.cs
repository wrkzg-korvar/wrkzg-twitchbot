using System;

namespace Wrkzg.Core.Models;

/// <summary>
/// A configured automation: Trigger → Conditions → Effect Chain.
/// All config is stored as JSON strings for flexibility.
/// </summary>
public class EffectList
{
    public int Id { get; set; }

    /// <summary>User-defined name (e.g. "Welcome VIPs", "Death Counter Combo").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Whether this automation is active.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Trigger type ID (e.g. "command", "event.follow", "hotkey").</summary>
    public string TriggerTypeId { get; set; } = string.Empty;

    /// <summary>Trigger configuration as JSON (e.g. {"trigger": "!welcome"}).</summary>
    public string TriggerConfig { get; set; } = "{}";

    /// <summary>Conditions as JSON array (e.g. [{"type":"role_check","params":{"min_priority":"5"}}]).</summary>
    public string ConditionsConfig { get; set; } = "[]";

    /// <summary>Effects as JSON array (e.g. [{"type":"chat_message","params":{"message":"Hello {user}!"}}]).</summary>
    public string EffectsConfig { get; set; } = "[]";

    /// <summary>Cooldown in seconds between activations. 0 = no cooldown.</summary>
    public int Cooldown { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

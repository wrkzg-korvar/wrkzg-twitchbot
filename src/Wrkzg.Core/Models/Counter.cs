using System;

namespace Wrkzg.Core.Models;

/// <summary>
/// A simple named counter that can be incremented/decremented via chat commands.
/// Useful for tracking deaths, wins, fails, etc.
/// </summary>
public class Counter
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Display name (e.g. "Deaths", "Wins").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Current value.</summary>
    public int Value { get; set; }

    /// <summary>Chat command trigger. Auto-generated from name: "Deaths" -> "!deaths".</summary>
    public string Trigger { get; set; } = string.Empty;

    /// <summary>Response template when a viewer checks the counter.</summary>
    public string ResponseTemplate { get; set; } = "{name}: {value}";

    /// <summary>When this counter was created.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

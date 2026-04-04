using System;

namespace Wrkzg.Core.Models;

/// <summary>
/// Maps a keyboard shortcut to a bot action.
/// Global hotkeys work even when Wrkzg is not in focus.
/// Compatible with Stream Deck's "Hotkey" action.
/// </summary>
public class HotkeyBinding
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Key combination string (e.g. "Ctrl+Shift+F1").</summary>
    public string KeyCombination { get; set; } = string.Empty;

    /// <summary>
    /// Action type: ChatMessage, CounterIncrement, CounterDecrement, CounterReset
    /// </summary>
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// Action payload — depends on ActionType:
    /// - ChatMessage: the message to send
    /// - CounterIncrement/Decrement/Reset: the counter ID
    /// </summary>
    public string ActionPayload { get; set; } = string.Empty;

    /// <summary>User-facing description (e.g. "Increment death counter").</summary>
    public string? Description { get; set; }

    /// <summary>Whether this hotkey binding is active.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>When this hotkey binding was created.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

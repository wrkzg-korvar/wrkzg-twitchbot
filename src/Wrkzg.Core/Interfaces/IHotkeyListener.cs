using System;
using System.Threading;
using System.Threading.Tasks;

namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Platform-specific global hotkey listener.
/// Registers system-wide keyboard shortcuts.
/// </summary>
public interface IHotkeyListener
{
    /// <summary>Start listening for registered hotkeys.</summary>
    Task StartListeningAsync(CancellationToken ct = default);

    /// <summary>Stop listening and unregister all hotkeys.</summary>
    Task StopListeningAsync(CancellationToken ct = default);

    /// <summary>Register a hotkey combination. Returns true if successful.</summary>
    bool RegisterHotkey(int id, string keyCombination);

    /// <summary>Unregister a previously registered hotkey.</summary>
    void UnregisterHotkey(int id);

    /// <summary>Unregister all hotkeys.</summary>
    void UnregisterAll();

    /// <summary>Fired when a registered hotkey is pressed. Parameter is the binding ID.</summary>
    event Action<int>? OnHotkeyPressed;

    /// <summary>Whether global hotkeys are supported on this platform.</summary>
    bool IsGlobalHotkeySupported { get; }

    /// <summary>Whether the app has the required OS permissions for global hotkeys.</summary>
    bool HasPermission { get; }

    /// <summary>Requests the required OS permissions (shows system dialog if needed).</summary>
    void RequestPermission();
}

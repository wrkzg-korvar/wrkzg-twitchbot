using System;
using System.Threading;
using System.Threading.Tasks;
using Wrkzg.Core.Interfaces;

namespace Wrkzg.Infrastructure.Hotkeys;

/// <summary>
/// No-op hotkey listener for unsupported platforms.
/// Hotkeys can still be triggered via the API (Stream Deck HTTP action).
/// </summary>
public class NoOpHotkeyListener : IHotkeyListener
{
    /// <summary>Raised when a hotkey press is simulated via <see cref="SimulateHotkeyPress"/>.</summary>
    public event Action<int>? OnHotkeyPressed;

    /// <summary>Gets whether global hotkey interception is supported. Always false for unsupported platforms.</summary>
    public bool IsGlobalHotkeySupported => false;

    /// <summary>Gets whether permission is granted. Always true since no global hotkeys are intercepted.</summary>
    public bool HasPermission => true; // No permission needed since no global hotkeys

    /// <summary>No-op. Returns immediately on unsupported platforms.</summary>
    public Task StartListeningAsync(CancellationToken ct = default) => Task.CompletedTask;

    /// <summary>No-op. Returns immediately on unsupported platforms.</summary>
    public Task StopListeningAsync(CancellationToken ct = default) => Task.CompletedTask;

    /// <summary>No-op. Always returns true since hotkeys can still be triggered via the API.</summary>
    public bool RegisterHotkey(int id, string keyCombination) => true;

    /// <summary>No-op on unsupported platforms.</summary>
    public void UnregisterHotkey(int id) { }

    /// <summary>No-op on unsupported platforms.</summary>
    public void UnregisterAll() { }

    /// <summary>No-op on unsupported platforms.</summary>
    public void RequestPermission() { }

    /// <summary>Simulates a hotkey press by directly invoking the callback for the given binding.</summary>
    public void SimulateHotkeyPress(int id) => OnHotkeyPressed?.Invoke(id);
}

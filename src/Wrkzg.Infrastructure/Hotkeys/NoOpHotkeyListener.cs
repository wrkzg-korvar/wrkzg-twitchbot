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
    public event Action<int>? OnHotkeyPressed;

    public bool IsGlobalHotkeySupported => false;
    public bool HasPermission => true; // No permission needed since no global hotkeys

    public Task StartListeningAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task StopListeningAsync(CancellationToken ct = default) => Task.CompletedTask;
    public bool RegisterHotkey(int id, string keyCombination) => true;
    public void UnregisterHotkey(int id) { }
    public void UnregisterAll() { }
    public void RequestPermission() { }

    public void SimulateHotkeyPress(int id) => OnHotkeyPressed?.Invoke(id);
}

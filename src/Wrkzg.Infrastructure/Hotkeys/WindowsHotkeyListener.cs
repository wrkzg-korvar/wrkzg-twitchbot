using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Interfaces;

namespace Wrkzg.Infrastructure.Hotkeys;

/// <summary>
/// Windows-specific global hotkey listener using RegisterHotKey/UnregisterHotKey (User32.dll).
/// Requires a message pump (Photino provides this via the UI thread).
/// </summary>
[SupportedOSPlatform("windows")]
public class WindowsHotkeyListener : IHotkeyListener
{
    private readonly ILogger<WindowsHotkeyListener> _logger;
    private readonly Dictionary<int, string> _registeredKeys = new();

    public event Action<int>? OnHotkeyPressed;

    public bool IsGlobalHotkeySupported => true;
    public bool HasPermission => true; // Windows doesn't need special permissions

    public void RequestPermission() { } // No-op on Windows

    public WindowsHotkeyListener(ILogger<WindowsHotkeyListener> logger)
    {
        _logger = logger;
    }

    public Task StartListeningAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Windows hotkey listener started");
        return Task.CompletedTask;
    }

    public Task StopListeningAsync(CancellationToken ct = default)
    {
        UnregisterAll();
        _logger.LogInformation("Windows hotkey listener stopped");
        return Task.CompletedTask;
    }

    public bool RegisterHotkey(int id, string keyCombination)
    {
        try
        {
            (uint modifiers, uint vk) = ParseKeyCombination(keyCombination);
            if (vk == 0)
            {
                _logger.LogWarning("Could not parse key combination: {Keys}", keyCombination);
                return false;
            }

            bool result = RegisterHotKey(IntPtr.Zero, id, modifiers, vk);
            if (result)
            {
                _registeredKeys[id] = keyCombination;
                _logger.LogInformation("Registered hotkey {Id}: {Keys}", id, keyCombination);
            }
            else
            {
                _logger.LogWarning("Failed to register hotkey {Id}: {Keys}", id, keyCombination);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering hotkey {Id}", id);
            return false;
        }
    }

    public void UnregisterHotkey(int id)
    {
        UnregisterHotKey(IntPtr.Zero, id);
        _registeredKeys.Remove(id);
    }

    public void UnregisterAll()
    {
        foreach (int id in _registeredKeys.Keys)
        {
            UnregisterHotKey(IntPtr.Zero, id);
        }
        _registeredKeys.Clear();
    }

    /// <summary>Simulates a hotkey press for testing via the API.</summary>
    public void SimulateHotkeyPress(int id)
    {
        OnHotkeyPressed?.Invoke(id);
    }

    private static (uint modifiers, uint vk) ParseKeyCombination(string combination)
    {
        uint modifiers = 0;
        uint vk = 0;

        string[] parts = combination.Split('+');
        foreach (string part in parts)
        {
            string key = part.Trim().ToUpperInvariant();
            switch (key)
            {
                case "CTRL": case "CONTROL": modifiers |= 0x0002; break;
                case "ALT": modifiers |= 0x0001; break;
                case "SHIFT": modifiers |= 0x0004; break;
                case "WIN": modifiers |= 0x0008; break;
                default:
                    // Try to parse as virtual key code
                    if (key.StartsWith("F") && int.TryParse(key[1..], out int fNum) && fNum >= 1 && fNum <= 24)
                    {
                        vk = (uint)(0x70 + fNum - 1); // VK_F1 = 0x70
                    }
                    else if (key.Length == 1 && char.IsLetterOrDigit(key[0]))
                    {
                        vk = (uint)key[0]; // ASCII = VK for A-Z, 0-9
                    }
                    break;
            }
        }

        return (modifiers, vk);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}

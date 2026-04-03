using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Interfaces;

namespace Wrkzg.Infrastructure.Hotkeys;

/// <summary>
/// macOS hotkey listener. Checks for Accessibility permissions on startup
/// and can open the System Settings to grant them.
/// </summary>
[SupportedOSPlatform("macos")]
public class MacOsHotkeyListener : IHotkeyListener
{
    private readonly ILogger<MacOsHotkeyListener> _logger;
    private readonly Dictionary<int, string> _registeredKeys = new();

    public event Action<int>? OnHotkeyPressed;

    public bool IsGlobalHotkeySupported => true;
    public bool HasPermission => _hasPermission;
    private bool _hasPermission;

    public MacOsHotkeyListener(ILogger<MacOsHotkeyListener> logger)
    {
        _logger = logger;
    }

    public Task StartListeningAsync(CancellationToken ct = default)
    {
        _hasPermission = AXIsProcessTrusted();
        if (_hasPermission)
        {
            _logger.LogInformation("macOS Accessibility permission granted");
        }
        else
        {
            _logger.LogWarning(
                "macOS Accessibility permission not granted. " +
                "Grant in: System Settings > Privacy & Security > Accessibility");
        }

        return Task.CompletedTask;
    }

    public Task StopListeningAsync(CancellationToken ct = default)
    {
        UnregisterAll();
        return Task.CompletedTask;
    }

    public bool RegisterHotkey(int id, string keyCombination)
    {
        _registeredKeys[id] = keyCombination;
        return true;
    }

    public void UnregisterHotkey(int id)
    {
        _registeredKeys.Remove(id);
    }

    public void UnregisterAll()
    {
        _registeredKeys.Clear();
    }

    public void SimulateHotkeyPress(int id)
    {
        OnHotkeyPressed?.Invoke(id);
    }

    public void RequestPermission()
    {
        try
        {
            // Open the Accessibility preferences pane directly
            Process.Start(new ProcessStartInfo
            {
                FileName = "open",
                ArgumentList = { "x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility" },
                UseShellExecute = false
            });
            _logger.LogInformation("Opened macOS Accessibility settings");

            // Re-check after a short delay
            _hasPermission = AXIsProcessTrusted();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to open macOS Accessibility settings");
        }
    }

    /// <summary>
    /// Simple check if the process is trusted for Accessibility.
    /// Uses AXIsProcessTrusted() which returns the current state without prompting.
    /// </summary>
    [DllImport("/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices")]
    private static extern bool AXIsProcessTrusted();
}

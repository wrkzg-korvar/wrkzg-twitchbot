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
/// macOS hotkey listener using CGEventTap to intercept global key events.
/// Requires Accessibility permissions (System Settings &gt; Privacy &amp; Security &gt; Accessibility).
/// </summary>
[SupportedOSPlatform("macos")]
public class MacOsHotkeyListener : IHotkeyListener
{
    private readonly ILogger<MacOsHotkeyListener> _logger;
    private readonly Dictionary<int, (string combination, ulong modifierMask, ushort keyCode)> _registeredKeys = new();
    private readonly object _lock = new();

    private IntPtr _eventTap;
    private IntPtr _runLoopSource;
    private IntPtr _tapRunLoop;
    private Thread? _tapThread;
    private volatile bool _running;

    /// <summary>Raised when a registered hotkey combination is pressed.</summary>
    public event Action<int>? OnHotkeyPressed;

    /// <summary>Gets whether global hotkey interception is supported on this platform.</summary>
    public bool IsGlobalHotkeySupported => true;

    /// <summary>Gets whether Accessibility permission has been granted to this application.</summary>
    public bool HasPermission => _hasPermission;
    private bool _hasPermission;

    // Store the delegate as a field to prevent GC collection
    private readonly CGEventTapCallBack _callbackDelegate;

    /// <summary>
    /// Initializes a new instance of the <see cref="MacOsHotkeyListener"/> class.
    /// </summary>
    /// <param name="logger">The logger for hotkey diagnostics.</param>
    public MacOsHotkeyListener(ILogger<MacOsHotkeyListener> logger)
    {
        _logger = logger;
        _callbackDelegate = EventTapCallback;
    }

    /// <summary>Starts the CGEventTap for intercepting global key events. Requires Accessibility permission.</summary>
    public Task StartListeningAsync(CancellationToken ct = default)
    {
        _hasPermission = AXIsProcessTrusted();
        if (!_hasPermission)
        {
            _logger.LogWarning(
                "macOS Accessibility permission not granted. " +
                "Grant in: System Settings > Privacy & Security > Accessibility. " +
                "Global hotkeys will not work until permission is granted.");
            return Task.CompletedTask;
        }

        _logger.LogInformation("macOS Accessibility permission granted — starting event tap");
        StartEventTap();
        return Task.CompletedTask;
    }

    /// <summary>Stops the CGEventTap and unregisters all hotkeys.</summary>
    public Task StopListeningAsync(CancellationToken ct = default)
    {
        StopEventTap();
        UnregisterAll();
        return Task.CompletedTask;
    }

    /// <summary>Registers a hotkey combination to be monitored by the event tap.</summary>
    public bool RegisterHotkey(int id, string keyCombination)
    {
        (ulong modifierMask, ushort keyCode) = ParseKeyCombination(keyCombination);
        if (keyCode == 0xFFFF)
        {
            _logger.LogWarning("Could not parse key combination: {Keys}", keyCombination);
            return false;
        }

        lock (_lock)
        {
            _registeredKeys[id] = (keyCombination, modifierMask, keyCode);
        }

        _logger.LogInformation("Registered hotkey {Id}: {Keys} (keyCode=0x{KeyCode:X2}, mods=0x{Mods:X})",
            id, keyCombination, keyCode, modifierMask);
        return true;
    }

    /// <summary>Unregisters a hotkey by its binding identifier.</summary>
    public void UnregisterHotkey(int id)
    {
        lock (_lock)
        {
            _registeredKeys.Remove(id);
        }
    }

    /// <summary>Unregisters all hotkey bindings.</summary>
    public void UnregisterAll()
    {
        lock (_lock)
        {
            _registeredKeys.Clear();
        }
    }

    /// <summary>Simulates a hotkey press by directly invoking the callback for the given binding.</summary>
    public void SimulateHotkeyPress(int id)
    {
        OnHotkeyPressed?.Invoke(id);
    }

    /// <summary>Opens the macOS Accessibility settings panel so the user can grant permission.</summary>
    public void RequestPermission()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "open",
                ArgumentList = { "x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility" },
                UseShellExecute = false
            });
            _logger.LogInformation("Opened macOS Accessibility settings");
            _hasPermission = AXIsProcessTrusted();
            if (_hasPermission && !_running)
            {
                _logger.LogInformation("Accessibility permission now granted — starting event tap");
                StartEventTap();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to open macOS Accessibility settings");
        }
    }

    private void StartEventTap()
    {
        if (_running)
        {
            return;
        }

        _tapThread = new Thread(EventTapThread)
        {
            IsBackground = true,
            Name = "WrkzgMacOsHotkeyTap"
        };
        _tapThread.Start();
    }

    private void StopEventTap()
    {
        _running = false;

        if (_eventTap != IntPtr.Zero)
        {
            CGEventTapEnable(_eventTap, false);
        }

        if (_tapRunLoop != IntPtr.Zero)
        {
            CFRunLoopStop(_tapRunLoop);
        }

        _tapThread?.Join(TimeSpan.FromSeconds(3));
        _tapThread = null;
    }

    private void EventTapThread()
    {
        try
        {
            _running = true;

            // CGEventMaskBit(kCGEventKeyDown) = 1 << 10 = 1024
            ulong keyDownMask = 1UL << 10;

            // kCGSessionEventTap = 1 (session level, works with Accessibility permission)
            // kCGHeadInsertEventTap = 0
            // kCGEventTapOptionListenOnly = 1
            _eventTap = CGEventTapCreate(
                1,                    // kCGSessionEventTap
                0,                    // kCGHeadInsertEventTap
                1,                    // kCGEventTapOptionListenOnly
                keyDownMask,          // CGEventMaskBit(kCGEventKeyDown)
                _callbackDelegate,
                IntPtr.Zero);

            if (_eventTap == IntPtr.Zero)
            {
                _logger.LogError(
                    "Failed to create CGEventTap — Accessibility permission may not be effective yet. " +
                    "Try restarting the app after granting permission in System Settings > Privacy & Security > Accessibility.");
                _running = false;
                return;
            }

            _logger.LogInformation("CGEventTap created successfully");

            _runLoopSource = CFMachPortCreateRunLoopSource(IntPtr.Zero, _eventTap, 0);
            if (_runLoopSource == IntPtr.Zero)
            {
                _logger.LogError("Failed to create run loop source for event tap");
                CFRelease(_eventTap);
                _eventTap = IntPtr.Zero;
                _running = false;
                return;
            }

            _tapRunLoop = CFRunLoopGetCurrent();

            // Load kCFRunLoopCommonModes symbol
            IntPtr commonModes = LoadCFRunLoopCommonModes();
            CFRunLoopAddSource(_tapRunLoop, _runLoopSource, commonModes);
            CGEventTapEnable(_eventTap, true);

            _logger.LogInformation("macOS global hotkey event tap started — listening for key events");

            // Blocks until CFRunLoopStop is called
            CFRunLoopRun();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "macOS event tap thread crashed");
        }
        finally
        {
            if (_runLoopSource != IntPtr.Zero)
            {
                CFRelease(_runLoopSource);
                _runLoopSource = IntPtr.Zero;
            }
            if (_eventTap != IntPtr.Zero)
            {
                CFRelease(_eventTap);
                _eventTap = IntPtr.Zero;
            }
            _tapRunLoop = IntPtr.Zero;
            _running = false;
            _logger.LogInformation("macOS event tap thread stopped");
        }
    }

    private IntPtr EventTapCallback(IntPtr proxy, uint type, IntPtr eventRef, IntPtr userInfo)
    {
        try
        {
            // CGEventType values that indicate the tap was disabled
            const uint kCGEventTapDisabledByTimeout = 0xFFFFFFFE;
            const uint kCGEventTapDisabledByUserInput = 0xFFFFFFFF;
            const uint kCGEventKeyDown = 10;

            if (type == kCGEventTapDisabledByTimeout || type == kCGEventTapDisabledByUserInput)
            {
                if (_eventTap != IntPtr.Zero && _running)
                {
                    CGEventTapEnable(_eventTap, true);
                    _logger.LogWarning("Re-enabled event tap after macOS disabled it (type={Type})", type);
                }
                return eventRef;
            }

            if (type != kCGEventKeyDown)
            {
                return eventRef;
            }

            // kCGKeyboardEventKeycode = 9
            ushort keyCode = (ushort)CGEventGetIntegerValueField(eventRef, 9);
            ulong flags = CGEventGetFlags(eventRef);

            // Mask: Shift(0x20000) | Control(0x40000) | Alt(0x80000) | Command(0x100000)
            // Exclude CapsLock/AlphaShift (0x10000) to prevent false negatives
            ulong modifierFlags = flags & 0x001E0000UL;

            _logger.LogDebug("Key event received: keycode=0x{KeyCode:X2}, flags=0x{Flags:X}, modifiers=0x{Mods:X}",
                keyCode, flags, modifierFlags);

            lock (_lock)
            {
                foreach (KeyValuePair<int, (string combination, ulong modifierMask, ushort keyCode)> kvp in _registeredKeys)
                {
                    if (kvp.Value.keyCode == keyCode && kvp.Value.modifierMask == modifierFlags)
                    {
                        int bindingId = kvp.Key;
                        _logger.LogInformation("Hotkey matched: {Combination} (binding {Id})", kvp.Value.combination, bindingId);

                        ThreadPool.QueueUserWorkItem(_ =>
                        {
                            try
                            {
                                OnHotkeyPressed?.Invoke(bindingId);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error in hotkey callback for binding {Id}", bindingId);
                            }
                        });
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in event tap callback");
        }

        return eventRef;
    }

    private static (ulong modifierMask, ushort keyCode) ParseKeyCombination(string combination)
    {
        ulong modifiers = 0;
        ushort keyCode = 0xFFFF;

        string[] parts = combination.Split('+');
        foreach (string part in parts)
        {
            string key = part.Trim().ToUpperInvariant();
            switch (key)
            {
                case "CTRL": case "CONTROL": modifiers |= 0x00100000UL; break;    // kCGEventFlagMaskCommand (⌘) — Mac convention: Ctrl maps to Cmd
                case "ALT": case "OPTION": modifiers |= 0x00080000UL; break;       // kCGEventFlagMaskAlternate
                case "SHIFT": modifiers |= 0x00020000UL; break;                     // kCGEventFlagMaskShift
                case "CMD": case "COMMAND": case "WIN": modifiers |= 0x00100000UL; break; // kCGEventFlagMaskCommand
                default:
                    keyCode = MapKeyToMacKeyCode(key);
                    break;
            }
        }

        return (modifiers, keyCode);
    }

    /// <summary>
    /// Maps key names to macOS virtual key codes.
    /// Reference: Events.h / CGKeyCode values
    /// </summary>
    private static ushort MapKeyToMacKeyCode(string key)
    {
        return key switch
        {
            "A" => 0x00, "S" => 0x01, "D" => 0x02, "F" => 0x03,
            "H" => 0x04, "G" => 0x05, "Z" => 0x06, "X" => 0x07,
            "C" => 0x08, "V" => 0x09, "B" => 0x0B, "Q" => 0x0C,
            "W" => 0x0D, "E" => 0x0E, "R" => 0x0F, "Y" => 0x10,
            "T" => 0x11, "1" => 0x12, "2" => 0x13, "3" => 0x14,
            "4" => 0x15, "6" => 0x16, "5" => 0x17, "9" => 0x19,
            "7" => 0x1A, "8" => 0x1C, "0" => 0x1D, "O" => 0x1F,
            "U" => 0x20, "I" => 0x22, "P" => 0x23, "L" => 0x25,
            "J" => 0x26, "K" => 0x28, "N" => 0x2D, "M" => 0x2E,
            "F1" => 0x7A, "F2" => 0x78, "F3" => 0x63, "F4" => 0x76,
            "F5" => 0x60, "F6" => 0x61, "F7" => 0x62, "F8" => 0x64,
            "F9" => 0x65, "F10" => 0x6D, "F11" => 0x67, "F12" => 0x6F,
            "F13" => 0x69, "F14" => 0x6B, "F15" => 0x71, "F16" => 0x6A,
            "F17" => 0x40, "F18" => 0x4F, "F19" => 0x50, "F20" => 0x5A,
            "SPACE" => 0x31, "RETURN" => 0x24, "ENTER" => 0x24,
            "TAB" => 0x30, "ESCAPE" => 0x35, "ESC" => 0x35,
            "DELETE" => 0x33, "BACKSPACE" => 0x33,
            "UP" => 0x7E, "DOWN" => 0x7D, "LEFT" => 0x7B, "RIGHT" => 0x7C,
            "HOME" => 0x73, "END" => 0x77, "PAGEUP" => 0x74, "PAGEDOWN" => 0x79,
            _ => 0xFFFF
        };
    }

    // ─── P/Invoke ──────────────────────────────────────────────────────

    [DllImport("/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices")]
    private static extern bool AXIsProcessTrusted();

    // Callback delegate — use [UnmanagedFunctionPointer] for correct calling convention
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr CGEventTapCallBack(IntPtr proxy, uint type, IntPtr eventRef, IntPtr userInfo);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern IntPtr CGEventTapCreate(
        int tap, int place, int options,
        ulong eventsOfInterest, CGEventTapCallBack callback, IntPtr userInfo);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern void CGEventTapEnable(IntPtr tap, bool enable);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern long CGEventGetIntegerValueField(IntPtr eventRef, int field);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern ulong CGEventGetFlags(IntPtr eventRef);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern IntPtr CFMachPortCreateRunLoopSource(IntPtr allocator, IntPtr port, long order);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern IntPtr CFRunLoopGetCurrent();

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRunLoopAddSource(IntPtr rl, IntPtr source, IntPtr mode);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRunLoopRun();

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRunLoopStop(IntPtr rl);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRelease(IntPtr cf);

    // Load kCFRunLoopCommonModes exported symbol via dlsym
    [DllImport("/usr/lib/libSystem.dylib")]
    private static extern IntPtr dlopen(string? path, int mode);

    [DllImport("/usr/lib/libSystem.dylib")]
    private static extern IntPtr dlsym(IntPtr handle, string symbol);

    private static IntPtr LoadCFRunLoopCommonModes()
    {
        IntPtr cf = dlopen("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation", 0);
        IntPtr symbolPtr = dlsym(cf, "kCFRunLoopCommonModes");
        return Marshal.ReadIntPtr(symbolPtr);
    }
}

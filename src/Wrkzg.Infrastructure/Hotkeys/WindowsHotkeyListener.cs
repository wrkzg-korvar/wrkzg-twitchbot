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
/// Creates a dedicated hidden window on a background thread to pump WM_HOTKEY messages.
/// </summary>
[SupportedOSPlatform("windows")]
public class WindowsHotkeyListener : IHotkeyListener
{
    private readonly ILogger<WindowsHotkeyListener> _logger;
    private readonly Dictionary<int, string> _registeredKeys = new();
    private readonly object _lock = new();
    private Thread? _messageThread;
    private IntPtr _hwnd;
    private volatile bool _running;

    /// <summary>Raised when a registered hotkey combination is pressed.</summary>
    public event Action<int>? OnHotkeyPressed;

    /// <summary>Gets whether global hotkey interception is supported on this platform.</summary>
    public bool IsGlobalHotkeySupported => true;

    /// <summary>Gets whether permission is granted. Always true on Windows (no special permission needed).</summary>
    public bool HasPermission => true; // Windows doesn't need special permissions

    /// <summary>No-op on Windows. No special permission is required for global hotkeys.</summary>
    public void RequestPermission() { } // No-op on Windows

    private const int WM_HOTKEY = 0x0312;
    private const int WM_DESTROY = 0x0002;
    private const string WindowClassName = "WrkzgHotkeyWindow";

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsHotkeyListener"/> class.
    /// </summary>
    /// <param name="logger">The logger for hotkey diagnostics.</param>
    public WindowsHotkeyListener(ILogger<WindowsHotkeyListener> logger)
    {
        _logger = logger;
    }

    /// <summary>Creates a hidden message window and starts the WM_HOTKEY message pump on a background thread.</summary>
    public Task StartListeningAsync(CancellationToken ct = default)
    {
        if (_running)
        {
            return Task.CompletedTask;
        }

        TaskCompletionSource tcs = new();

        _messageThread = new Thread(() => MessagePumpThread(tcs))
        {
            IsBackground = true,
            Name = "WrkzgHotkeyMessagePump"
        };
        _messageThread.SetApartmentState(ApartmentState.STA);
        _messageThread.Start();

        return tcs.Task;
    }

    /// <summary>Stops the message pump and destroys the hidden message window.</summary>
    public Task StopListeningAsync(CancellationToken ct = default)
    {
        if (!_running)
        {
            return Task.CompletedTask;
        }

        _running = false;

        if (_hwnd != IntPtr.Zero)
        {
            PostMessage(_hwnd, WM_DESTROY, IntPtr.Zero, IntPtr.Zero);
        }

        _messageThread?.Join(TimeSpan.FromSeconds(3));
        _messageThread = null;

        _logger.LogInformation("Windows hotkey listener stopped");
        return Task.CompletedTask;
    }

    /// <summary>Registers a global hotkey using the Windows RegisterHotKey API.</summary>
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

            // RegisterHotKey must be called on the message pump thread
            // (same thread that owns the window). Use synchronous dispatch.
            if (_hwnd == IntPtr.Zero)
            {
                _logger.LogWarning("Cannot register hotkey — message window not ready");
                return false;
            }

            // Always unregister first to avoid ERROR_HOTKEY_ALREADY_REGISTERED (1408)
            // when re-registering after a binding change or app restart without clean shutdown
            UnregisterHotKey(_hwnd, id);
            bool result = RegisterHotKey(_hwnd, id, modifiers, vk);
            if (result)
            {
                lock (_lock)
                {
                    _registeredKeys[id] = keyCombination;
                }
                _logger.LogInformation("Registered hotkey {Id}: {Keys}", id, keyCombination);
            }
            else
            {
                int error = Marshal.GetLastWin32Error();
                _logger.LogWarning("Failed to register hotkey {Id}: {Keys} (Win32 error: {Error})", id, keyCombination, error);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering hotkey {Id}", id);
            return false;
        }
    }

    /// <summary>Unregisters a global hotkey by its binding identifier.</summary>
    public void UnregisterHotkey(int id)
    {
        if (_hwnd != IntPtr.Zero)
        {
            UnregisterHotKey(_hwnd, id);
        }
        lock (_lock)
        {
            _registeredKeys.Remove(id);
        }
    }

    /// <summary>Unregisters all global hotkeys from the message window.</summary>
    public void UnregisterAll()
    {
        lock (_lock)
        {
            foreach (int id in _registeredKeys.Keys)
            {
                if (_hwnd != IntPtr.Zero)
                {
                    UnregisterHotKey(_hwnd, id);
                }
            }
            _registeredKeys.Clear();
        }
    }

    /// <summary>Simulates a hotkey press for testing via the API.</summary>
    public void SimulateHotkeyPress(int id)
    {
        OnHotkeyPressed?.Invoke(id);
    }

    private void MessagePumpThread(TaskCompletionSource tcs)
    {
        try
        {
            _hwnd = CreateMessageOnlyWindow();
            if (_hwnd == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                _logger.LogError("Failed to create hotkey message window (Win32 error: {Error})", error);
                tcs.TrySetException(new InvalidOperationException($"Failed to create message window: Win32 error {error}"));
                return;
            }

            _running = true;
            _logger.LogInformation("Windows hotkey listener started (message window: {Handle})", _hwnd);
            tcs.TrySetResult();

            // Message pump loop
            while (_running && GetMessage(out MSG msg, IntPtr.Zero, 0, 0) > 0)
            {
                if (msg.message == WM_HOTKEY)
                {
                    int bindingId = (int)msg.wParam;
                    try
                    {
                        OnHotkeyPressed?.Invoke(bindingId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in hotkey callback for binding {Id}", bindingId);
                    }
                }

                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hotkey message pump thread crashed");
            tcs.TrySetException(ex);
        }
        finally
        {
            if (_hwnd != IntPtr.Zero)
            {
                // Unregister all hotkeys before destroying window
                lock (_lock)
                {
                    foreach (int id in _registeredKeys.Keys)
                    {
                        UnregisterHotKey(_hwnd, id);
                    }
                    _registeredKeys.Clear();
                }
                DestroyWindow(_hwnd);
                _hwnd = IntPtr.Zero;
            }
            _running = false;
        }
    }

    private IntPtr CreateMessageOnlyWindow()
    {
        // HWND_MESSAGE (-3) creates a message-only window (no visible UI)
        IntPtr HWND_MESSAGE = new(-3);
        return CreateWindowEx(0, "STATIC", WindowClassName, 0, 0, 0, 0, 0, HWND_MESSAGE, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
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

    // ─── P/Invoke ──────────────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public POINT pt;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage(ref MSG lpMsg);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowEx(
        uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle,
        int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);
}

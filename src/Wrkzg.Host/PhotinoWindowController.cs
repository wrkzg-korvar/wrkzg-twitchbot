using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Photino.NET;
using Wrkzg.Core.Interfaces;

namespace Wrkzg.Host;

/// <summary>
/// Bridges IWindowController to the Photino window instance.
/// Window reference is set after creation in PhotinoHosting.Start().
/// </summary>
public class PhotinoWindowController : IWindowController
{
    private PhotinoWindow? _window;

    // Drag state: mouse start position and window start position
    private int _dragStartMouseX;
    private int _dragStartMouseY;
    private int _dragStartWindowX;
    private int _dragStartWindowY;

    /// <summary>Gets whether the Photino window is currently maximized.</summary>
    public bool IsMaximized => _window?.Maximized ?? false;

    /// <summary>Sets the Photino window instance after creation.</summary>
    public void SetWindow(PhotinoWindow window)
    {
        _window = window;
    }

    /// <summary>Minimizes the Photino window.</summary>
    public void Minimize()
    {
        _window?.SetMinimized(true);
    }

    /// <summary>Toggles the Photino window between maximized and restored state.</summary>
    public void ToggleMaximize()
    {
        if (_window is null)
        {
            return;
        }

        _window.SetMaximized(!_window.Maximized);
    }

    /// <summary>Closes the Photino window, triggering application shutdown.</summary>
    public void Close()
    {
        _window?.Close();
    }

    /// <summary>Records the initial mouse and window positions to begin a drag operation.</summary>
    public void DragStart(int screenX, int screenY)
    {
        if (_window is null)
        {
            return;
        }

        _dragStartMouseX = screenX;
        _dragStartMouseY = screenY;
        _dragStartWindowX = _window.Left;
        _dragStartWindowY = _window.Top;
    }

    /// <summary>Moves the window by the delta between the current and initial mouse positions.</summary>
    public void DragMove(int screenX, int screenY)
    {
        if (_window is null)
        {
            return;
        }

        int deltaX = screenX - _dragStartMouseX;
        int deltaY = screenY - _dragStartMouseY;

        _window.SetLeft(_dragStartWindowX + deltaX);
        _window.SetTop(_dragStartWindowY + deltaY);
    }

    /// <summary>Initiates a native window resize operation in the specified direction (Windows only).</summary>
    public void StartResize(string direction)
    {
        if (_window is null || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        if (!ResizeDirectionMap.TryGetValue(direction, out int wmszDirection))
        {
            return;
        }

        IntPtr hwnd = _window.WindowHandle;
        NativeMethods.ReleaseCapture();
        NativeMethods.SendMessage(hwnd, NativeMethods.WmSyscommand,
            (IntPtr)(NativeMethods.ScSize | wmszDirection), IntPtr.Zero);
    }

    private static readonly Dictionary<string, int> ResizeDirectionMap = new()
    {
        ["w"] = 1,   // WMSZ_LEFT
        ["e"] = 2,   // WMSZ_RIGHT
        ["n"] = 3,   // WMSZ_TOP
        ["nw"] = 4,  // WMSZ_TOPLEFT
        ["ne"] = 5,  // WMSZ_TOPRIGHT
        ["s"] = 6,   // WMSZ_BOTTOM
        ["sw"] = 7,  // WMSZ_BOTTOMLEFT
        ["se"] = 8,  // WMSZ_BOTTOMRIGHT
    };

    private static class NativeMethods
    {
        internal const int WmSyscommand = 0x0112;
        internal const int ScSize = 0xF000;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ReleaseCapture();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    }
}

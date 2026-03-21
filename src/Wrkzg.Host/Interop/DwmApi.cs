using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Wrkzg.Host.Interop;

[SupportedOSPlatform("windows")]
internal static class DwmApi
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaCaptionColor = 35;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(
        nint hwnd,
        int attribute,
        ref int value,
        int size);

    /// <summary>
    /// Enables dark mode for the window title bar (Windows 10 1809+ / Windows 11).
    /// </summary>
    public static void EnableDarkMode(nint hwnd)
    {
        int darkMode = 1;
        DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref darkMode, sizeof(int));
    }

    /// <summary>
    /// Sets the title bar caption color to match the app background.
    /// Color format: 0x00BBGGRR (BGR, not RGB!)
    /// Available on Windows 11 Build 22000+. Silently ignored on Windows 10.
    /// </summary>
    public static void SetCaptionColor(nint hwnd, byte r, byte g, byte b)
    {
        int color = r | (g << 8) | (b << 16);
        DwmSetWindowAttribute(hwnd, DwmwaCaptionColor, ref color, sizeof(int));
    }

    // ─── Window Icon via Win32 API ───────────────────────

    private const uint WmSeticon = 0x0080;
    private const uint ImageIcon = 1;
    private const uint LrLoadfromfile = 0x0010;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint SendMessageW(nint hWnd, uint msg, nint wParam, nint lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint LoadImage(nint hInst, string name, uint type, int cx, int cy, uint fuLoad);

    /// <summary>
    /// Sets the window icon from a .ico file via Win32 API.
    /// This sets both the title bar icon (16x16) and the taskbar icon (32x32).
    /// Photino's SetIconFile doesn't reliably set the taskbar icon on Windows.
    /// </summary>
    public static void SetWindowIcon(nint hwnd, string icoPath)
    {
        // Small icon (title bar, 16x16)
        nint smallIcon = LoadImage(0, icoPath, ImageIcon, 16, 16, LrLoadfromfile);
        if (smallIcon != 0)
        {
            SendMessageW(hwnd, WmSeticon, 0, smallIcon);
        }

        // Big icon (taskbar, Alt+Tab, 32x32)
        nint bigIcon = LoadImage(0, icoPath, ImageIcon, 32, 32, LrLoadfromfile);
        if (bigIcon != 0)
        {
            SendMessageW(hwnd, WmSeticon, 1, bigIcon);
        }
    }
}

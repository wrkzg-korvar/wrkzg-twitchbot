using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Photino.NET;

namespace Wrkzg.Host;

/// <summary>
/// Startet das Photino-Fenster nachdem Kestrel hochgefahren ist.
///
/// Dev-Modus:
///   - Versucht den Vite Dev Server auf :5173 zu erreichen (für HMR)
///   - Falls Vite nicht läuft, fällt es auf Kestrel zurück
///     (statische Dateien aus wwwroot/)
///
/// Production-Modus:
///   - Zeigt immer auf Kestrel
/// </summary>
public static class PhotinoHosting
{
    private const string ViteDevUrl = "http://localhost:5173";

    public static void Start(WebApplication app, PhotinoWindowController windowController)
    {
        try
        {
            // Kestrel asynchron im Hintergrund starten
            var serverTask = app.StartAsync();
            serverTask.Wait();

            // URL bestimmen
            string kestrelUrl = app.Urls.First();
            string url;

            if (app.Environment.IsDevelopment() && IsViteRunning())
            {
                url = ViteDevUrl;
            }
            else
            {
                url = kestrelUrl;
            }

            // Resolve icon path (relative to the executable)
            string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "icon.png");

            PhotinoWindow window = new PhotinoWindow()
                .SetTitle("Wrkzg")
                .SetSize(1280, 820)
                .SetMinSize(900, 600)
                .SetResizable(true)
                .SetContextMenuEnabled(false);

            // Chromeless only on macOS — on Windows, WebView2 breaks mouse events in chromeless mode
            if (OperatingSystem.IsMacOS())
            {
                window.SetChromeless(true);
            }

            if (File.Exists(iconPath))
            {
                window.SetIconFile(iconPath);
            }

            // Customize the native title bar once the window handle is available
            if (OperatingSystem.IsWindows())
            {
                window.RegisterWindowCreatedHandler((sender, args) =>
                {
                    PhotinoWindow win = (PhotinoWindow)sender!;
#pragma warning disable CA1416 // Platform guard is in the enclosing if block
                    ApplyWindowsTheme(win.WindowHandle);
#pragma warning restore CA1416
                });
            }

            window.Load(new Uri(url));

            windowController.SetWindow(window);

            // Blockiert bis das Fenster geschlossen wird
            window.WaitForClose();

            // Sauber herunterfahren
            app.StopAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Photino] Fatal error: {ex}");
            app.StopAsync().GetAwaiter().GetResult();
            throw;
        }
    }

    [SupportedOSPlatform("windows")]
    private static void ApplyWindowsTheme(nint hwnd)
    {
        Interop.DwmApi.EnableDarkMode(hwnd);
        // Set caption color to match --color-bg (#0a0a0f)
        Interop.DwmApi.SetCaptionColor(hwnd, 0x0a, 0x0a, 0x0f);

        // Set taskbar + title bar icon via Win32 API
        // (Photino's SetIconFile doesn't reliably set the taskbar icon on Windows)
        string icoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "icon.ico");
        if (File.Exists(icoPath))
        {
            Interop.DwmApi.SetWindowIcon(hwnd, icoPath);
        }
    }

    /// <summary>
    /// Prüft ob der Vite Dev Server erreichbar ist (1s Timeout).
    /// </summary>
    private static bool IsViteRunning()
    {
        try
        {
            using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(1) };
            HttpResponseMessage response = client.GetAsync(ViteDevUrl).GetAwaiter().GetResult();
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}

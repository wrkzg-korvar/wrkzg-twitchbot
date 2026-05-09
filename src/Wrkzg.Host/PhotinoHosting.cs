using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Photino.NET;
using Wrkzg.Api.Security;

namespace Wrkzg.Host;

/// <summary>
/// Starts the Photino window once Kestrel is up.
///
/// Dev mode:
///   - Tries to reach the Vite dev server on :5173 (for HMR)
///   - Falls back to Kestrel if Vite is not running
///     (static files from wwwroot/)
///
/// Production mode:
///   - Always points at Kestrel
/// </summary>
public static class PhotinoHosting
{
    private const string ViteDevUrl = "http://localhost:5173";

    /// <summary>Starts Kestrel, opens the Photino browser window, and blocks until the window is closed.</summary>
    public static void Start(WebApplication app, PhotinoWindowController windowController)
    {
        try
        {
            // Start Kestrel asynchronously in the background
            var serverTask = app.StartAsync();
            serverTask.Wait();

            // Determine URL
            string kestrelUrl = app.Urls.First();
            string baseUrl;

            if (app.Environment.IsDevelopment() && IsViteRunning())
            {
                baseUrl = ViteDevUrl;
            }
            else
            {
                baseUrl = kestrelUrl;
            }

            // Append the per-session API token so the frontend can authenticate requests
            ApiTokenService tokenService = app.Services.GetRequiredService<ApiTokenService>();
            string url = $"{baseUrl}?__wrkzg_token={Uri.EscapeDataString(tokenService.Token)}";

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

            // On Windows, skip SetIconFile — the Win32 API in ApplyWindowsTheme
            // handles both title bar and taskbar icons via .ico file.
            // SetIconFile with .png doesn't reliably set the Windows taskbar icon.
            if (File.Exists(iconPath) && !OperatingSystem.IsWindows())
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

            // Blocks until the window is closed
            window.WaitForClose();

            // Shut down cleanly
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
    /// Checks whether the Vite dev server is reachable (1s timeout).
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

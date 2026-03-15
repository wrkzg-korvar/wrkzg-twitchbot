using System;
using System.IO;
using System.Linq;
using System.Net.Http;
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
        string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "icon.ico");

        PhotinoWindow window = new PhotinoWindow()
            .SetTitle("Wrkzg")
            .SetChromeless(true)
            .SetSize(1280, 820)
            .SetMinSize(900, 600)
            .SetResizable(true)
            .SetContextMenuEnabled(false);

        if (File.Exists(iconPath))
        {
            window.SetIconFile(iconPath);
        }

        window.Load(new Uri(url));

        windowController.SetWindow(window);

        // Blockiert bis das Fenster geschlossen wird
        window.WaitForClose();

        // Sauber herunterfahren
        app.StopAsync().GetAwaiter().GetResult();
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

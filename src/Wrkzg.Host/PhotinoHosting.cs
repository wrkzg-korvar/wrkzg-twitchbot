using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Photino.NET;

namespace Wrkzg.Host;

/// <summary>
/// Startet das Photino-Fenster nachdem Kestrel hochgefahren ist.
/// </summary>
public static class PhotinoHosting
{
    public static void Start(WebApplication app)
    {
        // Kestrel asynchron im Hintergrund starten
        var serverTask = app.StartAsync();
        serverTask.Wait();

        // Tatsächlich gebundene URL ermitteln (random port aus launchSettings oder Konfiguration)
        var url = app.Urls.First();

        var window = new PhotinoWindow()
            .SetTitle("Wrkzg")
            .SetSize(1280, 820)
            .SetMinSize(900, 600)
            .SetResizable(true)
            .SetContextMenuEnabled(false)
            .Load(new Uri(url));

        // Systemtray initialisieren
        // TODO: TrayIconManager.Initialize(window);

        // Blockiert bis das Fenster geschlossen wird
        window.WaitForClose();

        // Sauber herunterfahren
        app.StopAsync().GetAwaiter().GetResult();
    }
}

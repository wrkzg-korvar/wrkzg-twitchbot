using System;
using System.IO;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Wrkzg.Api;
using Wrkzg.Api.Endpoints;
using Wrkzg.Api.Hubs;
using Wrkzg.Api.Security;
using Wrkzg.Core;
using Wrkzg.Core.Interfaces;
using Wrkzg.Host;
using Wrkzg.Infrastructure;
using Wrkzg.Infrastructure.Data;

// WebView2 on Windows requires STA threading for Photino to render correctly.
// Without this, the Photino window opens but shows a blank white screen.
if (OperatingSystem.IsWindows())
{
    Thread.CurrentThread.SetApartmentState(ApartmentState.Unknown);
    Thread.CurrentThread.SetApartmentState(ApartmentState.STA);
}


var builder = WebApplication.CreateBuilder(args);

// Bind Kestrel to the configured port (default 5050, avoids macOS AirPlay on 5000)
string port = builder.Configuration["Bot:Port"] ?? "5050";
builder.WebHost.UseUrls($"http://localhost:{port}");

PhotinoWindowController windowController = new();
builder.Services.AddSingleton<IWindowController>(windowController);

builder.Services.AddCoreServices();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiServices();

var app = builder.Build();

// Apply pending EF Core migrations on startup
using (IServiceScope scope = app.Services.CreateScope())
{
    BotDbContext db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// ─── Asset Serving (before auth — overlays need access without token) ──
WrkzgPaths.EnsureDirectories();
string assetsPath = WrkzgPaths.AssetsDirectory;
if (Directory.Exists(assetsPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(assetsPath),
        RequestPath = "/assets"
    });
}

// ─── Security ────────────────────────────────────────────────────────
// Security headers (CSP, X-Frame-Options, X-Content-Type-Options, Referrer-Policy)
app.UseMiddleware<SecurityHeadersMiddleware>();

// API token validation: all /api, /auth, /hubs routes require the session token.
// The Photino WebView receives the token via URL query parameter on startup.
app.UseMiddleware<ApiTokenMiddleware>();
app.UseCors();

// ─── Static Files ─────────────────────────────────────────────────────
// The React SPA build output lives in Wrkzg.Api/wwwroot/ (built by Vite).
// Since the entry point is Wrkzg.Host (a different project), ASP.NET Core's
// default WebRootPath points to Wrkzg.Host/wwwroot/ which doesn't exist.
// We resolve the actual path to where the built frontend files are.
string? wwwrootPath = ResolveWwwrootPath();

if (wwwrootPath is not null && Directory.Exists(wwwrootPath))
{
    PhysicalFileProvider fileProvider = new(wwwrootPath);

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = fileProvider
    });

    app.UseRouting();

    app.MapHub<ChatHub>("/hubs/chat");
    app.MapAuthEndpoints();
    app.MapCommandEndpoints();
    app.MapSettingsEndpoints();
    app.MapUserEndpoints();
    app.MapStatusEndpoints();
    app.MapWindowEndpoints();
    app.MapBotEndpoints();
    app.MapChatEndpoints();
    app.MapPollEndpoints();
    app.MapRaffleEndpoints();
    app.MapTimerEndpoints();
    app.MapCounterEndpoints();
    app.MapSpamFilterEndpoints();
    app.MapQuoteEndpoints();
    app.MapNotificationEndpoints();
    app.MapOverlayEndpoints();
    app.MapChannelPointEndpoints();
    app.MapRoleEndpoints();
    app.MapGameEndpoints();
    app.MapAnalyticsEndpoints();
    app.MapSongRequestEndpoints();
    app.MapHotkeyEndpoints();
    app.MapEffectEndpoints();
    app.MapIntegrationEndpoints();
    app.MapImportEndpoints();
    app.MapAssetEndpoints();
    app.MapCustomOverlayEndpoints();

    // SPA fallback: unmatched routes serve index.html for React Router
    app.MapFallbackToFile("index.html", new StaticFileOptions
    {
        FileProvider = fileProvider
    });

}
else
{
    app.UseRouting();

    app.MapHub<ChatHub>("/hubs/chat");
    app.MapAuthEndpoints();
    app.MapCommandEndpoints();
    app.MapSettingsEndpoints();
    app.MapUserEndpoints();
    app.MapStatusEndpoints();
    app.MapWindowEndpoints();
    app.MapBotEndpoints();
    app.MapChatEndpoints();
    app.MapPollEndpoints();
    app.MapRaffleEndpoints();
    app.MapTimerEndpoints();
    app.MapCounterEndpoints();
    app.MapSpamFilterEndpoints();
    app.MapQuoteEndpoints();
    app.MapNotificationEndpoints();
    app.MapOverlayEndpoints();
    app.MapChannelPointEndpoints();
    app.MapRoleEndpoints();
    app.MapGameEndpoints();
    app.MapAnalyticsEndpoints();
    app.MapSongRequestEndpoints();
    app.MapHotkeyEndpoints();
    app.MapEffectEndpoints();
    app.MapIntegrationEndpoints();
    app.MapImportEndpoints();
    app.MapAssetEndpoints();
    app.MapCustomOverlayEndpoints();
}

// In test environment, WebApplicationFactory manages the server lifecycle.
// In all other environments, Photino manages Kestrel + the browser window.
if (app.Environment.IsEnvironment("Testing"))
{
    await app.RunAsync();
}
else
{
    PhotinoHosting.Start(app, windowController);
}

// ─── Helper ───────────────────────────────────────────────────────────

// Finds the wwwroot directory containing the built React SPA.
// Checks multiple locations because the path differs between
// development (source tree) and published (alongside DLL) scenarios.
static string? ResolveWwwrootPath()
{
    string[] candidates = new[]
    {
        // 1. Next to the executable (published builds, SingleFile)
        Path.Combine(AppContext.BaseDirectory, "wwwroot"),

        // 2. Next to the Wrkzg.Api assembly (bin output when Host != Api)
        Path.Combine(
            Path.GetDirectoryName(typeof(Wrkzg.Api.DependencyInjection).Assembly.Location) ?? AppContext.BaseDirectory,
            "wwwroot"),

        // 3. Relative to CWD (dotnet run from solution root)
        Path.GetFullPath(Path.Combine("src", "Wrkzg.Api", "wwwroot")),

        // 4. Relative to AppContext.BaseDirectory going up to find Api project
        //    Handles: bin/Debug/net10.0/ → go up 4 levels to solution, then into Api
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Wrkzg.Api", "wwwroot")),

        // 5. Same but from src/ layout
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Wrkzg.Api", "wwwroot")),
    };

    foreach (string candidate in candidates)
    {
        if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "index.html")))
        {
            Console.WriteLine($"[Wrkzg] Serving frontend from: {candidate}");
            return candidate;
        }
    }

    Console.Error.WriteLine("[Wrkzg] WARNING: Frontend wwwroot not found!");
    Console.Error.WriteLine("[Wrkzg] Searched locations:");
    foreach (string candidate in candidates)
    {
        Console.Error.WriteLine($"  - {candidate} (exists: {Directory.Exists(candidate)})");
    }
    Console.Error.WriteLine("[Wrkzg] Run 'cd src/Wrkzg.Frontend && npm run build' to build the SPA.");

    return null;
}

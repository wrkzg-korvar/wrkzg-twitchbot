using System;
using System.IO;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Wrkzg.Api;
using Wrkzg.Api.Endpoints;
using Wrkzg.Api.Hubs;
using Wrkzg.Api.Middleware;
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

// ─── Serilog Bootstrap ──────────────────────────────────────────────
// Configure file logging before the host builder so that startup errors are captured.
// Logs are written to the Wrkzg data directory (same location as the SQLite database).
WrkzgPaths.EnsureDirectories();
string logDirectory = WrkzgPaths.LogsDirectory;

// In Testing mode (WebApplicationFactory) only log to Console — never to the
// shared production log file, otherwise multiple test host instances flood
// the file with duplicate lines.
bool isTesting = string.Equals(
    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
    "Testing",
    StringComparison.OrdinalIgnoreCase);

LoggerConfiguration logConfig = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("System.Net.Http", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Extensions.Http", LogEventLevel.Warning)
    .MinimumLevel.Override("Polly", LogEventLevel.Warning)
    .MinimumLevel.Override("TwitchLib.EventSub.Websockets", LogEventLevel.Warning)
    .MinimumLevel.Override("Wrkzg.Infrastructure.Twitch.TwitchChatClient", LogEventLevel.Information)
    .MinimumLevel.Override("Wrkzg.Infrastructure.Hotkeys.MacOsHotkeyListener", LogEventLevel.Information)
    .MinimumLevel.Override("Wrkzg.Infrastructure.Hotkeys.WindowsHotkeyListener", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
        restrictedToMinimumLevel: LogEventLevel.Information);

if (!isTesting)
{
    logConfig = logConfig.WriteTo.File(
        path: Path.Combine(logDirectory, "wrkzg-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        fileSizeLimitBytes: 50 * 1024 * 1024,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
        shared: true);
}

Log.Logger = logConfig.CreateLogger();

try
{
    Log.Information("Wrkzg starting up");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // Bind Kestrel to the configured port (default 5050, avoids macOS AirPlay on 5000)
    string port = builder.Configuration["Bot:Port"] ?? "5050";
    builder.WebHost.UseUrls($"http://localhost:{port}");

    PhotinoWindowController windowController = new();
    builder.Services.AddSingleton<IWindowController>(windowController);

    builder.Services.AddProblemDetails(options =>
    {
        options.CustomizeProblemDetails = ctx =>
        {
            ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
        };
    });
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

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

    app.UseExceptionHandler();
    app.UseStatusCodePages();

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    // ─── Asset Serving (before auth — overlays need access without token) ──
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
        app.MapHotkeyEndpoints();
        app.MapEffectEndpoints();
        app.MapIntegrationEndpoints();
        app.MapImportEndpoints();
        app.MapAssetEndpoints();
        app.MapCustomOverlayEndpoints();
        app.MapEmoteEndpoints();
        app.MapDiagnosticsEndpoints();

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
        app.MapHotkeyEndpoints();
        app.MapEffectEndpoints();
        app.MapIntegrationEndpoints();
        app.MapImportEndpoints();
        app.MapAssetEndpoints();
        app.MapCustomOverlayEndpoints();
        app.MapEmoteEndpoints();
        app.MapDiagnosticsEndpoints();
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
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// ─── Helper ───────────────────────────────────────────────────────────

// Finds the wwwroot directory containing the built React SPA.
// Checks multiple locations because the path differs between
// development (source tree) and published (alongside DLL) scenarios.
static string? ResolveWwwrootPath()
{
    string[] candidates = new[]
    {
        Path.Combine(AppContext.BaseDirectory, "wwwroot"),
        Path.Combine(
            Path.GetDirectoryName(typeof(Wrkzg.Api.DependencyInjection).Assembly.Location) ?? AppContext.BaseDirectory,
            "wwwroot"),
        Path.GetFullPath(Path.Combine("src", "Wrkzg.Api", "wwwroot")),
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Wrkzg.Api", "wwwroot")),
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

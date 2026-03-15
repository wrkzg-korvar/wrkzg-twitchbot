using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Wrkzg.Api;
using Wrkzg.Api.Endpoints;
using Wrkzg.Api.Hubs;
using Wrkzg.Core;
using Wrkzg.Core.Interfaces;
using Wrkzg.Host;
using Wrkzg.Infrastructure;
using Wrkzg.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

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
}

PhotinoHosting.Start(app, windowController);

// ─── Helper ───────────────────────────────────────────────────────────

// Finds the wwwroot directory containing the built React SPA.
// Checks multiple locations because the path differs between
// development (source tree) and published (alongside DLL) scenarios.
static string? ResolveWwwrootPath()
{
    // 1. Next to the executable (published/bin output)
    string binPath = Path.Combine(AppContext.BaseDirectory, "wwwroot");
    if (Directory.Exists(binPath) && File.Exists(Path.Combine(binPath, "index.html")))
    {
        return binPath;
    }

    // 2. Relative to CWD (when running from solution root with dotnet run)
    string devPath = Path.GetFullPath(Path.Combine("src", "Wrkzg.Api", "wwwroot"));
    if (Directory.Exists(devPath) && File.Exists(Path.Combine(devPath, "index.html")))
    {
        return devPath;
    }

    // 3. Relative to the Host project directory
    string hostDir = AppContext.BaseDirectory;
    string relPath = Path.GetFullPath(Path.Combine(hostDir, "..", "..", "..", "..", "Wrkzg.Api", "wwwroot"));
    if (Directory.Exists(relPath) && File.Exists(Path.Combine(relPath, "index.html")))
    {
        return relPath;
    }

    return null;
}

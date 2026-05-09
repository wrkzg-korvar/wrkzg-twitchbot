using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wrkzg.Core;

namespace Wrkzg.Api.Endpoints;

/// <summary>
/// Diagnostic endpoints for log export and system health.
/// Used by the Settings page to download the current log file for bug reports.
/// </summary>
public static class DiagnosticsEndpoints
{
    /// <summary>Registers diagnostic API endpoints.</summary>
    /// <param name="app">The endpoint route builder to register the endpoints on.</param>
    public static void MapDiagnosticsEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/diagnostics").WithTags("Diagnostics");

        // GET /api/diagnostics/log — download the most recent log file
        group.MapGet("/log", () =>
        {
            string logDir = WrkzgPaths.LogsDirectory;
            if (!Directory.Exists(logDir))
            {
                return Results.Problem(
                    detail: "Log directory does not exist.",
                    title: "Not Found",
                    statusCode: StatusCodes.Status404NotFound,
                    type: "https://wrkzg.app/problems/not-found");
            }

            FileInfo? latestLog = new DirectoryInfo(logDir)
                .GetFiles("wrkzg-*.log")
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .FirstOrDefault();

            if (latestLog is null || !latestLog.Exists)
            {
                return Results.Problem(
                    detail: "No log files found.",
                    title: "Not Found",
                    statusCode: StatusCodes.Status404NotFound,
                    type: "https://wrkzg.app/problems/not-found");
            }

            // Read the file with shared access (Serilog holds the file open with FileShare.ReadWrite)
            FileStream stream = new(
                latestLog.FullName,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite);

            string fileName = $"wrkzg-diagnostic-{DateTime.UtcNow:yyyyMMdd-HHmmss}.log";
            return Results.File(stream, "text/plain", fileName);
        });

        // POST /api/diagnostics/log/export — copy log to Downloads folder and return path
        // Photino's embedded WebView silently blocks programmatic Blob URL downloads,
        // so we copy the file server-side and surface the path to the user.
        group.MapPost("/log/export", () =>
        {
            string logDir = WrkzgPaths.LogsDirectory;
            if (!Directory.Exists(logDir))
            {
                return Results.Problem(
                    detail: "Log directory does not exist.",
                    title: "Not Found",
                    statusCode: StatusCodes.Status404NotFound,
                    type: "https://wrkzg.app/problems/not-found");
            }

            FileInfo? latestLog = new DirectoryInfo(logDir)
                .GetFiles("wrkzg-*.log")
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .FirstOrDefault();

            if (latestLog is null || !latestLog.Exists)
            {
                return Results.Problem(
                    detail: "No log files found.",
                    title: "Not Found",
                    statusCode: StatusCodes.Status404NotFound,
                    type: "https://wrkzg.app/problems/not-found");
            }

            string downloadsFolder = GetDownloadsFolder();
            string fileName = $"wrkzg-diagnostic-{DateTime.UtcNow:yyyyMMdd-HHmmss}.log";
            string destinationPath = Path.Combine(downloadsFolder, fileName);

            // FileShare.ReadWrite on source so we can copy while Serilog still holds the file
            using (FileStream source = new(latestLog.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (FileStream destination = new(destinationPath, FileMode.Create, FileAccess.Write))
            {
                source.CopyTo(destination);
            }

            return Results.Ok(new { path = destinationPath, fileName });
        });

        // GET /api/diagnostics/log/entries?count=N — return last N log lines as JSON
        group.MapGet("/log/entries", (int? count) =>
        {
            int lineCount = Math.Clamp(count ?? 100, 10, 1000);
            string logDir = WrkzgPaths.LogsDirectory;

            if (!Directory.Exists(logDir))
            {
                return Results.Ok(Array.Empty<string>());
            }

            FileInfo? latestLog = new DirectoryInfo(logDir)
                .GetFiles("wrkzg-*.log")
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .FirstOrDefault();

            if (latestLog is null || !latestLog.Exists)
            {
                return Results.Ok(Array.Empty<string>());
            }

            string[] allLines;
            using (FileStream fs = new(latestLog.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader reader = new(fs))
            {
                allLines = reader.ReadToEnd().Split('\n');
            }

            string[] lastLines = allLines
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .TakeLast(lineCount)
                .ToArray();

            return Results.Ok(lastLines);
        });
    }

    /// <summary>
    /// Resolves the user's Downloads folder across Windows and macOS.
    /// Falls back to Desktop, then to the log directory itself.
    /// </summary>
    private static string GetDownloadsFolder()
    {
        string? userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(userProfile))
        {
            string downloads = Path.Combine(userProfile, "Downloads");
            if (Directory.Exists(downloads))
            {
                return downloads;
            }
        }

        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        if (!string.IsNullOrWhiteSpace(desktop) && Directory.Exists(desktop))
        {
            return desktop;
        }

        return WrkzgPaths.LogsDirectory;
    }
}

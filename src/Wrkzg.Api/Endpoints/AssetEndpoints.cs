using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wrkzg.Core;

namespace Wrkzg.Api.Endpoints;

/// <summary>
/// REST endpoints for managing uploaded assets (sounds, images).
/// </summary>
public static class AssetEndpoints
{
    private static readonly Dictionary<string, string[]> AllowedExtensions = new()
    {
        ["sounds"] = new[] { ".mp3", ".wav", ".ogg" },
        ["images"] = new[] { ".png", ".jpg", ".jpeg", ".gif", ".webp", ".webm", ".svg" },
    };

    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    public static void MapAssetEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/assets").WithTags("Assets");

        // Upload a file
        group.MapPost("/upload/{category}", async (
            string category,
            HttpRequest request,
            CancellationToken ct) =>
        {
            string cat = category.ToLowerInvariant();
            if (!AllowedExtensions.ContainsKey(cat))
            {
                return Results.BadRequest(new { error = "Category must be 'sounds' or 'images'." });
            }

            IFormFile? file = request.Form.Files.GetFile("file");
            if (file is null || file.Length == 0)
            {
                return Results.BadRequest(new { error = "No file uploaded." });
            }

            if (file.Length > MaxFileSize)
            {
                return Results.BadRequest(new { error = "File exceeds 10 MB limit." });
            }

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions[cat].Contains(extension))
            {
                string allowed = string.Join(", ", AllowedExtensions[cat]);
                return Results.BadRequest(new { error = $"File type '{extension}' not allowed. Allowed: {allowed}" });
            }

            string safeName = SanitizeFileName(file.FileName);
            string targetDir = cat == "sounds" ? WrkzgPaths.SoundsDirectory : WrkzgPaths.ImagesDirectory;
            Directory.CreateDirectory(targetDir);

            string targetPath = Path.Combine(targetDir, safeName);
            if (File.Exists(targetPath))
            {
                string nameOnly = Path.GetFileNameWithoutExtension(safeName);
                safeName = $"{nameOnly}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{extension}";
                targetPath = Path.Combine(targetDir, safeName);
            }

            using (FileStream fs = new(targetPath, FileMode.Create))
            {
                await file.CopyToAsync(fs, ct);
            }

            return Results.Ok(new
            {
                fileName = safeName,
                url = $"/assets/{cat}/{safeName}",
                category = cat,
                size = file.Length
            });
        }).DisableAntiforgery();

        // List all assets in a category
        group.MapGet("/{category}", (string category) =>
        {
            string cat = category.ToLowerInvariant();
            if (!AllowedExtensions.ContainsKey(cat))
            {
                return Results.BadRequest(new { error = "Category must be 'sounds' or 'images'." });
            }

            string dir = cat == "sounds" ? WrkzgPaths.SoundsDirectory : WrkzgPaths.ImagesDirectory;
            if (!Directory.Exists(dir))
            {
                return Results.Ok(Array.Empty<object>());
            }

            string[] allowed = AllowedExtensions[cat];
            object[] files = Directory.GetFiles(dir)
                .Where(f => allowed.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .Select(f => new
                {
                    fileName = Path.GetFileName(f),
                    url = $"/assets/{cat}/{Path.GetFileName(f)}",
                    size = new FileInfo(f).Length,
                    lastModified = File.GetLastWriteTimeUtc(f)
                })
                .OrderByDescending(f => f.lastModified)
                .ToArray<object>();

            return Results.Ok(files);
        });

        // Delete an asset
        group.MapDelete("/{category}/{fileName}", (string category, string fileName) =>
        {
            string cat = category.ToLowerInvariant();
            if (!AllowedExtensions.ContainsKey(cat))
            {
                return Results.BadRequest(new { error = "Category must be 'sounds' or 'images'." });
            }

            if (fileName.Contains("..") || fileName.Contains('/') || fileName.Contains('\\'))
            {
                return Results.BadRequest(new { error = "Invalid file name." });
            }

            string dir = cat == "sounds" ? WrkzgPaths.SoundsDirectory : WrkzgPaths.ImagesDirectory;
            string filePath = Path.Combine(dir, fileName);

            if (!File.Exists(filePath))
            {
                return Results.NotFound();
            }

            File.Delete(filePath);
            return Results.NoContent();
        });
    }

    public static string SanitizeFileName(string fileName)
    {
        string name = Path.GetFileNameWithoutExtension(fileName);
        string ext = Path.GetExtension(fileName).ToLowerInvariant();

        string safe = new string(name
            .Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_')
            .ToArray());

        if (string.IsNullOrWhiteSpace(safe))
        {
            safe = $"asset_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        }

        return safe + ext;
    }
}

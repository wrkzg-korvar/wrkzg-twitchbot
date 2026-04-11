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

    /// <summary>Registers asset upload and management API endpoints.</summary>
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
                return TypedResults.Problem(detail: "Category must be 'sounds' or 'images'.", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
            }

            IFormFile? file = request.Form.Files.GetFile("file");
            if (file is null || file.Length == 0)
            {
                return TypedResults.Problem(detail: "No file uploaded.", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
            }

            if (file.Length > MaxFileSize)
            {
                return TypedResults.Problem(detail: "File exceeds 10 MB limit.", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
            }

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions[cat].Contains(extension))
            {
                string allowed = string.Join(", ", AllowedExtensions[cat]);
                return TypedResults.Problem(detail: $"File type '{extension}' not allowed. Allowed: {allowed}", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
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
                return TypedResults.Problem(detail: "Category must be 'sounds' or 'images'.", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
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
                return TypedResults.Problem(detail: "Category must be 'sounds' or 'images'.", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
            }

            if (fileName.Contains("..") || fileName.Contains('/') || fileName.Contains('\\'))
            {
                return TypedResults.Problem(detail: "Invalid file name.", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
            }

            string dir = cat == "sounds" ? WrkzgPaths.SoundsDirectory : WrkzgPaths.ImagesDirectory;
            string filePath = Path.Combine(dir, fileName);

            if (!File.Exists(filePath))
            {
                return TypedResults.Problem(title: "Not Found", statusCode: StatusCodes.Status404NotFound, type: "https://wrkzg.app/problems/not-found");
            }

            File.Delete(filePath);
            return Results.NoContent();
        });
    }

    /// <summary>Sanitizes a file name by removing special characters and lowercasing the extension.</summary>
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

using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Infrastructure.Import;

namespace Wrkzg.Api.Endpoints;

/// <summary>
/// REST endpoints for bot data import.
/// </summary>
public static class ImportEndpoints
{
    /// <summary>Registers bot data import preview and execution API endpoints.</summary>
    public static void MapImportEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/import").WithTags("Import");

        // Preview import (dry run — no DB writes)
        group.MapPost("/preview", async (
            HttpRequest request,
            IDataImportService importService,
            CancellationToken ct) =>
        {
            IFormFile? file = request.Form.Files.GetFile("file");
            if (file is null)
            {
                return TypedResults.Problem(detail: "No file uploaded.", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
            }

            ImportConfiguration config = ParseConfig(request);

            using System.IO.Stream stream = file.OpenReadStream();
            ImportResult result = await importService.PreviewAsync(stream, config, ct);
            return Results.Ok(result);
        }).DisableAntiforgery();

        // Execute import (writes to DB)
        group.MapPost("/execute", async (
            HttpRequest request,
            IDataImportService importService,
            CancellationToken ct) =>
        {
            IFormFile? file = request.Form.Files.GetFile("file");
            if (file is null)
            {
                return TypedResults.Problem(detail: "No file uploaded.", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
            }

            ImportConfiguration config = ParseConfig(request);

            using System.IO.Stream stream = file.OpenReadStream();
            ImportResult result = await importService.ExecuteAsync(stream, config, ct);
            return Results.Ok(result);
        }).DisableAntiforgery();

        // Preview CSV columns (for generic CSV mapping)
        group.MapPost("/preview-columns", async (HttpRequest request, CancellationToken ct) =>
        {
            IFormFile? file = request.Form.Files.GetFile("file");
            if (file is null)
            {
                return TypedResults.Problem(detail: "No file uploaded.", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
            }

            bool hasHeader = request.Form.ContainsKey("hasHeader")
                && request.Form["hasHeader"] == "true";
            char delimiter = request.Form.ContainsKey("delimiter")
                && request.Form["delimiter"].ToString().Length > 0
                ? request.Form["delimiter"].ToString()[0]
                : ',';

            using System.IO.Stream stream = file.OpenReadStream();
            CsvPreview preview = await GenericCsvParser.PreviewColumnsAsync(
                stream, hasHeader, delimiter, previewRows: 5, ct);

            return Results.Ok(preview);
        }).DisableAntiforgery();

        // Start async import (returns immediately with job ID)
        group.MapPost("/start", async (
            HttpRequest request,
            IImportJobService jobService,
            CancellationToken ct) =>
        {
            IFormFile? file = request.Form.Files.GetFile("file");
            if (file is null)
            {
                return TypedResults.Problem(detail: "No file uploaded.", title: "Validation Error", statusCode: StatusCodes.Status400BadRequest, type: "https://wrkzg.app/problems/validation-error");
            }

            ImportConfiguration config = ParseConfig(request);

            using Stream stream = file.OpenReadStream();

            try
            {
                ImportJob job = await jobService.StartAsync(stream, config, ct);
                return Results.Ok(new { jobId = job.Id });
            }
            catch (InvalidOperationException ex)
            {
                return TypedResults.Problem(detail: ex.Message, title: "Conflict", statusCode: StatusCodes.Status409Conflict, type: "https://wrkzg.app/problems/import-conflict");
            }
        }).DisableAntiforgery();

        // Get all jobs
        group.MapGet("/jobs", (IImportJobService jobService) =>
        {
            return Results.Ok(jobService.GetAll());
        });

        // Get specific job
        group.MapGet("/jobs/{id}", (string id, IImportJobService jobService) =>
        {
            ImportJob? job = jobService.GetById(id);
            return job is not null ? Results.Ok(job) : Results.NotFound();
        });

        // Cancel job
        group.MapDelete("/jobs/{id}", (string id, IImportJobService jobService) =>
        {
            return jobService.Cancel(id)
                ? Results.Ok(new { cancelled = true })
                : Results.NotFound();
        });

        // Get locked modules
        group.MapGet("/locks", (IImportJobService jobService) =>
        {
            return Results.Ok(new { lockedModules = jobService.GetLockedModules() });
        });

        // Available import templates
        group.MapGet("/templates", () =>
        {
            return Results.Ok(new[]
            {
                new
                {
                    id = "deepbot_csv",
                    name = "Deepbot (CSV)",
                    sourceType = ImportSourceType.DeepbotCsv,
                    description = "3 columns: Username, Points, Minutes Watched. No header row.",
                    fields = new[] { "Username", "Points", "Watch Time (Minutes)" },
                    fileTypes = new[] { ".csv" },
                    fileHint = "Exported CSV file from DeepBot"
                },
                new
                {
                    id = "deepbot_json",
                    name = "Deepbot (JSON)",
                    sourceType = ImportSourceType.DeepbotJson,
                    description = "Full export with VIP levels, mod status, join date, and last seen.",
                    fields = new[] { "Username", "Points", "Watch Time", "VIP Level", "Mod Status", "Join Date", "Last Seen" },
                    fileTypes = new[] { ".json" },
                    fileHint = "Exported JSON file from DeepBot WebSocket API"
                },
                new
                {
                    id = "streamlabs",
                    name = "Streamlabs Chatbot",
                    sourceType = ImportSourceType.StreamlabsChatbot,
                    description = "CSV export with header. Columns: Username, Points, Hours.",
                    fields = new[] { "Username", "Points", "Watch Time (Hours)" },
                    fileTypes = new[] { ".csv" },
                    fileHint = "Exported CSV from Streamlabs Chatbot settings"
                },
                new
                {
                    id = "generic_csv",
                    name = "Generic CSV",
                    sourceType = ImportSourceType.GenericCsv,
                    description = "Any CSV file with custom column mapping.",
                    fields = new[] { "Username", "Points (optional)", "Watch Time (optional)" },
                    fileTypes = new[] { ".csv" },
                    fileHint = "Any .csv file with user data"
                },
                new
                {
                    id = "deepbot_bin",
                    name = "Deepbot Users (Save File)",
                    sourceType = ImportSourceType.DeepbotBin,
                    description = "DeepBot users*.bin save file. Contains usernames, points, watch time, and (if available) display names and Twitch IDs.",
                    fields = new[] { "Username", "Points", "Watch Time (Minutes)", "Display Name", "Twitch ID" },
                    fileTypes = new[] { ".bin" },
                    fileHint = "File: users*.bin (e.g. users20260404-185357.bin)"
                },
                new
                {
                    id = "deepbot_bin_config",
                    name = "Deepbot Commands & Quotes (Save File)",
                    sourceType = ImportSourceType.DeepbotBinConfig,
                    description = "DeepBot chanmsgconfig*.bin save file. Contains custom chat commands, quotes, and timed messages.",
                    fields = new[] { "Commands", "Quotes", "Timed Messages" },
                    fileTypes = new[] { ".bin" },
                    fileHint = "File: chanmsgconfig*.bin (e.g. chanmsgconfig20260404-135348.bin)"
                }
            });
        });
    }

    private static ImportConfiguration ParseConfig(HttpRequest request)
    {
        string? configJson = request.Form.ContainsKey("config")
            ? request.Form["config"].ToString()
            : null;

        if (!string.IsNullOrWhiteSpace(configJson))
        {
            return JsonSerializer.Deserialize<ImportConfiguration>(configJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new ImportConfiguration();
        }

        return new ImportConfiguration { SourceType = ImportSourceType.DeepbotCsv };
    }
}

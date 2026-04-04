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
                return Results.BadRequest(new { error = "No file uploaded." });
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
                return Results.BadRequest(new { error = "No file uploaded." });
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
                return Results.BadRequest(new { error = "No file uploaded." });
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
                    fileTypes = new[] { ".csv" }
                },
                new
                {
                    id = "deepbot_json",
                    name = "Deepbot (JSON)",
                    sourceType = ImportSourceType.DeepbotJson,
                    description = "Full export with VIP levels, mod status, join date, and last seen.",
                    fields = new[] { "Username", "Points", "Watch Time", "VIP Level", "Mod Status", "Join Date", "Last Seen" },
                    fileTypes = new[] { ".json" }
                },
                new
                {
                    id = "streamlabs",
                    name = "Streamlabs Chatbot",
                    sourceType = ImportSourceType.StreamlabsChatbot,
                    description = "CSV export with header. Columns: Username, Points, Hours.",
                    fields = new[] { "Username", "Points", "Watch Time (Hours)" },
                    fileTypes = new[] { ".csv" }
                },
                new
                {
                    id = "generic_csv",
                    name = "Generic CSV",
                    sourceType = ImportSourceType.GenericCsv,
                    description = "Any CSV file with custom column mapping.",
                    fields = new[] { "Username", "Points (optional)", "Watch Time (optional)" },
                    fileTypes = new[] { ".csv" }
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

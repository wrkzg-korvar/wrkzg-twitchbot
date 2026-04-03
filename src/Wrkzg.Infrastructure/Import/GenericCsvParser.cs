using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Wrkzg.Infrastructure.Import;

/// <summary>
/// Parses generic CSV files with user-defined column mapping.
/// Supports files from Streamlabs Chatbot, PhantomBot, or custom exports.
/// </summary>
public static class GenericCsvParser
{
    /// <summary>
    /// Reads the first N rows to detect column structure.
    /// Returns header names (if present) or column indices.
    /// </summary>
    public static async Task<CsvPreview> PreviewColumnsAsync(
        Stream stream,
        bool hasHeader,
        char delimiter = ',',
        int previewRows = 5,
        CancellationToken ct = default)
    {
        CsvPreview preview = new();
        using StreamReader reader = new(stream);
        int rowCount = 0;

        // Read header
        if (hasHeader && await reader.ReadLineAsync(ct) is { } headerLine)
        {
            preview.Headers = SplitLine(headerLine, delimiter);
            preview.ColumnCount = preview.Headers.Length;
        }

        // Read sample rows
        while (rowCount < previewRows && await reader.ReadLineAsync(ct) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            string[] columns = SplitLine(line, delimiter);
            preview.SampleRows.Add(columns);

            if (preview.ColumnCount == 0)
            {
                preview.ColumnCount = columns.Length;
            }

            rowCount++;
        }

        // Count remaining rows
        preview.TotalRows = rowCount;
        while (await reader.ReadLineAsync(ct) is not null)
        {
            preview.TotalRows++;
        }

        if (hasHeader)
        {
            preview.TotalRows++; // Include header in total
        }

        return preview;
    }

    /// <summary>
    /// Parses the CSV file with the given column mapping.
    /// </summary>
    public static async Task<List<ImportUserRecord>> ParseAsync(
        Stream stream,
        Dictionary<string, string> columnMapping,
        bool hasHeader,
        char delimiter = ',',
        CancellationToken ct = default)
    {
        List<ImportUserRecord> records = new();
        using StreamReader reader = new(stream);
        int lineNumber = 0;
        string[]? headers = null;

        if (hasHeader && await reader.ReadLineAsync(ct) is { } headerLine)
        {
            headers = SplitLine(headerLine, delimiter);
            lineNumber++;
        }

        int usernameCol = ResolveColumnIndex("username", columnMapping, headers);
        int pointsCol = ResolveColumnIndex("points", columnMapping, headers);
        int watchedMinutesCol = ResolveColumnIndex("watchedMinutes", columnMapping, headers);

        if (usernameCol < 0)
        {
            return records;
        }

        while (await reader.ReadLineAsync(ct) is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            string[] columns = SplitLine(line, delimiter);

            string username = usernameCol < columns.Length
                ? columns[usernameCol].Trim().ToLowerInvariant()
                : string.Empty;

            if (string.IsNullOrWhiteSpace(username))
            {
                continue;
            }

            long points = 0;
            if (pointsCol >= 0 && pointsCol < columns.Length)
            {
                if (double.TryParse(columns[pointsCol].Trim(), NumberStyles.Float,
                    CultureInfo.InvariantCulture, out double p))
                {
                    points = (long)Math.Round(Math.Max(p, 0));
                }
            }

            int watchedMinutes = 0;
            if (watchedMinutesCol >= 0 && watchedMinutesCol < columns.Length)
            {
                if (double.TryParse(columns[watchedMinutesCol].Trim(), NumberStyles.Float,
                    CultureInfo.InvariantCulture, out double m))
                {
                    // Check if the mapping key says "hours" — Streamlabs uses hours
                    string? watchMapping = columnMapping.GetValueOrDefault("watchedMinutes");
                    bool isHours = watchMapping is not null &&
                        watchMapping.Contains("hour", StringComparison.OrdinalIgnoreCase);

                    watchedMinutes = isHours
                        ? (int)Math.Round(Math.Max(m, 0) * 60)
                        : (int)Math.Round(Math.Max(m, 0));
                }
            }

            records.Add(new ImportUserRecord
            {
                Username = username,
                Points = points,
                WatchedMinutes = watchedMinutes,
                LineNumber = lineNumber
            });
        }

        return records;
    }

    private static int ResolveColumnIndex(
        string fieldName,
        Dictionary<string, string> mapping,
        string[]? headers)
    {
        if (!mapping.TryGetValue(fieldName, out string? value) || value is null)
        {
            return -1;
        }

        // Try as numeric index first
        if (int.TryParse(value, out int index))
        {
            return index;
        }

        // Try as header name
        if (headers is not null)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                if (string.Equals(headers[i].Trim(), value.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
        }

        return -1;
    }

    private static string[] SplitLine(string line, char delimiter)
    {
        return line.Split(delimiter);
    }
}

public class CsvPreview
{
    public string[] Headers { get; set; } = Array.Empty<string>();
    public List<string[]> SampleRows { get; set; } = new();
    public int ColumnCount { get; set; }
    public int TotalRows { get; set; }
}

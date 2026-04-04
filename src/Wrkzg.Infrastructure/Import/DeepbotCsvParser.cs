using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Wrkzg.Infrastructure.Import;

/// <summary>
/// Parses Deepbot CSV export files.
/// Format: Username,Points,MinutesWatched (no header, 3 columns)
/// </summary>
public static class DeepbotCsvParser
{
    /// <summary>Parses a Deepbot CSV stream into a list of import user records.</summary>
    public static async Task<List<ImportUserRecord>> ParseAsync(
        Stream stream,
        CancellationToken ct = default)
    {
        List<ImportUserRecord> records = new();
        using StreamReader reader = new(stream);
        int lineNumber = 0;

        while (await reader.ReadLineAsync(ct) is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            string[] parts = line.Split(',');
            if (parts.Length < 3)
            {
                continue;
            }

            string username = parts[0].Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(username))
            {
                continue;
            }

            if (!double.TryParse(parts[1].Trim(), NumberStyles.Float,
                CultureInfo.InvariantCulture, out double points))
            {
                points = 0;
            }

            if (!double.TryParse(parts[2].Trim(), NumberStyles.Float,
                CultureInfo.InvariantCulture, out double minutes))
            {
                minutes = 0;
            }

            // Negative values make no sense — clamp to 0
            if (points < 0)
            {
                points = 0;
            }

            if (minutes < 0)
            {
                minutes = 0;
            }

            records.Add(new ImportUserRecord
            {
                Username = username,
                Points = (long)Math.Round(points),
                WatchedMinutes = (int)Math.Round(minutes),
                LineNumber = lineNumber
            });
        }

        return records;
    }
}

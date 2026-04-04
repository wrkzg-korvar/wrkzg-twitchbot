using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Wrkzg.Infrastructure.Import;

/// <summary>
/// Parses Deepbot JSON exports (from WebSocket API get_users response).
/// Supports both a raw JSON array and the wrapped API response format.
/// </summary>
public static class DeepbotJsonParser
{
    /// <summary>Parses a Deepbot JSON stream into a list of import user records.</summary>
    public static async Task<List<ImportUserRecord>> ParseAsync(
        Stream stream,
        CancellationToken ct = default)
    {
        using StreamReader reader = new(stream);
        string json = await reader.ReadToEndAsync(ct);
        json = json.Trim();

        List<ImportUserRecord> records = new();

        // Try to parse as array first, then as wrapped response
        JsonElement root;
        try
        {
            root = JsonDocument.Parse(json).RootElement;
        }
        catch (JsonException)
        {
            return records;
        }

        // If it's a wrapped response like {"msg": [...]}, extract the array
        JsonElement usersArray;
        if (root.ValueKind == JsonValueKind.Array)
        {
            usersArray = root;
        }
        else if (root.TryGetProperty("msg", out JsonElement msg) && msg.ValueKind == JsonValueKind.Array)
        {
            usersArray = msg;
        }
        else
        {
            return records;
        }

        int lineNumber = 0;
        foreach (JsonElement entry in usersArray.EnumerateArray())
        {
            lineNumber++;
            ct.ThrowIfCancellationRequested();

            string? username = entry.TryGetProperty("user", out JsonElement userProp)
                ? userProp.GetString()?.Trim().ToLowerInvariant()
                : null;

            if (string.IsNullOrWhiteSpace(username))
            {
                continue;
            }

            double points = entry.TryGetProperty("points", out JsonElement pointsProp)
                ? pointsProp.GetDouble()
                : 0;

            double watchTime = entry.TryGetProperty("watch_time", out JsonElement watchProp)
                ? watchProp.GetDouble()
                : 0;

            int vipRaw = entry.TryGetProperty("vip", out JsonElement vipProp)
                ? vipProp.GetInt32()
                : 0;

            int modRaw = entry.TryGetProperty("mod", out JsonElement modProp)
                ? modProp.GetInt32()
                : 0;

            DateTimeOffset? joinDate = ParseDate(entry, "join_date");
            DateTimeOffset? lastSeen = ParseDate(entry, "last_seen");

            records.Add(new ImportUserRecord
            {
                Username = username,
                Points = (long)Math.Round(Math.Max(points, 0)),
                WatchedMinutes = (int)Math.Round(Math.Max(watchTime, 0)),
                VipLevel = MapVipLevel(vipRaw),
                ModLevel = modRaw >= 1 && modRaw != 4 ? modRaw : null, // 4 = Bot itself
                JoinDate = joinDate,
                LastSeen = lastSeen,
                LineNumber = lineNumber
            });
        }

        return records;
    }

    /// <summary>
    /// Maps Deepbot VIP levels to a normalized level.
    /// Deepbot: 0/10 = Regular, 1 = Bronze, 2 = Silver, 3 = Gold
    /// </summary>
    private static int? MapVipLevel(int deepbotVip)
    {
        return deepbotVip switch
        {
            1 => 1,
            2 => 2,
            3 => 3,
            _ => null
        };
    }

    private static DateTimeOffset? ParseDate(JsonElement entry, string propertyName)
    {
        if (!entry.TryGetProperty(propertyName, out JsonElement prop))
        {
            return null;
        }

        string? dateStr = prop.GetString();
        if (string.IsNullOrWhiteSpace(dateStr))
        {
            return null;
        }

        if (DateTimeOffset.TryParse(dateStr, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out DateTimeOffset result))
        {
            return result;
        }

        return null;
    }
}

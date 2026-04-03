using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

#pragma warning disable CA1848 // Use LoggerMessage delegates

namespace Wrkzg.Infrastructure.Import;

/// <summary>
/// Orchestrates the entire import process:
/// 1. Parse file (based on SourceType)
/// 2. Create or merge users in Wrkzg DB
/// 3. Optionally map VIP levels to Roles
/// 4. Return result summary
/// </summary>
public class DataImportService : IDataImportService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DataImportService> _logger;

    public DataImportService(
        IServiceScopeFactory scopeFactory,
        ILogger<DataImportService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<ImportResult> PreviewAsync(
        Stream fileStream, ImportConfiguration config, CancellationToken ct = default)
    {
        List<ImportUserRecord> records = await ParseRecordsAsync(fileStream, config, ct);

        ImportResult result = new()
        {
            TotalRows = records.Count,
            ImportedCount = records.Count(r => !string.IsNullOrWhiteSpace(r.Username)),
            SkippedCount = records.Count(r => string.IsNullOrWhiteSpace(r.Username))
        };

        using IServiceScope scope = _scopeFactory.CreateScope();
        IUserRepository users = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        foreach (ImportUserRecord record in records)
        {
            if (string.IsNullOrWhiteSpace(record.Username))
            {
                continue;
            }

            User? existing = await users.GetByUsernameAsync(record.Username, ct);
            if (existing is not null)
            {
                result.UpdatedCount++;
            }
            else
            {
                result.CreatedCount++;
            }
        }

        // Count VIP role assignments for preview
        if (config.MapVipToRoles && config.VipRoleMapping is not null)
        {
            result.RolesAssignedCount = records.Count(r =>
                r.VipLevel.HasValue && r.VipLevel > 0 &&
                config.VipRoleMapping.ContainsKey(r.VipLevel.Value));
        }

        return result;
    }

    public async Task<ImportResult> ExecuteAsync(
        Stream fileStream, ImportConfiguration config, CancellationToken ct = default)
    {
        List<ImportUserRecord> records = await ParseRecordsAsync(fileStream, config, ct);
        ImportResult result = new() { TotalRows = records.Count };

        using IServiceScope scope = _scopeFactory.CreateScope();
        IUserRepository users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        IRoleRepository? roles = config.MapVipToRoles
            ? scope.ServiceProvider.GetRequiredService<IRoleRepository>()
            : null;

        foreach (ImportUserRecord record in records)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(record.Username))
                {
                    result.SkippedCount++;
                    continue;
                }

                await ImportSingleUserAsync(record, config, users, roles, result, ct);
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ImportRowError
                {
                    RowNumber = record.LineNumber,
                    Field = "general",
                    Message = ex.Message,
                    Severity = ImportErrorSeverity.Warning
                });
                result.SkippedCount++;
            }
        }

        _logger.LogInformation("Import completed: {Summary}", result.Summary);
        return result;
    }

    private async Task ImportSingleUserAsync(
        ImportUserRecord record,
        ImportConfiguration config,
        IUserRepository users,
        IRoleRepository? roles,
        ImportResult result,
        CancellationToken ct)
    {
        User? existing = await users.GetByUsernameAsync(record.Username, ct);

        if (existing is not null)
        {
            switch (config.ConflictStrategy)
            {
                case ImportConflictStrategy.Skip:
                    result.SkippedCount++;
                    return;

                case ImportConflictStrategy.Overwrite:
                    existing.Points = record.Points;
                    existing.WatchedMinutes = record.WatchedMinutes;
                    if (record.LastSeen.HasValue)
                    {
                        existing.LastSeenAt = record.LastSeen.Value;
                    }
                    break;

                case ImportConflictStrategy.KeepHigher:
                    existing.Points = Math.Max(existing.Points, record.Points);
                    existing.WatchedMinutes = Math.Max(existing.WatchedMinutes, record.WatchedMinutes);
                    break;

                case ImportConflictStrategy.Add:
                    existing.Points += record.Points;
                    existing.WatchedMinutes += record.WatchedMinutes;
                    break;
            }

            if (record.ModLevel.HasValue && record.ModLevel >= 1)
            {
                existing.IsMod = true;
            }

            await users.UpdateAsync(existing, ct);
            result.UpdatedCount++;
        }
        else
        {
            User newUser = new()
            {
                TwitchId = $"imported_{record.Username}",
                Username = record.Username,
                DisplayName = record.Username,
                Points = record.Points,
                WatchedMinutes = record.WatchedMinutes,
                MessageCount = 0,
                IsMod = record.ModLevel.HasValue && record.ModLevel >= 1,
                IsSubscriber = false,
                IsBroadcaster = false,
                LastSeenAt = record.LastSeen ?? DateTimeOffset.UtcNow,
                FirstSeenAt = record.JoinDate ?? DateTimeOffset.UtcNow
            };

            await users.CreateAsync(newUser, ct);
            result.CreatedCount++;
            existing = newUser;
        }

        // VIP to Role mapping (only for Deepbot JSON with VIP data)
        if (config.MapVipToRoles
            && record.VipLevel.HasValue
            && record.VipLevel > 0
            && config.VipRoleMapping is not null
            && roles is not null)
        {
            if (config.VipRoleMapping.TryGetValue(record.VipLevel.Value, out int roleId))
            {
                await roles.AssignRoleAsync(existing.Id, roleId, isAutoAssigned: false, ct);
                result.RolesAssignedCount++;
            }
        }

        result.ImportedCount++;
    }

    private static async Task<List<ImportUserRecord>> ParseRecordsAsync(
        Stream stream, ImportConfiguration config, CancellationToken ct)
    {
        return config.SourceType switch
        {
            ImportSourceType.DeepbotCsv => await DeepbotCsvParser.ParseAsync(stream, ct),
            ImportSourceType.DeepbotJson => await DeepbotJsonParser.ParseAsync(stream, ct),
            ImportSourceType.StreamlabsChatbot => await GenericCsvParser.ParseAsync(
                stream,
                new Dictionary<string, string>
                {
                    { "username", "0" },
                    { "points", "1" },
                    { "watchedMinutes", "2" }
                },
                hasHeader: true,
                delimiter: ',',
                ct),
            ImportSourceType.GenericCsv => await GenericCsvParser.ParseAsync(
                stream,
                config.ColumnMapping ?? new Dictionary<string, string>(),
                config.HasHeader,
                config.Delimiter,
                ct),
            _ => throw new ArgumentException($"Unsupported source type: {config.SourceType}")
        };
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Infrastructure.Data;

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

    /// <summary>
    /// Initializes a new instance of the <see cref="DataImportService"/> class.
    /// </summary>
    /// <param name="scopeFactory">Factory for creating scoped service providers to access repositories.</param>
    /// <param name="logger">The logger for import diagnostics.</param>
    public DataImportService(
        IServiceScopeFactory scopeFactory,
        ILogger<DataImportService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Previews an import without modifying data, returning counts of records
    /// that would be created, updated, or skipped.
    /// </summary>
    public async Task<ImportResult> PreviewAsync(
        Stream fileStream, ImportConfiguration config, CancellationToken ct = default)
    {
        if (config.SourceType == ImportSourceType.DeepbotBinConfig)
        {
            return await PreviewConfigImportAsync(fileStream, ct);
        }

        List<ImportUserRecord> records = await ParseRecordsAsync(fileStream, config, ct);

        List<ImportUserRecord> validRecords = records
            .Where(r => !string.IsNullOrWhiteSpace(r.Username))
            .ToList();

        ImportResult result = new()
        {
            TotalRows = records.Count,
            ImportedCount = validRecords.Count,
            SkippedCount = records.Count - validRecords.Count
        };

        if (validRecords.Count == 0)
        {
            return result;
        }

        // Load all existing usernames in one query (HashSet for O(1) lookup)
        using IServiceScope scope = _scopeFactory.CreateScope();
        BotDbContext db = scope.ServiceProvider.GetRequiredService<BotDbContext>();

        HashSet<string> existingUsernames = (await db.Users
            .AsNoTracking()
            .Select(u => u.Username)
            .ToListAsync(ct))
            .Select(u => u.ToLowerInvariant())
            .ToHashSet();

        foreach (ImportUserRecord record in validRecords)
        {
            if (existingUsernames.Contains(record.Username.ToLowerInvariant()))
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

    /// <summary>
    /// Executes the import, creating or merging users and optionally assigning VIP roles.
    /// </summary>
    public Task<ImportResult> ExecuteAsync(
        Stream fileStream, ImportConfiguration config, CancellationToken ct = default)
    {
        return ExecuteAsync(fileStream, config, null, ct);
    }

    /// <summary>
    /// Executes the import with progress reporting, creating or merging users and optionally assigning VIP roles.
    /// </summary>
    public async Task<ImportResult> ExecuteAsync(
        Stream fileStream, ImportConfiguration config, IProgress<int>? progress, CancellationToken ct = default)
    {
        if (config.SourceType == ImportSourceType.DeepbotBinConfig)
        {
            return await ExecuteConfigImportAsync(fileStream, ct);
        }

        List<ImportUserRecord> records = await ParseRecordsAsync(fileStream, config, ct);
        ImportResult result = new() { TotalRows = records.Count };

        // Filter out empty usernames upfront
        List<ImportUserRecord> validRecords = records
            .Where(r => !string.IsNullOrWhiteSpace(r.Username))
            .ToList();
        result.SkippedCount = records.Count - validRecords.Count;

        if (validRecords.Count == 0)
        {
            return result;
        }

        using IServiceScope scope = _scopeFactory.CreateScope();
        BotDbContext db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        IRoleRepository? roles = config.MapVipToRoles
            ? scope.ServiceProvider.GetRequiredService<IRoleRepository>()
            : null;

        // Temporary SQLite optimizations for bulk import
        await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode = WAL", ct);
        await db.Database.ExecuteSqlRawAsync("PRAGMA synchronous = NORMAL", ct);

        try
        {
            // STEP 1: Load ALL existing users into memory (one query)
            _logger.LogInformation("Import: Loading existing users from database...");
            Dictionary<string, User> existingUsers = await db.Users
                .AsNoTracking()
                .ToDictionaryAsync(u => u.Username.ToLowerInvariant(), ct);
            _logger.LogInformation("Import: {Count} existing users loaded", existingUsers.Count);

            // STEP 2: Classify records into creates vs updates (in-memory)
            List<User> toCreate = new();
            List<(User existing, ImportUserRecord record)> toUpdate = new();

            foreach (ImportUserRecord record in validRecords)
            {
                string key = record.Username.ToLowerInvariant();

                if (existingUsers.TryGetValue(key, out User? existing))
                {
                    if (config.ConflictStrategy == ImportConflictStrategy.Skip)
                    {
                        result.SkippedCount++;
                    }
                    else
                    {
                        toUpdate.Add((existing, record));
                    }
                }
                else
                {
                    User newUser = new()
                    {
                        TwitchId = !string.IsNullOrWhiteSpace(record.TwitchId)
                            ? record.TwitchId
                            : $"imported_{record.Username}",
                        Username = record.Username,
                        DisplayName = !string.IsNullOrWhiteSpace(record.DisplayName)
                            ? record.DisplayName
                            : record.Username,
                        Points = record.Points,
                        WatchedMinutes = record.WatchedMinutes,
                        MessageCount = 0,
                        IsMod = record.ModLevel.HasValue && record.ModLevel >= 1,
                        IsSubscriber = false,
                        IsBroadcaster = false,
                        LastSeenAt = record.LastSeen ?? DateTimeOffset.UtcNow,
                        FirstSeenAt = record.JoinDate ?? DateTimeOffset.UtcNow
                    };
                    toCreate.Add(newUser);
                    // Add to lookup so subsequent duplicates in the SAME file are caught
                    existingUsers[key] = newUser;
                }
            }

            int totalWork = toCreate.Count + toUpdate.Count;

            // STEP 3: Bulk insert new users in batches of 500
            _logger.LogInformation("Import: Creating {Count} new users in batches...", toCreate.Count);
            const int batchSize = 500;

            for (int i = 0; i < toCreate.Count; i += batchSize)
            {
                List<User> batch = toCreate.GetRange(i, Math.Min(batchSize, toCreate.Count - i));
                db.Users.AddRange(batch);
                await db.SaveChangesAsync(ct);
                // Detach to free memory (we don't need change tracking after save)
                foreach (User u in batch)
                {
                    db.Entry(u).State = EntityState.Detached;
                }
                result.CreatedCount += batch.Count;

                // Progress: Create-Phase = 0-60%
                if (totalWork > 0)
                {
                    int done = result.CreatedCount;
                    progress?.Report((int)((double)done / totalWork * 60));
                }
            }

            // STEP 4: Bulk update existing users in batches of 500
            if (toUpdate.Count > 0)
            {
                _logger.LogInformation("Import: Updating {Count} existing users in batches...", toUpdate.Count);

                for (int i = 0; i < toUpdate.Count; i += batchSize)
                {
                    List<(User existing, ImportUserRecord record)> batch =
                        toUpdate.GetRange(i, Math.Min(batchSize, toUpdate.Count - i));

                    // Load this batch of users by ID (tracked, so EF Core can generate UPDATEs)
                    List<int> ids = batch.Select(b => b.existing.Id).ToList();
                    List<User> tracked = await db.Users
                        .Where(u => ids.Contains(u.Id))
                        .ToListAsync(ct);

                    Dictionary<int, ImportUserRecord> recordMap = batch
                        .ToDictionary(b => b.existing.Id, b => b.record);

                    foreach (User user in tracked)
                    {
                        if (!recordMap.TryGetValue(user.Id, out ImportUserRecord? record))
                        {
                            continue;
                        }

                        switch (config.ConflictStrategy)
                        {
                            case ImportConflictStrategy.Overwrite:
                                user.Points = record.Points;
                                user.WatchedMinutes = record.WatchedMinutes;
                                if (record.LastSeen.HasValue)
                                {
                                    user.LastSeenAt = record.LastSeen.Value;
                                }
                                break;

                            case ImportConflictStrategy.KeepHigher:
                                user.Points = Math.Max(user.Points, record.Points);
                                user.WatchedMinutes = Math.Max(user.WatchedMinutes, record.WatchedMinutes);
                                break;

                            case ImportConflictStrategy.Add:
                                user.Points += record.Points;
                                user.WatchedMinutes += record.WatchedMinutes;
                                break;
                        }

                        if (record.ModLevel.HasValue && record.ModLevel >= 1)
                        {
                            user.IsMod = true;
                        }
                    }

                    await db.SaveChangesAsync(ct);

                    // Detach to free memory
                    foreach (User u in tracked)
                    {
                        db.Entry(u).State = EntityState.Detached;
                    }

                    result.UpdatedCount += tracked.Count;

                    // Progress: Update-Phase = 60-95%
                    if (totalWork > 0 && toUpdate.Count > 0)
                    {
                        progress?.Report(60 + (int)((double)result.UpdatedCount / toUpdate.Count * 35));
                    }
                }
            }

            result.ImportedCount = result.CreatedCount + result.UpdatedCount;

            // STEP 5: VIP role mapping (batch)
            if (config.MapVipToRoles && config.VipRoleMapping is not null && roles is not null)
            {
                List<ImportUserRecord> vipRecords = validRecords
                    .Where(r => r.VipLevel.HasValue && r.VipLevel > 0
                        && config.VipRoleMapping.ContainsKey(r.VipLevel.Value))
                    .ToList();

                foreach (ImportUserRecord vipRecord in vipRecords)
                {
                    try
                    {
                        User? user = await db.Users
                            .AsNoTracking()
                            .FirstOrDefaultAsync(u => u.Username == vipRecord.Username, ct);

                        if (user is not null && config.VipRoleMapping.TryGetValue(vipRecord.VipLevel!.Value, out int roleId))
                        {
                            await roles.AssignRoleAsync(user.Id, roleId, isAutoAssigned: false, ct);
                            result.RolesAssignedCount++;
                        }
                    }
                    catch
                    {
                        // Non-critical — skip role assignment failures
                    }
                }
            }

            progress?.Report(100);
        }
        finally
        {
            // Restore safe defaults
            await db.Database.ExecuteSqlRawAsync("PRAGMA synchronous = FULL", ct);
        }

        _logger.LogInformation("Import completed: {Summary}", result.Summary);
        return result;
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
            ImportSourceType.DeepbotBin => await DeepbotBinUserParser.ParseAsync(stream, ct),
            _ => throw new ArgumentException($"Unsupported source type: {config.SourceType}")
        };
    }

    private async Task<ImportResult> PreviewConfigImportAsync(Stream stream, CancellationToken ct)
    {
        DeepbotBinConfigParser.ParseResult parsed = await DeepbotBinConfigParser.ParseAsync(stream, ct);

        using IServiceScope scope = _scopeFactory.CreateScope();
        ICommandRepository commands = scope.ServiceProvider.GetRequiredService<ICommandRepository>();

        int commandsSkipped = 0;
        foreach (ImportCommandRecord cmd in parsed.Commands)
        {
            Command? existing = await commands.GetByTriggerOrAliasAsync(cmd.Trigger, ct);
            if (existing is not null)
            {
                commandsSkipped++;
            }
        }

        return new ImportResult
        {
            TotalRows = parsed.Commands.Count + parsed.Quotes.Count + parsed.TimedMessages.Count,
            CommandsImportedCount = parsed.Commands.Count - commandsSkipped,
            CommandsSkippedCount = commandsSkipped,
            QuotesImportedCount = parsed.Quotes.Count,
            TimersImportedCount = parsed.TimedMessages.Count
        };
    }

    private async Task<ImportResult> ExecuteConfigImportAsync(Stream stream, CancellationToken ct)
    {
        DeepbotBinConfigParser.ParseResult parsed = await DeepbotBinConfigParser.ParseAsync(stream, ct);

        ImportResult result = new()
        {
            TotalRows = parsed.Commands.Count + parsed.Quotes.Count + parsed.TimedMessages.Count
        };

        using IServiceScope scope = _scopeFactory.CreateScope();
        ICommandRepository commands = scope.ServiceProvider.GetRequiredService<ICommandRepository>();
        IQuoteRepository quotes = scope.ServiceProvider.GetRequiredService<IQuoteRepository>();
        ITimedMessageRepository timers = scope.ServiceProvider.GetRequiredService<ITimedMessageRepository>();

        // Import commands
        foreach (ImportCommandRecord cmd in parsed.Commands)
        {
            try
            {
                Command? existing = await commands.GetByTriggerOrAliasAsync(cmd.Trigger, ct);
                if (existing is not null)
                {
                    result.CommandsSkippedCount++;
                    continue;
                }

                Command newCommand = new()
                {
                    Trigger = cmd.Trigger,
                    ResponseTemplate = ConvertDeepbotVariables(cmd.Response),
                    PermissionLevel = MapAccessLevel(cmd.AccessLevel),
                    GlobalCooldownSeconds = cmd.CooldownSeconds,
                    IsEnabled = cmd.IsEnabled
                };

                await commands.CreateAsync(newCommand, ct);
                result.CommandsImportedCount++;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ImportRowError
                {
                    RowNumber = cmd.RecordNumber,
                    Field = "command",
                    Message = $"Command '{cmd.Trigger}': {ex.Message}",
                    Severity = ImportErrorSeverity.Warning
                });
            }
        }

        // Import quotes
        int nextNumber = await quotes.GetNextNumberAsync(ct);
        foreach (ImportQuoteRecord q in parsed.Quotes)
        {
            try
            {
                Quote newQuote = new()
                {
                    Number = nextNumber++,
                    Text = q.Text,
                    QuotedUser = q.User,
                    SavedBy = !string.IsNullOrWhiteSpace(q.AddedBy) ? q.AddedBy : "DeepBot Import",
                    CreatedAt = q.AddedOn ?? DateTimeOffset.UtcNow
                };

                await quotes.CreateAsync(newQuote, ct);
                result.QuotesImportedCount++;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ImportRowError
                {
                    RowNumber = q.Number,
                    Field = "quote",
                    Message = $"Quote #{q.Number}: {ex.Message}",
                    Severity = ImportErrorSeverity.Warning
                });
            }
        }

        // Import timed messages
        foreach (ImportTimedMessageRecord t in parsed.TimedMessages)
        {
            try
            {
                TimedMessage newTimer = new()
                {
                    Name = !string.IsNullOrWhiteSpace(t.Name) ? t.Name : $"DeepBot Timer {t.RecordNumber}",
                    Messages = new[] { ConvertDeepbotVariables(t.Message) },
                    IsEnabled = t.IsEnabled,
                    IsAnnouncement = t.IsAnnouncement,
                    IntervalMinutes = 10,
                    MinChatLines = 5,
                    RunWhenOnline = true
                };

                await timers.CreateAsync(newTimer, ct);
                result.TimersImportedCount++;
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ImportRowError
                {
                    RowNumber = t.RecordNumber,
                    Field = "timer",
                    Message = $"Timer '{t.Name}': {ex.Message}",
                    Severity = ImportErrorSeverity.Warning
                });
            }
        }

        _logger.LogInformation("Config import completed: {Summary}", result.Summary);
        return result;
    }

    private static PermissionLevel MapAccessLevel(int deepbotLevel)
    {
        return deepbotLevel switch
        {
            0 => PermissionLevel.Everyone,
            1 => PermissionLevel.Follower,
            2 or 3 => PermissionLevel.Subscriber,
            4 or 5 => PermissionLevel.Moderator,
            6 => PermissionLevel.Broadcaster,
            _ => PermissionLevel.Everyone
        };
    }

    private static string ConvertDeepbotVariables(string response)
    {
        return response
            .Replace("@user@", "{user}", StringComparison.OrdinalIgnoreCase)
            .Replace("@target@", "{target}", StringComparison.OrdinalIgnoreCase)
            .Replace("@counter@", "{count}", StringComparison.OrdinalIgnoreCase)
            .Replace("@hours@", "{hours}", StringComparison.OrdinalIgnoreCase)
            .Replace("@uptime2@", "{uptime}", StringComparison.OrdinalIgnoreCase)
            .Replace("@uptime@", "{uptime}", StringComparison.OrdinalIgnoreCase)
            .Replace("@subs@", "{subs}", StringComparison.OrdinalIgnoreCase)
            .Replace("@stream@", "{channel}", StringComparison.OrdinalIgnoreCase);
    }
}

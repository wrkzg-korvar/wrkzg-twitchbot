using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Infrastructure.Data;

namespace Wrkzg.Infrastructure.Repositories;

/// <summary>
/// SQLite-backed repository for key-value settings persistence.
/// </summary>
public class SettingsRepository : ISettingsRepository
{
    private readonly BotDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsRepository"/> class.
    /// </summary>
    /// <param name="db">The bot database context.</param>
    public SettingsRepository(BotDbContext db)
    {
        _db = db;
    }

    /// <summary>Gets a setting value by its key, or null if the key does not exist.</summary>
    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        Setting? setting = await _db.Settings.FindAsync(new object[] { key }, ct);
        return setting?.Value;
    }

    /// <summary>Gets all settings as a key-value dictionary.</summary>
    public async Task<IDictionary<string, string>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Settings
            .ToDictionaryAsync(s => s.Key, s => s.Value, ct);
    }

    /// <summary>Sets a setting value, creating the key if it does not exist or updating it if it does.</summary>
    public async Task SetAsync(string key, string value, CancellationToken ct = default)
    {
        Setting? existing = await _db.Settings.FindAsync(new object[] { key }, ct);

        if (existing is not null)
        {
            existing.Value = value;
        }
        else
        {
            _db.Settings.Add(new Setting { Key = key, Value = value });
        }

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Sets multiple settings at once, creating or updating each key.</summary>
    public async Task SetManyAsync(IDictionary<string, string> settings, CancellationToken ct = default)
    {
        foreach (KeyValuePair<string, string> kvp in settings)
        {
            Setting? existing = await _db.Settings.FindAsync(new object[] { kvp.Key }, ct);

            if (existing is not null)
            {
                existing.Value = kvp.Value;
            }
            else
            {
                _db.Settings.Add(new Setting { Key = kvp.Key, Value = kvp.Value });
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Deletes a setting by its key. Returns true if the key existed and was removed.</summary>
    public async Task<bool> DeleteAsync(string key, CancellationToken ct = default)
    {
        Setting? existing = await _db.Settings.FindAsync(new object[] { key }, ct);
        if (existing is null)
        {
            return false;
        }

        _db.Settings.Remove(existing);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

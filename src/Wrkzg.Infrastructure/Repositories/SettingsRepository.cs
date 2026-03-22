using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Infrastructure.Data;

namespace Wrkzg.Infrastructure.Repositories;

public class SettingsRepository : ISettingsRepository
{
    private readonly BotDbContext _db;

    public SettingsRepository(BotDbContext db)
    {
        _db = db;
    }

    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        Setting? setting = await _db.Settings.FindAsync(new object[] { key }, ct);
        return setting?.Value;
    }

    public async Task<IDictionary<string, string>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Settings
            .ToDictionaryAsync(s => s.Key, s => s.Value, ct);
    }

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

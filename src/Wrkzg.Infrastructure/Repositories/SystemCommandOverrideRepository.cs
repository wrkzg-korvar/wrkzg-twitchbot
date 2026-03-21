using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Infrastructure.Data;

namespace Wrkzg.Infrastructure.Repositories;

public class SystemCommandOverrideRepository : ISystemCommandOverrideRepository
{
    private readonly BotDbContext _db;

    public SystemCommandOverrideRepository(BotDbContext db)
    {
        _db = db;
    }

    public async Task<SystemCommandOverride?> GetByTriggerAsync(string trigger, CancellationToken ct = default)
    {
        return await _db.SystemCommandOverrides.FindAsync(new object[] { trigger }, ct);
    }

    public async Task<IReadOnlyList<SystemCommandOverride>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.SystemCommandOverrides.ToListAsync(ct);
    }

    public async Task SaveAsync(SystemCommandOverride entity, CancellationToken ct = default)
    {
        SystemCommandOverride? existing = await _db.SystemCommandOverrides.FindAsync(new object[] { entity.Trigger }, ct);
        if (existing is null)
        {
            _db.SystemCommandOverrides.Add(entity);
        }
        else
        {
            existing.CustomResponseTemplate = entity.CustomResponseTemplate;
            existing.IsEnabled = entity.IsEnabled;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(string trigger, CancellationToken ct = default)
    {
        SystemCommandOverride? existing = await _db.SystemCommandOverrides.FindAsync(new object[] { trigger }, ct);
        if (existing is not null)
        {
            _db.SystemCommandOverrides.Remove(existing);
            await _db.SaveChangesAsync(ct);
        }
    }
}

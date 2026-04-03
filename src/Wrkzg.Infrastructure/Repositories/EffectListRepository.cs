using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Infrastructure.Data;

namespace Wrkzg.Infrastructure.Repositories;

public class EffectListRepository : IEffectListRepository
{
    private readonly BotDbContext _db;

    public EffectListRepository(BotDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<EffectList>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.EffectLists.OrderBy(e => e.Id).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<EffectList>> GetEnabledAsync(CancellationToken ct = default)
    {
        return await _db.EffectLists.Where(e => e.IsEnabled).OrderBy(e => e.Id).ToListAsync(ct);
    }

    public async Task<EffectList?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.EffectLists.FindAsync(new object[] { id }, ct);
    }

    public async Task<EffectList> CreateAsync(EffectList effectList, CancellationToken ct = default)
    {
        _db.EffectLists.Add(effectList);
        await _db.SaveChangesAsync(ct);
        return effectList;
    }

    public async Task UpdateAsync(EffectList effectList, CancellationToken ct = default)
    {
        _db.EffectLists.Update(effectList);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        EffectList? effectList = await _db.EffectLists.FindAsync(new object[] { id }, ct);
        if (effectList is not null)
        {
            _db.EffectLists.Remove(effectList);
            await _db.SaveChangesAsync(ct);
        }
    }
}

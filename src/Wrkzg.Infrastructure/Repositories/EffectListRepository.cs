using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Infrastructure.Data;

namespace Wrkzg.Infrastructure.Repositories;

/// <summary>
/// SQLite-backed repository for effect list persistence.
/// </summary>
public class EffectListRepository : IEffectListRepository
{
    private readonly BotDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="EffectListRepository"/> class.
    /// </summary>
    /// <param name="db">The bot database context.</param>
    public EffectListRepository(BotDbContext db)
    {
        _db = db;
    }

    /// <summary>Gets all effect lists ordered by identifier.</summary>
    public async Task<IReadOnlyList<EffectList>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.EffectLists.OrderBy(e => e.Id).ToListAsync(ct);
    }

    /// <summary>Gets all enabled effect lists ordered by identifier.</summary>
    public async Task<IReadOnlyList<EffectList>> GetEnabledAsync(CancellationToken ct = default)
    {
        return await _db.EffectLists.Where(e => e.IsEnabled).OrderBy(e => e.Id).ToListAsync(ct);
    }

    /// <summary>Gets an effect list by its database identifier.</summary>
    public async Task<EffectList?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.EffectLists.FindAsync(new object[] { id }, ct);
    }

    /// <summary>Creates a new effect list and persists it to the database.</summary>
    public async Task<EffectList> CreateAsync(EffectList effectList, CancellationToken ct = default)
    {
        _db.EffectLists.Add(effectList);
        await _db.SaveChangesAsync(ct);
        return effectList;
    }

    /// <summary>Updates an existing effect list in the database.</summary>
    public async Task UpdateAsync(EffectList effectList, CancellationToken ct = default)
    {
        _db.EffectLists.Update(effectList);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Deletes an effect list by its database identifier.</summary>
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

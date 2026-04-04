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
/// SQLite-backed repository for stream counter persistence.
/// </summary>
public class CounterRepository : ICounterRepository
{
    private readonly BotDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="CounterRepository"/> class.
    /// </summary>
    /// <param name="db">The bot database context.</param>
    public CounterRepository(BotDbContext db)
    {
        _db = db;
    }

    /// <summary>Gets all counters ordered alphabetically by name.</summary>
    public async Task<IReadOnlyList<Counter>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Counters.OrderBy(c => c.Name).ToListAsync(ct);
    }

    /// <summary>Gets a counter by its database identifier.</summary>
    public async Task<Counter?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.Counters.FindAsync(new object[] { id }, ct);
    }

    /// <summary>Gets a counter by its chat trigger word.</summary>
    public async Task<Counter?> GetByTriggerAsync(string trigger, CancellationToken ct = default)
    {
        return await _db.Counters.FirstOrDefaultAsync(c => c.Trigger == trigger, ct);
    }

    /// <summary>Creates a new counter and persists it to the database.</summary>
    public async Task<Counter> CreateAsync(Counter counter, CancellationToken ct = default)
    {
        _db.Counters.Add(counter);
        await _db.SaveChangesAsync(ct);
        return counter;
    }

    /// <summary>Updates an existing counter in the database.</summary>
    public async Task UpdateAsync(Counter counter, CancellationToken ct = default)
    {
        _db.Counters.Update(counter);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Deletes a counter by its database identifier.</summary>
    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        Counter? counter = await _db.Counters.FindAsync(new object[] { id }, ct);
        if (counter is not null)
        {
            _db.Counters.Remove(counter);
            await _db.SaveChangesAsync(ct);
        }
    }
}

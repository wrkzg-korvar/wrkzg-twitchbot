using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Infrastructure.Data;

namespace Wrkzg.Infrastructure.Repositories;

public class CounterRepository : ICounterRepository
{
    private readonly BotDbContext _db;

    public CounterRepository(BotDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Counter>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Counters.OrderBy(c => c.Name).ToListAsync(ct);
    }

    public async Task<Counter?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.Counters.FindAsync(new object[] { id }, ct);
    }

    public async Task<Counter?> GetByTriggerAsync(string trigger, CancellationToken ct = default)
    {
        return await _db.Counters.FirstOrDefaultAsync(c => c.Trigger == trigger, ct);
    }

    public async Task<Counter> CreateAsync(Counter counter, CancellationToken ct = default)
    {
        _db.Counters.Add(counter);
        await _db.SaveChangesAsync(ct);
        return counter;
    }

    public async Task UpdateAsync(Counter counter, CancellationToken ct = default)
    {
        _db.Counters.Update(counter);
        await _db.SaveChangesAsync(ct);
    }

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

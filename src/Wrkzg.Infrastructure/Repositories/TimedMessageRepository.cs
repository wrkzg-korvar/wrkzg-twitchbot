using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Infrastructure.Data;

namespace Wrkzg.Infrastructure.Repositories;

public class TimedMessageRepository : ITimedMessageRepository
{
    private readonly BotDbContext _db;

    public TimedMessageRepository(BotDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TimedMessage>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.TimedMessages.OrderBy(t => t.Name).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<TimedMessage>> GetEnabledAsync(CancellationToken ct = default)
    {
        return await _db.TimedMessages.Where(t => t.IsEnabled).ToListAsync(ct);
    }

    public async Task<TimedMessage?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.TimedMessages.FindAsync(new object[] { id }, ct);
    }

    public async Task<TimedMessage> CreateAsync(TimedMessage timer, CancellationToken ct = default)
    {
        _db.TimedMessages.Add(timer);
        await _db.SaveChangesAsync(ct);
        return timer;
    }

    public async Task UpdateAsync(TimedMessage timer, CancellationToken ct = default)
    {
        _db.TimedMessages.Update(timer);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        TimedMessage? timer = await _db.TimedMessages.FindAsync(new object[] { id }, ct);
        if (timer is not null)
        {
            _db.TimedMessages.Remove(timer);
            await _db.SaveChangesAsync(ct);
        }
    }
}

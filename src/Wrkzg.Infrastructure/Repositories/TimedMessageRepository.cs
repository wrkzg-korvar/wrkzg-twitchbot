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
/// SQLite-backed repository for timed message (recurring chat timer) persistence.
/// </summary>
public class TimedMessageRepository : ITimedMessageRepository
{
    private readonly BotDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimedMessageRepository"/> class.
    /// </summary>
    /// <param name="db">The bot database context.</param>
    public TimedMessageRepository(BotDbContext db)
    {
        _db = db;
    }

    /// <summary>Gets all timed messages ordered alphabetically by name.</summary>
    public async Task<IReadOnlyList<TimedMessage>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.TimedMessages.OrderBy(t => t.Name).ToListAsync(ct);
    }

    /// <summary>Gets all enabled timed messages.</summary>
    public async Task<IReadOnlyList<TimedMessage>> GetEnabledAsync(CancellationToken ct = default)
    {
        return await _db.TimedMessages.Where(t => t.IsEnabled).ToListAsync(ct);
    }

    /// <summary>Gets a timed message by its database identifier.</summary>
    public async Task<TimedMessage?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.TimedMessages.FindAsync(new object[] { id }, ct);
    }

    /// <summary>Creates a new timed message and persists it to the database.</summary>
    public async Task<TimedMessage> CreateAsync(TimedMessage timer, CancellationToken ct = default)
    {
        _db.TimedMessages.Add(timer);
        await _db.SaveChangesAsync(ct);
        return timer;
    }

    /// <summary>Updates an existing timed message in the database.</summary>
    public async Task UpdateAsync(TimedMessage timer, CancellationToken ct = default)
    {
        _db.TimedMessages.Update(timer);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Deletes a timed message by its database identifier.</summary>
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

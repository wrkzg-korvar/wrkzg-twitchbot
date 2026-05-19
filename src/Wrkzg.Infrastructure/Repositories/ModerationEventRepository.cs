using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Infrastructure.Data;

namespace Wrkzg.Infrastructure.Repositories;

/// <summary>SQLite-backed repository for moderation event log entries.</summary>
public class ModerationEventRepository : IModerationEventRepository
{
    private readonly BotDbContext _db;

    /// <summary>Initializes a new instance of the <see cref="ModerationEventRepository"/> class.</summary>
    public ModerationEventRepository(BotDbContext db)
    {
        _db = db;
    }

    /// <summary>Creates a new moderation event and persists it to the database.</summary>
    public async Task<ModerationEvent> CreateAsync(ModerationEvent evt, CancellationToken ct = default)
    {
        _db.ModerationEvents.Add(evt);
        await _db.SaveChangesAsync(ct);
        return evt;
    }

    /// <summary>Gets events for a specific user, newest first.</summary>
    public async Task<IReadOnlyList<ModerationEvent>> GetByUserAsync(string twitchUserId, int limit = 50, CancellationToken ct = default)
    {
        return await _db.ModerationEvents
            .Where(e => e.TwitchUserId == twitchUserId)
            .OrderByDescending(e => e.Id)
            .Take(limit)
            .ToListAsync(ct);
    }

    /// <summary>Gets the most recent events across all users.</summary>
    public async Task<IReadOnlyList<ModerationEvent>> GetRecentAsync(int limit = 50, CancellationToken ct = default)
    {
        return await _db.ModerationEvents
            .OrderByDescending(e => e.Id)
            .Take(limit)
            .ToListAsync(ct);
    }

    /// <summary>Deletes events older than the cutoff date.</summary>
    public async Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct = default)
    {
        // SQLite stores DateTimeOffset as TEXT and cannot translate comparisons directly.
        // Use UtcTicks for a numeric comparison the provider can handle, with a client-side
        // fallback if the provider rejects the expression.
        List<ModerationEvent> old;
        try
        {
            long cutoffTicks = cutoff.UtcTicks;
            old = await _db.ModerationEvents
                .Where(e => e.CreatedAt.UtcTicks < cutoffTicks)
                .ToListAsync(ct);
        }
        catch (InvalidOperationException)
        {
            List<ModerationEvent> all = await _db.ModerationEvents.ToListAsync(ct);
            old = all.Where(e => e.CreatedAt < cutoff).ToList();
        }

        if (old.Count == 0)
        {
            return 0;
        }

        _db.ModerationEvents.RemoveRange(old);
        await _db.SaveChangesAsync(ct);
        return old.Count;
    }
}

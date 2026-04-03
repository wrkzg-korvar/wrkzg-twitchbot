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

public class StreamAnalyticsRepository : IStreamAnalyticsRepository
{
    private readonly BotDbContext _db;

    public StreamAnalyticsRepository(BotDbContext db)
    {
        _db = db;
    }

    public async Task<StreamSession> CreateSessionAsync(StreamSession session, CancellationToken ct = default)
    {
        _db.StreamSessions.Add(session);
        await _db.SaveChangesAsync(ct);
        return session;
    }

    public async Task UpdateSessionAsync(StreamSession session, CancellationToken ct = default)
    {
        _db.StreamSessions.Update(session);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<StreamSession?> GetActiveSessionAsync(CancellationToken ct = default)
    {
        return await _db.StreamSessions
            .Include(s => s.CategorySegments)
            .FirstOrDefaultAsync(s => s.EndedAt == null, ct);
    }

    public async Task<StreamSession?> GetSessionByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.StreamSessions
            .Include(s => s.CategorySegments)
            .Include(s => s.ViewerSnapshots)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<StreamSession?> GetLatestSessionAsync(CancellationToken ct = default)
    {
        return await _db.StreamSessions
            .Include(s => s.CategorySegments)
            .Include(s => s.ViewerSnapshots)
            .OrderByDescending(s => s.Id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<StreamSession>> GetSessionsAsync(int limit = 50, int offset = 0, CancellationToken ct = default)
    {
        return await _db.StreamSessions
            .Include(s => s.CategorySegments)
            .OrderByDescending(s => s.Id)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<StreamSession>> GetSessionsSinceAsync(DateTimeOffset since, CancellationToken ct = default)
    {
        // SQLite cannot translate DateTimeOffset comparisons in WHERE clauses.
        // Load all sessions and filter in memory, ordered by Id (creation order).
        List<StreamSession> all = await _db.StreamSessions
            .Include(s => s.CategorySegments)
            .OrderByDescending(s => s.Id)
            .ToListAsync(ct);

        return all.Where(s => s.StartedAt >= since).ToList();
    }

    public async Task<CategorySegment> CreateSegmentAsync(CategorySegment segment, CancellationToken ct = default)
    {
        _db.CategorySegments.Add(segment);
        await _db.SaveChangesAsync(ct);
        return segment;
    }

    public async Task UpdateSegmentAsync(CategorySegment segment, CancellationToken ct = default)
    {
        _db.CategorySegments.Update(segment);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddSnapshotAsync(ViewerSnapshot snapshot, CancellationToken ct = default)
    {
        _db.ViewerSnapshots.Add(snapshot);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ViewerSnapshot>> GetSnapshotsForSessionAsync(int sessionId, CancellationToken ct = default)
    {
        return await _db.ViewerSnapshots
            .Where(v => v.StreamSessionId == sessionId)
            .OrderBy(v => v.Id)
            .ToListAsync(ct);
    }
}

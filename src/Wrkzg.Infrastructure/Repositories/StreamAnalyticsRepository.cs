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

/// <summary>
/// SQLite-backed repository for stream session, category segment, and viewer snapshot persistence.
/// </summary>
public class StreamAnalyticsRepository : IStreamAnalyticsRepository
{
    private readonly BotDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamAnalyticsRepository"/> class.
    /// </summary>
    /// <param name="db">The bot database context.</param>
    public StreamAnalyticsRepository(BotDbContext db)
    {
        _db = db;
    }

    /// <summary>Creates a new stream session and persists it to the database.</summary>
    public async Task<StreamSession> CreateSessionAsync(StreamSession session, CancellationToken ct = default)
    {
        _db.StreamSessions.Add(session);
        await _db.SaveChangesAsync(ct);
        return session;
    }

    /// <summary>Updates an existing stream session in the database.</summary>
    public async Task UpdateSessionAsync(StreamSession session, CancellationToken ct = default)
    {
        _db.StreamSessions.Update(session);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Gets the currently active (not ended) stream session with its category segments.</summary>
    public async Task<StreamSession?> GetActiveSessionAsync(CancellationToken ct = default)
    {
        return await _db.StreamSessions
            .Include(s => s.CategorySegments)
            .FirstOrDefaultAsync(s => s.EndedAt == null, ct);
    }

    /// <summary>Gets a stream session by its database identifier, including segments and snapshots.</summary>
    public async Task<StreamSession?> GetSessionByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.StreamSessions
            .Include(s => s.CategorySegments)
            .Include(s => s.ViewerSnapshots)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    /// <summary>Gets the most recent stream session, including segments and snapshots.</summary>
    public async Task<StreamSession?> GetLatestSessionAsync(CancellationToken ct = default)
    {
        return await _db.StreamSessions
            .Include(s => s.CategorySegments)
            .Include(s => s.ViewerSnapshots)
            .OrderByDescending(s => s.Id)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>Gets a paginated list of stream sessions with category segments.</summary>
    public async Task<IReadOnlyList<StreamSession>> GetSessionsAsync(int limit = 50, int offset = 0, CancellationToken ct = default)
    {
        return await _db.StreamSessions
            .Include(s => s.CategorySegments)
            .OrderByDescending(s => s.Id)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);
    }

    /// <summary>Gets all stream sessions that started on or after the specified date.</summary>
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

    /// <summary>Creates a new category segment within a stream session.</summary>
    public async Task<CategorySegment> CreateSegmentAsync(CategorySegment segment, CancellationToken ct = default)
    {
        _db.CategorySegments.Add(segment);
        await _db.SaveChangesAsync(ct);
        return segment;
    }

    /// <summary>Updates an existing category segment in the database.</summary>
    public async Task UpdateSegmentAsync(CategorySegment segment, CancellationToken ct = default)
    {
        _db.CategorySegments.Update(segment);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Adds a viewer count snapshot to a stream session.</summary>
    public async Task AddSnapshotAsync(ViewerSnapshot snapshot, CancellationToken ct = default)
    {
        _db.ViewerSnapshots.Add(snapshot);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Gets all viewer snapshots for a stream session, ordered chronologically.</summary>
    public async Task<IReadOnlyList<ViewerSnapshot>> GetSnapshotsForSessionAsync(int sessionId, CancellationToken ct = default)
    {
        return await _db.ViewerSnapshots
            .Where(v => v.StreamSessionId == sessionId)
            .OrderBy(v => v.Id)
            .ToListAsync(ct);
    }
}

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
/// SQLite-backed repository for song request queue persistence.
/// </summary>
public class SongRequestRepository : ISongRequestRepository
{
    private readonly BotDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="SongRequestRepository"/> class.
    /// </summary>
    /// <param name="db">The bot database context.</param>
    public SongRequestRepository(BotDbContext db)
    {
        _db = db;
    }

    /// <summary>Gets the current song request queue (playing song first, then queued songs in order).</summary>
    public async Task<IReadOnlyList<SongRequest>> GetQueueAsync(CancellationToken ct = default)
    {
        return await _db.SongRequests
            .Where(s => s.Status == SongRequestStatus.Queued || s.Status == SongRequestStatus.Playing)
            .OrderBy(s => s.Status == SongRequestStatus.Playing ? 0 : 1)
            .ThenBy(s => s.Id)
            .ToListAsync(ct);
    }

    /// <summary>Gets the currently playing song request, or null if nothing is playing.</summary>
    public async Task<SongRequest?> GetCurrentlyPlayingAsync(CancellationToken ct = default)
    {
        return await _db.SongRequests
            .FirstOrDefaultAsync(s => s.Status == SongRequestStatus.Playing, ct);
    }

    /// <summary>Gets the next queued song request in order, or null if the queue is empty.</summary>
    public async Task<SongRequest?> GetNextInQueueAsync(CancellationToken ct = default)
    {
        return await _db.SongRequests
            .Where(s => s.Status == SongRequestStatus.Queued)
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>Gets a song request by its database identifier.</summary>
    public async Task<SongRequest?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.SongRequests.FindAsync(new object[] { id }, ct);
    }

    /// <summary>Creates a new song request and persists it to the database.</summary>
    public async Task<SongRequest> CreateAsync(SongRequest request, CancellationToken ct = default)
    {
        _db.SongRequests.Add(request);
        await _db.SaveChangesAsync(ct);
        return request;
    }

    /// <summary>Updates an existing song request in the database.</summary>
    public async Task UpdateAsync(SongRequest request, CancellationToken ct = default)
    {
        _db.SongRequests.Update(request);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Deletes a song request by its database identifier.</summary>
    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        SongRequest? request = await _db.SongRequests.FindAsync(new object[] { id }, ct);
        if (request is not null)
        {
            _db.SongRequests.Remove(request);
            await _db.SaveChangesAsync(ct);
        }
    }

    /// <summary>Gets the number of songs currently in the queue (excludes playing).</summary>
    public async Task<int> GetQueueCountAsync(CancellationToken ct = default)
    {
        return await _db.SongRequests.CountAsync(s => s.Status == SongRequestStatus.Queued, ct);
    }

    /// <summary>Gets the number of queued songs requested by a specific user.</summary>
    public async Task<int> GetUserQueueCountAsync(string requestedBy, CancellationToken ct = default)
    {
        return await _db.SongRequests.CountAsync(
            s => s.RequestedBy == requestedBy && s.Status == SongRequestStatus.Queued, ct);
    }

    /// <summary>Checks whether a video is already queued or currently playing.</summary>
    public async Task<bool> IsVideoInQueueAsync(string videoId, CancellationToken ct = default)
    {
        return await _db.SongRequests.AnyAsync(
            s => s.VideoId == videoId && (s.Status == SongRequestStatus.Queued || s.Status == SongRequestStatus.Playing), ct);
    }

    /// <summary>Removes all queued (not playing) song requests from the queue.</summary>
    public async Task ClearQueueAsync(CancellationToken ct = default)
    {
        List<SongRequest> queued = await _db.SongRequests
            .Where(s => s.Status == SongRequestStatus.Queued)
            .ToListAsync(ct);
        _db.SongRequests.RemoveRange(queued);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Gets recently played or skipped songs, ordered by most recent first.</summary>
    public async Task<IReadOnlyList<SongRequest>> GetHistoryAsync(int limit = 20, CancellationToken ct = default)
    {
        return await _db.SongRequests
            .Where(s => s.Status == SongRequestStatus.Played || s.Status == SongRequestStatus.Skipped)
            .OrderByDescending(s => s.Id)
            .Take(limit)
            .ToListAsync(ct);
    }
}

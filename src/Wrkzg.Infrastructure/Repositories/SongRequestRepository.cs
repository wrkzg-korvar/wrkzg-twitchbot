using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Infrastructure.Data;

namespace Wrkzg.Infrastructure.Repositories;

public class SongRequestRepository : ISongRequestRepository
{
    private readonly BotDbContext _db;

    public SongRequestRepository(BotDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SongRequest>> GetQueueAsync(CancellationToken ct = default)
    {
        return await _db.SongRequests
            .Where(s => s.Status == SongRequestStatus.Queued || s.Status == SongRequestStatus.Playing)
            .OrderBy(s => s.Status == SongRequestStatus.Playing ? 0 : 1)
            .ThenBy(s => s.Id)
            .ToListAsync(ct);
    }

    public async Task<SongRequest?> GetCurrentlyPlayingAsync(CancellationToken ct = default)
    {
        return await _db.SongRequests
            .FirstOrDefaultAsync(s => s.Status == SongRequestStatus.Playing, ct);
    }

    public async Task<SongRequest?> GetNextInQueueAsync(CancellationToken ct = default)
    {
        return await _db.SongRequests
            .Where(s => s.Status == SongRequestStatus.Queued)
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<SongRequest?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.SongRequests.FindAsync(new object[] { id }, ct);
    }

    public async Task<SongRequest> CreateAsync(SongRequest request, CancellationToken ct = default)
    {
        _db.SongRequests.Add(request);
        await _db.SaveChangesAsync(ct);
        return request;
    }

    public async Task UpdateAsync(SongRequest request, CancellationToken ct = default)
    {
        _db.SongRequests.Update(request);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        SongRequest? request = await _db.SongRequests.FindAsync(new object[] { id }, ct);
        if (request is not null)
        {
            _db.SongRequests.Remove(request);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<int> GetQueueCountAsync(CancellationToken ct = default)
    {
        return await _db.SongRequests.CountAsync(s => s.Status == SongRequestStatus.Queued, ct);
    }

    public async Task<int> GetUserQueueCountAsync(string requestedBy, CancellationToken ct = default)
    {
        return await _db.SongRequests.CountAsync(
            s => s.RequestedBy == requestedBy && s.Status == SongRequestStatus.Queued, ct);
    }

    public async Task<bool> IsVideoInQueueAsync(string videoId, CancellationToken ct = default)
    {
        return await _db.SongRequests.AnyAsync(
            s => s.VideoId == videoId && (s.Status == SongRequestStatus.Queued || s.Status == SongRequestStatus.Playing), ct);
    }

    public async Task ClearQueueAsync(CancellationToken ct = default)
    {
        List<SongRequest> queued = await _db.SongRequests
            .Where(s => s.Status == SongRequestStatus.Queued)
            .ToListAsync(ct);
        _db.SongRequests.RemoveRange(queued);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<SongRequest>> GetHistoryAsync(int limit = 20, CancellationToken ct = default)
    {
        return await _db.SongRequests
            .Where(s => s.Status == SongRequestStatus.Played || s.Status == SongRequestStatus.Skipped)
            .OrderByDescending(s => s.Id)
            .Take(limit)
            .ToListAsync(ct);
    }
}

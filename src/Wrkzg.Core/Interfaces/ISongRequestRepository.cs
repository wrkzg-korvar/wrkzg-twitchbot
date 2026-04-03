using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Repository for song request queue management.
/// </summary>
public interface ISongRequestRepository
{
    Task<IReadOnlyList<SongRequest>> GetQueueAsync(CancellationToken ct = default);
    Task<SongRequest?> GetCurrentlyPlayingAsync(CancellationToken ct = default);
    Task<SongRequest?> GetNextInQueueAsync(CancellationToken ct = default);
    Task<SongRequest?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<SongRequest> CreateAsync(SongRequest request, CancellationToken ct = default);
    Task UpdateAsync(SongRequest request, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<int> GetQueueCountAsync(CancellationToken ct = default);
    Task<int> GetUserQueueCountAsync(string requestedBy, CancellationToken ct = default);
    Task<bool> IsVideoInQueueAsync(string videoId, CancellationToken ct = default);
    Task ClearQueueAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SongRequest>> GetHistoryAsync(int limit = 20, CancellationToken ct = default);
}

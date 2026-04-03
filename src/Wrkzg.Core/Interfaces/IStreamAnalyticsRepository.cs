using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Repository for stream analytics data.
/// </summary>
public interface IStreamAnalyticsRepository
{
    Task<StreamSession> CreateSessionAsync(StreamSession session, CancellationToken ct = default);
    Task UpdateSessionAsync(StreamSession session, CancellationToken ct = default);
    Task<StreamSession?> GetActiveSessionAsync(CancellationToken ct = default);
    Task<StreamSession?> GetSessionByIdAsync(int id, CancellationToken ct = default);
    Task<StreamSession?> GetLatestSessionAsync(CancellationToken ct = default);
    Task<IReadOnlyList<StreamSession>> GetSessionsAsync(int limit = 50, int offset = 0, CancellationToken ct = default);
    Task<IReadOnlyList<StreamSession>> GetSessionsSinceAsync(DateTimeOffset since, CancellationToken ct = default);

    Task<CategorySegment> CreateSegmentAsync(CategorySegment segment, CancellationToken ct = default);
    Task UpdateSegmentAsync(CategorySegment segment, CancellationToken ct = default);

    Task AddSnapshotAsync(ViewerSnapshot snapshot, CancellationToken ct = default);
    Task<IReadOnlyList<ViewerSnapshot>> GetSnapshotsForSessionAsync(int sessionId, CancellationToken ct = default);
}

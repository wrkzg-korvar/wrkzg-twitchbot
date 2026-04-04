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
    /// <summary>
    /// Creates a new stream session record when a stream goes online.
    /// </summary>
    /// <param name="session">The stream session to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created session with its assigned database identifier.</returns>
    Task<StreamSession> CreateSessionAsync(StreamSession session, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing stream session (e.g. to set the end time or update statistics).
    /// </summary>
    /// <param name="session">The session with updated values.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateSessionAsync(StreamSession session, CancellationToken ct = default);

    /// <summary>
    /// Retrieves the currently active (ongoing) stream session, if any.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The active stream session, or null if no stream is currently tracked.</returns>
    Task<StreamSession?> GetActiveSessionAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves a stream session by its database identifier.
    /// </summary>
    /// <param name="id">The database identifier of the session.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching session, or null if not found.</returns>
    Task<StreamSession?> GetSessionByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves the most recently created stream session.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The latest session, or null if no sessions exist.</returns>
    Task<StreamSession?> GetLatestSessionAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves a paginated list of stream sessions ordered by start time (newest first).
    /// </summary>
    /// <param name="limit">The maximum number of sessions to return.</param>
    /// <param name="offset">The number of sessions to skip for pagination.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of stream sessions.</returns>
    Task<IReadOnlyList<StreamSession>> GetSessionsAsync(int limit = 50, int offset = 0, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all stream sessions that started on or after the specified date.
    /// </summary>
    /// <param name="since">The earliest start time to include.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of matching stream sessions.</returns>
    Task<IReadOnlyList<StreamSession>> GetSessionsSinceAsync(DateTimeOffset since, CancellationToken ct = default);

    /// <summary>
    /// Creates a new category segment within a stream session (e.g. when the game/category changes).
    /// </summary>
    /// <param name="segment">The category segment to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created segment with its assigned database identifier.</returns>
    Task<CategorySegment> CreateSegmentAsync(CategorySegment segment, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing category segment (e.g. to set its end time).
    /// </summary>
    /// <param name="segment">The segment with updated values.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateSegmentAsync(CategorySegment segment, CancellationToken ct = default);

    /// <summary>
    /// Records a periodic viewer count snapshot for a stream session.
    /// </summary>
    /// <param name="snapshot">The viewer snapshot to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddSnapshotAsync(ViewerSnapshot snapshot, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all viewer count snapshots recorded during a specific stream session.
    /// </summary>
    /// <param name="sessionId">The database identifier of the stream session.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of viewer snapshots for the session.</returns>
    Task<IReadOnlyList<ViewerSnapshot>> GetSnapshotsForSessionAsync(int sessionId, CancellationToken ct = default);
}

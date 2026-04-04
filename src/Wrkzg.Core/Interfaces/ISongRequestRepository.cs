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
    /// <summary>
    /// Retrieves all pending (queued) song requests in playback order.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of queued song requests.</returns>
    Task<IReadOnlyList<SongRequest>> GetQueueAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves the song request that is currently playing.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The currently playing song request, or null if nothing is playing.</returns>
    Task<SongRequest?> GetCurrentlyPlayingAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves the next song request in the queue (first pending item).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The next queued song request, or null if the queue is empty.</returns>
    Task<SongRequest?> GetNextInQueueAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves a song request by its database identifier.
    /// </summary>
    /// <param name="id">The database identifier of the song request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching song request, or null if not found.</returns>
    Task<SongRequest?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new song request and adds it to the queue.
    /// </summary>
    /// <param name="request">The song request to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created song request with its assigned database identifier.</returns>
    Task<SongRequest> CreateAsync(SongRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing song request (e.g. to mark it as playing or completed).
    /// </summary>
    /// <param name="request">The song request with updated values.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateAsync(SongRequest request, CancellationToken ct = default);

    /// <summary>
    /// Deletes a song request by its database identifier.
    /// </summary>
    /// <param name="id">The database identifier of the song request to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Returns the total number of pending song requests in the queue.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The number of queued song requests.</returns>
    Task<int> GetQueueCountAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns the number of pending song requests submitted by a specific user.
    /// </summary>
    /// <param name="requestedBy">The username who submitted the requests.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The count of queued requests by the specified user.</returns>
    Task<int> GetUserQueueCountAsync(string requestedBy, CancellationToken ct = default);

    /// <summary>
    /// Checks whether a specific video is already in the queue.
    /// </summary>
    /// <param name="videoId">The platform-specific video identifier (e.g. YouTube video ID).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the video is already queued.</returns>
    Task<bool> IsVideoInQueueAsync(string videoId, CancellationToken ct = default);

    /// <summary>
    /// Removes all pending song requests from the queue.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task ClearQueueAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves recently completed song requests for history display.
    /// </summary>
    /// <param name="limit">The maximum number of history entries to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of recently completed song requests.</returns>
    Task<IReadOnlyList<SongRequest>> GetHistoryAsync(int limit = 20, CancellationToken ct = default);
}

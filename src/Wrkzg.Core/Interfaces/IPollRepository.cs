using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Repository for polls and votes.
/// </summary>
public interface IPollRepository
{
    /// <summary>
    /// Retrieves the currently active (open) poll, if any.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The active poll, or null if no poll is currently running.</returns>
    Task<Poll?> GetActiveAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves a poll by its database identifier.
    /// </summary>
    /// <param name="id">The database identifier of the poll.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching poll, or null if not found.</returns>
    Task<Poll?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all polls ordered by creation date.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of all polls.</returns>
    Task<IReadOnlyList<Poll>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves the most recent polls, limited to the specified count.
    /// </summary>
    /// <param name="count">The maximum number of polls to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of the most recent polls.</returns>
    Task<IReadOnlyList<Poll>> GetRecentAsync(int count = 10, CancellationToken ct = default);

    /// <summary>
    /// Creates a new poll.
    /// </summary>
    /// <param name="poll">The poll to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created poll with its assigned database identifier.</returns>
    Task<Poll> CreateAsync(Poll poll, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing poll (e.g. to close it or update vote counts).
    /// </summary>
    /// <param name="poll">The poll with updated values.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateAsync(Poll poll, CancellationToken ct = default);

    /// <summary>
    /// Records a vote on a poll option.
    /// </summary>
    /// <param name="vote">The vote to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddVoteAsync(PollVote vote, CancellationToken ct = default);

    /// <summary>
    /// Checks whether a user has already voted in a specific poll.
    /// </summary>
    /// <param name="pollId">The database identifier of the poll.</param>
    /// <param name="userId">The database identifier of the user.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the user has already cast a vote in this poll.</returns>
    Task<bool> HasUserVotedAsync(int pollId, int userId, CancellationToken ct = default);
}

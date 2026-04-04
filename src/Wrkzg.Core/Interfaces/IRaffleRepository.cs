using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Repository for raffles and giveaways.
/// </summary>
public interface IRaffleRepository
{
    /// <summary>
    /// Retrieves the currently active (open) raffle, if any.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The active raffle, or null if no raffle is currently running.</returns>
    Task<Raffle?> GetActiveAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves a raffle by its database identifier.
    /// </summary>
    /// <param name="id">The database identifier of the raffle.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching raffle, or null if not found.</returns>
    Task<Raffle?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a raffle by its identifier, including all associated entries.
    /// </summary>
    /// <param name="raffleId">The database identifier of the raffle.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The raffle with its entries loaded, or null if not found.</returns>
    Task<Raffle?> GetWithEntriesAsync(int raffleId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all raffles ordered by creation date.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of all raffles.</returns>
    Task<IReadOnlyList<Raffle>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves the most recent raffles, limited to the specified count.
    /// </summary>
    /// <param name="count">The maximum number of raffles to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of the most recent raffles.</returns>
    Task<IReadOnlyList<Raffle>> GetRecentAsync(int count = 10, CancellationToken ct = default);

    /// <summary>
    /// Creates a new raffle.
    /// </summary>
    /// <param name="raffle">The raffle to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created raffle with its assigned database identifier.</returns>
    Task<Raffle> CreateAsync(Raffle raffle, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing raffle (e.g. to close it or change its status).
    /// </summary>
    /// <param name="raffle">The raffle with updated values.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateAsync(Raffle raffle, CancellationToken ct = default);

    /// <summary>
    /// Adds a user's entry to an active raffle.
    /// </summary>
    /// <param name="entry">The raffle entry to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddEntryAsync(RaffleEntry entry, CancellationToken ct = default);

    /// <summary>
    /// Checks whether a user has already entered a specific raffle.
    /// </summary>
    /// <param name="raffleId">The database identifier of the raffle.</param>
    /// <param name="userId">The database identifier of the user.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the user has already entered this raffle.</returns>
    Task<bool> HasUserEnteredAsync(int raffleId, int userId, CancellationToken ct = default);

    /// <summary>
    /// Returns the total number of entries in a specific raffle.
    /// </summary>
    /// <param name="raffleId">The database identifier of the raffle.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The total number of entries.</returns>
    Task<int> GetEntryCountAsync(int raffleId, CancellationToken ct = default);

    /// <summary>
    /// Records a draw result (winner selection) for a raffle.
    /// </summary>
    /// <param name="draw">The draw result to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddDrawAsync(RaffleDraw draw, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all draw results for a specific raffle.
    /// </summary>
    /// <param name="raffleId">The database identifier of the raffle.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of draw results for the raffle.</returns>
    Task<IReadOnlyList<RaffleDraw>> GetDrawsAsync(int raffleId, CancellationToken ct = default);
}

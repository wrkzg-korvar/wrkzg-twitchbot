using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Repository for named counters.
/// </summary>
public interface ICounterRepository
{
    /// <summary>
    /// Retrieves all counters.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of all counters.</returns>
    Task<IReadOnlyList<Counter>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves a counter by its database identifier.
    /// </summary>
    /// <param name="id">The database identifier of the counter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching counter, or null if not found.</returns>
    Task<Counter?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Finds a counter by its chat trigger (e.g. "!deaths").
    /// </summary>
    /// <param name="trigger">The chat trigger to search for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching counter, or null if no counter uses that trigger.</returns>
    Task<Counter?> GetByTriggerAsync(string trigger, CancellationToken ct = default);

    /// <summary>
    /// Creates a new counter.
    /// </summary>
    /// <param name="counter">The counter to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created counter with its assigned database identifier.</returns>
    Task<Counter> CreateAsync(Counter counter, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing counter (name, value, trigger, etc.).
    /// </summary>
    /// <param name="counter">The counter with updated values.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateAsync(Counter counter, CancellationToken ct = default);

    /// <summary>
    /// Deletes a counter by its database identifier.
    /// </summary>
    /// <param name="id">The database identifier of the counter to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(int id, CancellationToken ct = default);
}

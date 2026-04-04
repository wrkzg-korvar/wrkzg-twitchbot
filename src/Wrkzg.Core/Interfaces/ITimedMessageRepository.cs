using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Repository for timed/recurring chat messages.
/// </summary>
public interface ITimedMessageRepository
{
    /// <summary>
    /// Retrieves all timed messages, including disabled ones.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of all timed messages.</returns>
    Task<IReadOnlyList<TimedMessage>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves only the enabled timed messages for active scheduling.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of enabled timed messages.</returns>
    Task<IReadOnlyList<TimedMessage>> GetEnabledAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves a timed message by its database identifier.
    /// </summary>
    /// <param name="id">The database identifier of the timed message.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching timed message, or null if not found.</returns>
    Task<TimedMessage?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new timed message.
    /// </summary>
    /// <param name="timer">The timed message to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created timed message with its assigned database identifier.</returns>
    Task<TimedMessage> CreateAsync(TimedMessage timer, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing timed message (interval, content, enabled state, etc.).
    /// </summary>
    /// <param name="timer">The timed message with updated values.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateAsync(TimedMessage timer, CancellationToken ct = default);

    /// <summary>
    /// Deletes a timed message by its database identifier.
    /// </summary>
    /// <param name="id">The database identifier of the timed message to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(int id, CancellationToken ct = default);
}

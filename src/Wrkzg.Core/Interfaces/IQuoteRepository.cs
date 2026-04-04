using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Repository for saved chat quotes.
/// </summary>
public interface IQuoteRepository
{
    /// <summary>
    /// Retrieves all saved quotes.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of all quotes.</returns>
    Task<IReadOnlyList<Quote>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves a quote by its database identifier.
    /// </summary>
    /// <param name="id">The database identifier of the quote.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching quote, or null if not found.</returns>
    Task<Quote?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a quote by its sequential display number (e.g. quote #42).
    /// </summary>
    /// <param name="number">The sequential quote number.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching quote, or null if no quote has that number.</returns>
    Task<Quote?> GetByNumberAsync(int number, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a random quote from the collection.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A randomly selected quote, or null if the collection is empty.</returns>
    Task<Quote?> GetRandomAsync(CancellationToken ct = default);

    /// <summary>
    /// Determines the next sequential number to assign to a new quote.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The next available quote number.</returns>
    Task<int> GetNextNumberAsync(CancellationToken ct = default);

    /// <summary>
    /// Creates a new quote.
    /// </summary>
    /// <param name="quote">The quote to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created quote with its assigned database identifier.</returns>
    Task<Quote> CreateAsync(Quote quote, CancellationToken ct = default);

    /// <summary>
    /// Deletes a quote by its database identifier.
    /// </summary>
    /// <param name="id">The database identifier of the quote to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Returns the total number of saved quotes.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The total quote count.</returns>
    Task<int> GetCountAsync(CancellationToken ct = default);
}

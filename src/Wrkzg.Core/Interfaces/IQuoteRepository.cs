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
    Task<IReadOnlyList<Quote>> GetAllAsync(CancellationToken ct = default);
    Task<Quote?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Quote?> GetByNumberAsync(int number, CancellationToken ct = default);
    Task<Quote?> GetRandomAsync(CancellationToken ct = default);
    Task<int> GetNextNumberAsync(CancellationToken ct = default);
    Task<Quote> CreateAsync(Quote quote, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<int> GetCountAsync(CancellationToken ct = default);
}

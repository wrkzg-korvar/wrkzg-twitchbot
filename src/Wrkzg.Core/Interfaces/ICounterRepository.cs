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
    Task<IReadOnlyList<Counter>> GetAllAsync(CancellationToken ct = default);
    Task<Counter?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Counter?> GetByTriggerAsync(string trigger, CancellationToken ct = default);
    Task<Counter> CreateAsync(Counter counter, CancellationToken ct = default);
    Task UpdateAsync(Counter counter, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

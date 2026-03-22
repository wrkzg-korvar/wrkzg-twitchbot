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
    Task<IReadOnlyList<TimedMessage>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TimedMessage>> GetEnabledAsync(CancellationToken ct = default);
    Task<TimedMessage?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<TimedMessage> CreateAsync(TimedMessage timer, CancellationToken ct = default);
    Task UpdateAsync(TimedMessage timer, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

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
    Task<Raffle?> GetActiveAsync(CancellationToken ct = default);
    Task<Raffle?> GetWithEntriesAsync(int raffleId, CancellationToken ct = default);
    Task<IReadOnlyList<Raffle>> GetAllAsync(CancellationToken ct = default);
    Task<Raffle> CreateAsync(Raffle raffle, CancellationToken ct = default);
    Task UpdateAsync(Raffle raffle, CancellationToken ct = default);
    Task AddEntryAsync(RaffleEntry entry, CancellationToken ct = default);
}

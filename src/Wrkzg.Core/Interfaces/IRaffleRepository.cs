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
    Task<Raffle?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Raffle?> GetWithEntriesAsync(int raffleId, CancellationToken ct = default);
    Task<IReadOnlyList<Raffle>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Raffle>> GetRecentAsync(int count = 10, CancellationToken ct = default);
    Task<Raffle> CreateAsync(Raffle raffle, CancellationToken ct = default);
    Task UpdateAsync(Raffle raffle, CancellationToken ct = default);
    Task AddEntryAsync(RaffleEntry entry, CancellationToken ct = default);
    Task<bool> HasUserEnteredAsync(int raffleId, int userId, CancellationToken ct = default);
    Task<int> GetEntryCountAsync(int raffleId, CancellationToken ct = default);
    Task AddDrawAsync(RaffleDraw draw, CancellationToken ct = default);
    Task<IReadOnlyList<RaffleDraw>> GetDrawsAsync(int raffleId, CancellationToken ct = default);
}

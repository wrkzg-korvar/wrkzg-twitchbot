using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Repository for Channel Point Reward handlers.
/// </summary>
public interface IChannelPointRewardRepository
{
    Task<IReadOnlyList<ChannelPointReward>> GetAllAsync(CancellationToken ct = default);
    Task<ChannelPointReward?> GetByTwitchRewardIdAsync(string twitchRewardId, CancellationToken ct = default);
    Task<ChannelPointReward> CreateAsync(ChannelPointReward reward, CancellationToken ct = default);
    Task UpdateAsync(ChannelPointReward reward, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

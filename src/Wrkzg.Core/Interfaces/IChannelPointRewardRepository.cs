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
    /// <summary>
    /// Retrieves all configured channel point reward handlers.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of all channel point rewards.</returns>
    Task<IReadOnlyList<ChannelPointReward>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Finds a channel point reward handler by its Twitch reward identifier.
    /// </summary>
    /// <param name="twitchRewardId">The Twitch-assigned reward identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching reward handler, or null if not found.</returns>
    Task<ChannelPointReward?> GetByTwitchRewardIdAsync(string twitchRewardId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new channel point reward handler.
    /// </summary>
    /// <param name="reward">The reward handler to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created reward handler with its assigned database identifier.</returns>
    Task<ChannelPointReward> CreateAsync(ChannelPointReward reward, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing channel point reward handler.
    /// </summary>
    /// <param name="reward">The reward handler with updated values.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateAsync(ChannelPointReward reward, CancellationToken ct = default);

    /// <summary>
    /// Deletes a channel point reward handler by its database identifier.
    /// </summary>
    /// <param name="id">The database identifier of the reward handler to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(int id, CancellationToken ct = default);
}

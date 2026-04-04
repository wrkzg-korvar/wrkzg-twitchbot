using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Infrastructure.Data;

namespace Wrkzg.Infrastructure.Repositories;

/// <summary>
/// SQLite-backed repository for channel point reward configuration persistence.
/// </summary>
public class ChannelPointRewardRepository : IChannelPointRewardRepository
{
    private readonly BotDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChannelPointRewardRepository"/> class.
    /// </summary>
    /// <param name="db">The bot database context.</param>
    public ChannelPointRewardRepository(BotDbContext db)
    {
        _db = db;
    }

    /// <summary>Gets all channel point rewards ordered by title.</summary>
    public async Task<IReadOnlyList<ChannelPointReward>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.ChannelPointRewards.OrderBy(r => r.Title).ToListAsync(ct);
    }

    /// <summary>Gets a channel point reward by its Twitch-assigned reward identifier.</summary>
    public async Task<ChannelPointReward?> GetByTwitchRewardIdAsync(string twitchRewardId, CancellationToken ct = default)
    {
        return await _db.ChannelPointRewards
            .FirstOrDefaultAsync(r => r.TwitchRewardId == twitchRewardId, ct);
    }

    /// <summary>Creates a new channel point reward and persists it to the database.</summary>
    public async Task<ChannelPointReward> CreateAsync(ChannelPointReward reward, CancellationToken ct = default)
    {
        _db.ChannelPointRewards.Add(reward);
        await _db.SaveChangesAsync(ct);
        return reward;
    }

    /// <summary>Updates an existing channel point reward in the database.</summary>
    public async Task UpdateAsync(ChannelPointReward reward, CancellationToken ct = default)
    {
        _db.ChannelPointRewards.Update(reward);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Deletes a channel point reward by its database identifier.</summary>
    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        ChannelPointReward? reward = await _db.ChannelPointRewards.FindAsync(new object[] { id }, ct);
        if (reward is not null)
        {
            _db.ChannelPointRewards.Remove(reward);
            await _db.SaveChangesAsync(ct);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Infrastructure.Data;

namespace Wrkzg.Infrastructure.Repositories;

public class ChannelPointRewardRepository : IChannelPointRewardRepository
{
    private readonly BotDbContext _db;

    public ChannelPointRewardRepository(BotDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ChannelPointReward>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.ChannelPointRewards.OrderBy(r => r.Title).ToListAsync(ct);
    }

    public async Task<ChannelPointReward?> GetByTwitchRewardIdAsync(string twitchRewardId, CancellationToken ct = default)
    {
        return await _db.ChannelPointRewards
            .FirstOrDefaultAsync(r => r.TwitchRewardId == twitchRewardId, ct);
    }

    public async Task<ChannelPointReward> CreateAsync(ChannelPointReward reward, CancellationToken ct = default)
    {
        _db.ChannelPointRewards.Add(reward);
        await _db.SaveChangesAsync(ct);
        return reward;
    }

    public async Task UpdateAsync(ChannelPointReward reward, CancellationToken ct = default)
    {
        _db.ChannelPointRewards.Update(reward);
        await _db.SaveChangesAsync(ct);
    }

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

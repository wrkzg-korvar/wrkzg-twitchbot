using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Infrastructure.Data;

namespace Wrkzg.Infrastructure.Repositories;

public class PollRepository : IPollRepository
{
    private readonly BotDbContext _db;

    public PollRepository(BotDbContext db)
    {
        _db = db;
    }

    public async Task<Poll?> GetActiveAsync(CancellationToken ct = default)
    {
        return await _db.Polls
            .Include(p => p.Votes)
            .FirstOrDefaultAsync(p => p.IsActive, ct);
    }

    public async Task<IReadOnlyList<Poll>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Polls
            .Include(p => p.Votes)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Poll> CreateAsync(Poll poll, CancellationToken ct = default)
    {
        _db.Polls.Add(poll);
        await _db.SaveChangesAsync(ct);
        return poll;
    }

    public async Task UpdateAsync(Poll poll, CancellationToken ct = default)
    {
        _db.Polls.Update(poll);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddVoteAsync(PollVote vote, CancellationToken ct = default)
    {
        _db.PollVotes.Add(vote);
        await _db.SaveChangesAsync(ct);
    }
}

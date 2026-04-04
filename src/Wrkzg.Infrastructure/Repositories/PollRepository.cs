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
/// SQLite-backed repository for poll and poll vote persistence.
/// </summary>
public class PollRepository : IPollRepository
{
    private readonly BotDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="PollRepository"/> class.
    /// </summary>
    /// <param name="db">The bot database context.</param>
    public PollRepository(BotDbContext db)
    {
        _db = db;
    }

    /// <summary>Gets the currently active poll with its votes, or null if none is active.</summary>
    public async Task<Poll?> GetActiveAsync(CancellationToken ct = default)
    {
        return await _db.Polls
            .Include(p => p.Votes)
            .FirstOrDefaultAsync(p => p.IsActive, ct);
    }

    /// <summary>Gets a poll by its database identifier, including votes.</summary>
    public async Task<Poll?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.Polls
            .Include(p => p.Votes)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    /// <summary>Gets all polls with votes, ordered by most recent first.</summary>
    public async Task<IReadOnlyList<Poll>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Polls
            .Include(p => p.Votes)
            .OrderByDescending(p => p.Id)
            .ToListAsync(ct);
    }

    /// <summary>Gets the most recent polls with votes, limited by count.</summary>
    public async Task<IReadOnlyList<Poll>> GetRecentAsync(int count = 10, CancellationToken ct = default)
    {
        return await _db.Polls
            .Include(p => p.Votes)
            .OrderByDescending(p => p.Id)
            .Take(count)
            .ToListAsync(ct);
    }

    /// <summary>Creates a new poll and persists it to the database.</summary>
    public async Task<Poll> CreateAsync(Poll poll, CancellationToken ct = default)
    {
        _db.Polls.Add(poll);
        await _db.SaveChangesAsync(ct);
        return poll;
    }

    /// <summary>Updates an existing poll in the database.</summary>
    public async Task UpdateAsync(Poll poll, CancellationToken ct = default)
    {
        _db.Polls.Update(poll);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Adds a vote to a poll.</summary>
    public async Task AddVoteAsync(PollVote vote, CancellationToken ct = default)
    {
        _db.PollVotes.Add(vote);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Checks whether a user has already voted in a specific poll.</summary>
    public async Task<bool> HasUserVotedAsync(int pollId, int userId, CancellationToken ct = default)
    {
        return await _db.PollVotes
            .AnyAsync(v => v.PollId == pollId && v.UserId == userId, ct);
    }
}

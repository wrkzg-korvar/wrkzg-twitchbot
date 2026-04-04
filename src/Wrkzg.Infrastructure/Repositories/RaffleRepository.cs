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
/// SQLite-backed repository for raffle, raffle entry, and raffle draw persistence.
/// </summary>
public class RaffleRepository : IRaffleRepository
{
    private readonly BotDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="RaffleRepository"/> class.
    /// </summary>
    /// <param name="db">The bot database context.</param>
    public RaffleRepository(BotDbContext db)
    {
        _db = db;
    }

    /// <summary>Gets the currently open raffle with entries, draws, and pending winner.</summary>
    public async Task<Raffle?> GetActiveAsync(CancellationToken ct = default)
    {
        return await _db.Raffles
            .Include(r => r.Entries)
            .ThenInclude(e => e.User)
            .Include(r => r.Draws)
            .ThenInclude(d => d.User)
            .Include(r => r.PendingWinner)
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.IsOpen, ct);
    }

    /// <summary>Gets a raffle by its database identifier, including all related data.</summary>
    public async Task<Raffle?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.Raffles
            .Include(r => r.Entries)
            .ThenInclude(e => e.User)
            .Include(r => r.Winner)
            .Include(r => r.Draws)
            .ThenInclude(d => d.User)
            .Include(r => r.PendingWinner)
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    /// <summary>Gets a raffle with its entries and draw history by raffle identifier.</summary>
    public async Task<Raffle?> GetWithEntriesAsync(int raffleId, CancellationToken ct = default)
    {
        return await _db.Raffles
            .Include(r => r.Entries)
            .ThenInclude(e => e.User)
            .Include(r => r.Winner)
            .Include(r => r.Draws)
            .ThenInclude(d => d.User)
            .Include(r => r.PendingWinner)
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.Id == raffleId, ct);
    }

    /// <summary>Gets all raffles with their winners, ordered by most recent first.</summary>
    public async Task<IReadOnlyList<Raffle>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Raffles
            .Include(r => r.Winner)
            .OrderByDescending(r => r.Id)
            .ToListAsync(ct);
    }

    /// <summary>Gets the most recent raffles with winners and entries, limited by count.</summary>
    public async Task<IReadOnlyList<Raffle>> GetRecentAsync(int count = 10, CancellationToken ct = default)
    {
        return await _db.Raffles
            .Include(r => r.Winner)
            .Include(r => r.Entries)
            .AsSplitQuery()
            .OrderByDescending(r => r.Id)
            .Take(count)
            .ToListAsync(ct);
    }

    /// <summary>Creates a new raffle and persists it to the database.</summary>
    public async Task<Raffle> CreateAsync(Raffle raffle, CancellationToken ct = default)
    {
        _db.Raffles.Add(raffle);
        await _db.SaveChangesAsync(ct);
        return raffle;
    }

    /// <summary>Updates an existing raffle in the database.</summary>
    public async Task UpdateAsync(Raffle raffle, CancellationToken ct = default)
    {
        _db.Raffles.Update(raffle);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Adds a user entry to a raffle.</summary>
    public async Task AddEntryAsync(RaffleEntry entry, CancellationToken ct = default)
    {
        _db.RaffleEntries.Add(entry);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Checks whether a user has already entered a specific raffle.</summary>
    public async Task<bool> HasUserEnteredAsync(int raffleId, int userId, CancellationToken ct = default)
    {
        return await _db.RaffleEntries
            .AnyAsync(e => e.RaffleId == raffleId && e.UserId == userId, ct);
    }

    /// <summary>Gets the total number of entries for a specific raffle.</summary>
    public async Task<int> GetEntryCountAsync(int raffleId, CancellationToken ct = default)
    {
        return await _db.RaffleEntries.CountAsync(e => e.RaffleId == raffleId, ct);
    }

    /// <summary>Records a draw result for a raffle.</summary>
    public async Task AddDrawAsync(RaffleDraw draw, CancellationToken ct = default)
    {
        _db.RaffleDraws.Add(draw);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Gets all draws for a raffle ordered by draw number, including user data.</summary>
    public async Task<IReadOnlyList<RaffleDraw>> GetDrawsAsync(int raffleId, CancellationToken ct = default)
    {
        return await _db.RaffleDraws
            .Include(d => d.User)
            .Where(d => d.RaffleId == raffleId)
            .OrderBy(d => d.DrawNumber)
            .ToListAsync(ct);
    }
}

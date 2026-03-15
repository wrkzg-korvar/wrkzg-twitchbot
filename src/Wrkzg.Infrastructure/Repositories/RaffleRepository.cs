using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Infrastructure.Data;

namespace Wrkzg.Infrastructure.Repositories;

public class RaffleRepository : IRaffleRepository
{
    private readonly BotDbContext _db;

    public RaffleRepository(BotDbContext db)
    {
        _db = db;
    }

    public async Task<Raffle?> GetActiveAsync(CancellationToken ct = default)
    {
        return await _db.Raffles
            .Include(r => r.Entries)
            .ThenInclude(e => e.User)
            .FirstOrDefaultAsync(r => r.IsOpen, ct);
    }

    public async Task<Raffle?> GetWithEntriesAsync(int raffleId, CancellationToken ct = default)
    {
        return await _db.Raffles
            .Include(r => r.Entries)
            .ThenInclude(e => e.User)
            .Include(r => r.Winner)
            .FirstOrDefaultAsync(r => r.Id == raffleId, ct);
    }

    public async Task<IReadOnlyList<Raffle>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Raffles
            .Include(r => r.Winner)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Raffle> CreateAsync(Raffle raffle, CancellationToken ct = default)
    {
        _db.Raffles.Add(raffle);
        await _db.SaveChangesAsync(ct);
        return raffle;
    }

    public async Task UpdateAsync(Raffle raffle, CancellationToken ct = default)
    {
        _db.Raffles.Update(raffle);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddEntryAsync(RaffleEntry entry, CancellationToken ct = default)
    {
        _db.RaffleEntries.Add(entry);
        await _db.SaveChangesAsync(ct);
    }
}

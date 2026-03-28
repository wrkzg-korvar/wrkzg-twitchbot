using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Infrastructure.Data;

namespace Wrkzg.Infrastructure.Repositories;

public class QuoteRepository : IQuoteRepository
{
    private readonly BotDbContext _db;

    public QuoteRepository(BotDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Quote>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Quotes.OrderByDescending(q => q.Number).ToListAsync(ct);
    }

    public async Task<Quote?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.Quotes.FindAsync(new object[] { id }, ct);
    }

    public async Task<Quote?> GetByNumberAsync(int number, CancellationToken ct = default)
    {
        return await _db.Quotes.FirstOrDefaultAsync(q => q.Number == number, ct);
    }

    public async Task<Quote?> GetRandomAsync(CancellationToken ct = default)
    {
        int count = await _db.Quotes.CountAsync(ct);
        if (count == 0)
        {
            return null;
        }

        int skip = Random.Shared.Next(count);
        return await _db.Quotes.OrderBy(q => q.Id).Skip(skip).FirstOrDefaultAsync(ct);
    }

    public async Task<int> GetNextNumberAsync(CancellationToken ct = default)
    {
        int? maxNumber = await _db.Quotes.MaxAsync(q => (int?)q.Number, ct);
        return (maxNumber ?? 0) + 1;
    }

    public async Task<Quote> CreateAsync(Quote quote, CancellationToken ct = default)
    {
        _db.Quotes.Add(quote);
        await _db.SaveChangesAsync(ct);
        return quote;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        Quote? quote = await _db.Quotes.FindAsync(new object[] { id }, ct);
        if (quote is not null)
        {
            _db.Quotes.Remove(quote);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<int> GetCountAsync(CancellationToken ct = default)
    {
        return await _db.Quotes.CountAsync(ct);
    }
}

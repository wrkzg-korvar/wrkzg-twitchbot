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

/// <summary>
/// SQLite-backed repository for quote persistence.
/// </summary>
public class QuoteRepository : IQuoteRepository
{
    private readonly BotDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="QuoteRepository"/> class.
    /// </summary>
    /// <param name="db">The bot database context.</param>
    public QuoteRepository(BotDbContext db)
    {
        _db = db;
    }

    /// <summary>Gets all quotes ordered by number descending (newest first).</summary>
    public async Task<IReadOnlyList<Quote>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Quotes.OrderByDescending(q => q.Number).ToListAsync(ct);
    }

    /// <summary>Gets a quote by its database identifier.</summary>
    public async Task<Quote?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.Quotes.FindAsync(new object[] { id }, ct);
    }

    /// <summary>Gets a quote by its sequential display number.</summary>
    public async Task<Quote?> GetByNumberAsync(int number, CancellationToken ct = default)
    {
        return await _db.Quotes.FirstOrDefaultAsync(q => q.Number == number, ct);
    }

    /// <summary>Gets a random quote from the database, or null if no quotes exist.</summary>
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

    /// <summary>Gets the next available sequential quote number.</summary>
    public async Task<int> GetNextNumberAsync(CancellationToken ct = default)
    {
        int? maxNumber = await _db.Quotes.MaxAsync(q => (int?)q.Number, ct);
        return (maxNumber ?? 0) + 1;
    }

    /// <summary>Creates a new quote and persists it to the database.</summary>
    public async Task<Quote> CreateAsync(Quote quote, CancellationToken ct = default)
    {
        _db.Quotes.Add(quote);
        await _db.SaveChangesAsync(ct);
        return quote;
    }

    /// <summary>Deletes a quote by its database identifier.</summary>
    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        Quote? quote = await _db.Quotes.FindAsync(new object[] { id }, ct);
        if (quote is not null)
        {
            _db.Quotes.Remove(quote);
            await _db.SaveChangesAsync(ct);
        }
    }

    /// <summary>Gets the total number of quotes in the database.</summary>
    public async Task<int> GetCountAsync(CancellationToken ct = default)
    {
        return await _db.Quotes.CountAsync(ct);
    }
}

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
/// SQLite-backed repository for trivia question persistence.
/// </summary>
public class TriviaQuestionRepository : ITriviaQuestionRepository
{
    private readonly BotDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="TriviaQuestionRepository"/> class.
    /// </summary>
    /// <param name="db">The bot database context.</param>
    public TriviaQuestionRepository(BotDbContext db)
    {
        _db = db;
    }

    /// <summary>Gets all trivia questions ordered by category then by identifier.</summary>
    public async Task<IReadOnlyList<TriviaQuestion>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.TriviaQuestions.OrderBy(q => q.Category).ThenBy(q => q.Id).ToListAsync(ct);
    }

    /// <summary>Gets all custom (user-created) trivia questions ordered by identifier.</summary>
    public async Task<IReadOnlyList<TriviaQuestion>> GetCustomAsync(CancellationToken ct = default)
    {
        return await _db.TriviaQuestions.Where(q => q.IsCustom).OrderBy(q => q.Id).ToListAsync(ct);
    }

    /// <summary>Gets a random trivia question, or null if no questions exist.</summary>
    public async Task<TriviaQuestion?> GetRandomAsync(CancellationToken ct = default)
    {
        int count = await _db.TriviaQuestions.CountAsync(ct);
        if (count == 0)
        {
            return null;
        }

        int skip = Random.Shared.Next(count);
        return await _db.TriviaQuestions.OrderBy(q => q.Id).Skip(skip).FirstOrDefaultAsync(ct);
    }

    /// <summary>Creates a new trivia question and persists it to the database.</summary>
    public async Task<TriviaQuestion> CreateAsync(TriviaQuestion question, CancellationToken ct = default)
    {
        _db.TriviaQuestions.Add(question);
        await _db.SaveChangesAsync(ct);
        return question;
    }

    /// <summary>Deletes a trivia question by its database identifier.</summary>
    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        TriviaQuestion? question = await _db.TriviaQuestions.FindAsync(new object[] { id }, ct);
        if (question is not null)
        {
            _db.TriviaQuestions.Remove(question);
            await _db.SaveChangesAsync(ct);
        }
    }

    /// <summary>Gets the total number of trivia questions in the database.</summary>
    public async Task<int> GetCountAsync(CancellationToken ct = default)
    {
        return await _db.TriviaQuestions.CountAsync(ct);
    }
}

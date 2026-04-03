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

public class TriviaQuestionRepository : ITriviaQuestionRepository
{
    private readonly BotDbContext _db;

    public TriviaQuestionRepository(BotDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TriviaQuestion>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.TriviaQuestions.OrderBy(q => q.Category).ThenBy(q => q.Id).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<TriviaQuestion>> GetCustomAsync(CancellationToken ct = default)
    {
        return await _db.TriviaQuestions.Where(q => q.IsCustom).OrderBy(q => q.Id).ToListAsync(ct);
    }

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

    public async Task<TriviaQuestion> CreateAsync(TriviaQuestion question, CancellationToken ct = default)
    {
        _db.TriviaQuestions.Add(question);
        await _db.SaveChangesAsync(ct);
        return question;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        TriviaQuestion? question = await _db.TriviaQuestions.FindAsync(new object[] { id }, ct);
        if (question is not null)
        {
            _db.TriviaQuestions.Remove(question);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<int> GetCountAsync(CancellationToken ct = default)
    {
        return await _db.TriviaQuestions.CountAsync(ct);
    }
}

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Repository for trivia questions (built-in + custom).
/// </summary>
public interface ITriviaQuestionRepository
{
    Task<IReadOnlyList<TriviaQuestion>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TriviaQuestion>> GetCustomAsync(CancellationToken ct = default);
    Task<TriviaQuestion?> GetRandomAsync(CancellationToken ct = default);
    Task<TriviaQuestion> CreateAsync(TriviaQuestion question, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<int> GetCountAsync(CancellationToken ct = default);
}

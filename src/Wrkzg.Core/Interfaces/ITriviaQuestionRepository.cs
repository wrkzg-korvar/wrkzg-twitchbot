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
    /// <summary>
    /// Retrieves all trivia questions, both built-in and custom.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of all trivia questions.</returns>
    Task<IReadOnlyList<TriviaQuestion>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves only the custom (user-created) trivia questions.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of custom trivia questions.</returns>
    Task<IReadOnlyList<TriviaQuestion>> GetCustomAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves a random trivia question for use in a game round.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A randomly selected trivia question, or null if none exist.</returns>
    Task<TriviaQuestion?> GetRandomAsync(CancellationToken ct = default);

    /// <summary>
    /// Creates a new custom trivia question.
    /// </summary>
    /// <param name="question">The trivia question to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created question with its assigned database identifier.</returns>
    Task<TriviaQuestion> CreateAsync(TriviaQuestion question, CancellationToken ct = default);

    /// <summary>
    /// Deletes a trivia question by its database identifier.
    /// </summary>
    /// <param name="id">The database identifier of the question to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Returns the total number of trivia questions available.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The total question count.</returns>
    Task<int> GetCountAsync(CancellationToken ct = default);
}

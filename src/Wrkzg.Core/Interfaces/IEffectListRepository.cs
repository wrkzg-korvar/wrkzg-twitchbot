using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Repository for effect list automations.
/// </summary>
public interface IEffectListRepository
{
    /// <summary>
    /// Retrieves all effect lists, including disabled ones.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of all effect lists.</returns>
    Task<IReadOnlyList<EffectList>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves only the enabled effect lists for active processing.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of enabled effect lists.</returns>
    Task<IReadOnlyList<EffectList>> GetEnabledAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves an effect list by its database identifier.
    /// </summary>
    /// <param name="id">The database identifier of the effect list.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching effect list, or null if not found.</returns>
    Task<EffectList?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new effect list.
    /// </summary>
    /// <param name="effectList">The effect list to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created effect list with its assigned database identifier.</returns>
    Task<EffectList> CreateAsync(EffectList effectList, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing effect list (triggers, conditions, effects, enabled state).
    /// </summary>
    /// <param name="effectList">The effect list with updated values.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateAsync(EffectList effectList, CancellationToken ct = default);

    /// <summary>
    /// Deletes an effect list by its database identifier.
    /// </summary>
    /// <param name="id">The database identifier of the effect list to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(int id, CancellationToken ct = default);
}

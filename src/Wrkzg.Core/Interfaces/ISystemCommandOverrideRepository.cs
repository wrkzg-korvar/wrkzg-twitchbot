using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Repository for system command overrides (custom response text, enabled/disabled).
/// </summary>
public interface ISystemCommandOverrideRepository
{
    /// <summary>
    /// Retrieves an override for a specific system command trigger.
    /// </summary>
    /// <param name="trigger">The command trigger to look up (e.g. "!commands").</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The override configuration, or null if no override exists for the trigger.</returns>
    Task<SystemCommandOverride?> GetByTriggerAsync(string trigger, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all system command overrides.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of all override configurations.</returns>
    Task<IReadOnlyList<SystemCommandOverride>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Creates or updates an override for a system command.
    /// </summary>
    /// <param name="entity">The override configuration to save.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SaveAsync(SystemCommandOverride entity, CancellationToken ct = default);

    /// <summary>
    /// Deletes an override for a system command, restoring its default behavior.
    /// </summary>
    /// <param name="trigger">The command trigger whose override should be deleted.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(string trigger, CancellationToken ct = default);
}

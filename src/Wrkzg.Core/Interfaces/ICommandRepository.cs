using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Repository for custom chat commands.
/// </summary>
public interface ICommandRepository
{
    /// <summary>
    /// Retrieves a command by its database identifier.
    /// </summary>
    /// <param name="id">The database identifier of the command.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching command, or null if not found.</returns>
    Task<Command?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Finds a command by its primary trigger or any of its aliases (case-insensitive).
    /// </summary>
    /// <param name="trigger">The trigger or alias to search for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching command, or null if no command uses that trigger.</returns>
    Task<Command?> GetByTriggerOrAliasAsync(string trigger, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all custom commands.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of all custom commands.</returns>
    Task<IReadOnlyList<Command>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Creates a new custom command.
    /// </summary>
    /// <param name="command">The command to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created command with its assigned database identifier.</returns>
    Task<Command> CreateAsync(Command command, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing custom command.
    /// </summary>
    /// <param name="command">The command with updated values.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateAsync(Command command, CancellationToken ct = default);

    /// <summary>
    /// Deletes a custom command by its database identifier.
    /// </summary>
    /// <param name="id">The database identifier of the command to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(int id, CancellationToken ct = default);
}

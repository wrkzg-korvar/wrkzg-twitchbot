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
    Task<Command?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Command?> GetByTriggerOrAliasAsync(string trigger, CancellationToken ct = default);
    Task<IReadOnlyList<Command>> GetAllAsync(CancellationToken ct = default);
    Task<Command> CreateAsync(Command command, CancellationToken ct = default);
    Task UpdateAsync(Command command, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

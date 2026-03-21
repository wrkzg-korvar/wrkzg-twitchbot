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
    Task<SystemCommandOverride?> GetByTriggerAsync(string trigger, CancellationToken ct = default);
    Task<IReadOnlyList<SystemCommandOverride>> GetAllAsync(CancellationToken ct = default);
    Task SaveAsync(SystemCommandOverride entity, CancellationToken ct = default);
    Task DeleteAsync(string trigger, CancellationToken ct = default);
}

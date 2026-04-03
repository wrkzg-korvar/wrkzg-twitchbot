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
    Task<IReadOnlyList<EffectList>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<EffectList>> GetEnabledAsync(CancellationToken ct = default);
    Task<EffectList?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<EffectList> CreateAsync(EffectList effectList, CancellationToken ct = default);
    Task UpdateAsync(EffectList effectList, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Repository for hotkey bindings.
/// </summary>
public interface IHotkeyBindingRepository
{
    Task<IReadOnlyList<HotkeyBinding>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<HotkeyBinding>> GetEnabledAsync(CancellationToken ct = default);
    Task<HotkeyBinding?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<HotkeyBinding> CreateAsync(HotkeyBinding binding, CancellationToken ct = default);
    Task UpdateAsync(HotkeyBinding binding, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

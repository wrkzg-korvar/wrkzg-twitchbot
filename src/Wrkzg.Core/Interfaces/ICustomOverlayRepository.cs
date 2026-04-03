using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Repository for custom user-created overlays.
/// </summary>
public interface ICustomOverlayRepository
{
    Task<IReadOnlyList<CustomOverlay>> GetAllAsync(CancellationToken ct = default);
    Task<CustomOverlay?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<CustomOverlay> CreateAsync(CustomOverlay overlay, CancellationToken ct = default);
    Task UpdateAsync(CustomOverlay overlay, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

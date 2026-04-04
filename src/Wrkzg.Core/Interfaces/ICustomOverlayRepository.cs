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
    /// <summary>
    /// Retrieves all custom overlays.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of all custom overlays.</returns>
    Task<IReadOnlyList<CustomOverlay>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves a custom overlay by its database identifier.
    /// </summary>
    /// <param name="id">The database identifier of the overlay.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching overlay, or null if not found.</returns>
    Task<CustomOverlay?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new custom overlay.
    /// </summary>
    /// <param name="overlay">The overlay to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created overlay with its assigned database identifier.</returns>
    Task<CustomOverlay> CreateAsync(CustomOverlay overlay, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing custom overlay.
    /// </summary>
    /// <param name="overlay">The overlay with updated values.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateAsync(CustomOverlay overlay, CancellationToken ct = default);

    /// <summary>
    /// Deletes a custom overlay by its database identifier.
    /// </summary>
    /// <param name="id">The database identifier of the overlay to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(int id, CancellationToken ct = default);
}

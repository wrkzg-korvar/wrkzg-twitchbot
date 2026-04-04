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
    /// <summary>
    /// Retrieves all hotkey bindings, including disabled ones.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of all hotkey bindings.</returns>
    Task<IReadOnlyList<HotkeyBinding>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves only the enabled hotkey bindings for active registration.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of enabled hotkey bindings.</returns>
    Task<IReadOnlyList<HotkeyBinding>> GetEnabledAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves a hotkey binding by its database identifier.
    /// </summary>
    /// <param name="id">The database identifier of the binding.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching hotkey binding, or null if not found.</returns>
    Task<HotkeyBinding?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new hotkey binding.
    /// </summary>
    /// <param name="binding">The hotkey binding to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created binding with its assigned database identifier.</returns>
    Task<HotkeyBinding> CreateAsync(HotkeyBinding binding, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing hotkey binding.
    /// </summary>
    /// <param name="binding">The binding with updated values.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateAsync(HotkeyBinding binding, CancellationToken ct = default);

    /// <summary>
    /// Deletes a hotkey binding by its database identifier.
    /// </summary>
    /// <param name="id">The database identifier of the binding to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(int id, CancellationToken ct = default);
}

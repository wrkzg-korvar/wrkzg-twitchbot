using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Infrastructure.Data;

namespace Wrkzg.Infrastructure.Repositories;

/// <summary>
/// SQLite-backed repository for hotkey binding persistence.
/// </summary>
public class HotkeyBindingRepository : IHotkeyBindingRepository
{
    private readonly BotDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="HotkeyBindingRepository"/> class.
    /// </summary>
    /// <param name="db">The bot database context.</param>
    public HotkeyBindingRepository(BotDbContext db)
    {
        _db = db;
    }

    /// <summary>Gets all hotkey bindings ordered by identifier.</summary>
    public async Task<IReadOnlyList<HotkeyBinding>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.HotkeyBindings.OrderBy(h => h.Id).ToListAsync(ct);
    }

    /// <summary>Gets all enabled hotkey bindings ordered by identifier.</summary>
    public async Task<IReadOnlyList<HotkeyBinding>> GetEnabledAsync(CancellationToken ct = default)
    {
        return await _db.HotkeyBindings.Where(h => h.IsEnabled).OrderBy(h => h.Id).ToListAsync(ct);
    }

    /// <summary>Gets a hotkey binding by its database identifier.</summary>
    public async Task<HotkeyBinding?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.HotkeyBindings.FindAsync(new object[] { id }, ct);
    }

    /// <summary>Creates a new hotkey binding and persists it to the database.</summary>
    public async Task<HotkeyBinding> CreateAsync(HotkeyBinding binding, CancellationToken ct = default)
    {
        _db.HotkeyBindings.Add(binding);
        await _db.SaveChangesAsync(ct);
        return binding;
    }

    /// <summary>Updates an existing hotkey binding in the database.</summary>
    public async Task UpdateAsync(HotkeyBinding binding, CancellationToken ct = default)
    {
        _db.HotkeyBindings.Update(binding);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Deletes a hotkey binding by its database identifier.</summary>
    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        HotkeyBinding? binding = await _db.HotkeyBindings.FindAsync(new object[] { id }, ct);
        if (binding is not null)
        {
            _db.HotkeyBindings.Remove(binding);
            await _db.SaveChangesAsync(ct);
        }
    }
}

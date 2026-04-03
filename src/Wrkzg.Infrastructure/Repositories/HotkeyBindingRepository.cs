using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Infrastructure.Data;

namespace Wrkzg.Infrastructure.Repositories;

public class HotkeyBindingRepository : IHotkeyBindingRepository
{
    private readonly BotDbContext _db;

    public HotkeyBindingRepository(BotDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<HotkeyBinding>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.HotkeyBindings.OrderBy(h => h.Id).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<HotkeyBinding>> GetEnabledAsync(CancellationToken ct = default)
    {
        return await _db.HotkeyBindings.Where(h => h.IsEnabled).OrderBy(h => h.Id).ToListAsync(ct);
    }

    public async Task<HotkeyBinding?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.HotkeyBindings.FindAsync(new object[] { id }, ct);
    }

    public async Task<HotkeyBinding> CreateAsync(HotkeyBinding binding, CancellationToken ct = default)
    {
        _db.HotkeyBindings.Add(binding);
        await _db.SaveChangesAsync(ct);
        return binding;
    }

    public async Task UpdateAsync(HotkeyBinding binding, CancellationToken ct = default)
    {
        _db.HotkeyBindings.Update(binding);
        await _db.SaveChangesAsync(ct);
    }

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

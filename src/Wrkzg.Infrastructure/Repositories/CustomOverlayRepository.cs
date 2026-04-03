using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Infrastructure.Data;

namespace Wrkzg.Infrastructure.Repositories;

public class CustomOverlayRepository : ICustomOverlayRepository
{
    private readonly BotDbContext _db;

    public CustomOverlayRepository(BotDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CustomOverlay>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.CustomOverlays.OrderBy(o => o.Id).ToListAsync(ct);
    }

    public async Task<CustomOverlay?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.CustomOverlays.FindAsync(new object[] { id }, ct);
    }

    public async Task<CustomOverlay> CreateAsync(CustomOverlay overlay, CancellationToken ct = default)
    {
        overlay.CreatedAt = DateTimeOffset.UtcNow;
        overlay.UpdatedAt = DateTimeOffset.UtcNow;
        _db.CustomOverlays.Add(overlay);
        await _db.SaveChangesAsync(ct);
        return overlay;
    }

    public async Task UpdateAsync(CustomOverlay overlay, CancellationToken ct = default)
    {
        overlay.UpdatedAt = DateTimeOffset.UtcNow;
        _db.CustomOverlays.Update(overlay);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        CustomOverlay? overlay = await _db.CustomOverlays.FindAsync(new object[] { id }, ct);
        if (overlay is not null)
        {
            _db.CustomOverlays.Remove(overlay);
            await _db.SaveChangesAsync(ct);
        }
    }
}

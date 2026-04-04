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

/// <summary>
/// SQLite-backed repository for custom overlay persistence.
/// </summary>
public class CustomOverlayRepository : ICustomOverlayRepository
{
    private readonly BotDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomOverlayRepository"/> class.
    /// </summary>
    /// <param name="db">The bot database context.</param>
    public CustomOverlayRepository(BotDbContext db)
    {
        _db = db;
    }

    /// <summary>Gets all custom overlays ordered by identifier.</summary>
    public async Task<IReadOnlyList<CustomOverlay>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.CustomOverlays.OrderBy(o => o.Id).ToListAsync(ct);
    }

    /// <summary>Gets a custom overlay by its database identifier.</summary>
    public async Task<CustomOverlay?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.CustomOverlays.FindAsync(new object[] { id }, ct);
    }

    /// <summary>Creates a new custom overlay with timestamps and persists it to the database.</summary>
    public async Task<CustomOverlay> CreateAsync(CustomOverlay overlay, CancellationToken ct = default)
    {
        overlay.CreatedAt = DateTimeOffset.UtcNow;
        overlay.UpdatedAt = DateTimeOffset.UtcNow;
        _db.CustomOverlays.Add(overlay);
        await _db.SaveChangesAsync(ct);
        return overlay;
    }

    /// <summary>Updates an existing custom overlay and refreshes its update timestamp.</summary>
    public async Task UpdateAsync(CustomOverlay overlay, CancellationToken ct = default)
    {
        overlay.UpdatedAt = DateTimeOffset.UtcNow;
        _db.CustomOverlays.Update(overlay);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Deletes a custom overlay by its database identifier.</summary>
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

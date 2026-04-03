using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;
using Wrkzg.Infrastructure.Data;

namespace Wrkzg.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly BotDbContext _db;

    public UserRepository(BotDbContext db)
    {
        _db = db;
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.Users.FindAsync(new object[] { id }, ct);
    }

    public async Task<User?> GetByTwitchIdAsync(string twitchId, CancellationToken ct = default)
    {
        return await _db.Users.FirstOrDefaultAsync(u => u.TwitchId == twitchId, ct);
    }

    public async Task<User> GetOrCreateAsync(string twitchId, string username, CancellationToken ct = default)
    {
        User? user = await GetByTwitchIdAsync(twitchId, ct);

        if (user is not null)
        {
            return user;
        }

        // Check if this user was imported (has placeholder TwitchId "imported_{username}").
        // If found, adopt the imported user and update their TwitchId to the real one.
        User? imported = await _db.Users.FirstOrDefaultAsync(
            u => u.Username == username && u.TwitchId.StartsWith("imported_"), ct);

        if (imported is not null)
        {
            imported.TwitchId = twitchId;
            await _db.SaveChangesAsync(ct);
            return imported;
        }

        user = new User
        {
            TwitchId = twitchId,
            Username = username,
            DisplayName = username
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return user;
    }

    public async Task<IReadOnlyList<User>> GetTopByPointsAsync(int count, CancellationToken ct = default)
    {
        return await _db.Users
            .OrderByDescending(u => u.Points)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<User>> GetTopByWatchTimeAsync(int count, CancellationToken ct = default)
    {
        return await _db.Users
            .OrderByDescending(u => u.WatchedMinutes)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Users.OrderBy(u => u.DisplayName).ToListAsync(ct);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        return await _db.Users.FirstOrDefaultAsync(
            u => u.Username == username, ct);
    }

    public async Task<User> CreateAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return user;
    }
}

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
/// SQLite-backed repository for Twitch user persistence.
/// Handles user creation, lookup, and merging of imported users with real Twitch accounts.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly BotDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserRepository"/> class.
    /// </summary>
    /// <param name="db">The bot database context.</param>
    public UserRepository(BotDbContext db)
    {
        _db = db;
    }

    /// <summary>Gets a user by their database identifier.</summary>
    public async Task<User?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.Users.FindAsync(new object[] { id }, ct);
    }

    /// <summary>Gets a user by their Twitch user identifier.</summary>
    public async Task<User?> GetByTwitchIdAsync(string twitchId, CancellationToken ct = default)
    {
        return await _db.Users.FirstOrDefaultAsync(u => u.TwitchId == twitchId, ct);
    }

    /// <summary>
    /// Gets an existing user by Twitch ID, adopts an imported user by username match,
    /// or creates a new user if neither exists.
    /// </summary>
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

    /// <summary>Gets the top users ranked by points, limited by count.</summary>
    public async Task<IReadOnlyList<User>> GetTopByPointsAsync(int count, CancellationToken ct = default)
    {
        return await _db.Users
            .OrderByDescending(u => u.Points)
            .Take(count)
            .ToListAsync(ct);
    }

    /// <summary>Gets the top users ranked by watch time, limited by count.</summary>
    public async Task<IReadOnlyList<User>> GetTopByWatchTimeAsync(int count, CancellationToken ct = default)
    {
        return await _db.Users
            .OrderByDescending(u => u.WatchedMinutes)
            .Take(count)
            .ToListAsync(ct);
    }

    /// <summary>Updates an existing user in the database.</summary>
    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Gets all users ordered alphabetically by display name.</summary>
    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Users.OrderBy(u => u.DisplayName).ToListAsync(ct);
    }

    /// <summary>Gets a user by their Twitch username.</summary>
    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        return await _db.Users.FirstOrDefaultAsync(
            u => u.Username == username, ct);
    }

    /// <summary>Creates a new user and persists it to the database.</summary>
    public async Task<User> CreateAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return user;
    }

    /// <summary>Returns a paginated list of users with server-side sorting and search.</summary>
    public async Task<PaginatedResult<User>> GetPaginatedAsync(
        string? search, string sortBy, string sortDirection,
        int page, int pageSize, CancellationToken ct = default)
    {
        IQueryable<User> query = _db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string lower = search.ToLowerInvariant();
            query = query.Where(u =>
                u.Username.ToLower().Contains(lower) ||
                u.DisplayName.ToLower().Contains(lower));
        }

        int totalCount = await query.CountAsync(ct);

        bool desc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        query = sortBy?.ToLowerInvariant() switch
        {
            "username" => desc ? query.OrderByDescending(u => u.Username) : query.OrderBy(u => u.Username),
            "watchtime" => desc ? query.OrderByDescending(u => u.WatchedMinutes) : query.OrderBy(u => u.WatchedMinutes),
            "messages" => desc ? query.OrderByDescending(u => u.MessageCount) : query.OrderBy(u => u.MessageCount),
            "lastseen" => desc ? query.OrderByDescending(u => u.LastSeenAt) : query.OrderBy(u => u.LastSeenAt),
            "firstseen" => desc ? query.OrderByDescending(u => u.FirstSeenAt) : query.OrderBy(u => u.FirstSeenAt),
            _ => desc ? query.OrderByDescending(u => u.Points) : query.OrderBy(u => u.Points),
        };

        int skip = (page - 1) * pageSize;
        List<User> items = await query.Skip(skip).Take(pageSize).ToListAsync(ct);

        return new PaginatedResult<User>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    /// <summary>Returns the total number of tracked users.</summary>
    public Task<int> CountAsync(CancellationToken ct = default)
    {
        return _db.Users.CountAsync(ct);
    }
}

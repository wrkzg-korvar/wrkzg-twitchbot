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
/// SQLite-backed repository for role and user-role assignment persistence.
/// </summary>
public class RoleRepository : IRoleRepository
{
    private readonly BotDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoleRepository"/> class.
    /// </summary>
    /// <param name="db">The bot database context.</param>
    public RoleRepository(BotDbContext db)
    {
        _db = db;
    }

    /// <summary>Gets all roles ordered by priority descending (highest first).</summary>
    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Roles.OrderByDescending(r => r.Priority).ToListAsync(ct);
    }

    /// <summary>Gets a role by its database identifier.</summary>
    public async Task<Role?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.Roles.FindAsync(new object[] { id }, ct);
    }

    /// <summary>Creates a new role and persists it to the database.</summary>
    public async Task<Role> CreateAsync(Role role, CancellationToken ct = default)
    {
        _db.Roles.Add(role);
        await _db.SaveChangesAsync(ct);
        return role;
    }

    /// <summary>Updates an existing role in the database.</summary>
    public async Task UpdateAsync(Role role, CancellationToken ct = default)
    {
        _db.Roles.Update(role);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>Deletes a role by its database identifier, removing all user assignments first.</summary>
    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        Role? role = await _db.Roles.FindAsync(new object[] { id }, ct);
        if (role is not null)
        {
            // Remove all user-role assignments first
            List<UserRole> assignments = await _db.UserRoles
                .Where(ur => ur.RoleId == id)
                .ToListAsync(ct);
            _db.UserRoles.RemoveRange(assignments);

            _db.Roles.Remove(role);
            await _db.SaveChangesAsync(ct);
        }
    }

    /// <summary>Gets all roles assigned to a user, ordered by priority descending.</summary>
    public async Task<IReadOnlyList<Role>> GetUserRolesAsync(int userId, CancellationToken ct = default)
    {
        return await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .OrderByDescending(ur => ur.Role.Priority)
            .Select(ur => ur.Role)
            .ToListAsync(ct);
    }

    /// <summary>Assigns a role to a user if not already assigned.</summary>
    public async Task AssignRoleAsync(int userId, int roleId, bool isAutoAssigned, CancellationToken ct = default)
    {
        bool exists = await _db.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId, ct);

        if (!exists)
        {
            _db.UserRoles.Add(new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                IsAutoAssigned = isAutoAssigned
            });
            await _db.SaveChangesAsync(ct);
        }
    }

    /// <summary>Removes a role assignment from a user.</summary>
    public async Task RemoveRoleAsync(int userId, int roleId, CancellationToken ct = default)
    {
        UserRole? assignment = await _db.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, ct);

        if (assignment is not null)
        {
            _db.UserRoles.Remove(assignment);
            await _db.SaveChangesAsync(ct);
        }
    }

    /// <summary>Gets the highest role priority value for a user, or 0 if the user has no roles.</summary>
    public async Task<int> GetHighestPriorityForUserAsync(int userId, CancellationToken ct = default)
    {
        bool hasRoles = await _db.UserRoles.AnyAsync(ur => ur.UserId == userId, ct);
        if (!hasRoles)
        {
            return 0;
        }

        return await _db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .MaxAsync(ur => ur.Role.Priority, ct);
    }

    /// <summary>Gets all users assigned to a specific role, ordered by display name.</summary>
    public async Task<IReadOnlyList<User>> GetUsersWithRoleAsync(int roleId, CancellationToken ct = default)
    {
        return await _db.UserRoles
            .Where(ur => ur.RoleId == roleId)
            .Include(ur => ur.User)
            .Select(ur => ur.User)
            .OrderBy(u => u.DisplayName)
            .ToListAsync(ct);
    }

    /// <summary>Gets the number of users assigned to a specific role.</summary>
    public async Task<int> GetUserCountForRoleAsync(int roleId, CancellationToken ct = default)
    {
        return await _db.UserRoles.CountAsync(ur => ur.RoleId == roleId, ct);
    }

    /// <summary>Checks whether a user's role assignment was auto-assigned by criteria rules.</summary>
    public async Task<bool> IsAutoAssignedAsync(int userId, int roleId, CancellationToken ct = default)
    {
        UserRole? assignment = await _db.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, ct);
        return assignment?.IsAutoAssigned ?? false;
    }
}

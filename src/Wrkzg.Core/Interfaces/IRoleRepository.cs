using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Repository for community roles/ranks.
/// </summary>
public interface IRoleRepository
{
    /// <summary>
    /// Retrieves all defined roles ordered by priority.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of all roles.</returns>
    Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves a role by its database identifier.
    /// </summary>
    /// <param name="id">The database identifier of the role.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching role, or null if not found.</returns>
    Task<Role?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new role.
    /// </summary>
    /// <param name="role">The role to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created role with its assigned database identifier.</returns>
    Task<Role> CreateAsync(Role role, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing role (name, priority, color, etc.).
    /// </summary>
    /// <param name="role">The role with updated values.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateAsync(Role role, CancellationToken ct = default);

    /// <summary>
    /// Deletes a role by its database identifier. Also removes all user-role assignments.
    /// </summary>
    /// <param name="id">The database identifier of the role to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all roles assigned to a specific user.
    /// </summary>
    /// <param name="userId">The database identifier of the user.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of roles assigned to the user.</returns>
    Task<IReadOnlyList<Role>> GetUserRolesAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// Assigns a role to a user.
    /// </summary>
    /// <param name="userId">The database identifier of the user.</param>
    /// <param name="roleId">The database identifier of the role to assign.</param>
    /// <param name="isAutoAssigned">Whether the role was assigned automatically (e.g. by subscriber status).</param>
    /// <param name="ct">Cancellation token.</param>
    Task AssignRoleAsync(int userId, int roleId, bool isAutoAssigned, CancellationToken ct = default);

    /// <summary>
    /// Removes a role assignment from a user.
    /// </summary>
    /// <param name="userId">The database identifier of the user.</param>
    /// <param name="roleId">The database identifier of the role to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RemoveRoleAsync(int userId, int roleId, CancellationToken ct = default);

    /// <summary>
    /// Returns the highest role priority value among all roles assigned to a user.
    /// Higher priority grants more permissions. Returns 0 if the user has no roles.
    /// </summary>
    /// <param name="userId">The database identifier of the user.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The highest priority value, or 0 if no roles are assigned.</returns>
    Task<int> GetHighestPriorityForUserAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all users who have a specific role assigned.
    /// </summary>
    /// <param name="roleId">The database identifier of the role.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of users with the specified role.</returns>
    Task<IReadOnlyList<User>> GetUsersWithRoleAsync(int roleId, CancellationToken ct = default);

    /// <summary>
    /// Returns the number of users who have a specific role assigned.
    /// </summary>
    /// <param name="roleId">The database identifier of the role.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The count of users with the specified role.</returns>
    Task<int> GetUserCountForRoleAsync(int roleId, CancellationToken ct = default);

    /// <summary>
    /// Checks whether a specific role was auto-assigned to a user (versus manually assigned).
    /// </summary>
    /// <param name="userId">The database identifier of the user.</param>
    /// <param name="roleId">The database identifier of the role.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the role was auto-assigned to the user.</returns>
    Task<bool> IsAutoAssignedAsync(int userId, int roleId, CancellationToken ct = default);
}

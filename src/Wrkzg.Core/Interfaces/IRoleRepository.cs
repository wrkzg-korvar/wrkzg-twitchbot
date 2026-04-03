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
    Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken ct = default);
    Task<Role?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Role> CreateAsync(Role role, CancellationToken ct = default);
    Task UpdateAsync(Role role, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Role>> GetUserRolesAsync(int userId, CancellationToken ct = default);
    Task AssignRoleAsync(int userId, int roleId, bool isAutoAssigned, CancellationToken ct = default);
    Task RemoveRoleAsync(int userId, int roleId, CancellationToken ct = default);
    Task<int> GetHighestPriorityForUserAsync(int userId, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetUsersWithRoleAsync(int roleId, CancellationToken ct = default);
    Task<int> GetUserCountForRoleAsync(int roleId, CancellationToken ct = default);
    Task<bool> IsAutoAssignedAsync(int userId, int roleId, CancellationToken ct = default);
}

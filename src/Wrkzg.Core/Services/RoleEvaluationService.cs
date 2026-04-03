using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Services;

/// <summary>
/// Evaluates auto-assign criteria and updates user roles.
/// Called periodically by UserTrackingService (piggybacks on the existing 60s tick).
/// </summary>
public class RoleEvaluationService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RoleEvaluationService> _logger;

    public RoleEvaluationService(
        IServiceScopeFactory scopeFactory,
        ILogger<RoleEvaluationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Evaluates all auto-assign roles against a single user's stats.
    /// Returns true if any role was added or removed.
    /// </summary>
    public async Task<bool> EvaluateUserAsync(int userId, CancellationToken ct = default)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IRoleRepository roles = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
        IUserRepository users = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        User? user = await users.GetByIdAsync(userId, ct);
        if (user is null)
        {
            return false;
        }

        IReadOnlyList<Role> allRoles = await roles.GetAllAsync(ct);
        IReadOnlyList<Role> currentRoles = await roles.GetUserRolesAsync(userId, ct);
        bool changed = false;

        foreach (Role role in allRoles)
        {
            if (role.AutoAssign is null)
            {
                continue;
            }

            bool qualifies = EvaluateCriteria(user, role.AutoAssign);
            bool hasRole = currentRoles.Any(r => r.Id == role.Id);

            if (qualifies && !hasRole)
            {
                await roles.AssignRoleAsync(userId, role.Id, isAutoAssigned: true, ct);
                changed = true;
                _logger.LogInformation("Auto-assigned role {Role} to {User}", role.Name, user.DisplayName);
            }
            else if (!qualifies && hasRole)
            {
                // Only remove auto-assigned roles, keep manually assigned ones
                bool isAutoAssigned = await roles.IsAutoAssignedAsync(userId, role.Id, ct);
                if (isAutoAssigned)
                {
                    await roles.RemoveRoleAsync(userId, role.Id, ct);
                    changed = true;
                    _logger.LogInformation("Auto-removed role {Role} from {User}", role.Name, user.DisplayName);
                }
            }
        }

        return changed;
    }

    /// <summary>
    /// Bulk evaluation: checks all users against all auto-assign roles.
    /// Intended for manual re-evaluation from the dashboard.
    /// </summary>
    public async Task<int> EvaluateAllUsersAsync(CancellationToken ct = default)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        IUserRepository users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        IReadOnlyList<User> allUsers = await users.GetAllAsync(ct);

        int changedCount = 0;
        foreach (User user in allUsers)
        {
            bool changed = await EvaluateUserAsync(user.Id, ct);
            if (changed)
            {
                changedCount++;
            }
        }

        _logger.LogInformation("Role evaluation complete: {Count} users updated", changedCount);
        return changedCount;
    }

    private static bool EvaluateCriteria(User user, RoleAutoAssignCriteria criteria)
    {
        if (criteria.MinWatchedMinutes.HasValue && user.WatchedMinutes < criteria.MinWatchedMinutes.Value)
        {
            return false;
        }
        if (criteria.MinPoints.HasValue && user.Points < criteria.MinPoints.Value)
        {
            return false;
        }
        if (criteria.MinMessages.HasValue && user.MessageCount < criteria.MinMessages.Value)
        {
            return false;
        }
        if (criteria.MustBeSubscriber == true && !user.IsSubscriber)
        {
            return false;
        }
        if (criteria.MustBeFollower == true && !user.FollowDate.HasValue)
        {
            return false;
        }
        return true;
    }
}

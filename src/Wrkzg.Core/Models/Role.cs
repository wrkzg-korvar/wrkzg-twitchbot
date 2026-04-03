using System;

namespace Wrkzg.Core.Models;

/// <summary>
/// A community role/rank that users can achieve.
/// Roles are ordered by Priority (higher = more privileges).
/// </summary>
public class Role
{
    public int Id { get; set; }

    /// <summary>Display name (e.g. "Stammzuschauer", "Elite Viewer").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Priority/rank order. Higher = more privileges.
    /// Used for permission checks: user.HighestRole.Priority >= requiredRole.Priority
    /// </summary>
    public int Priority { get; set; }

    /// <summary>Optional color for display in dashboard/overlay (hex).</summary>
    public string? Color { get; set; }

    /// <summary>Optional icon/emoji for display.</summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Auto-assign criteria. Null = manual assignment only.
    /// </summary>
    public RoleAutoAssignCriteria? AutoAssign { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Criteria for automatic role assignment.
/// All non-null conditions must be met (AND logic).
/// </summary>
public class RoleAutoAssignCriteria
{
    /// <summary>Minimum watched minutes. Null = no requirement.</summary>
    public int? MinWatchedMinutes { get; set; }

    /// <summary>Minimum points. Null = no requirement.</summary>
    public long? MinPoints { get; set; }

    /// <summary>Minimum message count. Null = no requirement.</summary>
    public int? MinMessages { get; set; }

    /// <summary>Must be a follower. Null = no requirement.</summary>
    public bool? MustBeFollower { get; set; }

    /// <summary>Must be a subscriber. Null = no requirement.</summary>
    public bool? MustBeSubscriber { get; set; }
}

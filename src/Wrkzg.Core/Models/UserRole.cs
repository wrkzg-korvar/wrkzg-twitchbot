using System;

namespace Wrkzg.Core.Models;

/// <summary>
/// Junction table: User ↔ Role (many-to-many).
/// </summary>
public class UserRole
{
    /// <summary>Foreign key to the user.</summary>
    public int UserId { get; set; }

    /// <summary>Navigation property to the user.</summary>
    public User User { get; set; } = null!;

    /// <summary>Foreign key to the role.</summary>
    public int RoleId { get; set; }

    /// <summary>Navigation property to the role.</summary>
    public Role Role { get; set; } = null!;

    /// <summary>When this role was assigned to the user.</summary>
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Whether this was auto-assigned or manually set.</summary>
    public bool IsAutoAssigned { get; set; }
}

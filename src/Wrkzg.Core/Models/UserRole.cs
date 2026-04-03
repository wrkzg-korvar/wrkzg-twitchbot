using System;

namespace Wrkzg.Core.Models;

/// <summary>
/// Junction table: User ↔ Role (many-to-many).
/// </summary>
public class UserRole
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    /// <summary>When this role was assigned to the user.</summary>
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Whether this was auto-assigned or manually set.</summary>
    public bool IsAutoAssigned { get; set; }
}

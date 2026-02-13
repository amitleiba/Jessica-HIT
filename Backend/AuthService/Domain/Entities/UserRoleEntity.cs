namespace AuthService.Domain.Entities;

/// <summary>
/// Join table for the many-to-many relationship between Users and Roles.
/// Composite PK: (UserId, RoleId)
/// </summary>
public class UserRoleEntity
{
    public Guid UserId { get; set; }
    public UserEntity User { get; set; } = null!;

    public int RoleId { get; set; }
    public RoleEntity Role { get; set; } = null!;
}


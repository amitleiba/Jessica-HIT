namespace AuthService.Domain.Entities;

/// <summary>
/// Database entity representing a role (e.g. "Admin", "User", "Operator").
/// Seeded on startup via DatabaseExtensions.
/// </summary>
public class RoleEntity
{
    public int Id { get; set; }

    public required string Name { get; set; }

    // ── Navigation Properties ──
    public ICollection<UserRoleEntity> UserRoles { get; set; } = [];
}
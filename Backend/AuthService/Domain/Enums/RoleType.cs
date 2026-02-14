namespace AuthService.Domain.Enums;

/// <summary>
/// Defines the available system roles.
/// These are seeded into the Roles table on startup.
/// </summary>
public static class RoleType
{
    public const string Admin = "Admin";
    public const string User = "User";
    public const string Operator = "Operator";

    /// <summary>
    /// All roles that should be seeded into the database.
    /// </summary>
    public static readonly string[] All = [Admin, User, Operator];
}
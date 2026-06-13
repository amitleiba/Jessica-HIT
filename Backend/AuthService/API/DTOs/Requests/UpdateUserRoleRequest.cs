using System.ComponentModel.DataAnnotations;

namespace AuthService.API.DTOs.Requests;

/// <summary>
/// Request DTO for admin to update a user's role.
/// </summary>
public class UpdateUserRoleRequest
{
    [Required(ErrorMessage = "Role is required")]
    public required string Role { get; init; }
}

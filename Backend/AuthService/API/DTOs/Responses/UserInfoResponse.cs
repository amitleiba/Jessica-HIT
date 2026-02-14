using System.ComponentModel.DataAnnotations;

namespace AuthService.API.DTOs.Responses;

/// <summary>
/// Response DTO for user-info endpoint.
/// Matches the frontend's existing UserInfoResponse contract exactly.
/// </summary>
public class UserInfoResponse
{
    [Required]
    public required bool IsAuthenticated { get; init; }

    public string? Username { get; init; }

    public string? AuthenticationType { get; init; }

    [Required]
    public required IReadOnlyCollection<ClaimDto> Claims { get; init; }
}
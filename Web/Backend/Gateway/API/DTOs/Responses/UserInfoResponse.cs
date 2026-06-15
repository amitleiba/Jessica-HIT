using System.ComponentModel.DataAnnotations;

namespace Gateway.API.DTOs.Responses;

/// <summary>
/// User-info response (Gateway BFF contract; aligns with AuthService / frontend).
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

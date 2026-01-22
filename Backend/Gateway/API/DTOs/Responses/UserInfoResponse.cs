using System.ComponentModel.DataAnnotations;

namespace Gateway.API.DTOs.Responses;

/// <summary>
/// Response DTO for the user-info endpoint containing authentication status and user claims
/// </summary>
public class UserInfoResponse
{
    /// <summary>
    /// Indicates whether the user is authenticated
    /// </summary>
    [Required]
    public required bool IsAuthenticated { get; init; }

    /// <summary>
    /// The authenticated user's username (null if not authenticated)
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// The type of authentication used (e.g., "Bearer", "Cookie")
    /// </summary>
    public string? AuthenticationType { get; init; }

    /// <summary>
    /// List of claims associated with the user's token
    /// </summary>
    [Required]
    public required IReadOnlyCollection<ClaimDto> Claims { get; init; }
}

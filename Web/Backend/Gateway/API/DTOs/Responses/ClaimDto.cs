using System.ComponentModel.DataAnnotations;

namespace Gateway.API.DTOs.Responses;

/// <summary>
/// Single claim in user info (aligns with AuthService / frontend).
/// </summary>
public class ClaimDto
{
    [Required]
    public required string Type { get; init; }

    [Required]
    public required string Value { get; init; }
}

using System.ComponentModel.DataAnnotations;

namespace AuthService.API.DTOs.Responses;

/// <summary>
/// Represents a single claim in the user's token.
/// Matches the frontend's existing ClaimDto contract.
/// </summary>
public class ClaimDto
{
    [Required]
    public required string Type { get; init; }

    [Required]
    public required string Value { get; init; }
}


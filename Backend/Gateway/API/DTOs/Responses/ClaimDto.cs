using System.ComponentModel.DataAnnotations;

namespace Gateway.API.DTOs.Responses;

/// <summary>
/// Represents a single claim in the user's token
/// </summary>
public class ClaimDto
{
    /// <summary>
    /// The claim type (e.g., "preferred_username", "email")
    /// </summary>
    [Required]
    public required string Type { get; init; }

    /// <summary>
    /// The claim value
    /// </summary>
    [Required]
    public required string Value { get; init; }
}

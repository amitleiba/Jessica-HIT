using System.ComponentModel.DataAnnotations;

namespace Gateway.Domain.Models;

/// <summary>
/// Domain model representing a user
/// </summary>
public class User
{
    [Required]
    public required string Username { get; init; }

    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    [Required]
    public required string FirstName { get; init; }

    [Required]
    public required string LastName { get; init; }

    [Required]
    public required string Password { get; init; }
}

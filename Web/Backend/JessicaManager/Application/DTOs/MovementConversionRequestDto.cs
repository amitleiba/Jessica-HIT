using System.ComponentModel.DataAnnotations;

namespace JessicaManager.Application.DTOs;

public sealed class MovementConversionRequestDto
{
    [Required]
    public string Direction { get; init; } = string.Empty;

    [Range(0, 100)]
    public int Speed { get; init; }
}

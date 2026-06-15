using System.ComponentModel.DataAnnotations;

namespace Gateway.Domain.Configuration;

/// <summary>
/// CORS configuration for SignalR and API endpoints
/// </summary>
public class CorsConfig
{
    public const string SectionName = "Cors";

    [Required(ErrorMessage = "At least one allowed origin is required")]
    [MinLength(1, ErrorMessage = "At least one allowed origin is required")]
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();

    public bool AllowAnyHeader { get; set; } = true;

    public bool AllowAnyMethod { get; set; } = true;

    public bool AllowCredentials { get; set; } = true;
}

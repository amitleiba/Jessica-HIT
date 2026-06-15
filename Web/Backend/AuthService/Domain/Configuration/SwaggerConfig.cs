using System.ComponentModel.DataAnnotations;

namespace AuthService.Domain.Configuration;

/// <summary>
/// Swagger/OpenAPI configuration.
/// Bound from appsettings.json "Swagger" section.
/// </summary>
public class SwaggerConfig
{
    public const string SectionName = "Swagger";

    [Required(ErrorMessage = "Swagger Title is required")]
    public string Title { get; set; } = "Auth API";

    [Required(ErrorMessage = "Swagger Version is required")]
    public string Version { get; set; } = "v1";

    public string Description { get; set; } = "Jessica Auth & User Management API";

    public bool Enabled { get; set; } = true;
}
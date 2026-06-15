using System.ComponentModel.DataAnnotations;

namespace Gateway.Domain.Configuration;

public class SwaggerConfig
{
    public const string SectionName = "Swagger";

    [Required(ErrorMessage = "Swagger Title is required")]
    public string Title { get; set; } = "Gateway API";

    [Required(ErrorMessage = "Swagger Version is required")]
    public string Version { get; set; } = "v1";

    public string Description { get; set; } = "Jessica Gateway API with Keycloak Authentication";

    public bool Enabled { get; set; } = true;
}

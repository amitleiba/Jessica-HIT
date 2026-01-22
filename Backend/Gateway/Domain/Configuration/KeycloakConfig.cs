using System.ComponentModel.DataAnnotations;

namespace Gateway.Domain.Configuration;

public class KeycloakConfig
{
    public const string SectionName = "Keycloak";

    /// <summary>
    /// Base URL of the Keycloak server (e.g., "http://localhost:8082")
    /// </summary>
    [Required(ErrorMessage = "Keycloak Url is required")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Realm name (e.g., "jessica-realm")
    /// </summary>
    [Required(ErrorMessage = "Keycloak Realm is required")]
    public string Realm { get; set; } = string.Empty;

    /// <summary>
    /// Computed authority URL combining Url and Realm (e.g., "http://localhost:8082/realms/jessica-realm")
    /// </summary>
    public string Authority => $"{Url.TrimEnd('/')}/realms/{Realm}";

    [Required(ErrorMessage = "Keycloak ClientId is required")]
    public string ClientId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Keycloak ClientSecret is required")]
    public string ClientSecret { get; set; } = string.Empty;

    public bool RequireHttpsMetadata { get; set; } = false;

    [Required(ErrorMessage = "ResponseType is required")]
    public string ResponseType { get; set; } = "code";

    public bool SaveTokens { get; set; } = true;

    public bool GetClaimsFromUserInfoEndpoint { get; set; } = true;

    [Required(ErrorMessage = "At least one scope is required")]
    [MinLength(1, ErrorMessage = "At least one scope is required")]
    public string[] Scope { get; set; } = Array.Empty<string>();
}

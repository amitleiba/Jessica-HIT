using System.ComponentModel.DataAnnotations;

namespace Gateway.Domain.Configuration;

public class ReverseProxyConfig
{
    public const string SectionName = "ReverseProxy";

    [Required]
    public Dictionary<string, RouteConfig> Routes { get; set; } = new();

    [Required]
    public Dictionary<string, ClusterConfig> Clusters { get; set; } = new();
}

public class RouteConfig
{
    [Required]
    public string ClusterId { get; set; } = string.Empty;

    public string? AuthorizationPolicy { get; set; }

    [Required]
    public MatchConfig Match { get; set; } = new();

    public List<TransformConfig>? Transforms { get; set; }
}

public class MatchConfig
{
    [Required]
    public string Path { get; set; } = string.Empty;
}

public class TransformConfig
{
    public string? PathPattern { get; set; }
}

public class ClusterConfig
{
    [Required]
    public Dictionary<string, DestinationConfig> Destinations { get; set; } = new();
}

public class DestinationConfig
{
    [Required]
    public string Address { get; set; } = string.Empty;
}

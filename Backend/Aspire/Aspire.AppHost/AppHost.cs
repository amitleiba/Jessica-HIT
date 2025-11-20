var builder = DistributedApplication.CreateBuilder(args);

// Add Gateway service
var gateway = builder.AddProject<Projects.Gateway>("gateway");

// Add JessicaManager service
var jessicaManager = builder.AddProject<Projects.JessicaManager>("jessicamanager");

// Add TileServer GL container
// tilesStorage path is relative to Backend directory (../../tilesStorage from AppHost location)
var tileserver = builder.AddContainer("tileserver", "maptiler/tileserver-gl", "latest")
    .WithHttpEndpoint(8080, 80, name: "http")
    .WithEnvironment("NODE_ENV", "production")
    .WithEnvironment("BIND", "0.0.0.0")
    .WithEnvironment("PORT", "80")
    .WithBindMount("../../tilesStorage", "/data");

await builder.Build().RunAsync().ConfigureAwait(false);

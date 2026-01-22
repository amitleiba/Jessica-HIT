var builder = DistributedApplication.CreateBuilder(args);

// ============================================
// KAFKA - COMMENTED OUT (Not needed for now)
// ============================================
// var kafka = builder.AddKafka("kafka")
//     .WithKafkaUI();

// Note: Keycloak is running via Docker Compose on port 8082
// Gateway connects to it via appsettings.json configuration
// Run: docker-compose up -d keycloak postgres-keycloak

// Add JessicaManager service
var jessicaManager = builder.AddProject<Projects.JessicaManager>("jessicamanager");
    // .WithReference(kafka);  // Commented out - Kafka not in use

// Add Gateway service
var gateway = builder.AddProject<Projects.Gateway>("gateway")
    .WithReference(jessicaManager);

await builder.Build().RunAsync().ConfigureAwait(false);

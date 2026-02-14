var builder = DistributedApplication.CreateBuilder(args);

// ============================================
// KAFKA - COMMENTED OUT (Not needed for now)
// ============================================
// var kafka = builder.AddKafka("kafka")
//     .WithKafkaUI();

// ============================================
// DATABASES
// ============================================

// PostgreSQL for AuthService — users, roles, refresh tokens
var postgresAuth = builder.AddPostgres("postgres-auth")
    .WithDataVolume("postgres-auth-data")
    .WithPgAdmin();

var authDb = postgresAuth.AddDatabase("AuthDb", databaseName: "jessica_auth");

// ============================================
// SERVICES
// ============================================

// JessicaManager service
var jessicaManager = builder.AddProject<Projects.JessicaManager>("jessicamanager");
// .WithReference(kafka);  // Commented out - Kafka not in use

// AuthService (custom auth + user management)
// Receives the PostgreSQL connection string automatically via Aspire service discovery
var authService = builder.AddProject<Projects.AuthService>("authservice")
    .WithReference(authDb)
    .WaitFor(authDb);

// Gateway (routes to JessicaManager + AuthService via YARP)
var gateway = builder.AddProject<Projects.Gateway>("gateway")
    .WithReference(jessicaManager)
    .WithReference(authService)
    .WaitFor(authService);

await builder.Build().RunAsync().ConfigureAwait(false);
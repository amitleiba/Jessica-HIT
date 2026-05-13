var builder = DistributedApplication.CreateBuilder(args);

// ============================================
// KAFKA - COMMENTED OUT (Not needed for now)
// ============================================
// var kafka = builder.AddKafka("kafka")
//     .WithKafkaUI();

// ============================================
// DATABASES
// ============================================

// One stable password for all local Postgres containers. When .WithDataVolume() is used, the
// password is fixed at first init; if Aspire ever connected with a different generated password,
// health checks fail with "password authentication failed for user postgres". Override via
// Parameters:postgres-password in appsettings / user-secrets if needed.
var postgresPassword = builder.AddParameter("postgres-password", "JessicaHit_LocalDev_Pg_9", secret: false);

// PostgreSQL for AuthService — users, roles, refresh tokens
var postgresAuth = builder.AddPostgres("postgres-auth", password: postgresPassword)
    .WithDataVolume("jessica-hit-auth-pgdata")
    .WithPgAdmin();

var authDb = postgresAuth.AddDatabase("AuthDb", databaseName: "jessica_auth");

// PostgreSQL for RecordingManager — recordings and events
var postgresRecording = builder.AddPostgres("postgres-recording", password: postgresPassword)
    .WithDataVolume("jessica-hit-recording-pgdata")
    .WithPgAdmin();

var recordingDb = postgresRecording.AddDatabase("RecordingDb", databaseName: "jessica_recordings");

// ============================================
// SERVICES
// ============================================

// External Jessica robot WS endpoint (override per environment).
var jessicaWsUrl = builder.AddParameter("jessica-ws-url", "ws://127.0.0.1:8765", secret: false);

// JessicaManager service
var jessicaManager = builder.AddProject<Projects.JessicaManager>("jessicamanager")
    .WithEnvironment("JessicaWebSocket__Url", jessicaWsUrl);
// .WithReference(kafka);  // Commented out - Kafka not in use

// AuthService (custom auth + user management)
// Receives the PostgreSQL connection string automatically via Aspire service discovery
var authService = builder.AddProject<Projects.AuthService>("authservice")
    .WithReference(authDb)
    .WaitFor(authDb);

// RecordingManager service (recordings CRUD + PostgreSQL)
var recordingManager = builder.AddProject<Projects.RecordingManager>("recordingmanager")
    .WithReference(recordingDb)
    .WaitFor(recordingDb);

// Gateway (routes to JessicaManager + AuthService + RecordingManager via YARP)
var gateway = builder.AddProject<Projects.Gateway>("gateway")
    .WithReference(jessicaManager)
    .WithReference(authService)
    .WithReference(recordingManager)
    .WaitFor(authService)
    .WaitFor(recordingManager);

await builder.Build().RunAsync().ConfigureAwait(false);
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

// PostgreSQL for RecordingManager — recordings and events
var postgresRecording = builder.AddPostgres("postgres-recording")
    .WithDataVolume("postgres-recording-data")
    .WithPgAdmin();

var recordingDb = postgresRecording.AddDatabase("RecordingDb", databaseName: "jessica_recordings");

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
using Microsoft.EntityFrameworkCore;
using RecordingManager.Infrastructure.Persistence;

namespace RecordingManager.Extensions;

/// <summary>
/// Extension methods for database configuration and initialization.
/// </summary>
public static class DatabaseExtensions
{
    /// <summary>
    /// Registers the RecordingManagerDbContext with PostgreSQL (Npgsql)
    /// using the "RecordingDb" connection string.
    /// </summary>
    public static IServiceCollection AddRecordingDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("RecordingDb")
            ?? throw new InvalidOperationException("Connection string 'RecordingDb' is missing");

        services.AddDbContext<RecordingManagerDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }

    /// <summary>
    /// Applies pending EF Core migrations on startup.
    /// Falls back to EnsureCreated in development.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RecordingManagerDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<RecordingManagerDbContext>>();

        try
        {
            if (app.Environment.IsDevelopment())
            {
                logger.LogInformation("Applying RecordingManager database migrations...");
                await context.Database.MigrateAsync().ConfigureAwait(false);
                logger.LogInformation("RecordingManager database migrations applied successfully");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "RecordingManager database migration failed — falling back to EnsureCreated");

            if (app.Environment.IsDevelopment())
            {
                await context.Database.EnsureCreatedAsync().ConfigureAwait(false);
                logger.LogWarning(
                    "Database created via EnsureCreated — run 'dotnet ef migrations add InitialCreate' for proper migrations");
            }
        }
    }
}


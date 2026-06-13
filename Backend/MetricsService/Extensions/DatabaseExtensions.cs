using Microsoft.EntityFrameworkCore;
using MetricsService.Infrastructure.Persistence;

namespace MetricsService.Extensions;

/// <summary>
/// Extension methods for database configuration and initialization.
/// </summary>
public static class DatabaseExtensions
{
    /// <summary>
    /// Registers the MetricsDbContext with PostgreSQL (Npgsql)
    /// using the "MetricsDb" connection string.
    /// </summary>
    public static IServiceCollection AddMetricsDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("MetricsDb")
            ?? throw new InvalidOperationException("Connection string 'MetricsDb' is missing");

        services.AddDbContext<MetricsDbContext>(options =>
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
        var context = scope.ServiceProvider.GetRequiredService<MetricsDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<MetricsDbContext>>();

        try
        {
            logger.LogInformation("Applying MetricsService database migrations...");
            await context.Database.MigrateAsync().ConfigureAwait(false);
            logger.LogInformation("MetricsService database migrations applied successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MetricsService database migration failed — falling back to EnsureCreated");

            await context.Database.EnsureCreatedAsync().ConfigureAwait(false);
            logger.LogWarning(
                "Database created via EnsureCreated — run 'dotnet ef migrations add InitialCreate' for proper migrations");
        }
    }
}

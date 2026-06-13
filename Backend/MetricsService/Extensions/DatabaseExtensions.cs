using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
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
    /// Ensures that the database and its tables are created.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MetricsDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<MetricsDbContext>>();

        try
        {
            logger.LogInformation("Ensuring MetricsService database tables are created...");
            var creator = (IRelationalDatabaseCreator)context.Database.GetService<IDatabaseCreator>();

            if (!await creator.ExistsAsync().ConfigureAwait(false))
            {
                await creator.CreateAsync().ConfigureAwait(false);
            }

            try
            {
                await creator.CreateTablesAsync().ConfigureAwait(false);
                logger.LogInformation("MetricsService database tables created successfully");
            }
            catch (PostgresException ex) when (ex.SqlState == "42P07") // duplicate_table
            {
                logger.LogInformation("MetricsService database tables already exist — skipping creation");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MetricsService database creation failed");
        }
    }
}

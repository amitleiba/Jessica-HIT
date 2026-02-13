using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Extensions;

/// <summary>
/// Extension methods for database configuration, migration, and seeding.
/// </summary>
public static class DatabaseExtensions
{
    /// <summary>
    /// Registers the AuthDbContext with PostgreSQL (Npgsql) using the "AuthDb" connection string.
    /// </summary>
    public static IServiceCollection AddAuthDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AuthDb")
            ?? throw new InvalidOperationException("Connection string 'AuthDb' is missing");

        services.AddDbContext<AuthDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }

    /// <summary>
    /// Applies pending EF Core migrations and seeds default data.
    /// Call this after building the WebApplication.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AuthDbContext>>();

        try
        {
            // Apply pending migrations (or create DB if it doesn't exist)
            if (app.Environment.IsDevelopment())
            {
                logger.LogInformation("Applying database migrations...");
                await context.Database.MigrateAsync().ConfigureAwait(false);
                logger.LogInformation("Database migrations applied successfully");
            }

            // Seed default roles
            await SeedRolesAsync(context, logger).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database initialization failed — falling back to EnsureCreated");

            // Fallback: create tables without migration history (dev only)
            if (app.Environment.IsDevelopment())
            {
                await context.Database.EnsureCreatedAsync().ConfigureAwait(false);
                await SeedRolesAsync(context, logger).ConfigureAwait(false);
                logger.LogWarning("Database created via EnsureCreated — run 'dotnet ef migrations add InitialCreate' for proper migrations");
            }
        }
    }

    /// <summary>
    /// Seeds default roles (Admin, User, Operator) if they don't exist yet.
    /// </summary>
    private static async Task SeedRolesAsync(AuthDbContext context, ILogger logger)
    {
        var existingRoles = await context.Roles.Select(r => r.Name).ToListAsync().ConfigureAwait(false);

        var rolesToAdd = RoleType.All
            .Where(role => !existingRoles.Contains(role))
            .Select(role => new RoleEntity { Name = role })
            .ToList();

        if (rolesToAdd.Count > 0)
        {
            context.Roles.AddRange(rolesToAdd);
            await context.SaveChangesAsync().ConfigureAwait(false);
            logger.LogInformation("Seeded {Count} roles: {Roles}",
                rolesToAdd.Count, string.Join(", ", rolesToAdd.Select(r => r.Name)));
        }
        else
        {
            logger.LogDebug("All default roles already exist — skipping seed");
        }
    }
}


using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Infrastructure.Persistence;
using AuthService.Infrastructure.Adapters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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

            // Seed default users
            await SeedDefaultUsersAsync(context, scope.ServiceProvider, logger).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database initialization failed — falling back to EnsureCreated");

            // Fallback: create tables without migration history (dev only)
            if (app.Environment.IsDevelopment())
            {
                await context.Database.EnsureCreatedAsync().ConfigureAwait(false);
                await SeedRolesAsync(context, logger).ConfigureAwait(false);
                await SeedDefaultUsersAsync(context, scope.ServiceProvider, logger).ConfigureAwait(false);
                logger.LogWarning("Database created via EnsureCreated — run 'dotnet ef migrations add InitialCreate' for proper migrations");
            }
        }
    }

    /// <summary>
    /// Seeds default roles (Admin, User, Operator, Viewer) if they don't exist yet.
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

    /// <summary>
    /// Seeds default users (admin, operator, viewer) if the Users table is empty.
    /// </summary>
    private static async Task SeedDefaultUsersAsync(AuthDbContext context, IServiceProvider serviceProvider, ILogger logger)
    {
        if (await context.Users.AnyAsync().ConfigureAwait(false))
        {
            logger.LogDebug("Users database is not empty — skipping default users seed");
            return;
        }

        logger.LogInformation("Seeding default users (admin, operator, viewer)...");

        var cryptoManager = serviceProvider.GetRequiredService<ICryptoManager>();

        var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == RoleType.Admin).ConfigureAwait(false);
        var operatorRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == RoleType.Operator).ConfigureAwait(false);
        var viewerRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == RoleType.Viewer).ConfigureAwait(false);

        if (adminRole == null || operatorRole == null || viewerRole == null)
        {
            logger.LogError("Cannot seed default users: default roles are missing in database");
            return;
        }

        var usersToSeed = new List<(UserEntity User, string Password, int RoleId)>
        {
            (
                new UserEntity
                {
                    Id = Guid.NewGuid(),
                    Username = "admin",
                    Email = "admin@jessica.local",
                    FirstName = "System",
                    LastName = "Admin",
                    PasswordHash = "",
                    IsActive = true
                },
                "admin123",
                adminRole.Id
            ),
            (
                new UserEntity
                {
                    Id = Guid.NewGuid(),
                    Username = "operator",
                    Email = "operator@jessica.local",
                    FirstName = "System",
                    LastName = "Operator",
                    PasswordHash = "",
                    IsActive = true
                },
                "operator123",
                operatorRole.Id
            ),
            (
                new UserEntity
                {
                    Id = Guid.NewGuid(),
                    Username = "viewer",
                    Email = "viewer@jessica.local",
                    FirstName = "System",
                    LastName = "Viewer",
                    PasswordHash = "",
                    IsActive = true
                },
                "viewer123",
                viewerRole.Id
            )
        };

        foreach (var item in usersToSeed)
        {
            item.User.PasswordHash = cryptoManager.HashPassword(item.Password);
            item.User.UserRoles.Add(new UserRoleEntity
            {
                UserId = item.User.Id,
                RoleId = item.RoleId
            });
            context.Users.Add(item.User);
        }

        await context.SaveChangesAsync().ConfigureAwait(false);
        logger.LogInformation("Seeded default users successfully");
    }
}
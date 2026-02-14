using RecordingManager.Application.Adapters;
using RecordingManager.Application.Services;
using RecordingManager.Infrastructure.Adapters;
using RecordingManager.Infrastructure.Repositories;

namespace RecordingManager.Extensions;

/// <summary>
/// Extension methods for registering services with dependency injection.
/// Following Clean Architecture: Application → Infrastructure → Domain.
/// </summary>
public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        // ── Infrastructure: Repositories ──
        services.AddScoped<IRecordingRepository, RecordingRepository>();

        // ── Application: Services ──
        services.AddScoped<IRecordingService, RecordingService>();

        return services;
    }
}


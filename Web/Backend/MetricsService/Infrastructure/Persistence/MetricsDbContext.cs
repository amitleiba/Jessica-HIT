using Microsoft.EntityFrameworkCore;
using MetricsService.Domain.Entities;

namespace MetricsService.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the MetricsService database.
/// Stores historical sensor metrics from the Jessica robot.
/// </summary>
public class MetricsDbContext(DbContextOptions<MetricsDbContext> options)
    : DbContext(options)
{
    public DbSet<SensorMetric> SensorMetrics => Set<SensorMetric>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SensorMetric>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.Distance).IsRequired();
            entity.Property(e => e.Safety).IsRequired();
            entity.Property(e => e.Mode).IsRequired();
            entity.Property(e => e.SolarVoltage).IsRequired();

            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.SavedAt).HasDefaultValueSql("now() at time zone 'utc'");

            // Index for fast chronological queries (last N minutes / hours)
            entity.HasIndex(e => e.Timestamp);
        });
    }
}

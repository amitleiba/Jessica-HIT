using Microsoft.EntityFrameworkCore;
using RecordingManager.Domain.Entities;

namespace RecordingManager.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the RecordingManager database.
/// Stores recordings and their direction-change event timelines.
/// </summary>
public class RecordingManagerDbContext(DbContextOptions<RecordingManagerDbContext> options)
    : DbContext(options)
{
    public DbSet<RecordingEntity> Recordings => Set<RecordingEntity>();
    public DbSet<RecordingEventEntity> RecordingEvents => Set<RecordingEventEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── RecordingEntity ──
        modelBuilder.Entity<RecordingEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.UserId).HasMaxLength(256).IsRequired();
            entity.HasIndex(e => e.UserId); // Fast lookup by user

            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Speed).IsRequired();
            entity.Property(e => e.DurationMs).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now() at time zone 'utc'");
        });

        // ── RecordingEventEntity ──
        modelBuilder.Entity<RecordingEventEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

            entity.Property(e => e.OffsetMs).IsRequired();
            entity.Property(e => e.Direction).HasMaxLength(50).IsRequired();

            entity.HasOne(e => e.Recording)
                .WithMany(r => r.Events)
                .HasForeignKey(e => e.RecordingId)
                .OnDelete(DeleteBehavior.Cascade); // Delete events when recording is deleted

            // Index for fast ordered retrieval during replay
            entity.HasIndex(e => new { e.RecordingId, e.OffsetMs });
        });
    }
}


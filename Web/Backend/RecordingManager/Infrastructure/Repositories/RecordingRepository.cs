using Microsoft.EntityFrameworkCore;
using RecordingManager.Domain.Entities;
using RecordingManager.Infrastructure.Adapters;
using RecordingManager.Infrastructure.Persistence;

namespace RecordingManager.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of IRecordingRepository.
/// All queries are scoped to the requesting user's ID for security.
/// </summary>
public class RecordingRepository(
    RecordingManagerDbContext dbContext,
    ILogger<RecordingRepository> logger) : IRecordingRepository
{
    private readonly RecordingManagerDbContext _db = dbContext;
    private readonly ILogger<RecordingRepository> _logger = logger;

    public async Task<List<RecordingEntity>> GetAllByUserIdAsync(string userId)
    {
        _logger.LogDebug("Fetching all recordings for user: {UserId}", userId);

        return await _db.Recordings
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<RecordingEntity?> GetByIdWithEventsAsync(Guid id, string userId)
    {
        _logger.LogDebug("Fetching recording {RecordingId} with events for user: {UserId}", id, userId);

        return await _db.Recordings
            .Include(r => r.Events.OrderBy(e => e.OffsetMs))
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId)
            .ConfigureAwait(false);
    }

    public async Task<RecordingEntity> CreateAsync(RecordingEntity recording)
    {
        _logger.LogInformation(
            "Creating recording '{Name}' for user {UserId} ({EventCount} events, {DurationMs}ms)",
            recording.Name, recording.UserId, recording.Events.Count, recording.DurationMs);

        _db.Recordings.Add(recording);
        await _db.SaveChangesAsync().ConfigureAwait(false);

        _logger.LogInformation(
            "Recording created: {RecordingId} — '{Name}'",
            recording.Id, recording.Name);

        return recording;
    }

    public async Task<bool> DeleteAsync(Guid id, string userId)
    {
        _logger.LogInformation("Deleting recording {RecordingId} for user {UserId}", id, userId);

        var recording = await _db.Recordings
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId)
            .ConfigureAwait(false);

        if (recording is null)
        {
            _logger.LogWarning("Recording {RecordingId} not found for user {UserId} — cannot delete", id, userId);
            return false;
        }

        _db.Recordings.Remove(recording); // Cascade deletes events
        await _db.SaveChangesAsync().ConfigureAwait(false);

        _logger.LogInformation("Recording {RecordingId} deleted successfully", id);
        return true;
    }
}


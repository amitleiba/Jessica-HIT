using RecordingManager.Domain.Entities;

namespace RecordingManager.Infrastructure.Adapters;

/// <summary>
/// Repository interface for Recording database operations.
/// Implemented by Infrastructure.Repositories.RecordingRepository.
/// </summary>
public interface IRecordingRepository
{
    /// <summary>
    /// Gets all recording summaries for a user (without events — lightweight for list views).
    /// Ordered by newest first.
    /// </summary>
    Task<List<RecordingEntity>> GetAllByUserIdAsync(string userId);

    /// <summary>
    /// Gets a single recording with all its events (for replay).
    /// Returns null if not found or doesn't belong to the user.
    /// </summary>
    Task<RecordingEntity?> GetByIdWithEventsAsync(Guid id, string userId);

    /// <summary>
    /// Creates a new recording with its events in a single transaction.
    /// Returns the created entity with populated ID.
    /// </summary>
    Task<RecordingEntity> CreateAsync(RecordingEntity recording);

    /// <summary>
    /// Deletes a recording and all its events (cascade).
    /// Returns true if deleted, false if not found.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, string userId);
}


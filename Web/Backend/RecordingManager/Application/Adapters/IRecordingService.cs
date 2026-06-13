using RecordingManager.API.DTOs.Requests;
using RecordingManager.API.DTOs.Responses;

namespace RecordingManager.Application.Adapters;

/// <summary>
/// Application-level recording service interface.
/// Orchestrates between the API layer and the repository.
/// All methods are user-scoped (userId parameter).
/// </summary>
public interface IRecordingService
{
    /// <summary>
    /// Gets all recording summaries for a user (no events).
    /// </summary>
    Task<List<RecordingSummaryResponse>> GetAllAsync(string userId);

    /// <summary>
    /// Gets a full recording with events for replay.
    /// Returns null if not found or unauthorized.
    /// </summary>
    Task<RecordingDetailResponse?> GetByIdAsync(Guid id, string userId);

    /// <summary>
    /// Creates a new recording from the frontend capture data.
    /// Returns the created recording summary.
    /// </summary>
    Task<RecordingSummaryResponse> CreateAsync(CreateRecordingRequest request, string userId);

    /// <summary>
    /// Deletes a recording. Returns true if deleted, false if not found.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, string userId);
}


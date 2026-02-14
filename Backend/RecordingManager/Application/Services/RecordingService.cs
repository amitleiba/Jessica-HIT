using RecordingManager.API.DTOs.Requests;
using RecordingManager.API.DTOs.Responses;
using RecordingManager.Application.Adapters;
using RecordingManager.Domain.Entities;
using RecordingManager.Infrastructure.Adapters;

namespace RecordingManager.Application.Services;

/// <summary>
/// Recording service — maps between DTOs and entities, delegates to repository.
/// </summary>
public class RecordingService(
    IRecordingRepository repository,
    ILogger<RecordingService> logger) : IRecordingService
{
    private readonly IRecordingRepository _repository = repository;
    private readonly ILogger<RecordingService> _logger = logger;

    public async Task<List<RecordingSummaryResponse>> GetAllAsync(string userId)
    {
        _logger.LogInformation("Fetching all recordings for user {UserId}", userId);

        var entities = await _repository.GetAllByUserIdAsync(userId).ConfigureAwait(false);

        return entities.Select(MapToSummary).ToList();
    }

    public async Task<RecordingDetailResponse?> GetByIdAsync(Guid id, string userId)
    {
        _logger.LogInformation("Fetching recording {RecordingId} for user {UserId}", id, userId);

        var entity = await _repository.GetByIdWithEventsAsync(id, userId).ConfigureAwait(false);

        if (entity is null)
        {
            _logger.LogWarning("Recording {RecordingId} not found for user {UserId}", id, userId);
            return null;
        }

        return MapToDetail(entity);
    }

    public async Task<RecordingSummaryResponse> CreateAsync(CreateRecordingRequest request, string userId)
    {
        _logger.LogInformation(
            "Creating recording '{Name}' for user {UserId} — speed={Speed}, duration={DurationMs}ms, events={EventCount}",
            request.Name, userId, request.Speed, request.DurationMs, request.Events.Count);

        var entity = new RecordingEntity
        {
            UserId = userId,
            Name = request.Name,
            Speed = request.Speed,
            DurationMs = request.DurationMs,
            Events = request.Events.Select(e => new RecordingEventEntity
            {
                OffsetMs = e.OffsetMs,
                Direction = e.Direction
            }).ToList()
        };

        var created = await _repository.CreateAsync(entity).ConfigureAwait(false);

        _logger.LogInformation("Recording '{Name}' created with ID {RecordingId}", created.Name, created.Id);

        return MapToSummary(created);
    }

    public async Task<bool> DeleteAsync(Guid id, string userId)
    {
        _logger.LogInformation("Deleting recording {RecordingId} for user {UserId}", id, userId);

        return await _repository.DeleteAsync(id, userId).ConfigureAwait(false);
    }

    // ── Mapping helpers ──

    private static RecordingSummaryResponse MapToSummary(RecordingEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Speed = entity.Speed,
        DurationMs = entity.DurationMs,
        CreatedAt = entity.CreatedAt
    };

    private static RecordingDetailResponse MapToDetail(RecordingEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Speed = entity.Speed,
        DurationMs = entity.DurationMs,
        CreatedAt = entity.CreatedAt,
        Events = entity.Events.Select(e => new RecordingEventResponse
        {
            OffsetMs = e.OffsetMs,
            Direction = e.Direction
        }).ToList()
    };
}


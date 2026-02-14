using Microsoft.AspNetCore.Mvc;
using RecordingManager.API.DTOs.Requests;
using RecordingManager.Application.Adapters;

namespace RecordingManager.API.Controllers;

/// <summary>
/// REST API for managing user recordings.
///
/// All endpoints require the X-User-Id header (forwarded by the Gateway from the JWT "sub" claim).
/// The Gateway's "authenticated" YARP policy ensures only valid JWT holders reach this service.
///
/// Routes (as seen by the frontend, through Gateway YARP proxy):
///   GET    /api/recordings          → list all recordings for user
///   GET    /api/recordings/{id}     → get recording with events (for replay)
///   POST   /api/recordings          → create a new recording
///   DELETE /api/recordings/{id}     → delete a recording
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RecordingsController(
    IRecordingService recordingService,
    ILogger<RecordingsController> logger) : ControllerBase
{
    private readonly IRecordingService _recordingService = recordingService;
    private readonly ILogger<RecordingsController> _logger = logger;

    /// <summary>
    /// Extracts the user ID from the X-User-Id header (set by Gateway YARP transform).
    /// </summary>
    private string? GetUserId() => Request.Headers["X-User-Id"].FirstOrDefault();

    // ── GET /api/recordings ──

    /// <summary>
    /// Lists all recordings for the authenticated user (summaries only — no events).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("GET /api/recordings — missing X-User-Id header");
            return Unauthorized(new { message = "User identity not provided" });
        }

        _logger.LogInformation("GET /api/recordings — user {UserId}", userId);

        var recordings = await _recordingService.GetAllAsync(userId).ConfigureAwait(false);
        return Ok(recordings);
    }

    // ── GET /api/recordings/{id} ──

    /// <summary>
    /// Gets a single recording with all its direction events (for replay).
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("GET /api/recordings/{RecordingId} — missing X-User-Id header", id);
            return Unauthorized(new { message = "User identity not provided" });
        }

        _logger.LogInformation("GET /api/recordings/{RecordingId} — user {UserId}", id, userId);

        var recording = await _recordingService.GetByIdAsync(id, userId).ConfigureAwait(false);

        if (recording is null)
        {
            return NotFound(new { message = "Recording not found" });
        }

        return Ok(recording);
    }

    // ── POST /api/recordings ──

    /// <summary>
    /// Creates a new recording from the frontend capture data.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRecordingRequest request)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("POST /api/recordings — missing X-User-Id header");
            return Unauthorized(new { message = "User identity not provided" });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation(
            "POST /api/recordings — user {UserId}, name='{Name}', events={EventCount}",
            userId, request.Name, request.Events.Count);

        var created = await _recordingService.CreateAsync(request, userId).ConfigureAwait(false);

        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            created);
    }

    // ── DELETE /api/recordings/{id} ──

    /// <summary>
    /// Deletes a recording and all its events.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("DELETE /api/recordings/{RecordingId} — missing X-User-Id header", id);
            return Unauthorized(new { message = "User identity not provided" });
        }

        _logger.LogInformation("DELETE /api/recordings/{RecordingId} — user {UserId}", id, userId);

        var deleted = await _recordingService.DeleteAsync(id, userId).ConfigureAwait(false);

        if (!deleted)
        {
            return NotFound(new { message = "Recording not found" });
        }

        return NoContent();
    }
}


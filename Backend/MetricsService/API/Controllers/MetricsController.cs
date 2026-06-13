using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MetricsService.Infrastructure.Persistence;
using MetricsService.Domain.Entities;

namespace MetricsService.API.Controllers;

[ApiController]
[Authorize]
[Route("api/metrics")]
public sealed class MetricsController(
    ILogger<MetricsController> logger,
    MetricsDbContext context) : ControllerBase
{
    private readonly ILogger<MetricsController> _logger = logger;
    private readonly MetricsDbContext _context = context;

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int durationMinutes = 60,
        CancellationToken cancellationToken = default)
    {
        if (durationMinutes <= 0)
        {
            return BadRequest(new { message = "Duration must be greater than 0 minutes." });
        }

        try
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-durationMinutes);

            var queryResult = await _context.SensorMetrics
                .Where(m => m.Timestamp >= cutoff)
                .OrderBy(m => m.Timestamp)
                .Select(m => new MetricDto
                {
                    Distance = m.Distance,
                    Safety = m.Safety,
                    Mode = m.Mode,
                    SolarVoltage = m.SolarVoltage,
                    Timestamp = m.Timestamp
                })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            // Downsample if there are too many data points (e.g. limit to ~500 points for chart rendering performance)
            const int maxPoints = 500;
            var result = queryResult;
            if (queryResult.Count > maxPoints)
            {
                var step = (double)queryResult.Count / maxPoints;
                var downsampled = new List<MetricDto>();
                for (int i = 0; i < maxPoints; i++)
                {
                    var index = (int)Math.Min(queryResult.Count - 1, Math.Round(i * step));
                    downsampled.Add(queryResult[index]);
                }
                result = downsampled;
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching historical sensor metrics");
            return StatusCode(500, new { message = "Internal server error while retrieving metrics history." });
        }
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(
        [FromQuery] int durationMinutes = 60,
        CancellationToken cancellationToken = default)
    {
        if (durationMinutes <= 0)
        {
            return BadRequest(new { message = "Duration must be greater than 0 minutes." });
        }

        try
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-durationMinutes);

            var metrics = await _context.SensorMetrics
                .Where(m => m.Timestamp >= cutoff)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (metrics.Count == 0)
            {
                return Ok(new MetricsStatsDto
                {
                    TotalCount = 0,
                    AverageSolarVoltage = 0,
                    MaxDistance = 0,
                    MinDistance = 0,
                    AverageDistance = 0,
                    SafetyIncidentCount = 0,
                    ModeDistribution = []
                });
            }

            var avgSolar = metrics.Average(m => m.SolarVoltage);
            var maxDist = metrics.Max(m => m.Distance);
            var minDist = metrics.Min(m => m.Distance);
            var avgDist = metrics.Average(m => m.Distance);
            
            // Safety == 2 represents a critical hazard event; count only those as incidents
            const int SafetyCriticalValue = 2;
            var safetyIncidents = metrics.Count(m => m.Safety == SafetyCriticalValue);

            // Calculate mode distribution counts
            var modeDist = metrics
                .GroupBy(m => m.Mode)
                .ToDictionary(g => g.Key.ToString(System.Globalization.CultureInfo.InvariantCulture), g => g.Count());

            var stats = new MetricsStatsDto
            {
                TotalCount = metrics.Count,
                AverageSolarVoltage = Math.Round(avgSolar, 2),
                MaxDistance = maxDist,
                MinDistance = minDist,
                AverageDistance = Math.Round(avgDist, 1),
                SafetyIncidentCount = safetyIncidents,
                ModeDistribution = modeDist
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating metrics statistics");
            return StatusCode(500, new { message = "Internal server error while calculating metrics stats." });
        }
    }

    private sealed class MetricDto
    {
        public int Distance { get; init; }
        public int Safety { get; init; }
        public int Mode { get; init; }
        public double SolarVoltage { get; init; }
        public DateTime Timestamp { get; init; }
    }

    private sealed class MetricsStatsDto
    {
        public int TotalCount { get; init; }
        public double AverageSolarVoltage { get; init; }
        public int MaxDistance { get; init; }
        public int MinDistance { get; init; }
        public double AverageDistance { get; init; }
        public int SafetyIncidentCount { get; init; }
        public Dictionary<string, int> ModeDistribution { get; init; } = [];
    }
}

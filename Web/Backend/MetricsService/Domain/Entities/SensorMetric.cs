namespace MetricsService.Domain.Entities;

/// <summary>
/// A recorded sensor telemetry snapshot from Jessica.
/// Contains distance, safety state, current mode, and solar panel voltage.
/// </summary>
public class SensorMetric
{
    public Guid Id { get; set; }

    /// <summary>
    /// Distance measurement from the robot sensors (e.g. to obstacles).
    /// </summary>
    public int Distance { get; set; }

    /// <summary>
    /// Safety state indicator (e.g. 0=Safe, 1=Warning, 2=Danger/Hazard).
    /// </summary>
    public int Safety { get; set; }

    /// <summary>
    /// Operating mode (e.g. Manual, Autonomous, Charging, Idle).
    /// </summary>
    public int Mode { get; set; }

    /// <summary>
    /// Solar panel voltage reading in Volts.
    /// </summary>
    public double SolarVoltage { get; set; }

    /// <summary>
    /// Timestamp when this metric was recorded by the robot (UTC).
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Timestamp when this metric was saved to our database (UTC).
    /// </summary>
    public DateTime SavedAt { get; set; } = DateTime.UtcNow;
}

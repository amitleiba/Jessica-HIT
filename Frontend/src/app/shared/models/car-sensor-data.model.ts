/**
 * CarSensorData — raw telemetry received from the car over SignalR.
 *
 * Start minimal — add more sensor fields as they become available.
 */
export interface CarSensorData {
    /** Current speed in km/h */
    speed?: number;
}

/** Empty default for initial state */
export const EMPTY_SENSOR_DATA: CarSensorData = {};

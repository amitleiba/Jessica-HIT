/**
 * CarSensorData — raw telemetry received from the car over SignalR.
 *
 * Start minimal — add more sensor fields as they become available.
 */
export interface CarSensorData {
    /** Current speed in km/h */
    speed?: number;
    /** Base64 data URL or HTTP URL for a live camera frame when the hub pushes it */
    cameraFrame?: string;
}

/** Empty default for initial state */
export const EMPTY_SENSOR_DATA: CarSensorData = {};

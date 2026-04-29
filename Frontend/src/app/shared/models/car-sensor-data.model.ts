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
    /** Distance from nearest object */
    distance?: number;
    /** 1 when robot is too close to an object, otherwise 0 */
    safety?: number;
    /** Robot mode (manual/automatic as reported by firmware) */
    mode?: number;
    /** Battery voltage */
    battery?: number;
    /** Status event timestamp (UTC) */
    receivedAtUtc?: string;
}

/** Empty default for initial state */
export const EMPTY_SENSOR_DATA: CarSensorData = {};

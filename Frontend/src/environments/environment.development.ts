export const environment = {
  production: false,
  apiUrl: 'http://localhost:5207',  // Gateway URL
  signalRUrl: 'http://localhost:5207/hubs/jessica',  // SignalR Hub URL
  keycloakTokenUrl: 'http://localhost:8082/realms/jessica-realm/protocol/openid-connect/token',
  keycloakClientId: 'jessica-gateway',
  cameraUrl: 'http://192.168.1.160/'  // ESP32-CAM video feed URL
};





export const environment = {
  production: true,
  apiUrl: 'https://api.jessica.com',  // Production Gateway URL
  signalRUrl: 'https://api.jessica.com/hubs/jessica',  // Production SignalR Hub URL
  keycloakTokenUrl: 'https://auth.jessica.com/realms/jessica-realm/protocol/openid-connect/token',
  keycloakClientId: 'jessica-gateway',
  keycloakClientSecret: '',  // Set via environment variable in production
  cameraUrl: 'http://192.168.1.161/'  // ESP32-CAM video feed URL
};




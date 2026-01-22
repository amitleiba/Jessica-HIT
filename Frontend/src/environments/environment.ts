export const environment = {
  production: true,
  apiUrl: 'https://api.jessica.com',  // Production Gateway URL
  keycloakTokenUrl: 'https://auth.jessica.com/realms/jessica-realm/protocol/openid-connect/token',
  keycloakClientId: 'jessica-gateway',
  keycloakClientSecret: ''  // Set via environment variable in production
};




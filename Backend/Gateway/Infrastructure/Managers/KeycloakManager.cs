using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Gateway.Domain.Configuration;
using Gateway.Domain.Models;
using Gateway.Infrastructure.Adapters;

namespace Gateway.Infrastructure.Managers;

/// <summary>
/// Implementation of IKeycloakManager
/// Handles all Keycloak-specific operations via Keycloak Admin REST API
/// </summary>
public class KeycloakManager(
    IHttpClientFactory httpClientFactory,
    KeycloakConfig keycloakConfig,
    ILogger<KeycloakManager> logger) : IKeycloakManager
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly KeycloakConfig _keycloakConfig = keycloakConfig;
    private readonly ILogger<KeycloakManager> _logger = logger;

    private string BaseUrl => _keycloakConfig.Url.TrimEnd('/');
    private string TokenEndpoint => $"{BaseUrl}/realms/{_keycloakConfig.Realm}/protocol/openid-connect/token";
    private string UserInfoEndpoint => $"{BaseUrl}/realms/{_keycloakConfig.Realm}/protocol/openid-connect/userinfo";
    private string AdminUsersEndpoint => $"{BaseUrl}/admin/realms/{_keycloakConfig.Realm}/users";


    public async Task<string?> CreateUserAsync(User user)
    {
        _logger.LogInformation("Creating user in Keycloak: {Username}", user.Username);

        try
        {
            var httpClient = _httpClientFactory.CreateClient();

            // Get admin token using client credentials
            var adminToken = await GetAdminTokenAsync(httpClient).ConfigureAwait(false);
            if (string.IsNullOrEmpty(adminToken))
            {
                _logger.LogError("Failed to obtain admin token. Ensure Service Accounts is enabled and admin roles are assigned.");
                return null;
            }

            // Create user payload matching Keycloak's UserRepresentation structure
            var userPayload = new
            {
                username = user.Username,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                enabled = true,
                emailVerified = true,
                credentials = new[]
                {
                    new
                    {
                        type = "password",
                        value = user.Password,
                        temporary = false
                    }
                }
            };

            // Create user via Keycloak Admin API
            var request = new HttpRequestMessage(HttpMethod.Post, AdminUsersEndpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(userPayload), Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var locationHeader = response.Headers.Location?.ToString();
                var userId = locationHeader?.Split('/').Last();
                _logger.LogInformation("User created successfully: {Username} (ID: {UserId})", user.Username, userId);
                return userId ?? "created";
            }

            var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                _logger.LogError(
                    "Permission Denied (403). Service account needs 'realm-admin' or 'manage-users' role. " +
                    "Client: {ClientId}, Status: {Status}, Error: {Error}",
                    _keycloakConfig.ClientId, response.StatusCode, errorContent);
                throw new UnauthorizedAccessException(
                    "Service account does not have permission to create users. " +
                    "Please configure the service account with 'realm-admin' or 'manage-users' role in Keycloak.");
            }

            if (errorContent.Contains("User exists", StringComparison.OrdinalIgnoreCase) ||
                errorContent.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("User with this username or email already exists");
            }

            _logger.LogError("Failed to create user. Status: {Status}, Error: {Error}", response.StatusCode, errorContent);
            return null;
        }
        catch (Exception ex) when (!(ex is UnauthorizedAccessException || ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogError(ex, "Exception while creating user in Keycloak: {Username}", user.Username);
            throw;
        }
    }

    /// <summary>
    /// Gets an admin token using client credentials grant (client_id + client_secret)
    /// </summary>
    private async Task<string?> GetAdminTokenAsync(HttpClient httpClient)
    {
        var formData = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "client_id", _keycloakConfig.ClientId },
            { "client_secret", _keycloakConfig.ClientSecret }
        };

        return await GetTokenAsync(httpClient, formData, "admin token").ConfigureAwait(false);
    }

    /// <summary>
    /// Common method to get tokens from Keycloak token endpoint
    /// </summary>
    private async Task<string?> GetTokenAsync(HttpClient httpClient, Dictionary<string, string> formData, string tokenType)
    {
        try
        {
            var response = await httpClient.PostAsync(new Uri(TokenEndpoint), new FormUrlEncodedContent(formData)).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                _logger.LogError("Failed to get {TokenType}. Status: {Status}, Error: {Error}",
                    tokenType, response.StatusCode, errorContent);
                return null;
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>().ConfigureAwait(false);
            return tokenResponse?.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while obtaining {TokenType}", tokenType);
            return null;
        }
    }


    public async Task<(string AccessToken, string TokenType, int ExpiresIn, string? RefreshToken)?> AuthenticateUserAsync(string username, string password)
    {
        _logger.LogInformation("Authenticating user via password grant: {Username}", username);

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var formData = new Dictionary<string, string>
            {
                { "grant_type", "password" },
                { "client_id", _keycloakConfig.ClientId },
                { "client_secret", _keycloakConfig.ClientSecret },
                { "username", username },
                { "password", password },
                { "scope", string.Join(" ", _keycloakConfig.Scope) }
            };

            var response = await httpClient.PostAsync(new Uri(TokenEndpoint), new FormUrlEncodedContent(formData)).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                _logger.LogWarning("Authentication failed for user: {Username}. Status: {Status}, Error: {Error}",
                    username, response.StatusCode, errorContent);
                return null;
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>().ConfigureAwait(false);

            if (tokenResponse?.AccessToken == null)
            {
                _logger.LogError("Token response missing access_token for user: {Username}", username);
                return null;
            }

            _logger.LogInformation("User authenticated successfully: {Username}", username);

            return (
                AccessToken: tokenResponse.AccessToken,
                TokenType: tokenResponse.TokenType ?? "Bearer",
                ExpiresIn: tokenResponse.ExpiresIn,
                RefreshToken: tokenResponse.RefreshToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while authenticating user: {Username}", username);
            return null;
        }
    }

    /// <summary>
    /// Fetches user information from Keycloak's userinfo endpoint using the provided access token
    /// This returns fresh user data from Keycloak, not just what's in the token
    /// </summary>
    public async Task<KeycloakTokenClaims?> GetUserInfoFromKeycloakAsync(string accessToken)
    {
        _logger.LogInformation("Fetching user info from Keycloak userinfo endpoint");

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var cleanToken = CleanBearerToken(accessToken);

            var request = new HttpRequestMessage(HttpMethod.Get, UserInfoEndpoint);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", cleanToken);

            var response = await httpClient.SendAsync(request).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                _logger.LogWarning("Failed to fetch user info from Keycloak. Status: {Status}, Error: {Error}",
                    response.StatusCode, errorContent);
                return null;
            }

            // Deserialize Keycloak userinfo response into strongly-typed model
            var userInfoJson = await response.Content.ReadFromJsonAsync<KeycloakTokenClaims>().ConfigureAwait(false);

            if (userInfoJson == null)
            {
                _logger.LogWarning("Deserialized user info is null");
                return null;
            }

            // Validate required claim
            if (string.IsNullOrEmpty(userInfoJson.Sub))
            {
                _logger.LogError("Required claim 'sub' is missing from Keycloak userinfo response");
                return null;
            }

            _logger.LogInformation("Successfully fetched and deserialized user info from Keycloak for user: {Sub}", userInfoJson.Sub);
            return userInfoJson;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization failed while fetching user info from Keycloak");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while fetching user info from Keycloak");
            return null;
        }
    }

    /// <summary>
    /// Removes "Bearer " prefix from token if present
    /// </summary>
    private static string? CleanBearerToken(string? token)
    {
        return token?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true
            ? token["Bearer ".Length..].Trim()
            : token;
    }

}

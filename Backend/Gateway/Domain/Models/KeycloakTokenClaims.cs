using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gateway.Domain.Models;

/// <summary>
/// Comprehensive model representing all possible claims from a Keycloak JWT token.
/// Based on OpenID Connect Core 1.0 specification and Keycloak-specific extensions.
/// Reference: https://openid.net/specs/openid-connect-core-1_0.html#StandardClaims
/// </summary>
public class KeycloakTokenClaims
{
    // ============================================
    // Standard OIDC User Claims (OpenID Connect Core 1.0)
    // ============================================

    /// <summary>
    /// Subject - Unique identifier for the user (required claim)
    /// </summary>
    [JsonPropertyName("sub")]
    public required string Sub { get; init; }

    /// <summary>
    /// Full name of the user
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>
    /// Given name(s) or first name(s) of the user
    /// </summary>
    [JsonPropertyName("given_name")]
    public string? GivenName { get; init; }

    /// <summary>
    /// Surname(s) or last name(s) of the user
    /// </summary>
    [JsonPropertyName("family_name")]
    public string? FamilyName { get; init; }

    /// <summary>
    /// Middle name(s) of the user
    /// </summary>
    [JsonPropertyName("middle_name")]
    public string? MiddleName { get; init; }

    /// <summary>
    /// Casual name of the user
    /// </summary>
    [JsonPropertyName("nickname")]
    public string? Nickname { get; init; }

    /// <summary>
    /// Shorthand name by which the user wishes to be referred to
    /// </summary>
    [JsonPropertyName("preferred_username")]
    public string? PreferredUsername { get; init; }

    /// <summary>
    /// URL of the user's profile page
    /// </summary>
    [JsonPropertyName("profile")]
    public string? Profile { get; init; }

    /// <summary>
    /// URL of the user's profile picture
    /// </summary>
    [JsonPropertyName("picture")]
    public string? Picture { get; init; }

    /// <summary>
    /// URL of the user's web page or blog
    /// </summary>
    [JsonPropertyName("website")]
    public string? Website { get; init; }

    /// <summary>
    /// User's email address
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; init; }

    /// <summary>
    /// True if the user's email address has been verified; otherwise false
    /// </summary>
    [JsonPropertyName("email_verified")]
    public bool? EmailVerified { get; init; }

    /// <summary>
    /// User's gender (values defined by the user)
    /// </summary>
    [JsonPropertyName("gender")]
    public string? Gender { get; init; }

    /// <summary>
    /// User's birthday, represented as an ISO 8601:2004 [ISO8601‑2004] YYYY-MM-DD format
    /// </summary>
    [JsonPropertyName("birthdate")]
    public string? Birthdate { get; init; }

    /// <summary>
    /// String from the time zone database representing the user's time zone
    /// </summary>
    [JsonPropertyName("zoneinfo")]
    public string? ZoneInfo { get; init; }

    /// <summary>
    /// User's locale, represented as a BCP47 [RFC5646] language tag
    /// </summary>
    [JsonPropertyName("locale")]
    public string? Locale { get; init; }

    /// <summary>
    /// User's phone number (E.164 format recommended)
    /// </summary>
    [JsonPropertyName("phone_number")]
    public string? PhoneNumber { get; init; }

    /// <summary>
    /// True if the user's phone number has been verified; otherwise false
    /// </summary>
    [JsonPropertyName("phone_number_verified")]
    public bool? PhoneNumberVerified { get; init; }

    /// <summary>
    /// User's postal address
    /// </summary>
    [JsonPropertyName("address")]
    public AddressClaim? Address { get; init; }

    /// <summary>
    /// Time the user's information was last updated, represented as Unix timestamp
    /// </summary>
    [JsonPropertyName("updated_at")]
    public long? UpdatedAt { get; init; }

    // ============================================
    // JWT Standard Claims (RFC 7519)
    // ============================================

    /// <summary>
    /// Issuer - Identifies the principal that issued the JWT (Keycloak server URL)
    /// </summary>
    [JsonPropertyName("iss")]
    public string? Iss { get; init; }

    /// <summary>
    /// Audience - Identifies the recipients that the JWT is intended for (client ID(s))
    /// Can be a string or array of strings
    /// </summary>
    [JsonPropertyName("aud")]
    [JsonConverter(typeof(AudienceConverter))]
    public AudienceValue? Aud { get; init; }

    /// <summary>
    /// Expiration Time - Identifies the expiration time on or after which the JWT must not be accepted (Unix timestamp)
    /// </summary>
    [JsonPropertyName("exp")]
    public long? Exp { get; init; }

    /// <summary>
    /// Issued At - Identifies the time at which the JWT was issued (Unix timestamp)
    /// </summary>
    [JsonPropertyName("iat")]
    public long? Iat { get; init; }

    /// <summary>
    /// Not Before - Identifies the time before which the JWT must not be accepted (Unix timestamp)
    /// </summary>
    [JsonPropertyName("nbf")]
    public long? Nbf { get; init; }

    /// <summary>
    /// JWT ID - Unique identifier for the JWT
    /// </summary>
    [JsonPropertyName("jti")]
    public string? Jti { get; init; }

    // ============================================
    // OIDC Authentication Claims
    // ============================================

    /// <summary>
    /// Authentication Time - Time when the authentication occurred (Unix timestamp)
    /// </summary>
    [JsonPropertyName("auth_time")]
    public long? AuthTime { get; init; }

    /// <summary>
    /// Nonce - String value used to associate a Client session with an ID Token
    /// </summary>
    [JsonPropertyName("nonce")]
    public string? Nonce { get; init; }

    /// <summary>
    /// Authentication Context Class Reference - String specifying an Authentication Context Class Reference value
    /// </summary>
    [JsonPropertyName("acr")]
    public string? Acr { get; init; }

    /// <summary>
    /// Authorized Party - String specifying the party to which the ID Token was issued
    /// </summary>
    [JsonPropertyName("azp")]
    public string? Azp { get; init; }

    /// <summary>
    /// Session State - Opaque session state identifier
    /// </summary>
    [JsonPropertyName("session_state")]
    public string? SessionState { get; init; }

    /// <summary>
    /// Token Type - Type of token (typically "Bearer")
    /// </summary>
    [JsonPropertyName("typ")]
    public string? Typ { get; init; }

    /// <summary>
    /// Session ID - Unique session identifier
    /// </summary>
    [JsonPropertyName("sid")]
    public string? Sid { get; init; }

    // ============================================
    // Keycloak-Specific Claims
    // ============================================

    /// <summary>
    /// Realm-level roles assigned to the user
    /// </summary>
    [JsonPropertyName("realm_access")]
    public RealmAccess? RealmAccess { get; init; }

    /// <summary>
    /// Resource/client-level roles assigned to the user
    /// Key is the resource/client name, value contains the roles
    /// </summary>
    [JsonPropertyName("resource_access")]
    public Dictionary<string, ResourceAccess>? ResourceAccess { get; init; }

    /// <summary>
    /// Scopes granted to the token (space-separated string)
    /// </summary>
    [JsonPropertyName("scope")]
    public string? Scope { get; init; }

    /// <summary>
    /// Allowed CORS origins for this token
    /// </summary>
    [JsonPropertyName("allowed_origins")]
    public string[]? AllowedOrigins { get; init; }

    // ============================================
    // Custom Claims
    // ============================================

    /// <summary>
    /// Dictionary to hold any custom claims that don't fit the standard model
    /// This allows for extensibility without breaking deserialization
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object?>? CustomClaims { get; init; }

    // ============================================
    // Helper Methods
    // ============================================

    /// <summary>
    /// Converts the claims model to a dictionary for logging/debugging purposes
    /// </summary>
    public Dictionary<string, object?> ToDictionary()
    {
        var dict = new Dictionary<string, object?>();

        // Standard OIDC Claims
        dict["sub"] = Sub;
        if (Name != null) dict["name"] = Name;
        if (GivenName != null) dict["given_name"] = GivenName;
        if (FamilyName != null) dict["family_name"] = FamilyName;
        if (MiddleName != null) dict["middle_name"] = MiddleName;
        if (Nickname != null) dict["nickname"] = Nickname;
        if (PreferredUsername != null) dict["preferred_username"] = PreferredUsername;
        if (Profile != null) dict["profile"] = Profile;
        if (Picture != null) dict["picture"] = Picture;
        if (Website != null) dict["website"] = Website;
        if (Email != null) dict["email"] = Email;
        if (EmailVerified.HasValue) dict["email_verified"] = EmailVerified;
        if (Gender != null) dict["gender"] = Gender;
        if (Birthdate != null) dict["birthdate"] = Birthdate;
        if (ZoneInfo != null) dict["zoneinfo"] = ZoneInfo;
        if (Locale != null) dict["locale"] = Locale;
        if (PhoneNumber != null) dict["phone_number"] = PhoneNumber;
        if (PhoneNumberVerified.HasValue) dict["phone_number_verified"] = PhoneNumberVerified;
        if (Address != null) dict["address"] = Address;
        if (UpdatedAt.HasValue) dict["updated_at"] = UpdatedAt;

        // JWT Claims
        if (Iss != null) dict["iss"] = Iss;
        if (Aud != null) dict["aud"] = Aud;
        if (Exp.HasValue) dict["exp"] = Exp;
        if (Iat.HasValue) dict["iat"] = Iat;
        if (Nbf.HasValue) dict["nbf"] = Nbf;
        if (Jti != null) dict["jti"] = Jti;

        // OIDC Auth Claims
        if (AuthTime.HasValue) dict["auth_time"] = AuthTime;
        if (Nonce != null) dict["nonce"] = Nonce;
        if (Acr != null) dict["acr"] = Acr;
        if (Azp != null) dict["azp"] = Azp;
        if (SessionState != null) dict["session_state"] = SessionState;
        if (Typ != null) dict["typ"] = Typ;
        if (Sid != null) dict["sid"] = Sid;

        // Keycloak Claims
        if (RealmAccess != null) dict["realm_access"] = RealmAccess;
        if (ResourceAccess != null) dict["resource_access"] = ResourceAccess;
        if (Scope != null) dict["scope"] = Scope;
        if (AllowedOrigins != null) dict["allowed_origins"] = AllowedOrigins;

        // Custom Claims
        if (CustomClaims != null)
        {
            foreach (var claim in CustomClaims)
            {
                dict[claim.Key] = claim.Value;
            }
        }

        return dict;
    }
}

/// <summary>
/// Represents a postal address claim
/// </summary>
public class AddressClaim
{
    /// <summary>
    /// Full mailing address, formatted for display
    /// </summary>
    [JsonPropertyName("formatted")]
    public string? Formatted { get; init; }

    /// <summary>
    /// Full street address component
    /// </summary>
    [JsonPropertyName("street_address")]
    public string? StreetAddress { get; init; }

    /// <summary>
    /// City or locality component
    /// </summary>
    [JsonPropertyName("locality")]
    public string? Locality { get; init; }

    /// <summary>
    /// State, province, prefecture, or region component
    /// </summary>
    [JsonPropertyName("region")]
    public string? Region { get; init; }

    /// <summary>
    /// Zip code or postal code component
    /// </summary>
    [JsonPropertyName("postal_code")]
    public string? PostalCode { get; init; }

    /// <summary>
    /// Country name component
    /// </summary>
    [JsonPropertyName("country")]
    public string? Country { get; init; }
}

/// <summary>
/// Represents realm-level access (roles)
/// </summary>
public class RealmAccess
{
    /// <summary>
    /// List of realm-level roles assigned to the user
    /// </summary>
    [JsonPropertyName("roles")]
    public string[]? Roles { get; init; }
}

/// <summary>
/// Represents resource/client-level access (roles)
/// </summary>
public class ResourceAccess
{
    /// <summary>
    /// List of roles assigned to the user for this resource/client
    /// </summary>
    [JsonPropertyName("roles")]
    public string[]? Roles { get; init; }
}

/// <summary>
/// Represents the audience claim which can be either a string or array of strings
/// </summary>
public class AudienceValue
{
    public string? Single { get; init; }
    public string[]? Multiple { get; init; }

    public object? Value => Multiple ?? (object?)Single;
}

/// <summary>
/// Custom JSON converter for audience claim (can be string or string[])
/// </summary>
public class AudienceConverter : JsonConverter<AudienceValue?>
{
    public override AudienceValue? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var single = reader.GetString();
            return new AudienceValue { Single = single };
        }
        else if (reader.TokenType == JsonTokenType.StartArray)
        {
            var multiple = JsonSerializer.Deserialize<string[]>(ref reader, options);
            return new AudienceValue { Multiple = multiple };
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, AudienceValue? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        if (value.Multiple != null)
        {
            JsonSerializer.Serialize(writer, value.Multiple, options);
        }
        else if (value.Single != null)
        {
            writer.WriteStringValue(value.Single);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

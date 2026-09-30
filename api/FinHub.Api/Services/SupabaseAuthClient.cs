using System.Text.Json;
using System.Text.Json.Serialization;
using FinHub.Api.DTOs;

namespace FinHub.Api.Services;

/// <summary>
/// A client for interacting with Supabase authentication endpoints.
/// </summary>
/// <param name="httpClient">The HTTP client to use for making requests.</param>
/// <param name="configuration">The application configuration.</param>
/// <param name="logger">The logger to use for logging messages.</param>
public sealed class SupabaseAuthClient(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<SupabaseAuthClient> logger)
{
    /// <summary>
    /// Registers a new user with Supabase authentication.
    /// </summary>
    /// <param name="request">The registration request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The registration result.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the Supabase configuration is missing or invalid.</exception>
    public async Task<RegistrationResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        string supabaseUrl = configuration["Supabase:Url"]
            ?? throw new InvalidOperationException("Supabase URL is not configured.");
        string publishableKey = configuration["Supabase:PublishableKey"]
            ?? throw new InvalidOperationException("Supabase publishable key is not configured.");

        using HttpRequestMessage message = new(
            HttpMethod.Post,
            $"{supabaseUrl.TrimEnd('/')}/auth/v1/signup");

        message.Headers.Add("apikey", publishableKey);
        message.Content = JsonContent.Create(new
        {
            email = request.Email,
            password = request.Password,
            data = new
            {
                first_name = request.FirstName,
                last_name = request.LastName,
                preferred_name = request.PreferredName
            }
        });

        using HttpResponseMessage response = await httpClient.SendAsync(message, cancellationToken);
        string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            SupabaseError? error = null;
            try
            {
                error = JsonSerializer.Deserialize<SupabaseError>(responseContent);
            }
            catch (JsonException ex)
            {
                // Log the exception and the raw response content to expect what Supabase actually sent
                logger.LogWarning(ex, "Failed to deserialize Supabase error response as JSON. Raw content: {ResponseContent}", responseContent);
            }

            string errorMessage = error?.Message ?? error?.DetailedMessage ??
                (!string.IsNullOrWhiteSpace(responseContent) ? responseContent : "Registration failed.");

            return RegistrationResult.Failed((int)response.StatusCode, errorMessage);
        }

        SupabaseSignupResponse? result = null;
        try
        {
            result = JsonSerializer.Deserialize<SupabaseSignupResponse>(responseContent);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize successful Supabase signup response as JSON. Raw content: {ResponseContent}", responseContent);
            return RegistrationResult.Failed(
                StatusCodes.Status502BadGateway,
                "Supabase returned an invalid registration response.");
        }

        // Extract user ID whether it is nested in a 'user' object or flattened at the root
        string? rawUserId = result?.User?.Id ?? result?.Id;
        if (rawUserId is null || !Guid.TryParse(rawUserId, out Guid userId))
        {
            return RegistrationResult.Failed(
                StatusCodes.Status502BadGateway,
                "Supabase returned an invalid registration response.");
        }

        // If AccessToken is null or empty, email confirmation is required and no active session was returned
        bool requiresEmailConfirmation = string.IsNullOrEmpty(result?.AccessToken);

        return RegistrationResult.Success(
            new RegisterResponse(userId, requiresEmailConfirmation));
    }

    /// <summary>
    /// Represents the response from Supabase when a user signs up.
    /// </summary>
    private sealed class SupabaseSignupResponse
    {
        /// <summary>
        /// The user object returned by Supabase. This may be null if the user creation failed.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        /// <summary>
        /// The user object returned by Supabase. This may be null if the user creation failed.
        /// </summary>
        [JsonPropertyName("user")]
        public SupabaseUser? User { get; init; }

        /// <summary>
        /// The access token returned by Supabase. This may be null if email confirmation is required.
        /// </summary>
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }

    }

    /// <summary>
    /// Represents a user object returned by Supabase.
    /// </summary>
    private sealed class SupabaseUser
    {
        /// <summary>
        /// The unique identifier of the user. This is a GUID string.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        /// <summary>
        /// The email address of the user. This may be null if the user creation failed or if the email is not provided.
        /// </summary>
        [JsonPropertyName("email")]
        public string? Email { get; init; }
    }

    /// <summary>
    /// Represents an error response from Supabase.
    /// </summary>
    private sealed class SupabaseError
    {
        /// <summary>
        /// The error message returned by Supabase.
        /// </summary>
        [JsonPropertyName("msg")]
        public string? Message { get; init; }

        /// <summary>
        /// The detailed error message returned by Supabase, if available.
        /// </summary>
        [JsonPropertyName("message")]
        public string? DetailedMessage { get; init; }
    }
}

/// <summary>
/// Represents the result of a registration attempt with Supabase.
/// </summary>
/// <param name="Succeeded">The success status of the registration.</param>
/// <param name="StatusCode">The status code of the registration response.</param>
/// <param name="Response">The response from the registration attempt.</param>
/// <param name="Error">The error message, if the registration failed.</param>
public sealed record RegistrationResult(
    bool Succeeded,
    int StatusCode,
    RegisterResponse? Response,
    string? Error)
{
    /// <summary>
    /// Creates a successful registration result with the provided response.
    /// </summary>
    /// <param name="response">The response from the registration attempt.</param>
    /// <returns>A new RegistrationResult instance indicating success.</returns>
    public static RegistrationResult Success(RegisterResponse response) =>
        new(true, StatusCodes.Status201Created, response, null);

    /// <summary>
    /// Creates a failed registration result with the provided status code and error message.
    /// </summary>
    /// <param name="statusCode">The status code of the registration response.</param>
    /// <param name="error">The error message.</param>
    /// <returns>A new RegistrationResult instance indicating failure.</returns>
    public static RegistrationResult Failed(int statusCode, string error) =>
        new(false, statusCode, null, error);
}
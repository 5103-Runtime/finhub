using System.Net.Http.Headers;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using FinHub.Api.DTOs.Auth;

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
    #region Public Methods
    /// <summary>
    /// Registers a new user with Supabase authentication.
    /// </summary>
    /// <param name="request">The registration request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The registration result.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the Supabase configuration is missing or invalid.</exception>
    public async Task<AuthResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        (string supabaseUrl, string publishableKey) = GetSupabaseConfig();

        using HttpRequestMessage message = new(
            HttpMethod.Post,
            $"{supabaseUrl.TrimEnd('/')}/auth/v1/signup");

        message.Headers.Add("apikey", publishableKey);
        message.Content = JsonContent.Create(new
        {
            email = request.Email,
            password = request.Password,
        });

        using HttpResponseMessage response = await httpClient.SendAsync(message, cancellationToken);
        string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return HandleErrorResponse(response, responseContent, "Registration failed.");
        }

        return ParseAuthResponse(responseContent, StatusCodes.Status201Created);
    }

    /// <summary>
    /// Logs in a user with Supabase authentication.
    /// </summary>
    /// <param name="request">The login request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authentication result.</returns>
    public async Task<AuthResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        (string supabaseUrl, string publishableKey) = GetSupabaseConfig();

        using HttpRequestMessage message = new(
            HttpMethod.Post,
            $"{supabaseUrl.TrimEnd('/')}/auth/v1/token?grant_type=password");

        message.Headers.Add("apikey", publishableKey);
        message.Content = JsonContent.Create(new
        {
            email = request.Email,
            password = request.Password
        });

        using HttpResponseMessage response = await httpClient.SendAsync(message, cancellationToken);
        string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return HandleErrorResponse(response, responseContent, "Login failed.");
        }

        return ParseAuthResponse(responseContent, StatusCodes.Status200OK);
    }

    /// <summary>
    /// Exchanges a refresh token for a new Supabase access token.
    /// </summary>
    /// <param name="refreshToken">The refresh token.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The refreshed authentication result.</returns>
    public async Task<AuthResult> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        (string supabaseUrl, string publishableKey) = GetSupabaseConfig();

        using HttpRequestMessage message = new(
            HttpMethod.Post,
            $"{supabaseUrl.TrimEnd('/')}/auth/v1/token?grant_type=refresh_token");

        message.Headers.Add("apikey", publishableKey);
        message.Content = JsonContent.Create(new
        {
            refresh_token = refreshToken
        });

        try
        {
            using HttpResponseMessage response = await httpClient.SendAsync(message, cancellationToken);
            string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return HandleErrorResponse(response, responseContent, "Token refresh failed.");
            }

            return ParseAuthResponse(responseContent, StatusCodes.Status200OK);
        }
        catch (HttpRequestException exception)
        {
            LogSupabaseRequestFailure(exception, "refresh the authentication token");
            return AuthResult.Failed(
                StatusCodes.Status503ServiceUnavailable,
                "Authentication service is temporarily unavailable.",
                "authentication_service_unavailable");
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogSupabaseRequestFailure(exception, "refresh the authentication token");
            return AuthResult.Failed(
                StatusCodes.Status503ServiceUnavailable,
                "Authentication service is temporarily unavailable.",
                "authentication_service_unavailable");
        }
    }

    /// <summary>
    /// Updates the password for the authenticated user using Supabase authentication.
    /// </summary>
    /// <param name="accessToken">The access token for the authenticated user.</param>
    /// <param name="request">The update password request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authentication result.</returns>
    public async Task<AuthResult> UpdatePasswordAsync(
        string accessToken,
        UpdatePasswordRequest request,
        CancellationToken cancellationToken)
    {
        (string supabaseUrl, string publishableKey) = GetSupabaseConfig();

        using HttpRequestMessage message = new(
            HttpMethod.Put,
            $"{supabaseUrl.TrimEnd('/')}/auth/v1/user");

        message.Headers.Add("apikey", publishableKey);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        message.Content = JsonContent.Create(new
        {
            password = request.Password,
            current_password = request.CurrentPassword
        });

        using HttpResponseMessage response = await httpClient.SendAsync(message, cancellationToken);
        string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return HandleErrorResponse(response, responseContent, "Password update failed.");
        }

        // Supabase /user returns a user object, hence reuse ParseAuthResponse with expectTokens: false to safely extract the userId
        return ParseAuthResponse(responseContent, StatusCodes.Status200OK, expectTokens: false);
    }

    /// <summary>
    /// Sends a password recovery email to the user if they forgot their password (FIN-17).
    /// </summary>
    /// <param name="request">The forgot password request containing the email.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authentication result.</returns>
    public async Task<AuthResult> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        (string supabaseUrl, string publishableKey) = GetSupabaseConfig();

        string recoverUrl = $"{supabaseUrl.TrimEnd('/')}/auth/v1/recover";
        string? redirectTo = configuration["Supabase:PasswordResetRedirectUrl"];
        if (string.IsNullOrWhiteSpace(redirectTo))
        {
            logger.LogWarning(
                "Supabase:PasswordResetRedirectUrl is not configured; reset links will use the Supabase default Site URL.");
        }
        else
        {
            recoverUrl += $"?redirect_to={Uri.EscapeDataString(redirectTo)}";
        }

        using HttpRequestMessage message = new(HttpMethod.Post, recoverUrl);

        message.Headers.Add("apikey", publishableKey);
        message.Content = JsonContent.Create(new
        {
            email = request.Email
        });

        using HttpResponseMessage response = await httpClient.SendAsync(message, cancellationToken);
        string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return HandleErrorResponse(response, responseContent, "Password recovery failed.");
        }

        // Supabase /recover returns an empty response, hence just return a 200 OK status code
        return AuthResult.Success(StatusCodes.Status200OK);
    }

    /// <summary>
    /// Sets a new password for a user who followed a password recovery link.
    /// </summary>
    /// <param name="request">The reset request containing the recovery token and new password.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The authentication result.</returns>
    public async Task<AuthResult> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        (string supabaseUrl, string publishableKey) = GetSupabaseConfig();

        using HttpRequestMessage message = new(
            HttpMethod.Put,
            $"{supabaseUrl.TrimEnd('/')}/auth/v1/user");

        message.Headers.Add("apikey", publishableKey);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.AccessToken);
        message.Content = JsonContent.Create(new
        {
            password = request.NewPassword
        });

        try
        {
            using HttpResponseMessage response = await httpClient.SendAsync(message, cancellationToken);
            string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return HandleErrorResponse(response, responseContent, "Password reset failed.");
            }

            return AuthResult.Success(StatusCodes.Status200OK);
        }
        catch (HttpRequestException exception)
        {
            LogSupabaseRequestFailure(exception, "reset the password");
            return AuthResult.Failed(
                StatusCodes.Status503ServiceUnavailable,
                "Authentication service is temporarily unavailable.",
                "authentication_service_unavailable");
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogSupabaseRequestFailure(exception, "reset the password");
            return AuthResult.Failed(
                StatusCodes.Status503ServiceUnavailable,
                "Authentication service is temporarily unavailable.",
                "authentication_service_unavailable");
        }
    }

    /// <summary>
    /// Retrieves the user ID associated with the provided access token from Supabase authentication.
    /// </summary>
    /// <param name="accessToken">The access token.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the user lookup.</returns>
    public async Task<SupabaseUserResult> GetUserIdAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        (string supabaseUrl, string publishableKey) = GetSupabaseConfig();
        using HttpRequestMessage message = new(
            HttpMethod.Get,
            $"{supabaseUrl.TrimEnd('/')}/auth/v1/user");
        message.Headers.Add("apikey", publishableKey);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage? response;
        try
        {
            response = await httpClient.SendAsync(message, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            LogSupabaseRequestFailure(exception, "retrieve the authenticated user");
            return SupabaseUserResult.ServiceUnavailable();
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogSupabaseRequestFailure(exception, "retrieve the authenticated user");
            return SupabaseUserResult.ServiceUnavailable();
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                return response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden
                    ? SupabaseUserResult.InvalidToken()
                    : SupabaseUserResult.ServiceUnavailable();
            }

            string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            try
            {
                SupabaseUser? user = JsonSerializer.Deserialize<SupabaseUser>(responseContent);
                return user?.Id is not null && Guid.TryParse(user.Id, out Guid userId)
                    ? SupabaseUserResult.Authenticated(userId)
                    : SupabaseUserResult.ServiceUnavailable();
            }
            catch (JsonException exception)
            {
                logger.LogWarning(exception, "Supabase returned an invalid user response.");
                return SupabaseUserResult.ServiceUnavailable();
            }
        }
    }

    /// <summary>
    /// Invalidates the current user's Supabase session.
    /// </summary>
    /// <param name="accessToken">The access token to invalidate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True when Supabase successfully invalidates the session.</returns>
    public async Task<bool> LogoutAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        (string supabaseUrl, string publishableKey) = GetSupabaseConfig();
        using HttpRequestMessage message = new(
            HttpMethod.Post,
            $"{supabaseUrl.TrimEnd('/')}/auth/v1/logout");

        message.Headers.Add("apikey", publishableKey);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            using HttpResponseMessage response = await httpClient.SendAsync(message, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            logger.LogWarning(
                "Supabase logout failed with status code {StatusCode}.",
                (int)response.StatusCode);
            return false;
        }
        catch (HttpRequestException exception)
        {
            LogSupabaseRequestFailure(exception, "log out the user");
            return false;
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogSupabaseRequestFailure(exception, "log out the user");
            return false;
        }
    }
    #endregion

    #region Private Helper Methods
    /// <summary>
    /// Logs a non-cancellation failure while calling Supabase.
    /// </summary>
    /// <param name="exception">The request failure.</param>
    /// <param name="operation">The operation that could not be completed.</param>
    private void LogSupabaseRequestFailure(Exception exception, string operation)
    {
        logger.LogWarning(
            exception,
            "Supabase request could not {Operation}.",
            operation);
    }

    /// <summary>
    /// Retrieves the Supabase configuration (URL and publishable key) from the application settings.
    /// </summary>
    /// <returns>A tuple containing the Supabase URL and publishable key.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the Supabase configuration is missing.</exception>
    private (string Url, string Key) GetSupabaseConfig()
    {
        string url = configuration["Supabase:Url"]
            ?? throw new InvalidOperationException("Supabase URL is not configured.");
        string key = configuration["Supabase:PublishableKey"]
            ?? throw new InvalidOperationException("Supabase publishable key is not configured.");
        return (url, key);
    }

    /// <summary>
    /// Parses the authentication response from Supabase and constructs an AuthResult.
    /// </summary>
    /// <param name="responseContent">The raw response content from Supabase.</param>
    /// <param name="statusCode">The status code of the response.</param>
    /// <param name="expectTokens">Indicates whether tokens are expected in the response.</param>
    /// <returns>The authentication result.</returns>
    private AuthResult ParseAuthResponse(string responseContent, int statusCode, bool expectTokens = true)
    {
        SupabaseAuthResponse? result = null;
        try
        {
            result = JsonSerializer.Deserialize<SupabaseAuthResponse>(responseContent);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize Supabase auth response. Raw content: {ResponseContent}", responseContent);
            return AuthResult.Failed(StatusCodes.Status502BadGateway, "Supabase returned an invalid authentication response.");
        }

        string? rawUserId = result?.User?.Id ?? result?.Id;
        if (rawUserId is null || !Guid.TryParse(rawUserId, out Guid userId))
        {
            return AuthResult.Failed(StatusCodes.Status502BadGateway, "Supabase returned an invalid user identifier.");
        }

        bool requiresEmailConfirmation = expectTokens && string.IsNullOrEmpty(result?.AccessToken);

        AuthResponse responseDto = new(
            userId,
            expectTokens ? result?.AccessToken : null,
            expectTokens ? result?.RefreshToken : null,
            expectTokens ? result?.ExpiresIn : null,
            requiresEmailConfirmation);

        return AuthResult.Success(statusCode, responseDto);
    }

    /// <summary>
    /// Handles error responses from Supabase and constructs an AuthResult indicating failure.
    /// </summary>
    /// <param name="response">The HTTP response from Supabase.</param>
    /// <param name="responseContent">The raw response content from Supabase.</param>
    /// <param name="defaultMessage">The default error message to use if the response content is empty.</param>
    /// <returns>The authentication result indicating failure.</returns>
    private AuthResult HandleErrorResponse(HttpResponseMessage response, string responseContent, string defaultMessage)
    {
        SupabaseError? error = null;
        try
        {
            error = JsonSerializer.Deserialize<SupabaseError>(responseContent);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to deserialize Supabase error response. Raw content: {ResponseContent}", responseContent);
        }

        string errorMessage = !string.IsNullOrWhiteSpace(error?.Message)
            ? error.Message
            : (error?.ErrorCode ?? (!string.IsNullOrWhiteSpace(responseContent) ? responseContent : defaultMessage));
        string? errorCode = error?.ErrorCode;

        return AuthResult.Failed((int)response.StatusCode, errorMessage, errorCode);
    }
    #endregion

    /// <summary>
    /// Represents the response from Supabase when a user attempts to authenticate or register.
    /// </summary>
    private sealed class SupabaseAuthResponse
    {
        /// <summary>
        /// The unique identifier of the user. This is a GUID string.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        /// <summary>
        /// The user object returned by Supabase. This may be null if the user creation failed or if the email is not provided.
        /// </summary>
        [JsonPropertyName("user")]
        public SupabaseUser? User { get; init; }

        /// <summary>
        /// The access token returned by Supabase upon successful authentication. 
        /// This may be null if the user needs to confirm their email or if the authentication failed.
        /// </summary>
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }

        /// <summary>
        /// The refresh token returned by Supabase upon successful authentication.
        /// This may be null if the user needs to confirm their email or if the authentication failed
        /// </summary>
        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; init; }

        /// <summary>
        /// The number of seconds until the access token expires. 
        /// This may be null if the user needs to confirm their email or if the authentication failed.
        /// </summary>
        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; init; }
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
    }

    /// <summary>
    /// Represents an error response from Supabase.
    /// </summary>
    private sealed class SupabaseError
    {
        /// <summary>
        /// The HTTP status code returned by Supabase.
        /// </summary>
        [JsonPropertyName("code")]
        public int? Code { get; init; }

        /// <summary>
        /// The error code returned by Supabase.
        /// </summary>
        [JsonPropertyName("error_code")]
        public string? ErrorCode { get; init; }

        /// <summary>
        /// The error message returned by Supabase.
        /// </summary>
        [JsonPropertyName("msg")]
        public string? Message { get; init; }
    }
}

/// <summary>
/// Represents the result of a authentication attempt with Supabase.
/// </summary>
/// <param name="Succeeded">The success status of the authentication.</param>
/// <param name="StatusCode">The status code of the authentication response.</param>
/// <param name="Response">The response from the authentication attempt.</param>
/// <param name="Error">The error message, if the authentication failed.</param>
/// <param name="ErrorCode">The error code, if the authentication failed.</param>
public sealed record AuthResult(
    bool Succeeded,
    int StatusCode,
    AuthResponse? Response,
    string? Error,
    string? ErrorCode)
{
    /// <summary>
    /// Creates a successful authentication result with the provided response.
    /// </summary>
    /// <param name="statusCode">The status code of the authentication response.</param>
    /// <param name="response">The response from the authentication attempt.</param>
    /// <returns>A new AuthResult instance indicating success.</returns>
    public static AuthResult Success(int statusCode, AuthResponse? response = null) => new(true, statusCode, response, null, null);

    /// <summary>
    /// Creates a failed authentication result with the provided status code and error message.
    /// </summary>
    /// <param name="statusCode">The status code of the authentication response.</param>
    /// <param name="error">The error message.</param>
    /// <param name="errorCode">The error code.</param>
    /// <returns>A new AuthResult instance indicating failure.</returns>
    public static AuthResult Failed(int statusCode, string error, string? errorCode = null) => new(false, statusCode, null, error, errorCode);
}

/// <summary>
/// Represents the result of looking up an authenticated Supabase user.
/// </summary>
/// <param name="UserId">The authenticated user's ID, if available.</param>
/// <param name="Status">The outcome of the lookup.</param>
public sealed record SupabaseUserResult(
    Guid? UserId,
    SupabaseUserLookupStatus Status)
{
    /// <summary>
    /// Creates a successful user lookup result.
    /// </summary>
    public static SupabaseUserResult Authenticated(Guid userId) =>
        new(userId, SupabaseUserLookupStatus.Authenticated);

    /// <summary>
    /// Creates a result indicating that the access token is invalid.
    /// </summary>
    public static SupabaseUserResult InvalidToken() =>
        new(null, SupabaseUserLookupStatus.InvalidToken);

    /// <summary>
    /// Creates a result indicating that Supabase could not be reached or returned an invalid response.
    /// </summary>
    public static SupabaseUserResult ServiceUnavailable() =>
        new(null, SupabaseUserLookupStatus.ServiceUnavailable);
}

/// <summary>
/// Describes the outcome of a Supabase user lookup.
/// </summary>
public enum SupabaseUserLookupStatus
{
    /// <summary>
    /// Supabase authenticated the user.
    /// </summary>
    Authenticated,

    /// <summary>
    /// Supabase rejected the access token.
    /// </summary>
    InvalidToken,

    /// <summary>
    /// Supabase could not complete the lookup.
    /// </summary>
    ServiceUnavailable
}

namespace FinHub.Api.Constants;

/// <summary>
/// Contains constants related to authentication cookies used in the application.
/// </summary>
public static class AuthCookies
{
    /// <summary>
    /// The name of the cookie used to store the access token for authenticated users.
    /// </summary>
    public const string AccessToken = @"finhub_access_token";

    /// <summary>
    /// The name of the cookie used to store the refresh token for authenticated users.
    /// </summary>
    public const string RefreshToken = @"finhub_refresh_token";
}

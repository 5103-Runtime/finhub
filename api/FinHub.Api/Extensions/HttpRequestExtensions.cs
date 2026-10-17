using FinHub.Api.Constants;

namespace FinHub.Api.Extensions;

/// <summary>
/// Reads FinHub authentication cookies from a request.
/// </summary>
public static class HttpRequestExtensions
{
    /// <summary>
    /// Retrieves the access token from the request cookies.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <returns>The access token, or null if it is not found.</returns>
    public static string? GetAccessToken(this HttpRequest request) => GetCookie(request, AuthCookies.AccessToken);

    /// <summary>
    /// Retrieves the refresh token from the request cookies.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <returns>The refresh token, or null if it is not found.</returns>
    public static string? GetRefreshToken(this HttpRequest request) => GetCookie(request, AuthCookies.RefreshToken);

    private static string? GetCookie(HttpRequest request, string name) =>
        request.Cookies.TryGetValue(name, out string? value) && !string.IsNullOrWhiteSpace(value) ? value : null;
}

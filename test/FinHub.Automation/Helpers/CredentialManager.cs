using Microsoft.Extensions.Configuration;
using FinHub.Automation.Models;

namespace FinHub.Automation.Helpers;

/// <summary>
/// A class that handles retrieving FinHub credentials from environment variables.
/// </summary>
internal static class CredentialManager
{
    // Private attributes
    private static readonly Lazy<Credentials> _credentials;
    private const string EnvironmentVariablePrefix = "FinHubAutomation__";

    // Public properties
    private static Credentials CredentialSet => _credentials.Value;

    // Constructor
    static CredentialManager()
    {
        _credentials = new(LoadCredentials);
    }

    // Public methods
    /// <summary>
    /// Retrieves the credentials associated with the specified role.
    /// </summary>
    /// <param name="role">The role for which to retrieve credentials.</param>
    /// <returns>The email and password associated with the specified role.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the specified role is not supported.</exception>
    public static RoleCredentials GetCredentials(string role)
    {
        return role.Trim().ToLowerInvariant() switch
        {
            "user" => ValidateCredentials(CredentialSet.User, role),
            _ => throw new KeyNotFoundException($"[ERROR] Role '{role}' not found in credentials.")
        };
    }

    // Private helper methods
    /// <summary>
    /// Validates that the provided credentials contain both an email and a password.
    /// </summary>
    /// <param name="credentials">The credentials to validate.</param>
    /// <param name="role">The role associated with the credentials.</param>
    /// <returns>The validated credentials.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the credentials are invalid.</exception>
    private static RoleCredentials ValidateCredentials(RoleCredentials credentials, string role)
    {
        if (string.IsNullOrWhiteSpace(credentials.Email) || string.IsNullOrWhiteSpace(credentials.Password))
        {
            throw new InvalidOperationException($"[ERROR] Email and password are required for role '{role}'.");
        }

        return credentials;
    }

    /// <summary>
    /// Loads the credentials from environment variables.
    /// </summary>
    /// <returns>The loaded credentials.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the credentials are invalid.</exception>
    private static Credentials LoadCredentials()
    {
        IConfigurationRoot configEngine = new ConfigurationBuilder()
            .AddUserSecrets(typeof(CredentialManager).Assembly, optional: true)
            .AddEnvironmentVariables(EnvironmentVariablePrefix)
            .Build();

        Credentials credentials = new();
        configEngine.Bind(credentials);

        if (string.IsNullOrWhiteSpace(credentials.User.Email) ||
            string.IsNullOrWhiteSpace(credentials.User.Password))
        {
            throw new InvalidOperationException(
                $"[ERROR] Test credentials are incomplete. Set {EnvironmentVariablePrefix}User__Email, " +
                $"{EnvironmentVariablePrefix}User__Password.");
        }

        return credentials;
    }
}

using Microsoft.Extensions.Configuration;
using FinHub.Automation.Models;

namespace FinHub.Automation.Helpers;

/// <summary>
/// A class that handles parsing and retrieving FinHub credentials from credentials.json.
/// </summary>
internal static class CredentialManager
{
    // Private attributes
    private static readonly Lazy<Credentials> _credentials;
    private const string CredentialsFileName = "credentials.json";

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
            "admin" => ValidateCredentials(CredentialSet.Admin, role),
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
    /// Loads the credentials from the credentials.json file located in the project root directory.
    /// </summary>
    /// <returns>The loaded credentials.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the credentials.json file is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the credentials are invalid.</exception>
    private static Credentials LoadCredentials()
    {
        try
        {
            // Determine the path to the credentials.json file on the project root directory
            string projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
            string jsonPath = Path.Combine(projectRoot, CredentialsFileName);

            if (!File.Exists(jsonPath))
            {
                throw new FileNotFoundException($"[ERROR] Could not find {CredentialsFileName} at: {jsonPath}");
            }

            // Build the configuration from the credentials.json file
            IConfigurationRoot configEngine = new ConfigurationBuilder()
                .SetBasePath(projectRoot)
                .AddJsonFile(CredentialsFileName, optional: false, reloadOnChange: true)
                .Build();

            Credentials credentials = new();
            configEngine.Bind(credentials);

            return credentials;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"[ERROR] Failed to load credentials: {ex.Message}", ex);
        }
    }
}

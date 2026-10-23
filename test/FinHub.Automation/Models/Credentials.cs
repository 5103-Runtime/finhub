namespace FinHub.Automation.Models;

/// <summary>
/// Contains the credentials for each application role.
/// </summary>
internal sealed class Credentials
{
    /// <summary>
    /// The credentials for the user role.
    /// </summary>
    public RoleCredentials User { get; set; } = new();

    /// <summary>
    /// The credentials for the admin role.
    /// </summary>
    public RoleCredentials Admin { get; set; } = new();
}

/// <summary>
/// Represents the email and password for an application role.
/// </summary>
internal sealed class RoleCredentials
{
    /// <summary>
    /// The email address associated with the role.
    /// </summary>
    public string Email { get; set; } = "";

    /// <summary>
    /// The password associated with the role.
    /// </summary>
    public string Password { get; set; } = "";
}
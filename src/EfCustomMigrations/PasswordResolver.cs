namespace EfCustomMigrations;
/// <summary>
/// Represents password resolver.
/// </summary>

internal static class PasswordResolver
{
    /// <summary>
    /// Performs the resolve operation.
    /// </summary>
    /// <param name="password">The password.</param>
    /// <param name="passwordEnvironmentVariable">The password environment variable.</param>
    /// <returns>The operation result.</returns>
    public static string Resolve(string? password, string? passwordEnvironmentVariable)
    {
        if (!string.IsNullOrWhiteSpace(password))
        {
            return password;
        }

        if (!string.IsNullOrWhiteSpace(passwordEnvironmentVariable))
        {
            var environmentPassword = Environment.GetEnvironmentVariable(passwordEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(environmentPassword))
            {
                return environmentPassword;
            }

            throw new InvalidOperationException(
                $"Environment variable '{passwordEnvironmentVariable}' was not found or is empty.");
        }

        throw new InvalidOperationException(
            "Either operation.Password or operation.PasswordEnvironmentVariable must be set.");
    }
}

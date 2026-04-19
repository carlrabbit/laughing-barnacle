namespace EfCustomMigrations;

internal static class PasswordResolver
{
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

namespace CliTool.Helpers;

internal static class PasswordHelper
{
    private const string EnvVarName = "RCLONE_ENCRYPT_PASSWORD";

    internal static string Resolve(string? flagPassword)
    {
        if (flagPassword is not null)
        {
            Console.Error.WriteLine(
                "WARNING: Passing --password on the command line is insecure. " +
                $"Consider using the {EnvVarName} environment variable instead. " +
                "Your password may be visible in terminal history.");
            return flagPassword;
        }

        var envPassword = Environment.GetEnvironmentVariable(EnvVarName);
        if (envPassword is not null)
            return envPassword;

        return PromptPassword();
    }

    private static string PromptPassword()
    {
        Console.Error.Write("Password: ");
        var sb = new System.Text.StringBuilder();

        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.Error.WriteLine();
                break;
            }
            if (key.Key == ConsoleKey.Backspace)
            {
                if (sb.Length > 0)
                    sb.Length--;
            }
            else
            {
                sb.Append(key.KeyChar);
            }
        }

        return sb.ToString();
    }
}

namespace Modulus.Cli.Services;

/// <summary>
/// Optional path overrides for CLI execution, commonly used by tests/automation.
/// </summary>
public sealed record CliPathOverrides(string? DatabasePath, string? ModulesDirectory)
{
    public static CliPathOverrides FromEnvironment()
    {
        var db = Environment.GetEnvironmentVariable(CliEnvironmentVariables.DatabasePath);
        var modulesDir = Environment.GetEnvironmentVariable(CliEnvironmentVariables.ModulesDirectory);

        return new CliPathOverrides(
            string.IsNullOrWhiteSpace(db) ? null : db.Trim(),
            string.IsNullOrWhiteSpace(modulesDir) ? null : modulesDir.Trim());
    }
}



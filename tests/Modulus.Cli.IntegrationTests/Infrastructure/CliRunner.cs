using Modulus.Cli.Services;

namespace Modulus.Cli.IntegrationTests.Infrastructure;

/// <summary>
/// Executes CLI commands for integration testing (in-process).
/// </summary>
public sealed class CliRunner
{
    private readonly CliTestContext _context;

    /// <summary>
    /// Default timeout for CLI commands.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);

    public CliRunner(CliTestContext context)
    {
        _context = context;
    }

    public async Task<CliResult> RunAsync(IReadOnlyList<string> args, string? workingDirectory = null, string? standardInput = null)
    {
        var effectiveWorkingDir = workingDirectory ?? _context.WorkingDirectory;

        var oldCwd = Directory.GetCurrentDirectory();
        var oldDb = Environment.GetEnvironmentVariable(CliEnvironmentVariables.DatabasePath);
        var oldModules = Environment.GetEnvironmentVariable(CliEnvironmentVariables.ModulesDirectory);

        var oldOut = Console.Out;
        var oldErr = Console.Error;
        var oldIn = Console.In;

        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        using var stdin = new StringReader(standardInput ?? "");

        try
        {
            Directory.SetCurrentDirectory(effectiveWorkingDir);

            Environment.SetEnvironmentVariable(CliEnvironmentVariables.DatabasePath, _context.DatabasePath);
            Environment.SetEnvironmentVariable(CliEnvironmentVariables.ModulesDirectory, _context.ModulesDirectory);

            Console.SetOut(stdout);
            Console.SetError(stderr);
            Console.SetIn(stdin);

            var exitCode = await Modulus.Cli.Program.Main(args.ToArray());

            return new CliResult
            {
                ExitCode = exitCode,
                StandardOutput = stdout.ToString(),
                StandardError = stderr.ToString()
            };
        }
        catch (Exception ex)
        {
            return new CliResult
            {
                ExitCode = 1,
                StandardOutput = stdout.ToString(),
                StandardError = stderr.ToString(),
                Exception = ex
            };
        }
        finally
        {
            Console.SetOut(oldOut);
            Console.SetError(oldErr);
            Console.SetIn(oldIn);

            Environment.SetEnvironmentVariable(CliEnvironmentVariables.DatabasePath, oldDb);
            Environment.SetEnvironmentVariable(CliEnvironmentVariables.ModulesDirectory, oldModules);

            Directory.SetCurrentDirectory(oldCwd);
        }
    }

    public Task<CliResult> NewAsync(
        string moduleName,
        string? template = null,
        string? outputPath = null,
        bool force = false)
    {
        var args = new List<string> { "new" };
        if (!string.IsNullOrWhiteSpace(template))
        {
            args.Add(template);
        }

        args.Add("--name");
        args.Add(moduleName);

        if (!string.IsNullOrEmpty(outputPath))
        {
            args.Add("--output");
            args.Add(outputPath);
        }

        if (force)
        {
            args.Add("--force");
        }

        return RunAsync(args);
    }

    public Task<CliResult> BuildAsync(
        string? path = null,
        string configuration = "Release",
        bool verbose = false)
    {
        var args = new List<string> { "build" };
        if (!string.IsNullOrEmpty(path))
        {
            args.Add("--path");
            args.Add(path);
        }

        args.Add("--configuration");
        args.Add(configuration);

        if (verbose)
        {
            args.Add("--verbose");
        }

        return RunAsync(args);
    }

    public Task<CliResult> PackAsync(
        string? path = null,
        string? output = null,
        string configuration = "Release",
        bool noBuild = false,
        bool verbose = false)
    {
        var args = new List<string> { "pack" };
        if (!string.IsNullOrEmpty(path))
        {
            args.Add("--path");
            args.Add(path);
        }

        if (!string.IsNullOrEmpty(output))
        {
            args.Add("--output");
            args.Add(output);
        }

        args.Add("--configuration");
        args.Add(configuration);

        if (noBuild)
        {
            args.Add("--no-build");
        }

        if (verbose)
        {
            args.Add("--verbose");
        }

        return RunAsync(args);
    }

    public Task<CliResult> InstallAsync(string source, bool force = false, bool verbose = false)
    {
        var args = new List<string> { "install", source };
        if (force) args.Add("--force");
        if (verbose) args.Add("--verbose");
        return RunAsync(args);
    }

    public Task<CliResult> UninstallAsync(string module, bool force = false, bool verbose = false)
    {
        var args = new List<string> { "uninstall", module };
        if (force) args.Add("--force");
        if (verbose) args.Add("--verbose");
        return RunAsync(args);
    }

    public Task<CliResult> ListAsync(bool verbose = false)
    {
        var args = new List<string> { "list" };
        if (verbose) args.Add("--verbose");
        return RunAsync(args);
    }
}



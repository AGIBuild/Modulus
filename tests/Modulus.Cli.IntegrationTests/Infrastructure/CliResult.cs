namespace Modulus.Cli.IntegrationTests.Infrastructure;

/// <summary>
/// Represents the result of a CLI command execution.
/// </summary>
public class CliResult
{
    /// <summary>
    /// Exit code from the process (0 = success).
    /// </summary>
    public int ExitCode { get; init; }
    
    /// <summary>
    /// Standard output from the command.
    /// </summary>
    public string StandardOutput { get; init; } = string.Empty;
    
    /// <summary>
    /// Standard error from the command.
    /// </summary>
    public string StandardError { get; init; } = string.Empty;
    
    /// <summary>
    /// Whether the command succeeded (exit code 0).
    /// </summary>
    public bool IsSuccess => ExitCode == 0;
    
    /// <summary>
    /// Combined output (stdout + stderr) for easier assertion.
    /// </summary>
    public string CombinedOutput => string.IsNullOrEmpty(StandardError) 
        ? StandardOutput 
        : $"{StandardOutput}\n{StandardError}";
    
    /// <summary>
    /// Exception if the command failed to execute.
    /// </summary>
    public Exception? Exception { get; init; }
}



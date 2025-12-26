namespace Modulus.Cli.Services;

public interface IProcessRunner
{
    Task<ProcessRunResult> RunAsync(ProcessRunRequest request, CancellationToken cancellationToken);
}

public sealed record ProcessRunRequest(
    string FileName,
    string Arguments,
    string WorkingDirectory,
    bool RedirectOutput);

public sealed record ProcessRunResult(
    int ExitCode,
    string StandardOutput,
    string StandardError);



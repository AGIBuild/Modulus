using System.Diagnostics;

namespace Modulus.Cli.Services;

/// <summary>
/// Default process runner using <see cref="Process"/>.
/// </summary>
public sealed class ProcessRunner : IProcessRunner
{
    public async Task<ProcessRunResult> RunAsync(ProcessRunRequest request, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = request.FileName,
            Arguments = request.Arguments,
            WorkingDirectory = request.WorkingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = request.RedirectOutput,
            RedirectStandardError = request.RedirectOutput,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        if (!request.RedirectOutput)
        {
            await process.WaitForExitAsync(cancellationToken);
            return new ProcessRunResult(process.ExitCode, "", "");
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await Task.WhenAll(process.WaitForExitAsync(cancellationToken), stdoutTask, stderrTask);

        return new ProcessRunResult(
            process.ExitCode,
            await stdoutTask,
            await stderrTask);
    }
}



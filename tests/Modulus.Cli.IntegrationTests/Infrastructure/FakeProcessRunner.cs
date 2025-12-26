using Modulus.Cli.Services;

namespace Modulus.Cli.IntegrationTests.Infrastructure;

internal sealed class FakeProcessRunner : IProcessRunner
{
    private readonly Func<ProcessRunRequest, ProcessRunResult> _run;

    public FakeProcessRunner(Func<ProcessRunRequest, ProcessRunResult> run)
    {
        _run = run;
    }

    public Task<ProcessRunResult> RunAsync(ProcessRunRequest request, CancellationToken cancellationToken)
        => Task.FromResult(_run(request));
}



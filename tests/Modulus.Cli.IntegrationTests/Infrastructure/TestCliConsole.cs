using Modulus.Cli.Services;

namespace Modulus.Cli.IntegrationTests.Infrastructure;

internal sealed class TestCliConsole : ICliConsole
{
    public TestCliConsole(
        TextWriter? @out = null,
        TextWriter? error = null,
        TextReader? @in = null,
        bool isInputRedirected = false)
    {
        Out = @out ?? new StringWriter();
        Error = error ?? new StringWriter();
        In = @in ?? new StringReader("");
        IsInputRedirected = isInputRedirected;
    }

    public TextWriter Out { get; }
    public TextWriter Error { get; }
    public TextReader In { get; }
    public bool IsInputRedirected { get; }
}



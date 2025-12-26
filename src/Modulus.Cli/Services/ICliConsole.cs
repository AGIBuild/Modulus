namespace Modulus.Cli.Services;

/// <summary>
/// Minimal console abstraction for CLI handlers.
/// </summary>
public interface ICliConsole
{
    TextWriter Out { get; }
    TextWriter Error { get; }
    TextReader In { get; }
    bool IsInputRedirected { get; }
}



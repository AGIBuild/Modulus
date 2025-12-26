namespace Modulus.Cli.Services;

/// <summary>
/// Default console implementation backed by <see cref="System.Console"/>.
/// </summary>
public sealed class SystemCliConsole : ICliConsole
{
    public TextWriter Out => Console.Out;
    public TextWriter Error => Console.Error;
    public TextReader In => Console.In;
    public bool IsInputRedirected => Console.IsInputRedirected;
}



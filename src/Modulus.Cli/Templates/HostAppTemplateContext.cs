namespace Modulus.Cli.Templates;

public sealed class HostAppTemplateContext
{
    public required string AppName { get; init; }

    public string AppNameLower => AppName.ToLowerInvariant();

    public required TargetHostType TargetHost { get; init; }
}



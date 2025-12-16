namespace Modulus.Sdk;

/// <summary>
/// Standard asset types for extension.vsixmanifest.
/// </summary>
public static class ModulusAssetTypes
{
    /// <summary>
    /// Package assembly containing entry points (ModulusPackage/ModulusComponent).
    /// Runtime loads and scans for entry point types.
    /// </summary>
    public const string Package = "Modulus.Package";

    /// <summary>
    /// Regular dependency assembly. Runtime loads but does not scan for entry points.
    /// </summary>
    public const string Assembly = "Modulus.Assembly";

    /// <summary>
    /// Extension icon resource.
    /// </summary>
    public const string Icon = "Modulus.Icon";

    /// <summary>
    /// License file.
    /// </summary>
    public const string License = "Modulus.License";

    /// <summary>
    /// README file.
    /// </summary>
    public const string Readme = "Modulus.Readme";
}


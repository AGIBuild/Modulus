namespace Modulus.Sdk;

/// <summary>
/// Base class for extension entry points. Similar to VS VsPackage.
/// The runtime discovers types inheriting from this class and uses them as entry points.
/// 
/// This is the recommended base class for new extensions.
/// </summary>
#pragma warning disable CS0618 // Suppress obsolete warning - ModulusPackage inherits from ModulusComponent for backward compatibility
public abstract class ModulusPackage : ModulusComponent
#pragma warning restore CS0618
{
    // Inherits all lifecycle methods from ModulusComponent.
    // No additional members needed - this is primarily a naming/semantic change
    // to align with VS Extension terminology.
}


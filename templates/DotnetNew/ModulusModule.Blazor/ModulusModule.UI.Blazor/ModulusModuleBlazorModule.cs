using Modulus.Sdk;
using Modulus.UI.Abstractions;

namespace Modulus.Modules.ModulusModule.UI.Blazor;

/// <summary>
/// ModulusModule Blazor UI Module.
/// </summary>
[DependsOn(typeof(ModulusModuleModule))]
[BlazorMenu("modulusmodule", "{{DisplayNameComputed}}", "/modulusmodule", Icon = IconKind.Folder, Order = 100)]
public class ModulusModuleBlazorModule : ModulusPackage
{
}


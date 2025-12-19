using Modulus.Sdk;
using Modulus.UI.Abstractions;
using Modulus.UI.Avalonia.Infrastructure;
using Modulus.Modules.ModulusModule.ViewModels;

namespace Modulus.Modules.ModulusModule.UI.Avalonia;

/// <summary>
/// ModulusModule Avalonia UI Module.
/// </summary>
[DependsOn(typeof(ModulusModuleModule))]
[AvaloniaMenu("modulusmodule", "{{DisplayNameComputed}}", typeof(MainViewModel), Icon = IconKind.Folder, Order = 100)]
public class ModulusModuleAvaloniaModule : AvaloniaModuleBase
{
}


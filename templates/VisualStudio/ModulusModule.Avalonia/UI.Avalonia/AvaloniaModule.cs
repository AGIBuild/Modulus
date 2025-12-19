using Modulus.Sdk;
using Modulus.UI.Avalonia.Infrastructure;
using Modulus.UI.Abstractions;
using Modulus.Modules.$ext_safeprojectname$.ViewModels;

namespace Modulus.Modules.$ext_safeprojectname$.UI.Avalonia;

/// <summary>
/// $ext_safeprojectname$ Avalonia UI Module.
/// </summary>
[DependsOn(typeof($ext_safeprojectname$Module))]
[AvaloniaMenu("$ext_safeprojectname$", "$ext_safeprojectname$", typeof(MainViewModel), Icon = IconKind.Apps, Order = 100)]
public class $ext_safeprojectname$AvaloniaModule : AvaloniaModuleBase
{
}


using Modulus.Sdk;
using Modulus.UI.Abstractions;

namespace Modulus.Modules.$ext_safeprojectname$.UI.Blazor;

/// <summary>
/// $ext_safeprojectname$ Blazor UI Module.
/// </summary>
[DependsOn(typeof($ext_safeprojectname$Module))]
[BlazorMenu("$ext_safeprojectname$", "$ext_safeprojectname$", "/$ext_safeprojectname$", Icon = IconKind.Apps, Order = 100)]
public class $ext_safeprojectname$BlazorModule : ModulusPackage
{
}


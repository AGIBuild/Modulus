namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Minimal RouteAttribute stub for metadata-only tests.
/// The production host provides the real attribute via ASP.NET Core.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class RouteAttribute : Attribute
{
    public string Template { get; }

    public RouteAttribute(string template)
    {
        Template = template;
    }
}



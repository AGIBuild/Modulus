using System.Reflection;
using Modulus.Sdk;
using Modulus.UI.Abstractions;

namespace Modulus.Core.Installation;

/// <summary>
/// Reads menu attributes from module assemblies without executing module code.
/// </summary>
public static class ModuleMenuAttributeReader
{
    private static readonly string ModulusPackageFullName = typeof(ModulusPackage).FullName ?? "Modulus.Sdk.ModulusPackage";
    private static readonly string BlazorMenuAttributeFullName = typeof(BlazorMenuAttribute).FullName ?? "Modulus.Sdk.BlazorMenuAttribute";
    private static readonly string AvaloniaMenuAttributeFullName = typeof(AvaloniaMenuAttribute).FullName ?? "Modulus.Sdk.AvaloniaMenuAttribute";

    /// <summary>
    /// Reads menu attributes from an assembly for the specified host type.
    /// </summary>
    public static IReadOnlyList<MenuInfo> ReadMenus(string assemblyPath, string hostType)
    {
        if (string.IsNullOrWhiteSpace(assemblyPath))
            throw new ArgumentException("Assembly path cannot be null or empty.", nameof(assemblyPath));
        if (string.IsNullOrWhiteSpace(hostType))
            throw new ArgumentException("Host type cannot be null or empty.", nameof(hostType));

        // NOTE: Use MetadataLoadContext to avoid executing module code (no static initializers, no attribute instantiation).
        var resolverPaths = BuildResolverPaths(assemblyPath);
        var resolver = new PathAssemblyResolver(resolverPaths);

        using var mlc = new MetadataLoadContext(resolver);
        var assembly = mlc.LoadFromAssemblyPath(assemblyPath);

        var menus = new List<MenuInfo>();
        var isBlazor = ModulusHostIds.Matches(hostType, ModulusHostIds.Blazor);
        var isAvalonia = ModulusHostIds.Matches(hostType, ModulusHostIds.Avalonia);

        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface)
                continue;

            if (!IsDerivedFrom(type, ModulusPackageFullName))
                continue;

            var declaringType = type.FullName ?? type.Name;
            var attrs = type.GetCustomAttributesData();

            if (isBlazor)
                AppendBlazorMenus(declaringType, attrs, menus);
            else if (isAvalonia)
                AppendAvaloniaMenus(declaringType, attrs, menus);
        }

        return menus;
    }

    private static IEnumerable<string> BuildResolverPaths(string assemblyPath)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            assemblyPath,
            typeof(object).Assembly.Location,
            typeof(ModulusPackage).Assembly.Location,
            typeof(BlazorMenuAttribute).Assembly.Location,
            typeof(MenuLocation).Assembly.Location
        };

        var dir = Path.GetDirectoryName(assemblyPath);
        if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
        {
            foreach (var dll in Directory.GetFiles(dir, "*.dll"))
                paths.Add(dll);
        }

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                if (asm.IsDynamic) continue;
                if (string.IsNullOrWhiteSpace(asm.Location)) continue;
                paths.Add(asm.Location);
            }
            catch
            {
                // ignore
            }
        }

        return paths;
    }

    private static bool IsDerivedFrom(Type type, string baseTypeFullName)
    {
        for (var t = type; t != null; t = t.BaseType)
        {
            if (string.Equals(t.FullName, baseTypeFullName, StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    private static void AppendBlazorMenus(string declaringType, IList<CustomAttributeData> attrs, List<MenuInfo> menus)
    {
        foreach (var a in attrs)
        {
            if (!string.Equals(a.AttributeType.FullName, BlazorMenuAttributeFullName, StringComparison.Ordinal))
                continue;

            if (a.ConstructorArguments.Count < 3)
                throw new InvalidOperationException($"Invalid [BlazorMenu] on '{declaringType}': expected constructor arguments (key, displayName, route).");

            var key = a.ConstructorArguments[0].Value as string;
            var displayName = a.ConstructorArguments[1].Value as string;
            var route = a.ConstructorArguments[2].Value as string;

            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException($"Invalid [BlazorMenu] on '{declaringType}': 'key' is required.");
            if (string.IsNullOrWhiteSpace(displayName))
                throw new InvalidOperationException($"Invalid [BlazorMenu] on '{declaringType}': 'displayName' is required.");
            if (string.IsNullOrWhiteSpace(route))
                throw new InvalidOperationException($"Invalid [BlazorMenu] on '{declaringType}': 'route' is required.");

            var icon = IconKind.Grid;
            var location = MenuLocation.Main;
            var order = 50;

            foreach (var na in a.NamedArguments)
            {
                if (string.Equals(na.MemberName, nameof(BlazorMenuAttribute.Icon), StringComparison.Ordinal) &&
                    TryGetInt32(na.TypedValue, out var iconValue))
                {
                    icon = (IconKind)iconValue;
                }
                else if (string.Equals(na.MemberName, nameof(BlazorMenuAttribute.Location), StringComparison.Ordinal) &&
                         TryGetInt32(na.TypedValue, out var locValue))
                {
                    location = (MenuLocation)locValue;
                }
                else if (string.Equals(na.MemberName, nameof(BlazorMenuAttribute.Order), StringComparison.Ordinal) &&
                         TryGetInt32(na.TypedValue, out var orderValue))
                {
                    order = orderValue;
                }
            }

            menus.Add(new MenuInfo
            {
                Key = key,
                DisplayName = displayName,
                Route = route,
                Icon = icon.ToString(),
                Location = location,
                Order = order,
                DeclaringType = declaringType
            });
        }
    }

    private static void AppendAvaloniaMenus(string declaringType, IList<CustomAttributeData> attrs, List<MenuInfo> menus)
    {
        foreach (var a in attrs)
        {
            if (!string.Equals(a.AttributeType.FullName, AvaloniaMenuAttributeFullName, StringComparison.Ordinal))
                continue;

            if (a.ConstructorArguments.Count < 3)
                throw new InvalidOperationException($"Invalid [AvaloniaMenu] on '{declaringType}': expected constructor arguments (key, displayName, viewModelType).");

            var key = a.ConstructorArguments[0].Value as string;
            var displayName = a.ConstructorArguments[1].Value as string;
            var viewModelType = a.ConstructorArguments[2].Value as Type;
            var route = viewModelType?.FullName ?? viewModelType?.Name ?? string.Empty;

            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException($"Invalid [AvaloniaMenu] on '{declaringType}': 'key' is required.");
            if (string.IsNullOrWhiteSpace(displayName))
                throw new InvalidOperationException($"Invalid [AvaloniaMenu] on '{declaringType}': 'displayName' is required.");
            if (string.IsNullOrWhiteSpace(route))
                throw new InvalidOperationException($"Invalid [AvaloniaMenu] on '{declaringType}': 'viewModelType' is required.");

            var icon = IconKind.Grid;
            var location = MenuLocation.Main;
            var order = 50;

            foreach (var na in a.NamedArguments)
            {
                if (string.Equals(na.MemberName, nameof(AvaloniaMenuAttribute.Icon), StringComparison.Ordinal) &&
                    TryGetInt32(na.TypedValue, out var iconValue))
                {
                    icon = (IconKind)iconValue;
                }
                else if (string.Equals(na.MemberName, nameof(AvaloniaMenuAttribute.Location), StringComparison.Ordinal) &&
                         TryGetInt32(na.TypedValue, out var locValue))
                {
                    location = (MenuLocation)locValue;
                }
                else if (string.Equals(na.MemberName, nameof(AvaloniaMenuAttribute.Order), StringComparison.Ordinal) &&
                         TryGetInt32(na.TypedValue, out var orderValue))
                {
                    order = orderValue;
                }
            }

            menus.Add(new MenuInfo
            {
                Key = key,
                DisplayName = displayName,
                Route = route,
                Icon = icon.ToString(),
                Location = location,
                Order = order,
                DeclaringType = declaringType
            });
        }
    }

    private static bool TryGetInt32(CustomAttributeTypedArgument value, out int result)
    {
        var v = value.Value;
        switch (v)
        {
            case null:
                result = default;
                return false;
            case int i:
                result = i;
                return true;
            case short s:
                result = s;
                return true;
            case byte b:
                result = b;
                return true;
            case sbyte sb:
                result = sb;
                return true;
            case long l:
                result = unchecked((int)l);
                return true;
            case IConvertible c:
                try
                {
                    result = c.ToInt32(null);
                    return true;
                }
                catch
                {
                    result = default;
                    return false;
                }
            default:
                result = default;
                return false;
        }
    }
}

/// <summary>
/// Menu information read from assembly attributes.
/// </summary>
public sealed class MenuInfo
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public MenuLocation Location { get; set; } = MenuLocation.Main;
    public int Order { get; set; }
    public string DeclaringType { get; set; } = string.Empty;
}


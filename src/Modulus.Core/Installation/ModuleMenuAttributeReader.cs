using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Modulus.Sdk;
using Modulus.UI.Abstractions;

namespace Modulus.Core.Installation;

/// <summary>
/// Reads menu attributes from module assemblies without executing module code.
/// </summary>
public static class ModuleMenuAttributeReader
{
    private const string ModulusSdkNamespace = "Modulus.Sdk";
    private const string ModulusPackageTypeName = "ModulusPackage";

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

        var isBlazor = ModulusHostIds.Matches(hostType, ModulusHostIds.Blazor);
        var isAvalonia = ModulusHostIds.Matches(hostType, ModulusHostIds.Avalonia);
        if (!isBlazor && !isAvalonia) return Array.Empty<MenuInfo>();

        // Metadata-only: do NOT use MetadataLoadContext / reflection type loading (would require resolving referenced assemblies).
        // We parse custom attributes directly from ECMA-335 metadata via PEReader.
        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream, PEStreamOptions.LeaveOpen);
        if (!peReader.HasMetadata) return Array.Empty<MenuInfo>();

        var reader = peReader.GetMetadataReader();
        var decoder = new AttributeValueDecoder(reader);

        var menus = new List<MenuInfo>();

        foreach (var typeHandle in reader.TypeDefinitions)
        {
            var typeDef = reader.GetTypeDefinition(typeHandle);

            // Skip abstract/interface types (align with previous behavior)
            if ((typeDef.Attributes & TypeAttributes.Abstract) != 0) continue;
            if ((typeDef.Attributes & TypeAttributes.Interface) != 0) continue;

            var declaringType = GetTypeFullName(reader, typeHandle);

            // Only parse likely entry types:
            // - Prefer types derived from ModulusPackage (direct or via in-assembly base classes)
            // - Also allow types that declare the target menu attribute (to avoid false negatives when base type chain crosses assemblies)
            var hasTargetAttr = HasTargetMenuAttribute(reader, typeDef, isBlazor, isAvalonia);
            if (!hasTargetAttr && !IsDerivedFromModulusPackage(reader, typeHandle))
                continue;

            foreach (var caHandle in typeDef.GetCustomAttributes())
            {
                var ca = reader.GetCustomAttribute(caHandle);
                var attrTypeFullName = GetAttributeTypeFullName(reader, ca);
                if (attrTypeFullName == null) continue;

                if (isBlazor && string.Equals(attrTypeFullName, BlazorMenuAttributeFullName, StringComparison.Ordinal))
                {
                    menus.Add(ParseBlazorMenu(reader, decoder, ca, declaringType));
                }
                else if (isAvalonia && string.Equals(attrTypeFullName, AvaloniaMenuAttributeFullName, StringComparison.Ordinal))
                {
                    menus.Add(ParseAvaloniaMenu(reader, decoder, ca, declaringType));
                }
            }
        }

        return menus;
    }

    private static bool HasTargetMenuAttribute(MetadataReader reader, TypeDefinition typeDef, bool isBlazor, bool isAvalonia)
    {
        foreach (var caHandle in typeDef.GetCustomAttributes())
        {
            var ca = reader.GetCustomAttribute(caHandle);
            var attrTypeFullName = GetAttributeTypeFullName(reader, ca);
            if (attrTypeFullName == null) continue;

            if (isBlazor && string.Equals(attrTypeFullName, BlazorMenuAttributeFullName, StringComparison.Ordinal)) return true;
            if (isAvalonia && string.Equals(attrTypeFullName, AvaloniaMenuAttributeFullName, StringComparison.Ordinal)) return true;
        }
        return false;
    }

    private static bool IsDerivedFromModulusPackage(MetadataReader reader, TypeDefinitionHandle typeHandle)
    {
        var current = typeHandle;
        var guard = 0;

        while (!current.IsNil && guard++ < 64)
        {
            var td = reader.GetTypeDefinition(current);
            var baseHandle = td.BaseType;
            if (baseHandle.IsNil) return false;

            if (baseHandle.Kind == HandleKind.TypeReference)
            {
                var tr = reader.GetTypeReference((TypeReferenceHandle)baseHandle);
                var ns = reader.GetString(tr.Namespace);
                var name = reader.GetString(tr.Name);
                return string.Equals(ns, ModulusSdkNamespace, StringComparison.Ordinal)
                       && string.Equals(name, ModulusPackageTypeName, StringComparison.Ordinal);
            }

            if (baseHandle.Kind == HandleKind.TypeDefinition)
            {
                current = (TypeDefinitionHandle)baseHandle;
                continue;
            }

            // TypeSpecification / others - do not attempt to resolve
            return false;
        }

        return false;
    }

    private static string GetTypeFullName(MetadataReader reader, TypeDefinitionHandle handle)
    {
        var td = reader.GetTypeDefinition(handle);
        var name = reader.GetString(td.Name);
        var ns = reader.GetString(td.Namespace);

        var declaring = td.GetDeclaringType();
        if (!declaring.IsNil)
        {
            return $"{GetTypeFullName(reader, declaring)}+{name}";
        }

        return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
    }

    private static string? GetAttributeTypeFullName(MetadataReader reader, CustomAttribute attribute)
    {
        var ctor = attribute.Constructor;

        if (ctor.Kind == HandleKind.MemberReference)
        {
            var mr = reader.GetMemberReference((MemberReferenceHandle)ctor);
            return GetTypeFullName(reader, mr.Parent);
        }

        if (ctor.Kind == HandleKind.MethodDefinition)
        {
            var md = reader.GetMethodDefinition((MethodDefinitionHandle)ctor);
            return GetTypeFullName(reader, md.GetDeclaringType());
        }

        return null;
    }

    private static string? GetTypeFullName(MetadataReader reader, EntityHandle handle)
    {
        switch (handle.Kind)
        {
            case HandleKind.TypeReference:
            {
                var tr = reader.GetTypeReference((TypeReferenceHandle)handle);
                var ns = reader.GetString(tr.Namespace);
                var name = reader.GetString(tr.Name);
                return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
            }
            case HandleKind.TypeDefinition:
                return GetTypeFullName(reader, (TypeDefinitionHandle)handle);
            default:
                return null;
        }
    }

    private static MenuInfo ParseBlazorMenu(MetadataReader reader, AttributeValueDecoder decoder, CustomAttribute ca, string declaringType)
    {
        var value = ca.DecodeValue(decoder);
        if (value.FixedArguments.Length < 3)
            throw new InvalidOperationException($"Invalid [BlazorMenu] on '{declaringType}': expected constructor arguments (key, displayName, route).");

        var key = value.FixedArguments[0].Value as string;
        var displayName = value.FixedArguments[1].Value as string;
        var route = value.FixedArguments[2].Value as string;

        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException($"Invalid [BlazorMenu] on '{declaringType}': 'key' is required.");
        if (string.IsNullOrWhiteSpace(displayName))
            throw new InvalidOperationException($"Invalid [BlazorMenu] on '{declaringType}': 'displayName' is required.");
        if (string.IsNullOrWhiteSpace(route))
            throw new InvalidOperationException($"Invalid [BlazorMenu] on '{declaringType}': 'route' is required.");

        var icon = IconKind.Grid;
        var location = MenuLocation.Main;
        var order = 50;

        foreach (var na in value.NamedArguments)
        {
            var naValue = na.Value;
            if (string.Equals(na.Name, nameof(BlazorMenuAttribute.Icon), StringComparison.Ordinal) &&
                TryGetInt32(naValue, out var iconValue))
            {
                icon = (IconKind)iconValue;
            }
            else if (string.Equals(na.Name, nameof(BlazorMenuAttribute.Location), StringComparison.Ordinal) &&
                     TryGetInt32(naValue, out var locValue))
            {
                location = (MenuLocation)locValue;
            }
            else if (string.Equals(na.Name, nameof(BlazorMenuAttribute.Order), StringComparison.Ordinal) &&
                     TryGetInt32(naValue, out var orderValue))
            {
                order = orderValue;
            }
        }

        return new MenuInfo
        {
            Key = key,
            DisplayName = displayName,
            Route = route,
            Icon = icon.ToString(),
            Location = location,
            Order = order,
            DeclaringType = declaringType
        };
    }

    private static MenuInfo ParseAvaloniaMenu(MetadataReader reader, AttributeValueDecoder decoder, CustomAttribute ca, string declaringType)
    {
        var value = ca.DecodeValue(decoder);
        if (value.FixedArguments.Length < 3)
            throw new InvalidOperationException($"Invalid [AvaloniaMenu] on '{declaringType}': expected constructor arguments (key, displayName, viewModelType).");

        var key = value.FixedArguments[0].Value as string;
        var displayName = value.FixedArguments[1].Value as string;
        var route = value.FixedArguments[2].Value as string; // Type argument decoded to a normalized full name string

        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException($"Invalid [AvaloniaMenu] on '{declaringType}': 'key' is required.");
        if (string.IsNullOrWhiteSpace(displayName))
            throw new InvalidOperationException($"Invalid [AvaloniaMenu] on '{declaringType}': 'displayName' is required.");
        if (string.IsNullOrWhiteSpace(route))
            throw new InvalidOperationException($"Invalid [AvaloniaMenu] on '{declaringType}': 'viewModelType' is required.");

        var icon = IconKind.Grid;
        var location = MenuLocation.Main;
        var order = 50;

        foreach (var na in value.NamedArguments)
        {
            var naValue = na.Value;
            if (string.Equals(na.Name, nameof(AvaloniaMenuAttribute.Icon), StringComparison.Ordinal) &&
                TryGetInt32(naValue, out var iconValue))
            {
                icon = (IconKind)iconValue;
            }
            else if (string.Equals(na.Name, nameof(AvaloniaMenuAttribute.Location), StringComparison.Ordinal) &&
                     TryGetInt32(naValue, out var locValue))
            {
                location = (MenuLocation)locValue;
            }
            else if (string.Equals(na.Name, nameof(AvaloniaMenuAttribute.Order), StringComparison.Ordinal) &&
                     TryGetInt32(naValue, out var orderValue))
            {
                order = orderValue;
            }
        }

        return new MenuInfo
        {
            Key = key,
            DisplayName = displayName,
            Route = route,
            Icon = icon.ToString(),
            Location = location,
            Order = order,
            DeclaringType = declaringType
        };
    }

    private static bool TryGetInt32(object? value, out int result)
    {
        switch (value)
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

internal sealed class AttributeValueDecoder : ICustomAttributeTypeProvider<object?>
{
    private readonly MetadataReader _reader;
    private const string SystemTypeFullName = "System.Type";

    public AttributeValueDecoder(MetadataReader reader) => _reader = reader;

    public object? GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode;

    // IMPORTANT:
    // Custom attribute encoding treats System.Type specially (it is serialized as a string).
    // We represent "System.Type" as its full name string so IsSystemType can recognize it
    // even when it comes from TypeReference/TypeDefinition handles.
    public object? GetSystemType() => SystemTypeFullName;

    public object? GetSZArrayType(object? elementType) => elementType;

    public object? GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
        => GetTypeFullName(reader, handle);

    public object? GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
        => GetTypeFullName(reader, handle);

    public object? GetTypeFromSerializedName(string name) => NormalizeSerializedTypeName(name);

    public PrimitiveTypeCode GetUnderlyingEnumType(object? type) => PrimitiveTypeCode.Int32;

    public bool IsSystemType(object? type)
        => type is string s && string.Equals(s, SystemTypeFullName, StringComparison.Ordinal);

    private static string? GetTypeFullName(MetadataReader reader, EntityHandle handle)
    {
        switch (handle.Kind)
        {
            case HandleKind.TypeReference:
            {
                var tr = reader.GetTypeReference((TypeReferenceHandle)handle);
                var ns = reader.GetString(tr.Namespace);
                var name = reader.GetString(tr.Name);
                return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
            }
            case HandleKind.TypeDefinition:
            {
                var td = reader.GetTypeDefinition((TypeDefinitionHandle)handle);
                var name = reader.GetString(td.Name);
                var ns = reader.GetString(td.Namespace);
                var declaring = td.GetDeclaringType();
                if (!declaring.IsNil)
                {
                    var parent = GetTypeFullName(reader, declaring);
                    return parent == null ? name : $"{parent}+{name}";
                }
                return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
            }
            default:
                return null;
        }
    }

    private static string NormalizeSerializedTypeName(string serialized)
    {
        // Serialized 'Type' arguments are encoded as assembly-qualified names.
        // We only need the type name, and we MUST avoid resolving any referenced assemblies.
        if (string.IsNullOrWhiteSpace(serialized)) return string.Empty;

        // e.g. "Namespace.Type, AssemblyName, Version=..., Culture=..., PublicKeyToken=..."
        var comma = serialized.IndexOf(',');
        var typeName = comma >= 0 ? serialized[..comma] : serialized;
        return typeName.Trim();
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


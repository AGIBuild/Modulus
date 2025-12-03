using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Modulus.Sdk;
using Modulus.UI.Abstractions;

namespace Modulus.Core.Runtime;

/// <summary>
/// Scans module types for declarative attributes and processes them.
/// </summary>
public class ModuleMetadataScanner
{
    private readonly ILogger _logger;

    public ModuleMetadataScanner(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Scans a module type and extracts core metadata from ModuleAttribute.
    /// </summary>
    public ModuleMetadata? ScanCoreModule(Type moduleType)
    {
        var attr = moduleType.GetCustomAttribute<ModuleAttribute>();
        if (attr == null) return null;

        // Get version from assembly
        var assembly = moduleType.Assembly;
        var version = assembly.GetName().Version?.ToString() ?? "1.0.0";
        var assemblyInfo = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (assemblyInfo != null)
        {
            version = assemblyInfo.InformationalVersion.Split('+')[0]; // Remove commit hash if present
        }

        return new ModuleMetadata
        {
            Id = attr.Id,
            DisplayName = attr.DisplayName,
            Description = attr.Description,
            Version = version,
            Author = attr.Author,
            ModuleType = moduleType
        };
    }

    /// <summary>
    /// Scans Avalonia UI module for menu declarations.
    /// </summary>
    public List<ModuleMenuMetadata> ScanAvaloniaMenus(Type moduleType)
    {
        var menus = new List<ModuleMenuMetadata>();
        var attrs = moduleType.GetCustomAttributes<AvaloniaMenuAttribute>();

        foreach (var attr in attrs)
        {
            menus.Add(new ModuleMenuMetadata
            {
                Id = attr.ViewModelType.Name,
                DisplayName = attr.DisplayName,
                Icon = attr.Icon,
                ViewModelType = attr.ViewModelType.FullName,
                Location = attr.Location == "Bottom" ? MenuLocation.Bottom : MenuLocation.Main,
                Order = attr.Order
            });
        }

        return menus;
    }

    /// <summary>
    /// Scans Blazor UI module for menu declarations.
    /// </summary>
    public List<ModuleMenuMetadata> ScanBlazorMenus(Type moduleType)
    {
        var menus = new List<ModuleMenuMetadata>();
        var attrs = moduleType.GetCustomAttributes<BlazorMenuAttribute>();

        foreach (var attr in attrs)
        {
            menus.Add(new ModuleMenuMetadata
            {
                Id = attr.Route,
                DisplayName = attr.DisplayName,
                Icon = attr.Icon,
                Route = attr.Route,
                Location = attr.Location == "Bottom" ? MenuLocation.Bottom : MenuLocation.Main,
                Order = attr.Order
            });
        }

        return menus;
    }
}

/// <summary>
/// Metadata extracted from module attributes.
/// </summary>
public class ModuleMetadata
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string Author { get; set; } = string.Empty;
    public Type? ModuleType { get; set; }
}

public class ModuleMenuMetadata
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Icon { get; set; } = "circle";
    public string? ViewModelType { get; set; }
    public string? Route { get; set; }
    public MenuLocation Location { get; set; } = MenuLocation.Main;
    public int Order { get; set; } = 50;
}

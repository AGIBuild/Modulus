using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Styling;

namespace Modulus.UI.Avalonia.Infrastructure;

/// <summary>
/// Describes a component that can provide Avalonia resource URIs to be merged into an application.
/// </summary>
public interface IAvaloniaResourceProvider
{
    /// <summary>
    /// Returns style resources (control templates, global styles, etc.) that must be merged into <see cref="Application.Styles"/>.
    /// </summary>
    IEnumerable<Uri> GetStyleUris();

    /// <summary>
    /// Returns theme-specific resource dictionaries that should be merged into <see cref="Application.Resources.ThemeDictionaries"/>.
    /// </summary>
    IEnumerable<AvaloniaThemeResourceDescriptor> GetThemeResources();
}

/// <summary>
/// Represents a theme dictionary registration.
/// </summary>
/// <param name="Theme">The theme variant (e.g. <see cref="ThemeVariant.Light"/>).</param>
/// <param name="Source">Resource dictionary source URI.</param>
public sealed record AvaloniaThemeResourceDescriptor(ThemeVariant Theme, Uri Source);


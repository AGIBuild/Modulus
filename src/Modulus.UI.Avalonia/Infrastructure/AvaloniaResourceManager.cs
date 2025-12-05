using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

namespace Modulus.UI.Avalonia.Infrastructure;

/// <summary>
/// Central registry that aggregates resource providers and injects their dictionaries/styles into the current application.
/// </summary>
public static class AvaloniaResourceManager
{
    private static readonly object SyncRoot = new();
    private static readonly HashSet<Uri> StyleUris = new();
    private static readonly List<AvaloniaThemeResourceDescriptor> ThemeResources = new();

    static AvaloniaResourceManager()
    {
        RegisterProvider(new ModulusLibraryResourceProvider());
    }

    /// <summary>
    /// Registers a resource provider so its resources can be injected later.
    /// </summary>
    public static void RegisterProvider(IAvaloniaResourceProvider provider)
    {
        if (provider == null)
        {
            throw new ArgumentNullException(nameof(provider));
        }

        lock (SyncRoot)
        {
            foreach (var uri in provider.GetStyleUris() ?? Array.Empty<Uri>())
            {
                if (uri != null)
                {
                    StyleUris.Add(uri);
                }
            }

            foreach (var descriptor in provider.GetThemeResources() ?? Array.Empty<AvaloniaThemeResourceDescriptor>())
            {
                if (descriptor?.Source == null || descriptor.Theme is null)
                {
                    continue;
                }

                ThemeResources.Add(descriptor);
            }
        }
    }

    /// <summary>
    /// Ensures all registered resources are merged into the supplied application.
    /// </summary>
    public static void EnsureResources(Application? application)
    {
        if (application == null)
        {
            return;
        }

        lock (SyncRoot)
        {
            EnsureStyles(application);
            EnsureThemeDictionaries(application);
        }
    }

    private static void EnsureStyles(Application application)
    {
        foreach (var uri in StyleUris)
        {
            if (IsStyleLoaded(application.Styles, uri))
            {
                continue;
            }

            application.Styles.Add(new StyleInclude(uri)
            {
                Source = uri
            });
        }
    }

    private static bool IsStyleLoaded(Styles styles, Uri uri)
    {
        return styles.OfType<StyleInclude>()
                     .Any(include => UriEquals(include.Source, uri));
    }

    private static void EnsureThemeDictionaries(Application application)
    {
        foreach (var descriptor in ThemeResources)
        {
            if (application.Resources.ThemeDictionaries.ContainsKey(descriptor.Theme))
            {
                continue;
            }

            application.Resources.ThemeDictionaries[descriptor.Theme] = new ResourceInclude(descriptor.Source)
            {
                Source = descriptor.Source
            };
        }
    }

    private static bool UriEquals(Uri? left, Uri right)
    {
        return left != null &&
               Uri.Compare(left, right, UriComponents.AbsoluteUri, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase) == 0;
    }
}


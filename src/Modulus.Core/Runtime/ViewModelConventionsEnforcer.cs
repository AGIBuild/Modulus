using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Modulus.UI.Abstractions;

namespace Modulus.Core.Runtime;

internal static class ViewModelConventionsEnforcer
{
    public static void Enforce(string moduleId, IReadOnlyList<Assembly> moduleAssemblies, ILogger logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleId);
        ArgumentNullException.ThrowIfNull(moduleAssemblies);
        ArgumentNullException.ThrowIfNull(logger);

        foreach (var asm in moduleAssemblies)
        {
            foreach (var t in SafeGetTypes(asm, logger))
            {
                if (!t.IsClass || t.IsAbstract) continue;
                if (t.IsGenericTypeDefinition) continue;
                if (t.GetCustomAttribute<CompilerGeneratedAttribute>() != null) continue;

                var endsWithViewModel = t.Name.EndsWith("ViewModel", StringComparison.Ordinal);
                var implementsIViewModel = typeof(IViewModel).IsAssignableFrom(t);
                var inheritsViewModelBase = typeof(ViewModelBase).IsAssignableFrom(t);

                // Convention definition:
                // - ViewModels MUST implement IViewModel
                // - All IViewModel implementations MUST inherit ViewModelBase
                // - Any type named *ViewModel MUST implement IViewModel (prevents "fake ViewModel" types)

                if (endsWithViewModel && !implementsIViewModel)
                {
                    logger.LogError(
                        "Module {ModuleId} violates ViewModel convention. Type {Type} ends with 'ViewModel' but does not implement IViewModel.",
                        moduleId,
                        t.FullName);

                    throw new ViewModelConventionViolationException(
                        $"Module '{moduleId}' has type '{t.FullName}' named '*ViewModel' that MUST implement IViewModel (and therefore inherit ViewModelBase).");
                }

                if (implementsIViewModel && !inheritsViewModelBase)
                {
                    logger.LogError(
                        "Module {ModuleId} violates ViewModel convention. Type {Type} implements IViewModel but does not inherit ViewModelBase.",
                        moduleId,
                        t.FullName);

                    throw new ViewModelConventionViolationException(
                        $"Module '{moduleId}' has IViewModel type '{t.FullName}' that MUST inherit ViewModelBase.");
                }
            }
        }
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly asm, ILogger logger)
    {
        try
        {
            return asm.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            logger.LogError(ex, "Failed to enumerate types for assembly {Assembly}.", asm.FullName);
            return ex.Types.Where(t => t != null)!;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to enumerate types for assembly {Assembly}.", asm.FullName);
            return Array.Empty<Type>();
        }
    }
}



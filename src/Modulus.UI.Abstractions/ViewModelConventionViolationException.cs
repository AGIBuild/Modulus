using System;

namespace Modulus.UI.Abstractions;

/// <summary>
/// Thrown when a module violates the Modulus ViewModel convention that all ViewModels MUST inherit <see cref="ViewModelBase"/>.
/// </summary>
public sealed class ViewModelConventionViolationException : InvalidOperationException
{
    public ViewModelConventionViolationException(string message) : base(message)
    {
    }

    public static ViewModelConventionViolationException ForType(string role, Type actualType)
    {
        if (string.IsNullOrWhiteSpace(role)) role = "ViewModel";
        ArgumentNullException.ThrowIfNull(actualType);

        return new ViewModelConventionViolationException(
            $"{role} MUST inherit ViewModelBase. Actual: '{actualType.FullName}'.");
    }
}



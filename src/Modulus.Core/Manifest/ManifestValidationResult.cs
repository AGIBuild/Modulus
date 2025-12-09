using System.Collections.Generic;

namespace Modulus.Core.Manifest;

/// <summary>
/// Result of manifest validation containing success status and any error messages.
/// </summary>
public sealed class ManifestValidationResult
{
    public bool IsValid => Errors.Count == 0;

    public IReadOnlyList<string> Errors { get; }

    private ManifestValidationResult(IReadOnlyList<string> errors)
    {
        Errors = errors;
    }

    public static ManifestValidationResult Success() => new(Array.Empty<string>());

    public static ManifestValidationResult Failure(IEnumerable<string> errors) =>
        new(new List<string>(errors));
}


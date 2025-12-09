using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Modulus.Core.Installation;
using Modulus.Core.Manifest;

namespace Modulus.Core.Runtime;

/// <summary>
/// Result of loading module detail content.
/// </summary>
public sealed class ModuleDetailResult
{
    public bool Success { get; }
    public string Content { get; }
    public Exception? Error { get; }
    public bool WasCancelled { get; }
    public bool WasTimedOut { get; }

    private ModuleDetailResult(bool success, string content, Exception? error = null, bool wasCancelled = false, bool wasTimedOut = false)
    {
        Success = success;
        Content = content;
        Error = error;
        WasCancelled = wasCancelled;
        WasTimedOut = wasTimedOut;
    }

    public static ModuleDetailResult Succeeded(string content) => new(true, content);
    public static ModuleDetailResult Failed(Exception error) => new(false, $"Error loading details: {error.Message}", error);
    public static ModuleDetailResult Cancelled() => new(false, "Loading cancelled.", wasCancelled: true);
    public static ModuleDetailResult TimedOut() => new(false, "Loading timed out.", wasTimedOut: true);
    public static ModuleDetailResult NoContent() => new(true, "No description provided.");
}

/// <summary>
/// Service for loading module detail content (README or manifest description).
/// Supports cancellation and timeout.
/// </summary>
public sealed class ModuleDetailLoader
{
    private readonly ILogger<ModuleDetailLoader> _logger;
    private readonly TimeSpan _defaultTimeout;

    public ModuleDetailLoader(ILogger<ModuleDetailLoader> logger, TimeSpan? defaultTimeout = null)
    {
        _logger = logger;
        _defaultTimeout = defaultTimeout ?? TimeSpan.FromSeconds(10);
    }

    /// <summary>
    /// Loads module detail content asynchronously with cancellation and timeout support.
    /// </summary>
    /// <param name="modulePath">Path to extension.vsixmanifest or module directory.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <param name="timeout">Optional timeout (defaults to 10 seconds).</param>
    /// <returns>Result containing content or error information.</returns>
    public async Task<ModuleDetailResult> LoadDetailAsync(
        string modulePath,
        CancellationToken cancellationToken = default,
        TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? _defaultTimeout;

        using var timeoutCts = new CancellationTokenSource(effectiveTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            return await LoadDetailCoreAsync(modulePath, linkedCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Module detail load timed out after {Timeout}ms for path: {Path}", effectiveTimeout.TotalMilliseconds, modulePath);
            return ModuleDetailResult.TimedOut();
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Module detail load cancelled for path: {Path}", modulePath);
            return ModuleDetailResult.Cancelled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading module detail from: {Path}", modulePath);
            return ModuleDetailResult.Failed(ex);
        }
    }

    private async Task<ModuleDetailResult> LoadDetailCoreAsync(string modulePath, CancellationToken cancellationToken)
    {
        var manifestPath = modulePath;
        var dir = modulePath;

        // Normalize path
        if (File.Exists(modulePath))
        {
            manifestPath = Path.GetFullPath(modulePath);
            dir = Path.GetDirectoryName(manifestPath);
        }
        else if (Directory.Exists(modulePath))
        {
            dir = Path.GetFullPath(modulePath);
            manifestPath = Path.Combine(dir, SystemModuleInstaller.VsixManifestFileName);
        }
        else
        {
            return ModuleDetailResult.Failed(new FileNotFoundException("Module path not found.", modulePath));
        }

        // 1. Try README.md
        if (dir != null)
        {
            var readmePath = Path.Combine(dir, "README.md");
            if (File.Exists(readmePath))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var content = await File.ReadAllTextAsync(readmePath, cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("Loaded README.md for module at: {Path}", dir);
                return ModuleDetailResult.Succeeded(content);
            }
        }

        // 2. Fallback to Manifest Description
        if (File.Exists(manifestPath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var manifest = await VsixManifestReader.ReadFromFileAsync(manifestPath).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(manifest?.Metadata.Description))
            {
                _logger.LogDebug("Loaded description from manifest for module at: {Path}", dir);
                return ModuleDetailResult.Succeeded(manifest.Metadata.Description);
            }
        }

        return ModuleDetailResult.NoContent();
    }
}

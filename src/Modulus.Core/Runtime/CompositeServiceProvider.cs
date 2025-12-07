using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Modulus.Core.Runtime;

internal sealed class CompositeServiceProvider : IServiceProvider, IServiceProviderIsService, IAsyncDisposable, IDisposable
{
    private readonly IServiceProvider _primary;
    private readonly IServiceProvider _fallback;
    private readonly IServiceProviderIsService? _primaryIsService;
    private readonly IServiceProviderIsService? _fallbackIsService;

    public CompositeServiceProvider(IServiceProvider primary, IServiceProvider fallback)
    {
        _primary = primary;
        _fallback = fallback;
        _primaryIsService = primary.GetService<IServiceProviderIsService>();
        _fallbackIsService = fallback.GetService<IServiceProviderIsService>();
    }

    public object? GetService(Type serviceType)
    {
        // Return self for IServiceProviderIsService queries
        if (serviceType == typeof(IServiceProviderIsService))
        {
            return this;
        }
        
        var service = _primary.GetService(serviceType);
        return service ?? _fallback.GetService(serviceType);
    }

    public bool IsService(Type serviceType)
    {
        if (_primaryIsService?.IsService(serviceType) == true) return true;
        if (_fallbackIsService?.IsService(serviceType) == true) return true;

        // Fallback: probe providers directly (covers default DI where IServiceProviderIsService is not exposed)
        return _primary.GetService(serviceType) != null
            || _fallback.GetService(serviceType) != null;
    }

    public ValueTask DisposeAsync()
    {
        if (_primary is IAsyncDisposable asyncDisposable)
        {
            return asyncDisposable.DisposeAsync();
        }

        (_primary as IDisposable)?.Dispose();
        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        if (_primary is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}


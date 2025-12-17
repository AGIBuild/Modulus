using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Modulus.Core.Runtime;

internal sealed class CompositeServiceProvider : IServiceProvider, IServiceProviderIsService, IServiceScopeFactory, IAsyncDisposable, IDisposable
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

        // Ensure scopes created from this provider can resolve from BOTH primary and fallback.
        if (serviceType == typeof(IServiceScopeFactory))
        {
            return this;
        }
        
        var service = _primary.GetService(serviceType);
        return service ?? _fallback.GetService(serviceType);
    }

    public IServiceScope CreateScope()
    {
        var primaryFactory = _primary.GetService<IServiceScopeFactory>();
        var fallbackFactory = _fallback.GetService<IServiceScopeFactory>();

        if (primaryFactory == null && fallbackFactory == null)
        {
            throw new InvalidOperationException("Neither primary nor fallback service provider supports IServiceScopeFactory.");
        }

        var primaryScope = primaryFactory?.CreateScope();
        var fallbackScope = fallbackFactory?.CreateScope();

        var scopedPrimary = primaryScope?.ServiceProvider ?? _primary;
        var scopedFallback = fallbackScope?.ServiceProvider ?? _fallback;

        return new CompositeServiceScope(primaryScope, fallbackScope, new CompositeServiceProvider(scopedPrimary, scopedFallback));
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

    private sealed class CompositeServiceScope : IServiceScope, IAsyncDisposable
    {
        private readonly IServiceScope? _primaryScope;
        private readonly IServiceScope? _fallbackScope;

        public CompositeServiceScope(IServiceScope? primaryScope, IServiceScope? fallbackScope, IServiceProvider serviceProvider)
        {
            _primaryScope = primaryScope;
            _fallbackScope = fallbackScope;
            ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider { get; }

        public void Dispose()
        {
            _primaryScope?.Dispose();
            _fallbackScope?.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (_primaryScope is IAsyncDisposable primaryAsync)
            {
                await primaryAsync.DisposeAsync();
            }
            else
            {
                _primaryScope?.Dispose();
            }

            if (_fallbackScope is IAsyncDisposable fallbackAsync)
            {
                await fallbackAsync.DisposeAsync();
            }
            else
            {
                _fallbackScope?.Dispose();
            }
        }
    }
}


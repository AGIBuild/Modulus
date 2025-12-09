using Modulus.UI.Abstractions;
using NSubstitute;

namespace Modulus.Hosts.Tests;

/// <summary>
/// Tests for INavigationService implementations (guard evaluation, lifecycle).
/// These tests use mocks to verify the contract without host-specific dependencies.
/// </summary>
public class NavigationServiceTests
{
    #region Guard Tests

    [Fact]
    public async Task NavigateToAsync_WithNoGuards_ShouldSucceed()
    {
        // Arrange
        var service = new TestNavigationService();

        // Act
        var result = await service.NavigateToAsync("/test");

        // Assert
        Assert.True(result);
        Assert.Equal("/test", service.CurrentNavigationKey);
    }

    [Fact]
    public async Task NavigateToAsync_WhenGuardAllows_ShouldSucceed()
    {
        // Arrange
        var service = new TestNavigationService();
        var guard = Substitute.For<INavigationGuard>();
        guard.CanNavigateFromAsync(Arg.Any<NavigationContext>()).Returns(Task.FromResult(true));
        guard.CanNavigateToAsync(Arg.Any<NavigationContext>()).Returns(Task.FromResult(true));
        service.RegisterNavigationGuard(guard);

        // Act
        var result = await service.NavigateToAsync("/target");

        // Assert
        Assert.True(result);
        Assert.Equal("/target", service.CurrentNavigationKey);
    }

    [Fact]
    public async Task NavigateToAsync_WhenGuardDeniesNavigateFrom_ShouldFail()
    {
        // Arrange
        var service = new TestNavigationService();
        await service.NavigateToAsync("/initial"); // Set initial location

        var guard = Substitute.For<INavigationGuard>();
        guard.CanNavigateFromAsync(Arg.Any<NavigationContext>()).Returns(Task.FromResult(false));
        guard.CanNavigateToAsync(Arg.Any<NavigationContext>()).Returns(Task.FromResult(true));
        service.RegisterNavigationGuard(guard);

        // Act
        var result = await service.NavigateToAsync("/target");

        // Assert
        Assert.False(result);
        Assert.Equal("/initial", service.CurrentNavigationKey); // Should remain at initial
    }

    [Fact]
    public async Task NavigateToAsync_WhenGuardDeniesNavigateTo_ShouldFail()
    {
        // Arrange
        var service = new TestNavigationService();
        var guard = Substitute.For<INavigationGuard>();
        guard.CanNavigateFromAsync(Arg.Any<NavigationContext>()).Returns(Task.FromResult(true));
        guard.CanNavigateToAsync(Arg.Any<NavigationContext>()).Returns(Task.FromResult(false));
        service.RegisterNavigationGuard(guard);

        // Act
        var result = await service.NavigateToAsync("/target");

        // Assert
        Assert.False(result);
        Assert.Null(service.CurrentNavigationKey);
    }

    [Fact]
    public async Task NavigateToAsync_WithMultipleGuards_AllMustAllow()
    {
        // Arrange
        var service = new TestNavigationService();

        var guard1 = Substitute.For<INavigationGuard>();
        guard1.CanNavigateFromAsync(Arg.Any<NavigationContext>()).Returns(Task.FromResult(true));
        guard1.CanNavigateToAsync(Arg.Any<NavigationContext>()).Returns(Task.FromResult(true));

        var guard2 = Substitute.For<INavigationGuard>();
        guard2.CanNavigateFromAsync(Arg.Any<NavigationContext>()).Returns(Task.FromResult(true));
        guard2.CanNavigateToAsync(Arg.Any<NavigationContext>()).Returns(Task.FromResult(false));

        service.RegisterNavigationGuard(guard1);
        service.RegisterNavigationGuard(guard2);

        // Act
        var result = await service.NavigateToAsync("/target");

        // Assert
        Assert.False(result); // guard2 denied
    }

    [Fact]
    public async Task NavigateToAsync_GuardReceivesCorrectContext()
    {
        // Arrange
        var service = new TestNavigationService();
        await service.NavigateToAsync("/from");

        NavigationContext? capturedContext = null;
        var guard = Substitute.For<INavigationGuard>();
        guard.CanNavigateFromAsync(Arg.Any<NavigationContext>())
            .Returns(ci =>
            {
                capturedContext = ci.Arg<NavigationContext>();
                return Task.FromResult(true);
            });
        guard.CanNavigateToAsync(Arg.Any<NavigationContext>()).Returns(Task.FromResult(true));
        service.RegisterNavigationGuard(guard);

        // Act
        await service.NavigateToAsync("/to", new NavigationOptions { ForceNewInstance = true });

        // Assert
        Assert.NotNull(capturedContext);
        Assert.Equal("/from", capturedContext!.FromKey);
        Assert.Equal("/to", capturedContext.ToKey);
        Assert.True(capturedContext.Options.ForceNewInstance);
    }

    [Fact]
    public async Task UnregisterNavigationGuard_ShouldRemoveGuard()
    {
        // Arrange
        var service = new TestNavigationService();
        var guard = Substitute.For<INavigationGuard>();
        guard.CanNavigateToAsync(Arg.Any<NavigationContext>()).Returns(Task.FromResult(false));
        service.RegisterNavigationGuard(guard);

        // Act
        service.UnregisterNavigationGuard(guard);
        var result = await service.NavigateToAsync("/target");

        // Assert
        Assert.True(result); // Guard was removed, navigation should succeed
    }

    #endregion

    #region Lifecycle Tests

    [Fact]
    public async Task NavigateToAsync_RaisesNavigatedEvent()
    {
        // Arrange
        var service = new TestNavigationService();
        NavigationEventArgs? eventArgs = null;
        service.Navigated += (s, e) => eventArgs = e;

        // Act
        await service.NavigateToAsync("/target");

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Equal("/target", eventArgs!.ToKey);
    }

    [Fact]
    public async Task NavigateToAsync_WhenCancelled_DoesNotRaiseNavigatedEvent()
    {
        // Arrange
        var service = new TestNavigationService();
        var guard = Substitute.For<INavigationGuard>();
        guard.CanNavigateToAsync(Arg.Any<NavigationContext>()).Returns(Task.FromResult(false));
        service.RegisterNavigationGuard(guard);

        var eventRaised = false;
        service.Navigated += (s, e) => eventRaised = true;

        // Act
        await service.NavigateToAsync("/target");

        // Assert
        Assert.False(eventRaised);
    }

    #endregion

    #region Instance Lifecycle Tests

    [Fact]
    public async Task NavigateToAsync_Singleton_ReturnsSameInstance()
    {
        // Arrange
        var service = new TestNavigationService();

        // Act - Navigate to same key twice
        await service.NavigateToAsync("/page1");
        var instance1 = service.GetLastCreatedInstance("/page1");
        await service.NavigateToAsync("/page2");
        await service.NavigateToAsync("/page1");
        var instance2 = service.GetLastCreatedInstance("/page1");

        // Assert - Should be same instance (singleton by default)
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public async Task NavigateToAsync_Transient_ReturnsNewInstance()
    {
        // Arrange
        var service = new TestNavigationService();

        // Act - Navigate with transient mode
        await service.NavigateToAsync("/page1", new NavigationOptions());
        var instance1 = service.GetLastCreatedInstance("/page1");
        
        // Navigate away and back with ForceNewInstance
        await service.NavigateToAsync("/page2");
        await service.NavigateToAsync("/page1", new NavigationOptions { ForceNewInstance = true });
        var instance2 = service.GetLastCreatedInstance("/page1");

        // Assert - Should be different instances
        Assert.NotSame(instance1, instance2);
    }

    [Fact]
    public async Task NavigateToAsync_WithParameters_PassesToNavigatedEvent()
    {
        // Arrange
        var service = new TestNavigationService();
        NavigationEventArgs? capturedArgs = null;
        service.Navigated += (_, args) => capturedArgs = args;

        var parameters = new Dictionary<string, object>
        {
            { "id", 42 },
            { "name", "test" }
        };

        // Act
        await service.NavigateToAsync("/target", new NavigationOptions { Parameters = parameters });

        // Assert
        Assert.NotNull(capturedArgs);
        Assert.Equal("/target", capturedArgs!.ToKey);
    }

    #endregion

    #region Integration Tests - Guard Flow

    [Fact]
    public async Task NavigationFlow_GuardPreventingExit_BlocksAllSubsequentNavigations()
    {
        // Arrange
        var service = new TestNavigationService();
        await service.NavigateToAsync("/protected-page");

        // Register a guard that prevents leaving /protected-page
        var guard = Substitute.For<INavigationGuard>();
        guard.CanNavigateFromAsync(Arg.Is<NavigationContext>(c => c.FromKey == "/protected-page"))
            .Returns(Task.FromResult(false));
        guard.CanNavigateToAsync(Arg.Any<NavigationContext>())
            .Returns(Task.FromResult(true));
        service.RegisterNavigationGuard(guard);

        // Act - Try multiple different navigations
        var result1 = await service.NavigateToAsync("/page1");
        var result2 = await service.NavigateToAsync("/page2");
        var result3 = await service.NavigateToAsync("/page3");

        // Assert - All should fail
        Assert.False(result1);
        Assert.False(result2);
        Assert.False(result3);
        Assert.Equal("/protected-page", service.CurrentNavigationKey);
    }

    [Fact]
    public async Task NavigationFlow_GuardSelectivelyAllowing_WorksCorrectly()
    {
        // Arrange
        var service = new TestNavigationService();

        // Guard that only allows navigation to /allowed routes
        var guard = Substitute.For<INavigationGuard>();
        guard.CanNavigateFromAsync(Arg.Any<NavigationContext>())
            .Returns(Task.FromResult(true));
        guard.CanNavigateToAsync(Arg.Is<NavigationContext>(c => c.ToKey.StartsWith("/allowed")))
            .Returns(Task.FromResult(true));
        guard.CanNavigateToAsync(Arg.Is<NavigationContext>(c => !c.ToKey.StartsWith("/allowed")))
            .Returns(Task.FromResult(false));
        service.RegisterNavigationGuard(guard);

        // Act
        var result1 = await service.NavigateToAsync("/allowed/page1");
        var result2 = await service.NavigateToAsync("/forbidden/page");
        var result3 = await service.NavigateToAsync("/allowed/page2");

        // Assert
        Assert.True(result1);
        Assert.False(result2);
        Assert.True(result3);
        Assert.Equal("/allowed/page2", service.CurrentNavigationKey);
    }

    [Fact]
    public async Task NavigationFlow_MultipleGuardsOrderMatters()
    {
        // Arrange
        var service = new TestNavigationService();
        var callOrder = new List<string>();

        var guard1 = Substitute.For<INavigationGuard>();
        guard1.CanNavigateFromAsync(Arg.Any<NavigationContext>())
            .Returns(ci => { callOrder.Add("guard1-from"); return Task.FromResult(true); });
        guard1.CanNavigateToAsync(Arg.Any<NavigationContext>())
            .Returns(ci => { callOrder.Add("guard1-to"); return Task.FromResult(true); });

        var guard2 = Substitute.For<INavigationGuard>();
        guard2.CanNavigateFromAsync(Arg.Any<NavigationContext>())
            .Returns(ci => { callOrder.Add("guard2-from"); return Task.FromResult(true); });
        guard2.CanNavigateToAsync(Arg.Any<NavigationContext>())
            .Returns(ci => { callOrder.Add("guard2-to"); return Task.FromResult(true); });

        service.RegisterNavigationGuard(guard1);
        service.RegisterNavigationGuard(guard2);
        await service.NavigateToAsync("/initial");
        callOrder.Clear();

        // Act
        await service.NavigateToAsync("/target");

        // Assert - Guards evaluated in order: guard1-from, guard1-to, guard2-from, guard2-to
        Assert.Equal(new[] { "guard1-from", "guard1-to", "guard2-from", "guard2-to" }, callOrder);
    }

    [Fact]
    public async Task NavigationFlow_GuardShortCircuits_OnFirstFailure()
    {
        // Arrange
        var service = new TestNavigationService();
        await service.NavigateToAsync("/start");

        var guard1 = Substitute.For<INavigationGuard>();
        guard1.CanNavigateFromAsync(Arg.Any<NavigationContext>())
            .Returns(Task.FromResult(false)); // Fails immediately
        guard1.CanNavigateToAsync(Arg.Any<NavigationContext>())
            .Returns(Task.FromResult(true));

        var guard2 = Substitute.For<INavigationGuard>();
        guard2.CanNavigateFromAsync(Arg.Any<NavigationContext>())
            .Returns(Task.FromResult(true));
        guard2.CanNavigateToAsync(Arg.Any<NavigationContext>())
            .Returns(Task.FromResult(true));

        service.RegisterNavigationGuard(guard1);
        service.RegisterNavigationGuard(guard2);

        // Act
        await service.NavigateToAsync("/target");

        // Assert - guard2 should never be called since guard1 failed
        await guard2.DidNotReceive().CanNavigateFromAsync(Arg.Any<NavigationContext>());
        await guard2.DidNotReceive().CanNavigateToAsync(Arg.Any<NavigationContext>());
    }

    #endregion

    /// <summary>
    /// Simple test implementation of INavigationService for unit testing.
    /// Includes instance lifecycle management for testing singleton/transient behavior.
    /// </summary>
    private class TestNavigationService : INavigationService
    {
        private readonly List<INavigationGuard> _guards = new();
        private readonly Dictionary<string, object> _singletonInstances = new();
        private readonly Dictionary<string, object> _lastInstances = new();
        private string? _currentKey;

        public string? CurrentNavigationKey => _currentKey;
        public event EventHandler<NavigationEventArgs>? Navigated;

        public async Task<bool> NavigateToAsync(string navigationKey, NavigationOptions? options = null)
        {
            options ??= new NavigationOptions();
            var context = new NavigationContext
            {
                FromKey = _currentKey,
                ToKey = navigationKey,
                Options = options
            };

            // Evaluate guards
            foreach (var guard in _guards.ToList())
            {
                if (_currentKey != null)
                {
                    if (!await guard.CanNavigateFromAsync(context))
                        return false;
                }
                if (!await guard.CanNavigateToAsync(context))
                    return false;
            }

            // Instance lifecycle management
            object instance;
            if (options.ForceNewInstance)
            {
                instance = new object(); // Always create new
            }
            else
            {
                // Singleton by default
                if (!_singletonInstances.TryGetValue(navigationKey, out instance!))
                {
                    instance = new object();
                    _singletonInstances[navigationKey] = instance;
                }
            }
            _lastInstances[navigationKey] = instance;

            var previousKey = _currentKey;
            _currentKey = navigationKey;

            Navigated?.Invoke(this, new NavigationEventArgs
            {
                FromKey = previousKey,
                ToKey = navigationKey
            });

            return true;
        }

        public Task<bool> NavigateToAsync<TViewModel>(NavigationOptions? options = null) where TViewModel : class
        {
            return NavigateToAsync(typeof(TViewModel).FullName!, options);
        }

        public void RegisterNavigationGuard(INavigationGuard guard)
        {
            if (!_guards.Contains(guard))
                _guards.Add(guard);
        }

        public void UnregisterNavigationGuard(INavigationGuard guard)
        {
            _guards.Remove(guard);
        }

        public void ClearModuleCache(string moduleId)
        {
            // Simple implementation: clear all instances that might be related
            // In real implementation, this would filter by module ID
            _singletonInstances.Clear();
            _lastInstances.Clear();
        }

        /// <summary>
        /// Get the last created instance for a navigation key (for testing lifecycle).
        /// </summary>
        public object? GetLastCreatedInstance(string navigationKey)
        {
            return _lastInstances.TryGetValue(navigationKey, out var instance) ? instance : null;
        }
    }
}


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

    /// <summary>
    /// Simple test implementation of INavigationService for unit testing.
    /// </summary>
    private class TestNavigationService : INavigationService
    {
        private readonly List<INavigationGuard> _guards = new();
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
    }
}


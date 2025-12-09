using Modulus.Core.Runtime;
using Modulus.Sdk;

namespace Modulus.Core.Tests;

/// <summary>
/// Tests for module state transitions and diagnostics.
/// </summary>
public class ModuleStateDiagnosticsTests
{
    private RuntimeModule CreateTestModule(string id = "test-module")
    {
        var descriptor = new ModuleDescriptor(id, "1.0.0", "Test", "Desc", new[] { ModulusHostIds.Avalonia });
        var sharedCatalog = Modulus.Core.Architecture.SharedAssemblyCatalog.FromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
        var loadContext = new ModuleLoadContext(id, "/path/to/module", sharedCatalog);
        var manifest = CreateTestManifest(id, "1.0.0");
        return new RuntimeModule(descriptor, loadContext, "/path/to/module", manifest, false);
    }

    [Fact]
    public void Diagnostics_RecordsInitialTransition()
    {
        // Act
        var module = CreateTestModule();

        // Assert
        Assert.Single(module.Diagnostics.Transitions);
        Assert.Equal(ModuleState.Unknown, module.Diagnostics.Transitions[0].FromState);
        Assert.Equal(ModuleState.Loaded, module.Diagnostics.Transitions[0].ToState);
    }

    [Fact]
    public void Diagnostics_RecordsSubsequentTransitions()
    {
        // Arrange
        var module = CreateTestModule();

        // Act
        module.TransitionTo(ModuleState.Active, "Host bound");
        module.TransitionTo(ModuleState.Error, "Runtime error", new Exception("Test error"));

        // Assert
        Assert.Equal(3, module.Diagnostics.Transitions.Count);
        Assert.Equal(ModuleState.Active, module.Diagnostics.Transitions[1].ToState);
        Assert.Equal(ModuleState.Error, module.Diagnostics.Transitions[2].ToState);
    }

    [Fact]
    public void Diagnostics_StoresExceptionOnError()
    {
        // Arrange
        var module = CreateTestModule();
        var testError = new InvalidOperationException("Test error");

        // Act
        module.TransitionTo(ModuleState.Error, "Init failed", testError);

        // Assert
        Assert.Equal(testError, module.LastError);
        Assert.Equal(testError, module.Diagnostics.GetLastError());
    }

    [Fact]
    public void Diagnostics_GetLastReason_ReturnsCorrectReason()
    {
        // Arrange
        var module = CreateTestModule();

        // Act
        module.TransitionTo(ModuleState.Active, "Activated successfully");

        // Assert
        Assert.Equal("Activated successfully", module.Diagnostics.GetLastReason());
    }

    [Fact]
    public void Diagnostics_GetSummary_IncludesTransitionChain()
    {
        // Arrange
        var module = CreateTestModule();
        module.TransitionTo(ModuleState.Active, "Activate");

        // Act
        var summary = module.Diagnostics.GetSummary();

        // Assert
        Assert.Contains("Loaded", summary);
        Assert.Contains("Active", summary);
    }

    [Fact]
    public void Diagnostics_GetSummary_IncludesErrorInfo()
    {
        // Arrange
        var module = CreateTestModule();
        module.TransitionTo(ModuleState.Error, "Failed", new Exception("Something went wrong"));

        // Act
        var summary = module.Diagnostics.GetSummary();

        // Assert
        Assert.Contains("Error", summary);
        Assert.Contains("Something went wrong", summary);
    }

    [Fact]
    public void Diagnostics_GetTimeInState_ReturnsNonNullForCurrentState()
    {
        // Arrange
        var module = CreateTestModule();

        // Act - wait a tiny bit
        Thread.Sleep(10);
        var time = module.Diagnostics.GetTimeInState(ModuleState.Loaded);

        // Assert
        Assert.NotNull(time);
        Assert.True(time.Value.TotalMilliseconds >= 10);
    }

    private static VsixManifest CreateTestManifest(string id, string version)
    {
        return new VsixManifest
        {
            Version = "2.0.0",
            Metadata = new ManifestMetadata
            {
                Identity = new ManifestIdentity { Id = id, Version = version, Publisher = "Test" },
                DisplayName = id
            },
            Installation = new() { new InstallationTarget { Id = ModulusHostIds.Avalonia } },
            Assets = new() { new ManifestAsset { Type = ModulusAssetTypes.Package, Path = "Test.dll" } }
        };
    }
}

/// <summary>
/// Tests for RuntimeModule state machine.
/// </summary>
public class RuntimeModuleStateTests
{
    private RuntimeModule CreateTestModule(string id = "test-module")
    {
        var descriptor = new ModuleDescriptor(id, "1.0.0", "Test", "Desc", new[] { ModulusHostIds.Avalonia });
        var sharedCatalog = Modulus.Core.Architecture.SharedAssemblyCatalog.FromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
        var loadContext = new ModuleLoadContext(id, "/path/to/module", sharedCatalog);
        var manifest = CreateTestManifest(id, "1.0.0");
        return new RuntimeModule(descriptor, loadContext, "/path/to/module", manifest, false);
    }

    [Fact]
    public void NewModule_StartsInLoadedState()
    {
        // Act
        var module = CreateTestModule();

        // Assert
        Assert.Equal(ModuleState.Loaded, module.State);
    }

    [Fact]
    public void TransitionTo_ChangesState()
    {
        // Arrange
        var module = CreateTestModule();

        // Act
        module.TransitionTo(ModuleState.Active, "Host bound");

        // Assert
        Assert.Equal(ModuleState.Active, module.State);
    }

    [Fact]
    public void TransitionTo_RecordsDiagnostics()
    {
        // Arrange
        var module = CreateTestModule();

        // Act
        module.TransitionTo(ModuleState.Active, "Activated");

        // Assert
        Assert.Equal(2, module.Diagnostics.Transitions.Count); // Initial + transition
        Assert.Equal("Activated", module.Diagnostics.GetLastReason());
    }

    [Fact]
    public void TransitionTo_WithError_StoresError()
    {
        // Arrange
        var module = CreateTestModule();
        var error = new Exception("Init failed");

        // Act
        module.TransitionTo(ModuleState.Error, "Initialization error", error);

        // Assert
        Assert.Equal(ModuleState.Error, module.State);
        Assert.Equal(error, module.LastError);
    }

    [Fact]
    public void TransitionTo_SameState_NoOp()
    {
        // Arrange
        var module = CreateTestModule();
        var initialCount = module.Diagnostics.Transitions.Count;

        // Act
        module.TransitionTo(ModuleState.Loaded, "Should not record");

        // Assert
        Assert.Equal(initialCount, module.Diagnostics.Transitions.Count);
    }

    [Theory]
    [InlineData(ModuleState.Loaded, ModuleState.Active, true)]
    [InlineData(ModuleState.Loaded, ModuleState.Error, true)]
    [InlineData(ModuleState.Active, ModuleState.Error, true)]
    [InlineData(ModuleState.Active, ModuleState.Loaded, true)]
    [InlineData(ModuleState.Error, ModuleState.Loaded, true)]
    [InlineData(ModuleState.Unloaded, ModuleState.Active, false)]
    public void CanTransitionTo_ValidatesTransitions(ModuleState from, ModuleState to, bool expected)
    {
        // Arrange
        var module = CreateTestModule();
        module.TransitionTo(from, "Setup");

        // Act
        var result = module.CanTransitionTo(to);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void StateProperty_SetterUsesTransitionTo()
    {
        // Arrange
        var module = CreateTestModule();
        var initialCount = module.Diagnostics.Transitions.Count;

        // Act
        module.State = ModuleState.Active;

        // Assert
        Assert.Equal(ModuleState.Active, module.State);
        Assert.Equal(initialCount + 1, module.Diagnostics.Transitions.Count);
    }

    private static VsixManifest CreateTestManifest(string id, string version)
    {
        return new VsixManifest
        {
            Version = "2.0.0",
            Metadata = new ManifestMetadata
            {
                Identity = new ManifestIdentity { Id = id, Version = version, Publisher = "Test" },
                DisplayName = id
            },
            Installation = new() { new InstallationTarget { Id = ModulusHostIds.Avalonia } },
            Assets = new() { new ManifestAsset { Type = ModulusAssetTypes.Package, Path = "Test.dll" } }
        };
    }
}

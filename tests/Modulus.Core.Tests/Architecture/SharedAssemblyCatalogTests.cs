using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Modulus.Architecture;
using Modulus.Core.Architecture;
using Xunit;

namespace Modulus.Core.Tests.Architecture;

public class SharedAssemblyCatalogTests
{
    [Fact]
    public void FromAssemblies_ScansSharedDomainAssemblies()
    {
        // Arrange
        var assemblies = new[] { typeof(SharedAssemblyCatalog).Assembly };
        
        // Act
        var catalog = SharedAssemblyCatalog.FromAssemblies(assemblies);
        
        // Assert
        Assert.Contains("Modulus.Core", catalog.Names);
    }
    
    [Fact]
    public void FromAssemblies_MergesConfiguredAssemblies()
    {
        // Arrange
        var assemblies = Array.Empty<Assembly>();
        var configuredAssemblies = new[] { "TestAssembly1", "TestAssembly2" };
        
        // Act
        var catalog = SharedAssemblyCatalog.FromAssemblies(assemblies, configuredAssemblies);
        
        // Assert
        Assert.Contains("TestAssembly1", catalog.Names);
        Assert.Contains("TestAssembly2", catalog.Names);
        
        var entries = catalog.GetEntries();
        var entry1 = entries.FirstOrDefault(e => e.Name == "TestAssembly1");
        Assert.NotNull(entry1);
        Assert.Equal(SharedAssemblySource.HostConfig, entry1.Source);
    }
    
    [Fact]
    public void FromAssemblies_RejectsEmptyConfigNames()
    {
        // Arrange
        var assemblies = Array.Empty<Assembly>();
        var configuredAssemblies = new[] { "", "  ", "ValidAssembly" };
        
        // Act
        var catalog = SharedAssemblyCatalog.FromAssemblies(assemblies, configuredAssemblies);
        
        // Assert
        Assert.Single(catalog.Names); // Only ValidAssembly
        Assert.Contains("ValidAssembly", catalog.Names);
        
        var mismatches = catalog.GetMismatches();
        Assert.Equal(2, mismatches.Count); // Two empty/whitespace entries
    }
    
    [Fact]
    public void FromAssemblies_RejectsTooLongNames()
    {
        // Arrange
        var assemblies = Array.Empty<Assembly>();
        var longName = new string('x', SharedAssemblyOptions.MaxAssemblyNameLength + 1);
        var configuredAssemblies = new[] { longName };
        
        // Act
        var catalog = SharedAssemblyCatalog.FromAssemblies(assemblies, configuredAssemblies);
        
        // Assert
        Assert.Empty(catalog.Names);
        var mismatches = catalog.GetMismatches();
        Assert.Single(mismatches);
        Assert.Contains("maximum length", mismatches.First().Reason);
    }
    
    [Fact]
    public void IsShared_ReturnsTrueForCatalogEntry()
    {
        // Arrange
        var assemblies = Array.Empty<Assembly>();
        var configuredAssemblies = new[] { "MySharedAssembly" };
        var catalog = SharedAssemblyCatalog.FromAssemblies(assemblies, configuredAssemblies);
        
        // Act
        var isShared = catalog.IsShared(new AssemblyName("MySharedAssembly"));
        
        // Assert
        Assert.True(isShared);
    }
    
    [Fact]
    public void IsShared_ReturnsFalseForUnknownAssembly()
    {
        // Arrange
        var catalog = SharedAssemblyCatalog.FromAssemblies(Array.Empty<Assembly>());
        
        // Act
        var isShared = catalog.IsShared(new AssemblyName("UnknownAssembly"));
        
        // Assert
        Assert.False(isShared);
    }
    
    [Fact]
    public void AddManifestHints_AddsEntriesToCatalog()
    {
        // Arrange
        var catalog = SharedAssemblyCatalog.FromAssemblies(Array.Empty<Assembly>());
        var moduleId = "test-module";
        var hints = new[] { "HintedAssembly1", "HintedAssembly2" };
        
        // Act
        var mismatches = catalog.AddManifestHints(moduleId, hints);
        
        // Assert
        Assert.Empty(mismatches);
        Assert.Contains("HintedAssembly1", catalog.Names);
        Assert.Contains("HintedAssembly2", catalog.Names);
        
        var entries = catalog.GetEntries();
        var entry = entries.FirstOrDefault(e => e.Name == "HintedAssembly1");
        Assert.NotNull(entry);
        Assert.Equal(SharedAssemblySource.ManifestHint, entry.Source);
        Assert.Equal(moduleId, entry.SourceModuleId);
    }
    
    [Fact]
    public void AddManifestHints_TruncatesExcessiveHints()
    {
        // Arrange
        var catalog = SharedAssemblyCatalog.FromAssemblies(Array.Empty<Assembly>());
        var moduleId = "test-module";
        var hints = Enumerable.Range(1, SharedAssemblyOptions.MaxManifestHints + 10)
            .Select(i => $"Assembly{i}")
            .ToList();
        
        // Act
        var mismatches = catalog.AddManifestHints(moduleId, hints);
        
        // Assert
        Assert.Single(mismatches);
        Assert.Contains("exceeding maximum", mismatches[0].Reason);
        Assert.Equal(SharedAssemblyOptions.MaxManifestHints, catalog.Names.Count);
    }
    
    [Fact]
    public void AddManifestHints_SkipsExistingEntries()
    {
        // Arrange
        var configuredAssemblies = new[] { "ExistingAssembly" };
        var catalog = SharedAssemblyCatalog.FromAssemblies(Array.Empty<Assembly>(), configuredAssemblies);
        var moduleId = "test-module";
        var hints = new[] { "ExistingAssembly", "NewAssembly" };
        
        // Act
        var mismatches = catalog.AddManifestHints(moduleId, hints);
        
        // Assert
        Assert.Empty(mismatches);
        
        var entries = catalog.GetEntries();
        var existingEntry = entries.First(e => e.Name == "ExistingAssembly");
        Assert.Equal(SharedAssemblySource.HostConfig, existingEntry.Source); // Should keep original source
    }
    
    [Fact]
    public void GetEntries_ReturnsAllEntriesWithSources()
    {
        // Arrange
        var assemblies = new[] { typeof(SharedAssemblyCatalog).Assembly };
        var configuredAssemblies = new[] { "ConfigAssembly" };
        var catalog = SharedAssemblyCatalog.FromAssemblies(assemblies, configuredAssemblies);
        catalog.AddManifestHints("module1", new[] { "ManifestAssembly" });
        
        // Act
        var entries = catalog.GetEntries();
        
        // Assert
        Assert.True(entries.Count >= 3); // At least domain + config + manifest
        Assert.Contains(entries, e => e.Source == SharedAssemblySource.DomainAttribute);
        Assert.Contains(entries, e => e.Source == SharedAssemblySource.HostConfig);
        Assert.Contains(entries, e => e.Source == SharedAssemblySource.ManifestHint);
    }
}

public class SharedAssemblyDiagnosticsServiceTests
{
    [Fact]
    public void GetDiagnostics_ReturnsCorrectCounts()
    {
        // Arrange
        var assemblies = new[] { typeof(SharedAssemblyCatalog).Assembly };
        var configuredAssemblies = new[] { "ConfigAssembly1", "ConfigAssembly2" };
        var catalog = SharedAssemblyCatalog.FromAssemblies(assemblies, configuredAssemblies);
        catalog.AddManifestHints("module1", new[] { "ManifestAssembly" });
        
        var service = new SharedAssemblyDiagnosticsService(catalog);
        
        // Act
        var diagnostics = service.GetDiagnostics();
        
        // Assert
        Assert.Equal(catalog.GetEntries().Count, diagnostics.TotalCount);
        Assert.NotEmpty(diagnostics.DomainEntries);
        Assert.Equal(2, diagnostics.ConfigEntries.Count);
        Assert.Single(diagnostics.ManifestEntries);
        Assert.NotNull(diagnostics.PrefixRules);
    }
    
    [Fact]
    public void GetEntriesBySource_FiltersCorrectly()
    {
        // Arrange
        var configuredAssemblies = new[] { "ConfigAssembly" };
        var catalog = SharedAssemblyCatalog.FromAssemblies(Array.Empty<Assembly>(), configuredAssemblies);
        var service = new SharedAssemblyDiagnosticsService(catalog);
        
        // Act
        var configEntries = service.GetEntriesBySource(SharedAssemblySource.HostConfig);
        var manifestEntries = service.GetEntriesBySource(SharedAssemblySource.ManifestHint);
        
        // Assert
        Assert.Single(configEntries);
        Assert.Empty(manifestEntries);
    }
    
    [Fact]
    public void GetEntriesForModule_ReturnsModuleSpecificEntries()
    {
        // Arrange
        var catalog = SharedAssemblyCatalog.FromAssemblies(Array.Empty<Assembly>());
        catalog.AddManifestHints("module1", new[] { "Assembly1", "Assembly2" });
        catalog.AddManifestHints("module2", new[] { "Assembly3" });
        
        var service = new SharedAssemblyDiagnosticsService(catalog);
        
        // Act
        var module1Entries = service.GetEntriesForModule("module1");
        var module2Entries = service.GetEntriesForModule("module2");
        
        // Assert
        Assert.Equal(2, module1Entries.Count);
        Assert.Single(module2Entries);
    }
    
    [Fact]
    public void GetMismatchesForModule_ReturnsModuleSpecificMismatches()
    {
        // Arrange
        var catalog = SharedAssemblyCatalog.FromAssemblies(Array.Empty<Assembly>());
        catalog.AddManifestHints("module1", new[] { "", "ValidAssembly" }); // Empty name causes mismatch
        catalog.AddManifestHints("module2", new[] { "AnotherValidAssembly" });
        
        var service = new SharedAssemblyDiagnosticsService(catalog);
        
        // Act
        var module1Mismatches = service.GetMismatchesForModule("module1");
        var module2Mismatches = service.GetMismatchesForModule("module2");
        
        // Assert
        Assert.Single(module1Mismatches);
        Assert.Empty(module2Mismatches);
    }
}


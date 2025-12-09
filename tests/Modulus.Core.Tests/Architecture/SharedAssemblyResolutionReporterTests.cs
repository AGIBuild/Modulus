using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Modulus.Architecture;
using Modulus.Core.Architecture;
using Xunit;

namespace Modulus.Core.Tests.Architecture;

public class SharedAssemblyResolutionReporterTests
{
    [Fact]
    public void ReportFailure_AddsToReportedFailures()
    {
        // Arrange
        var logger = NullLogger<SharedAssemblyResolutionReporter>.Instance;
        var reporter = new SharedAssemblyResolutionReporter(logger);
        var failure = new SharedAssemblyResolutionFailedEvent
        {
            ModuleId = "test-module",
            AssemblyName = "TestAssembly",
            Source = SharedAssemblySource.ManifestHint,
            Reason = "Test failure reason"
        };
        
        // Act
        reporter.ReportFailure(failure);
        
        // Assert
        var reported = reporter.GetReportedFailures();
        Assert.Single(reported);
        Assert.Equal("test-module", reported[0].ModuleId);
        Assert.Equal("TestAssembly", reported[0].AssemblyName);
    }
    
    [Fact]
    public void ReportFailure_PreservesAllProperties()
    {
        // Arrange
        var logger = NullLogger<SharedAssemblyResolutionReporter>.Instance;
        var reporter = new SharedAssemblyResolutionReporter(logger);
        var exception = new InvalidOperationException("Test exception");
        var failure = new SharedAssemblyResolutionFailedEvent
        {
            ModuleId = "test-module",
            AssemblyName = "TestAssembly",
            Source = SharedAssemblySource.HostConfig,
            DeclaredDomain = AssemblyDomainType.Module,
            Reason = "Domain mismatch",
            Exception = exception
        };
        
        // Act
        reporter.ReportFailure(failure);
        
        // Assert
        var reported = reporter.GetReportedFailures()[0];
        Assert.Equal(SharedAssemblySource.HostConfig, reported.Source);
        Assert.Equal(AssemblyDomainType.Module, reported.DeclaredDomain);
        Assert.Equal(exception, reported.Exception);
    }
    
    [Fact]
    public void GetReportedFailures_ReturnsAllFailuresInOrder()
    {
        // Arrange
        var logger = NullLogger<SharedAssemblyResolutionReporter>.Instance;
        var reporter = new SharedAssemblyResolutionReporter(logger);
        
        // Act
        for (int i = 1; i <= 3; i++)
        {
            reporter.ReportFailure(new SharedAssemblyResolutionFailedEvent
            {
                ModuleId = $"module{i}",
                AssemblyName = $"Assembly{i}",
                Source = SharedAssemblySource.ManifestHint,
                Reason = $"Reason {i}"
            });
        }
        
        // Assert
        var reported = reporter.GetReportedFailures();
        Assert.Equal(3, reported.Count);
        Assert.Equal("module1", reported[0].ModuleId);
        Assert.Equal("module2", reported[1].ModuleId);
        Assert.Equal("module3", reported[2].ModuleId);
    }
    
    [Fact]
    public void ReportFailure_ThrowsOnNull()
    {
        // Arrange
        var logger = NullLogger<SharedAssemblyResolutionReporter>.Instance;
        var reporter = new SharedAssemblyResolutionReporter(logger);
        
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => reporter.ReportFailure(null!));
    }
}

public class SharedAssemblyResolutionFailedEventTests
{
    [Fact]
    public void Constructor_SetsDefaultTimestamp()
    {
        // Arrange & Act
        var before = DateTimeOffset.UtcNow;
        var evt = new SharedAssemblyResolutionFailedEvent
        {
            ModuleId = "test",
            AssemblyName = "TestAssembly",
            Source = SharedAssemblySource.DomainAttribute,
            Reason = "Test"
        };
        var after = DateTimeOffset.UtcNow;
        
        // Assert
        Assert.True(evt.Timestamp >= before && evt.Timestamp <= after);
    }
    
    [Fact]
    public void Record_SupportsEquality()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var evt1 = new SharedAssemblyResolutionFailedEvent
        {
            ModuleId = "test",
            AssemblyName = "TestAssembly",
            Source = SharedAssemblySource.DomainAttribute,
            Reason = "Test",
            Timestamp = timestamp
        };
        var evt2 = new SharedAssemblyResolutionFailedEvent
        {
            ModuleId = "test",
            AssemblyName = "TestAssembly",
            Source = SharedAssemblySource.DomainAttribute,
            Reason = "Test",
            Timestamp = timestamp
        };
        
        // Assert
        Assert.Equal(evt1, evt2);
    }
}


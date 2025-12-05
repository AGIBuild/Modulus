using System.Reflection;
using Modulus.Architecture;
using Modulus.Core.Architecture;
using Xunit;

namespace Modulus.Core.Tests.Architecture;

public class AssemblyDomainInfoTests
{
    [Fact]
    public void GetDomainType_ModulusCore_ReturnsShared()
    {
        // Arrange
        var assembly = typeof(AssemblyDomainInfo).Assembly;
        
        // Act
        var domainType = AssemblyDomainInfo.GetDomainType(assembly);
        
        // Assert
        Assert.Equal(AssemblyDomainType.Shared, domainType);
    }

    [Fact]
    public void GetDomainType_ModulusUIAbstractions_ReturnsShared()
    {
        // Arrange
        var assembly = typeof(AssemblyDomainAttribute).Assembly;
        
        // Act
        var domainType = AssemblyDomainInfo.GetDomainType(assembly);
        
        // Assert
        Assert.Equal(AssemblyDomainType.Shared, domainType);
    }

    [Fact]
    public void GetDiagnostics_ReturnsValidInfo()
    {
        // Arrange
        var assembly = typeof(AssemblyDomainInfo).Assembly;
        
        // Act
        var diagnostics = AssemblyDomainInfo.GetDiagnostics(assembly);
        
        // Assert
        Assert.NotNull(diagnostics);
        Assert.Equal("Modulus.Core", diagnostics.AssemblyName);
        Assert.True(diagnostics.IsDefaultContext);
    }

    [Fact]
    public void GetLoadContextName_ReturnsDefaultForSharedAssembly()
    {
        // Arrange
        var assembly = typeof(AssemblyDomainInfo).Assembly;
        
        // Act
        var contextName = AssemblyDomainInfo.GetLoadContextName(assembly);
        
        // Assert
        Assert.Equal("Default", contextName);
    }

    [Fact]
    public void IsSharedAssembly_KnownSharedAssembly_ReturnsTrue()
    {
        // Arrange
        var assembly = typeof(AssemblyDomainInfo).Assembly;
        
        // Act
        var isShared = AssemblyDomainInfo.IsSharedAssembly(assembly);
        
        // Assert
        Assert.True(isShared);
    }

    [Fact]
    public void ValidateDomainDeclaration_SharedAssemblyInDefaultContext_ReturnsTrue()
    {
        // Arrange
        var assembly = typeof(AssemblyDomainInfo).Assembly;
        
        // Act
        var isValid = AssemblyDomainInfo.ValidateDomainDeclaration(assembly, out var errorMessage);
        
        // Assert
        Assert.True(isValid);
        Assert.Null(errorMessage);
    }
}

public class AssemblyDomainTypeTests
{
    [Theory]
    [InlineData(AssemblyDomainType.Unknown, 0)]
    [InlineData(AssemblyDomainType.Shared, 1)]
    [InlineData(AssemblyDomainType.Module, 2)]
    public void AssemblyDomainType_HasExpectedValues(AssemblyDomainType domainType, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)domainType);
    }
}

public class AssemblyDomainAttributeTests
{
    [Fact]
    public void Constructor_SetsDomainType()
    {
        // Arrange & Act
        var attr = new AssemblyDomainAttribute(AssemblyDomainType.Shared);
        
        // Assert
        Assert.Equal(AssemblyDomainType.Shared, attr.DomainType);
    }

    [Fact]
    public void Description_CanBeSet()
    {
        // Arrange & Act
        var attr = new AssemblyDomainAttribute(AssemblyDomainType.Module)
        {
            Description = "Test module"
        };
        
        // Assert
        Assert.Equal("Test module", attr.Description);
    }

    [Fact]
    public void Attribute_IsOnlyForAssemblies()
    {
        // Arrange
        var attributeUsage = typeof(AssemblyDomainAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>();
        
        // Assert
        Assert.NotNull(attributeUsage);
        Assert.Equal(AttributeTargets.Assembly, attributeUsage.ValidOn);
        Assert.False(attributeUsage.AllowMultiple);
        Assert.False(attributeUsage.Inherited);
    }
}


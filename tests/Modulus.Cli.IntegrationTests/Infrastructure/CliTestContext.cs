using Microsoft.Extensions.DependencyInjection;
using Modulus.Cli.Services;

namespace Modulus.Cli.IntegrationTests.Infrastructure;

/// <summary>
/// Test environment isolation for CLI integration tests.
/// Each test gets independent working directory, database, and modules directory.
/// </summary>
public class CliTestContext : IDisposable
{
    private ServiceProvider? _serviceProvider;
    private bool _disposed;
    
    /// <summary>
    /// Unique test ID for this context.
    /// </summary>
    public string TestId { get; }
    
    /// <summary>
    /// Root working directory for this test.
    /// </summary>
    public string WorkingDirectory { get; }
    
    /// <summary>
    /// Path to the test SQLite database.
    /// </summary>
    public string DatabasePath { get; }
    
    /// <summary>
    /// Directory where modules are installed for this test.
    /// </summary>
    public string ModulesDirectory { get; }
    
    /// <summary>
    /// Directory for module project output (e.g., .modpkg files).
    /// </summary>
    public string OutputDirectory { get; }
    
    public CliTestContext()
    {
        TestId = Guid.NewGuid().ToString("N")[..8];
        WorkingDirectory = Path.Combine(Path.GetTempPath(), $"modulus-cli-test-{TestId}");
        DatabasePath = Path.Combine(WorkingDirectory, "test.db");
        ModulesDirectory = Path.Combine(WorkingDirectory, "Modules");
        OutputDirectory = Path.Combine(WorkingDirectory, "output");
        
        Directory.CreateDirectory(WorkingDirectory);
        Directory.CreateDirectory(ModulesDirectory);
        Directory.CreateDirectory(OutputDirectory);
    }
    
    /// <summary>
    /// Gets a configured service provider for this test context.
    /// Database migrations are run automatically on first access.
    /// </summary>
    public async Task<ServiceProvider> GetServiceProviderAsync()
    {
        if (_serviceProvider == null)
        {
            _serviceProvider = CliServiceProvider.Build(
                verbose: false,
                databasePath: DatabasePath,
                modulesDirectory: ModulesDirectory);
            
            await CliServiceProvider.EnsureMigratedAsync(_serviceProvider);
        }
        return _serviceProvider;
    }
    
    /// <summary>
    /// Creates a subdirectory in the working directory.
    /// </summary>
    public string CreateSubDirectory(string name)
    {
        var path = Path.Combine(WorkingDirectory, name);
        Directory.CreateDirectory(path);
        return path;
    }
    
    /// <summary>
    /// Creates a file with specified content in the working directory.
    /// </summary>
    public string CreateFile(string relativePath, string content)
    {
        var path = Path.Combine(WorkingDirectory, relativePath);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllText(path, content);
        return path;
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        _serviceProvider?.Dispose();
        
        // Clean up test directory
        try
        {
            if (Directory.Exists(WorkingDirectory))
            {
                Directory.Delete(WorkingDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
        
        GC.SuppressFinalize(this);
    }
}



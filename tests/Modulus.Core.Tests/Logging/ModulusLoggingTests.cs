using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Modulus.Core;
using Modulus.Core.Logging;
using Modulus.Sdk;
using Xunit;

namespace Modulus.Core.Tests.Logging;

public class ModulusLoggingTests : IDisposable
{
    private readonly string _tempDir;
    private readonly TextWriter _originalOut;

    public ModulusLoggingTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "ModulusLoggingTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _originalOut = Console.Out;
    }

    [Fact]
    public void CreateLoggerFactory_WritesFileAndRespectsRetention()
    {
        var configDict = new Dictionary<string, string?>
        {
            ["Serilog:Using:0"] = "Serilog.Sinks.File",
            ["Serilog:MinimumLevel:Default"] = "Information",
            ["Serilog:WriteTo:0:Name"] = "File",
            ["Serilog:WriteTo:0:Args:path"] = Path.Combine(_tempDir, "log-.txt"),
            ["Serilog:WriteTo:0:Args:rollingInterval"] = "Day",
            ["Serilog:WriteTo:0:Args:retainedFileCountLimit"] = "2",
            ["Serilog:WriteTo:0:Args:fileSizeLimitBytes"] = "20000",
            ["Serilog:WriteTo:0:Args:rollOnFileSizeLimit"] = "true",
            ["Serilog:WriteTo:0:Args:outputTemplate"] = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict!)
            .Build();

        using (var factory = ModulusLogging.CreateLoggerFactory(configuration, ModulusHostIds.Avalonia))
        {
            var logger = factory.CreateLogger("file-test");
            var payload = new string('x', 4096);

            for (var i = 0; i < 600; i++)
            {
                logger.LogInformation("Entry {Index} {Payload}", i, payload);
            }
        }

        var files = Directory.GetFiles(_tempDir, "log-*.txt");
        Assert.NotEmpty(files);
        Assert.True(files.Length <= 2, $"Retention should cap files to 2 but got {files.Length}");

        var content = File.ReadAllText(files[0]);
        Assert.Contains("Entry", content);
    }

    [Fact]
    public void CreateLoggerFactory_ConsoleEnabled_WritesToConsole()
    {
        var configDict = new Dictionary<string, string?>
        {
            ["Serilog:Using:0"] = "Serilog.Sinks.Console",
            ["Serilog:MinimumLevel:Default"] = "Information",
            ["Serilog:WriteTo:0:Name"] = "Console",
            ["Serilog:WriteTo:0:Args:outputTemplate"] = "[{Level}] {Message:lj}{NewLine}"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict!)
            .Build();

        using var writer = new StringWriter();
        Console.SetOut(writer);
        try
        {
            using (var factory = ModulusLogging.CreateLoggerFactory(configuration, ModulusHostIds.Avalonia))
            {
                var logger = factory.CreateLogger("console-test");
                logger.LogInformation("console-message-{Number}", 42);
            }

            var output = writer.ToString();
            Assert.Contains("console-message-42", output);
        }
        finally
        {
            Console.SetOut(_originalOut);
        }
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch
        {
            // Ignore cleanup issues in tests
        }

        Console.SetOut(_originalOut);
    }
}


using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Modulus.Core.Paths;
using Serilog;
using Serilog.Extensions.Logging;

namespace Modulus.Core.Logging;

public static class ModulusLogging
{
    public static ILoggerFactory CreateLoggerFactory(IConfiguration configuration, string hostType)
    {
        var serilogLogger = BuildSerilogLogger(configuration, hostType);
        return LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(serilogLogger, dispose: true);
        });
    }

    public static void AddLoggerFactory(IServiceCollection services, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        services.AddLogging();
        services.Replace(ServiceDescriptor.Singleton(loggerFactory));
        services.TryAddSingleton(typeof(ILogger<>), typeof(Logger<>));
    }

    private static Serilog.ILogger BuildSerilogLogger(IConfiguration configuration, string hostType)
    {
        var effectiveConfig = NormalizeSerilogConfiguration(configuration, hostType);

        var loggerConfiguration = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("HostType", hostType);

        loggerConfiguration.ReadFrom.Configuration(effectiveConfig);

        return loggerConfiguration.CreateLogger();
    }

    private static IConfiguration NormalizeSerilogConfiguration(IConfiguration configuration, string hostType)
    {
        var overrides = new Dictionary<string, string?>();
        var fallbackDir = LocalStorage.GetLogsRoot(hostType);

        foreach (var wt in configuration.GetSection("Serilog:WriteTo").GetChildren())
        {
            var name = wt["Name"];
            if (!string.Equals(name, "File", StringComparison.OrdinalIgnoreCase)) continue;

            var rawPath = wt.GetSection("Args")["path"];
            if (string.IsNullOrWhiteSpace(rawPath)) continue;
            if (Path.IsPathRooted(rawPath)) continue;

            var combined = Path.Combine(fallbackDir, rawPath);
            overrides[$"{wt.Path}:Args:path"] = combined;
        }

        if (overrides.Count == 0)
        {
            return configuration;
        }

        return new ConfigurationBuilder()
            .AddConfiguration(configuration)
            .AddInMemoryCollection(overrides!)
            .Build();
    }
}

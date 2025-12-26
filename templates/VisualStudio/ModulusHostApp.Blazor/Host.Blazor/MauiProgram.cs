using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Modulus.Core.Data;
using Modulus.Core.Logging;
using Modulus.Core.Runtime;
using Modulus.HostSdk.Abstractions;
using Modulus.HostSdk.Runtime;
using Modulus.Sdk;

namespace $ext_safeprojectname$.Host.Blazor;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        var loggerFactory = ModulusLogging.CreateLoggerFactory(configuration, ModulusHostIds.Blazor);

        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        ModulusLogging.AddLoggerFactory(builder.Services, loggerFactory);

        var dbName = configuration["Modulus:DatabaseName"] ?? "$safeprojectname$";
        var dbPath = DatabaseServiceExtensions.GetDefaultDatabasePath(dbName);

        var hostVersion = typeof(App).Assembly.GetName().Version ?? new Version(1, 0, 0);
        var sdkOptions = new ModulusHostSdkOptions
        {
            HostId = ModulusHostIds.Blazor,
            HostVersion = hostVersion,
            DatabasePath = dbPath
        };

        var sdkBuilder = new ModulusHostSdkBuilder(builder.Services, configuration, sdkOptions)
            .AddDefaultModuleDirectories()
            .AddDefaultRuntimeServices();

        var modulusApp = Task.Run(async () =>
            await sdkBuilder.BuildAsync<HostModule>(loggerFactory)
        ).GetAwaiter().GetResult();

        builder.Services.AddSingleton(modulusApp);

        return builder.Build();
    }
}

public sealed class HostModule : ModulusPackage
{
}



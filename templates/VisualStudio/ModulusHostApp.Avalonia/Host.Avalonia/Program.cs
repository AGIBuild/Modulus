using Avalonia;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Modulus.Core.Logging;
using Modulus.Sdk;

namespace $ext_safeprojectname$.Host.Avalonia;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        var loggerFactory = ModulusLogging.CreateLoggerFactory(configuration, ModulusHostIds.Avalonia);
        loggerFactory.CreateLogger("Host").LogInformation("Starting $safeprojectname$ (Avalonia host)...");

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}



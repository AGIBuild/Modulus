using Avalonia;
using Avalonia.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Modulus.Core.Logging;
using System;
using System.Threading.Tasks;

namespace Modulus.Host.Avalonia;

class Program
{
    private static ILogger _logger = null!;

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Initialize logger first - Serilog doesn't depend on Avalonia
        var emptyConfig = new ConfigurationBuilder().Build();
        var loggerFactory = ModulusLogging.CreateLoggerFactory(emptyConfig, "AvaloniaApp");
        _logger = loggerFactory.CreateLogger<Program>();

        // Setup global exception handlers (logger is now ready)
        SetupGlobalExceptionHandlers();
        _logger.LogInformation("Global exception handlers initialized.");

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Application startup failed: {Message}", ex.Message);
            throw;
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    private static void SetupGlobalExceptionHandlers()
    {
        // Handle all unhandled exceptions in the AppDomain
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        // Handle unobserved task exceptions
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;

        if (e.IsTerminating)
        {
            _logger.LogCritical(exception, "Fatal unhandled exception - application will terminate: {Message}", 
                exception?.Message ?? "Unknown error");
        }
        else
        {
            _logger.LogError(exception, "Unhandled exception caught: {Message}", 
                exception?.Message ?? "Unknown error");
        }
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        // Mark as observed to prevent process termination
        e.SetObserved();

        _logger.LogError(e.Exception, "Unobserved task exception: {Message}", e.Exception.Message);
    }

    /// <summary>
    /// Called by App to register the Avalonia Dispatcher exception handler after Avalonia is initialized.
    /// </summary>
    internal static void RegisterDispatcherExceptionHandler()
    {
        Dispatcher.UIThread.UnhandledException += OnDispatcherUnhandledException;
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger.LogError(e.Exception, "UI thread exception: {Message}", e.Exception.Message);

        // Mark as handled to prevent crash - the UI can continue
        e.Handled = true;
    }
}


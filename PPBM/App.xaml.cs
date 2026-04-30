using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PPBM.Extensions;

namespace PPBM;

/// <summary>
/// Application entry point. Configures the dependency injection container
/// and launches the main window.
/// </summary>
public partial class App : System.Windows.Application
{
    private IServiceProvider _serviceProvider = null!;

    /// <summary>
    /// Initializes a new instance of <see cref="App"/>.
    /// </summary>
    public App()
    {
        var services = new ServiceCollection();
        services.AddProjectServices();
        _serviceProvider = services.BuildServiceProvider();
    }

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            System.Windows.MessageBox.Show(
                $"PPBM encountered an error:\n\n{args.Exception.Message}\n\n" +
                "The app will continue running, but some features may not work.",
                "PPBM Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            args.Handled = true;
        };

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
}

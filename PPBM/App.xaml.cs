using System.Windows;
using System.Windows.Threading;

namespace PPBM;

public partial class App : System.Windows.Application
{
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
    }
}

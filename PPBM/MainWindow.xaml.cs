using System.Windows;
using System.Windows.Input;
using PPBM.Models;
using PPBM.ViewModels;

namespace PPBM;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        StateChanged += (_, _) =>
        {
            if (WindowState == WindowState.Maximized)
            {
                var border = (System.Windows.Controls.Border)Content;
                border.CornerRadius = new CornerRadius(0);
            }
        };
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            Maximize_Click(sender, e);
        }
        else
        {
            DragMove();
        }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ProfileCard_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && element.Tag is PowerProfile profile)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.SelectedProfile = profile;
            }
        }
    }
}

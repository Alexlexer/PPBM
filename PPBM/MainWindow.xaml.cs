using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
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
            var border = (System.Windows.Controls.Border)Content;
            border.CornerRadius = WindowState == WindowState.Maximized
                ? new CornerRadius(0)
                : new CornerRadius(14);
        };
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
            Maximize_Click(sender, e);
        else
            DragMove();
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
        var element = sender as DependencyObject;
        while (element != null)
        {
            if (element is FrameworkElement fe && fe.Tag is PowerProfile profile)
            {
                if (DataContext is MainViewModel vm)
                    vm.SelectedProfile = profile;
                return;
            }
            element = VisualTreeHelper.GetParent(element);
        }
    }

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is PowerProfile profile && DataContext is MainViewModel vm)
        {
            vm.SelectedProfile = profile;
            vm.ApplyProfileCommand.Execute(null);
        }
    }
}

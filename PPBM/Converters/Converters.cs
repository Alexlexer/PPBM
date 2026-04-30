using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using WColor = System.Windows.Media.Color;

namespace PPBM.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class RecommendBgConverter : IValueConverter
{
    private static readonly SolidColorBrush RecommendedBg = new(WColor.FromRgb(0x36, 0x3B, 0x36));
    private static readonly SolidColorBrush NormalBg = new(WColor.FromRgb(0x2C, 0x2C, 0x2C));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is true) return RecommendedBg;
        return NormalBg;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class WarnColorConverter : IValueConverter
{
    private static readonly SolidColorBrush WarnColor = new(WColor.FromRgb(0xE0, 0x43, 0x43));
    private static readonly SolidColorBrush OkColor = new(WColor.FromRgb(0x6B, 0xCB, 0x77));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is true) return WarnColor;
        return OkColor;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

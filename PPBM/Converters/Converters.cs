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
    private static readonly SolidColorBrush RecommendedBg = new(WColor.FromRgb(0x31, 0x3F, 0x3F));
    private static readonly SolidColorBrush NormalBg = new(WColor.FromRgb(0x45, 0x47, 0x5A));

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
    private static readonly SolidColorBrush WarnColor = new(WColor.FromRgb(0xF3, 0x8B, 0xA8));
    private static readonly SolidColorBrush OkColor = new(WColor.FromRgb(0xA6, 0xE3, 0xA1));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is true) return WarnColor;
        return OkColor;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

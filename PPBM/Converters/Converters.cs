using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using PPBM.Models;

namespace PPBM.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class InvertBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? false : true;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? false : true;
}

public class IsRecommendedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is PowerProfile profile)
            return profile.IsRecommended;
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class RecommendBgConverter : IValueConverter
{
    private static readonly SolidColorBrush RecommendedBg = new(Color.FromRgb(0x31, 0x3F, 0x3F));
    private static readonly SolidColorBrush NormalBg = new(Color.FromRgb(0x45, 0x47, 0x5A));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is true) return RecommendedBg;
        return NormalBg;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class ProfileEqualityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is PowerProfile selected && parameter is PowerProfile item)
            return selected == item;
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is true && parameter is PowerProfile profile)
            return profile;
        return Binding.DoNothing;
    }
}

public class WarnColorConverter : IValueConverter
{
    private static readonly SolidColorBrush WarnColor = new(Color.FromRgb(0xF3, 0x8B, 0xA8));
    private static readonly SolidColorBrush OkColor = new(Color.FromRgb(0xA6, 0xE3, 0xA1));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is true) return WarnColor;
        return OkColor;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class BoostModeColorConverter : IValueConverter
{
    private static readonly SolidColorBrush BadColor = new(Color.FromRgb(0xF3, 0x8B, 0xA8));
    private static readonly SolidColorBrush OkColor = new(Color.FromRgb(0xA6, 0xE3, 0xA1));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is BoostMode mode && mode == BoostMode.Aggressive)
            return BadColor;
        return OkColor;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using WColor = System.Windows.Media.Color;

namespace PPBM.Converters;

/// <summary>
/// Converts a boolean warning flag to a foreground color brush using the monochrome palette:
/// <c>true</c> (warning) returns light gray (#888888), <c>false</c> (OK) returns tertiary gray (#606060).
/// </summary>
public class WarnColorConverter : IValueConverter
{
    private static readonly SolidColorBrush WarnColor = new(WColor.FromRgb(0x88, 0x88, 0x88));
    private static readonly SolidColorBrush OkColor = new(WColor.FromRgb(0x60, 0x60, 0x60));

    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is true) return WarnColor;
        return OkColor;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

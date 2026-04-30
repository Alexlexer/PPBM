using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using WColor = System.Windows.Media.Color;

namespace PPBM.Converters;

/// <summary>
/// Converts a boolean warning flag to a foreground <see cref="SolidColorBrush"/>:
/// <c>true</c> returns red, <c>false</c> returns green.
/// </summary>
public class WarnColorConverter : IValueConverter
{
    private static readonly SolidColorBrush WarnColor = new(WColor.FromRgb(0xE0, 0x43, 0x43));
    private static readonly SolidColorBrush OkColor = new(WColor.FromRgb(0x6B, 0xCB, 0x77));

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

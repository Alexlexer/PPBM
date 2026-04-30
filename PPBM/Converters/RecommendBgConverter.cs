using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using WColor = System.Windows.Media.Color;

namespace PPBM.Converters;

/// <summary>
/// Converts a boolean "is recommended" flag to a background <see cref="SolidColorBrush"/>:
/// <c>true</c> returns a dark green tint, <c>false</c> returns a neutral dark gray.
/// </summary>
public class RecommendBgConverter : IValueConverter
{
    private static readonly SolidColorBrush RecommendedBg = new(WColor.FromRgb(0x36, 0x3B, 0x36));
    private static readonly SolidColorBrush NormalBg = new(WColor.FromRgb(0x2C, 0x2C, 0x2C));

    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is true) return RecommendedBg;
        return NormalBg;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

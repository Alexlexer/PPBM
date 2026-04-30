using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using WColor = System.Windows.Media.Color;

namespace PPBM.Converters;

/// <summary>
/// Returns a background brush for a profile card based on its recommendation flag.
/// Uses the monochrome palette: recommended cards get a subtle lighter background.
/// </summary>
public class RecommendBgConverter : IValueConverter
{
    private static readonly SolidColorBrush RecommendedBg = new(WColor.FromRgb(0x20, 0x20, 0x20));
    private static readonly SolidColorBrush NormalBg = new(WColor.FromRgb(0x1E, 0x1E, 0x1E));

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

using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PPBM.Converters;

/// <summary>
/// Converts a boolean value to <see cref="Visibility"/>:
/// <c>true</c> maps to <see cref="Visibility.Visible"/>, <c>false</c> to <see cref="Visibility.Collapsed"/>.
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

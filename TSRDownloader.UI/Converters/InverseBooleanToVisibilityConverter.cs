namespace TSRDownloader.UI.Converters;

/// <summary>
/// Inverts a boolean and converts to Visibility (false = Visible, true = Collapsed).
/// The non-inverted direction is WPF's built-in <see cref="BooleanToVisibilityConverter"/>.
/// </summary>
public class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

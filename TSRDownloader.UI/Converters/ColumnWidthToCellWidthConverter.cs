namespace TSRDownloader.UI.Converters;

/// <summary>
/// Converts a <see cref="System.Windows.Controls.GridViewColumn"/>'s width into a usable
/// cell content width by subtracting the row presenter's per-cell padding. This lets a
/// GridView cell (e.g. a progress bar) fill its column and track resizing, which GridView
/// does not do on its own.
/// </summary>
public class ColumnWidthToCellWidthConverter : IValueConverter
{
    // Approximate horizontal padding the GridViewRowPresenter reserves per cell.
    private const double CellPadding = 14;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double width && !double.IsNaN(width))
            return Math.Max(0, width - CellPadding);

        // Width not yet resolved (Auto/NaN) — let the element size naturally.
        return DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

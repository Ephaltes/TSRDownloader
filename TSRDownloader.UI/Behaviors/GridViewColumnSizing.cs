namespace TSRDownloader.UI.Behaviors;

/// <summary>
/// Makes a <see cref="GridView"/>'s columns fill the available <see cref="ListView"/>
/// width proportionally, so the list stays responsive as the window is resized while
/// the columns remain individually draggable.
///
/// Usage in XAML:
/// <code>
/// &lt;ListView behaviors:GridViewColumnSizing.Fill="True"&gt;
///     &lt;GridView&gt;
///         &lt;GridViewColumn Width="55"/&gt;                                 &lt;!-- fixed --&gt;
///         &lt;GridViewColumn behaviors:GridViewColumnSizing.StarWeight="2"/&gt; &lt;!-- fills --&gt;
/// </code>
/// Columns without a <c>StarWeight</c> keep their design width; the remaining space is
/// shared between star-weighted columns in proportion to their weights.
/// </summary>
public static class GridViewColumnSizing
{
    private const double MinStarColumnWidth = 60;

    // A small allowance so star columns never quite reach the edge — this avoids a
    // horizontal scrollbar appearing and leaves room for the vertical scrollbar.
    private const double EdgeAllowance = 4;

    /// <summary>Enables proportional fill on a <see cref="ListView"/>.</summary>
    public static readonly DependencyProperty FillProperty = DependencyProperty.RegisterAttached(
        "Fill", typeof(bool), typeof(GridViewColumnSizing),
        new PropertyMetadata(false, OnFillChanged));

    public static void SetFill(DependencyObject element, bool value) => element.SetValue(FillProperty, value);
    public static bool GetFill(DependencyObject element) => (bool)element.GetValue(FillProperty);

    /// <summary>
    /// The proportional weight of a <see cref="GridViewColumn"/>. <c>0</c> (the default)
    /// means the column keeps its fixed design width.
    /// </summary>
    public static readonly DependencyProperty StarWeightProperty = DependencyProperty.RegisterAttached(
        "StarWeight", typeof(double), typeof(GridViewColumnSizing),
        new PropertyMetadata(0.0));

    public static void SetStarWeight(DependencyObject element, double value) => element.SetValue(StarWeightProperty, value);
    public static double GetStarWeight(DependencyObject element) => (double)element.GetValue(StarWeightProperty);

    private static void OnFillChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ListView listView)
            return;

        listView.Loaded -= OnListViewLoaded;
        listView.SizeChanged -= OnListViewSizeChanged;

        if (e.NewValue is true)
        {
            listView.Loaded += OnListViewLoaded;
            listView.SizeChanged += OnListViewSizeChanged;
        }
    }

    private static void OnListViewLoaded(object sender, RoutedEventArgs e) => DistributeWidths((ListView)sender);

    private static void OnListViewSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.WidthChanged)
            DistributeWidths((ListView)sender);
    }

    private static void DistributeWidths(ListView listView)
    {
        if (listView.View is not GridView gridView || gridView.Columns.Count == 0)
            return;

        double available = listView.ActualWidth
                           - SystemParameters.VerticalScrollBarWidth
                           - EdgeAllowance;
        if (available <= 0)
            return;

        double fixedWidth = 0;
        double totalWeight = 0;
        foreach (GridViewColumn column in gridView.Columns)
        {
            double weight = GetStarWeight(column);
            if (weight > 0)
                totalWeight += weight;
            else
                fixedWidth += column.ActualWidth;
        }

        if (totalWeight <= 0)
            return;

        double remaining = available - fixedWidth;
        if (remaining <= 0)
            return;

        foreach (GridViewColumn column in gridView.Columns)
        {
            double weight = GetStarWeight(column);
            if (weight > 0)
                column.Width = Math.Max(MinStarColumnWidth, remaining * weight / totalWeight);
        }
    }
}

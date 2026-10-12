namespace TSRDownloader.UI.Presentation;

/// <summary>Formats byte counts and remaining time for the progress text.</summary>
public static class TransferFormat
{
    private static readonly string[] Units = ["B", "KB", "MB", "GB"];

    /// <summary>"3.2 MB", "512 B".</summary>
    public static string Size(long bytes, CultureInfo culture)
    {
        int unit = UnitFor(bytes);
        return $"{Scaled(bytes, unit, culture)} {Units[unit]}";
    }

    /// <summary>"1.2 / 4.2 MB": both numbers in the unit of the total.</summary>
    public static string Progress(long done, long total, CultureInfo culture)
    {
        int unit = UnitFor(total);
        return $"{Scaled(done, unit, culture)} / {Scaled(total, unit, culture)} {Units[unit]}";
    }

    /// <summary>"0:42", "12:05", "1:02:03".</summary>
    public static string Duration(TimeSpan duration) =>
        duration.TotalHours >= 1 ? duration.ToString(@"h\:mm\:ss") : duration.ToString(@"m\:ss");

    private static int UnitFor(long bytes)
    {
        int unit = 0;
        for (double value = bytes; value >= 1024 && unit < Units.Length - 1; value /= 1024)
            unit++;
        return unit;
    }

    private static string Scaled(long bytes, int unit, CultureInfo culture) =>
        unit == 0
            ? bytes.ToString(culture)
            : (bytes / Math.Pow(1024, unit)).ToString("0.0", culture);
}

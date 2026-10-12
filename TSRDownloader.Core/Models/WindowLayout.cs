namespace TSRDownloader.Core.Models;

/// <summary>Size, position and column widths of the main window, restored on the next start.</summary>
public class WindowLayout
{
    /// <summary>Position and size of the restored (not maximized) window, in device-independent pixels.</summary>
    public double Left { get; set; }
    public double Top { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    public bool IsMaximized { get; set; }

    /// <summary>Width of each list column, in display order.</summary>
    public List<double> ColumnWidths { get; set; } = [];

    /// <summary>
    /// Whether the window's top-left corner (where its title bar can be grabbed) lies on the
    /// given screen area, so a monitor that was unplugged since cannot hide the window.
    /// </summary>
    public bool IsReachableOn(double screenLeft, double screenTop, double screenWidth, double screenHeight)
    {
        const double titleBarGrip = 40;
        return Width > 0 && Height > 0
               && Left + titleBarGrip >= screenLeft && Left + titleBarGrip <= screenLeft + screenWidth
               && Top >= screenTop && Top + titleBarGrip <= screenTop + screenHeight;
    }
}

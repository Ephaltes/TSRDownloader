using TSRDownloader.Core.Models;

namespace TSRDownloader.UI.Services;

/// <summary>Switches the colour scheme of all open and future windows.</summary>
public interface IThemeService
{
    void Apply(AppTheme theme);
}

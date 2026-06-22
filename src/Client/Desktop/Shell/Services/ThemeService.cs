using CommunityToolkit.Mvvm.ComponentModel;
using MaterialDesignThemes.Wpf;

namespace LYBT.Desktop.Shell.Services;

public partial class ThemeService : ObservableObject
{
    private readonly PaletteHelper _paletteHelper = new();

    [ObservableProperty]
    private bool _isDarkMode;

    public void ToggleTheme()
    {
        IsDarkMode = !IsDarkMode;
        var theme = _paletteHelper.GetTheme();
        theme.SetBaseTheme(IsDarkMode ? BaseTheme.Dark : BaseTheme.Light);
        _paletteHelper.SetTheme(theme);
    }

    public void InitializeThemeSync()
    {
        if (_paletteHelper.GetThemeManager() is { } themeManager)
        {
            themeManager.ThemeChanged += (_, e) =>
            {
                IsDarkMode = e.NewTheme?.GetBaseTheme() == BaseTheme.Dark;
            };
        }
    }
}

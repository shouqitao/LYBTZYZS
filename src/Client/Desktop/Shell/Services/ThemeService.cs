using CommunityToolkit.Mvvm.ComponentModel;
using MaterialDesignThemes.Wpf;

namespace LYBT.Desktop.Shell.Services;

public partial class ThemeService : ObservableObject
{
    private readonly PaletteHelper _paletteHelper = new();

    [ObservableProperty]
    private bool _isDarkMode;

    public ThemeService()
    {
        InitializeThemeSync();
    }

    public void ToggleTheme()
    {
        ApplyTheme(!IsDarkMode);
    }

    public void ApplyTheme(bool isDark)
    {
        IsDarkMode = isDark;
        var theme = _paletteHelper.GetTheme();
        theme.SetBaseTheme(isDark ? BaseTheme.Dark : BaseTheme.Light);
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

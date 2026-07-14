using CommunityToolkit.Mvvm.ComponentModel;
using MaterialDesignThemes.Wpf;

namespace LYBT.Desktop.Shell.Services;

public partial class ThemeService : ObservableObject, IThemeService, IDisposable
{
    private readonly PaletteHelper _paletteHelper = new();
    private EventHandler<ThemeChangedEventArgs>? _themeChangedHandler;
    private bool _disposed;

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
            _themeChangedHandler = (_, e) =>
            {
                IsDarkMode = e.NewTheme?.GetBaseTheme() == BaseTheme.Dark;
            };
            themeManager.ThemeChanged += _themeChangedHandler;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        if (_paletteHelper.GetThemeManager() is { } themeManager && _themeChangedHandler != null)
        {
            themeManager.ThemeChanged -= _themeChangedHandler;
        }
        _disposed = true;
    }
}

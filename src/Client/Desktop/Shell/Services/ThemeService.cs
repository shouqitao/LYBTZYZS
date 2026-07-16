using CommunityToolkit.Mvvm.ComponentModel;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Configuration;

namespace LYBT.Desktop.Shell.Services;

public partial class ThemeService : ObservableObject, IThemeService, IDisposable
{
    private readonly PaletteHelper _paletteHelper = new();
    private readonly IConfiguration? _configuration;
    private EventHandler<ThemeChangedEventArgs>? _themeChangedHandler;
    private bool _disposed;

    [ObservableProperty]
    private bool _isDarkMode;

    public ThemeService(IConfiguration? configuration = null)
    {
        _configuration = configuration;
        LoadThemePreference();
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
        SaveThemePreference(isDark);
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

    private static readonly string ThemePreferencePath =
        System.IO.Path.Combine(AppContext.BaseDirectory, "theme-preference.json");

    private void LoadThemePreference()
    {
        var isDarkMode = _configuration?.GetValue<bool>("Theme:IsDarkMode") ?? false;

        try
        {
            if (System.IO.File.Exists(ThemePreferencePath))
            {
                var json = System.IO.File.ReadAllText(ThemePreferencePath);
                var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("IsDarkMode", out var prop))
                    isDarkMode = prop.GetBoolean();
            }
        }
        catch
        {
            // 读取失败使用默认值
        }

        ApplyTheme(isDarkMode);
    }

    private void SaveThemePreference(bool isDarkMode)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(
                new { IsDarkMode = isDarkMode },
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText(ThemePreferencePath, json);
        }
        catch
        {
            // 保存失败不影响主题切换
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

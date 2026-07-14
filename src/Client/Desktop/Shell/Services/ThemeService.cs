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

    private void LoadThemePreference()
    {
        var isDarkMode = _configuration?.GetValue<bool>("Theme:IsDarkMode") ?? false;
        ApplyTheme(isDarkMode);
    }

    private void SaveThemePreference(bool isDarkMode)
    {
        try
        {
            var configPath = System.IO.Path.Combine(
                AppContext.BaseDirectory,
                "appsettings.json");

            if (!System.IO.File.Exists(configPath))
                return;

            var json = System.IO.File.ReadAllText(configPath);
            var config = System.Text.Json.JsonDocument.Parse(json);

            if (config.RootElement.TryGetProperty("Theme", out _))
            {
                var themeObj = config.RootElement.GetProperty("Theme");
                var updatedTheme = System.Text.Json.JsonSerializer.Serialize(new { IsDarkMode = isDarkMode });
                var updatedThemeObj = System.Text.Json.JsonDocument.Parse(updatedTheme).RootElement;

                var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                // 简单替换Theme部分
                json = json.Replace(
                    $"\"IsDarkMode\": {themeObj.GetProperty("IsDarkMode").GetBoolean().ToString().ToLower()}",
                    $"\"IsDarkMode\": {isDarkMode.ToString().ToLower()}");
                System.IO.File.WriteAllText(configPath, json);
            }
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

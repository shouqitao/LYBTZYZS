using CommunityToolkit.Mvvm.ComponentModel;
using MaterialDesignThemes.Wpf;

namespace LYBT.Desktop.Shell.Services;

// TODO(M5): ThemeService 目前为具体类（仅注入到一个 VM）。若后续需要多消费者或测试替换，
// 应抽取 IThemeService 接口并在 App.xaml.cs 注册。
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

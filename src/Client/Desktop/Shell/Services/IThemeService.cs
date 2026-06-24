namespace LYBT.Desktop.Shell.Services;

public interface IThemeService
{
    bool IsDarkMode { get; }
    void ToggleTheme();
    void ApplyTheme(bool isDark);
}

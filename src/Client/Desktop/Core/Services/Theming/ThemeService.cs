using System.Windows;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Core.Services.Theming
{

    /// <summary>
    /// 简化的主题服务实现 - 快速交付核心功能
    /// </summary>
    public class ThemeService : IThemeService
    {
        private readonly ILogger<ThemeService> _logger;
        private string _currentTheme = "Default";

        public string CurrentTheme => _currentTheme;

        public string[] AvailableThemes => new[] { "Default", "Dark", "HighContrast" };

        public ThemeService(ILogger<ThemeService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task SwitchThemeAsync(string themeName)
        {
            try
            {
                if (!AvailableThemes.Contains(themeName))
                {
                    _logger.LogWarning("主题 {ThemeName} 不存在，使用默认主题", themeName);
                    themeName = "Default";
                }

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    switch (themeName)
                    {
                        case "Dark":
                            ApplyDarkTheme();
                            break;

                        case "HighContrast":
                            ApplyHighContrastTheme();
                            break;

                        default:
                            ApplyDefaultTheme();
                            break;
                    }
                });

                _currentTheme = themeName;
                _logger.LogInformation("主题已切换到: {ThemeName}", themeName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换主题失败: {ThemeName}", themeName);
                throw;
            }
        }

        public async Task ApplyThemeSettingsAsync(int fontSize = 14, bool isDarkMode = false)
        {
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // 应用字体大小
                    if (Application.Current.Resources.Contains("BaseFontSize"))
                    {
                        Application.Current.Resources["BaseFontSize"] = (double)fontSize;
                    }
                    else
                    {
                        Application.Current.Resources.Add("BaseFontSize", (double)fontSize);
                    }

                    // 应用深色模式
                    if (isDarkMode)
                    {
                        ApplyDarkTheme();
                    }
                    else
                    {
                        ApplyDefaultTheme();
                    }
                });

                _logger.LogDebug("主题设置已应用: 字体{FontSize}, 深色模式{IsDark}", fontSize, isDarkMode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "应用主题设置失败");
                throw;
            }
        }

        #region 私有主题应用方法

        private void ApplyDefaultTheme()
        {
            // 使用现有的默认主题色彩
            UpdateThemeColors(
                primary: "#FF0FA968",
                background: "#FFF8F9FA",
                surface: "#FFFFFFFF",
                textPrimary: "#FF1A1A1A"
            );
        }

        private void ApplyDarkTheme()
        {
            // 深色主题配色
            UpdateThemeColors(
                primary: "#FF3FBF85",
                background: "#FF1E1E1E",
                surface: "#FF2D2D2D",
                textPrimary: "#FFFFFFFF"
            );
        }

        private void ApplyHighContrastTheme()
        {
            // 高对比度主题 - 使用系统色彩
            if (SystemParameters.HighContrast)
            {
                UpdateThemeColors(
                    primary: SystemColors.HighlightColor.ToString(),
                    background: SystemColors.WindowColor.ToString(),
                    surface: SystemColors.WindowColor.ToString(),
                    textPrimary: SystemColors.WindowTextColor.ToString()
                );
            }
            else
            {
                // 手动高对比度
                UpdateThemeColors(
                    primary: "#FF0000FF",
                    background: "#FFFFFFFF",
                    surface: "#FFFFFFFF",
                    textPrimary: "#FF000000"
                );
            }
        }

        private void UpdateThemeColors(string primary, string background, string surface, string textPrimary)
        {
            try
            {
                var resources = Application.Current.Resources;

                // 更新主要颜色资源
                UpdateColorResource(resources, "PrimaryColor", primary);
                UpdateColorResource(resources, "BackgroundColor", background);
                UpdateColorResource(resources, "SurfaceColor", surface);
                UpdateColorResource(resources, "TextPrimaryColor", textPrimary);

                // 更新对应的画刷资源
                UpdateBrushResource(resources, "PrimaryBrush", primary);
                UpdateBrushResource(resources, "BackgroundBrush", background);
                UpdateBrushResource(resources, "SurfaceBrush", surface);
                UpdateBrushResource(resources, "TextPrimaryBrush", textPrimary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新主题颜色失败");
            }
        }

        private void UpdateColorResource(ResourceDictionary resources, string key, string colorValue)
        {
            try
            {
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorValue);
                if (resources.Contains(key))
                {
                    resources[key] = color;
                }
                else
                {
                    resources.Add(key, color);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "更新颜色资源失败: {Key} = {Value}", key, colorValue);
            }
        }

        private void UpdateBrushResource(ResourceDictionary resources, string key, string colorValue)
        {
            try
            {
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorValue);
                var brush = new System.Windows.Media.SolidColorBrush(color);

                if (resources.Contains(key))
                {
                    resources[key] = brush;
                }
                else
                {
                    resources.Add(key, brush);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "更新画刷资源失败: {Key} = {Value}", key, colorValue);
            }
        }

        #endregion 私有主题应用方法
    }
}

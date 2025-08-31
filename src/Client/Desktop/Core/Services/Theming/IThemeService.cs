using System.Threading.Tasks;

namespace LYBT.Desktop.Core.Services.Theming
{
    /// <summary>
    /// 简化的主题服务接口 - 专注核心功能交付
    /// </summary>
    public interface IThemeService
    {
        /// <summary>
        /// 当前主题名称
        /// </summary>
        string CurrentTheme { get; }

        /// <summary>
        /// 可用主题列表
        /// </summary>
        string[] AvailableThemes { get; }

        /// <summary>
        /// 切换主题
        /// </summary>
        /// <param name="themeName">主题名称</param>
        Task SwitchThemeAsync(string themeName);

        /// <summary>
        /// 应用用户偏好的主题设置
        /// </summary>
        /// <param name="fontSize">字体大小</param>
        /// <param name="isDarkMode">是否深色模式</param>
        Task ApplyThemeSettingsAsync(int fontSize = 14, bool isDarkMode = false);
    }
}
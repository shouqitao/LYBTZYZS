using System.Threading.Tasks;

namespace LYBT.Desktop.Core.Services.Settings
{
    /// <summary>
    /// UltraThink Phase H: 用户偏好设置服务接口
    /// 提供个性化用户体验设置的持久化管理
    /// </summary>
    public interface IUserPreferencesService
    {
        /// <summary>
        /// 获取用户偏好设置
        /// </summary>
        /// <param name="userId">用户ID</param>
        Task<UserPreferences> GetUserPreferencesAsync(string userId);

        /// <summary>
        /// 保存用户偏好设置
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="preferences">偏好设置</param>
        Task SaveUserPreferencesAsync(string userId, UserPreferences preferences);

        /// <summary>
        /// 重置用户偏好为默认设置
        /// </summary>
        /// <param name="userId">用户ID</param>
        Task ResetUserPreferencesAsync(string userId);

        /// <summary>
        /// 应用用户偏好设置到当前会话
        /// </summary>
        /// <param name="preferences">偏好设置</param>
        Task ApplyPreferencesAsync(UserPreferences preferences);
    }

    /// <summary>
    /// 用户偏好设置数据模型
    /// </summary>
    public class UserPreferences
    {
        /// <summary>
        /// 窗口设置
        /// </summary>
        public WindowSettings Window { get; set; } = new();

        /// <summary>
        /// 界面主题设置
        /// </summary>
        public ThemeSettings Theme { get; set; } = new();

        /// <summary>
        /// 工作流偏好
        /// </summary>
        public WorkflowSettings Workflow { get; set; } = new();

        /// <summary>
        /// 键盘快捷键设置
        /// </summary>
        public KeyboardSettings Keyboard { get; set; } = new();

        /// <summary>
        /// 设置最后更新时间
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 窗口相关设置
    /// </summary>
    public class WindowSettings
    {
        public double Width { get; set; } = 1200;
        public double Height { get; set; } = 800;
        public double Left { get; set; } = 100;
        public double Top { get; set; } = 100;
        public bool IsMaximized { get; set; } = false;
        public bool RememberPosition { get; set; } = true;
    }

    /// <summary>
    /// 主题相关设置
    /// </summary>
    public class ThemeSettings
    {
        public string ThemeName { get; set; } = "Default";
        public int FontSize { get; set; } = 14;
        public string FontFamily { get; set; } = "Microsoft YaHei";
        public bool UseDarkMode { get; set; } = false;
    }

    /// <summary>
    /// 工作流相关设置
    /// </summary>
    public class WorkflowSettings
    {
        public string DefaultWorkbench { get; set; } = "Auto";
        public bool AutoSaveEnabled { get; set; } = true;
        public int AutoSaveInterval { get; set; } = 300; // 秒
        public bool ShowConfirmationDialogs { get; set; } = true;
    }

    /// <summary>
    /// 键盘快捷键设置
    /// </summary>
    public class KeyboardSettings
    {
        public bool EnableKeyboardShortcuts { get; set; } = true;
        public Dictionary<string, string> CustomShortcuts { get; set; } = new();
    }
}

using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Core.Services.Settings
{

    /// <summary>
    /// UltraThink Phase H: 用户偏好设置服务实现
    /// 基于本地JSON文件的轻量级偏好设置持久化
    /// </summary>
    public class UserPreferencesService : IUserPreferencesService
    {
        private readonly ILogger<UserPreferencesService> _logger;
        private readonly string _preferencesDirectory;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public UserPreferencesService(ILogger<UserPreferencesService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 使用用户本地应用数据目录
            _preferencesDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LYBT", "UserPreferences");

            EnsureDirectoryExists();
        }

        /// <inheritdoc/>
        public async Task<UserPreferences> GetUserPreferencesAsync(string userId)
        {
            try
            {
                var filePath = GetPreferencesFilePath(userId);

                if (!File.Exists(filePath))
                {
                    _logger.LogDebug("用户偏好设置文件不存在，返回默认设置: {UserId}", userId);
                    return CreateDefaultPreferences();
                }

                var json = await File.ReadAllTextAsync(filePath);
                var preferences = JsonSerializer.Deserialize<UserPreferences>(json, JsonOptions);

                _logger.LogDebug("成功加载用户偏好设置: {UserId}", userId);
                return preferences ?? CreateDefaultPreferences();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载用户偏好设置失败: {UserId}", userId);
                return CreateDefaultPreferences();
            }
        }

        /// <inheritdoc/>
        public async Task SaveUserPreferencesAsync(string userId, UserPreferences preferences)
        {
            try
            {
                if (preferences == null)
                {
                    throw new ArgumentNullException(nameof(preferences));
                }

                preferences.LastModified = DateTime.Now;

                var filePath = GetPreferencesFilePath(userId);
                var json = JsonSerializer.Serialize(preferences, JsonOptions);

                await File.WriteAllTextAsync(filePath, json);

                _logger.LogDebug("成功保存用户偏好设置: {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存用户偏好设置失败: {UserId}", userId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task ResetUserPreferencesAsync(string userId)
        {
            try
            {
                var defaultPreferences = CreateDefaultPreferences();
                await SaveUserPreferencesAsync(userId, defaultPreferences);

                _logger.LogInformation("已重置用户偏好设置为默认值: {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重置用户偏好设置失败: {UserId}", userId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task ApplyPreferencesAsync(UserPreferences preferences)
        {
            try
            {
                // 应用窗口设置
                await ApplyWindowSettingsAsync(preferences.Window);

                // 应用主题设置
                await ApplyThemeSettingsAsync(preferences.Theme);

                // 应用工作流设置
                await ApplyWorkflowSettingsAsync(preferences.Workflow);

                _logger.LogDebug("成功应用用户偏好设置");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "应用用户偏好设置失败");
                throw;
            }
        }

        #region 私有方法

        private void EnsureDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(_preferencesDirectory))
                {
                    Directory.CreateDirectory(_preferencesDirectory);
                    _logger.LogDebug("创建偏好设置目录: {Directory}", _preferencesDirectory);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建偏好设置目录失败: {Directory}", _preferencesDirectory);
                throw;
            }
        }

        private string GetPreferencesFilePath(string userId)
        {
            var fileName = $"preferences_{userId}.json";
            return Path.Combine(_preferencesDirectory, fileName);
        }

        private static UserPreferences CreateDefaultPreferences()
        {
            return new UserPreferences
            {
                Window = new WindowSettings
                {
                    Width = 1200,
                    Height = 800,
                    IsMaximized = false,
                    RememberPosition = true
                },
                Theme = new ThemeSettings
                {
                    ThemeName = "Default",
                    FontSize = 14,
                    FontFamily = "Microsoft YaHei",
                    UseDarkMode = false
                },
                Workflow = new WorkflowSettings
                {
                    DefaultWorkbench = "Auto",
                    AutoSaveEnabled = true,
                    AutoSaveInterval = 300,
                    ShowConfirmationDialogs = true
                },
                Keyboard = new KeyboardSettings
                {
                    EnableKeyboardShortcuts = true,
                    CustomShortcuts = new Dictionary<string, string>()
                }
            };
        }

        private async Task ApplyWindowSettingsAsync(WindowSettings windowSettings)
        {
            // 在WPF应用中，窗口设置通常需要在主UI线程上应用
            // 这里提供接口，具体实现可以通过事件或回调来处理
            await Task.CompletedTask;
            _logger.LogDebug("窗口设置已准备应用");
        }

        private async Task ApplyThemeSettingsAsync(ThemeSettings themeSettings)
        {
            // 主题设置应用逻辑
            await Task.CompletedTask;
            _logger.LogDebug("主题设置已准备应用: {ThemeName}", themeSettings.ThemeName);
        }

        private async Task ApplyWorkflowSettingsAsync(WorkflowSettings workflowSettings)
        {
            // 工作流设置应用逻辑
            await Task.CompletedTask;
            _logger.LogDebug("工作流设置已准备应用");
        }

        #endregion 私有方法
    }
}

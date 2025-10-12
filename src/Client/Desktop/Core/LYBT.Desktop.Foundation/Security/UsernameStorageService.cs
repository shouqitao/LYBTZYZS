using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Services.Business
{
    /// <summary>
    /// 用户名存储服务实现 - Issue #861
    /// 使用 JSON 文件存储用户名和"记住用户名"设置
    /// 存储路径: %LOCALAPPDATA%\LYBT\Desktop\username.json
    /// </summary>
    public class UsernameStorageService : IUsernameStorageService
    {
        private readonly ILogger<UsernameStorageService> _logger;
        private readonly string _storageFilePath;
        private UsernameStorage? _cachedStorage; // 内存缓存

        public UsernameStorageService(ILogger<UsernameStorageService> logger)
        {
            _logger = logger;

            // 存储路径: %LOCALAPPDATA%\LYBT\Desktop\username.json
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var lybtFolder = Path.Combine(appDataPath, "LYBT", "Desktop");

            // 确保目录存在
            if (!Directory.Exists(lybtFolder))
            {
                Directory.CreateDirectory(lybtFolder);
            }

            _storageFilePath = Path.Combine(lybtFolder, "username.json");
        }

        /// <summary>
        /// 保存用户名
        /// </summary>
        public async Task SaveUsernameAsync(string username, bool rememberMe)
        {
            try
            {
                if (rememberMe)
                {
                    // 保存到文件
                    var storage = new UsernameStorage
                    {
                        Username = username,
                        RememberMe = true
                    };

                    var json = JsonSerializer.Serialize(storage, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    });

                    await File.WriteAllTextAsync(_storageFilePath, json, System.Text.Encoding.UTF8);
                    _cachedStorage = storage;
                    _logger.LogInformation("用户名已保存: {Username}", username);
                }
                else
                {
                    // 不记住用户名，删除文件
                    await ClearUsernameAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存用户名失败");
                throw;
            }
        }

        /// <summary>
        /// 获取已保存的用户名
        /// </summary>
        public async Task<string?> GetSavedUsernameAsync()
        {
            try
            {
                // 优先返回内存缓存
                if (_cachedStorage != null)
                {
                    return _cachedStorage.RememberMe ? _cachedStorage.Username : null;
                }

                // 从文件加载
                if (File.Exists(_storageFilePath))
                {
                    var json = await File.ReadAllTextAsync(_storageFilePath, System.Text.Encoding.UTF8);
                    _cachedStorage = JsonSerializer.Deserialize<UsernameStorage>(json);

                    if (_cachedStorage?.RememberMe == true)
                    {
                        _logger.LogInformation("从本地加载用户名: {Username}", _cachedStorage.Username);
                        return _cachedStorage.Username;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "读取用户名失败");
                return null;
            }
        }

        /// <summary>
        /// 检查是否启用了"记住用户名"
        /// </summary>
        public async Task<bool> IsRememberMeEnabledAsync()
        {
            try
            {
                // 优先检查内存缓存
                if (_cachedStorage != null)
                {
                    return _cachedStorage.RememberMe;
                }

                // 从文件加载
                if (File.Exists(_storageFilePath))
                {
                    var json = await File.ReadAllTextAsync(_storageFilePath, System.Text.Encoding.UTF8);
                    _cachedStorage = JsonSerializer.Deserialize<UsernameStorage>(json);
                    return _cachedStorage?.RememberMe ?? false;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查RememberMe状态失败");
                return false;
            }
        }

        /// <summary>
        /// 清除已保存的用户名
        /// </summary>
        public async Task ClearUsernameAsync()
        {
            try
            {
                // 清除内存缓存
                _cachedStorage = null;

                // 删除文件
                if (File.Exists(_storageFilePath))
                {
                    File.Delete(_storageFilePath);
                    _logger.LogInformation("已清除保存的用户名");
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除用户名失败");
                throw;
            }
        }

        /// <summary>
        /// 用户名存储数据结构
        /// </summary>
        private class UsernameStorage
        {
            public string Username { get; set; } = string.Empty;
            public bool RememberMe { get; set; }
        }
    }
}

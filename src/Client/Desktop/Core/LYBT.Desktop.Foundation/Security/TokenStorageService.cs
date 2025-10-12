using System.IO;
using System.Text.Json;
using LYBT.Shared.Models.Contracts.Auth;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Foundation.Security
{
    /// <summary>
    /// Token 存储服务实现 - 使用 JSON 文件存储认证信息
    /// MVP 实现:简单文件存储,生产环境可升级为 Windows Credential Manager
    /// </summary>
    public class TokenStorageService : ITokenStorageService
    {
        private readonly ILogger<TokenStorageService> _logger;
        private readonly string _storageFilePath;
        private LoginResponse? _cachedLoginResponse; // 内存缓存

        public TokenStorageService(ILogger<TokenStorageService> logger)
        {
            _logger = logger;

            // 存储路径: %LOCALAPPDATA%\LYBT\Desktop\auth.json
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var lybtFolder = Path.Combine(appDataPath, "LYBT", "Desktop");

            // 确保目录存在
            if (!Directory.Exists(lybtFolder))
            {
                Directory.CreateDirectory(lybtFolder);
            }

            _storageFilePath = Path.Combine(lybtFolder, "auth.json");
        }

        /// <summary>
        /// 保存认证信息
        /// </summary>
        public async Task SaveAuthenticationAsync(LoginResponse loginResponse, bool rememberMe)
        {
            try
            {
                // 更新内存缓存
                _cachedLoginResponse = loginResponse;

                // 如果 RememberMe=true,持久化到文件
                if (rememberMe)
                {
                    var json = JsonSerializer.Serialize(loginResponse, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    });

                    await File.WriteAllTextAsync(_storageFilePath, json, System.Text.Encoding.UTF8);
                    _logger.LogInformation("认证信息已保存到本地文件");
                }
                else
                {
                    // RememberMe=false,删除持久化文件(仅保留内存缓存)
                    if (File.Exists(_storageFilePath))
                    {
                        File.Delete(_storageFilePath);
                        _logger.LogInformation("已删除持久化认证文件(仅内存缓存)");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存认证信息失败");
                throw;
            }
        }

        /// <summary>
        /// 获取当前保存的Token
        /// </summary>
        public async Task<string?> GetTokenAsync()
        {
            var loginResponse = await GetLoginResponseAsync();
            return loginResponse?.Token;
        }

        /// <summary>
        /// 获取当前保存的RefreshToken
        /// </summary>
        public async Task<string?> GetRefreshTokenAsync()
        {
            var loginResponse = await GetLoginResponseAsync();
            return loginResponse?.RefreshToken;
        }

        /// <summary>
        /// 获取完整的登录响应数据
        /// </summary>
        public async Task<LoginResponse?> GetLoginResponseAsync()
        {
            try
            {
                // 优先返回内存缓存
                if (_cachedLoginResponse != null)
                {
                    return _cachedLoginResponse;
                }

                // 从文件加载
                if (File.Exists(_storageFilePath))
                {
                    var json = await File.ReadAllTextAsync(_storageFilePath, System.Text.Encoding.UTF8);
                    _cachedLoginResponse = JsonSerializer.Deserialize<LoginResponse>(json);
                    _logger.LogInformation("从本地文件加载认证信息成功");
                    return _cachedLoginResponse;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "读取认证信息失败");
                return null;
            }
        }

        /// <summary>
        /// 清除所有认证信息
        /// </summary>
        public async Task ClearAuthenticationAsync()
        {
            try
            {
                // 清除内存缓存
                _cachedLoginResponse = null;

                // 删除持久化文件
                if (File.Exists(_storageFilePath))
                {
                    File.Delete(_storageFilePath);
                    _logger.LogInformation("已清除本地认证文件");
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除认证信息失败");
                throw;
            }
        }

        /// <summary>
        /// 检查Token是否过期
        /// </summary>
        public async Task<bool> IsTokenExpiredAsync()
        {
            var loginResponse = await GetLoginResponseAsync();

            if (loginResponse == null)
            {
                return true; // 无Token视为已过期
            }

            // 检查过期时间(留5分钟缓冲)
            var isExpired = loginResponse.ExpiresAt <= DateTime.UtcNow.AddMinutes(5);

            if (isExpired)
            {
                _logger.LogWarning("Token已过期或即将过期,ExpiresAt: {ExpiresAt}", loginResponse.ExpiresAt);
            }

            return isExpired;
        }
    }
}

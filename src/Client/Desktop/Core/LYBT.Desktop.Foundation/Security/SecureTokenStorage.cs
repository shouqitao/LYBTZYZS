using LYBT.Shared.Models.Contracts.Auth;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LYBT.Desktop.Foundation.Security
{
    /// <summary>
    /// 安全Token存储实现 - 使用Windows DPAPI加密存储
    /// </summary>
    /// <remarks>
    /// Issue #1862: Token认证安全重构 - 使用DPAPI加密存储
    ///
    /// 设计要点：
    /// 1. 使用Windows DPAPI（ProtectedData.Protect）加密Token
    /// 2. 保护范围：CurrentUser级别
    /// 3. 存储路径：%LOCALAPPDATA%\LYBTZYZS\tokens.dat
    /// 4. 降级策略：DPAPI失败时降级为明文存储并记录警告
    /// </remarks>
    public class SecureTokenStorage : ITokenStorage
    {
        private readonly ILogger<SecureTokenStorage> _logger;
        private readonly string _storageFilePath;

        public SecureTokenStorage(ILogger<SecureTokenStorage> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 设置存储路径：%LOCALAPPDATA%\LYBTZYZS\tokens.dat
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var lybtFolder = Path.Combine(appDataPath, "LYBTZYZS");
            _storageFilePath = Path.Combine(lybtFolder, "tokens.dat");

            // 确保目录存在
            Directory.CreateDirectory(lybtFolder);
        }

        /// <summary>
        /// 保存Token到加密存储
        /// </summary>
        public async Task SaveTokenAsync(LoginResponse loginResponse)
        {
            if (loginResponse == null)
            {
                throw new ArgumentNullException(nameof(loginResponse));
            }

            try
            {
                // 序列化为JSON
                var json = JsonSerializer.Serialize(loginResponse, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                // 转换为字节数组
                var plainBytes = Encoding.UTF8.GetBytes(json);

                // 使用DPAPI加密
                var encryptedBytes = ProtectedData.Protect(
                    plainBytes,
                    null,
                    DataProtectionScope.CurrentUser);

                // 写入加密文件
                await File.WriteAllBytesAsync(_storageFilePath, encryptedBytes);

                _logger.LogDebug("Token已加密存储到 {FilePath}", _storageFilePath);
            }
            catch (CryptographicException ex)
            {
                // 降级策略：DPAPI加密失败，降级为明文存储
                _logger.LogWarning(ex, "DPAPI加密失败，降级为明文存储。请检查Windows用户配置文件是否正常。");

                // 明文存储
                var json = JsonSerializer.Serialize(loginResponse, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await File.WriteAllTextAsync(_storageFilePath, json, Encoding.UTF8);

                _logger.LogWarning("Token已以明文形式存储，存在安全风险");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存Token时发生错误");
                throw;
            }
        }

        /// <summary>
        /// 从加密存储加载Token
        /// </summary>
        public async Task<LoginResponse?> LoadTokenAsync()
        {
            try
            {
                // 检查文件是否存在
                if (!File.Exists(_storageFilePath))
                {
                    _logger.LogDebug("Token存储文件不存在: {FilePath}", _storageFilePath);
                    return null;
                }

                // 读取加密文件
                var encryptedBytes = await File.ReadAllBytesAsync(_storageFilePath);

                if (encryptedBytes == null || encryptedBytes.Length == 0)
                {
                    _logger.LogWarning("Token存储文件为空");
                    return null;
                }

                try
                {
                    // 尝试使用DPAPI解密
                    var plainBytes = ProtectedData.Unprotect(
                        encryptedBytes,
                        null,
                        DataProtectionScope.CurrentUser);

                    // 转换为JSON字符串
                    var json = Encoding.UTF8.GetString(plainBytes);

                    // 反序列化
                    var loginResponse = JsonSerializer.Deserialize<LoginResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    _logger.LogDebug("Token已从加密存储加载");
                    return loginResponse;
                }
                catch (CryptographicException ex)
                {
                    // 尝试以明文方式读取（降级模式）
                    _logger.LogWarning(ex, "DPAPI解密失败，尝试以明文方式读取");

                    var json = Encoding.UTF8.GetString(encryptedBytes);
                    var loginResponse = JsonSerializer.Deserialize<LoginResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    if (loginResponse != null)
                    {
                        _logger.LogWarning("Token以明文形式读取成功，建议重新登录以使用加密存储");
                        return loginResponse;
                    }

                    throw;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Token反序列化失败，数据可能已损坏");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载Token时发生错误");
                return null;
            }
        }

        /// <summary>
        /// 清除加密存储中的Token
        /// </summary>
        public async Task ClearTokenAsync()
        {
            try
            {
                if (File.Exists(_storageFilePath))
                {
                    File.Delete(_storageFilePath);
                    _logger.LogDebug("Token存储文件已删除: {FilePath}", _storageFilePath);
                }
                else
                {
                    _logger.LogDebug("Token存储文件不存在，无需删除");
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除Token存储时发生错误");
                throw;
            }
        }
    }
}

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Foundation.Security
{
    /// <summary>
    /// 安全凭据存储服务实现 - Issue #1246
    /// 使用 Windows DPAPI 加密存储用户名和密码
    /// 存储路径: %LOCALAPPDATA%\LYBT\Desktop\credentials.dat
    ///
    /// 安全特性：
    /// - 密码使用 DPAPI（DataProtectionScope.CurrentUser）加密
    /// - 只有当前 Windows 用户能解密
    /// - 加密数据无法复制到其他电脑使用
    /// </summary>
    public class SecureCredentialStorage : ISecureCredentialStorage
    {
        private readonly ILogger<SecureCredentialStorage> _logger;
        private readonly string _storageFilePath;
        private CredentialCache? _cachedCredentials; // 内存缓存（密码已解密）

        public SecureCredentialStorage(ILogger<SecureCredentialStorage> logger)
        {
            _logger = logger;

            // 存储路径: %LOCALAPPDATA%\LYBT\Desktop\credentials.dat
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var lybtFolder = Path.Combine(appDataPath, "LYBT", "Desktop");

            // 确保目录存在
            if (!Directory.Exists(lybtFolder))
            {
                Directory.CreateDirectory(lybtFolder);
            }

            _storageFilePath = Path.Combine(lybtFolder, "credentials.dat");
        }

        /// <summary>
        /// 保存凭据（密码使用 DPAPI 加密）
        /// </summary>
        public async Task SaveCredentialsAsync(string username, string password, bool rememberPassword)
        {
            try
            {
                if (rememberPassword && !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    // 1. 加密密码（使用 DPAPI）
                    var passwordBytes = Encoding.UTF8.GetBytes(password);
                    var encryptedPassword = ProtectedData.Protect(
                        passwordBytes,
                        null, // entropy: 不使用额外的熵（足够安全，因为绑定到 Windows 用户）
                        DataProtectionScope.CurrentUser); // 只有当前用户能解密

                    // 2. 构建存储对象
                    var storage = new CredentialStorage
                    {
                        Username = username,
                        EncryptedPassword = Convert.ToBase64String(encryptedPassword),
                        RememberPassword = true
                    };

                    // 3. 序列化为 JSON 并保存
                    var json = JsonSerializer.Serialize(storage, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    });

                    await File.WriteAllTextAsync(_storageFilePath, json, Encoding.UTF8);

                    // 4. 缓存到内存（已解密状态）
                    _cachedCredentials = new CredentialCache
                    {
                        Username = username,
                        Password = password,
                        RememberPassword = true
                    };

                    _logger.LogInformation("凭据已保存并加密（DPAPI）: {Username}", username);
                }
                else
                {
                    // 不记住密码，删除文件
                    await ClearCredentialsAsync();
                }
            }
            catch (CryptographicException ex)
            {
                _logger.LogError(ex, "DPAPI 加密失败");
                throw new InvalidOperationException("密码加密失败，请检查系统权限", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存凭据失败");
                throw;
            }
        }

        /// <summary>
        /// 加载已保存的凭据（自动解密）
        /// </summary>
        public async Task<(string Username, string Password)?> LoadCredentialsAsync()
        {
            try
            {
                // 1. 优先返回内存缓存
                if (_cachedCredentials != null && _cachedCredentials.RememberPassword)
                {
                    return (_cachedCredentials.Username, _cachedCredentials.Password);
                }

                // 2. 从文件加载
                if (!File.Exists(_storageFilePath))
                {
                    return null;
                }

                var json = await File.ReadAllTextAsync(_storageFilePath, Encoding.UTF8);
                var storage = JsonSerializer.Deserialize<CredentialStorage>(json);

                if (storage == null || !storage.RememberPassword || string.IsNullOrEmpty(storage.EncryptedPassword))
                {
                    return null;
                }

                // 3. 解密密码（使用 DPAPI）
                var encryptedBytes = Convert.FromBase64String(storage.EncryptedPassword);
                var decryptedBytes = ProtectedData.Unprotect(
                    encryptedBytes,
                    null, // entropy
                    DataProtectionScope.CurrentUser);

                var password = Encoding.UTF8.GetString(decryptedBytes);

                // 4. 缓存到内存
                _cachedCredentials = new CredentialCache
                {
                    Username = storage.Username,
                    Password = password,
                    RememberPassword = true
                };

                _logger.LogInformation("从本地加载凭据（已解密）: {Username}", storage.Username);
                return (storage.Username, password);
            }
            catch (CryptographicException ex)
            {
                _logger.LogError(ex, "DPAPI 解密失败（可能是其他 Windows 用户加密的数据）");
                // 解密失败时删除损坏的文件
                await ClearCredentialsAsync();
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "读取凭据失败");
                return null;
            }
        }

        /// <summary>
        /// 检查是否启用了"记住密码"
        /// </summary>
        public async Task<bool> IsRememberPasswordEnabledAsync()
        {
            try
            {
                // 1. 优先检查内存缓存
                if (_cachedCredentials != null)
                {
                    return _cachedCredentials.RememberPassword;
                }

                // 2. 从文件加载
                if (!File.Exists(_storageFilePath))
                {
                    return false;
                }

                var json = await File.ReadAllTextAsync(_storageFilePath, Encoding.UTF8);
                var storage = JsonSerializer.Deserialize<CredentialStorage>(json);
                return storage?.RememberPassword ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查 RememberPassword 状态失败");
                return false;
            }
        }

        /// <summary>
        /// 清除已保存的凭据
        /// </summary>
        public async Task ClearCredentialsAsync()
        {
            try
            {
                // 1. 清除内存缓存
                _cachedCredentials = null;

                // 2. 删除文件
                if (File.Exists(_storageFilePath))
                {
                    File.Delete(_storageFilePath);
                    _logger.LogInformation("已清除保存的凭据");
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除凭据失败");
                throw;
            }
        }

        #region 数据结构

        /// <summary>
        /// 持久化存储结构（密码已加密）
        /// </summary>
        private class CredentialStorage
        {
            public string Username { get; set; } = string.Empty;
            public string EncryptedPassword { get; set; } = string.Empty; // Base64 编码的 DPAPI 加密数据
            public bool RememberPassword { get; set; }
        }

        /// <summary>
        /// 内存缓存结构（密码已解密）
        /// </summary>
        private class CredentialCache
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty; // 明文密码（仅内存中）
            public bool RememberPassword { get; set; }
        }

        #endregion
    }
}

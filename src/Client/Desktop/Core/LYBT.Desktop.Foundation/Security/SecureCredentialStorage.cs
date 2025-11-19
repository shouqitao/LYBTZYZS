using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
                _logger.LogInformation(" [SaveCredentials] 开始保存凭据 - UserName: {UserName}, RememberPassword: {RememberPassword}", username, rememberPassword);

                if (rememberPassword && !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    _logger.LogInformation(" [SaveCredentials] 参数校验通过，开始加密密码");
                    // 1. 加密密码（使用 DPAPI）
                    var encryptedPassword = EncryptPassword(password);
                    _logger.LogInformation(" [SaveCredentials] 密码加密成功，Base64 长度: {Length}", encryptedPassword.Length);

                    // 2. 构建存储对象
                    var storage = new CredentialStorage
                    {
                        Username = username,
                        EncryptedPassword = encryptedPassword,
                        RememberPassword = true
                    };

                    // 3. 序列化为 JSON 并保存
                    _logger.LogInformation(" [SaveCredentials] 准备写入文件: {Path}", _storageFilePath);
                    await SaveStorageToFileAsync(storage);
                    _logger.LogInformation(" [SaveCredentials] 文件写入成功");

                    // 4. 缓存到内存（已解密状态）
                    UpdateCache(username, password, true);

                    _logger.LogInformation(" [SaveCredentials] 凭据已保存并加密（DPAPI）: {UserName}, 文件路径: {Path}", username, _storageFilePath);
                }
                else
                {
                    _logger.LogWarning(" [SaveCredentials] 参数校验失败 - RememberPassword: {RememberPassword}, Username空: {UsernameEmpty}, Password空: {PasswordEmpty}",
                        rememberPassword, string.IsNullOrEmpty(username), string.IsNullOrEmpty(password));
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
                _logger.LogInformation("📂 [LoadCredentials] 开始加载凭据，文件路径: {Path}", _storageFilePath);

                // 1. 优先返回内存缓存
                if (_cachedCredentials != null && _cachedCredentials.RememberPassword)
                {
                    _logger.LogInformation(" [LoadCredentials] 从内存缓存加载: {UserName}", _cachedCredentials.Username);
                    return (_cachedCredentials.Username, _cachedCredentials.Password);
                }

                // 2. 从文件加载
                var storage = await LoadStorageFromFileAsync();
                if (storage == null) return null;

                // 3. 解密密码（使用 DPAPI）
                _logger.LogInformation("🔓 [LoadCredentials] 开始解密，加密数据长度: {Length}", storage.EncryptedPassword.Length);
                var password = DecryptPassword(storage.EncryptedPassword);
                _logger.LogInformation(" [LoadCredentials] 密码解密成功");

                // 4. 缓存到内存
                _cachedCredentials = new CredentialCache
                {
                    Username = storage.Username,
                    Password = password,
                    RememberPassword = true
                };

                _logger.LogInformation(" [LoadCredentials] 从本地加载凭据（已解密）: {UserName}", storage.Username);
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

        #region 私有辅助方法

        /// <summary>
        /// 使用DPAPI加密密码
        /// </summary>
        private string EncryptPassword(string password)
        {
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var encryptedPassword = ProtectedData.Protect(
                passwordBytes,
                null,
                DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedPassword);
        }

        /// <summary>
        /// 使用DPAPI解密密码
        /// </summary>
        private string DecryptPassword(string encryptedPasswordBase64)
        {
            var encryptedBytes = Convert.FromBase64String(encryptedPasswordBase64);
            var decryptedBytes = ProtectedData.Unprotect(
                encryptedBytes,
                null,
                DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decryptedBytes);
        }

        /// <summary>
        /// 从文件加载凭据存储对象
        /// </summary>
        private async Task<CredentialStorage?> LoadStorageFromFileAsync()
        {
            if (!File.Exists(_storageFilePath))
            {
                _logger.LogWarning(" [LoadCredentials] 文件不存在: {Path}", _storageFilePath);
                return null;
            }

            _logger.LogInformation("📖 [LoadCredentials] 文件存在，开始读取");
            var json = await File.ReadAllTextAsync(_storageFilePath, Encoding.UTF8);
            _logger.LogInformation("📄 [LoadCredentials] JSON读取成功，长度: {Length}", json.Length);

            var storage = JsonSerializer.Deserialize<CredentialStorage>(json);
            if (storage == null || !storage.RememberPassword || string.IsNullOrEmpty(storage.EncryptedPassword))
            {
                _logger.LogWarning(" [LoadCredentials] 数据校验失败");
                return null;
            }

            _logger.LogInformation(" [LoadCredentials] JSON反序列化成功，UserName: {UserName}", storage.Username);
            return storage;
        }

        /// <summary>
        /// 更新内存缓存
        /// </summary>
        private void UpdateCache(string username, string password, bool rememberPassword)
        {
            _cachedCredentials = new CredentialCache
            {
                Username = username,
                Password = password,
                RememberPassword = rememberPassword
            };
        }

        /// <summary>
        /// 保存凭据到文件
        /// </summary>
        private async Task SaveStorageToFileAsync(CredentialStorage storage)
        {
            var json = JsonSerializer.Serialize(storage, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            await File.WriteAllTextAsync(_storageFilePath, json, Encoding.UTF8);
        }

        #endregion

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

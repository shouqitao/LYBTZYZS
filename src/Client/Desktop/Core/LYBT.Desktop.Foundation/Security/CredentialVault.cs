using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Foundation.Security
{
    /// <summary>
    /// 凭据保险库实现 - 安全存储AutoLoginToken
    /// OpenSpec: refactor-login-authentication (CVT-001, CVT-002)
    ///
    /// 安全特性：
    /// - 使用 DPAPI（DataProtectionScope.CurrentUser）加密
    /// - HMAC-SHA256 完整性校验防止篡改
    /// - 只有当前 Windows 用户能解密
    /// - 加密数据无法复制到其他电脑使用
    ///
    /// 存储路径: %LOCALAPPDATA%\LYBT\Desktop\vault.dat
    /// </summary>
    public class CredentialVault : ICredentialVault
    {
        private readonly ILogger<CredentialVault> _logger;
        private readonly string _vaultFilePath;
        private readonly string _oldCredentialsPath;
        private readonly object _lock = new();

        // HMAC密钥派生源（结合机器名和用户名增强安全性）
        private readonly byte[] _hmacKeySource;

        public CredentialVault(ILogger<CredentialVault> logger)
        {
            _logger = logger;

            // 存储路径: %LOCALAPPDATA%\LYBT\Desktop\vault.dat
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var lybtFolder = Path.Combine(appDataPath, "LYBT", "Desktop");

            // 确保目录存在
            if (!Directory.Exists(lybtFolder))
            {
                Directory.CreateDirectory(lybtFolder);
            }

            _vaultFilePath = Path.Combine(lybtFolder, "vault.dat");
            _oldCredentialsPath = Path.Combine(lybtFolder, "credentials.dat");

            // 派生HMAC密钥源（基于机器和用户上下文）
            var keyMaterial = $"LYBT_VAULT_{Environment.MachineName}_{Environment.UserName}";
            _hmacKeySource = SHA256.HashData(Encoding.UTF8.GetBytes(keyMaterial));
        }

        /// <summary>
        /// 保存AutoLoginToken
        /// </summary>
        public async Task<bool> SaveAutoLoginTokenAsync(string username, string autoLoginToken)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("用户名不能为空", nameof(username));
            }
            if (string.IsNullOrWhiteSpace(autoLoginToken))
            {
                throw new ArgumentException("AutoLoginToken不能为空", nameof(autoLoginToken));
            }

            try
            {
                _logger.LogInformation("开始保存AutoLoginToken - UserName: {UserName}", username);

                // 1. 加载现有数据（支持多用户）
                var vault = await LoadVaultAsync() ?? new VaultStorage();

                // 2. 加密Token
                var encryptedToken = EncryptWithDpapi(autoLoginToken);

                // 3. 计算HMAC
                var hmac = ComputeHmac(username, encryptedToken);

                // 4. 更新或添加条目
                var entry = new VaultEntry
                {
                    Username = username,
                    EncryptedAutoLoginToken = encryptedToken,
                    Hmac = hmac,
                    CreatedAt = DateTime.UtcNow
                };

                // 查找现有条目
                var existingIndex = vault.Entries.FindIndex(e =>
                    string.Equals(e.Username, username, StringComparison.OrdinalIgnoreCase));

                if (existingIndex >= 0)
                {
                    vault.Entries[existingIndex] = entry;
                }
                else
                {
                    vault.Entries.Add(entry);
                }

                // 5. 保存到文件
                await SaveVaultAsync(vault);

                _logger.LogInformation("AutoLoginToken已保存 - UserName: {UserName}", username);
                return true;
            }
            catch (CryptographicException ex)
            {
                _logger.LogError(ex, "DPAPI加密失败");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存AutoLoginToken失败 - UserName: {UserName}", username);
                return false;
            }
        }

        /// <summary>
        /// 获取已保存的AutoLoginToken
        /// </summary>
        public async Task<string?> GetAutoLoginTokenAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return null;
            }

            try
            {
                var vault = await LoadVaultAsync();
                if (vault == null)
                {
                    return null;
                }

                var entry = vault.Entries.Find(e =>
                    string.Equals(e.Username, username, StringComparison.OrdinalIgnoreCase));

                if (entry == null || string.IsNullOrEmpty(entry.EncryptedAutoLoginToken))
                {
                    _logger.LogDebug("未找到AutoLoginToken - UserName: {UserName}", username);
                    return null;
                }

                // 验证HMAC完整性
                var expectedHmac = ComputeHmac(username, entry.EncryptedAutoLoginToken);
                if (!string.Equals(entry.Hmac, expectedHmac, StringComparison.Ordinal))
                {
                    _logger.LogWarning("HMAC校验失败，数据可能被篡改 - UserName: {UserName}", username);
                    return null;
                }

                // 解密Token
                var token = DecryptWithDpapi(entry.EncryptedAutoLoginToken);
                _logger.LogDebug("AutoLoginToken已加载 - UserName: {UserName}", username);
                return token;
            }
            catch (CryptographicException ex)
            {
                _logger.LogError(ex, "DPAPI解密失败（可能是其他Windows用户的数据）- UserName: {UserName}", username);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取AutoLoginToken失败 - UserName: {UserName}", username);
                return null;
            }
        }

        /// <summary>
        /// 清除指定用户的凭据
        /// </summary>
        public async Task<bool> ClearCredentialsAsync(string? username = null)
        {
            try
            {
                if (username == null)
                {
                    // 清除所有：删除文件
                    lock (_lock)
                    {
                        if (File.Exists(_vaultFilePath))
                        {
                            File.Delete(_vaultFilePath);
                            _logger.LogInformation("已清除所有凭据");
                        }
                    }
                    return true;
                }

                // 清除指定用户
                var vault = await LoadVaultAsync();
                if (vault == null)
                {
                    return true;
                }

                var removed = vault.Entries.RemoveAll(e =>
                    string.Equals(e.Username, username, StringComparison.OrdinalIgnoreCase));

                if (removed > 0)
                {
                    await SaveVaultAsync(vault);
                    _logger.LogInformation("已清除用户凭据 - UserName: {UserName}", username);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除凭据失败 - UserName: {UserName}", username ?? "(all)");
                return false;
            }
        }

        /// <summary>
        /// 验证存储数据的完整性
        /// </summary>
        public async Task<bool> VerifyIntegrityAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return false;
            }

            try
            {
                var vault = await LoadVaultAsync();
                if (vault == null)
                {
                    return false;
                }

                var entry = vault.Entries.Find(e =>
                    string.Equals(e.Username, username, StringComparison.OrdinalIgnoreCase));

                if (entry == null || string.IsNullOrEmpty(entry.EncryptedAutoLoginToken))
                {
                    return false;
                }

                // 验证HMAC
                var expectedHmac = ComputeHmac(username, entry.EncryptedAutoLoginToken);
                return string.Equals(entry.Hmac, expectedHmac, StringComparison.Ordinal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证完整性失败 - UserName: {UserName}", username);
                return false;
            }
        }

        /// <summary>
        /// 迁移旧格式凭据
        /// 注意：旧格式存储的是密码，新格式需要AutoLoginToken
        /// 此方法仅标记旧文件已处理，实际迁移需要用户重新登录获取AutoLoginToken
        /// </summary>
        public async Task MigrateOldFormatAsync()
        {
            try
            {
                if (!File.Exists(_oldCredentialsPath))
                {
                    _logger.LogDebug("无旧格式凭据需要迁移");
                    return;
                }

                // 检查是否已经迁移过（vault.dat存在）
                if (File.Exists(_vaultFilePath))
                {
                    _logger.LogDebug("已存在vault.dat，跳过迁移");
                    return;
                }

                _logger.LogInformation("检测到旧格式凭据文件，准备迁移提示");

                // 读取旧格式获取用户名（用于显示迁移提示）
                var json = await File.ReadAllTextAsync(_oldCredentialsPath, Encoding.UTF8);
                var oldData = JsonSerializer.Deserialize<OldCredentialFormat>(json);

                if (oldData != null && !string.IsNullOrEmpty(oldData.Username))
                {
                    _logger.LogInformation("发现旧格式凭据 - UserName: {UserName}，需要重新登录以启用新的安全存储",
                        oldData.Username);

                    // 创建一个空的vault标记迁移状态
                    var vault = new VaultStorage
                    {
                        MigratedFromOldFormat = true,
                        MigrationUsername = oldData.Username
                    };
                    await SaveVaultAsync(vault);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "迁移旧格式凭据时出现问题，将忽略旧数据");
            }
        }

        /// <summary>
        /// 检查是否存在有效的AutoLoginToken
        /// </summary>
        public async Task<bool> HasValidTokenAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return false;
            }

            try
            {
                var vault = await LoadVaultAsync();
                if (vault == null)
                {
                    return false;
                }

                var entry = vault.Entries.Find(e =>
                    string.Equals(e.Username, username, StringComparison.OrdinalIgnoreCase));

                if (entry == null || string.IsNullOrEmpty(entry.EncryptedAutoLoginToken))
                {
                    return false;
                }

                // 验证HMAC完整性
                var expectedHmac = ComputeHmac(username, entry.EncryptedAutoLoginToken);
                if (!string.Equals(entry.Hmac, expectedHmac, StringComparison.Ordinal))
                {
                    return false;
                }

                // 尝试解密验证
                try
                {
                    var token = DecryptWithDpapi(entry.EncryptedAutoLoginToken);
                    return !string.IsNullOrEmpty(token);
                }
                catch
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查有效Token失败 - UserName: {UserName}", username);
                return false;
            }
        }

        #region 私有辅助方法

        /// <summary>
        /// 使用DPAPI加密
        /// </summary>
        private string EncryptWithDpapi(string plainText)
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = ProtectedData.Protect(
                plainBytes,
                null,
                DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedBytes);
        }

        /// <summary>
        /// 使用DPAPI解密
        /// </summary>
        private string DecryptWithDpapi(string encryptedBase64)
        {
            var encryptedBytes = Convert.FromBase64String(encryptedBase64);
            var decryptedBytes = ProtectedData.Unprotect(
                encryptedBytes,
                null,
                DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decryptedBytes);
        }

        /// <summary>
        /// 计算HMAC-SHA256
        /// </summary>
        private string ComputeHmac(string username, string encryptedData)
        {
            var dataToSign = $"{username.ToLowerInvariant()}:{encryptedData}";
            var dataBytes = Encoding.UTF8.GetBytes(dataToSign);

            using var hmac = new HMACSHA256(_hmacKeySource);
            var hashBytes = hmac.ComputeHash(dataBytes);
            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// 加载Vault数据
        /// </summary>
        private Task<VaultStorage?> LoadVaultAsync()
        {
            return Task.Run(() =>
            {
                lock (_lock)
                {
                    if (!File.Exists(_vaultFilePath))
                    {
                        return null;
                    }

                    try
                    {
                        var json = File.ReadAllText(_vaultFilePath, Encoding.UTF8);
                        return JsonSerializer.Deserialize<VaultStorage>(json);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Vault文件格式错误，将重置");
                        return null;
                    }
                }
            });
        }

        /// <summary>
        /// 保存Vault数据
        /// </summary>
        private Task SaveVaultAsync(VaultStorage vault)
        {
            return Task.Run(() =>
            {
                var json = JsonSerializer.Serialize(vault, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                lock (_lock)
                {
                    File.WriteAllText(_vaultFilePath, json, Encoding.UTF8);
                }
            });
        }

        #endregion

        #region 数据结构

        /// <summary>
        /// Vault存储结构
        /// </summary>
        private class VaultStorage
        {
            public int Version { get; set; } = 1;
            public List<VaultEntry> Entries { get; set; } = new();
            public bool MigratedFromOldFormat { get; set; }
            public string? MigrationUsername { get; set; }
        }

        /// <summary>
        /// 单个用户的Vault条目
        /// </summary>
        private class VaultEntry
        {
            public string Username { get; set; } = string.Empty;
            public string EncryptedAutoLoginToken { get; set; } = string.Empty;
            public string Hmac { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
        }

        /// <summary>
        /// 旧格式凭据结构（用于迁移）
        /// </summary>
        private class OldCredentialFormat
        {
            public string Username { get; set; } = string.Empty;
            public string EncryptedPassword { get; set; } = string.Empty;
            public bool RememberPassword { get; set; }
        }

        #endregion
    }
}

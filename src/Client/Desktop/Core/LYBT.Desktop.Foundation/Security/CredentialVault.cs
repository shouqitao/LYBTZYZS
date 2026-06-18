using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Foundation.Security
{
    /// <summary>
    /// 凭据保险库实现 - 安全存储密码和AutoLoginToken
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
        private readonly DpapiProtector _protector;
        private readonly CredentialStorage _storage;

        public CredentialVault(ILogger<CredentialVault> logger)
        {
            _logger = logger;
            _protector = new DpapiProtector();
            _storage = new CredentialStorage(logger);
        }

        #region 密码存储

        /// <summary>
        /// 保存密码（DPAPI加密）
        /// </summary>
        public async Task<bool> SavePasswordAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("用户名不能为空", nameof(username));
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("密码不能为空", nameof(password));
            }

            try
            {
                _logger.LogInformation("开始保存密码 - UserName: {UserName}", username);

                // 1. 加载现有数据
                var vault = await _storage.LoadVaultAsync() ?? new VaultStorage();

                // 2. 加密密码
                var encryptedPassword = _protector.Encrypt(password);

                // 3. 计算HMAC
                var hmac = _protector.ComputeHmac(username + "_password", encryptedPassword);

                // 4. 查找或创建条目
                var entry = vault.Entries.Find(e =>
                    string.Equals(e.Username, username, StringComparison.OrdinalIgnoreCase));

                if (entry == null)
                {
                    entry = new VaultEntry { Username = username, CreatedAt = DateTime.UtcNow };
                    vault.Entries.Add(entry);
                }

                // 5. 更新密码字段
                entry.EncryptedPassword = encryptedPassword;
                entry.PasswordHmac = hmac;
                entry.PasswordSavedAt = DateTime.UtcNow;

                // 6. 保存到文件
                await _storage.SaveVaultAsync(vault);

                _logger.LogInformation("密码已保存 - UserName: {UserName}", username);
                return true;
            }
            catch (CryptographicException ex)
            {
                _logger.LogError(ex, "DPAPI加密失败");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存密码失败 - UserName: {UserName}", username);
                return false;
            }
        }

        /// <summary>
        /// 获取已保存的密码
        /// </summary>
        public async Task<string?> GetPasswordAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return null;
            }

            try
            {
                var vault = await _storage.LoadVaultAsync();
                if (vault == null)
                {
                    return null;
                }

                var entry = vault.Entries.Find(e =>
                    string.Equals(e.Username, username, StringComparison.OrdinalIgnoreCase));

                if (entry == null || string.IsNullOrEmpty(entry.EncryptedPassword))
                {
                    _logger.LogDebug("未找到已保存的密码 - UserName: {UserName}", username);
                    return null;
                }

                // 验证HMAC完整性
                var expectedHmac = _protector.ComputeHmac(username + "_password", entry.EncryptedPassword);
                if (!string.Equals(entry.PasswordHmac, expectedHmac, StringComparison.Ordinal))
                {
                    _logger.LogWarning("密码HMAC校验失败，数据可能被篡改 - UserName: {UserName}", username);
                    return null;
                }

                // 解密密码
                var password = _protector.Decrypt(entry.EncryptedPassword);
                _logger.LogDebug("密码已加载 - UserName: {UserName}", username);
                return password;
            }
            catch (CryptographicException ex)
            {
                _logger.LogError(ex, "DPAPI解密失败（可能是其他Windows用户的数据）- UserName: {UserName}", username);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取密码失败 - UserName: {UserName}", username);
                return null;
            }
        }

        /// <summary>
        /// 检查是否存在已保存的密码
        /// </summary>
        public async Task<bool> HasSavedPasswordAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return false;
            }

            try
            {
                var vault = await _storage.LoadVaultAsync();
                if (vault == null)
                {
                    return false;
                }

                var entry = vault.Entries.Find(e =>
                    string.Equals(e.Username, username, StringComparison.OrdinalIgnoreCase));

                if (entry == null || string.IsNullOrEmpty(entry.EncryptedPassword))
                {
                    return false;
                }

                // 验证HMAC完整性
                var expectedHmac = _protector.ComputeHmac(username + "_password", entry.EncryptedPassword);
                if (!string.Equals(entry.PasswordHmac, expectedHmac, StringComparison.Ordinal))
                {
                    return false;
                }

                // 尝试解密验证
                try
                {
                    var password = _protector.Decrypt(entry.EncryptedPassword);
                    return !string.IsNullOrEmpty(password);
                }
                catch
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查已保存密码失败 - UserName: {UserName}", username);
                return false;
            }
        }

        /// <summary>
        /// 清除已保存的密码
        /// </summary>
        public async Task<bool> ClearPasswordAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return false;
            }

            try
            {
                var vault = await _storage.LoadVaultAsync();
                if (vault == null)
                {
                    return true;
                }

                var entry = vault.Entries.Find(e =>
                    string.Equals(e.Username, username, StringComparison.OrdinalIgnoreCase));

                if (entry != null && !string.IsNullOrEmpty(entry.EncryptedPassword))
                {
                    entry.EncryptedPassword = string.Empty;
                    entry.PasswordHmac = string.Empty;
                    entry.PasswordSavedAt = null;
                    await _storage.SaveVaultAsync(vault);
                    _logger.LogInformation("已清除已保存的密码 - UserName: {UserName}", username);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除密码失败 - UserName: {UserName}", username);
                return false;
            }
        }

        #endregion

        #region AutoLoginToken存储

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
                var vault = await _storage.LoadVaultAsync() ?? new VaultStorage();

                // 2. 加密Token
                var encryptedToken = _protector.Encrypt(autoLoginToken);

                // 3. 计算HMAC
                var hmac = _protector.ComputeHmac(username, encryptedToken);

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
                await _storage.SaveVaultAsync(vault);

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
                var vault = await _storage.LoadVaultAsync();
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

                // T5-P2-03: 验证HMAC完整性，篡改时清除凭据
                var expectedHmac = _protector.ComputeHmac(username, entry.EncryptedAutoLoginToken);
                if (!string.Equals(entry.Hmac, expectedHmac, StringComparison.Ordinal))
                {
                    _logger.LogWarning("HMAC校验失败，数据可能被篡改，清除凭据 - UserName: {UserName}", username);
                    // 篡改检测: 立即清除被篡改的 Token
                    entry.EncryptedAutoLoginToken = string.Empty;
                    entry.Hmac = string.Empty;
                    await _storage.SaveVaultAsync(vault);
                    return null;
                }

                // 解密Token
                var token = _protector.Decrypt(entry.EncryptedAutoLoginToken);
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
                    var deleted = _storage.DeleteVaultFile();
                    if (deleted)
                    {
                        _logger.LogInformation("已清除所有凭据");
                    }
                    return true;
                }

                // 清除指定用户
                var vault = await _storage.LoadVaultAsync();
                if (vault == null)
                {
                    return true;
                }

                var removed = vault.Entries.RemoveAll(e =>
                    string.Equals(e.Username, username, StringComparison.OrdinalIgnoreCase));

                if (removed > 0)
                {
                    await _storage.SaveVaultAsync(vault);
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
                var vault = await _storage.LoadVaultAsync();
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
                var expectedHmac = _protector.ComputeHmac(username, entry.EncryptedAutoLoginToken);
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
                if (!_storage.OldCredentialsFileExists())
                {
                    _logger.LogDebug("无旧格式凭据需要迁移");
                    return;
                }

                // 检查是否已经迁移过（vault.dat存在）
                if (_storage.VaultFileExists())
                {
                    _logger.LogDebug("已存在vault.dat，跳过迁移");
                    return;
                }

                _logger.LogInformation("检测到旧格式凭据文件，准备迁移提示");

                // 读取旧格式获取用户名（用于显示迁移提示）
                var oldData = await _storage.ReadOldCredentialsAsync();

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
                    await _storage.SaveVaultAsync(vault);
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
                var vault = await _storage.LoadVaultAsync();
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
                var expectedHmac = _protector.ComputeHmac(username, entry.EncryptedAutoLoginToken);
                if (!string.Equals(entry.Hmac, expectedHmac, StringComparison.Ordinal))
                {
                    return false;
                }

                // 尝试解密验证
                try
                {
                    var token = _protector.Decrypt(entry.EncryptedAutoLoginToken);
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

        #endregion
    }
}

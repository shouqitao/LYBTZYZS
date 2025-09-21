using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Core.Services.Configuration
{

    /// <summary>
    /// 安全配置管理服务接口 - UltraThink Stage 5.3.2
    /// 提供敏感配置的加密存储、访问控制、审计等功能
    /// </summary>
    public interface ISecureConfigurationService
    {

        /// <summary>
        /// 获取安全配置值
        /// </summary>
        Task<T?> GetSecureValueAsync<T>(string key, string? passphrase = null);

        /// <summary>
        /// 设置安全配置值
        /// </summary>
        Task SetSecureValueAsync<T>(string key, T value, string? passphrase = null);

        /// <summary>
        /// 删除安全配置
        /// </summary>
        Task RemoveSecureValueAsync(string key);

        /// <summary>
        /// 检查配置是否存在
        /// </summary>
        bool HasSecureValue(string key);

        /// <summary>
        /// 获取所有安全配置键（不包含值）
        /// </summary>
        List<string> GetSecureKeys();

        /// <summary>
        /// 导出安全配置（加密）
        /// </summary>
        Task<string> ExportSecureConfigurationAsync(string passphrase);

        /// <summary>
        /// 导入安全配置
        /// </summary>
        Task ImportSecureConfigurationAsync(string encryptedData, string passphrase);

        /// <summary>
        /// 轮换加密密钥
        /// </summary>
        Task RotateEncryptionKeyAsync(string oldPassphrase, string newPassphrase);

        /// <summary>
        /// 验证配置完整性
        /// </summary>
        Task<IntegrityCheckResult> VerifyIntegrityAsync();

        /// <summary>
        /// 获取访问审计日志
        /// </summary>
        List<SecurityAuditEntry> GetAuditLog(DateTime? startTime = null, DateTime? endTime = null);

        /// <summary>
        /// 设置访问控制策略
        /// </summary>
        void SetAccessPolicy(string key, AccessPolicy policy);

        /// <summary>
        /// 清理过期的安全配置
        /// </summary>
        Task<int> CleanupExpiredConfigurationsAsync();
    }

    /// <summary>
    /// 安全配置管理服务实现
    /// </summary>
    public class SecureConfigurationService : ISecureConfigurationService, IDisposable
    {
        private readonly ILogger<SecureConfigurationService> _logger;
        private readonly string _secureStorePath;
        private readonly Dictionary<string, SecureConfigEntry> _secureConfigs = new();
        private readonly Dictionary<string, AccessPolicy> _accessPolicies = new();
        private readonly List<SecurityAuditEntry> _auditLog = new();
        private readonly object _lock = new object();

        private byte[] _masterKey;
        private readonly int _keyIterations = 100000; // OWASP 2025 建议最小值
        private const int _saltLength = 32; // 256-bit 盐长度

        public SecureConfigurationService(ILogger<SecureConfigurationService> logger)
        {
            _logger = logger;
            _secureStorePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LYBT",
                "SecureConfig");

            Directory.CreateDirectory(_secureStorePath);

            // 初始化主密钥（使用持久化的随机盐）
            InitializeMasterKey();

            LoadSecureConfigurations();
            InitializeDefaultPolicies();
        }

        #region 初始化

        private void InitializeMasterKey()
        {
            var saltFile = Path.Combine(_secureStorePath, "master.salt");
            byte[] salt;

            if (File.Exists(saltFile))
            {
                // 读取已存在的盐值
                var saltData = File.ReadAllText(saltFile);
                salt = Convert.FromBase64String(saltData);
                _logger.LogDebug("使用已存在的主密钥盐值");
            }
            else
            {
                // 生成新的随机盐值
                salt = GenerateRandomSalt();
                File.WriteAllText(saltFile, Convert.ToBase64String(salt));

                // 设置文件属性为隐藏和系统
                File.SetAttributes(saltFile, FileAttributes.Hidden | FileAttributes.System);
                _logger.LogInformation("生成新的主密钥盐值");
            }

            // 使用机器密钥和盐值派生主密钥
            _masterKey = DeriveKey(GetMachineKey(), salt);
        }

        private void LoadSecureConfigurations()
        {
            try
            {
                var configFile = Path.Combine(_secureStorePath, "secure.dat");
                if (File.Exists(configFile))
                {
                    var encryptedData = File.ReadAllBytes(configFile);
                    var decryptedJson = DecryptData(encryptedData, _masterKey);

                    var configs = JsonSerializer.Deserialize<Dictionary<string, SecureConfigEntry>>(decryptedJson);
                    if (configs != null)
                    {
                        lock (_lock)
                        {
                            foreach (var kvp in configs)
                            {
                                _secureConfigs[kvp.Key] = kvp.Value;
                            }
                        }
                    }

                    _logger.LogInformation("加载了 {Count} 个安全配置项", _secureConfigs.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载安全配置失败");
            }
        }

        private void InitializeDefaultPolicies()
        {
            // 设置默认访问策略
            _accessPolicies["Database:ConnectionString"] = new AccessPolicy
            {
                RequireAuthentication = true,
                RequireAdminRole = true,
                AllowedRoles = new[] { "Admin", "SystemAdmin" },
                MaxAccessPerHour = 10
            };

            _accessPolicies["API:SecretKey"] = new AccessPolicy
            {
                RequireAuthentication = true,
                RequireAdminRole = true,
                AllowedRoles = new[] { "SystemAdmin" },
                MaxAccessPerHour = 5,
                RequirePassphrase = true
            };

            _accessPolicies["Security:*"] = new AccessPolicy
            {
                RequireAuthentication = true,
                RequireAdminRole = true,
                LogAccess = true
            };
        }

        #endregion 初始化

        #region 核心功能

        /// <inheritdoc/>
        public Task<T?> GetSecureValueAsync<T>(string key, string? passphrase = null)
        {
            try
            {
                // 检查访问策略
                CheckAccessPolicy(key, passphrase);

                lock (_lock)
                {
                    if (!_secureConfigs.TryGetValue(key, out var entry))
                    {
                        _logger.LogDebug("安全配置项未找到: {Key}", key);
                        return Task.FromResult<T?>(default);
                    }

                    // 检查过期
                    if (entry.ExpiresAt.HasValue && entry.ExpiresAt.Value < DateTime.Now)
                    {
                        _logger.LogWarning("安全配置已过期: {Key}", key);
                        _secureConfigs.Remove(key);
                        return Task.FromResult<T?>(default);
                    }

                    // 解密值（支持新旧格式）
                    string decryptedData;
                    var encryptedBytes = Convert.FromBase64String(entry.EncryptedValue);

                    if (!string.IsNullOrEmpty(entry.Salt))
                    {
                        // 新格式：使用记录特定的盐值
                        var salt = Convert.FromBase64String(entry.Salt);
                        var iterations = entry.Iterations ?? _keyIterations;

                        // 使用记录的迭代次数（支持渐进升级）
                        var decryptKey = string.IsNullOrEmpty(passphrase) ?
                            DeriveKeyWithIterations(Convert.ToBase64String(_masterKey), salt, iterations) :
                            DeriveKeyWithIterations(passphrase, salt, iterations);

                        decryptedData = DecryptData(encryptedBytes, decryptKey);
                    }
                    else
                    {
                        // 旧格式：向后兼容（无独立盐值）
                        var decryptKey = string.IsNullOrEmpty(passphrase) ?
                            _masterKey : DeriveKey(passphrase, Encoding.UTF8.GetBytes(key));

                        decryptedData = DecryptData(encryptedBytes, decryptKey);

                        // 可选：自动迁移到新格式
                        _logger.LogInformation("检测到旧格式配置 {Key}，建议运行密钥轮换升级", key);
                    }

                    var value = JsonSerializer.Deserialize<T>(decryptedData);

                    // 记录审计日志
                    LogAccess(key, "Read", true);

                    // 更新访问时间
                    entry.LastAccessed = DateTime.Now;
                    entry.AccessCount++;

                    return Task.FromResult<T?>(value);
                }
            }
            catch (Exception ex)
            {
                LogAccess(key, "Read", false, ex.Message);
                _logger.LogError(ex, "获取安全配置失败: {Key}", key);
                throw;
            }
        }

        /// <inheritdoc/>
        public Task SetSecureValueAsync<T>(string key, T value, string? passphrase = null)
        {
            try
            {
                // 检查访问策略
                CheckAccessPolicy(key, passphrase);

                // 序列化值
                var json = JsonSerializer.Serialize(value);

                // 为每条记录生成独立的随机盐
                var recordSalt = GenerateRandomSalt();

                // 使用记录特定的盐值派生密钥
                var encryptKey = string.IsNullOrEmpty(passphrase) ?
                    DeriveKey(Convert.ToBase64String(_masterKey), recordSalt) :
                    DeriveKey(passphrase, recordSalt);

                // 加密数据
                var encryptedData = EncryptData(json, encryptKey);

                lock (_lock)
                {
                    var isNew = !_secureConfigs.ContainsKey(key);

                    _secureConfigs[key] = new SecureConfigEntry
                    {
                        Key = key,
                        EncryptedValue = Convert.ToBase64String(encryptedData),
                        Salt = Convert.ToBase64String(recordSalt), // 存储盐值
                        Iterations = _keyIterations, // 记录迭代次数
                        CreatedAt = isNew ? DateTime.Now : _secureConfigs[key].CreatedAt,
                        UpdatedAt = DateTime.Now,
                        LastAccessed = DateTime.Now,
                        AccessCount = isNew ? 0 : _secureConfigs[key].AccessCount,
                        Checksum = ComputeChecksum(encryptedData),
                        Metadata = new Dictionary<string, string>
                        {
                            ["Type"] = typeof(T).Name,
                            ["Size"] = encryptedData.Length.ToString()
                        }
                    };

                    // 保存到文件
                    SaveSecureConfigurations();

                    // 记录审计日志
                    LogAccess(key, isNew ? "Create" : "Update", true);

                    _logger.LogInformation(
                        "安全配置已{Operation}: {Key}",
                        isNew ? "创建" : "更新", key);
                }
            }
            catch (Exception ex)
            {
                LogAccess(key, "Write", false, ex.Message);
                _logger.LogError(ex, "设置安全配置失败: {Key}", key);
                throw;
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task RemoveSecureValueAsync(string key)
        {
            try
            {
                CheckAccessPolicy(key, null);

                lock (_lock)
                {
                    if (_secureConfigs.Remove(key))
                    {
                        SaveSecureConfigurations();
                        LogAccess(key, "Delete", true);
                        _logger.LogInformation("安全配置已删除: {Key}", key);
                    }
                }

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                LogAccess(key, "Delete", false, ex.Message);
                _logger.LogError(ex, "删除安全配置失败: {Key}", key);
                throw;
            }
        }

        /// <inheritdoc/>
        public bool HasSecureValue(string key)
        {
            lock (_lock)
            {
                return _secureConfigs.ContainsKey(key);
            }
        }

        /// <inheritdoc/>
        public List<string> GetSecureKeys()
        {
            lock (_lock)
            {
                return _secureConfigs.Keys.ToList();
            }
        }

        #endregion 核心功能

        #region 导入导出

        /// <inheritdoc/>
        public Task<string> ExportSecureConfigurationAsync(string passphrase)
        {
            try
            {
                lock (_lock)
                {
                    var exportData = new SecureConfigExport
                    {
                        Version = "1.0",
                        ExportedAt = DateTime.Now,
                        Configurations = _secureConfigs.ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value),
                        Policies = _accessPolicies.ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value)
                    };

                    var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                    // 使用提供的密码加密
                    var exportSalt = Encoding.UTF8.GetBytes("EXPORT_SALT_2025");
                    var exportKey = DeriveKey(passphrase, exportSalt);
                    var encryptedData = EncryptData(json, exportKey);

                    LogAccess("*", "Export", true);

                    return Task.FromResult(Convert.ToBase64String(encryptedData));
                }
            }
            catch (Exception ex)
            {
                LogAccess("*", "Export", false, ex.Message);
                _logger.LogError(ex, "导出安全配置失败");
                throw;
            }
        }

        /// <inheritdoc/>
        public Task ImportSecureConfigurationAsync(string encryptedData, string passphrase)
        {
            try
            {
                // 解密导入数据
                var exportSalt = Encoding.UTF8.GetBytes("EXPORT_SALT_2025");
                var exportKey = DeriveKey(passphrase, exportSalt);
                var decryptedJson = DecryptData(Convert.FromBase64String(encryptedData), exportKey);

                var importData = JsonSerializer.Deserialize<SecureConfigExport>(decryptedJson);
                if (importData == null)
                {
                    throw new InvalidOperationException("无效的导入数据");
                }

                lock (_lock)
                {
                    // 备份现有配置
                    var backup = new Dictionary<string, SecureConfigEntry>(_secureConfigs);

                    try
                    {
                        // 导入配置
                        foreach (var kvp in importData.Configurations)
                        {
                            _secureConfigs[kvp.Key] = kvp.Value;
                        }

                        // 导入策略
                        foreach (var kvp in importData.Policies)
                        {
                            _accessPolicies[kvp.Key] = kvp.Value;
                        }

                        SaveSecureConfigurations();

                        LogAccess("*", "Import", true);
                        _logger.LogInformation("导入了 {Count} 个安全配置项", importData.Configurations.Count);
                    }
                    catch
                    {
                        // 恢复备份
                        _secureConfigs.Clear();
                        foreach (var kvp in backup)
                        {
                            _secureConfigs[kvp.Key] = kvp.Value;
                        }

                        throw;
                    }
                }

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                LogAccess("*", "Import", false, ex.Message);
                _logger.LogError(ex, "导入安全配置失败");
                throw;
            }
        }

        #endregion 导入导出

        #region 密钥管理

        /// <inheritdoc/>
        public Task RotateEncryptionKeyAsync(string oldPassphrase, string newPassphrase)
        {
            try
            {
                _logger.LogInformation("开始轮换加密密钥");

                // 为密钥轮换生成新的盐值
                var rotationSalt = GenerateRandomSalt();
                var oldKey = _masterKey;
                var newKey = DeriveKey(newPassphrase, rotationSalt);

                lock (_lock)
                {
                    var reencryptedConfigs = new Dictionary<string, SecureConfigEntry>();

                    foreach (var kvp in _secureConfigs)
                    {
                        // 解密
                        var decryptedData = DecryptData(
                            Convert.FromBase64String(kvp.Value.EncryptedValue),
                            oldKey);

                        // 重新加密
                        var reencryptedData = EncryptData(decryptedData, newKey);

                        kvp.Value.EncryptedValue = Convert.ToBase64String(reencryptedData);
                        kvp.Value.Checksum = ComputeChecksum(reencryptedData);
                        kvp.Value.UpdatedAt = DateTime.Now;

                        reencryptedConfigs[kvp.Key] = kvp.Value;
                    }

                    // 更新配置
                    _secureConfigs.Clear();
                    foreach (var kvp in reencryptedConfigs)
                    {
                        _secureConfigs[kvp.Key] = kvp.Value;
                    }

                    // 更新主密钥
                    _masterKey = newKey;

                    SaveSecureConfigurations();

                    LogAccess("*", "KeyRotation", true);
                    _logger.LogInformation("密钥轮换完成，更新了 {Count} 个配置项", reencryptedConfigs.Count);
                }

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                LogAccess("*", "KeyRotation", false, ex.Message);
                _logger.LogError(ex, "密钥轮换失败");
                throw;
            }
        }

        #endregion 密钥管理

        #region 完整性和审计

        /// <inheritdoc/>
        public Task<IntegrityCheckResult> VerifyIntegrityAsync()
        {
            var result = new IntegrityCheckResult
            {
                CheckedAt = DateTime.Now,
                TotalConfigs = _secureConfigs.Count,
                Issues = new List<IntegrityIssue>()
            };

            lock (_lock)
            {
                foreach (var kvp in _secureConfigs)
                {
                    try
                    {
                        var encryptedData = Convert.FromBase64String(kvp.Value.EncryptedValue);
                        var currentChecksum = ComputeChecksum(encryptedData);

                        if (currentChecksum != kvp.Value.Checksum)
                        {
                            result.Issues.Add(new IntegrityIssue
                            {
                                Key = kvp.Key,
                                Type = IntegrityIssueType.ChecksumMismatch,
                                Description = "校验和不匹配，数据可能已损坏"
                            });
                        }

                        // 尝试解密验证
                        try
                        {
                            var decrypted = DecryptData(encryptedData, _masterKey);
                        }
                        catch
                        {
                            result.Issues.Add(new IntegrityIssue
                            {
                                Key = kvp.Key,
                                Type = IntegrityIssueType.DecryptionFailed,
                                Description = "无法解密，密钥可能不正确"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Issues.Add(new IntegrityIssue
                        {
                            Key = kvp.Key,
                            Type = IntegrityIssueType.Unknown,
                            Description = ex.Message
                        });
                    }
                }

                result.IsValid = result.Issues.Count == 0;
                result.ValidConfigs = result.TotalConfigs - result.Issues.Count;
            }

            _logger.LogInformation(
                "完整性检查完成: {Valid}/{Total} 配置有效",
                result.ValidConfigs, result.TotalConfigs);

            return Task.FromResult(result);
        }

        /// <inheritdoc/>
        public List<SecurityAuditEntry> GetAuditLog(DateTime? startTime = null, DateTime? endTime = null)
        {
            lock (_auditLog)
            {
                var query = _auditLog.AsEnumerable();

                if (startTime.HasValue)
                {
                    query = query.Where(e => e.Timestamp >= startTime.Value);
                }

                if (endTime.HasValue)
                {
                    query = query.Where(e => e.Timestamp <= endTime.Value);
                }

                return query.OrderByDescending(e => e.Timestamp).ToList();
            }
        }

        /// <inheritdoc/>
        public void SetAccessPolicy(string key, AccessPolicy policy)
        {
            lock (_accessPolicies)
            {
                _accessPolicies[key] = policy;
                _logger.LogInformation("访问策略已更新: {Key}", key);
            }
        }

        /// <inheritdoc/>
        public Task<int> CleanupExpiredConfigurationsAsync()
        {
            var removed = 0;

            lock (_lock)
            {
                var expiredKeys = _secureConfigs
                    .Where(kvp => kvp.Value.ExpiresAt.HasValue && kvp.Value.ExpiresAt.Value < DateTime.Now)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredKeys)
                {
                    _secureConfigs.Remove(key);
                    removed++;
                    _logger.LogDebug("清理过期配置: {Key}", key);
                }

                if (removed > 0)
                {
                    SaveSecureConfigurations();
                }
            }

            _logger.LogInformation("清理了 {Count} 个过期的安全配置", removed);
            return Task.FromResult(removed);
        }

        #endregion 完整性和审计

        #region 私有方法

        private void CheckAccessPolicy(string key, string? passphrase)
        {
            AccessPolicy? policy = null;

            // 查找匹配的策略
            if (_accessPolicies.ContainsKey(key))
            {
                policy = _accessPolicies[key];
            }
            else
            {
                // 查找通配符策略
                var wildcardKey = _accessPolicies.Keys
                    .Where(k => k.EndsWith("*"))
                    .FirstOrDefault(k => key.StartsWith(k.TrimEnd('*')));

                if (wildcardKey != null)
                {
                    policy = _accessPolicies[wildcardKey];
                }
            }

            if (policy != null)
            {
                // 检查密码要求
                if (policy.RequirePassphrase && string.IsNullOrEmpty(passphrase))
                {
                    throw new UnauthorizedAccessException("此配置需要提供访问密码");
                }

                // 检查访问频率限制
                if (policy.MaxAccessPerHour > 0)
                {
                    var recentAccess = _auditLog.Count(e =>
                        e.ConfigKey == key &&
                        e.Timestamp > DateTime.Now.AddHours(-1));

                    if (recentAccess >= policy.MaxAccessPerHour)
                    {
                        throw new InvalidOperationException("超过访问频率限制");
                    }
                }
            }
        }

        private void LogAccess(string key, string operation, bool success, string? error = null)
        {
            lock (_auditLog)
            {
                _auditLog.Add(new SecurityAuditEntry
                {
                    Timestamp = DateTime.Now,
                    ConfigKey = key,
                    Operation = operation,
                    Success = success,
                    Error = error,
                    User = Environment.UserName,
                    Machine = Environment.MachineName
                });

                // 只保留最近1000条记录
                while (_auditLog.Count > 1000)
                {
                    _auditLog.RemoveAt(0);
                }
            }
        }

        private void SaveSecureConfigurations()
        {
            try
            {
                var json = JsonSerializer.Serialize(_secureConfigs);
                var encryptedData = EncryptData(json, _masterKey);

                var configFile = Path.Combine(_secureStorePath, "secure.dat");
                File.WriteAllBytes(configFile, encryptedData);

                // 设置文件属性为隐藏和系统
                File.SetAttributes(configFile, FileAttributes.Hidden | FileAttributes.System);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存安全配置失败");
                throw;
            }
        }

        #endregion 私有方法

        #region 加密方法

        private byte[] DeriveKey(string passphrase, byte[] salt)
        {
            // 使用 PBKDF2 with SHA-256，100,000 次迭代（OWASP 2025 推荐）
            using (var pbkdf2 = new Rfc2898DeriveBytes(
                passphrase,
                salt,
                _keyIterations,
                HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(32); // 256-bit key
            }
        }

        private byte[] GenerateRandomSalt()
        {
            var salt = new byte[_saltLength];
            RandomNumberGenerator.Fill(salt);
            return salt;
        }

        private byte[] DeriveKeyWithIterations(string passphrase, byte[] salt, int iterations)
        {
            // 使用指定的迭代次数派生密钥（支持渐进升级）
            using (var pbkdf2 = new Rfc2898DeriveBytes(
                passphrase,
                salt,
                iterations,
                HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(32); // 256-bit key
            }
        }

        private string GetMachineKey()
        {
            // 组合多个机器特征生成唯一密钥
            var machineKey = $"{Environment.MachineName}:{Environment.UserName}:{Environment.ProcessorCount}";
            return machineKey;
        }

        private byte[] EncryptData(string plainText, byte[] key)
        {
            // 使用 AES-GCM 实现 AEAD 加密
            using (var aesGcm = new AesGcm(key))
            {
                // 生成随机 nonce (12 bytes for AES-GCM)
                var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
                RandomNumberGenerator.Fill(nonce);

                // 准备明文数据
                var plaintextBytes = Encoding.UTF8.GetBytes(plainText);
                var ciphertext = new byte[plaintextBytes.Length];

                // 生成认证标签 (16 bytes)
                var tag = new byte[AesGcm.TagByteSizes.MaxSize];

                // 执行加密
                aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);

                // 组合结果: nonce + tag + ciphertext
                using (var ms = new MemoryStream())
                {
                    ms.Write(nonce, 0, nonce.Length);
                    ms.Write(tag, 0, tag.Length);
                    ms.Write(ciphertext, 0, ciphertext.Length);
                    return ms.ToArray();
                }
            }
        }

        private string DecryptData(byte[] cipherData, byte[] key)
        {
            // 使用 AES-GCM 实现 AEAD 解密
            try
            {
                using (var aesGcm = new AesGcm(key))
                {
                    // 提取 nonce (12 bytes)
                    var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
                    Array.Copy(cipherData, 0, nonce, 0, nonce.Length);

                    // 提取认证标签 (16 bytes)
                    var tag = new byte[AesGcm.TagByteSizes.MaxSize];
                    Array.Copy(cipherData, nonce.Length, tag, 0, tag.Length);

                    // 提取密文
                    var ciphertextLength = cipherData.Length - nonce.Length - tag.Length;
                    var ciphertext = new byte[ciphertextLength];
                    Array.Copy(cipherData, nonce.Length + tag.Length, ciphertext, 0, ciphertextLength);

                    // 准备明文缓冲区
                    var plaintext = new byte[ciphertextLength];

                    // 执行解密并验证认证标签
                    aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);

                    return Encoding.UTF8.GetString(plaintext);
                }
            }
            catch (CryptographicException ex)
            {
                // 认证失败或数据被篡改
                _logger.LogError(ex, "解密失败：数据可能被篡改或密钥不正确");
                throw new SecurityException("数据完整性验证失败，配置可能被篡改", ex);
            }
        }

        private string ComputeChecksum(byte[] data)
        {
            // AES-GCM 已经包含认证标签，此方法保留为兼容性
            // 可以用于记录数据指纹用于审计
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(data);
                return Convert.ToBase64String(hash);
            }
        }

        #endregion 加密方法

        #region IDisposable

        /// <inheritdoc/>
        public void Dispose()
        {
            lock (_lock)
            {
                SaveSecureConfigurations();
            }

            _logger.LogInformation("安全配置服务已关闭，审计日志包含 {Count} 条记录", _auditLog.Count);
        }

        #endregion IDisposable
    }

    #region 数据模型

    /// <summary>
    /// 安全配置条目
    /// </summary>
    public class SecureConfigEntry
    {
        public string Key { get; set; } = string.Empty;
        public string EncryptedValue { get; set; } = string.Empty;
        public string Checksum { get; set; } = string.Empty;
        public string? Salt { get; set; } // 每条记录独立的盐值（Base64）
        public int? Iterations { get; set; } // 记录使用的迭代次数，支持渐进升级
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastAccessed { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int AccessCount { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
    }

    /// <summary>
    /// 访问策略
    /// </summary>
    public class AccessPolicy
    {
        public bool RequireAuthentication { get; set; } = true;
        public bool RequireAdminRole { get; set; } = false;
        public string[]? AllowedRoles { get; set; }
        public bool RequirePassphrase { get; set; } = false;
        public int MaxAccessPerHour { get; set; } = 0;
        public bool LogAccess { get; set; } = true;
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
    }

    /// <summary>
    /// 安全审计条目
    /// </summary>
    public class SecurityAuditEntry
    {
        public DateTime Timestamp { get; set; }
        public string ConfigKey { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string User { get; set; } = string.Empty;
        public string Machine { get; set; } = string.Empty;
    }

    /// <summary>
    /// 完整性检查结果
    /// </summary>
    public class IntegrityCheckResult
    {
        public DateTime CheckedAt { get; set; }
        public bool IsValid { get; set; }
        public int TotalConfigs { get; set; }
        public int ValidConfigs { get; set; }
        public List<IntegrityIssue> Issues { get; set; } = new();
    }

    /// <summary>
    /// 完整性问题
    /// </summary>
    public class IntegrityIssue
    {
        public string Key { get; set; } = string.Empty;
        public IntegrityIssueType Type { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// 完整性问题类型
    /// </summary>
    public enum IntegrityIssueType
    {
        ChecksumMismatch,
        DecryptionFailed,
        Expired,
        Unknown
    }

    /// <summary>
    /// 安全配置导出数据
    /// </summary>
    internal class SecureConfigExport
    {
        public string Version { get; set; } = string.Empty;
        public DateTime ExportedAt { get; set; }
        public Dictionary<string, SecureConfigEntry> Configurations { get; set; } = new();
        public Dictionary<string, AccessPolicy> Policies { get; set; } = new();
    }

    #endregion 数据模型
}

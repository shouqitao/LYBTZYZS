using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LYBT.Infrastructure.Security
{
    /// <summary>
    /// 安全配置管理服务 - UltraThink重构安全配置架构
    /// 管理系统安全相关配置，包括加密密钥、安全策略等
    /// </summary>
    public class SecurityConfigurationService : ISecurityConfigurationService
    {
        private readonly IConfiguration _configuration;
        private readonly IEncryptionService _encryptionService;
        private readonly ILogger<SecurityConfigurationService> _logger;
        private readonly object _lock = new object();
        private SecurityConfiguration? _cachedConfig;
        private DateTime _lastConfigUpdate = DateTime.MinValue;

        public SecurityConfigurationService(
            IConfiguration configuration,
            IEncryptionService encryptionService,
            ILogger<SecurityConfigurationService> logger)
        {
            _configuration = configuration;
            _encryptionService = encryptionService;
            _logger = logger;
        }

        /// <summary>
        /// 获取安全配置
        /// </summary>
        public SecurityConfiguration GetSecurityConfiguration()
        {
            lock (_lock)
            {
                // 如果配置未缓存或已过期（每5分钟更新一次），重新加载
                if (_cachedConfig == null || 
                    DateTime.UtcNow - _lastConfigUpdate > TimeSpan.FromMinutes(5))
                {
                    _cachedConfig = LoadSecurityConfiguration();
                    _lastConfigUpdate = DateTime.UtcNow;
                }

                return _cachedConfig;
            }
        }

        /// <summary>
        /// 更新安全配置
        /// </summary>
        public async Task UpdateSecurityConfigurationAsync(SecurityConfiguration configuration)
        {
            try
            {
                // 验证配置
                ValidateSecurityConfiguration(configuration);

                // 加密敏感数据
                var encryptedConfig = EncryptSensitiveData(configuration);

                // 保存配置（这里需要实现配置持久化）
                await SaveSecurityConfigurationAsync(encryptedConfig);

                // 更新缓存
                lock (_lock)
                {
                    _cachedConfig = configuration;
                    _lastConfigUpdate = DateTime.UtcNow;
                }

                _logger.LogInformation("安全配置已更新");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新安全配置失败");
                throw;
            }
        }

        /// <summary>
        /// 获取密码策略
        /// </summary>
        public PasswordPolicy GetPasswordPolicy()
        {
            var config = GetSecurityConfiguration();
            return config.PasswordPolicy;
        }

        /// <summary>
        /// 获取JWT配置
        /// </summary>
        public EnhancedJwtOptions GetJwtOptions()
        {
            var config = GetSecurityConfiguration();
            return config.JwtOptions;
        }

        /// <summary>
        /// 获取限流配置
        /// </summary>
        public RateLimitOptions GetRateLimitOptions()
        {
            var config = GetSecurityConfiguration();
            return config.RateLimitOptions;
        }

        /// <summary>
        /// 获取输入验证配置
        /// </summary>
        public InputValidationOptions GetInputValidationOptions()
        {
            var config = GetSecurityConfiguration();
            return config.InputValidationOptions;
        }

        /// <summary>
        /// 检查功能是否启用
        /// </summary>
        public bool IsFeatureEnabled(string featureName)
        {
            var config = GetSecurityConfiguration();
            return config.FeatureFlags.GetValueOrDefault(featureName, false);
        }

        /// <summary>
        /// 获取加密密钥
        /// </summary>
        public string GetEncryptionKey(string keyName)
        {
            var config = GetSecurityConfiguration();
            var encryptedKey = config.EncryptionKeys.GetValueOrDefault(keyName);
            
            if (string.IsNullOrEmpty(encryptedKey))
            {
                throw new SecurityException($"加密密钥 '{keyName}' 未找到");
            }

            return _encryptionService.Decrypt(encryptedKey);
        }

        /// <summary>
        /// 设置加密密钥
        /// </summary>
        public async Task SetEncryptionKeyAsync(string keyName, string key)
        {
            var config = GetSecurityConfiguration();
            config.EncryptionKeys[keyName] = _encryptionService.Encrypt(key);
            
            await UpdateSecurityConfigurationAsync(config);
            _logger.LogInformation("加密密钥已设置: {KeyName}", keyName);
        }

        /// <summary>
        /// 轮换加密密钥
        /// </summary>
        public async Task<string> RotateEncryptionKeyAsync(string keyName)
        {
            try
            {
                var newKey = _encryptionService.GenerateSecureKey();
                await SetEncryptionKeyAsync(keyName, newKey);
                
                _logger.LogInformation("加密密钥已轮换: {KeyName}", keyName);
                return newKey;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "轮换加密密钥失败: {KeyName}", keyName);
                throw;
            }
        }

        /// <summary>
        /// 获取安全头部配置
        /// </summary>
        public SecurityHeadersOptions GetSecurityHeadersOptions()
        {
            var config = GetSecurityConfiguration();
            return config.SecurityHeadersOptions;
        }

        /// <summary>
        /// 加载安全配置
        /// </summary>
        private SecurityConfiguration LoadSecurityConfiguration()
        {
            try
            {
                var config = new SecurityConfiguration();

                // 从appsettings.json加载基础配置
                _configuration.GetSection("Security").Bind(config);

                // 从环境变量覆盖敏感配置
                LoadFromEnvironmentVariables(config);

                // 解密敏感数据
                DecryptSensitiveData(config);

                // 设置默认值
                SetDefaultValues(config);

                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载安全配置失败");
                throw new SecurityException("无法加载安全配置", ex);
            }
        }

        /// <summary>
        /// 从环境变量加载配置
        /// </summary>
        private void LoadFromEnvironmentVariables(SecurityConfiguration config)
        {
            // JWT密钥
            var jwtSecret = Environment.GetEnvironmentVariable("LYBT_JWT_SECRET");
            if (!string.IsNullOrEmpty(jwtSecret))
            {
                config.JwtOptions.SecretKey = jwtSecret;
            }

            // 数据库加密密钥
            var dbEncryptionKey = Environment.GetEnvironmentVariable("LYBT_DB_ENCRYPTION_KEY");
            if (!string.IsNullOrEmpty(dbEncryptionKey))
            {
                config.EncryptionKeys["DatabaseEncryption"] = dbEncryptionKey;
            }

            // API密钥加密密钥
            var apiEncryptionKey = Environment.GetEnvironmentVariable("LYBT_API_ENCRYPTION_KEY");
            if (!string.IsNullOrEmpty(apiEncryptionKey))
            {
                config.EncryptionKeys["ApiKeyEncryption"] = apiEncryptionKey;
            }
        }

        /// <summary>
        /// 解密敏感数据
        /// </summary>
        private void DecryptSensitiveData(SecurityConfiguration config)
        {
            try
            {
                // 解密加密密钥（如果已加密）
                var decryptedKeys = new Dictionary<string, string>();
                foreach (var kvp in config.EncryptionKeys)
                {
                    try
                    {
                        decryptedKeys[kvp.Key] = _encryptionService.Decrypt(kvp.Value);
                    }
                    catch
                    {
                        // 如果解密失败，说明可能未加密，直接使用原值
                        decryptedKeys[kvp.Key] = kvp.Value;
                    }
                }
                config.EncryptionKeys = decryptedKeys;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "解密敏感配置数据时出现警告");
            }
        }

        /// <summary>
        /// 加密敏感数据
        /// </summary>
        private SecurityConfiguration EncryptSensitiveData(SecurityConfiguration config)
        {
            var encryptedConfig = JsonSerializer.Deserialize<SecurityConfiguration>(
                JsonSerializer.Serialize(config))!;

            // 加密密钥
            var encryptedKeys = new Dictionary<string, string>();
            foreach (var kvp in config.EncryptionKeys)
            {
                encryptedKeys[kvp.Key] = _encryptionService.Encrypt(kvp.Value);
            }
            encryptedConfig.EncryptionKeys = encryptedKeys;

            return encryptedConfig;
        }

        /// <summary>
        /// 设置默认值
        /// </summary>
        private void SetDefaultValues(SecurityConfiguration config)
        {
            // JWT默认配置
            if (string.IsNullOrEmpty(config.JwtOptions.Issuer))
                config.JwtOptions.Issuer = "LYBT.WebAPI";
            
            if (string.IsNullOrEmpty(config.JwtOptions.Audience))
                config.JwtOptions.Audience = "LYBT.Client";

            // 密码策略默认配置
            if (config.PasswordPolicy.MinimumLength == 0)
                config.PasswordPolicy.MinimumLength = 8;

            // 默认功能标志
            if (!config.FeatureFlags.ContainsKey("EnableAuditLogging"))
                config.FeatureFlags["EnableAuditLogging"] = true;
            
            if (!config.FeatureFlags.ContainsKey("EnableRateLimit"))
                config.FeatureFlags["EnableRateLimit"] = true;
        }

        /// <summary>
        /// 验证安全配置
        /// </summary>
        private void ValidateSecurityConfiguration(SecurityConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            // 验证JWT配置
            if (string.IsNullOrEmpty(config.JwtOptions.SecretKey))
                throw new SecurityException("JWT密钥不能为空");

            if (config.JwtOptions.SecretKey.Length < 32)
                throw new SecurityException("JWT密钥长度不能少于32字符");

            // 验证密码策略
            if (config.PasswordPolicy.MinimumLength < 6)
                throw new SecurityException("密码最小长度不能少于6");

            // 验证加密密钥
            foreach (var kvp in config.EncryptionKeys)
            {
                if (string.IsNullOrEmpty(kvp.Value))
                    throw new SecurityException($"加密密钥 '{kvp.Key}' 不能为空");
            }
        }

        /// <summary>
        /// 保存安全配置（需要实现具体的持久化逻辑）
        /// </summary>
        private async Task SaveSecurityConfigurationAsync(SecurityConfiguration configuration)
        {
            // TODO: 实现配置持久化到数据库或安全的配置存储
            await Task.CompletedTask;
        }
    }
}
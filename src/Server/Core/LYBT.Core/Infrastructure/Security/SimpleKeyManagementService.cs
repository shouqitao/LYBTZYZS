using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using LYBT.Core.Infrastructure.Configuration.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LYBT.Core.Infrastructure.Security
{
    /// <summary>
    /// 简化的密钥管理服务实现 - 基于配置文件的密钥管理
    /// </summary>
    public class SimpleKeyManagementService : IKeyManagementService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SimpleKeyManagementService> _logger;
        private readonly SecurityOptions _securityOptions;
        private readonly IDataProtectionService _dataProtectionService;

        // 内存缓存
        private readonly Dictionary<string, string> _keyCache = new();
        private readonly Dictionary<string, DateTime> _keyCacheTime = new();
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

        public SimpleKeyManagementService(
            IConfiguration configuration,
            ILogger<SimpleKeyManagementService> logger,
            IOptions<SecurityOptions> securityOptions,
            IDataProtectionService dataProtectionService)
        {
            _configuration = configuration;
            _logger = logger;
            _securityOptions = securityOptions.Value;
            _dataProtectionService = dataProtectionService;
        }

        /// <summary>
        /// 获取当前JWT签名密钥
        /// </summary>
        public async Task<string> GetCurrentJwtSecretAsync()
        {
            const string cacheKey = "JWT_SECRET_CURRENT";

            // 检查缓存
            if (_keyCache.ContainsKey(cacheKey) &&
                _keyCacheTime.ContainsKey(cacheKey) &&
                DateTime.UtcNow - _keyCacheTime[cacheKey] < _cacheExpiry)
            {
                return _keyCache[cacheKey];
            }

            try
            {
                // 从配置获取JWT密钥
                var jwtSecret = _configuration["JwtOptions:Secret"] ??
                               Environment.GetEnvironmentVariable("JWT_SECRET");

                if (string.IsNullOrEmpty(jwtSecret))
                {
                    // 生成新密钥
                    jwtSecret = GenerateSecureKey(64);
                    _logger.LogWarning("未找到JWT密钥配置，生成新密钥");
                }

                // 缓存密钥
                _keyCache[cacheKey] = jwtSecret;
                _keyCacheTime[cacheKey] = DateTime.UtcNow;

                return await Task.FromResult(jwtSecret);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取JWT密钥失败");
                throw new InvalidOperationException("无法获取JWT密钥", ex);
            }
        }

        /// <summary>
        /// 获取所有有效的JWT密钥（用于验证旧令牌）
        /// </summary>
        public async Task<IEnumerable<string>> GetValidJwtSecretsAsync()
        {
            var secrets = new List<string>();

            // 添加当前密钥
            var currentSecret = await GetCurrentJwtSecretAsync();
            secrets.Add(currentSecret);

            // 添加配置中的备用密钥（如果有）
            var fallbackSecret = _configuration["JwtOptions:FallbackSecret"];
            if (!string.IsNullOrEmpty(fallbackSecret) && fallbackSecret != currentSecret)
            {
                secrets.Add(fallbackSecret);
            }

            return secrets;
        }

        /// <summary>
        /// 旋转JWT密钥（简化实现：记录日志）
        /// </summary>
        public async Task<string> RotateJwtSecretAsync()
        {
            try
            {
                var newSecret = GenerateSecureKey(64);

                // 清除缓存
                _keyCache.Clear();
                _keyCacheTime.Clear();

                _logger.LogInformation("JWT密钥旋转请求已记录。新密钥需要手动更新到配置文件或环境变量");
                _logger.LogInformation("新JWT密钥（请安全保存）: {KeyPreview}...", newSecret.Substring(0, 10));

                return await Task.FromResult(newSecret);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JWT密钥旋转失败");
                throw;
            }
        }

        /// <summary>
        /// 获取数据库连接字符串
        /// </summary>
        public async Task<string> GetDatabaseConnectionStringAsync()
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection") ??
                                 Environment.GetEnvironmentVariable("CONNECTION_STRING");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("未找到数据库连接字符串");
            }

            return await Task.FromResult(connectionString);
        }

        /// <summary>
        /// 更新数据库连接字符串（简化实现：记录日志）
        /// </summary>
        public async Task UpdateDatabaseConnectionStringAsync(string connectionString)
        {
            _logger.LogInformation("数据库连接字符串更新请求已记录");
            await Task.CompletedTask;
        }

        /// <summary>
        /// 获取API密钥
        /// </summary>
        public async Task<string> GetApiKeyAsync(string keyName)
        {
            var apiKey = _configuration[$"ApiKeys:{keyName}"] ??
                        Environment.GetEnvironmentVariable($"API_KEY_{keyName}");

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new KeyNotFoundException($"API密钥 {keyName} 不存在");
            }

            return await Task.FromResult(apiKey);
        }

        /// <summary>
        /// 生成新的API密钥
        /// </summary>
        public async Task<string> GenerateApiKeyAsync(string keyName, int expiryDays = 365)
        {
            var apiKey = GenerateSecureKey(32);

            _logger.LogInformation("API密钥生成成功: {KeyName}, 过期时间: {ExpiryDays} 天",
                keyName, expiryDays);

            return await Task.FromResult(apiKey);
        }

        /// <summary>
        /// 验证API密钥
        /// </summary>
        public async Task<bool> ValidateApiKeyAsync(string keyName, string apiKey)
        {
            try
            {
                var storedKey = await GetApiKeyAsync(keyName);
                return CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(storedKey),
                    Encoding.UTF8.GetBytes(apiKey));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取证书
        /// </summary>
        public async Task<X509Certificate2?> GetCertificateAsync(string thumbprint)
        {
            try
            {
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly);

                var certificates = store.Certificates.Find(
                    X509FindType.FindByThumbprint,
                    thumbprint,
                    false);

                return await Task.FromResult(certificates.Count > 0 ? certificates[0] : null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取证书失败: {Thumbprint}", thumbprint);
                return null;
            }
        }

        /// <summary>
        /// 获取密钥最后旋转时间
        /// </summary>
        public async Task<DateTime?> GetLastRotationTimeAsync(string keyType)
        {
            // 简化实现：返回null表示未记录
            return await Task.FromResult<DateTime?>(null);
        }

        /// <summary>
        /// 检查是否需要旋转密钥
        /// </summary>
        public async Task<bool> IsRotationRequiredAsync(string keyType)
        {
            // 简化实现：默认90天旋转一次
            var lastRotation = await GetLastRotationTimeAsync(keyType);
            if (lastRotation == null)
            {
                return false; // 如果没有记录，不强制旋转
            }

            var daysSinceRotation = (DateTime.UtcNow - lastRotation.Value).Days;
            return daysSinceRotation >= 90;
        }

        /// <summary>
        /// 获取密钥元数据
        /// </summary>
        public async Task<KeyMetadata?> GetKeyMetadataAsync(string keyType)
        {
            // 简化实现：返回基本元数据
            return await Task.FromResult(new KeyMetadata
            {
                KeyType = keyType,
                CreatedAt = DateTime.UtcNow.AddDays(-30), // 模拟数据
                RotationIntervalDays = 90,
                Version = "1.0",
                IsActive = true
            });
        }

        #region 私有辅助方法

        /// <summary>
        /// 生成安全的随机密钥
        /// </summary>
        private static string GenerateSecureKey(int length)
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[length];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        #endregion
    }
}

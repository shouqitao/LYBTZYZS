using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Module.Auth.Interfaces;

namespace LYBT.Module.Auth.Services
{
    /// <summary>
    /// 密钥管理服务实现 - 支持开发环境和生产环境的不同密钥存储策略
    /// </summary>
    public class SecurityKeyService : ISecurityKeyService
    {
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SecurityKeyService> _logger;
        private readonly JwtOptions _jwtOptions;
        private readonly string _environment;

        private const string CURRENT_KEY_CACHE = "JWT_CURRENT_KEY";
        private const string ALL_KEYS_CACHE = "JWT_ALL_KEYS";
        private const int CACHE_DURATION_MINUTES = 60;
        private const int KEY_SIZE_BYTES = 64; // 512 bits

        public SecurityKeyService(
            IConfiguration configuration,
            IMemoryCache cache,
            ILogger<SecurityKeyService> logger,
            IOptions<JwtOptions> jwtOptions,
            IHostEnvironment environment)
        {
            _configuration = configuration;
            _cache = cache;
            _logger = logger;
            _jwtOptions = jwtOptions.Value;
            _environment = environment.EnvironmentName;
        }

        public async Task<SecurityKey> GetCurrentKeyAsync()
        {
            return await _cache.GetOrCreateAsync(CURRENT_KEY_CACHE, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_DURATION_MINUTES);

                if (IsProduction())
                {
                    // 生产环境：从Azure Key Vault或环境变量读取
                    return await GetProductionKeyAsync();
                }
                else
                {
                    // 开发环境：从用户机密或配置文件读取
                    return GetDevelopmentKey();
                }
            });
        }

        public async Task<IEnumerable<SecurityKey>> GetAllKeysAsync()
        {
            return await _cache.GetOrCreateAsync(ALL_KEYS_CACHE, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_DURATION_MINUTES);

                var keys = new List<SecurityKey>();

                // 添加当前密钥
                keys.Add(await GetCurrentKeyAsync());

                // 添加备用密钥（用于密钥轮换期间的验证）
                var secondaryKey = await GetSecondaryKeyAsync();
                if (secondaryKey != null)
                {
                    keys.Add(secondaryKey);
                }

                return keys;
            });
        }

        public async Task RotateKeyAsync()
        {
            try
            {
                _logger.LogInformation("开始执行密钥轮换");

                // 生成新密钥
                var newKey = GenerateSecureKey();

                // 将当前密钥移至备用位置
                var currentKey = await GetCurrentKeyAsync();
                await StoreSecondaryKeyAsync(GetKeyString(currentKey));

                // 存储新密钥为当前密钥
                await StorePrimaryKeyAsync(newKey);

                // 清除缓存
                _cache.Remove(CURRENT_KEY_CACHE);
                _cache.Remove(ALL_KEYS_CACHE);

                _logger.LogInformation("密钥轮换成功完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "密钥轮换失败");
                throw new InvalidOperationException("密钥轮换失败", ex);
            }
        }

        public async Task<string> GetCurrentKeyIdAsync()
        {
            // 使用密钥的SHA256哈希作为ID
            var key = await GetCurrentKeyAsync();
            var keyBytes = GetKeyBytes(key);
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(keyBytes);
            return Convert.ToBase64String(hash).Substring(0, 8);
        }

        public async Task<SecurityKey?> GetKeyByIdAsync(string keyId)
        {
            var allKeys = await GetAllKeysAsync();

            foreach (var key in allKeys)
            {
                var keyBytes = GetKeyBytes(key);
                using var sha256 = SHA256.Create();
                var hash = sha256.ComputeHash(keyBytes);
                var id = Convert.ToBase64String(hash).Substring(0, 8);

                if (id == keyId)
                {
                    return key;
                }
            }

            return null;
        }

        private async Task<SecurityKey> GetProductionKeyAsync()
        {
            // 优先级：
            // 1. Azure Key Vault (需要配置)
            // 2. 环境变量
            // 3. 配置文件（不推荐用于生产）

            var keyString = Environment.GetEnvironmentVariable("JWT_SIGNING_KEY");

            if (string.IsNullOrEmpty(keyString))
            {
                // 如果未配置环境变量，尝试从配置读取（应该避免）
                keyString = _configuration["Authentication:Jwt:ProductionKey"];

                if (!string.IsNullOrEmpty(keyString))
                {
                    _logger.LogWarning("生产环境使用配置文件中的密钥，建议使用环境变量或Key Vault");
                }
            }

            if (string.IsNullOrEmpty(keyString))
            {
                throw new InvalidOperationException("生产环境JWT密钥未配置");
            }

            return new SymmetricSecurityKey(Convert.FromBase64String(keyString));
        }

        private SecurityKey GetDevelopmentKey()
        {
            // 开发环境：从用户机密或配置文件读取
            var keyString = _configuration["Authentication:Jwt:DevelopmentKey"];

            if (string.IsNullOrEmpty(keyString))
            {
                // 如果没有配置，使用JwtOptions中的Secret（向后兼容）
                keyString = _jwtOptions.Secret;

                if (string.IsNullOrEmpty(keyString))
                {
                    // 生成并显示开发用密钥
                    keyString = GenerateSecureKey();
                    _logger.LogWarning($"开发环境密钥未配置，已生成临时密钥。请将以下密钥添加到用户机密：\n{keyString}");
                }
            }

            // 如果是Base64编码的，解码
            if (IsBase64String(keyString))
            {
                return new SymmetricSecurityKey(Convert.FromBase64String(keyString));
            }
            else
            {
                // 如果是纯文本，使用UTF8编码
                return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            }
        }

        private async Task<SecurityKey?> GetSecondaryKeyAsync()
        {
            var keyString = IsProduction()
                ? Environment.GetEnvironmentVariable("JWT_SIGNING_KEY_SECONDARY")
                : _configuration["Authentication:Jwt:SecondaryKey"];

            if (string.IsNullOrEmpty(keyString))
            {
                return null;
            }

            if (IsBase64String(keyString))
            {
                return new SymmetricSecurityKey(Convert.FromBase64String(keyString));
            }
            else
            {
                return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            }
        }

        private async Task StorePrimaryKeyAsync(string keyString)
        {
            if (IsProduction())
            {
                // 生产环境：需要更新环境变量或Key Vault
                // 这里只记录日志，实际部署需要DevOps流程
                _logger.LogInformation($"新的主密钥已生成，请更新环境变量JWT_SIGNING_KEY");
            }
            else
            {
                // 开发环境：可以更新用户机密
                _logger.LogInformation($"新的主密钥：{keyString}");
            }
        }

        private async Task StoreSecondaryKeyAsync(string keyString)
        {
            if (IsProduction())
            {
                _logger.LogInformation($"请更新环境变量JWT_SIGNING_KEY_SECONDARY");
            }
            else
            {
                _logger.LogInformation($"备用密钥：{keyString}");
            }
        }

        private string GenerateSecureKey()
        {
            using var rng = RandomNumberGenerator.Create();
            var keyBytes = new byte[KEY_SIZE_BYTES];
            rng.GetBytes(keyBytes);
            return Convert.ToBase64String(keyBytes);
        }

        private bool IsProduction()
        {
            return _environment?.ToLower() == "production";
        }

        private bool IsBase64String(string s)
        {
            if (string.IsNullOrEmpty(s))
                return false;

            try
            {
                Convert.FromBase64String(s);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private byte[] GetKeyBytes(SecurityKey key)
        {
            if (key is SymmetricSecurityKey symmetricKey)
            {
                return symmetricKey.Key;
            }
            throw new NotSupportedException("仅支持对称密钥");
        }

        private string GetKeyString(SecurityKey key)
        {
            var bytes = GetKeyBytes(key);
            return Convert.ToBase64String(bytes);
        }
    }
}
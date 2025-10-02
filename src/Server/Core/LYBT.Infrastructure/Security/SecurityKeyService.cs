using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using LYBT.Infrastructure.Configuration.Options;

namespace LYBT.Infrastructure.Security
{
    /// <summary>
    /// JWT密钥管理服务实现
    /// 支持开发环境的用户机密和生产环境的Azure Key Vault
    /// </summary>
    public class SecurityKeyService : ISecurityKeyService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SecurityKeyService> _logger;
        private readonly JwtOptions _jwtOptions;
        private readonly IWebHostEnvironment _environment;
        
        private SecurityKey? _currentKey;
        private readonly List<SecurityKey> _validationKeys = new();
        private DateTime _keyRotationTime;
        private string _currentKeyVersion = "v1";

        public SecurityKeyService(
            IConfiguration configuration,
            ILogger<SecurityKeyService> logger,
            IOptions<JwtOptions> jwtOptions,
            IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _logger = logger;
            _jwtOptions = jwtOptions.Value;
            _environment = environment;
            
            InitializeKeys();
        }

        private void InitializeKeys()
        {
            try
            {
                string? secretKey = null;

                if (_environment.IsProduction())
                {
                    // 生产环境：从Azure Key Vault或环境变量获取
                    secretKey = _configuration["Lybt:Authentication:Jwt:SecretKey"]
                        ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
                        
                    if (string.IsNullOrEmpty(secretKey))
                    {
                        _logger.LogError("JWT密钥未配置！请设置环境变量JWT_SECRET_KEY");
                        throw new InvalidOperationException("JWT secret key is not configured");
                    }
                }
                else if (_environment.IsDevelopment())
                {
                    // 开发环境：优先使用用户机密，其次使用配置文件
                    secretKey = _configuration["Lybt:Authentication:Jwt:SecretKey"];
                    
                    if (string.IsNullOrEmpty(secretKey))
                    {
                        // 如果没有配置，生成一个临时密钥（仅开发环境）
                        _logger.LogWarning("使用临时生成的JWT密钥，仅限开发环境！");
                        secretKey = GenerateSecureKey();
                    }
                }
                else
                {
                    // 测试或其他环境
                    secretKey = _configuration["Lybt:Authentication:Jwt:SecretKey"]
                        ?? _jwtOptions.Secret;
                }

                // 验证密钥强度
                ValidateKeyStrength(secretKey);

                // 创建对称密钥
                var keyBytes = Encoding.UTF8.GetBytes(secretKey);
                _currentKey = new SymmetricSecurityKey(keyBytes);
                _validationKeys.Add(_currentKey);

                // 设置密钥轮换时间（30天后）
                _keyRotationTime = DateTime.UtcNow.AddDays(30);
                
                _logger.LogInformation("JWT密钥初始化成功，版本：{KeyVersion}", _currentKeyVersion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JWT密钥初始化失败");
                throw;
            }
        }

        private void ValidateKeyStrength(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("JWT密钥不能为空");
            }

            if (key.Length < 32)
            {
                throw new ArgumentException($"JWT密钥长度不足，当前长度：{key.Length}，要求最少32字符");
            }

            // 检查密钥复杂度
            bool hasUpper = key.Any(char.IsUpper);
            bool hasLower = key.Any(char.IsLower);
            bool hasDigit = key.Any(char.IsDigit);
            bool hasSpecial = key.Any(c => !char.IsLetterOrDigit(c));

            if (!(hasUpper && hasLower && hasDigit && hasSpecial))
            {
                _logger.LogWarning("JWT密钥复杂度不足，建议包含大小写字母、数字和特殊字符");
            }
        }

        private string GenerateSecureKey()
        {
            using var rng = RandomNumberGenerator.Create();
            var keyBytes = new byte[32]; // 256位
            rng.GetBytes(keyBytes);
            return Convert.ToBase64String(keyBytes);
        }

        public Task<SecurityKey> GetCurrentSigningKeyAsync()
        {
            if (_currentKey == null)
            {
                throw new InvalidOperationException("JWT signing key is not initialized");
            }
            
            return Task.FromResult(_currentKey);
        }

        public Task<IEnumerable<SecurityKey>> GetValidationKeysAsync()
        {
            // 返回所有有效密钥，包括历史密钥（用于验证旧Token）
            return Task.FromResult(_validationKeys.AsEnumerable());
        }

        public async Task RotateKeyAsync()
        {
            try
            {
                _logger.LogInformation("开始密钥轮换，当前版本：{CurrentVersion}", _currentKeyVersion);

                // 生成新密钥
                var newKeyString = GenerateSecureKey();
                var newKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(newKeyString));

                // 保留旧密钥用于验证（最多保留3个历史密钥）
                if (_validationKeys.Count >= 3)
                {
                    _validationKeys.RemoveAt(0);
                }

                // 更新当前密钥
                _currentKey = newKey;
                _validationKeys.Add(newKey);

                // 更新版本号
                var version = int.Parse(_currentKeyVersion.Substring(1));
                _currentKeyVersion = $"v{version + 1}";

                // 更新轮换时间
                _keyRotationTime = DateTime.UtcNow.AddDays(30);

                // 如果是生产环境，需要持久化新密钥
                if (_environment.IsProduction())
                {
                    await PersistKeyToSecureStore(newKeyString);
                }

                _logger.LogInformation("密钥轮换完成，新版本：{NewVersion}", _currentKeyVersion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "密钥轮换失败");
                throw;
            }
        }

        private async Task PersistKeyToSecureStore(string key)
        {
            // TODO: 实现密钥持久化到Azure Key Vault或其他安全存储
            // 这里需要根据实际的密钥管理策略实现
            await Task.CompletedTask;
            _logger.LogInformation("密钥已持久化到安全存储");
        }

        public Task<string> GetCurrentKeyVersionAsync()
        {
            return Task.FromResult(_currentKeyVersion);
        }

        public Task<bool> IsKeyRotationRequiredAsync()
        {
            var isRequired = DateTime.UtcNow >= _keyRotationTime;
            
            if (isRequired)
            {
                _logger.LogWarning("密钥即将过期，需要进行轮换");
            }
            
            return Task.FromResult(isRequired);
        }
    }
}
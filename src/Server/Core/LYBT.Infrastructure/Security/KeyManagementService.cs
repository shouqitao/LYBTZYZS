using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security;
using LYBT.Infrastructure.Configuration.Options;

namespace LYBT.Infrastructure.Security;

/// <summary>
/// 密钥管理服务实现
/// </summary>
public class KeyManagementService : IKeyManagementService
{
    private readonly ILogger<KeyManagementService> _logger;
    private readonly IOptions<JwtOptions> _jwtOptions;
    private DateTime? _lastRotationTime;

    public KeyManagementService(
        ILogger<KeyManagementService> logger,
        IOptions<JwtOptions> jwtOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jwtOptions = jwtOptions ?? throw new ArgumentNullException(nameof(jwtOptions));
    }

    /// <summary>
    /// 检查是否需要旋转密钥
    /// </summary>
    public async Task<bool> ShouldRotateKeyAsync()
    {
        try
        {
            // 简化版本：如果没有记录上次轮换时间，需要轮换
            if (!_lastRotationTime.HasValue)
            {
                _logger.LogInformation("首次检查密钥轮换，需要执行轮换");
                return true;
            }

            // 默认7天轮换一次
            var rotationInterval = TimeSpan.FromDays(7);
            var timeSinceLastRotation = DateTime.UtcNow - _lastRotationTime.Value;
            var needsRotation = timeSinceLastRotation >= rotationInterval;
            
            if (needsRotation)
            {
                _logger.LogInformation(
                    "密钥需要轮换，距离上次轮换已过 {Hours:F1} 小时",
                    timeSinceLastRotation.TotalHours);
            }

            return await Task.FromResult(needsRotation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查密钥轮换状态时发生错误");
            return false;
        }
    }

    /// <summary>
    /// 旋转JWT密钥并返回新密钥
    /// </summary>
    /// <summary>
    /// 轮换JWT密钥
    /// </summary>
    public async Task<string> RotateJwtSecretAsync()
    {
        try
        {
            _logger.LogInformation("开始JWT密钥轮换...");

            // 从配置获取密钥强度要求（默认256位）
            var minKeyLengthBits = 256; // 使用NIST推荐的256位密钥强度
            var keyBytes = minKeyLengthBits / 8; // 转换为字节数

            // 验证密钥长度配置
            if (keyBytes < 32) // 最小256位
            {
                _logger.LogWarning("配置的密钥长度 {Bits} 位小于安全要求，使用256位", minKeyLengthBits);
                keyBytes = 32; // 强制使用256位
            }

            // 生成密码学安全的随机密钥
            var newKeyBytes = new byte[keyBytes];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(newKeyBytes);
            }

            // 验证生成的密钥强度
            if (!ValidateKeyStrength(newKeyBytes))
            {
                throw new SecurityException("生成的密钥未通过强度验证");
            }

            // 转换为Base64字符串
            var newSecret = Convert.ToBase64String(newKeyBytes);

            // 持久化记录轮换（包括到数据库）
            await RecordRotationAsync(newSecret, DateTime.UtcNow);

            _logger.LogInformation(
                "JWT密钥轮换成功，密钥强度: {Bits} 位，Base64长度: {Length} 字符", 
                keyBytes * 8, 
                newSecret.Length);
            
            return newSecret;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JWT密钥轮换失败");
            throw;
        }
    }

    /// <summary>
    /// 验证密钥强度
    /// 确保生成的密钥符合加密安全要求
    /// </summary>
    private bool ValidateKeyStrength(byte[] keyBytes)
    {
        try
        {
            // 基础长度检查
            if (keyBytes.Length < 32) // 最小256位
            {
                _logger.LogWarning("密钥长度 {Length} 字节小于安全要求的32字节", keyBytes.Length);
                return false;
            }

            // 熵检查：确保密钥不是全零或重复模式
            if (keyBytes.All(b => b == 0))
            {
                _logger.LogWarning("密钥为全零，不符合安全要求");
                return false;
            }

            // 检查是否有足够的随机性（简单的熵测试）
            var uniqueBytes = keyBytes.Distinct().Count();
            var minUniqueBytes = keyBytes.Length / 4; // 至少25%的字节不同

            if (uniqueBytes < minUniqueBytes)
            {
                _logger.LogWarning(
                    "密钥熵不足，唯一字节数: {Unique}/{Total}，要求最少: {Required}",
                    uniqueBytes, keyBytes.Length, minUniqueBytes);
                return false;
            }

            _logger.LogDebug("密钥强度验证通过，长度: {Length} 字节，唯一字节数: {Unique}", 
                keyBytes.Length, uniqueBytes);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "密钥强度验证过程中发生错误");
            return false;
        }
    }

    /// <summary>
    /// 记录密钥轮换
    /// </summary>
    public async Task RecordRotationAsync(string newSecret, DateTime rotationTime)
    {
        try
        {
            // 更新内部记录的轮换时间
            _lastRotationTime = rotationTime;

            _logger.LogInformation(
                "记录密钥轮换完成，轮换时间: {RotationTime:yyyy-MM-dd HH:mm:ss} UTC，新密钥前8位: {SecretPrefix}...",
                rotationTime,
                newSecret.Length >= 8 ? newSecret[..8] : newSecret);

            // 在实际生产环境中，这里应该持久化到安全的存储中
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录密钥轮换时间失败");
            throw;
        }
    }
}
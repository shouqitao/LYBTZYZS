using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    public async Task<string> RotateJwtSecretAsync()
    {
        try
        {
            _logger.LogInformation("开始生成新的JWT密钥");

            // 生成新的安全密钥（256位 = 32字节）
            var keyBytes = new byte[32];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(keyBytes);
            }

            // 转换为Base64字符串
            var newSecret = Convert.ToBase64String(keyBytes);

            // 记录轮换
            await RecordRotationAsync(newSecret, DateTime.UtcNow);

            _logger.LogInformation("JWT密钥轮换成功，新密钥长度: {Length} 字符", newSecret.Length);
            
            return newSecret;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JWT密钥轮换失败");
            throw;
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
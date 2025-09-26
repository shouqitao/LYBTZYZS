using System;
using System.ComponentModel.DataAnnotations;
using LYBT.Entities.Common;

namespace LYBT.Entities.Auth
{
    /// <summary>
    /// RefreshToken实体 - 用于管理JWT刷新令牌
    /// </summary>
    /// <summary>
    /// Token安全验证结果
    /// </summary>
    public class TokenSecurityValidationResult
    {
        /// <summary>
        /// 是否通过验证
        /// </summary>
        public bool IsValid { get; set; } = true;

        /// <summary>
        /// 安全等级
        /// </summary>
        public TokenSecurityLevel SecurityLevel { get; set; } = TokenSecurityLevel.Low;

        /// <summary>
        /// 验证失败或警告原因
        /// </summary>
        public List<string> Reasons { get; set; } = new List<string>();

        /// <summary>
        /// 是否需要额外验证（如二次验证）
        /// </summary>
        public bool RequiresAdditionalVerification => SecurityLevel >= TokenSecurityLevel.Medium;
    }

    /// <summary>
    /// Token安全等级
    /// </summary>
    public enum TokenSecurityLevel
    {
        /// <summary>
        /// 低风险：正常使用
        /// </summary>
        Low = 0,

        /// <summary>
        /// 中等风险：建议额外验证
        /// </summary>
        Medium = 1,

        /// <summary>
        /// 高风险：拒绝访问或强制重新认证
        /// </summary>
        High = 2
    }

    public class RefreshToken : BaseEntity
    {
        /// <summary>
        /// 令牌值（唯一）
        /// </summary>
        [Required]
        [MaxLength(512)]
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// JWT ID (jti) - 关联的AccessToken标识
        /// </summary>
        [Required]
        [MaxLength(128)]
        public string JwtId { get; set; } = string.Empty;

        /// <summary>
        /// 关联的用户ID
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 过期时间
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// 是否已使用
        /// </summary>
        public bool IsUsed { get; set; }

        /// <summary>
        /// 是否已撤销
        /// </summary>
        public bool IsRevoked { get; set; }

        /// <summary>
        /// 使用时间
        /// </summary>
        public DateTime? UsedAt { get; set; }

        /// <summary>
        /// 撤销时间
        /// </summary>
        public DateTime? RevokedAt { get; set; }

        /// <summary>
        /// 撤销原因
        /// </summary>
        [MaxLength(500)]
        public string? RevokedReason { get; set; }

        /// <summary>
        /// 客户端信息（User-Agent）
        /// </summary>
        [MaxLength(1000)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// 客户端IP地址
        /// </summary>
        [MaxLength(45)] // IPv6最长45字符
        public string? IpAddress { get; set; }

        /// <summary>
        /// 设备标识（可选，用于设备管理）
        /// </summary>
        [MaxLength(128)]
        public string? DeviceId { get; set; }
        /// <summary>
        /// 设备指纹（用于设备绑定验证）
        /// </summary>
        [MaxLength(256)]
        public string? DeviceFingerprint { get; set; }

        /// <summary>
        /// 地理位置信息（可选，用于异常检测）
        /// </summary>
        [MaxLength(100)]
        public string? GeolocationInfo { get; set; }

        /// <summary>
        /// 上次使用时间（用于频率检测）
        /// </summary>
        public DateTime? LastUsedAt { get; set; }

        /// <summary>
        /// 使用次数计数（用于异常检测）
        /// </summary>
        public int UsageCount { get; set; } = 0;

        /// <summary>
        /// 创建时的IP地址（用于对比检测）
        /// </summary>
        [MaxLength(45)]
        public string? OriginalIpAddress { get; set; }

        /// <summary>
        /// 可信设备标记（首次验证后设为true）
        /// </summary>
        public bool IsTrustedDevice { get; set; } = false;

        /// <summary>
        /// 检查RefreshToken是否有效
        /// </summary>
        public bool IsValid()
        {
            return !IsUsed
                && !IsRevoked
                && DateTime.UtcNow < ExpiresAt;
        }

        /// <summary>
        /// 验证设备安全性（设备指纹、IP地址等）
        /// </summary>
        /// <param name="currentDeviceFingerprint">当前设备指纹</param>
        /// <param name="currentIpAddress">当前IP地址</param>
        /// <param name="currentUserAgent">当前User-Agent</param>
        /// <returns>安全验证结果</returns>
        public TokenSecurityValidationResult ValidateDeviceSecurity(
            string? currentDeviceFingerprint,
            string? currentIpAddress,
            string? currentUserAgent)
        {
            var result = new TokenSecurityValidationResult { IsValid = true };

            // 基本有效性检查
            if (!IsValid())
            {
                result.IsValid = false;
                result.Reasons.Add("Token已过期、已使用或已撤销");
                return result;
            }

            // 设备指纹验证
            if (!string.IsNullOrEmpty(DeviceFingerprint) && 
                !string.IsNullOrEmpty(currentDeviceFingerprint) &&
                DeviceFingerprint != currentDeviceFingerprint)
            {
                result.IsValid = false;
                result.Reasons.Add("设备指纹不匹配，疑似跨设备使用");
                result.SecurityLevel = TokenSecurityLevel.High;
            }

            // IP地址异常检测
            if (!string.IsNullOrEmpty(OriginalIpAddress) && 
                !string.IsNullOrEmpty(currentIpAddress))
            {
                if (OriginalIpAddress != currentIpAddress)
                {
                    // 如果不是可信设备且IP地址发生变化，标记为中等风险
                    if (!IsTrustedDevice)
                    {
                        result.SecurityLevel = TokenSecurityLevel.Medium;
                        result.Reasons.Add($"IP地址发生变化：{OriginalIpAddress} -> {currentIpAddress}");
                    }
                }
            }

            // User-Agent变化检测
            if (!string.IsNullOrEmpty(UserAgent) && 
                !string.IsNullOrEmpty(currentUserAgent) &&
                UserAgent != currentUserAgent)
            {
                result.SecurityLevel = TokenSecurityLevel.Low;
                result.Reasons.Add("浏览器环境发生变化");
            }

            // 使用频率异常检测
            if (LastUsedAt.HasValue)
            {
                var timeSinceLastUse = DateTime.UtcNow - LastUsedAt.Value;
                if (timeSinceLastUse.TotalMinutes < 1 && UsageCount > 10) // 1分钟内使用超过10次
                {
                    result.SecurityLevel = TokenSecurityLevel.Medium;
                    result.Reasons.Add($"使用频率异常：{UsageCount}次/{timeSinceLastUse.TotalMinutes:F1}分钟");
                }
            }

            return result;
        }

        /// <summary>
        /// 记录使用情况
        /// </summary>
        /// <param name="currentIpAddress">当前IP地址</param>
        /// <param name="currentUserAgent">当前User-Agent</param>
        public void RecordUsage(string? currentIpAddress, string? currentUserAgent)
        {
            LastUsedAt = DateTime.UtcNow;
            UsageCount++;
            
            // 更新最新的访问信息
            if (!string.IsNullOrEmpty(currentIpAddress))
            {
                IpAddress = currentIpAddress;
            }
            
            if (!string.IsNullOrEmpty(currentUserAgent))
            {
                UserAgent = currentUserAgent;
            }
        }

        /// <summary>
        /// 标记为可信设备
        /// </summary>
        public void MarkAsTrusted()
        {
            IsTrustedDevice = true;
        }

        /// <summary>
        /// 标记为已使用
        /// </summary>
        public void MarkAsUsed()
        {
            IsUsed = true;
            UsedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 撤销令牌
        /// </summary>
        public void Revoke(string reason = "Manual revocation")
        {
            IsRevoked = true;
            RevokedAt = DateTime.UtcNow;
            RevokedReason = reason;
        }
    }
}
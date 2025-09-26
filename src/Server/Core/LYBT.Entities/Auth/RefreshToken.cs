using System;
using System.ComponentModel.DataAnnotations;
using LYBT.Entities.Common;

namespace LYBT.Entities.Auth
{
    /// <summary>
    /// RefreshToken实体 - 用于管理JWT刷新令牌
    /// </summary>
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
        /// 检查RefreshToken是否有效
        /// </summary>
        public bool IsValid()
        {
            return !IsUsed
                && !IsRevoked
                && DateTime.UtcNow < ExpiresAt;
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
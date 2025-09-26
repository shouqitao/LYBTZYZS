using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Entities.Common;

namespace LYBT.Entities.Auth
{
    /// <summary>
    /// RefreshToken实体
    /// 用于管理JWT刷新令牌
    /// </summary>
    public class RefreshToken : BaseEntity
    {
        /// <summary>
        /// 令牌值（加密存储）
        /// </summary>
        [Required]
        [StringLength(512)]
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// 关联的用户ID
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// JTI (JWT ID) - 关联的AccessToken唯一标识
        /// </summary>
        [Required]
        [StringLength(128)]
        public string Jti { get; set; } = string.Empty;

        /// <summary>
        /// 过期时间
        /// </summary>
        [Required]
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// 是否已撤销
        /// </summary>
        public bool IsRevoked { get; set; }

        /// <summary>
        /// 撤销原因
        /// </summary>
        [StringLength(256)]
        public string? RevokedReason { get; set; }

        /// <summary>
        /// 撤销时间
        /// </summary>
        public DateTime? RevokedAt { get; set; }

        /// <summary>
        /// 撤销者（用户ID或系统）
        /// </summary>
        [StringLength(128)]
        public string? RevokedBy { get; set; }

        /// <summary>
        /// 客户端IP地址
        /// </summary>
        [StringLength(45)] // 支持IPv6
        public string? ClientIp { get; set; }

        /// <summary>
        /// 用户代理字符串
        /// </summary>
        [StringLength(512)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// 设备标识（用于多设备管理）
        /// </summary>
        [StringLength(128)]
        public string? DeviceId { get; set; }

        /// <summary>
        /// 设备名称（友好名称）
        /// </summary>
        [StringLength(128)]
        public string? DeviceName { get; set; }

        /// <summary>
        /// 使用次数（用于监控异常使用）
        /// </summary>
        public int UsageCount { get; set; }

        /// <summary>
        /// 最后使用时间
        /// </summary>
        public DateTime? LastUsedAt { get; set; }

        /// <summary>
        /// 被替换的令牌（用于令牌轮换）
        /// </summary>
        [StringLength(512)]
        public string? ReplacedByToken { get; set; }

        /// <summary>
        /// 令牌家族ID（用于检测令牌重用攻击）
        /// </summary>
        [StringLength(128)]
        public string? FamilyId { get; set; }

        /// <summary>
        /// 是否激活（计算属性）
        /// </summary>
        [NotMapped]
        public bool IsActive => !IsRevoked && !IsDeleted && ExpiresAt > DateTime.UtcNow;

        /// <summary>
        /// 检查Token是否有效
        /// </summary>
        public bool IsValid()
        {
            return !IsRevoked && 
                   !IsDeleted && 
                   ExpiresAt > DateTime.UtcNow;
        }

        /// <summary>
        /// 撤销Token
        /// </summary>
        public void Revoke(string reason, string? revokedBy = null)
        {
            IsRevoked = true;
            RevokedReason = reason;
            RevokedAt = DateTime.UtcNow;
            RevokedBy = revokedBy ?? "System";
        }

        /// <summary>
        /// 记录使用
        /// </summary>
        public void RecordUsage()
        {
            UsageCount++;
            LastUsedAt = DateTime.UtcNow;
        }
    }
}
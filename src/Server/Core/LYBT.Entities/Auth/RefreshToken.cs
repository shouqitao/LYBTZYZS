using System;
using LYBT.Entities.Users;

namespace LYBT.Entities.Auth
{
    /// <summary>
    /// 刷新令牌实体
    /// </summary>
    public class RefreshToken
    {
        /// <summary>
        /// 刷新令牌ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 令牌值（加密存储）
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// 关联的用户ID
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// JWT ID 用于关联Access Token
        /// </summary>
        public string JwtId { get; set; } = string.Empty;

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
        /// 使用时间
        /// </summary>
        public DateTime? UsedAt { get; set; }

        /// <summary>
        /// 是否已撤销
        /// </summary>
        public bool IsRevoked { get; set; }

        /// <summary>
        /// 撤销原因
        /// </summary>
        public string? RevokedReason { get; set; }

        /// <summary>
        /// 撤销时间
        /// </summary>
        public DateTime? RevokedAt { get; set; }

        /// <summary>
        /// 撤销者用户ID
        /// </summary>
        public Guid? RevokedByUserId { get; set; }

        /// <summary>
        /// IP地址
        /// </summary>
        public string IpAddress { get; set; } = string.Empty;

        /// <summary>
        /// 用户代理字符串
        /// </summary>
        public string UserAgent { get; set; } = string.Empty;

        /// <summary>
        /// 设备ID（可选，用于多设备管理）
        /// </summary>
        public string? DeviceId { get; set; }

        /// <summary>
        /// 设备名称（可选）
        /// </summary>
        public string? DeviceName { get; set; }

        /// <summary>
        /// 刷新次数
        /// </summary>
        public int RefreshCount { get; set; }

        /// <summary>
        /// 最后刷新时间
        /// </summary>
        public DateTime? LastRefreshedAt { get; set; }

        /// <summary>
        /// 是否有效（未过期、未使用且未撤销）
        /// </summary>
        public bool IsValid => !IsRevoked && !IsUsed && ExpiresAt > DateTime.UtcNow;

        /// <summary>
        /// 是否过期
        /// </summary>
        public bool IsExpired => ExpiresAt <= DateTime.UtcNow;

        /// <summary>
        /// 关联的用户
        /// </summary>
        public virtual User? User { get; set; }
    }
}
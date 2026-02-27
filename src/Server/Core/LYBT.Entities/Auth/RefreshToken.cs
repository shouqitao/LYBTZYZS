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

        /// &lt;summary&gt;
        /// 用户类型 (Issue #1861)
        /// "superadmin" - 超级管理员（Auth模块）
        /// "user" - 普通用户（User模块）
        /// &lt;/summary&gt;
        [Required]
        [StringLength(50)]
        public string UserType { get; set; } = "user";

        /// <summary>
        /// JTI (JWT ID) - 关联的AccessToken唯一标识
        /// </summary>
        [Required]
        [StringLength(128)]
        public string Jti { get; set; } = string.Empty;

        /// <summary>
        /// 过期时间 (滑动过期，每次轮换时刷新)
        /// </summary>
        [Required]
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// T5-P2-04: 绝对过期时间 (30天，Token Family 创建时设定，轮换时继承，不可延长)
        /// </summary>
        public DateTime? AbsoluteExpiresAt { get; set; }

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
        [StringLength(200)] // 统一为200，支持较长的设备名称
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
        /// Issue #1864 AUTH-007: Token Family用于Refresh Token轮换
        /// </summary>
        [StringLength(128)]
        public string? FamilyId { get; set; }

        /// <summary>
        /// 是否已使用（用于重放攻击检测）
        /// Issue #1864 AUTH-007: 当Token被用于刷新后标记为已使用
        /// 如果已使用的Token再次被使用，则检测到重放攻击
        /// </summary>
        public bool IsUsed { get; set; }

        /// <summary>
        /// 使用时间（用于重放攻击检测）
        /// </summary>
        public DateTime? UsedAt { get; set; }

        /// <summary>
        /// 是否激活（计算属性）
        /// Issue #1864: 增加IsUsed检查，已使用的Token不再激活
        /// T5-P2-04: 增加AbsoluteExpiresAt检查
        /// </summary>
        [NotMapped]
        public bool IsActive => !IsRevoked && !IsDeleted && !IsUsed &&
                                ExpiresAt > DateTime.UtcNow &&
                                (!AbsoluteExpiresAt.HasValue || AbsoluteExpiresAt.Value > DateTime.UtcNow);

        /// <summary>
        /// 检查Token是否有效
        /// Issue #1864: 增加IsUsed检查，已使用的Token不再有效
        /// T5-P2-04: 增加30天绝对过期检查
        /// </summary>
        public bool IsValid()
        {
            return !IsRevoked &&
                   !IsDeleted &&
                   !IsUsed &&
                   ExpiresAt > DateTime.UtcNow &&
                   (!AbsoluteExpiresAt.HasValue || AbsoluteExpiresAt.Value > DateTime.UtcNow);
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

        /// <summary>
        /// 标记为已使用（用于Token轮换）
        /// Issue #1864 AUTH-007: 当Token被用于刷新后标记为已使用
        /// </summary>
        /// <param name="replacedByToken">替换此Token的新Token</param>
        public void MarkAsUsed(string? replacedByToken = null)
        {
            IsUsed = true;
            UsedAt = DateTime.UtcNow;
            ReplacedByToken = replacedByToken;
        }

        /// <summary>
        /// 检测是否为重放攻击（已使用的Token再次被使用）
        /// Issue #1864 AUTH-007: 如果Token已被使用，说明检测到重放攻击
        /// </summary>
        public bool IsReplayAttack => IsUsed;
    }
}

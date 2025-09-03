using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LYBT.Infrastructure.Security
{
    /// <summary>
    /// JWT令牌存储实体 - UltraThink安全优化
    /// 用于实现JWT令牌撤销和会话管理
    /// </summary>
    [Table("TokenStore")]
    public class TokenStoreEntity
    {
        /// <summary>令牌ID (JTI)</summary>
        [Key]
        [StringLength(32)]
        public string TokenId { get; set; } = string.Empty;

        /// <summary>用户ID</summary>
        public Guid UserId { get; set; }

        /// <summary>令牌哈希值（安全存储）</summary>
        [StringLength(128)]
        public string TokenHash { get; set; } = string.Empty;

        /// <summary>令牌类型</summary>
        [StringLength(32)]
        public string TokenType { get; set; } = "access_token";

        /// <summary>客户端IP地址</summary>
        [StringLength(45)]
        public string ClientIP { get; set; } = string.Empty;

        /// <summary>会话ID</summary>
        [StringLength(32)]
        public string? SessionId { get; set; }

        /// <summary>设备ID</summary>
        [StringLength(64)]
        public string? DeviceId { get; set; }

        /// <summary>用户代理信息</summary>
        [StringLength(512)]
        public string? UserAgent { get; set; }

        /// <summary>是否已撤销</summary>
        public bool IsRevoked { get; set; } = false;

        /// <summary>撤销原因</summary>
        [StringLength(256)]
        public string? RevokeReason { get; set; }

        /// <summary>撤销时间</summary>
        public DateTime? RevokedAt { get; set; }

        /// <summary>令牌过期时间</summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>创建时间</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>最后使用时间</summary>
        public DateTime? LastUsedAt { get; set; }

        /// <summary>使用次数</summary>
        public int UsageCount { get; set; } = 0;
    }

    /// <summary>
    /// 刷新令牌存储实体
    /// </summary>
    [Table("RefreshTokenStore")]
    public class RefreshTokenStoreEntity
    {
        /// <summary>刷新令牌ID</summary>
        [Key]
        [StringLength(128)]
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>关联的访问令牌ID</summary>
        [StringLength(32)]
        public string AccessTokenId { get; set; } = string.Empty;

        /// <summary>用户ID</summary>
        public Guid UserId { get; set; }

        /// <summary>用户名</summary>
        [StringLength(256)]
        public string Username { get; set; } = string.Empty;

        /// <summary>角色</summary>
        [StringLength(64)]
        public string Role { get; set; } = string.Empty;

        /// <summary>客户端IP地址</summary>
        [StringLength(45)]
        public string ClientIP { get; set; } = string.Empty;

        /// <summary>会话ID</summary>
        [StringLength(32)]
        public string? SessionId { get; set; }

        /// <summary>设备ID</summary>
        [StringLength(64)]
        public string? DeviceId { get; set; }

        /// <summary>是否长期令牌</summary>
        public bool IsLongTerm { get; set; } = false;

        /// <summary>是否已使用</summary>
        public bool IsUsed { get; set; } = false;

        /// <summary>是否已撤销</summary>
        public bool IsRevoked { get; set; } = false;

        /// <summary>撤销原因</summary>
        [StringLength(256)]
        public string? RevokeReason { get; set; }

        /// <summary>撤销时间</summary>
        public DateTime? RevokedAt { get; set; }

        /// <summary>过期时间</summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>创建时间</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>使用时间</summary>
        public DateTime? UsedAt { get; set; }
    }

    /// <summary>
    /// 可疑活动记录实体
    /// </summary>
    [Table("SuspiciousTokenActivity")]
    public class SuspiciousTokenActivityEntity
    {
        /// <summary>记录ID</summary>
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>活动类型</summary>
        [Required]
        [StringLength(64)]
        public string ActivityType { get; set; } = string.Empty;

        /// <summary>令牌ID（如果有）</summary>
        [StringLength(32)]
        public string? TokenId { get; set; }

        /// <summary>用户ID（如果已识别）</summary>
        public Guid? UserId { get; set; }

        /// <summary>客户端IP地址</summary>
        [StringLength(45)]
        public string? ClientIP { get; set; }

        /// <summary>用户代理</summary>
        [StringLength(512)]
        public string? UserAgent { get; set; }

        /// <summary>详细信息</summary>
        [StringLength(1024)]
        public string? Details { get; set; }

        /// <summary>严重程度：Low, Medium, High, Critical</summary>
        [StringLength(16)]
        public string Severity { get; set; } = "Medium";

        /// <summary>风险评分 (0-100)</summary>
        public int RiskScore { get; set; } = 0;

        /// <summary>是否已处理</summary>
        public bool IsHandled { get; set; } = false;

        /// <summary>处理备注</summary>
        [StringLength(512)]
        public string? HandledNote { get; set; }

        /// <summary>记录时间</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>处理时间</summary>
        public DateTime? HandledAt { get; set; }
    }
}
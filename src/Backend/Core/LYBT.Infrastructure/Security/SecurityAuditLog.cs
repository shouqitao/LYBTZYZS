using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LYBT.Infrastructure.Security
{
    /// <summary>
    /// 安全审计日志实体 - UltraThink重构安全审计架构
    /// </summary>
    [Table("SecurityAuditLogs")]
    public class SecurityAuditLog
    {
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// 事件类型
        /// </summary>
        [Required]
        public SecurityEventType EventType { get; set; }

        /// <summary>
        /// 用户ID（可为空，匿名访问时）
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// 用户名（可为空）
        /// </summary>
        [MaxLength(100)]
        public string? UserName { get; set; }

        /// <summary>
        /// 客户端IP地址
        /// </summary>
        [Required]
        [MaxLength(45)] // IPv6地址最大长度
        public string ClientIP { get; set; } = string.Empty;

        /// <summary>
        /// 用户代理字符串
        /// </summary>
        [MaxLength(500)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// 事件是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 事件详细数据（JSON格式）
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? EventData { get; set; }

        /// <summary>
        /// 会话ID
        /// </summary>
        [MaxLength(100)]
        public string? SessionId { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 威胁级别（用于安全异常事件）
        /// </summary>
        public ThreatLevel? ThreatLevel { get; set; }

        /// <summary>
        /// 地理位置信息（可选）
        /// </summary>
        [MaxLength(200)]
        public string? GeoLocation { get; set; }

        /// <summary>
        /// 事件哈希（用于检测重复事件）
        /// </summary>
        [MaxLength(64)]
        public string? EventHash { get; set; }

        /// <summary>
        /// 索引：按用户ID和时间查询
        /// </summary>
        [NotMapped]
        public static string IndexByUserAndTime => "IX_SecurityAuditLogs_UserId_CreatedAt";

        /// <summary>
        /// 索引：按IP和时间查询
        /// </summary>
        [NotMapped]
        public static string IndexByIPAndTime => "IX_SecurityAuditLogs_ClientIP_CreatedAt";

        /// <summary>
        /// 索引：按事件类型和时间查询
        /// </summary>
        [NotMapped]
        public static string IndexByEventTypeAndTime => "IX_SecurityAuditLogs_EventType_CreatedAt";
    }
}
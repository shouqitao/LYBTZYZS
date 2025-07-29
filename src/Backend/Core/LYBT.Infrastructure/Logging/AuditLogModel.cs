using System.ComponentModel;

namespace LYBT.Infrastructure.Logging {

    /// <summary>
    /// 审计日志实体模型
    /// </summary>
    public class AuditLogModel {

        /// <summary>
        /// 审计日志ID（主键）
        /// </summary>
        [DisplayName("审计日志ID（主键）")]
        public Guid Id { get; set; }

        /// <summary>
        /// 审计事件类型
        /// </summary>
        [DisplayName("审计事件类型")]
        public string? EventType { get; set; }

        /// <summary>
        /// 资源类型
        /// </summary>
        [DisplayName("资源类型")]
        public string? ResourceType { get; set; }

        /// <summary>
        /// 资源ID
        /// </summary>
        [DisplayName("资源ID")]
        public string? ResourceId { get; set; }

        /// <summary>
        /// 操作者ID
        /// </summary>
        [DisplayName("操作者ID")]
        public Guid? UserId { get; set; }

        /// <summary>
        /// 操作者名称
        /// </summary>
        [DisplayName("操作者名称")]
        public string? UserName { get; set; }

        /// <summary>
        /// 操作时间
        /// </summary>
        [DisplayName("操作时间")]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// 操作描述
        /// </summary>
        [DisplayName("操作描述")]
        public string? Description { get; set; }

        /// <summary>
        /// 变更前数据
        /// </summary>
        [DisplayName("变更前数据")]
        public string? OldValues { get; set; }

        /// <summary>
        /// 变更后数据
        /// </summary>
        [DisplayName("变更后数据")]
        public string? NewValues { get; set; }

        /// <summary>
        /// 变更字段
        /// </summary>
        [DisplayName("变更字段")]
        public string? ChangedFields { get; set; }

        /// <summary>
        /// 客户端IP
        /// </summary>
        [DisplayName("客户端IP")]
        public string? ClientIP { get; set; }

        /// <summary>
        /// 会话ID
        /// </summary>
        [DisplayName("会话ID")]
        public string? SessionId { get; set; }

        /// <summary>
        /// 请求ID
        /// </summary>
        [DisplayName("请求ID")]
        public string? RequestId { get; set; }

        /// <summary>
        /// 操作结果
        /// </summary>
        [DisplayName("操作结果")]
        public string? Result { get; set; }

        /// <summary>
        /// 风险级别
        /// </summary>
        [DisplayName("风险级别")]
        public string? RiskLevel { get; set; }

        /// <summary>
        /// 合规标记
        /// </summary>
        [DisplayName("合规标记")]
        public string? ComplianceFlags { get; set; }
    }
}
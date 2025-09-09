using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LYBT.Entities.Common
{
    /// <summary>
    /// 事务日志实体
    /// </summary>
    [Table("TransactionLogs")]
    public class TransactionLog
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 事务ID
        /// </summary>
        [Required]
        public Guid TransactionId { get; set; }

        /// <summary>
        /// 事务名称
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string TransactionName { get; set; } = string.Empty;

        /// <summary>
        /// 事务状态
        /// </summary>
        [Required]
        public int Status { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        [Required]
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 执行时长（毫秒）
        /// </summary>
        public long? DurationMs { get; set; }

        /// <summary>
        /// 关联用户ID
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// 实体ID（JSON格式存储）
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? EntityIds { get; set; }

        /// <summary>
        /// 异常信息
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? Exception { get; set; }

        /// <summary>
        /// 事务上下文数据快照（JSON格式）
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? ContextSnapshot { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LYBT.Entities.Common
{
    /// <summary>
    /// 事务步骤日志实体
    /// </summary>
    [Table("TransactionStepLogs")]
    public class TransactionStepLog
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
        /// 步骤名称
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string StepName { get; set; } = string.Empty;

        /// <summary>
        /// 步骤执行顺序
        /// </summary>
        [Required]
        public int StepOrder { get; set; }

        /// <summary>
        /// 步骤状态
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
        /// 异常信息
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? Exception { get; set; }

        /// <summary>
        /// 步骤元数据（JSON格式）
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? Metadata { get; set; }

        /// <summary>
        /// 是否为补偿操作
        /// </summary>
        [Required]
        public bool IsCompensation { get; set; }

        /// <summary>
        /// 重试次数
        /// </summary>
        [Required]
        public int RetryCount { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 关联的事务日志
        /// </summary>
        [ForeignKey(nameof(TransactionId))]
        public virtual TransactionLog? TransactionLog { get; set; }
    }
}
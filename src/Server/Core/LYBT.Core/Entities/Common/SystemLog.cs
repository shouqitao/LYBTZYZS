using System.ComponentModel.DataAnnotations;

namespace LYBT.Core.Entities.Common
{
    /// <summary>
    /// 系统日志实体 - SQL Server存储
    /// </summary>
    public class SystemLog
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 日志时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 日志级别 (Information, Warning, Error, Debug等)
        /// </summary>
        [MaxLength(50)]
        public string Level { get; set; } = string.Empty;

        /// <summary>
        /// 日志消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 异常信息
        /// </summary>
        public string? Exception { get; set; }

        /// <summary>
        /// 日志来源 (类名或模块名)
        /// </summary>
        [MaxLength(255)]
        public string? LoggerName { get; set; }

        /// <summary>
        /// 用户ID (如果可用)
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// 请求ID (用于跟踪请求)
        /// </summary>
        [MaxLength(36)]
        public string? RequestId { get; set; }

        /// <summary>
        /// 机器名
        /// </summary>
        [MaxLength(100)]
        public string? MachineName { get; set; }

        /// <summary>
        /// 线程ID
        /// </summary>
        public int? ThreadId { get; set; }

        /// <summary>
        /// 扩展属性 (JSON格式)
        /// </summary>
        public string? Properties { get; set; }
    }
}
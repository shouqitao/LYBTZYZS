using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace LYBT.Infrastructure.Logging {

    /// <summary>
    /// 系统日志实体模型
    /// </summary>
    public class SystemLogModel {

        /// <summary>
        /// 日志ID（主键）
        /// </summary>
        [DisplayName("日志ID（主键）")]
        public Guid Id { get; set; }

        /// <summary>
        /// 日志级别（信息、警告、错误、致命）
        /// </summary>
        [DisplayName("日志级别（信息、警告、错误、致命）")]
        public LogLevel Level { get; set; }

        /// <summary>
        /// 日志来源（模块名称）
        /// </summary>
        [DisplayName("日志来源（模块名称）")]
        public string? Source { get; set; }

        /// <summary>
        /// 日志消息
        /// </summary>
        [DisplayName("日志消息")]
        public string? Message { get; set; }

        /// <summary>
        /// 异常详情（JSON格式）
        /// </summary>
        [DisplayName("异常详情（JSON格式）")]
        public string? Exception { get; set; }

        /// <summary>
        /// 日志时间
        /// </summary>
        [DisplayName("日志时间")]
        public DateTime LogTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 服务器信息
        /// </summary>
        [DisplayName("服务器信息")]
        public string? ServerInfo { get; set; }

        /// <summary>
        /// 用户ID（可选）
        /// </summary>
        [DisplayName("用户ID（可选）")]
        public Guid? UserId { get; set; }

        /// <summary>
        /// 请求ID（用于链路追踪）
        /// </summary>
        [DisplayName("请求ID（用于链路追踪）")]
        public string? RequestId { get; set; }
    }
}
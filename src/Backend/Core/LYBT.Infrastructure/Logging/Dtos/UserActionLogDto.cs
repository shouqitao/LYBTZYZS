using LYBT.Infrastructure.Logging.Enums;
using System.ComponentModel;

namespace LYBT.Infrastructure.Logging.Dtos {

    /// <summary>
    /// 用户操作日志传输对象
    /// </summary>
    public class UserActionLogDto {

        /// <summary>
        /// 日志ID
        /// </summary>
        [DisplayName("日志ID")]
        public Guid Id { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        [DisplayName("用户ID")]
        public Guid UserId { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        [DisplayName("用户名")]
        public string? UserName { get; set; }

        /// <summary>
        /// 操作类型
        /// </summary>
        [DisplayName("操作类型")]
        public LogActionType ActionType { get; set; }

        /// <summary>
        /// 操作模块
        /// </summary>
        [DisplayName("操作模块")]
        public string? Module { get; set; }

        /// <summary>
        /// 操作功能
        /// </summary>
        [DisplayName("操作功能")]
        public string? Function { get; set; }

        /// <summary>
        /// 操作描述
        /// </summary>
        [DisplayName("操作描述")]
        public string? Description { get; set; }

        /// <summary>
        /// 请求路径
        /// </summary>
        [DisplayName("请求路径")]
        public string? RequestPath { get; set; }

        /// <summary>
        /// 请求方法
        /// </summary>
        [DisplayName("请求方法")]
        public string? HttpMethod { get; set; }

        /// <summary>
        /// 请求参数
        /// </summary>
        [DisplayName("请求参数")]
        public string? Parameters { get; set; }

        /// <summary>
        /// 操作结果
        /// </summary>
        [DisplayName("操作结果")]
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        [DisplayName("错误消息")]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 客户端IP
        /// </summary>
        [DisplayName("客户端IP")]
        public string? ClientIP { get; set; }

        /// <summary>
        /// 用户代理
        /// </summary>
        [DisplayName("用户代理")]
        public string? UserAgent { get; set; }

        /// <summary>
        /// 操作时间
        /// </summary>
        [DisplayName("操作时间")]
        public DateTime ActionTime { get; set; }

        /// <summary>
        /// 执行耗时（毫秒）
        /// </summary>
        [DisplayName("执行耗时（毫秒）")]
        public long Duration { get; set; }
    }
}
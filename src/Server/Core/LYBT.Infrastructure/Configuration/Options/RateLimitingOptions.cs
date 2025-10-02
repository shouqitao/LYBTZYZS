using System.ComponentModel.DataAnnotations;

namespace LYBT.Infrastructure.Configuration.Options
{
    /// <summary>
    /// 速率限制配置选项
    /// </summary>
    public class RateLimitingOptions
    {
        /// <summary>
        /// 配置节名称
        /// </summary>
        public const string SectionName = "RateLimiting";

        /// <summary>
        /// 全局速率限制配置
        /// </summary>
        public GlobalRateLimitConfig Global { get; set; } = new();

        /// <summary>
        /// 登录端点速率限制配置
        /// </summary>
        public LoginRateLimitConfig Login { get; set; } = new();

        /// <summary>
        /// API端点速率限制配置
        /// </summary>
        public ApiRateLimitConfig Api { get; set; } = new();

        /// <summary>
        /// IP白名单
        /// </summary>
        public List<string> WhitelistedIPs { get; set; } = new();

        /// <summary>
        /// 是否启用速率限制
        /// </summary>
        public bool Enabled { get; set; } = true;
    }

    /// <summary>
    /// 全局速率限制配置
    /// </summary>
    public class GlobalRateLimitConfig
    {
        /// <summary>
        /// 每分钟允许的请求数
        /// </summary>
        [Range(1, 10000)]
        public int PermitLimit { get; set; } = 120;

        /// <summary>
        /// 时间窗口（秒）
        /// </summary>
        [Range(1, 3600)]
        public int WindowSeconds { get; set; } = 60;

        /// <summary>
        /// 队列限制
        /// </summary>
        [Range(0, 1000)]
        public int QueueLimit { get; set; } = 60;
    }

    /// <summary>
    /// 登录端点速率限制配置
    /// </summary>
    public class LoginRateLimitConfig
    {
        /// <summary>
        /// 每分钟允许的登录尝试次数
        /// </summary>
        [Range(1, 100)]
        public int PermitLimit { get; set; } = 30;

        /// <summary>
        /// 内网每分钟允许的登录尝试次数
        /// </summary>
        [Range(1, 10000)]
        public int InternalPermitLimit { get; set; } = 1000;

        /// <summary>
        /// 时间窗口（秒）
        /// </summary>
        [Range(1, 3600)]
        public int WindowSeconds { get; set; } = 60;

        /// <summary>
        /// 队列限制
        /// </summary>
        [Range(0, 1000)]
        public int QueueLimit { get; set; } = 20;

        /// <summary>
        /// 内网队列限制
        /// </summary>
        [Range(0, 1000)]
        public int InternalQueueLimit { get; set; } = 200;
    }

    /// <summary>
    /// API端点速率限制配置
    /// </summary>
    public class ApiRateLimitConfig
    {
        /// <summary>
        /// 每分钟允许的请求数（普通用户）
        /// </summary>
        [Range(1, 10000)]
        public int UserPermitLimit { get; set; } = 60;

        /// <summary>
        /// 每分钟允许的请求数（管理员）
        /// </summary>
        [Range(1, 10000)]
        public int AdminPermitLimit { get; set; } = 200;

        /// <summary>
        /// 时间窗口（秒）
        /// </summary>
        [Range(1, 3600)]
        public int WindowSeconds { get; set; } = 60;

        /// <summary>
        /// 队列限制
        /// </summary>
        [Range(0, 1000)]
        public int QueueLimit { get; set; } = 30;
    }
}

using System.ComponentModel;

namespace LYBT.Infrastructure.Logging {

    /// <summary>
    /// 性能日志实体模型
    /// </summary>
    public class PerformanceLogModel {

        /// <summary>
        /// 性能日志ID（主键）
        /// </summary>
        [DisplayName("性能日志ID（主键）")]
        public Guid Id { get; set; }

        /// <summary>
        /// 操作名称
        /// </summary>
        [DisplayName("操作名称")]
        public string? OperationName { get; set; }

        /// <summary>
        /// 模块名称
        /// </summary>
        [DisplayName("模块名称")]
        public string? ModuleName { get; set; }

        /// <summary>
        /// 方法名称
        /// </summary>
        [DisplayName("方法名称")]
        public string? MethodName { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        [DisplayName("开始时间")]
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        [DisplayName("结束时间")]
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 执行时长（毫秒）
        /// </summary>
        [DisplayName("执行时长（毫秒）")]
        public long Duration { get; set; }

        /// <summary>
        /// CPU使用率
        /// </summary>
        [DisplayName("CPU使用率")]
        public double? CpuUsage { get; set; }

        /// <summary>
        /// 内存使用（字节）
        /// </summary>
        [DisplayName("内存使用（字节）")]
        public long? MemoryUsage { get; set; }

        /// <summary>
        /// 数据库查询次数
        /// </summary>
        [DisplayName("数据库查询次数")]
        public int? DatabaseQueries { get; set; }

        /// <summary>
        /// 缓存命中次数
        /// </summary>
        [DisplayName("缓存命中次数")]
        public int? CacheHits { get; set; }

        /// <summary>
        /// 缓存未命中次数
        /// </summary>
        [DisplayName("缓存未命中次数")]
        public int? CacheMisses { get; set; }

        /// <summary>
        /// HTTP状态码
        /// </summary>
        [DisplayName("HTTP状态码")]
        public int? HttpStatusCode { get; set; }

        /// <summary>
        /// 请求大小（字节）
        /// </summary>
        [DisplayName("请求大小（字节）")]
        public long? RequestSize { get; set; }

        /// <summary>
        /// 响应大小（字节）
        /// </summary>
        [DisplayName("响应大小（字节）")]
        public long? ResponseSize { get; set; }

        /// <summary>
        /// 用户ID
        /// </summary>
        [DisplayName("用户ID")]
        public Guid? UserId { get; set; }

        /// <summary>
        /// 客户端IP
        /// </summary>
        [DisplayName("客户端IP")]
        public string? ClientIP { get; set; }

        /// <summary>
        /// 请求路径
        /// </summary>
        [DisplayName("请求路径")]
        public string? RequestPath { get; set; }

        /// <summary>
        /// 性能级别（正常、慢、超慢）
        /// </summary>
        [DisplayName("性能级别（正常、慢、超慢）")]
        public string? PerformanceLevel { get; set; }

        /// <summary>
        /// 额外数据（JSON格式）
        /// </summary>
        [DisplayName("额外数据（JSON格式）")]
        public string? AdditionalData { get; set; }
    }
}
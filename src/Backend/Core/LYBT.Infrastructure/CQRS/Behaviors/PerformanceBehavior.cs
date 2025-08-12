using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.CQRS.Behaviors
{
    /// <summary>
    /// 性能监控行为管道 - UltraThink重构架构
    /// 监控CQRS操作的执行性能，识别慢查询和瓶颈
    /// </summary>
    /// <typeparam name="TRequest">请求类型</typeparam>
    /// <typeparam name="TResponse">响应类型</typeparam>
    public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
        
        // 性能阈值配置（毫秒）
        private static readonly int WarningThresholdMs = 1000;  // 1秒警告阈值
        private static readonly int ErrorThresholdMs = 5000;    // 5秒错误阈值

        public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            var requestType = GetRequestType(requestName);
            
            var stopwatch = Stopwatch.StartNew();
            var response = await next();
            stopwatch.Stop();

            var elapsedMs = stopwatch.ElapsedMilliseconds;

            // 根据执行时间记录不同级别的日志
            LogPerformanceResult(requestName, requestType, elapsedMs);

            // 记录性能指标（可以集成到APM系统）
            RecordPerformanceMetrics(requestName, requestType, elapsedMs);

            return response;
        }

        /// <summary>
        /// 根据性能结果记录日志
        /// </summary>
        private void LogPerformanceResult(string requestName, string requestType, long elapsedMs)
        {
            if (elapsedMs >= ErrorThresholdMs)
            {
                _logger.LogError("🚨 严重性能问题 - {RequestType} {RequestName} 执行时间: {ElapsedMs}ms (超过错误阈值 {ErrorThreshold}ms)", 
                    requestType, requestName, elapsedMs, ErrorThresholdMs);
            }
            else if (elapsedMs >= WarningThresholdMs)
            {
                _logger.LogWarning("⚠️ 性能警告 - {RequestType} {RequestName} 执行时间: {ElapsedMs}ms (超过警告阈值 {WarningThreshold}ms)", 
                    requestType, requestName, elapsedMs, WarningThresholdMs);
            }
            else
            {
                _logger.LogDebug("✅ 性能正常 - {RequestType} {RequestName} 执行时间: {ElapsedMs}ms", 
                    requestType, requestName, elapsedMs);
            }
        }

        /// <summary>
        /// 记录性能指标
        /// </summary>
        private void RecordPerformanceMetrics(string requestName, string requestType, long elapsedMs)
        {
            try
            {
                // 这里可以集成各种APM监控系统
                // 例如：Application Insights, Prometheus, Grafana等
                
                // 示例：使用System.Diagnostics.Activity记录指标
                using var activity = new Activity("CQRS.Performance");
                activity?.SetTag("request.name", requestName);
                activity?.SetTag("request.type", requestType);
                activity?.SetTag("duration.ms", elapsedMs.ToString());
                activity?.SetTag("performance.level", GetPerformanceLevel(elapsedMs));
                activity?.Start();

                // 可以在这里发送指标到外部系统
                // await _metricsService.RecordAsync(requestName, elapsedMs);
                
                _logger.LogTrace("性能指标已记录: {RequestName}, 耗时: {ElapsedMs}ms", requestName, elapsedMs);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "记录性能指标时发生错误: {RequestName}", requestName);
            }
        }

        /// <summary>
        /// 获取请求类型（Command/Query）
        /// </summary>
        private string GetRequestType(string requestName)
        {
            if (requestName.EndsWith("Command"))
                return "Command";
            else if (requestName.EndsWith("Query"))
                return "Query";
            else
                return "Request";
        }

        /// <summary>
        /// 获取性能等级
        /// </summary>
        private string GetPerformanceLevel(long elapsedMs)
        {
            if (elapsedMs >= ErrorThresholdMs)
                return "Critical";
            else if (elapsedMs >= WarningThresholdMs)
                return "Warning";
            else if (elapsedMs >= 500)
                return "Normal";
            else
                return "Fast";
        }
    }

    /// <summary>
    /// 性能监控配置
    /// </summary>
    public static class PerformanceThresholds
    {
        /// <summary>
        /// 不同操作类型的性能阈值配置
        /// </summary>
        public static class Commands
        {
            public static int Create = 1000;    // 创建操作：1秒
            public static int Update = 800;     // 更新操作：0.8秒
            public static int Delete = 500;     // 删除操作：0.5秒
            public static int Batch = 3000;     // 批量操作：3秒
        }

        /// <summary>
        /// 查询操作性能阈值
        /// </summary>
        public static class Queries
        {
            public static int GetById = 200;    // ID查询：0.2秒
            public static int GetList = 1000;   // 列表查询：1秒
            public static int Search = 1500;    // 搜索查询：1.5秒
            public static int Statistics = 2000; // 统计查询：2秒
        }

        /// <summary>
        /// 根据请求名称获取适当的阈值
        /// </summary>
        public static int GetThreshold(string requestName)
        {
            // 命令阈值
            if (requestName.Contains("Create")) return Commands.Create;
            if (requestName.Contains("Update")) return Commands.Update;
            if (requestName.Contains("Delete") && requestName.Contains("Batch")) return Commands.Batch;
            if (requestName.Contains("Delete")) return Commands.Delete;

            // 查询阈值
            if (requestName.Contains("GetById") || requestName.Contains("ById")) return Queries.GetById;
            if (requestName.Contains("Search")) return Queries.Search;
            if (requestName.Contains("Statistics")) return Queries.Statistics;
            if (requestName.Contains("GetPaged") || requestName.Contains("List")) return Queries.GetList;

            // 默认阈值
            return 1000;
        }
    }
}
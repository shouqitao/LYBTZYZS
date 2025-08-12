using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using LYBT.Infrastructure.Performance;

namespace LYBT.Infrastructure.CQRS.Behaviors
{
    /// <summary>
    /// 增强型性能监控行为 - UltraThink重构性能优化架构
    /// 自动收集CQRS操作的详细性能数据
    /// </summary>
    public class EnhancedPerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly ILogger<EnhancedPerformanceBehavior<TRequest, TResponse>> _logger;
        private readonly CQRSPerformanceMonitor _performanceMonitor;
        private readonly IPerformanceCollector _performanceCollector;

        // 性能阈值配置
        private readonly Dictionary<string, double> _performanceThresholds = new()
        {
            ["Query"] = 500,    // 查询操作500ms警告阈值
            ["Command"] = 1000, // 命令操作1000ms警告阈值
            ["Critical"] = 2000 // 严重性能问题2000ms
        };

        public EnhancedPerformanceBehavior(
            ILogger<EnhancedPerformanceBehavior<TRequest, TResponse>> logger,
            CQRSPerformanceMonitor performanceMonitor,
            IPerformanceCollector performanceCollector)
        {
            _logger = logger;
            _performanceMonitor = performanceMonitor;
            _performanceCollector = performanceCollector;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            var operationType = GetOperationType(requestName);
            var stopwatch = Stopwatch.StartNew();
            var startTime = DateTime.UtcNow;
            
            Exception exception = null;
            TResponse response = default;
            
            var tags = new Dictionary<string, object>
            {
                ["request_type"] = requestName,
                ["operation_type"] = operationType,
                ["thread_id"] = Thread.CurrentThread.ManagedThreadId,
                ["start_time"] = startTime
            };

            // 记录操作开始
            _performanceCollector.Counter($"cqrs.{operationType.ToLower()}.started", 1, tags);

            try
            {
                // 监控内存使用情况
                var beforeMemory = GC.GetTotalMemory(false);
                var beforeGen0 = GC.CollectionCount(0);
                var beforeGen1 = GC.CollectionCount(1);
                var beforeGen2 = GC.CollectionCount(2);

                using var performanceContext = _performanceCollector.StartTimer($"cqrs.{operationType.ToLower()}.execution", tags);
                
                response = await next();
                
                stopwatch.Stop();

                // 记录内存和GC变化
                var afterMemory = GC.GetTotalMemory(false);
                var afterGen0 = GC.CollectionCount(0);
                var afterGen1 = GC.CollectionCount(1);
                var afterGen2 = GC.CollectionCount(2);

                var memoryAllocated = afterMemory - beforeMemory;
                var gcGen0Collections = afterGen0 - beforeGen0;
                var gcGen1Collections = afterGen1 - beforeGen1;
                var gcGen2Collections = afterGen2 - beforeGen2;

                // 扩展标签信息
                tags.Add("memory_allocated", memoryAllocated);
                tags.Add("gc_gen0_collections", gcGen0Collections);
                tags.Add("gc_gen1_collections", gcGen1Collections);
                tags.Add("gc_gen2_collections", gcGen2Collections);
                tags.Add("execution_time_ms", stopwatch.ElapsedMilliseconds);

                // 记录成功的性能数据
                _performanceMonitor.RecordOperation(
                    operationType,
                    requestName,
                    stopwatch.ElapsedMilliseconds,
                    true,
                    null,
                    tags);

                // 记录详细的性能指标
                RecordDetailedMetrics(operationType, requestName, stopwatch.ElapsedMilliseconds, memoryAllocated, tags);

                // 检查性能警告
                CheckPerformanceWarnings(operationType, requestName, stopwatch.ElapsedMilliseconds, tags);

                return response;
            }
            catch (Exception ex)
            {
                exception = ex;
                stopwatch.Stop();

                // 记录错误的性能数据
                _performanceMonitor.RecordOperation(
                    operationType,
                    requestName,
                    stopwatch.ElapsedMilliseconds,
                    false,
                    ex.GetType().Name,
                    tags);

                // 记录错误指标
                _performanceCollector.Counter($"cqrs.{operationType.ToLower()}.error", 1, new Dictionary<string, object>(tags)
                {
                    ["error_type"] = ex.GetType().Name,
                    ["error_message"] = ex.Message
                });

                _logger.LogError(ex, 
                    "CQRS {OperationType} {RequestName} failed after {ElapsedMilliseconds}ms",
                    operationType, requestName, stopwatch.ElapsedMilliseconds);

                throw;
            }
            finally
            {
                // 记录完成指标
                _performanceCollector.Counter($"cqrs.{operationType.ToLower()}.completed", 1, new Dictionary<string, object>(tags)
                {
                    ["is_success"] = exception == null,
                    ["total_time_ms"] = stopwatch.ElapsedMilliseconds
                });
            }
        }

        /// <summary>
        /// 确定操作类型
        /// </summary>
        private string GetOperationType(string requestName)
        {
            if (requestName.EndsWith("Query", StringComparison.OrdinalIgnoreCase))
                return "Query";
            
            if (requestName.EndsWith("Command", StringComparison.OrdinalIgnoreCase))
                return "Command";
            
            // 基于接口类型判断
            if (typeof(TRequest).GetInterfaces().Any(i => i.Name.Contains("Query")))
                return "Query";
            
            if (typeof(TRequest).GetInterfaces().Any(i => i.Name.Contains("Command")))
                return "Command";
            
            return "Unknown";
        }

        /// <summary>
        /// 记录详细的性能指标
        /// </summary>
        private void RecordDetailedMetrics(
            string operationType, 
            string requestName, 
            long elapsedMilliseconds, 
            long memoryAllocated,
            Dictionary<string, object> tags)
        {
            var operationKey = operationType.ToLower();
            
            // 记录执行时间分布
            _performanceCollector.Histogram($"cqrs.{operationKey}.duration_distribution", elapsedMilliseconds, tags);
            
            // 记录内存分配
            _performanceCollector.Histogram($"cqrs.{operationKey}.memory_allocation", memoryAllocated, tags);
            
            // 记录吞吐量指标
            _performanceCollector.Gauge($"cqrs.{operationKey}.throughput", 1, tags);
            
            // 记录并发度
            _performanceCollector.Gauge($"cqrs.{operationKey}.concurrency", Thread.CurrentThread.ManagedThreadId, tags);
        }

        /// <summary>
        /// 检查性能警告
        /// </summary>
        private void CheckPerformanceWarnings(
            string operationType, 
            string requestName, 
            long elapsedMilliseconds,
            Dictionary<string, object> tags)
        {
            var threshold = _performanceThresholds.GetValueOrDefault(operationType, 1000);
            var criticalThreshold = _performanceThresholds["Critical"];

            if (elapsedMilliseconds > criticalThreshold)
            {
                _logger.LogCritical(
                    "CRITICAL PERFORMANCE: {OperationType} {RequestName} took {ElapsedMilliseconds}ms (threshold: {CriticalThreshold}ms)",
                    operationType, requestName, elapsedMilliseconds, criticalThreshold);

                _performanceCollector.Counter("cqrs.performance.critical", 1, new Dictionary<string, object>(tags)
                {
                    ["threshold_exceeded"] = criticalThreshold,
                    ["actual_duration"] = elapsedMilliseconds
                });
            }
            else if (elapsedMilliseconds > threshold)
            {
                _logger.LogWarning(
                    "SLOW OPERATION: {OperationType} {RequestName} took {ElapsedMilliseconds}ms (threshold: {Threshold}ms)",
                    operationType, requestName, elapsedMilliseconds, threshold);

                _performanceCollector.Counter("cqrs.performance.warning", 1, new Dictionary<string, object>(tags)
                {
                    ["threshold_exceeded"] = threshold,
                    ["actual_duration"] = elapsedMilliseconds
                });
            }

            // 检查内存分配警告
            if (tags.TryGetValue("memory_allocated", out var memoryObj) && memoryObj is long memory)
            {
                var memoryThreshold = 10 * 1024 * 1024; // 10MB
                if (memory > memoryThreshold)
                {
                    _logger.LogWarning(
                        "HIGH MEMORY ALLOCATION: {OperationType} {RequestName} allocated {MemoryMB:F2}MB",
                        operationType, requestName, memory / (1024.0 * 1024.0));

                    _performanceCollector.Counter("cqrs.memory.warning", 1, new Dictionary<string, object>(tags)
                    {
                        ["memory_threshold_mb"] = memoryThreshold / (1024.0 * 1024.0),
                        ["actual_memory_mb"] = memory / (1024.0 * 1024.0)
                    });
                }
            }

            // 检查GC压力
            var totalGcCollections = 
                (tags.GetValueOrDefault("gc_gen0_collections", 0L) as long? ?? 0) +
                (tags.GetValueOrDefault("gc_gen1_collections", 0L) as long? ?? 0) +
                (tags.GetValueOrDefault("gc_gen2_collections", 0L) as long? ?? 0);

            if (totalGcCollections > 0)
            {
                _logger.LogInformation(
                    "GC ACTIVITY: {OperationType} {RequestName} triggered {GcCollections} GC collections",
                    operationType, requestName, totalGcCollections);

                _performanceCollector.Counter("cqrs.gc.triggered", (int)totalGcCollections, tags);
            }
        }
    }

    /// <summary>
    /// 性能阈值配置
    /// </summary>
    public class PerformanceThresholdOptions
    {
        public Dictionary<string, double> QueryThresholds { get; set; } = new()
        {
            ["Warning"] = 500,
            ["Critical"] = 2000
        };

        public Dictionary<string, double> CommandThresholds { get; set; } = new()
        {
            ["Warning"] = 1000,
            ["Critical"] = 5000
        };

        public long MemoryAllocationThreshold { get; set; } = 10 * 1024 * 1024; // 10MB
    }
}
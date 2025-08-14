using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LYBT.Infrastructure.Performance
{
    /// <summary>
    /// 性能优化建议
    /// </summary>
    public class PerformanceOptimizationSuggestion
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Category { get; set; } // Database, Memory, CPU, Network, CQRS
        public string Priority { get; set; } // Critical, High, Medium, Low
        public string Title { get; set; }
        public string Description { get; set; }
        public string Recommendation { get; set; }
        public List<string> AffectedOperations { get; set; } = new();
        public Dictionary<string, object> Metrics { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsImplemented { get; set; }
        public string ImplementationNotes { get; set; }
    }

    /// <summary>
    /// 性能优化报告
    /// </summary>
    public class PerformanceOptimizationReport
    {
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public string OverallHealthStatus { get; set; } // Excellent, Good, Fair, Poor, Critical
        public double OverallPerformanceScore { get; set; } // 0-100
        public List<PerformanceOptimizationSuggestion> Suggestions { get; set; } = new();
        public Dictionary<string, object> SystemHealthMetrics { get; set; } = new();
        public List<string> QuickWins { get; set; } = new(); // 容易实施的优化建议
        public List<string> LongTermImprovements { get; set; } = new(); // 长期改进建议
    }

    /// <summary>
    /// 性能优化引擎配置
    /// </summary>
    public class PerformanceOptimizationOptions
    {
        public double SlowQueryThresholdMs { get; set; } = 500;
        public double SlowCommandThresholdMs { get; set; } = 1000;
        public double CriticalThresholdMs { get; set; } = 2000;
        public double MinSuccessRate { get; set; } = 0.95;
        public long HighMemoryAllocationBytes { get; set; } = 10 * 1024 * 1024; // 10MB
        public int MinExecutionCountForAnalysis { get; set; } = 10;
        public double CpuThresholdPercent { get; set; } = 80;
        public double MemoryThresholdPercent { get; set; } = 85;
    }

    /// <summary>
    /// 性能优化引擎 - UltraThink重构自动化性能优化
    /// 分析系统性能数据并提供智能优化建议
    /// </summary>
    public class PerformanceOptimizationEngine
    {
        private readonly CQRSPerformanceMonitor _cqrsMonitor;
        private readonly IPerformanceCollector _performanceCollector;
        private readonly ILogger<PerformanceOptimizationEngine> _logger;
        private readonly PerformanceOptimizationOptions _options;

        public PerformanceOptimizationEngine(
            CQRSPerformanceMonitor cqrsMonitor,
            IPerformanceCollector performanceCollector,
            ILogger<PerformanceOptimizationEngine> logger,
            IOptions<PerformanceOptimizationOptions> options)
        {
            _cqrsMonitor = cqrsMonitor;
            _performanceCollector = performanceCollector;
            _logger = logger;
            _options = options.Value;
        }

        /// <summary>
        /// 生成性能优化报告
        /// </summary>
        public async Task<PerformanceOptimizationReport> GenerateOptimizationReportAsync()
        {
            _logger.LogInformation("Starting performance optimization analysis");

            var report = new PerformanceOptimizationReport();
            var suggestions = new List<PerformanceOptimizationSuggestion>();

            try
            {
                // 分析CQRS操作性能
                await AnalyzeCQRSPerformance(suggestions);

                // 分析系统资源使用
                await AnalyzeSystemResources(suggestions, report);

                // 分析内存和GC性能
                await AnalyzeMemoryPerformance(suggestions);

                // 分析数据库性能（基于查询执行时间）
                await AnalyzeDatabasePerformance(suggestions);

                // 对建议进行优先级排序
                suggestions = suggestions.OrderByDescending(s => GetPriority(s.Priority))
                                       .ThenByDescending(s => s.AffectedOperations.Count)
                                       .ToList();

                report.Suggestions = suggestions;
                report.OverallPerformanceScore = CalculateOverallScore(suggestions);
                report.OverallHealthStatus = GetHealthStatus(report.OverallPerformanceScore);

                // 分类建议
                CategorizeRecommendations(report, suggestions);

                _logger.LogInformation("Performance optimization analysis completed. Generated {SuggestionsCount} suggestions", 
                    suggestions.Count);

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating performance optimization report");
                throw;
            }
        }

        /// <summary>
        /// 分析CQRS操作性能
        /// </summary>
        private async Task AnalyzeCQRSPerformance(List<PerformanceOptimizationSuggestion> suggestions)
        {
            var operationStats = _cqrsMonitor.GetOperationStats();

            foreach (var (key, stats) in operationStats)
            {
                if (stats.ExecutionCount < _options.MinExecutionCountForAnalysis)
                    continue;

                // 分析慢操作
                if (stats.AverageExecutionTimeMs > _options.CriticalThresholdMs)
                {
                    suggestions.Add(new PerformanceOptimizationSuggestion
                    {
                        Category = "CQRS",
                        Priority = "Critical",
                        Title = $"严重性能问题: {stats.OperationType} {stats.OperationName}",
                        Description = $"操作平均执行时间 {stats.AverageExecutionTimeMs:F2}ms，远超临界阈值 {_options.CriticalThresholdMs}ms",
                        Recommendation = GetCQRSOptimizationRecommendation(stats),
                        AffectedOperations = new List<string> { key },
                        Metrics = new Dictionary<string, object>
                        {
                            ["average_time"] = stats.AverageExecutionTimeMs,
                            ["max_time"] = stats.MaxExecutionTimeMs,
                            ["p99_time"] = stats.P99ExecutionTimeMs,
                            ["execution_count"] = stats.ExecutionCount,
                            ["success_rate"] = stats.SuccessRate
                        }
                    });
                }
                else if (stats.AverageExecutionTimeMs > GetThresholdForOperation(stats.OperationType))
                {
                    suggestions.Add(new PerformanceOptimizationSuggestion
                    {
                        Category = "CQRS",
                        Priority = "High",
                        Title = $"性能警告: {stats.OperationType} {stats.OperationName}",
                        Description = $"操作平均执行时间 {stats.AverageExecutionTimeMs:F2}ms，超过建议阈值",
                        Recommendation = GetCQRSOptimizationRecommendation(stats),
                        AffectedOperations = new List<string> { key },
                        Metrics = new Dictionary<string, object>
                        {
                            ["average_time"] = stats.AverageExecutionTimeMs,
                            ["threshold"] = GetThresholdForOperation(stats.OperationType),
                            ["execution_count"] = stats.ExecutionCount
                        }
                    });
                }

                // 分析错误率
                if (stats.SuccessRate < _options.MinSuccessRate)
                {
                    suggestions.Add(new PerformanceOptimizationSuggestion
                    {
                        Category = "Reliability",
                        Priority = "High",
                        Title = $"高错误率: {stats.OperationType} {stats.OperationName}",
                        Description = $"成功率仅 {stats.SuccessRate:P2}，低于最低要求 {_options.MinSuccessRate:P2}",
                        Recommendation = "检查业务逻辑错误、数据验证和异常处理机制",
                        AffectedOperations = new List<string> { key },
                        Metrics = new Dictionary<string, object>
                        {
                            ["success_rate"] = stats.SuccessRate,
                            ["error_count"] = stats.ErrorCount,
                            ["success_count"] = stats.SuccessCount,
                            ["execution_count"] = stats.ExecutionCount
                        }
                    });
                }
            }
        }

        /// <summary>
        /// 分析系统资源使用
        /// </summary>
        private async Task AnalyzeSystemResources(
            List<PerformanceOptimizationSuggestion> suggestions, 
            PerformanceOptimizationReport report)
        {
            using var monitor = new SystemPerformanceMonitor();
            var systemInfo = monitor.GetCurrentInfo();

            report.SystemHealthMetrics = new Dictionary<string, object>
            {
                ["cpu_usage_percent"] = systemInfo.CpuUsagePercent,
                ["memory_usage_percent"] = systemInfo.MemoryUsagePercent,
                ["thread_count"] = systemInfo.ThreadCount,
                ["gc_gen0_collections"] = systemInfo.GcGen0Collections,
                ["gc_gen1_collections"] = systemInfo.GcGen1Collections,
                ["gc_gen2_collections"] = systemInfo.GcGen2Collections,
                ["gc_total_memory"] = systemInfo.GcTotalMemory
            };

            // 分析CPU使用率
            if (systemInfo.CpuUsagePercent > _options.CpuThresholdPercent)
            {
                suggestions.Add(new PerformanceOptimizationSuggestion
                {
                    Category = "CPU",
                    Priority = "High",
                    Title = "CPU使用率过高",
                    Description = $"当前CPU使用率 {systemInfo.CpuUsagePercent:F1}%，超过阈值 {_options.CpuThresholdPercent}%",
                    Recommendation = "考虑优化算法复杂度、增加异步处理、使用缓存减少计算量，或者升级硬件",
                    Metrics = new Dictionary<string, object>
                    {
                        ["current_cpu_usage"] = systemInfo.CpuUsagePercent,
                        ["cpu_threshold"] = _options.CpuThresholdPercent
                    }
                });
            }

            // 分析内存使用率
            if (systemInfo.MemoryUsagePercent > _options.MemoryThresholdPercent)
            {
                suggestions.Add(new PerformanceOptimizationSuggestion
                {
                    Category = "Memory",
                    Priority = "High",
                    Title = "内存使用率过高",
                    Description = $"当前内存使用率 {systemInfo.MemoryUsagePercent:F1}%，超过阈值 {_options.MemoryThresholdPercent}%",
                    Recommendation = "检查内存泄漏、优化对象生命周期管理、考虑增加内存或优化数据结构",
                    Metrics = new Dictionary<string, object>
                    {
                        ["current_memory_usage"] = systemInfo.MemoryUsagePercent,
                        ["memory_threshold"] = _options.MemoryThresholdPercent,
                        ["total_memory_mb"] = systemInfo.MemoryTotalBytes / (1024.0 * 1024.0)
                    }
                });
            }

            // 分析GC压力
            var totalCollections = systemInfo.GcGen0Collections + systemInfo.GcGen1Collections + systemInfo.GcGen2Collections;
            if (totalCollections > 1000) // 示例阈值
            {
                suggestions.Add(new PerformanceOptimizationSuggestion
                {
                    Category = "Memory",
                    Priority = "Medium",
                    Title = "GC回收频繁",
                    Description = $"GC总回收次数 {totalCollections}，可能存在内存分配压力",
                    Recommendation = "优化对象分配模式，使用对象池，减少不必要的装箱操作",
                    Metrics = new Dictionary<string, object>
                    {
                        ["total_gc_collections"] = totalCollections,
                        ["gc_gen0_collections"] = systemInfo.GcGen0Collections,
                        ["gc_gen1_collections"] = systemInfo.GcGen1Collections,
                        ["gc_gen2_collections"] = systemInfo.GcGen2Collections
                    }
                });
            }
        }

        /// <summary>
        /// 分析内存性能
        /// </summary>
        private async Task AnalyzeMemoryPerformance(List<PerformanceOptimizationSuggestion> suggestions)
        {
            // 这里可以根据收集到的内存分配指标进行分析
            // 由于当前使用的是简单的内存收集器，这里暂时跳过详细的内存分析
            await Task.CompletedTask;
        }

        /// <summary>
        /// 分析数据库性能
        /// </summary>
        private async Task AnalyzeDatabasePerformance(List<PerformanceOptimizationSuggestion> suggestions)
        {
            var operationStats = _cqrsMonitor.GetOperationStats();
            var dbRelatedOperations = operationStats.Values
                .Where(s => s.OperationType == "Query" && s.AverageExecutionTimeMs > _options.SlowQueryThresholdMs)
                .ToList();

            if (dbRelatedOperations.Any())
            {
                var avgQueryTime = dbRelatedOperations.Average(s => s.AverageExecutionTimeMs);
                
                suggestions.Add(new PerformanceOptimizationSuggestion
                {
                    Category = "Database",
                    Priority = "High",
                    Title = "数据库查询性能问题",
                    Description = $"发现 {dbRelatedOperations.Count} 个慢查询，平均执行时间 {avgQueryTime:F2}ms",
                    Recommendation = "检查数据库索引、优化查询语句、考虑添加缓存、分析查询计划",
                    AffectedOperations = dbRelatedOperations.Select(s => $"{s.OperationType}.{s.OperationName}").ToList(),
                    Metrics = new Dictionary<string, object>
                    {
                        ["slow_queries_count"] = dbRelatedOperations.Count,
                        ["average_query_time"] = avgQueryTime,
                        ["threshold"] = _options.SlowQueryThresholdMs
                    }
                });
            }
        }

        /// <summary>
        /// 获取CQRS优化建议
        /// </summary>
        private string GetCQRSOptimizationRecommendation(CQRSOperationStats stats)
        {
            var recommendations = new List<string>();

            if (stats.OperationType == "Query")
            {
                recommendations.Add("考虑添加缓存");
                recommendations.Add("检查数据库索引");
                recommendations.Add("优化查询语句");
                recommendations.Add("考虑读写分离");
            }
            else if (stats.OperationType == "Command")
            {
                recommendations.Add("检查业务逻辑复杂度");
                recommendations.Add("考虑异步处理");
                recommendations.Add("优化数据库事务");
                recommendations.Add("减少外部依赖调用");
            }

            if (stats.P99ExecutionTimeMs > stats.AverageExecutionTimeMs * 3)
            {
                recommendations.Add("存在性能尖峰，检查异常处理和资源竞争");
            }

            return string.Join("；", recommendations);
        }

        /// <summary>
        /// 获取操作类型的阈值
        /// </summary>
        private double GetThresholdForOperation(string operationType)
        {
            return operationType switch
            {
                "Query" => _options.SlowQueryThresholdMs,
                "Command" => _options.SlowCommandThresholdMs,
                _ => 1000
            };
        }

        /// <summary>
        /// 获取优先级数值
        /// </summary>
        private int GetPriority(string priority)
        {
            return priority switch
            {
                "Critical" => 4,
                "High" => 3,
                "Medium" => 2,
                "Low" => 1,
                _ => 0
            };
        }

        /// <summary>
        /// 计算总体性能分数
        /// </summary>
        private double CalculateOverallScore(List<PerformanceOptimizationSuggestion> suggestions)
        {
            if (!suggestions.Any()) return 100;

            var criticalCount = suggestions.Count(s => s.Priority == "Critical");
            var highCount = suggestions.Count(s => s.Priority == "High");
            var mediumCount = suggestions.Count(s => s.Priority == "Medium");
            var lowCount = suggestions.Count(s => s.Priority == "Low");

            // 权重计算
            var totalScore = 100 - (criticalCount * 20 + highCount * 10 + mediumCount * 5 + lowCount * 2);
            return Math.Max(0, Math.Min(100, totalScore));
        }

        /// <summary>
        /// 获取健康状态
        /// </summary>
        private string GetHealthStatus(double score)
        {
            return score switch
            {
                >= 90 => "Excellent",
                >= 80 => "Good",
                >= 60 => "Fair",
                >= 40 => "Poor",
                _ => "Critical"
            };
        }

        /// <summary>
        /// 分类建议
        /// </summary>
        private void CategorizeRecommendations(
            PerformanceOptimizationReport report, 
            List<PerformanceOptimizationSuggestion> suggestions)
        {
            // 快速胜利（容易实施的优化）
            report.QuickWins = suggestions
                .Where(s => s.Priority == "Medium" || s.Priority == "Low")
                .Where(s => s.Category == "CQRS" || s.Category == "Memory")
                .Select(s => s.Title)
                .ToList();

            // 长期改进
            report.LongTermImprovements = suggestions
                .Where(s => s.Priority == "Critical" || s.Priority == "High")
                .Where(s => s.Category == "Database" || s.Category == "CPU")
                .Select(s => s.Title)
                .ToList();
        }
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Performance.Database.Components;
using LYBT.Infrastructure.Performance.Database.Models;

namespace LYBT.Infrastructure.Performance.Database
{
    /// <summary>
    /// 重构后的统一数据库优化器 - UltraThink架构实现
    /// 职责单一：作为6个专门组件的协调器和统一接口
    /// 代码干净：简洁的组件组合和清晰的职责分离
    /// 性能出色：优化的组件协作和资源管理
    /// 
    /// 从原来的888行超大文件，重构为简洁的协调器模式：
    /// - QueryPerformanceAnalyzer: 查询性能分析器
    /// - BatchOperationExecutor: 批量操作执行器
    /// - DatabaseStatisticsCollector: 数据库统计信息收集器
    /// - QueryOptimizer: 查询优化器
    /// - DatabaseMaintenanceManager: 数据库维护管理器
    /// - SlowQueryAnalyzer: 慢查询分析器
    /// </summary>
    public class UnifiedDatabaseOptimizerRefactored : IUnifiedDatabaseOptimizer, IDisposable
    {
        #region UltraThink专门化组件

        private readonly QueryPerformanceAnalyzer _queryPerformanceAnalyzer;
        private readonly BatchOperationExecutor _batchOperationExecutor;
        private readonly DatabaseStatisticsCollector _databaseStatisticsCollector;
        private readonly QueryOptimizer _queryOptimizer;
        private readonly DatabaseMaintenanceManager _databaseMaintenanceManager;
        private readonly SlowQueryAnalyzer _slowQueryAnalyzer;
        private readonly ILogger<UnifiedDatabaseOptimizerRefactored> _logger;

        #endregion

        #region 构造函数

        public UnifiedDatabaseOptimizerRefactored(
            AppDbContext context,
            ILogger<UnifiedDatabaseOptimizerRefactored> logger,
            ILogger<QueryPerformanceAnalyzer> queryAnalyzerLogger,
            ILogger<BatchOperationExecutor> batchExecutorLogger,
            ILogger<DatabaseStatisticsCollector> statisticsCollectorLogger,
            ILogger<QueryOptimizer> queryOptimizerLogger,
            ILogger<DatabaseMaintenanceManager> maintenanceManagerLogger,
            ILogger<SlowQueryAnalyzer> slowQueryAnalyzerLogger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            try
            {
                _logger.LogDebug("开始初始化重构后的UnifiedDatabaseOptimizer");

                // 创建专门化组件
                _queryPerformanceAnalyzer = new QueryPerformanceAnalyzer(context, queryAnalyzerLogger);
                _batchOperationExecutor = new BatchOperationExecutor(context, batchExecutorLogger);
                _databaseStatisticsCollector = new DatabaseStatisticsCollector(context, statisticsCollectorLogger);
                _queryOptimizer = new QueryOptimizer(context, queryOptimizerLogger);
                _databaseMaintenanceManager = new DatabaseMaintenanceManager(context, maintenanceManagerLogger);
                _slowQueryAnalyzer = new SlowQueryAnalyzer(context, slowQueryAnalyzerLogger);

                _logger.LogInformation("UnifiedDatabaseOptimizer重构完成，组件化架构已建立");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化UnifiedDatabaseOptimizer失败");
                throw;
            }
        }

        #endregion

        #region 查询性能分析接口（委托给QueryPerformanceAnalyzer）

        /// <summary>
        /// 分析查询性能
        /// </summary>
        public async Task<QueryPerformanceAnalysis> AnalyzeQueryPerformanceAsync<T>(
            IQueryable<T> query,
            CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                _logger.LogDebug("委托查询性能分析：{QueryType}", typeof(T).Name);
                return await _queryPerformanceAnalyzer.AnalyzeQueryPerformanceAsync(query, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询性能分析失败：{QueryType}", typeof(T).Name);
                throw;
            }
        }

        /// <summary>
        /// 批量分析多个查询的性能
        /// </summary>
        public async Task<List<QueryPerformanceAnalysis>> AnalyzeMultipleQueriesPerformanceAsync<T>(
            IEnumerable<IQueryable<T>> queries,
            CancellationToken cancellationToken = default) where T : class
        {
            return await _queryPerformanceAnalyzer.AnalyzeMultipleQueriesAsync(queries, cancellationToken);
        }

        #endregion

        #region 批量操作接口（委托给BatchOperationExecutor）

        /// <summary>
        /// 执行批量操作
        /// </summary>
        public async Task<BatchOperationResult> ExecuteBatchOperationAsync<T>(
            IEnumerable<T> entities,
            BatchOperationType operationType,
            CancellationToken cancellationToken = default) where T : class
        {
            try
            {
                _logger.LogDebug("委托批量操作：{OperationType}，实体类型：{EntityType}", 
                    operationType, typeof(T).Name);
                return await _batchOperationExecutor.ExecuteBatchOperationAsync(entities, operationType, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量操作失败：{OperationType}", operationType);
                throw;
            }
        }

        /// <summary>
        /// 执行批量操作（带配置选项）
        /// </summary>
        public async Task<BatchOperationResult> ExecuteBatchOperationAsync<T>(
            IEnumerable<T> entities,
            BatchOperationType operationType,
            BatchOperationOptions options,
            CancellationToken cancellationToken = default) where T : class
        {
            return await _batchOperationExecutor.ExecuteBatchOperationAsync(entities, operationType, options, cancellationToken);
        }

        #endregion

        #region 数据库统计接口（委托给DatabaseStatisticsCollector）

        /// <summary>
        /// 获取数据库统计信息
        /// </summary>
        public async Task<DatabaseStatistics> GetDatabaseStatisticsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("委托数据库统计信息收集");
                return await _databaseStatisticsCollector.GetDatabaseStatisticsAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取数据库统计信息失败");
                throw;
            }
        }

        /// <summary>
        /// 获取详细的数据库统计信息
        /// </summary>
        public async Task<DetailedDatabaseStatistics> GetDetailedDatabaseStatisticsAsync(CancellationToken cancellationToken = default)
        {
            return await _databaseStatisticsCollector.GetDetailedDatabaseStatisticsAsync(cancellationToken);
        }

        /// <summary>
        /// 获取实时性能指标
        /// </summary>
        public async Task<RealTimePerformanceMetrics> GetRealTimePerformanceMetricsAsync(CancellationToken cancellationToken = default)
        {
            return await _databaseStatisticsCollector.GetRealTimePerformanceMetricsAsync(cancellationToken);
        }

        #endregion

        #region 查询优化接口（委托给QueryOptimizer）

        /// <summary>
        /// 优化查询
        /// </summary>
        public async Task<IQueryable<T>> OptimizeQueryAsync<T>(
            IQueryable<T> query,
            QueryOptimizationOptions? options = null) where T : class
        {
            try
            {
                _logger.LogDebug("委托查询优化：{QueryType}", typeof(T).Name);
                return await _queryOptimizer.OptimizeQueryAsync(query, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询优化失败：{QueryType}", typeof(T).Name);
                return query; // 返回原始查询
            }
        }

        /// <summary>
        /// 预热数据库连接池
        /// </summary>
        public async Task WarmUpConnectionPoolAsync(int connectionCount = 5, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("委托连接池预热：{ConnectionCount}个连接", connectionCount);
                await _queryOptimizer.WarmUpConnectionPoolAsync(connectionCount, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "连接池预热失败");
                throw;
            }
        }

        /// <summary>
        /// 优化连接池配置
        /// </summary>
        public async Task<ConnectionPoolOptimizationResult> OptimizeConnectionPoolAsync(
            ConnectionPoolOptimizationOptions options,
            CancellationToken cancellationToken = default)
        {
            return await _queryOptimizer.OptimizeConnectionPoolAsync(options, cancellationToken);
        }

        /// <summary>
        /// 分析查询并提供优化建议
        /// </summary>
        public async Task<QueryOptimizationSuggestions> AnalyzeQueryForOptimizationAsync<T>(
            IQueryable<T> query,
            CancellationToken cancellationToken = default) where T : class
        {
            return await _queryOptimizer.AnalyzeQueryForOptimizationAsync(query, cancellationToken);
        }

        #endregion

        #region 数据库维护接口（委托给DatabaseMaintenanceManager）

        /// <summary>
        /// 执行数据库维护任务
        /// </summary>
        public async Task<MaintenanceResult> ExecuteMaintenanceAsync(
            MaintenanceOptions options,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("委托数据库维护任务");
                return await _databaseMaintenanceManager.ExecuteMaintenanceAsync(options, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据库维护失败");
                throw;
            }
        }

        /// <summary>
        /// 获取索引使用建议
        /// </summary>
        public async Task<List<IndexRecommendation>> GetIndexRecommendationsAsync(
            string tableName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("委托索引建议分析：{TableName}", tableName);
                return await _databaseMaintenanceManager.GetIndexRecommendationsAsync(tableName, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取索引建议失败：{TableName}", tableName);
                throw;
            }
        }

        /// <summary>
        /// 执行完整的数据库维护
        /// </summary>
        public async Task<MaintenanceResult> ExecuteFullMaintenanceAsync(CancellationToken cancellationToken = default)
        {
            return await _databaseMaintenanceManager.ExecuteFullMaintenanceAsync(cancellationToken);
        }

        /// <summary>
        /// 执行快速维护
        /// </summary>
        public async Task<MaintenanceResult> ExecuteQuickMaintenanceAsync(CancellationToken cancellationToken = default)
        {
            return await _databaseMaintenanceManager.ExecuteQuickMaintenanceAsync(cancellationToken);
        }

        /// <summary>
        /// 分析索引使用情况
        /// </summary>
        public async Task<IndexAnalysisReport> AnalyzeIndexUsageAsync(CancellationToken cancellationToken = default)
        {
            return await _databaseMaintenanceManager.AnalyzeIndexUsageAsync(cancellationToken);
        }

        #endregion

        #region 慢查询分析接口（委托给SlowQueryAnalyzer）

        /// <summary>
        /// 获取慢查询报告
        /// </summary>
        public async Task<SlowQueryReport> GetSlowQueryReportAsync(
            DateTime startTime,
            DateTime endTime,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("委托慢查询报告生成：{StartTime} - {EndTime}", startTime, endTime);
                return await _slowQueryAnalyzer.GetSlowQueryReportAsync(startTime, endTime, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成慢查询报告失败");
                throw;
            }
        }

        /// <summary>
        /// 获取实时慢查询
        /// </summary>
        public async Task<List<ActiveSlowQuery>> GetActiveSlowQueriesAsync(
            long minExecutionTimeMs = 5000,
            CancellationToken cancellationToken = default)
        {
            return await _slowQueryAnalyzer.GetActiveSlowQueriesAsync(minExecutionTimeMs, cancellationToken);
        }

        /// <summary>
        /// 分析查询性能趋势
        /// </summary>
        public async Task<QueryPerformanceTrend> AnalyzeQueryPerformanceTrendAsync(
            TimeSpan timeWindow,
            int intervalMinutes = 15,
            CancellationToken cancellationToken = default)
        {
            return await _slowQueryAnalyzer.AnalyzeQueryPerformanceTrendAsync(timeWindow, intervalMinutes, cancellationToken);
        }

        #endregion

        #region 组合功能（利用多个组件）

        /// <summary>
        /// 执行全面的数据库性能诊断
        /// </summary>
        public async Task<ComprehensivePerformanceDiagnosis> ExecuteComprehensiveDiagnosisAsync(
            CancellationToken cancellationToken = default)
        {
            var diagnosis = new ComprehensivePerformanceDiagnosis();

            try
            {
                _logger.LogInformation("开始全面数据库性能诊断");

                // 并行收集各种统计信息
                var tasks = new List<Task>
                {
                    Task.Run(async () => diagnosis.DatabaseStatistics = await GetDatabaseStatisticsAsync(cancellationToken), cancellationToken),
                    Task.Run(async () => diagnosis.SlowQueryReport = await GetSlowQueryReportAsync(DateTime.UtcNow.AddHours(-24), DateTime.UtcNow, cancellationToken), cancellationToken),
                    Task.Run(async () => diagnosis.IndexAnalysis = await AnalyzeIndexUsageAsync(cancellationToken), cancellationToken),
                    Task.Run(async () => diagnosis.PerformanceMetrics = await GetRealTimePerformanceMetricsAsync(cancellationToken), cancellationToken)
                };

                await Task.WhenAll(tasks);

                // 生成综合建议
                diagnosis.OverallRecommendations = GenerateOverallRecommendations(diagnosis);
                diagnosis.DiagnosisTime = DateTime.UtcNow;

                _logger.LogInformation("全面数据库性能诊断完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "全面性能诊断失败");
                diagnosis.Errors.Add(ex.Message);
            }

            return diagnosis;
        }

        /// <summary>
        /// 执行性能优化建议
        /// </summary>
        public async Task<PerformanceOptimizationResult> ExecutePerformanceOptimizationAsync(
            PerformanceOptimizationOptions options,
            CancellationToken cancellationToken = default)
        {
            var result = new PerformanceOptimizationResult();

            try
            {
                _logger.LogInformation("开始执行性能优化");

                if (options.OptimizeIndexes)
                {
                    var maintenanceResult = await ExecuteQuickMaintenanceAsync(cancellationToken);
                    result.MaintenanceResults.Add(maintenanceResult);
                }

                if (options.WarmUpConnections)
                {
                    await WarmUpConnectionPoolAsync(options.ConnectionCount, cancellationToken);
                    result.CompletedOptimizations.Add("连接池预热完成");
                }

                if (options.UpdateStatistics)
                {
                    var fullMaintenanceResult = await ExecuteFullMaintenanceAsync(cancellationToken);
                    result.MaintenanceResults.Add(fullMaintenanceResult);
                }

                result.IsSuccessful = result.MaintenanceResults.All(r => r.CompletedTasks.Any());
                result.OptimizationTime = DateTime.UtcNow;

                _logger.LogInformation("性能优化完成：成功={IsSuccessful}", result.IsSuccessful);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "性能优化失败");
                result.IsSuccessful = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 生成综合建议
        /// </summary>
        private List<string> GenerateOverallRecommendations(ComprehensivePerformanceDiagnosis diagnosis)
        {
            var recommendations = new List<string>();

            try
            {
                // 基于数据库统计的建议
                if (diagnosis.DatabaseStatistics?.DatabaseSizeMB > 10000) // 大于10GB
                {
                    recommendations.Add("数据库较大，建议定期进行维护和索引优化");
                }

                if (diagnosis.DatabaseStatistics?.ActiveConnections > 100)
                {
                    recommendations.Add("活动连接数较多，建议检查连接池配置");
                }

                // 基于慢查询的建议
                var slowQueryCount = diagnosis.SlowQueryReport?.TotalSlowQueries ?? 0;
                if (slowQueryCount > 10)
                {
                    recommendations.Add($"发现{slowQueryCount}个慢查询，建议重点优化");
                }

                // 基于索引分析的建议
                var unusedIndexCount = diagnosis.IndexAnalysis?.UnusedIndexes ?? 0;
                if (unusedIndexCount > 5)
                {
                    recommendations.Add($"发现{unusedIndexCount}个未使用的索引，考虑删除以提升性能");
                }

                if (!recommendations.Any())
                {
                    recommendations.Add("数据库性能状态良好，继续保持当前配置");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "生成综合建议失败");
                recommendations.Add("综合建议生成过程中发生错误");
            }

            return recommendations;
        }

        #endregion

        #region IDisposable实现

        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    try
                    {
                        // 清理编译查询缓存
                        _queryOptimizer?.ClearCompiledQueryCache();
                        
                        // 清理统计信息缓存
                        _databaseStatisticsCollector?.ClearCache();

                        _logger.LogDebug("UnifiedDatabaseOptimizer资源清理完成");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "清理UnifiedDatabaseOptimizer资源失败");
                    }
                }

                _disposed = true;
            }
        }

        #endregion
    }

    #region 扩展数据类

    /// <summary>
    /// 全面性能诊断结果
    /// </summary>
    public class ComprehensivePerformanceDiagnosis
    {
        public DatabaseStatistics? DatabaseStatistics { get; set; }
        public SlowQueryReport? SlowQueryReport { get; set; }
        public IndexAnalysisReport? IndexAnalysis { get; set; }
        public RealTimePerformanceMetrics? PerformanceMetrics { get; set; }
        public List<string> OverallRecommendations { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public DateTime DiagnosisTime { get; set; }
    }

    /// <summary>
    /// 性能优化选项
    /// </summary>
    public class PerformanceOptimizationOptions
    {
        public bool OptimizeIndexes { get; set; } = true;
        public bool WarmUpConnections { get; set; } = true;
        public bool UpdateStatistics { get; set; } = true;
        public int ConnectionCount { get; set; } = 10;
    }

    /// <summary>
    /// 性能优化结果
    /// </summary>
    public class PerformanceOptimizationResult
    {
        public bool IsSuccessful { get; set; }
        public List<string> CompletedOptimizations { get; set; } = new();
        public List<MaintenanceResult> MaintenanceResults { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public DateTime OptimizationTime { get; set; }
    }

    #endregion
}
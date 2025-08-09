using System.Data;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Infrastructure.Performance.Database
{
    /// <summary>
    /// 统一数据库优化接口 - UltraThink性能优化
    /// </summary>
    public interface IUnifiedDatabaseOptimizer
    {
        /// <summary>
        /// 分析查询性能
        /// </summary>
        Task<QueryPerformanceAnalysis> AnalyzeQueryPerformanceAsync<T>(
            IQueryable<T> query, 
            CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// 执行批量操作
        /// </summary>
        Task<BatchOperationResult> ExecuteBatchOperationAsync<T>(
            IEnumerable<T> entities, 
            BatchOperationType operationType, 
            CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// 获取数据库统计信息
        /// </summary>
        Task<DatabaseStatistics> GetDatabaseStatisticsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 优化查询 - 自动应用最佳实践
        /// </summary>
        Task<IQueryable<T>> OptimizeQueryAsync<T>(
            IQueryable<T> query, 
            QueryOptimizationOptions? options = null) where T : class;

        /// <summary>
        /// 预热数据库连接池
        /// </summary>
        Task WarmUpConnectionPoolAsync(int connectionCount = 5, CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取索引使用建议
        /// </summary>
        Task<List<IndexRecommendation>> GetIndexRecommendationsAsync(
            string tableName, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 执行数据库维护任务
        /// </summary>
        Task<MaintenanceResult> ExecuteMaintenanceAsync(
            MaintenanceOptions options, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取慢查询报告
        /// </summary>
        Task<SlowQueryReport> GetSlowQueryReportAsync(
            DateTime startTime, 
            DateTime endTime, 
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 查询性能分析结果
    /// </summary>
    public class QueryPerformanceAnalysis
    {
        /// <summary>
        /// 执行时间（毫秒）
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// 返回记录数
        /// </summary>
        public int RecordCount { get; set; }

        /// <summary>
        /// SQL查询语句
        /// </summary>
        public string SqlQuery { get; set; } = string.Empty;

        /// <summary>
        /// 执行计划
        /// </summary>
        public string? ExecutionPlan { get; set; }

        /// <summary>
        /// 性能等级
        /// </summary>
        public PerformanceLevel PerformanceLevel { get; set; }

        /// <summary>
        /// 优化建议
        /// </summary>
        public List<string> OptimizationSuggestions { get; set; } = new List<string>();

        /// <summary>
        /// 资源使用统计
        /// </summary>
        public ResourceUsage ResourceUsage { get; set; } = new ResourceUsage();
    }

    /// <summary>
    /// 资源使用统计
    /// </summary>
    public class ResourceUsage
    {
        /// <summary>
        /// CPU使用时间（毫秒）
        /// </summary>
        public long CpuTimeMs { get; set; }

        /// <summary>
        /// IO读取次数
        /// </summary>
        public long LogicalReads { get; set; }

        /// <summary>
        /// IO写入次数
        /// </summary>
        public long PhysicalReads { get; set; }

        /// <summary>
        /// 内存使用（KB）
        /// </summary>
        public long MemoryUsageKB { get; set; }
    }

    /// <summary>
    /// 性能等级
    /// </summary>
    public enum PerformanceLevel
    {
        Excellent,      // 优秀
        Good,           // 良好
        Average,        // 一般
        Poor,           // 较差
        Critical        // 严重
    }

    /// <summary>
    /// 批量操作类型
    /// </summary>
    public enum BatchOperationType
    {
        Insert,         // 批量插入
        Update,         // 批量更新
        Delete,         // 批量删除
        Upsert          // 批量更新或插入
    }

    /// <summary>
    /// 批量操作结果
    /// </summary>
    public class BatchOperationResult
    {
        /// <summary>
        /// 成功处理的记录数
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// 失败的记录数
        /// </summary>
        public int FailureCount { get; set; }

        /// <summary>
        /// 执行时间（毫秒）
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess => FailureCount == 0;

        /// <summary>
        /// 处理速度（记录/秒）
        /// </summary>
        public double ThroughputPerSecond => ExecutionTimeMs > 0 ? (SuccessCount * 1000.0 / ExecutionTimeMs) : 0;
    }

    /// <summary>
    /// 数据库统计信息
    /// </summary>
    public class DatabaseStatistics
    {
        /// <summary>
        /// 活动连接数
        /// </summary>
        public int ActiveConnections { get; set; }

        /// <summary>
        /// 总连接数
        /// </summary>
        public int TotalConnections { get; set; }

        /// <summary>
        /// 数据库大小（MB）
        /// </summary>
        public long DatabaseSizeMB { get; set; }

        /// <summary>
        /// 索引大小（MB）
        /// </summary>
        public long IndexSizeMB { get; set; }

        /// <summary>
        /// 平均查询时间（毫秒）
        /// </summary>
        public double AverageQueryTimeMs { get; set; }

        /// <summary>
        /// 慢查询数量
        /// </summary>
        public int SlowQueryCount { get; set; }

        /// <summary>
        /// 缓存命中率
        /// </summary>
        public double CacheHitRatio { get; set; }

        /// <summary>
        /// 死锁数量
        /// </summary>
        public int DeadlockCount { get; set; }

        /// <summary>
        /// 碎片化程度
        /// </summary>
        public double FragmentationPercentage { get; set; }
    }

    /// <summary>
    /// 查询优化选项
    /// </summary>
    public class QueryOptimizationOptions
    {
        /// <summary>
        /// 启用查询缓存
        /// </summary>
        public bool EnableQueryCaching { get; set; } = true;

        /// <summary>
        /// 启用自动包含导航属性
        /// </summary>
        public bool AutoIncludeNavigationProperties { get; set; } = false;

        /// <summary>
        /// 启用无跟踪查询
        /// </summary>
        public bool AsNoTracking { get; set; } = true;

        /// <summary>
        /// 最大返回记录数
        /// </summary>
        public int? MaxRecords { get; set; }

        /// <summary>
        /// 查询超时时间（秒）
        /// </summary>
        public int? QueryTimeoutSeconds { get; set; }

        /// <summary>
        /// 启用编译查询
        /// </summary>
        public bool UseCompiledQuery { get; set; } = false;

        /// <summary>
        /// 分页参数
        /// </summary>
        public PaginationOptions? Pagination { get; set; }
    }

    /// <summary>
    /// 分页选项
    /// </summary>
    public class PaginationOptions
    {
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public bool CountTotal { get; set; } = true;
    }

    /// <summary>
    /// 索引推荐
    /// </summary>
    public class IndexRecommendation
    {
        /// <summary>
        /// 表名
        /// </summary>
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// 推荐的列
        /// </summary>
        public List<string> Columns { get; set; } = new List<string>();

        /// <summary>
        /// 索引类型
        /// </summary>
        public IndexType IndexType { get; set; }

        /// <summary>
        /// 预计性能提升
        /// </summary>
        public double EstimatedImprovementPercent { get; set; }

        /// <summary>
        /// 创建索引的SQL
        /// </summary>
        public string CreateIndexSql { get; set; } = string.Empty;

        /// <summary>
        /// 推荐原因
        /// </summary>
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// 索引类型
    /// </summary>
    public enum IndexType
    {
        Clustered,      // 聚集索引
        NonClustered,   // 非聚集索引
        Unique,         // 唯一索引
        Filtered,       // 筛选索引
        Columnstore     // 列存储索引
    }

    /// <summary>
    /// 数据库维护选项
    /// </summary>
    public class MaintenanceOptions
    {
        /// <summary>
        /// 重建索引
        /// </summary>
        public bool RebuildIndexes { get; set; } = false;

        /// <summary>
        /// 重新组织索引
        /// </summary>
        public bool ReorganizeIndexes { get; set; } = true;

        /// <summary>
        /// 更新统计信息
        /// </summary>
        public bool UpdateStatistics { get; set; } = true;

        /// <summary>
        /// 收缩数据库
        /// </summary>
        public bool ShrinkDatabase { get; set; } = false;

        /// <summary>
        /// 清理查询计划缓存
        /// </summary>
        public bool ClearPlanCache { get; set; } = false;

        /// <summary>
        /// 碎片化阈值（百分比）
        /// </summary>
        public double FragmentationThreshold { get; set; } = 30.0;

        /// <summary>
        /// 要维护的表列表（为空表示所有表）
        /// </summary>
        public List<string> TableNames { get; set; } = new List<string>();
    }

    /// <summary>
    /// 维护结果
    /// </summary>
    public class MaintenanceResult
    {
        /// <summary>
        /// 执行的任务
        /// </summary>
        public List<string> CompletedTasks { get; set; } = new List<string>();

        /// <summary>
        /// 总执行时间（毫秒）
        /// </summary>
        public long TotalExecutionTimeMs { get; set; }

        /// <summary>
        /// 维护前数据库大小（MB）
        /// </summary>
        public long DatabaseSizeBeforeMB { get; set; }

        /// <summary>
        /// 维护后数据库大小（MB）
        /// </summary>
        public long DatabaseSizeAfterMB { get; set; }

        /// <summary>
        /// 节省的空间（MB）
        /// </summary>
        public long SpaceSavedMB => DatabaseSizeBeforeMB - DatabaseSizeAfterMB;

        /// <summary>
        /// 错误信息
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess => Errors.Count == 0;
    }

    /// <summary>
    /// 慢查询报告
    /// </summary>
    public class SlowQueryReport
    {
        /// <summary>
        /// 报告生成时间
        /// </summary>
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 报告时间范围
        /// </summary>
        public TimeSpan TimeRange { get; set; }

        /// <summary>
        /// 慢查询列表
        /// </summary>
        public List<SlowQuery> SlowQueries { get; set; } = new List<SlowQuery>();

        /// <summary>
        /// 总慢查询数
        /// </summary>
        public int TotalSlowQueries => SlowQueries.Count;

        /// <summary>
        /// 平均执行时间
        /// </summary>
        public double AverageExecutionTimeMs => SlowQueries.Any() ? SlowQueries.Average(q => q.ExecutionTimeMs) : 0;
    }

    /// <summary>
    /// 慢查询信息
    /// </summary>
    public class SlowQuery
    {
        /// <summary>
        /// SQL查询语句
        /// </summary>
        public string SqlText { get; set; } = string.Empty;

        /// <summary>
        /// 执行时间（毫秒）
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// 执行次数
        /// </summary>
        public int ExecutionCount { get; set; }

        /// <summary>
        /// 总执行时间（毫秒）
        /// </summary>
        public long TotalExecutionTimeMs { get; set; }

        /// <summary>
        /// 最后执行时间
        /// </summary>
        public DateTime LastExecutionTime { get; set; }

        /// <summary>
        /// CPU时间（毫秒）
        /// </summary>
        public long CpuTimeMs { get; set; }

        /// <summary>
        /// 逻辑读取次数
        /// </summary>
        public long LogicalReads { get; set; }

        /// <summary>
        /// 物理读取次数
        /// </summary>
        public long PhysicalReads { get; set; }
    }
}
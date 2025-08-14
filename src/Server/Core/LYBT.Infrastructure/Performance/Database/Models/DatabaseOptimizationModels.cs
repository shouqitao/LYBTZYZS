using System;
using System.Collections.Generic;

namespace LYBT.Infrastructure.Performance.Database.Models
{
    /// <summary>
    /// 索引分析报告
    /// </summary>
    public class IndexAnalysisReport
    {
        public DateTime AnalysisTime { get; set; }
        public int TotalIndexes { get; set; }
        public int FragmentedIndexes { get; set; }
        public int UnusedIndexes { get; set; }
        public int MissingIndexes { get; set; }
        public List<IndexDetail> IndexDetails { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        // 添加缺失的属性
        public List<string> Errors { get; set; } = new();
        // 添加详细的未使用索引列表
        public List<UnusedIndex> UnusedIndexDetails { get; set; } = new();
    }

    /// <summary>
    /// 索引详情
    /// </summary>
    public class IndexDetail
    {
        public string IndexName { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public double FragmentationPercent { get; set; }
        public long RowCount { get; set; }
        public long PageCount { get; set; }
        public DateTime LastUsed { get; set; }
        public bool IsUnused { get; set; }
    }

    /// <summary>
    /// 索引维护结果
    /// </summary>
    public class IndexMaintenanceResult
    {
        public bool Success { get; set; }
        public int IndexesRebuilt { get; set; }
        public int IndexesReorganized { get; set; }
        public int IndexesSkipped { get; set; }
        public TimeSpan Duration { get; set; }
        public List<string> Errors { get; set; } = new();
        public DateTime MaintenanceTime { get; set; }
        // 添加缺失的属性
        public int RebuiltIndexCount => IndexesRebuilt;
        public int ReorganizedIndexCount => IndexesReorganized;
        public int SkippedIndexCount => IndexesSkipped;
    }

    /// <summary>
    /// 详细数据库统计信息
    /// </summary>
    public class DetailedDatabaseStatistics
    {
        public DateTime CollectionTime { get; set; }
        public long DatabaseSizeMB { get; set; }
        public long DataSizeMB { get; set; }
        public long IndexSizeMB { get; set; }
        public long LogSizeMB { get; set; }
        public int TableCount { get; set; }
        public int IndexCount { get; set; }
        public int ConnectionCount { get; set; }
        public double CacheHitRatio { get; set; }
        public List<TableStatistics> TableStats { get; set; } = new();
        public List<IndexStatistics> IndexStats { get; set; } = new();
        public ConnectionStatistics ConnectionStats { get; set; } = new();
        // 添加缺失的属性别名
        public DatabaseStatistics BasicStatistics { get; set; } = new();
        public List<TableStatistics> TableStatistics => TableStats;
        public List<IndexStatistics> IndexStatistics => IndexStats;
        public ConnectionStatistics ConnectionStatistics => ConnectionStats;
        public RealTimePerformanceMetrics PerformanceCounters { get; set; } = new();
    }

    /// <summary>
    /// 实时性能指标
    /// </summary>
    public class RealTimePerformanceMetrics
    {
        public DateTime Timestamp { get; set; }
        public double CpuUsagePercent { get; set; }
        public double MemoryUsageMB { get; set; }
        public double DiskIOMBPerSec { get; set; }
        public int ActiveConnections { get; set; }
        public int ActiveQueries { get; set; }
        public double AverageQueryTimeMs { get; set; }
        public int QueriesPerSecond { get; set; }
        public double BufferCacheHitRatio { get; set; }
        public int DeadlockCount { get; set; }
        public int BlockedProcesses { get; set; }
        // 添加缺失的属性
        public DateTime CollectionTime { get; set; }
        public double BatchRequestsPerSecond { get; set; }
        public double PageReadsPerSecond { get; set; }
        public double PageWritesPerSecond { get; set; }
    }

    /// <summary>
    /// 连接池优化选项
    /// </summary>
    public class ConnectionPoolOptimizationOptions
    {
        public int MinPoolSize { get; set; } = 10;
        public int MaxPoolSize { get; set; } = 100;
        public int ConnectionTimeout { get; set; } = 30;
        public int ConnectionLifetime { get; set; } = 0;
        public bool Pooling { get; set; } = true;
        public bool LoadBalancing { get; set; } = false;
    }

    /// <summary>
    /// 连接池优化结果
    /// </summary>
    public class ConnectionPoolOptimizationResult
    {
        public bool Success { get; set; }
        public int PreviousMaxPool { get; set; }
        public int NewMaxPool { get; set; }
        public int ConnectionsRecycled { get; set; }
        public TimeSpan OptimizationDuration { get; set; }
        public string Message { get; set; } = string.Empty;
        // 添加缺失的属性
        public List<string> TestResults { get; set; } = new();
        public bool IsOptimized { get; set; }
        public int OptimalPoolSize { get; set; }
        public double PerformanceScore { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// 查询优化建议
    /// </summary>
    public class QueryOptimizationSuggestions
    {
        public string QueryHash { get; set; } = string.Empty;
        public List<string> Suggestions { get; set; } = new();
        public List<string> MissingIndexes { get; set; } = new();
        public List<string> UnusedIndexes { get; set; } = new();
        public double EstimatedImprovementPercent { get; set; }
        public string OptimizedQuery { get; set; } = string.Empty;
        // 添加缺失的属性
        public string SqlQuery { get; set; } = string.Empty;
        public int ComplexityScore { get; set; }
        public List<string> Recommendations { get; set; } = new();
        public string EstimatedPerformanceLevel { get; set; } = string.Empty;
    }

    /// <summary>
    /// 表统计信息
    /// </summary>
    public class TableStatistics
    {
        public string TableName { get; set; } = string.Empty;
        public long RowCount { get; set; }
        public long DataSizeKB { get; set; }
        public long IndexSizeKB { get; set; }
        public DateTime LastUpdate { get; set; }
        public int IndexCount { get; set; }
        // 添加缺失的属性
        public double SizeMB => DataSizeKB / 1024.0;
        public double UsedSizeMB => (DataSizeKB + IndexSizeKB) / 1024.0;
    }

    /// <summary>
    /// 索引统计信息
    /// </summary>
    public class IndexStatistics
    {
        public string IndexName { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public long SizeKB { get; set; }
        public double FragmentationPercent { get; set; }
        public long UserSeeks { get; set; }
        public long UserScans { get; set; }
        public DateTime LastUsed { get; set; }
        // 添加缺失的属性
        public string IndexType { get; set; } = string.Empty;
        public long UserLookups { get; set; }
        public long UserUpdates { get; set; }
        public DateTime LastUserSeek { get; set; }
        public DateTime LastUserScan { get; set; }
    }

    /// <summary>
    /// 活动慢查询
    /// </summary>
    public class ActiveSlowQuery
    {
        public int SessionId { get; set; }
        public string QueryText { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public DateTime StartTime { get; set; }
        public string WaitType { get; set; } = string.Empty;
        public long CpuTime { get; set; }
        public long LogicalReads { get; set; }
        public string BlockingSession { get; set; } = string.Empty;
        // 添加缺失的属性
        public int RequestId { get; set; }
        public long ElapsedTimeMs { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public double PercentComplete { get; set; }
        public long EstimatedCompletionTimeMs { get; set; }
        public long PhysicalReads { get; set; }
        public long Writes { get; set; }
        public string SqlText { get; set; } = string.Empty;
    }

    /// <summary>
    /// 连接统计信息
    /// </summary>
    public class ConnectionStatistics
    {
        public int TotalConnections { get; set; }
        public int ActiveConnections { get; set; }
        public int IdleConnections { get; set; }
        public int BlockedConnections { get; set; }
        public double AverageConnectionTimeMs { get; set; }
        public DateTime OldestConnectionTime { get; set; }
        // 添加缺失的属性
        public int SleepingConnections { get; set; }
        public int RunningConnections { get; set; }
    }

    /// <summary>
    /// 查询性能趋势
    /// </summary>
    public class QueryPerformanceTrend
    {
        public string QueryHash { get; set; } = string.Empty;
        public List<QueryPerformancePoint> DataPoints { get; set; } = new();
        public double AverageExecutionTime { get; set; }
        public double TrendSlope { get; set; }
        public string TrendDirection { get; set; } = string.Empty;
        public bool IsDegrading { get; set; }
        // 添加缺失的属性
        public TimeSpan TimeWindow { get; set; }
        public int IntervalMinutes { get; set; }
        public List<QueryIntervalStats> IntervalStats { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public int TotalQueries { get; set; }
        public double AverageQueriesPerInterval { get; set; }
        public string PerformanceTrendDirection { get; set; } = string.Empty;
        public double TrendMagnitude { get; set; }
    }

    /// <summary>
    /// 查询性能数据点
    /// </summary>
    public class QueryPerformancePoint
    {
        public DateTime Timestamp { get; set; }
        public double ExecutionTimeMs { get; set; }
        public long LogicalReads { get; set; }
        public long PhysicalReads { get; set; }
    }

    /// <summary>
    /// 查询间隔统计
    /// </summary>
    public class QueryIntervalStats
    {
        public TimeSpan Interval { get; set; }
        public int QueryCount { get; set; }
        public double AverageExecutionTime { get; set; }
        public double MaxExecutionTime { get; set; }
        public double MinExecutionTime { get; set; }
        public long TotalLogicalReads { get; set; }
        // 添加缺失的属性
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public double AverageExecutionTimeMs { get; set; }
        public double MaxExecutionTimeMs { get; set; }
        public long TotalExecutions { get; set; }
    }

    /// <summary>
    /// 查询类型统计
    /// </summary>
    public class QueryTypeStats
    {
        /// <summary>
        /// 查询类型
        /// </summary>
        public string QueryType { get; set; } = string.Empty;
        
        /// <summary>
        /// 查询数量
        /// </summary>
        public int Count { get; set; }
        
        /// <summary>
        /// 平均执行时间
        /// </summary>
        public double AverageExecutionTime { get; set; }
        
        /// <summary>
        /// 总执行时间
        /// </summary>
        public long TotalExecutionTime { get; set; }
    }
    
    /// <summary>
    /// 未使用索引信息
    /// </summary>
    public class UnusedIndex
    {
        /// <summary>
        /// 索引名称
        /// </summary>
        public string IndexName { get; set; } = string.Empty;
        
        /// <summary>
        /// 表名称
        /// </summary>
        public string TableName { get; set; } = string.Empty;
        
        /// <summary>
        /// 索引类型
        /// </summary>
        public string IndexType { get; set; } = string.Empty;
        
        /// <summary>
        /// 索引大小KB
        /// </summary>
        public long SizeKB { get; set; }
        
        /// <summary>
        /// 最后使用时间
        /// </summary>
        public DateTime LastUsed { get; set; }
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Performance.Database.Models;

namespace LYBT.Infrastructure.Performance.Database.Components
{
    /// <summary>
    /// 慢查询分析器 - UltraThink专门化组件
    /// 职责单一：专注慢查询的检测、分析和报告生成
    /// 代码干净：清晰的查询分析逻辑和报告生成
    /// 性能出色：高效的查询统计和分析算法
    /// </summary>
    public class SlowQueryAnalyzer
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SlowQueryAnalyzer> _logger;

        public SlowQueryAnalyzer(AppDbContext context, ILogger<SlowQueryAnalyzer> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 核心分析方法

        /// <summary>
        /// 获取慢查询报告
        /// </summary>
        public async Task<SlowQueryReport> GetSlowQueryReportAsync(
            DateTime startTime, 
            DateTime endTime, 
            CancellationToken cancellationToken = default)
        {
            var report = new SlowQueryReport
            {
                TimeRange = endTime - startTime,
                GeneratedAt = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("开始生成慢查询报告: {StartTime} - {EndTime}", startTime, endTime);

                var connection = _context.Database.GetDbConnection();
                await EnsureConnectionOpenAsync(connection, cancellationToken);

                if (IsSqlServer(connection))
                {
                    report.SlowQueries = await GetSqlServerSlowQueriesAsync(startTime, endTime, connection, cancellationToken);
                }
                else
                {
                    // 对于其他数据库类型，可以实现相应的慢查询检测逻辑
                    report.SlowQueries = await GetGenericSlowQueriesAsync(startTime, endTime, connection, cancellationToken);
                }

                // 生成分析统计
                report.TotalSlowQueries = report.SlowQueries.Count;
                report.AverageExecutionTime = report.SlowQueries.Any() 
                    ? report.SlowQueries.Average(q => q.ExecutionTimeMs) 
                    : 0;
                report.MaxExecutionTime = report.SlowQueries.Any() 
                    ? report.SlowQueries.Max(q => q.ExecutionTimeMs) 
                    : 0;

                // 分析查询类型分布
                AnalyzeQueryTypeDistribution(report);

                // 生成优化建议
                report.OptimizationSuggestions = GenerateOptimizationSuggestions(report.SlowQueries);

                _logger.LogInformation("慢查询报告生成完成: 时间范围={TimeRange}, 慢查询数={Count}, 平均执行时间={AvgTime}ms", 
                    report.TimeRange, report.TotalSlowQueries, report.AverageExecutionTime);

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成慢查询报告失败");
                report.Errors.Add(ex.Message);
                return report;
            }
        }

        /// <summary>
        /// 获取实时慢查询
        /// </summary>
        public async Task<List<ActiveSlowQuery>> GetActiveSlowQueriesAsync(
            long minExecutionTimeMs = 5000,
            CancellationToken cancellationToken = default)
        {
            var activeQueries = new List<ActiveSlowQuery>();

            try
            {
                _logger.LogDebug("开始获取实时慢查询，最小执行时间: {MinTime}ms", minExecutionTimeMs);

                var connection = _context.Database.GetDbConnection();
                await EnsureConnectionOpenAsync(connection, cancellationToken);

                if (IsSqlServer(connection))
                {
                    activeQueries = await GetSqlServerActiveSlowQueriesAsync(minExecutionTimeMs, connection, cancellationToken);
                }

                _logger.LogInformation("获取实时慢查询完成: {Count} 个查询", activeQueries.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取实时慢查询失败");
            }

            return activeQueries;
        }

        /// <summary>
        /// 分析查询性能趋势
        /// </summary>
        public async Task<QueryPerformanceTrend> AnalyzeQueryPerformanceTrendAsync(
            TimeSpan timeWindow,
            int intervalMinutes = 15,
            CancellationToken cancellationToken = default)
        {
            var trend = new QueryPerformanceTrend
            {
                TimeWindow = timeWindow,
                IntervalMinutes = intervalMinutes
            };

            try
            {
                _logger.LogInformation("开始分析查询性能趋势: 时间窗口={TimeWindow}, 间隔={Interval}分钟", 
                    timeWindow, intervalMinutes);

                var endTime = DateTime.UtcNow;
                var startTime = endTime - timeWindow;
                var intervals = GenerateTimeIntervals(startTime, endTime, TimeSpan.FromMinutes(intervalMinutes));

                foreach (var interval in intervals)
                {
                    var intervalStats = await GetQueryStatsForIntervalAsync(
                        interval.Start, interval.End, cancellationToken);
                    
                    trend.IntervalStats.Add(intervalStats);
                }

                // 计算趋势指标
                CalculateTrendMetrics(trend);

                _logger.LogInformation("查询性能趋势分析完成: {IntervalCount} 个时间段", trend.IntervalStats.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分析查询性能趋势失败");
                trend.Errors.Add(ex.Message);
            }

            return trend;
        }

        #endregion

        #region SQL Server 特定分析

        /// <summary>
        /// 获取SQL Server慢查询
        /// </summary>
        private async Task<List<SlowQuery>> GetSqlServerSlowQueriesAsync(
            DateTime startTime, 
            DateTime endTime, 
            DbConnection connection, 
            CancellationToken cancellationToken)
        {
            var slowQueries = new List<SlowQuery>();
            
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT TOP 100
                        qs.sql_handle,
                        qs.execution_count,
                        qs.total_elapsed_time / 1000 AS total_elapsed_time_ms,
                        qs.total_elapsed_time / qs.execution_count / 1000 AS avg_elapsed_time_ms,
                        qs.last_execution_time,
                        qs.total_worker_time / 1000 AS total_cpu_time_ms,
                        qs.total_logical_reads,
                        qs.total_physical_reads,
                        qs.total_writes,
                        SUBSTRING(st.text, (qs.statement_start_offset/2)+1, 
                            ((CASE qs.statement_end_offset
                                WHEN -1 THEN DATALENGTH(st.text)
                                ELSE qs.statement_end_offset
                            END - qs.statement_start_offset)/2) + 1) AS sql_text
                    FROM sys.dm_exec_query_stats qs
                    CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) st
                    WHERE qs.last_execution_time BETWEEN @StartTime AND @EndTime
                        AND qs.total_elapsed_time / qs.execution_count > 1000000 -- 超过1秒的查询
                    ORDER BY qs.total_elapsed_time / qs.execution_count DESC";
                
                var startParam = command.CreateParameter();
                startParam.ParameterName = "@StartTime";
                startParam.Value = startTime;
                command.Parameters.Add(startParam);
                
                var endParam = command.CreateParameter();
                endParam.ParameterName = "@EndTime";
                endParam.Value = endTime;
                command.Parameters.Add(endParam);
                
                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var slowQuery = new SlowQuery
                    {
                        SqlText = reader["sql_text"]?.ToString() ?? string.Empty,
                        ExecutionCount = Convert.ToInt32(reader["execution_count"]),
                        ExecutionTimeMs = Convert.ToInt64(reader["avg_elapsed_time_ms"]),
                        TotalExecutionTimeMs = Convert.ToInt64(reader["total_elapsed_time_ms"]),
                        LastExecutionTime = Convert.ToDateTime(reader["last_execution_time"]),
                        CpuTimeMs = Convert.ToInt64(reader["total_cpu_time_ms"]),
                        LogicalReads = Convert.ToInt64(reader["total_logical_reads"]),
                        PhysicalReads = Convert.ToInt64(reader["total_physical_reads"]),
                        Writes = Convert.ToInt64(reader["total_writes"]),
                        QueryType = ClassifyQueryType(reader["sql_text"]?.ToString() ?? string.Empty)
                    };
                    
                    slowQueries.Add(slowQuery);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取SQL Server慢查询失败");
            }

            return slowQueries;
        }

        /// <summary>
        /// 获取SQL Server实时慢查询
        /// </summary>
        private async Task<List<ActiveSlowQuery>> GetSqlServerActiveSlowQueriesAsync(
            long minExecutionTimeMs, 
            DbConnection connection, 
            CancellationToken cancellationToken)
        {
            var activeQueries = new List<ActiveSlowQuery>();

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT 
                        r.session_id,
                        r.request_id,
                        r.start_time,
                        DATEDIFF(ms, r.start_time, GETDATE()) AS elapsed_time_ms,
                        r.status,
                        r.command,
                        r.percent_complete,
                        r.estimated_completion_time,
                        r.cpu_time,
                        r.logical_reads,
                        r.reads,
                        r.writes,
                        SUBSTRING(st.text, (r.statement_start_offset/2)+1, 
                            ((CASE r.statement_end_offset
                                WHEN -1 THEN DATALENGTH(st.text)
                                ELSE r.statement_end_offset
                            END - r.statement_start_offset)/2) + 1) AS sql_text
                    FROM sys.dm_exec_requests r
                    CROSS APPLY sys.dm_exec_sql_text(r.sql_handle) st
                    WHERE DATEDIFF(ms, r.start_time, GETDATE()) > @MinExecutionTime
                        AND r.session_id != @@SPID
                    ORDER BY elapsed_time_ms DESC";

                var param = command.CreateParameter();
                param.ParameterName = "@MinExecutionTime";
                param.Value = minExecutionTimeMs;
                command.Parameters.Add(param);

                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var activeQuery = new ActiveSlowQuery
                    {
                        SessionId = reader.GetInt32("session_id"),
                        RequestId = reader.GetInt32("request_id"),
                        StartTime = reader.GetDateTime("start_time"),
                        ElapsedTimeMs = Convert.ToInt64(reader["elapsed_time_ms"]),
                        Status = reader.GetString("status"),
                        Command = reader.GetString("command"),
                        PercentComplete = Convert.ToDouble(reader["percent_complete"]),
                        EstimatedCompletionTimeMs = Convert.ToInt64(reader["estimated_completion_time"]),
                        CpuTime = Convert.ToInt64(reader["cpu_time"]),
                        LogicalReads = Convert.ToInt64(reader["logical_reads"]),
                        PhysicalReads = Convert.ToInt64(reader["reads"]),
                        Writes = Convert.ToInt64(reader["writes"]),
                        SqlText = reader["sql_text"]?.ToString() ?? string.Empty
                    };

                    activeQueries.Add(activeQuery);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取SQL Server实时慢查询失败");
            }

            return activeQueries;
        }

        #endregion

        #region 通用分析方法

        /// <summary>
        /// 获取通用慢查询（非SQL Server）
        /// </summary>
        private async Task<List<SlowQuery>> GetGenericSlowQueriesAsync(
            DateTime startTime,
            DateTime endTime,
            DbConnection connection,
            CancellationToken cancellationToken)
        {
            // 这里可以实现其他数据库类型的慢查询检测逻辑
            // 目前返回空列表作为占位符
            await Task.Delay(1, cancellationToken);
            return new List<SlowQuery>();
        }

        /// <summary>
        /// 获取时间间隔内的查询统计
        /// </summary>
        private async Task<QueryIntervalStats> GetQueryStatsForIntervalAsync(
            DateTime startTime,
            DateTime endTime,
            CancellationToken cancellationToken)
        {
            var stats = new QueryIntervalStats
            {
                StartTime = startTime,
                EndTime = endTime
            };

            try
            {
                var connection = _context.Database.GetDbConnection();
                await EnsureConnectionOpenAsync(connection, cancellationToken);

                if (IsSqlServer(connection))
                {
                    using var command = connection.CreateCommand();
                    command.CommandText = @"
                        SELECT 
                            COUNT(*) AS QueryCount,
                            AVG(total_elapsed_time / execution_count / 1000) AS AvgExecutionTime,
                            MAX(total_elapsed_time / execution_count / 1000) AS MaxExecutionTime,
                            SUM(execution_count) AS TotalExecutions
                        FROM sys.dm_exec_query_stats qs
                        WHERE qs.last_execution_time BETWEEN @StartTime AND @EndTime";

                    var startParam = command.CreateParameter();
                    startParam.ParameterName = "@StartTime";
                    startParam.Value = startTime;
                    command.Parameters.Add(startParam);

                    var endParam = command.CreateParameter();
                    endParam.ParameterName = "@EndTime";
                    endParam.Value = endTime;
                    command.Parameters.Add(endParam);

                    using var reader = await command.ExecuteReaderAsync(cancellationToken);
                    if (await reader.ReadAsync(cancellationToken))
                    {
                        stats.QueryCount = Convert.ToInt32(reader["QueryCount"] ?? 0);
                        stats.AverageExecutionTimeMs = Convert.ToDouble(reader["AvgExecutionTime"] ?? 0);
                        stats.MaxExecutionTimeMs = Convert.ToDouble(reader["MaxExecutionTime"] ?? 0);
                        stats.TotalExecutions = Convert.ToInt64(reader["TotalExecutions"] ?? 0);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取时间间隔查询统计失败: {StartTime}-{EndTime}", startTime, endTime);
            }

            return stats;
        }

        #endregion

        #region 分析辅助方法

        /// <summary>
        /// 分析查询类型分布
        /// </summary>
        private void AnalyzeQueryTypeDistribution(SlowQueryReport report)
        {
            var typeGroups = report.SlowQueries
                .GroupBy(q => q.QueryType)
                .Select(g => new QueryTypeStats
                {
                    QueryType = g.Key,
                    Count = g.Count(),
                    AverageExecutionTime = g.Average(q => q.ExecutionTimeMs),
                    TotalExecutionTime = g.Sum(q => q.TotalExecutionTimeMs)
                })
                .OrderByDescending(s => s.Count)
                .ToList();

            report.QueryTypeDistribution = typeGroups;
        }

        /// <summary>
        /// 生成优化建议
        /// </summary>
        private List<string> GenerateOptimizationSuggestions(List<SlowQuery> slowQueries)
        {
            var suggestions = new List<string>();

            if (!slowQueries.Any())
            {
                suggestions.Add("未发现慢查询，系统性能良好");
                return suggestions;
            }

            // 分析最慢的查询
            var slowestQuery = slowQueries.OrderByDescending(q => q.ExecutionTimeMs).First();
            suggestions.Add($"最慢查询执行时间 {slowestQuery.ExecutionTimeMs}ms，建议重点优化");

            // 分析高频慢查询
            var frequentSlowQueries = slowQueries.Where(q => q.ExecutionCount > 10).ToList();
            if (frequentSlowQueries.Any())
            {
                suggestions.Add($"发现 {frequentSlowQueries.Count} 个高频慢查询，建议优先优化");
            }

            // 分析SELECT查询
            var selectQueries = slowQueries.Where(q => q.QueryType == "SELECT").ToList();
            if (selectQueries.Count > slowQueries.Count * 0.8)
            {
                suggestions.Add("大量SELECT慢查询，建议检查索引策略和查询条件");
            }

            // 分析UPDATE/DELETE查询
            var modifyQueries = slowQueries.Where(q => q.QueryType is "UPDATE" or "DELETE").ToList();
            if (modifyQueries.Any())
            {
                suggestions.Add($"发现 {modifyQueries.Count} 个修改操作慢查询，建议检查事务范围和锁争用");
            }

            // 分析物理读取
            var highPhysicalReads = slowQueries.Where(q => q.PhysicalReads > 1000).ToList();
            if (highPhysicalReads.Any())
            {
                suggestions.Add($"发现 {highPhysicalReads.Count} 个高物理读取查询，建议优化缓冲池配置");
            }

            return suggestions;
        }

        /// <summary>
        /// 分类查询类型
        /// </summary>
        private string ClassifyQueryType(string sqlText)
        {
            if (string.IsNullOrEmpty(sqlText))
                return "UNKNOWN";

            var normalizedSql = sqlText.Trim().ToUpper();

            return normalizedSql switch
            {
                var s when s.StartsWith("SELECT") => "SELECT",
                var s when s.StartsWith("INSERT") => "INSERT",
                var s when s.StartsWith("UPDATE") => "UPDATE",
                var s when s.StartsWith("DELETE") => "DELETE",
                var s when s.StartsWith("EXEC") || s.StartsWith("EXECUTE") => "PROCEDURE",
                var s when s.StartsWith("CREATE") => "CREATE",
                var s when s.StartsWith("ALTER") => "ALTER",
                var s when s.StartsWith("DROP") => "DROP",
                _ => "OTHER"
            };
        }

        /// <summary>
        /// 生成时间间隔
        /// </summary>
        private List<(DateTime Start, DateTime End)> GenerateTimeIntervals(
            DateTime startTime, 
            DateTime endTime, 
            TimeSpan intervalSize)
        {
            var intervals = new List<(DateTime Start, DateTime End)>();
            var currentStart = startTime;

            while (currentStart < endTime)
            {
                var currentEnd = currentStart.Add(intervalSize);
                if (currentEnd > endTime)
                    currentEnd = endTime;

                intervals.Add((currentStart, currentEnd));
                currentStart = currentEnd;
            }

            return intervals;
        }

        /// <summary>
        /// 计算趋势指标
        /// </summary>
        private void CalculateTrendMetrics(QueryPerformanceTrend trend)
        {
            if (!trend.IntervalStats.Any())
                return;

            // 计算总体指标
            trend.TotalQueries = trend.IntervalStats.Sum(s => s.QueryCount);
            trend.AverageQueriesPerInterval = trend.IntervalStats.Average(s => s.QueryCount);
            
            // 计算趋势方向
            var firstHalf = trend.IntervalStats.Take(trend.IntervalStats.Count / 2);
            var secondHalf = trend.IntervalStats.Skip(trend.IntervalStats.Count / 2);
            
            var firstHalfAvg = firstHalf.Average(s => s.AverageExecutionTimeMs);
            var secondHalfAvg = secondHalf.Average(s => s.AverageExecutionTimeMs);
            
            trend.PerformanceTrendDirection = secondHalfAvg > firstHalfAvg ? "恶化" : "改善";
            trend.TrendMagnitude = Math.Abs(secondHalfAvg - firstHalfAvg);
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 确保数据库连接已打开
        /// </summary>
        private async Task EnsureConnectionOpenAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }
        }

        /// <summary>
        /// 检查是否为SQL Server
        /// </summary>
        private bool IsSqlServer(DbConnection connection)
        {
            return connection.GetType().Name.Contains("SqlConnection", StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}
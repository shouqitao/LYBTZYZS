using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using LYBT.Infrastructure.Data;

namespace LYBT.Infrastructure.Performance.Database.Components
{
    /// <summary>
    /// 查询性能分析器 - UltraThink专门化组件
    /// 职责单一：专注查询性能分析、执行时间测量和优化建议生成
    /// 代码干净：清晰的性能分析逻辑和建议生成
    /// 性能出色：高效的性能测量和分析算法
    /// </summary>
    public class QueryPerformanceAnalyzer
    {
        private readonly AppDbContext _context;
        private readonly ILogger<QueryPerformanceAnalyzer> _logger;

        public QueryPerformanceAnalyzer(AppDbContext context, ILogger<QueryPerformanceAnalyzer> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 核心性能分析方法

        /// <summary>
        /// 分析查询性能
        /// </summary>
        public async Task<QueryPerformanceAnalysis> AnalyzeQueryPerformanceAsync<T>(
            IQueryable<T> query, 
            CancellationToken cancellationToken = default) where T : class
        {
            ArgumentNullException.ThrowIfNull(query);

            var analysis = new QueryPerformanceAnalysis();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogDebug("开始查询性能分析: {QueryType}", typeof(T).Name);

                // 获取SQL查询语句
                analysis.SqlQuery = query.ToQueryString();

                // 启用统计信息收集
                var connection = _context.Database.GetDbConnection();
                await EnsureConnectionOpenAsync(connection, cancellationToken);

                await EnableStatisticsCollectionAsync(connection, cancellationToken);

                // 执行查询并测量性能
                var performanceData = await MeasureQueryExecutionAsync(query, cancellationToken);
                analysis.ExecutionTimeMs = performanceData.ExecutionTime;
                analysis.RecordCount = performanceData.RecordCount;

                // 关闭统计信息收集
                await DisableStatisticsCollectionAsync(connection, cancellationToken);

                // 分析性能等级
                analysis.PerformanceLevel = AnalyzePerformanceLevel(analysis.ExecutionTimeMs, analysis.RecordCount);

                // 生成优化建议
                analysis.OptimizationSuggestions = GenerateOptimizationSuggestions(analysis);

                // 获取执行计划（仅SQL Server）
                if (IsSqlServer(connection))
                {
                    analysis.ExecutionPlan = await GetExecutionPlanAsync(analysis.SqlQuery, connection, cancellationToken);
                }

                _logger.LogInformation("查询性能分析完成: 执行时间={ExecutionTime}ms, 记录数={RecordCount}, 性能等级={Level}", 
                    analysis.ExecutionTimeMs, analysis.RecordCount, analysis.PerformanceLevel);

                return analysis;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询性能分析失败: {QueryType}", typeof(T).Name);
                analysis.OptimizationSuggestions.Add($"分析过程中发生错误: {ex.Message}");
                return analysis;
            }
            finally
            {
                stopwatch.Stop();
            }
        }

        /// <summary>
        /// 批量分析多个查询的性能
        /// </summary>
        public async Task<List<QueryPerformanceAnalysis>> AnalyzeMultipleQueriesAsync<T>(
            IEnumerable<IQueryable<T>> queries, 
            CancellationToken cancellationToken = default) where T : class
        {
            var results = new List<QueryPerformanceAnalysis>();
            var queriesList = queries.ToList();

            try
            {
                _logger.LogInformation("开始批量查询性能分析，查询数量: {Count}", queriesList.Count);

                foreach (var query in queriesList)
                {
                    var analysis = await AnalyzeQueryPerformanceAsync(query, cancellationToken);
                    results.Add(analysis);
                }

                // 生成批量分析总结
                var summary = GenerateBatchAnalysisSummary(results);
                _logger.LogInformation("批量查询性能分析完成: {Summary}", summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量查询性能分析失败");
            }

            return results;
        }

        #endregion

        #region 性能测量方法

        /// <summary>
        /// 测量查询执行性能
        /// </summary>
        private async Task<QueryExecutionData> MeasureQueryExecutionAsync<T>(
            IQueryable<T> query, 
            CancellationToken cancellationToken) where T : class
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var results = await query.ToListAsync(cancellationToken);
                stopwatch.Stop();

                return new QueryExecutionData
                {
                    ExecutionTime = stopwatch.ElapsedMilliseconds,
                    RecordCount = results.Count
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "查询执行测量失败");
                
                return new QueryExecutionData
                {
                    ExecutionTime = stopwatch.ElapsedMilliseconds,
                    RecordCount = 0
                };
            }
        }

        /// <summary>
        /// 分析性能等级
        /// </summary>
        private PerformanceLevel AnalyzePerformanceLevel(long executionTimeMs, int recordCount)
        {
            var avgTimePerRecord = recordCount > 0 ? (double)executionTimeMs / recordCount : executionTimeMs;

            return avgTimePerRecord switch
            {
                < 0.1 => PerformanceLevel.Excellent,
                < 1.0 => PerformanceLevel.Good,
                < 5.0 => PerformanceLevel.Average,
                < 20.0 => PerformanceLevel.Poor,
                _ => PerformanceLevel.Critical
            };
        }

        #endregion

        #region 优化建议生成

        /// <summary>
        /// 生成优化建议
        /// </summary>
        private List<string> GenerateOptimizationSuggestions(QueryPerformanceAnalysis analysis)
        {
            var suggestions = new List<string>();

            try
            {
                // 执行时间分析
                if (analysis.ExecutionTimeMs > 1000)
                {
                    suggestions.Add("查询执行时间超过1秒，建议优化查询条件或添加索引");
                }

                // 记录数分析
                if (analysis.RecordCount > 10000)
                {
                    suggestions.Add("返回记录数过多，建议使用分页查询");
                }

                // SQL语句分析
                var sqlAnalysis = AnalyzeSqlStatement(analysis.SqlQuery);
                suggestions.AddRange(sqlAnalysis);

                // 性能等级建议
                var levelSuggestions = GeneratePerformanceLevelSuggestions(analysis.PerformanceLevel);
                suggestions.AddRange(levelSuggestions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成优化建议失败");
                suggestions.Add("生成优化建议时发生错误");
            }

            return suggestions;
        }

        /// <summary>
        /// 分析SQL语句
        /// </summary>
        private List<string> AnalyzeSqlStatement(string sqlQuery)
        {
            var suggestions = new List<string>();

            if (string.IsNullOrEmpty(sqlQuery))
                return suggestions;

            // SELECT * 检查
            if (sqlQuery.Contains("SELECT *", StringComparison.OrdinalIgnoreCase))
            {
                suggestions.Add("避免使用SELECT *，建议明确指定需要的列");
            }

            // WHERE 条件检查
            if (!sqlQuery.Contains("WHERE", StringComparison.OrdinalIgnoreCase))
            {
                suggestions.Add("查询缺少WHERE条件，可能导致全表扫描");
            }

            // LIKE 前缀通配符检查
            if (sqlQuery.Contains("LIKE '%", StringComparison.OrdinalIgnoreCase))
            {
                suggestions.Add("前缀通配符查询无法使用索引，考虑全文搜索或其他方案");
            }

            // JOIN 检查
            var joinCount = CountJoins(sqlQuery);
            if (joinCount > 5)
            {
                suggestions.Add($"查询包含{joinCount}个JOIN，考虑简化查询或使用视图");
            }

            // ORDER BY 检查
            if (sqlQuery.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase) && 
                !sqlQuery.Contains("TOP", StringComparison.OrdinalIgnoreCase))
            {
                suggestions.Add("ORDER BY 没有配合TOP使用，考虑添加记录限制");
            }

            return suggestions;
        }

        /// <summary>
        /// 生成性能等级建议
        /// </summary>
        private List<string> GeneratePerformanceLevelSuggestions(PerformanceLevel level)
        {
            var suggestions = new List<string>();

            switch (level)
            {
                case PerformanceLevel.Critical:
                    suggestions.Add("性能严重不足，需要立即优化");
                    suggestions.Add("建议检查索引、查询逻辑和数据库设计");
                    break;
                case PerformanceLevel.Poor:
                    suggestions.Add("性能较差，建议进行优化");
                    suggestions.Add("考虑添加适当的索引或优化查询条件");
                    break;
                case PerformanceLevel.Average:
                    suggestions.Add("性能一般，可考虑进一步优化");
                    break;
                case PerformanceLevel.Good:
                    suggestions.Add("性能良好，可监控后续变化");
                    break;
                case PerformanceLevel.Excellent:
                    suggestions.Add("性能优秀，保持当前状态");
                    break;
            }

            return suggestions;
        }

        /// <summary>
        /// 生成批量分析总结
        /// </summary>
        private string GenerateBatchAnalysisSummary(List<QueryPerformanceAnalysis> analyses)
        {
            if (!analyses.Any())
                return "无查询分析结果";

            var avgExecutionTime = analyses.Average(a => a.ExecutionTimeMs);
            var totalRecords = analyses.Sum(a => a.RecordCount);
            var criticalQueries = analyses.Count(a => a.PerformanceLevel == PerformanceLevel.Critical);
            var poorQueries = analyses.Count(a => a.PerformanceLevel == PerformanceLevel.Poor);

            return $"总查询数: {analyses.Count}, 平均执行时间: {avgExecutionTime:F2}ms, " +
                   $"总记录数: {totalRecords}, 严重问题查询: {criticalQueries}, 性能较差查询: {poorQueries}";
        }

        #endregion

        #region 执行计划分析

        /// <summary>
        /// 获取执行计划
        /// </summary>
        private async Task<string> GetExecutionPlanAsync(string sqlQuery, DbConnection connection, CancellationToken cancellationToken)
        {
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = $"SET SHOWPLAN_XML ON; {sqlQuery}; SET SHOWPLAN_XML OFF;";
                
                var result = await command.ExecuteScalarAsync(cancellationToken);
                return result?.ToString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取执行计划失败");
                return string.Empty;
            }
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
        /// 启用统计信息收集
        /// </summary>
        private async Task EnableStatisticsCollectionAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            if (IsSqlServer(connection))
            {
                using var command = connection.CreateCommand();
                command.CommandText = "SET STATISTICS IO ON; SET STATISTICS TIME ON;";
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        /// <summary>
        /// 关闭统计信息收集
        /// </summary>
        private async Task DisableStatisticsCollectionAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            if (IsSqlServer(connection))
            {
                using var command = connection.CreateCommand();
                command.CommandText = "SET STATISTICS IO OFF; SET STATISTICS TIME OFF;";
                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        /// <summary>
        /// 检查是否为SQL Server
        /// </summary>
        private bool IsSqlServer(DbConnection connection)
        {
            return connection.GetType().Name.Contains("SqlConnection", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 计算JOIN数量
        /// </summary>
        private int CountJoins(string sqlQuery)
        {
            if (string.IsNullOrEmpty(sqlQuery))
                return 0;

            var joinKeywords = new[] { " JOIN ", " INNER JOIN ", " LEFT JOIN ", " RIGHT JOIN ", " FULL JOIN " };
            return joinKeywords.Sum(keyword => 
                (sqlQuery.Length - sqlQuery.Replace(keyword, "", StringComparison.OrdinalIgnoreCase).Length) / keyword.Length);
        }

        #endregion

        #region 内部数据类

        /// <summary>
        /// 查询执行数据
        /// </summary>
        private class QueryExecutionData
        {
            public long ExecutionTime { get; set; }
            public int RecordCount { get; set; }
        }

        #endregion
    }
}
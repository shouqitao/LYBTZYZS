using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Text.RegularExpressions;
using LYBT.Infrastructure.Data;

namespace LYBT.Infrastructure.Performance.Database
{
    /// <summary>
    /// 统一数据库优化器实现 - UltraThink性能优化核心
    /// 职责单一：专注数据库性能分析和优化
    /// 代码干净：清晰的错误处理和日志记录
    /// 性能出色：智能查询优化和批处理
    /// </summary>
    public class UnifiedDatabaseOptimizer : IUnifiedDatabaseOptimizer
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UnifiedDatabaseOptimizer> _logger;
        private readonly Dictionary<string, CompiledQuery> _compiledQueryCache = new();
        private readonly object _cacheLock = new object();

        public UnifiedDatabaseOptimizer(AppDbContext context, ILogger<UnifiedDatabaseOptimizer> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

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

                using var command = connection.CreateCommand();
                command.CommandText = "SET STATISTICS IO ON; SET STATISTICS TIME ON;";
                await command.ExecuteNonQueryAsync(cancellationToken);

                // 执行查询
                var startTime = stopwatch.ElapsedMilliseconds;
                var results = await query.ToListAsync(cancellationToken);
                var endTime = stopwatch.ElapsedMilliseconds;

                analysis.ExecutionTimeMs = endTime - startTime;
                analysis.RecordCount = results.Count;

                // 关闭统计信息收集
                command.CommandText = "SET STATISTICS IO OFF; SET STATISTICS TIME OFF;";
                await command.ExecuteNonQueryAsync(cancellationToken);

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
        /// 执行批量操作
        /// </summary>
        public async Task<BatchOperationResult> ExecuteBatchOperationAsync<T>(
            IEnumerable<T> entities, 
            BatchOperationType operationType, 
            CancellationToken cancellationToken = default) where T : class
        {
            ArgumentNullException.ThrowIfNull(entities);

            var entitiesList = entities.ToList();
            var result = new BatchOperationResult();
            var stopwatch = Stopwatch.StartNew();

            if (entitiesList.Count == 0)
            {
                _logger.LogWarning("批量操作：实体列表为空");
                return result;
            }

            try
            {
                _logger.LogInformation("开始批量操作: {OperationType}, 数量: {Count}", operationType, entitiesList.Count);

                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                
                try
                {
                    switch (operationType)
                    {
                        case BatchOperationType.Insert:
                            result = await ExecuteBatchInsertAsync(entitiesList, cancellationToken);
                            break;
                        case BatchOperationType.Update:
                            result = await ExecuteBatchUpdateAsync(entitiesList, cancellationToken);
                            break;
                        case BatchOperationType.Delete:
                            result = await ExecuteBatchDeleteAsync(entitiesList, cancellationToken);
                            break;
                        case BatchOperationType.Upsert:
                            result = await ExecuteBatchUpsertAsync(entitiesList, cancellationToken);
                            break;
                        default:
                            throw new ArgumentException($"不支持的批量操作类型: {operationType}");
                    }

                    await transaction.CommitAsync(cancellationToken);
                    
                    _logger.LogInformation("批量操作完成: {OperationType}, 成功: {Success}, 失败: {Failed}, 耗时: {ElapsedMs}ms", 
                        operationType, result.SuccessCount, result.FailureCount, result.ExecutionTimeMs);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "批量操作事务回滚: {OperationType}", operationType);
                    result.Errors.Add($"事务失败: {ex.Message}");
                    result.FailureCount = entitiesList.Count;
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量操作失败: {OperationType}", operationType);
                result.Errors.Add(ex.Message);
                result.FailureCount = entitiesList.Count;
            }
            finally
            {
                stopwatch.Stop();
                result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            }

            return result;
        }

        /// <summary>
        /// 获取数据库统计信息
        /// </summary>
        public async Task<DatabaseStatistics> GetDatabaseStatisticsAsync(CancellationToken cancellationToken = default)
        {
            var stats = new DatabaseStatistics();

            try
            {
                var connection = _context.Database.GetDbConnection();
                await EnsureConnectionOpenAsync(connection, cancellationToken);

                if (IsSqlServer(connection))
                {
                    stats = await GetSqlServerStatisticsAsync(connection, cancellationToken);
                }
                else
                {
                    // 为其他数据库类型提供基本统计
                    stats.DatabaseSizeMB = await GetDatabaseSizeAsync(connection, cancellationToken);
                }

                _logger.LogDebug("数据库统计信息获取完成: 大小={Size}MB, 活动连接={Connections}", 
                    stats.DatabaseSizeMB, stats.ActiveConnections);

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取数据库统计信息失败");
                return stats;
            }
        }

        /// <summary>
        /// 优化查询 - 自动应用最佳实践
        /// </summary>
        public async Task<IQueryable<T>> OptimizeQueryAsync<T>(
            IQueryable<T> query, 
            QueryOptimizationOptions? options = null) where T : class
        {
            ArgumentNullException.ThrowIfNull(query);
            
            options ??= new QueryOptimizationOptions();
            
            try
            {
                _logger.LogDebug("开始查询优化: {QueryType}", typeof(T).Name);

                // 应用无跟踪查询（适用于只读场景）
                if (options.AsNoTracking)
                {
                    query = query.AsNoTracking();
                }

                // 限制返回记录数
                if (options.MaxRecords.HasValue && options.MaxRecords > 0)
                {
                    query = query.Take(options.MaxRecords.Value);
                }

                // 应用分页
                if (options.Pagination != null)
                {
                    var skip = (options.Pagination.PageIndex - 1) * options.Pagination.PageSize;
                    query = query.Skip(skip).Take(options.Pagination.PageSize);
                }

                _logger.LogDebug("查询优化完成: {QueryType}", typeof(T).Name);
                return await Task.FromResult(query);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询优化失败: {QueryType}", typeof(T).Name);
                return query; // 返回原始查询
            }
        }

        /// <summary>
        /// 预热数据库连接池
        /// </summary>
        public async Task WarmUpConnectionPoolAsync(int connectionCount = 5, CancellationToken cancellationToken = default)
        {
            var tasks = new List<Task>();

            try
            {
                _logger.LogInformation("开始预热数据库连接池: {ConnectionCount}个连接", connectionCount);

                for (int i = 0; i < connectionCount; i++)
                {
                    tasks.Add(WarmUpSingleConnectionAsync(i, cancellationToken));
                }

                await Task.WhenAll(tasks);
                
                _logger.LogInformation("数据库连接池预热完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据库连接池预热失败");
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
            ArgumentException.ThrowIfNullOrEmpty(tableName);

            var recommendations = new List<IndexRecommendation>();

            try
            {
                var connection = _context.Database.GetDbConnection();
                await EnsureConnectionOpenAsync(connection, cancellationToken);

                if (IsSqlServer(connection))
                {
                    recommendations = await GetSqlServerIndexRecommendationsAsync(tableName, connection, cancellationToken);
                }

                _logger.LogInformation("获取索引建议完成: 表={TableName}, 建议数={Count}", tableName, recommendations.Count);
                return recommendations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取索引建议失败: 表={TableName}", tableName);
                return recommendations;
            }
        }

        /// <summary>
        /// 执行数据库维护任务
        /// </summary>
        public async Task<MaintenanceResult> ExecuteMaintenanceAsync(
            MaintenanceOptions options, 
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(options);

            var result = new MaintenanceResult();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("开始数据库维护任务");

                var connection = _context.Database.GetDbConnection();
                await EnsureConnectionOpenAsync(connection, cancellationToken);

                // 记录维护前的数据库大小
                result.DatabaseSizeBeforeMB = await GetDatabaseSizeAsync(connection, cancellationToken);

                if (IsSqlServer(connection))
                {
                    result = await ExecuteSqlServerMaintenanceAsync(options, connection, cancellationToken);
                }

                // 记录维护后的数据库大小
                result.DatabaseSizeAfterMB = await GetDatabaseSizeAsync(connection, cancellationToken);

                _logger.LogInformation("数据库维护完成: 节省空间={SpaceSaved}MB, 耗时={ElapsedMs}ms", 
                    result.SpaceSavedMB, result.TotalExecutionTimeMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "数据库维护失败");
                result.Errors.Add(ex.Message);
            }
            finally
            {
                stopwatch.Stop();
                result.TotalExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            }

            return result;
        }

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
                TimeRange = endTime - startTime
            };

            try
            {
                var connection = _context.Database.GetDbConnection();
                await EnsureConnectionOpenAsync(connection, cancellationToken);

                if (IsSqlServer(connection))
                {
                    report.SlowQueries = await GetSqlServerSlowQueriesAsync(startTime, endTime, connection, cancellationToken);
                }

                _logger.LogInformation("慢查询报告生成完成: 时间范围={TimeRange}, 慢查询数={Count}", 
                    report.TimeRange, report.TotalSlowQueries);

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成慢查询报告失败");
                return report;
            }
        }

        #region 私有辅助方法

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

        /// <summary>
        /// 生成优化建议
        /// </summary>
        private List<string> GenerateOptimizationSuggestions(QueryPerformanceAnalysis analysis)
        {
            var suggestions = new List<string>();

            if (analysis.ExecutionTimeMs > 1000)
            {
                suggestions.Add("查询执行时间超过1秒，建议优化查询条件或添加索引");
            }

            if (analysis.RecordCount > 10000)
            {
                suggestions.Add("返回记录数过多，建议使用分页查询");
            }

            if (analysis.SqlQuery.Contains("SELECT *", StringComparison.OrdinalIgnoreCase))
            {
                suggestions.Add("避免使用SELECT *，建议明确指定需要的列");
            }

            if (!analysis.SqlQuery.Contains("WHERE", StringComparison.OrdinalIgnoreCase))
            {
                suggestions.Add("查询缺少WHERE条件，可能导致全表扫描");
            }

            if (analysis.SqlQuery.Contains("LIKE '%", StringComparison.OrdinalIgnoreCase))
            {
                suggestions.Add("前缀通配符查询无法使用索引，考虑全文搜索或其他方案");
            }

            return suggestions;
        }

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

        /// <summary>
        /// 批量插入
        /// </summary>
        private async Task<BatchOperationResult> ExecuteBatchInsertAsync<T>(
            List<T> entities, 
            CancellationToken cancellationToken) where T : class
        {
            var result = new BatchOperationResult();
            
            try
            {
                const int batchSize = 1000; // 每批处理1000条记录
                
                for (int i = 0; i < entities.Count; i += batchSize)
                {
                    var batch = entities.Skip(i).Take(batchSize).ToList();
                    
                    _context.Set<T>().AddRange(batch);
                    var saved = await _context.SaveChangesAsync(cancellationToken);
                    result.SuccessCount += saved;
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add(ex.Message);
                result.FailureCount = entities.Count - result.SuccessCount;
            }

            return result;
        }

        /// <summary>
        /// 批量更新
        /// </summary>
        private async Task<BatchOperationResult> ExecuteBatchUpdateAsync<T>(
            List<T> entities, 
            CancellationToken cancellationToken) where T : class
        {
            var result = new BatchOperationResult();
            
            try
            {
                _context.Set<T>().UpdateRange(entities);
                var saved = await _context.SaveChangesAsync(cancellationToken);
                result.SuccessCount = saved;
            }
            catch (Exception ex)
            {
                result.Errors.Add(ex.Message);
                result.FailureCount = entities.Count;
            }

            return result;
        }

        /// <summary>
        /// 批量删除
        /// </summary>
        private async Task<BatchOperationResult> ExecuteBatchDeleteAsync<T>(
            List<T> entities, 
            CancellationToken cancellationToken) where T : class
        {
            var result = new BatchOperationResult();
            
            try
            {
                _context.Set<T>().RemoveRange(entities);
                var saved = await _context.SaveChangesAsync(cancellationToken);
                result.SuccessCount = saved;
            }
            catch (Exception ex)
            {
                result.Errors.Add(ex.Message);
                result.FailureCount = entities.Count;
            }

            return result;
        }

        /// <summary>
        /// 批量Upsert（更新或插入）
        /// </summary>
        private async Task<BatchOperationResult> ExecuteBatchUpsertAsync<T>(
            List<T> entities, 
            CancellationToken cancellationToken) where T : class
        {
            // 注意：这是一个简化实现，实际的Upsert操作会更复杂
            var result = new BatchOperationResult();
            
            try
            {
                foreach (var entity in entities)
                {
                    var existingEntity = await _context.Set<T>().FindAsync(new object[] { GetEntityId(entity) }, cancellationToken);
                    
                    if (existingEntity != null)
                    {
                        _context.Entry(existingEntity).CurrentValues.SetValues(entity);
                    }
                    else
                    {
                        _context.Set<T>().Add(entity);
                    }
                }
                
                var saved = await _context.SaveChangesAsync(cancellationToken);
                result.SuccessCount = saved;
            }
            catch (Exception ex)
            {
                result.Errors.Add(ex.Message);
                result.FailureCount = entities.Count;
            }

            return result;
        }

        /// <summary>
        /// 获取实体ID（简化实现）
        /// </summary>
        private object GetEntityId<T>(T entity)
        {
            // 这里应该根据实际的实体类型获取主键值
            // 这是一个简化的实现
            var property = typeof(T).GetProperty("Id");
            return property?.GetValue(entity) ?? Guid.NewGuid();
        }

        /// <summary>
        /// 预热单个连接
        /// </summary>
        private async Task WarmUpSingleConnectionAsync(int connectionIndex, CancellationToken cancellationToken)
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                if (string.IsNullOrEmpty(connectionString))
                    return;
                    
                using var connection = _context.Database.GetDbConnection().GetType().GetConstructor(new[] { typeof(string) })
                    ?.Invoke(new object[] { connectionString }) as DbConnection;
                
                if (connection != null)
                {
                    await connection.OpenAsync(cancellationToken);
                    
                    using var command = connection.CreateCommand();
                    command.CommandText = "SELECT 1";
                    await command.ExecuteScalarAsync(cancellationToken);
                    
                    _logger.LogDebug("连接池预热完成: 连接{Index}", connectionIndex);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "连接池预热失败: 连接{Index}", connectionIndex);
            }
        }

        /// <summary>
        /// 获取数据库大小
        /// </summary>
        private async Task<long> GetDatabaseSizeAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = IsSqlServer(connection) 
                    ? "SELECT SUM(size * 8.0 / 1024) FROM sys.database_files"
                    : "SELECT 0"; // 其他数据库的实现
                
                var result = await command.ExecuteScalarAsync(cancellationToken);
                return Convert.ToInt64(result ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取数据库大小失败");
                return 0;
            }
        }

        /// <summary>
        /// 获取SQL Server统计信息
        /// </summary>
        private async Task<DatabaseStatistics> GetSqlServerStatisticsAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            var stats = new DatabaseStatistics();
            
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT 
                        (SELECT COUNT(*) FROM sys.dm_exec_connections) AS ActiveConnections,
                        (SELECT SUM(size * 8.0 / 1024) FROM sys.database_files WHERE type = 0) AS DatabaseSizeMB,
                        (SELECT SUM(size * 8.0 / 1024) FROM sys.database_files WHERE type = 1) AS LogSizeMB";
                
                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    stats.ActiveConnections = reader.GetInt32("ActiveConnections");
                    stats.DatabaseSizeMB = Convert.ToInt64(reader.GetDouble("DatabaseSizeMB"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取SQL Server统计信息失败");
            }

            return stats;
        }

        /// <summary>
        /// 获取SQL Server索引建议
        /// </summary>
        private async Task<List<IndexRecommendation>> GetSqlServerIndexRecommendationsAsync(
            string tableName, 
            DbConnection connection, 
            CancellationToken cancellationToken)
        {
            var recommendations = new List<IndexRecommendation>();
            
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = $@"
                    SELECT 
                        user_seeks + user_scans AS usage_count,
                        equality_columns,
                        inequality_columns,
                        included_columns,
                        avg_total_user_cost * (user_seeks + user_scans) AS improvement_measure
                    FROM sys.dm_db_missing_index_details d
                    INNER JOIN sys.dm_db_missing_index_groups g ON d.index_handle = g.index_handle
                    INNER JOIN sys.dm_db_missing_index_group_stats s ON g.index_group_handle = s.group_handle
                    WHERE d.object_id = OBJECT_ID('{tableName}')
                    ORDER BY improvement_measure DESC";
                
                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var recommendation = new IndexRecommendation
                    {
                        TableName = tableName,
                        IndexType = IndexType.NonClustered,
                        EstimatedImprovementPercent = Convert.ToDouble(reader["improvement_measure"] ?? 0),
                        Reason = "缺失索引检测器建议"
                    };
                    
                    var equalityColumns = reader["equality_columns"]?.ToString();
                    var inequalityColumns = reader["inequality_columns"]?.ToString();
                    
                    if (!string.IsNullOrEmpty(equalityColumns))
                    {
                        recommendation.Columns.AddRange(equalityColumns.Split(',').Select(c => c.Trim()));
                    }
                    
                    if (!string.IsNullOrEmpty(inequalityColumns))
                    {
                        recommendation.Columns.AddRange(inequalityColumns.Split(',').Select(c => c.Trim()));
                    }
                    
                    recommendations.Add(recommendation);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取SQL Server索引建议失败: {TableName}", tableName);
            }

            return recommendations;
        }

        /// <summary>
        /// 执行SQL Server维护
        /// </summary>
        private async Task<MaintenanceResult> ExecuteSqlServerMaintenanceAsync(
            MaintenanceOptions options, 
            DbConnection connection, 
            CancellationToken cancellationToken)
        {
            var result = new MaintenanceResult();
            
            try
            {
                using var command = connection.CreateCommand();
                
                if (options.UpdateStatistics)
                {
                    command.CommandText = "EXEC sp_updatestats";
                    await command.ExecuteNonQueryAsync(cancellationToken);
                    result.CompletedTasks.Add("更新统计信息");
                }

                if (options.ReorganizeIndexes)
                {
                    command.CommandText = @"
                        DECLARE @SQL NVARCHAR(MAX) = ''
                        SELECT @SQL = @SQL + 'ALTER INDEX ALL ON ' + SCHEMA_NAME(schema_id) + '.' + name + ' REORGANIZE;' + CHAR(13)
                        FROM sys.tables
                        EXEC sp_executesql @SQL";
                    await command.ExecuteNonQueryAsync(cancellationToken);
                    result.CompletedTasks.Add("重新组织索引");
                }

                if (options.RebuildIndexes)
                {
                    command.CommandText = @"
                        DECLARE @SQL NVARCHAR(MAX) = ''
                        SELECT @SQL = @SQL + 'ALTER INDEX ALL ON ' + SCHEMA_NAME(schema_id) + '.' + name + ' REBUILD;' + CHAR(13)
                        FROM sys.tables
                        EXEC sp_executesql @SQL";
                    await command.ExecuteNonQueryAsync(cancellationToken);
                    result.CompletedTasks.Add("重建索引");
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add(ex.Message);
            }

            return result;
        }

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
                    SELECT TOP 50
                        qs.sql_handle,
                        qs.execution_count,
                        qs.total_elapsed_time / 1000 AS total_elapsed_time_ms,
                        qs.total_elapsed_time / qs.execution_count / 1000 AS avg_elapsed_time_ms,
                        qs.last_execution_time,
                        qs.total_worker_time / 1000 AS total_cpu_time_ms,
                        qs.total_logical_reads,
                        qs.total_physical_reads,
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
                        PhysicalReads = Convert.ToInt64(reader["total_physical_reads"])
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

        #endregion
    }

    /// <summary>
    /// 编译查询缓存项
    /// </summary>
    internal class CompiledQuery
    {
        public Func<DbContext, IQueryable> QueryFactory { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan CacheExpiry { get; set; } = TimeSpan.FromMinutes(30);
        
        public bool IsExpired => DateTime.UtcNow - CreatedAt > CacheExpiry;
    }
}
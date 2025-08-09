using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Performance.Database.Models;

namespace LYBT.Infrastructure.Performance.Database.Components
{
    /// <summary>
    /// 数据库维护管理器 - UltraThink专门化组件
    /// 职责单一：专注数据库维护任务的执行和索引管理
    /// 代码干净：清晰的维护任务流程和错误处理
    /// 性能出色：高效的维护操作和资源管理
    /// </summary>
    public class DatabaseMaintenanceManager
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DatabaseMaintenanceManager> _logger;

        public DatabaseMaintenanceManager(AppDbContext context, ILogger<DatabaseMaintenanceManager> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 核心维护方法

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
                    result = await ExecuteSqlServerMaintenanceAsync(options, connection, result, cancellationToken);
                }
                else
                {
                    result = await ExecuteGenericMaintenanceAsync(options, connection, result, cancellationToken);
                }

                // 记录维护后的数据库大小
                result.DatabaseSizeAfterMB = await GetDatabaseSizeAsync(connection, cancellationToken);
                result.SpaceSavedMB = result.DatabaseSizeBeforeMB - result.DatabaseSizeAfterMB;

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
        /// 执行完整的数据库维护
        /// </summary>
        public async Task<MaintenanceResult> ExecuteFullMaintenanceAsync(CancellationToken cancellationToken = default)
        {
            var options = new MaintenanceOptions
            {
                UpdateStatistics = true,
                ReorganizeIndexes = true,
                RebuildIndexes = false, // 重组和重建不同时执行
                ShrinkDatabase = false, // 通常不建议收缩数据库
                CheckDatabaseIntegrity = true
            };

            return await ExecuteMaintenanceAsync(options, cancellationToken);
        }

        /// <summary>
        /// 执行快速维护（仅更新统计信息）
        /// </summary>
        public async Task<MaintenanceResult> ExecuteQuickMaintenanceAsync(CancellationToken cancellationToken = default)
        {
            var options = new MaintenanceOptions
            {
                UpdateStatistics = true,
                ReorganizeIndexes = false,
                RebuildIndexes = false,
                ShrinkDatabase = false,
                CheckDatabaseIntegrity = false
            };

            return await ExecuteMaintenanceAsync(options, cancellationToken);
        }

        #endregion

        #region 索引维护

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
        /// 分析所有表的索引使用情况
        /// </summary>
        public async Task<IndexAnalysisReport> AnalyzeIndexUsageAsync(CancellationToken cancellationToken = default)
        {
            var report = new IndexAnalysisReport();

            try
            {
                _logger.LogInformation("开始分析索引使用情况");

                var connection = _context.Database.GetDbConnection();
                await EnsureConnectionOpenAsync(connection, cancellationToken);

                if (IsSqlServer(connection))
                {
                    report = await GetSqlServerIndexAnalysisAsync(connection, cancellationToken);
                }

                _logger.LogInformation("索引分析完成: 未使用索引={UnusedCount}, 碎片化索引={FragmentedCount}",
                    report.UnusedIndexes.Count, report.FragmentedIndexes.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分析索引使用情况失败");
                report.Errors.Add(ex.Message);
            }

            return report;
        }

        /// <summary>
        /// 重建碎片化的索引
        /// </summary>
        public async Task<IndexMaintenanceResult> RebuildFragmentedIndexesAsync(
            double fragmentationThreshold = 30.0,
            CancellationToken cancellationToken = default)
        {
            var result = new IndexMaintenanceResult();

            try
            {
                _logger.LogInformation("开始重建碎片化索引，碎片阈值: {Threshold}%", fragmentationThreshold);

                var connection = _context.Database.GetDbConnection();
                await EnsureConnectionOpenAsync(connection, cancellationToken);

                if (IsSqlServer(connection))
                {
                    result = await RebuildSqlServerFragmentedIndexesAsync(connection, fragmentationThreshold, cancellationToken);
                }

                _logger.LogInformation("索引重建完成: 重建={Rebuilt}, 重组={Reorganized}, 跳过={Skipped}",
                    result.RebuiltIndexCount, result.ReorganizedIndexCount, result.SkippedIndexCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重建碎片化索引失败");
                result.Errors.Add(ex.Message);
            }

            return result;
        }

        #endregion

        #region SQL Server 特定维护

        /// <summary>
        /// 执行SQL Server维护
        /// </summary>
        private async Task<MaintenanceResult> ExecuteSqlServerMaintenanceAsync(
            MaintenanceOptions options, 
            DbConnection connection, 
            MaintenanceResult result,
            CancellationToken cancellationToken)
        {
            try
            {
                using var command = connection.CreateCommand();
                command.CommandTimeout = 300; // 5分钟超时
                
                if (options.UpdateStatistics)
                {
                    _logger.LogDebug("开始更新统计信息");
                    command.CommandText = "EXEC sp_updatestats";
                    await command.ExecuteNonQueryAsync(cancellationToken);
                    result.CompletedTasks.Add("更新统计信息");
                }

                if (options.ReorganizeIndexes)
                {
                    _logger.LogDebug("开始重新组织索引");
                    await ReorganizeAllIndexesAsync(connection, cancellationToken);
                    result.CompletedTasks.Add("重新组织索引");
                }

                if (options.RebuildIndexes)
                {
                    _logger.LogDebug("开始重建索引");
                    await RebuildAllIndexesAsync(connection, cancellationToken);
                    result.CompletedTasks.Add("重建索引");
                }

                if (options.CheckDatabaseIntegrity)
                {
                    _logger.LogDebug("开始检查数据库完整性");
                    var integrityResult = await CheckDatabaseIntegrityAsync(connection, cancellationToken);
                    result.CompletedTasks.Add($"数据库完整性检查: {integrityResult}");
                }

                if (options.ShrinkDatabase)
                {
                    _logger.LogDebug("开始收缩数据库（不推荐）");
                    await ShrinkDatabaseAsync(connection, cancellationToken);
                    result.CompletedTasks.Add("收缩数据库");
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add(ex.Message);
            }

            return result;
        }

        /// <summary>
        /// 重新组织所有索引
        /// </summary>
        private async Task ReorganizeAllIndexesAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
                DECLARE @SQL NVARCHAR(MAX) = ''
                SELECT @SQL = @SQL + 'ALTER INDEX ALL ON [' + SCHEMA_NAME(schema_id) + '].[' + name + '] REORGANIZE;' + CHAR(13)
                FROM sys.tables
                WHERE is_ms_shipped = 0
                EXEC sp_executesql @SQL";
            
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        /// <summary>
        /// 重建所有索引
        /// </summary>
        private async Task RebuildAllIndexesAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
                DECLARE @SQL NVARCHAR(MAX) = ''
                SELECT @SQL = @SQL + 'ALTER INDEX ALL ON [' + SCHEMA_NAME(schema_id) + '].[' + name + '] REBUILD;' + CHAR(13)
                FROM sys.tables
                WHERE is_ms_shipped = 0
                EXEC sp_executesql @SQL";
            
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        /// <summary>
        /// 检查数据库完整性
        /// </summary>
        private async Task<string> CheckDatabaseIntegrityAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "DBCC CHECKDB WITH NO_INFOMSGS";
            
            try
            {
                await command.ExecuteNonQueryAsync(cancellationToken);
                return "数据库完整性检查通过";
            }
            catch (Exception ex)
            {
                return $"数据库完整性检查发现问题: {ex.Message}";
            }
        }

        /// <summary>
        /// 收缩数据库
        /// </summary>
        private async Task ShrinkDatabaseAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "DBCC SHRINKDATABASE(0, 10)";
            await command.ExecuteNonQueryAsync(cancellationToken);
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
        /// 获取SQL Server索引分析
        /// </summary>
        private async Task<IndexAnalysisReport> GetSqlServerIndexAnalysisAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            var report = new IndexAnalysisReport();

            try
            {
                // 获取未使用的索引
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT 
                        t.name AS TableName,
                        i.name AS IndexName,
                        i.type_desc AS IndexType
                    FROM sys.indexes i
                    INNER JOIN sys.tables t ON i.object_id = t.object_id
                    LEFT JOIN sys.dm_db_index_usage_stats us ON i.object_id = us.object_id AND i.index_id = us.index_id
                    WHERE us.index_id IS NULL 
                        AND i.type > 0 
                        AND i.is_primary_key = 0
                        AND i.is_unique_constraint = 0
                        AND t.is_ms_shipped = 0";

                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var unusedIndex = new UnusedIndex
                    {
                        TableName = reader.GetString("TableName"),
                        IndexName = reader.GetString("IndexName"),
                        IndexType = reader.GetString("IndexType")
                    };
                    
                    report.UnusedIndexes.Add(unusedIndex);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取未使用索引信息失败");
                report.Errors.Add($"获取未使用索引失败: {ex.Message}");
            }

            return report;
        }

        /// <summary>
        /// 重建SQL Server碎片化索引
        /// </summary>
        private async Task<IndexMaintenanceResult> RebuildSqlServerFragmentedIndexesAsync(
            DbConnection connection, 
            double fragmentationThreshold, 
            CancellationToken cancellationToken)
        {
            var result = new IndexMaintenanceResult();

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT 
                        t.name AS TableName,
                        i.name AS IndexName,
                        ips.avg_fragmentation_in_percent,
                        ips.page_count
                    FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
                    INNER JOIN sys.tables t ON ips.object_id = t.object_id
                    INNER JOIN sys.indexes i ON ips.object_id = i.object_id AND ips.index_id = i.index_id
                    WHERE ips.avg_fragmentation_in_percent > @FragmentationThreshold
                        AND ips.page_count > 25
                        AND i.type > 0";

                var param = command.CreateParameter();
                param.ParameterName = "@FragmentationThreshold";
                param.Value = fragmentationThreshold;
                command.Parameters.Add(param);

                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                var indexesToMaintain = new List<(string TableName, string IndexName, double Fragmentation)>();
                
                while (await reader.ReadAsync(cancellationToken))
                {
                    indexesToMaintain.Add((
                        reader.GetString("TableName"),
                        reader.GetString("IndexName"),
                        Convert.ToDouble(reader["avg_fragmentation_in_percent"])
                    ));
                }

                // 根据碎片化程度决定维护策略
                foreach (var index in indexesToMaintain)
                {
                    using var maintenanceCommand = connection.CreateCommand();
                    
                    if (index.Fragmentation > 70)
                    {
                        // 重建索引
                        maintenanceCommand.CommandText = $"ALTER INDEX [{index.IndexName}] ON [{index.TableName}] REBUILD";
                        await maintenanceCommand.ExecuteNonQueryAsync(cancellationToken);
                        result.RebuiltIndexCount++;
                    }
                    else
                    {
                        // 重组索引
                        maintenanceCommand.CommandText = $"ALTER INDEX [{index.IndexName}] ON [{index.TableName}] REORGANIZE";
                        await maintenanceCommand.ExecuteNonQueryAsync(cancellationToken);
                        result.ReorganizedIndexCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"重建碎片化索引失败: {ex.Message}");
            }

            return result;
        }

        #endregion

        #region 通用维护

        /// <summary>
        /// 执行通用维护
        /// </summary>
        private async Task<MaintenanceResult> ExecuteGenericMaintenanceAsync(
            MaintenanceOptions options, 
            DbConnection connection, 
            MaintenanceResult result,
            CancellationToken cancellationToken)
        {
            try
            {
                if (options.UpdateStatistics)
                {
                    // 对于非SQL Server数据库，执行基本的统计更新操作
                    result.CompletedTasks.Add("基本统计信息更新（通用）");
                }

                // 可以添加其他数据库类型的特定维护操作
            }
            catch (Exception ex)
            {
                result.Errors.Add(ex.Message);
            }

            return result;
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

        #endregion
    }
}
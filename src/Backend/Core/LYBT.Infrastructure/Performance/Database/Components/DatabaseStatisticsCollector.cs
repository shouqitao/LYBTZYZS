using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Performance.Database.Models;

namespace LYBT.Infrastructure.Performance.Database.Components
{
    /// <summary>
    /// 数据库统计信息收集器 - UltraThink专门化组件
    /// 职责单一：专注数据库性能统计信息的收集和分析
    /// 代码干净：清晰的统计收集逻辑和数据处理
    /// 性能出色：高效的统计查询和数据聚合算法
    /// </summary>
    public class DatabaseStatisticsCollector
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DatabaseStatisticsCollector> _logger;
        private readonly Dictionary<string, object> _statisticsCache = new();
        private DateTime _lastCacheUpdate = DateTime.MinValue;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

        public DatabaseStatisticsCollector(AppDbContext context, ILogger<DatabaseStatisticsCollector> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 核心统计收集方法

        /// <summary>
        /// 获取数据库统计信息
        /// </summary>
        public async Task<DatabaseStatistics> GetDatabaseStatisticsAsync(CancellationToken cancellationToken = default)
        {
            var stats = new DatabaseStatistics();

            try
            {
                _logger.LogDebug("开始收集数据库统计信息");

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
                    stats.TableCount = await GetTableCountAsync(connection, cancellationToken);
                }

                // 获取通用统计信息
                await EnrichWithCommonStatisticsAsync(stats, connection, cancellationToken);

                _logger.LogInformation("数据库统计信息收集完成: 大小={Size}MB, 活动连接={Connections}, 表数={Tables}", 
                    stats.DatabaseSizeMB, stats.ActiveConnections, stats.TableCount);

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取数据库统计信息失败");
                return stats;
            }
        }

        /// <summary>
        /// 获取详细的数据库统计信息
        /// </summary>
        public async Task<DetailedDatabaseStatistics> GetDetailedDatabaseStatisticsAsync(CancellationToken cancellationToken = default)
        {
            var detailedStats = new DetailedDatabaseStatistics();

            try
            {
                _logger.LogDebug("开始收集详细数据库统计信息");

                var connection = _context.Database.GetDbConnection();
                await EnsureConnectionOpenAsync(connection, cancellationToken);

                // 获取基本统计信息
                var basicStats = await GetDatabaseStatisticsAsync(cancellationToken);
                detailedStats.BasicStatistics = basicStats;

                // 获取表级统计信息
                detailedStats.TableStatistics = await GetTableStatisticsAsync(connection, cancellationToken);

                // 获取索引统计信息
                detailedStats.IndexStatistics = await GetIndexStatisticsAsync(connection, cancellationToken);

                // 获取连接统计信息
                detailedStats.ConnectionStatistics = await GetConnectionStatisticsAsync(connection, cancellationToken);

                // 获取性能计数器
                detailedStats.PerformanceCounters = await GetPerformanceCountersAsync(connection, cancellationToken);

                _logger.LogInformation("详细数据库统计信息收集完成");
                return detailedStats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取详细数据库统计信息失败");
                return detailedStats;
            }
        }

        /// <summary>
        /// 获取实时性能指标
        /// </summary>
        public async Task<RealTimePerformanceMetrics> GetRealTimePerformanceMetricsAsync(CancellationToken cancellationToken = default)
        {
            var metrics = new RealTimePerformanceMetrics();

            try
            {
                var connection = _context.Database.GetDbConnection();
                await EnsureConnectionOpenAsync(connection, cancellationToken);

                if (IsSqlServer(connection))
                {
                    metrics = await GetSqlServerRealTimeMetricsAsync(connection, cancellationToken);
                }

                metrics.CollectionTime = DateTime.UtcNow;
                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取实时性能指标失败");
                return metrics;
            }
        }

        #endregion

        #region SQL Server 特定统计

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
                        (SELECT SUM(size * 8.0 / 1024) FROM sys.database_files WHERE type = 1) AS LogSizeMB,
                        (SELECT COUNT(*) FROM sys.tables WHERE type = 'U') AS TableCount,
                        (SELECT COUNT(*) FROM sys.indexes WHERE type > 0) AS IndexCount";
                
                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    stats.ActiveConnections = reader.GetInt32("ActiveConnections");
                    stats.DatabaseSizeMB = Convert.ToInt64(reader.GetDouble("DatabaseSizeMB"));
                    stats.LogSizeMB = Convert.ToInt64(reader.GetDouble("LogSizeMB"));
                    stats.TableCount = reader.GetInt32("TableCount");
                    stats.IndexCount = reader.GetInt32("IndexCount");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取SQL Server统计信息失败");
            }

            return stats;
        }

        /// <summary>
        /// 获取SQL Server实时性能指标
        /// </summary>
        private async Task<RealTimePerformanceMetrics> GetSqlServerRealTimeMetricsAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            var metrics = new RealTimePerformanceMetrics();

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT 
                        cntr_value AS BatchRequestsPerSec
                    FROM sys.dm_os_performance_counters 
                    WHERE counter_name = 'Batch Requests/sec';

                    SELECT 
                        cntr_value AS PageReadsPerSec
                    FROM sys.dm_os_performance_counters 
                    WHERE counter_name = 'Page reads/sec';

                    SELECT 
                        cntr_value AS PageWritesPerSec
                    FROM sys.dm_os_performance_counters 
                    WHERE counter_name = 'Page writes/sec';";

                using var reader = await command.ExecuteReaderAsync(cancellationToken);
                
                // 读取批处理请求
                if (await reader.ReadAsync(cancellationToken))
                {
                    metrics.BatchRequestsPerSecond = Convert.ToDouble(reader["BatchRequestsPerSec"]);
                }

                // 读取页面读取
                if (await reader.NextResultAsync(cancellationToken) && await reader.ReadAsync(cancellationToken))
                {
                    metrics.PageReadsPerSecond = Convert.ToDouble(reader["PageReadsPerSec"]);
                }

                // 读取页面写入
                if (await reader.NextResultAsync(cancellationToken) && await reader.ReadAsync(cancellationToken))
                {
                    metrics.PageWritesPerSecond = Convert.ToDouble(reader["PageWritesPerSec"]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取SQL Server实时指标失败");
            }

            return metrics;
        }

        #endregion

        #region 表和索引统计

        /// <summary>
        /// 获取表统计信息
        /// </summary>
        private async Task<List<TableStatistics>> GetTableStatisticsAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            var tableStats = new List<TableStatistics>();

            try
            {
                if (IsSqlServer(connection))
                {
                    using var command = connection.CreateCommand();
                    command.CommandText = @"
                        SELECT 
                            t.name AS TableName,
                            p.rows AS RowCount,
                            SUM(au.total_pages) * 8 / 1024 AS SizeMB,
                            SUM(au.used_pages) * 8 / 1024 AS UsedSizeMB
                        FROM sys.tables t
                        INNER JOIN sys.indexes i ON t.object_id = i.object_id
                        INNER JOIN sys.partitions p ON i.object_id = p.object_id AND i.index_id = p.index_id
                        INNER JOIN sys.allocation_units au ON p.partition_id = au.container_id
                        WHERE t.type = 'U'
                        GROUP BY t.name, p.rows
                        ORDER BY SUM(au.total_pages) DESC";

                    using var reader = await command.ExecuteReaderAsync(cancellationToken);
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        var tableStat = new TableStatistics
                        {
                            TableName = reader.GetString("TableName"),
                            RowCount = Convert.ToInt64(reader["RowCount"]),
                            SizeMB = Convert.ToDecimal(reader["SizeMB"]),
                            UsedSizeMB = Convert.ToDecimal(reader["UsedSizeMB"])
                        };

                        tableStats.Add(tableStat);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取表统计信息失败");
            }

            return tableStats;
        }

        /// <summary>
        /// 获取索引统计信息
        /// </summary>
        private async Task<List<IndexStatistics>> GetIndexStatisticsAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            var indexStats = new List<IndexStatistics>();

            try
            {
                if (IsSqlServer(connection))
                {
                    using var command = connection.CreateCommand();
                    command.CommandText = @"
                        SELECT 
                            t.name AS TableName,
                            i.name AS IndexName,
                            i.type_desc AS IndexType,
                            us.user_seeks,
                            us.user_scans,
                            us.user_lookups,
                            us.user_updates,
                            us.last_user_seek,
                            us.last_user_scan
                        FROM sys.indexes i
                        INNER JOIN sys.tables t ON i.object_id = t.object_id
                        LEFT JOIN sys.dm_db_index_usage_stats us ON i.object_id = us.object_id AND i.index_id = us.index_id
                        WHERE t.type = 'U' AND i.type > 0
                        ORDER BY t.name, i.name";

                    using var reader = await command.ExecuteReaderAsync(cancellationToken);
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        var indexStat = new IndexStatistics
                        {
                            TableName = reader.GetString("TableName"),
                            IndexName = reader.IsDBNull("IndexName") ? "N/A" : reader.GetString("IndexName"),
                            IndexType = reader.GetString("IndexType"),
                            UserSeeks = reader.IsDBNull("user_seeks") ? 0 : Convert.ToInt64(reader["user_seeks"]),
                            UserScans = reader.IsDBNull("user_scans") ? 0 : Convert.ToInt64(reader["user_scans"]),
                            UserLookups = reader.IsDBNull("user_lookups") ? 0 : Convert.ToInt64(reader["user_lookups"]),
                            UserUpdates = reader.IsDBNull("user_updates") ? 0 : Convert.ToInt64(reader["user_updates"]),
                            LastUserSeek = reader.IsDBNull("last_user_seek") ? null : reader.GetDateTime("last_user_seek"),
                            LastUserScan = reader.IsDBNull("last_user_scan") ? null : reader.GetDateTime("last_user_scan")
                        };

                        indexStats.Add(indexStat);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取索引统计信息失败");
            }

            return indexStats;
        }

        #endregion

        #region 连接和性能统计

        /// <summary>
        /// 获取连接统计信息
        /// </summary>
        private async Task<ConnectionStatistics> GetConnectionStatisticsAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            var connStats = new ConnectionStatistics();

            try
            {
                if (IsSqlServer(connection))
                {
                    using var command = connection.CreateCommand();
                    command.CommandText = @"
                        SELECT 
                            COUNT(*) AS TotalConnections,
                            SUM(CASE WHEN status = 'sleeping' THEN 1 ELSE 0 END) AS SleepingConnections,
                            SUM(CASE WHEN status = 'running' THEN 1 ELSE 0 END) AS RunningConnections
                        FROM sys.dm_exec_sessions
                        WHERE is_user_process = 1";

                    using var reader = await command.ExecuteReaderAsync(cancellationToken);
                    if (await reader.ReadAsync(cancellationToken))
                    {
                        connStats.TotalConnections = reader.GetInt32("TotalConnections");
                        connStats.SleepingConnections = reader.GetInt32("SleepingConnections");
                        connStats.RunningConnections = reader.GetInt32("RunningConnections");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取连接统计信息失败");
            }

            return connStats;
        }

        /// <summary>
        /// 获取性能计数器
        /// </summary>
        private async Task<Dictionary<string, object>> GetPerformanceCountersAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            var counters = new Dictionary<string, object>();

            try
            {
                if (IsSqlServer(connection))
                {
                    using var command = connection.CreateCommand();
                    command.CommandText = @"
                        SELECT 
                            counter_name,
                            cntr_value
                        FROM sys.dm_os_performance_counters
                        WHERE counter_name IN (
                            'Buffer cache hit ratio',
                            'Page life expectancy',
                            'Lazy writes/sec',
                            'Checkpoint pages/sec',
                            'Free Memory (KB)'
                        )";

                    using var reader = await command.ExecuteReaderAsync(cancellationToken);
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        var counterName = reader.GetString("counter_name");
                        var counterValue = reader.GetValue("cntr_value");
                        counters[counterName] = counterValue;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取性能计数器失败");
            }

            return counters;
        }

        #endregion

        #region 缓存管理

        /// <summary>
        /// 获取缓存的统计信息
        /// </summary>
        public async Task<T?> GetCachedStatisticsAsync<T>(string key, Func<Task<T>> statisticsProvider) where T : class
        {
            if (IsCacheValid(key))
            {
                if (_statisticsCache.TryGetValue(key, out var cachedValue) && cachedValue is T typedValue)
                {
                    _logger.LogDebug("返回缓存的统计信息: {Key}", key);
                    return typedValue;
                }
            }

            try
            {
                var statistics = await statisticsProvider();
                _statisticsCache[key] = statistics!;
                _lastCacheUpdate = DateTime.UtcNow;
                
                _logger.LogDebug("更新统计信息缓存: {Key}", key);
                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取统计信息失败: {Key}", key);
                return null;
            }
        }

        /// <summary>
        /// 检查缓存是否有效
        /// </summary>
        private bool IsCacheValid(string key)
        {
            return _statisticsCache.ContainsKey(key) && 
                   DateTime.UtcNow - _lastCacheUpdate < _cacheExpiration;
        }

        /// <summary>
        /// 清除统计缓存
        /// </summary>
        public void ClearCache()
        {
            _statisticsCache.Clear();
            _lastCacheUpdate = DateTime.MinValue;
            _logger.LogDebug("统计信息缓存已清除");
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

        /// <summary>
        /// 获取表数量
        /// </summary>
        private async Task<int> GetTableCountAsync(DbConnection connection, CancellationToken cancellationToken)
        {
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = IsSqlServer(connection)
                    ? "SELECT COUNT(*) FROM sys.tables WHERE type = 'U'"
                    : "SELECT COUNT(*) FROM information_schema.tables WHERE table_type = 'BASE TABLE'";
                
                var result = await command.ExecuteScalarAsync(cancellationToken);
                return Convert.ToInt32(result ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "获取表数量失败");
                return 0;
            }
        }

        /// <summary>
        /// 使用通用信息丰富统计数据
        /// </summary>
        private async Task EnrichWithCommonStatisticsAsync(DatabaseStatistics stats, DbConnection connection, CancellationToken cancellationToken)
        {
            try
            {
                if (stats.TableCount == 0)
                {
                    stats.TableCount = await GetTableCountAsync(connection, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "丰富统计信息失败");
            }
        }

        #endregion
    }
}
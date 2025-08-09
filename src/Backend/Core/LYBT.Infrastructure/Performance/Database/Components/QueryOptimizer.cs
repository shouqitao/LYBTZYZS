using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Performance.Database.Models;

namespace LYBT.Infrastructure.Performance.Database.Components
{
    /// <summary>
    /// 查询优化器 - UltraThink专门化组件
    /// 职责单一：专注查询性能优化和最佳实践应用
    /// 代码干净：清晰的优化策略和配置管理
    /// 性能出色：智能的查询优化和连接池管理
    /// </summary>
    public class QueryOptimizer
    {
        private readonly AppDbContext _context;
        private readonly ILogger<QueryOptimizer> _logger;
        private readonly Dictionary<string, CompiledQuery> _compiledQueryCache = new();
        private readonly object _cacheLock = new object();

        public QueryOptimizer(AppDbContext context, ILogger<QueryOptimizer> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 核心优化方法

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

                var optimizedQuery = query;

                // 应用无跟踪查询（适用于只读场景）
                if (options.AsNoTracking)
                {
                    optimizedQuery = optimizedQuery.AsNoTracking();
                    _logger.LogDebug("应用无跟踪查询优化");
                }

                // 应用分页优化
                if (options.Pagination != null)
                {
                    optimizedQuery = ApplyPagination(optimizedQuery, options.Pagination);
                    _logger.LogDebug("应用分页优化: 页码{Page}, 页大小{Size}", 
                        options.Pagination.PageIndex, options.Pagination.PageSize);
                }

                // 限制返回记录数
                if (options.MaxRecords.HasValue && options.MaxRecords > 0)
                {
                    optimizedQuery = optimizedQuery.Take(options.MaxRecords.Value);
                    _logger.LogDebug("应用记录数限制: {MaxRecords}", options.MaxRecords.Value);
                }

                // 应用预加载优化
                if (options.IncludeProperties?.Any() == true)
                {
                    optimizedQuery = ApplyIncludes(optimizedQuery, options.IncludeProperties);
                    _logger.LogDebug("应用预加载优化: {Properties}", string.Join(", ", options.IncludeProperties));
                }

                // 应用查询分割优化（对于复杂查询）
                if (options.SplitQuery)
                {
                    optimizedQuery = optimizedQuery.AsSplitQuery();
                    _logger.LogDebug("应用查询分割优化");
                }

                // 应用查询标签（用于调试）
                if (!string.IsNullOrEmpty(options.QueryTag))
                {
                    optimizedQuery = optimizedQuery.TagWith(options.QueryTag);
                    _logger.LogDebug("应用查询标签: {Tag}", options.QueryTag);
                }

                _logger.LogDebug("查询优化完成: {QueryType}", typeof(T).Name);
                return await Task.FromResult(optimizedQuery);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询优化失败: {QueryType}", typeof(T).Name);
                return query; // 返回原始查询
            }
        }

        /// <summary>
        /// 批量优化查询
        /// </summary>
        public async Task<List<IQueryable<T>>> OptimizeBatchQueriesAsync<T>(
            IEnumerable<IQueryable<T>> queries,
            QueryOptimizationOptions options,
            CancellationToken cancellationToken = default) where T : class
        {
            var queriesList = queries.ToList();
            var optimizedQueries = new List<IQueryable<T>>();

            try
            {
                _logger.LogInformation("开始批量查询优化，查询数量: {Count}", queriesList.Count);

                foreach (var query in queriesList)
                {
                    var optimizedQuery = await OptimizeQueryAsync(query, options);
                    optimizedQueries.Add(optimizedQuery);
                }

                _logger.LogInformation("批量查询优化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量查询优化失败");
            }

            return optimizedQueries;
        }

        #endregion

        #region 编译查询缓存

        /// <summary>
        /// 获取或创建编译查询
        /// </summary>
        public Func<DbContext, IQueryable<T>> GetOrCreateCompiledQuery<T>(
            string queryKey,
            Func<DbContext, IQueryable<T>> queryFactory) where T : class
        {
            lock (_cacheLock)
            {
                if (_compiledQueryCache.TryGetValue(queryKey, out var cachedQuery))
                {
                    _logger.LogDebug("返回缓存的编译查询: {QueryKey}", queryKey);
                    return (Func<DbContext, IQueryable<T>>)cachedQuery.QueryFactory;
                }

                try
                {
                    // 创建编译查询
                    var compiledQuery = EF.CompileQuery(queryFactory);
                    
                    var cacheItem = new CompiledQuery
                    {
                        QueryFactory = ctx => compiledQuery((DbContext)ctx)
                    };
                    
                    _compiledQueryCache[queryKey] = cacheItem;
                    
                    _logger.LogDebug("创建并缓存编译查询: {QueryKey}", queryKey);
                    return compiledQuery;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "创建编译查询失败: {QueryKey}", queryKey);
                    return queryFactory; // 返回原始查询工厂
                }
            }
        }

        /// <summary>
        /// 清除编译查询缓存
        /// </summary>
        public void ClearCompiledQueryCache()
        {
            lock (_cacheLock)
            {
                var count = _compiledQueryCache.Count;
                _compiledQueryCache.Clear();
                _logger.LogInformation("已清除 {Count} 个编译查询缓存", count);
            }
        }

        #endregion

        #region 连接池优化

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
        /// 优化连接池配置
        /// </summary>
        public async Task<ConnectionPoolOptimizationResult> OptimizeConnectionPoolAsync(
            ConnectionPoolOptimizationOptions options,
            CancellationToken cancellationToken = default)
        {
            var result = new ConnectionPoolOptimizationResult();

            try
            {
                _logger.LogInformation("开始连接池优化");

                // 测试不同的连接池大小
                var testSizes = new[] { 5, 10, 20, 30 };
                var testResults = new Dictionary<int, double>();

                foreach (var poolSize in testSizes)
                {
                    var performanceScore = await TestConnectionPoolPerformanceAsync(poolSize, cancellationToken);
                    testResults[poolSize] = performanceScore;
                }

                // 找到最优的连接池大小
                var optimalSize = testResults.OrderByDescending(kvp => kvp.Value).First();
                
                result.OptimalPoolSize = optimalSize.Key;
                result.PerformanceScore = optimalSize.Value;
                result.TestResults = testResults;
                result.IsOptimized = true;

                _logger.LogInformation("连接池优化完成: 最优大小={OptimalSize}, 性能评分={Score:F2}",
                    result.OptimalPoolSize, result.PerformanceScore);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "连接池优化失败");
                result.IsOptimized = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        #endregion

        #region 查询分析和建议

        /// <summary>
        /// 分析查询并提供优化建议
        /// </summary>
        public async Task<QueryOptimizationSuggestions> AnalyzeQueryForOptimizationAsync<T>(
            IQueryable<T> query,
            CancellationToken cancellationToken = default) where T : class
        {
            var suggestions = new QueryOptimizationSuggestions();

            try
            {
                _logger.LogDebug("开始分析查询优化建议: {QueryType}", typeof(T).Name);

                var sqlQuery = query.ToQueryString();
                suggestions.SqlQuery = sqlQuery;

                // 分析查询复杂度
                suggestions.ComplexityScore = AnalyzeQueryComplexity(sqlQuery);

                // 检查是否使用了跟踪
                if (!IsNoTrackingQuery(query))
                {
                    suggestions.Recommendations.Add("建议使用AsNoTracking()提高只读查询性能");
                }

                // 检查是否需要分页
                if (!HasLimitClause(sqlQuery))
                {
                    suggestions.Recommendations.Add("建议添加分页或记录数限制避免返回过多数据");
                }

                // 检查连接数量
                var joinCount = CountJoins(sqlQuery);
                if (joinCount > 3)
                {
                    suggestions.Recommendations.Add($"查询包含{joinCount}个JOIN，考虑使用AsSplitQuery()优化");
                }

                // 分析预加载
                var includeAnalysis = AnalyzeIncludeUsage(query);
                suggestions.Recommendations.AddRange(includeAnalysis);

                // 估算性能等级
                suggestions.EstimatedPerformanceLevel = EstimatePerformanceLevel(suggestions.ComplexityScore, joinCount);

                _logger.LogDebug("查询优化建议分析完成: 复杂度={Complexity}, 建议数={Count}",
                    suggestions.ComplexityScore, suggestions.Recommendations.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分析查询优化建议失败");
                suggestions.Recommendations.Add($"分析过程中发生错误: {ex.Message}");
            }

            return suggestions;
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 应用分页
        /// </summary>
        private IQueryable<T> ApplyPagination<T>(IQueryable<T> query, PaginationOptions pagination)
        {
            var skip = (pagination.PageIndex - 1) * pagination.PageSize;
            return query.Skip(skip).Take(pagination.PageSize);
        }

        /// <summary>
        /// 应用包含属性
        /// </summary>
        private IQueryable<T> ApplyIncludes<T>(IQueryable<T> query, IEnumerable<string> includeProperties) where T : class
        {
            return includeProperties.Aggregate(query, (current, includeProperty) => 
                current.Include(includeProperty));
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
        /// 测试连接池性能
        /// </summary>
        private async Task<double> TestConnectionPoolPerformanceAsync(int poolSize, CancellationToken cancellationToken)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var tasks = new List<Task>();

                // 创建并发任务测试连接池
                for (int i = 0; i < poolSize; i++)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        using var connection = _context.Database.GetDbConnection().GetType()
                            .GetConstructor(new[] { typeof(string) })
                            ?.Invoke(new object[] { _context.Database.GetConnectionString()! }) as DbConnection;
                        
                        if (connection != null)
                        {
                            await connection.OpenAsync(cancellationToken);
                            using var command = connection.CreateCommand();
                            command.CommandText = "SELECT GETDATE()";
                            await command.ExecuteScalarAsync(cancellationToken);
                        }
                    }, cancellationToken));
                }

                await Task.WhenAll(tasks);
                var endTime = DateTime.UtcNow;

                // 计算性能评分（基于时间）
                var executionTimeMs = (endTime - startTime).TotalMilliseconds;
                return Math.Max(0, 1000 - executionTimeMs); // 执行时间越短，评分越高
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "测试连接池性能失败: 池大小{PoolSize}", poolSize);
                return 0;
            }
        }

        /// <summary>
        /// 分析查询复杂度
        /// </summary>
        private int AnalyzeQueryComplexity(string sqlQuery)
        {
            var complexity = 0;

            if (string.IsNullOrEmpty(sqlQuery))
                return complexity;

            // 基础复杂度评分
            complexity += CountJoins(sqlQuery) * 10;
            complexity += CountSubqueries(sqlQuery) * 15;
            complexity += CountOrderBy(sqlQuery) * 5;
            complexity += CountGroupBy(sqlQuery) * 8;

            return complexity;
        }

        /// <summary>
        /// 检查是否为无跟踪查询
        /// </summary>
        private bool IsNoTrackingQuery<T>(IQueryable<T> query)
        {
            // 简化实现：检查查询表达式
            return query.ToString()?.Contains("AsNoTracking") == true;
        }

        /// <summary>
        /// 检查是否有LIMIT子句
        /// </summary>
        private bool HasLimitClause(string sqlQuery)
        {
            return sqlQuery.Contains("TOP ", StringComparison.OrdinalIgnoreCase) ||
                   sqlQuery.Contains("LIMIT ", StringComparison.OrdinalIgnoreCase);
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

        /// <summary>
        /// 计算子查询数量
        /// </summary>
        private int CountSubqueries(string sqlQuery)
        {
            if (string.IsNullOrEmpty(sqlQuery))
                return 0;

            return sqlQuery.Split('(').Length - 1;
        }

        /// <summary>
        /// 计算ORDER BY数量
        /// </summary>
        private int CountOrderBy(string sqlQuery)
        {
            if (string.IsNullOrEmpty(sqlQuery))
                return 0;

            return sqlQuery.Split(new[] { "ORDER BY" }, StringSplitOptions.None).Length - 1;
        }

        /// <summary>
        /// 计算GROUP BY数量
        /// </summary>
        private int CountGroupBy(string sqlQuery)
        {
            if (string.IsNullOrEmpty(sqlQuery))
                return 0;

            return sqlQuery.Split(new[] { "GROUP BY" }, StringSplitOptions.None).Length - 1;
        }

        /// <summary>
        /// 分析Include使用情况
        /// </summary>
        private List<string> AnalyzeIncludeUsage<T>(IQueryable<T> query)
        {
            var recommendations = new List<string>();

            // 这里是一个简化实现
            var queryString = query.ToString();
            
            if (queryString?.Contains(".Include(") == true)
            {
                recommendations.Add("检测到Include使用，确保只加载必需的关联数据");
            }

            return recommendations;
        }

        /// <summary>
        /// 估算性能等级
        /// </summary>
        private PerformanceLevel EstimatePerformanceLevel(int complexityScore, int joinCount)
        {
            return (complexityScore, joinCount) switch
            {
                ( < 20, < 2) => PerformanceLevel.Excellent,
                ( < 50, < 4) => PerformanceLevel.Good,
                ( < 100, < 6) => PerformanceLevel.Average,
                ( < 200, < 8) => PerformanceLevel.Poor,
                _ => PerformanceLevel.Critical
            };
        }

        #endregion

        #region 内部数据类

        /// <summary>
        /// 编译查询缓存项
        /// </summary>
        internal class CompiledQuery
        {
            public Func<DbContext, IQueryable> QueryFactory { get; set; } = null!;
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        }

        #endregion
    }
}
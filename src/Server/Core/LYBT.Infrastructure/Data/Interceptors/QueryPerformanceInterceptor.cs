using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using LYBT.Infrastructure.Data.Monitoring;

namespace LYBT.Infrastructure.Data.Interceptors
{
    /// <summary>
    /// EF Core查询性能监控拦截器
    /// 用于检测和记录慢查询，帮助识别潜在的性能问题
    /// </summary>
    public class QueryPerformanceInterceptor : DbCommandInterceptor
    {
        private readonly ILogger<QueryPerformanceInterceptor> _logger;
        private readonly IQueryStatisticsCollector? _statisticsCollector;
        private readonly int _slowQueryThresholdMs;
        private readonly bool _includeStackTrace;

        /// <summary>
        /// 初始化查询性能拦截器
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="slowQueryThresholdMs">慢查询阈值（毫秒），默认100ms</param>
        /// <param name="includeStackTrace">是否包含调用堆栈，默认false</param>
        /// <param name="statisticsCollector">查询统计收集器（可选）</param>
        public QueryPerformanceInterceptor(
            ILogger<QueryPerformanceInterceptor> logger,
            int slowQueryThresholdMs = 100,
            bool includeStackTrace = false,
            IQueryStatisticsCollector? statisticsCollector = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _slowQueryThresholdMs = slowQueryThresholdMs;
            _includeStackTrace = includeStackTrace;
            _statisticsCollector = statisticsCollector;
        }

        /// <summary>
        /// 同步查询执行后的拦截
        /// </summary>
        public override DbDataReader ReaderExecuted(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result)
        {
            CheckSlowQuery(eventData, command);
            return base.ReaderExecuted(command, eventData, result);
        }

        /// <summary>
        /// 异步查询执行后的拦截
        /// </summary>
        public override async ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result,
            CancellationToken cancellationToken = default)
        {
            CheckSlowQuery(eventData, command);
            return await base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }

        /// <summary>
        /// 非查询命令（INSERT/UPDATE/DELETE）执行后的拦截
        /// </summary>
        public override int NonQueryExecuted(
            DbCommand command,
            CommandExecutedEventData eventData,
            int result)
        {
            CheckSlowQuery(eventData, command);
            return base.NonQueryExecuted(command, eventData, result);
        }

        /// <summary>
        /// 异步非查询命令执行后的拦截
        /// </summary>
        public override async ValueTask<int> NonQueryExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            CheckSlowQuery(eventData, command);
            return await base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
        }

        /// <summary>
        /// 标量查询执行后的拦截
        /// </summary>
        public override object? ScalarExecuted(
            DbCommand command,
            CommandExecutedEventData eventData,
            object? result)
        {
            CheckSlowQuery(eventData, command);
            return base.ScalarExecuted(command, eventData, result);
        }

        /// <summary>
        /// 异步标量查询执行后的拦截
        /// </summary>
        public override async ValueTask<object?> ScalarExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            object? result,
            CancellationToken cancellationToken = default)
        {
            CheckSlowQuery(eventData, command);
            return await base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
        }

        /// <summary>
        /// 检查是否为慢查询并记录
        /// </summary>
        private void CheckSlowQuery(CommandExecutedEventData eventData, DbCommand command)
        {
            var duration = eventData.Duration;
            var isSlowQuery = duration.TotalMilliseconds > _slowQueryThresholdMs;

            // 记录到统计收集器
            _statisticsCollector?.RecordQueryExecution(
                command.CommandText, 
                duration.TotalMilliseconds, 
                isSlowQuery);

            // 记录所有查询的基本统计信息（仅在Debug级别）
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "查询执行统计 - 耗时: {Duration}ms, 命令类型: {CommandType}",
                    duration.TotalMilliseconds,
                    command.CommandType);
            }

            // 检查是否为慢查询
            if (duration.TotalMilliseconds > _slowQueryThresholdMs)
            {
                var commandText = command.CommandText;
                var parameters = GetParameterInfo(command);
                
                var logMessage = $"慢查询检测: {duration.TotalMilliseconds:F2}ms (阈值: {_slowQueryThresholdMs}ms)\n" +
                               $"SQL: {commandText}\n" +
                               $"参数: {parameters}";

                if (_includeStackTrace)
                {
                    var stackTrace = Environment.StackTrace;
                    logMessage += $"\n调用堆栈:\n{GetRelevantStackTrace(stackTrace)}";
                }

                // 根据查询时间选择日志级别
                if (duration.TotalMilliseconds > _slowQueryThresholdMs * 10)
                {
                    // 极慢查询（超过阈值10倍）使用Error级别
                    _logger.LogError(logMessage);
                }
                else if (duration.TotalMilliseconds > _slowQueryThresholdMs * 5)
                {
                    // 很慢查询（超过阈值5倍）使用Warning级别
                    _logger.LogWarning(logMessage);
                }
                else
                {
                    // 一般慢查询使用Information级别
                    _logger.LogInformation(logMessage);
                }

                // 分析潜在的N+1问题
                AnalyzePotentialN1Issue(commandText, duration);
            }
        }

        /// <summary>
        /// 获取参数信息
        /// </summary>
        private string GetParameterInfo(DbCommand command)
        {
            if (command.Parameters.Count == 0)
                return "无";

            var parameters = new List<string>();
            foreach (DbParameter param in command.Parameters)
            {
                parameters.Add($"{param.ParameterName}={param.Value ?? "NULL"}");
            }
            return string.Join(", ", parameters);
        }

        /// <summary>
        /// 获取相关的调用堆栈（过滤掉EF Core内部调用）
        /// </summary>
        private string GetRelevantStackTrace(string fullStackTrace)
        {
            var lines = fullStackTrace.Split('\n');
            var relevantLines = lines
                .Where(line => !line.Contains("Microsoft.EntityFrameworkCore") &&
                              !line.Contains("System.") &&
                              !line.Contains("at Microsoft.") &&
                              line.Contains("LYBT"))
                .Take(10); // 只取前10行相关的

            return string.Join("\n", relevantLines);
        }

        /// <summary>
        /// 分析潜在的N+1查询问题
        /// </summary>
        private void AnalyzePotentialN1Issue(string commandText, TimeSpan duration)
        {
            // 简单的启发式检测：如果在短时间内执行了多个相似的单行查询，可能是N+1问题
            var lowerCommand = commandText.ToLower();
            
            // 检查是否为单行查询（包含TOP 1或LIMIT 1）
            bool isSingleRowQuery = lowerCommand.Contains("top(1)") || 
                                  lowerCommand.Contains("top 1") ||
                                  lowerCommand.Contains("limit 1");

            // 检查是否为通过外键查询
            bool hasForeignKeyPattern = lowerCommand.Contains("where") && 
                                       (lowerCommand.Contains("_id = @") || 
                                        lowerCommand.Contains("id] = @"));

            if (isSingleRowQuery && hasForeignKeyPattern)
            {
                _logger.LogWarning(
                    "潜在N+1查询模式检测: 单行外键查询\n" +
                    "建议: 考虑使用Include()预加载相关数据或使用投影(Select)减少查询次数\n" +
                    "SQL片段: {SqlSnippet}",
                    commandText.Length > 200 ? commandText.Substring(0, 200) + "..." : commandText);
            }

            // 检查是否为批量小查询（可能是循环查询）
            if (duration.TotalMilliseconds < 10 && hasForeignKeyPattern)
            {
                _logger.LogInformation(
                    "快速外键查询检测（{Duration}ms）: 如果频繁出现，可能存在N+1问题",
                    duration.TotalMilliseconds);
            }
        }
    }
}
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LYBT.Infrastructure.Transactions
{
    /// <summary>
    /// 事务性能指标收集器
    /// </summary>
    public class TransactionMetrics
    {
        private readonly ConcurrentDictionary<string, MetricData> _metrics = new();
        private readonly object _lock = new();
        private long _totalTransactions = 0;
        private long _successfulTransactions = 0;
        private long _failedTransactions = 0;

        /// <summary>
        /// 记录事务执行时间
        /// </summary>
        /// <param name="transactionName">事务名称</param>
        /// <param name="duration">执行时长</param>
        public void RecordExecutionTime(string transactionName, TimeSpan duration)
        {
            _metrics.AddOrUpdate(
                transactionName,
                new MetricData { Name = transactionName, ExecutionTimes = [duration] },
                (key, existing) =>
                {
                    lock (existing)
                    {
                        existing.ExecutionTimes.Add(duration);
                        existing.TotalExecutions++;
                        existing.LastExecutionTime = DateTime.UtcNow;
                        return existing;
                    }
                });
        }

        /// <summary>
        /// 记录事务成功
        /// </summary>
        /// <param name="transactionName">事务名称</param>
        public void RecordTransactionSuccess(string transactionName = "")
        {
            Interlocked.Increment(ref _totalTransactions);
            Interlocked.Increment(ref _successfulTransactions);

            if (!string.IsNullOrEmpty(transactionName))
            {
                _metrics.AddOrUpdate(
                    transactionName,
                    new MetricData { Name = transactionName, SuccessCount = 1 },
                    (key, existing) =>
                    {
                        lock (existing)
                        {
                            existing.SuccessCount++;
                            existing.TotalExecutions++;
                            existing.LastExecutionTime = DateTime.UtcNow;
                            return existing;
                        }
                    });
            }
        }

        /// <summary>
        /// 记录事务失败
        /// </summary>
        /// <param name="transactionName">事务名称</param>
        /// <param name="errorMessage">错误消息</param>
        public void RecordTransactionFailure(string transactionName = "", string errorMessage = "")
        {
            Interlocked.Increment(ref _totalTransactions);
            Interlocked.Increment(ref _failedTransactions);

            if (!string.IsNullOrEmpty(transactionName))
            {
                _metrics.AddOrUpdate(
                    transactionName,
                    new MetricData { Name = transactionName, FailureCount = 1 },
                    (key, existing) =>
                    {
                        lock (existing)
                        {
                            existing.FailureCount++;
                            existing.TotalExecutions++;
                            existing.LastExecutionTime = DateTime.UtcNow;
                            if (!string.IsNullOrEmpty(errorMessage))
                            {
                                existing.RecentErrors.Add($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}: {errorMessage}");

                                // 只保留最近10个错误
                                if (existing.RecentErrors.Count > 10)
                                {
                                    existing.RecentErrors.RemoveAt(0);
                                }
                            }

                            return existing;
                        }
                    });
            }
        }

        /// <summary>
        /// 获取整体统计信息
        /// </summary>
        /// <returns>统计信息</returns>
        public TransactionStatistics GetOverallStatistics()
        {
            return new TransactionStatistics
            {
                TotalTransactions = _totalTransactions,
                SuccessfulTransactions = _successfulTransactions,
                FailedTransactions = _failedTransactions,
                SuccessRate = _totalTransactions > 0 ? (double)_successfulTransactions / _totalTransactions * 100 : 0
            };
        }

        /// <summary>
        /// 获取指定事务的统计信息
        /// </summary>
        /// <param name="transactionName">事务名称</param>
        /// <returns>事务统计信息</returns>
        public TransactionMetricDetails? GetTransactionMetrics(string transactionName)
        {
            if (!_metrics.TryGetValue(transactionName, out var metric))
                return null;

            lock (metric)
            {
                var executionTimes = metric.ExecutionTimes.ToArray();
                return new TransactionMetricDetails
                {
                    TransactionName = transactionName,
                    TotalExecutions = metric.TotalExecutions,
                    SuccessCount = metric.SuccessCount,
                    FailureCount = metric.FailureCount,
                    SuccessRate = metric.TotalExecutions > 0 ? (double)metric.SuccessCount / metric.TotalExecutions * 100 : 0,
                    AverageExecutionTime = executionTimes.Length > 0 ? TimeSpan.FromTicks((long)executionTimes.Average(t => t.Ticks)) : TimeSpan.Zero,
                    MinExecutionTime = executionTimes.Length > 0 ? executionTimes.Min() : TimeSpan.Zero,
                    MaxExecutionTime = executionTimes.Length > 0 ? executionTimes.Max() : TimeSpan.Zero,
                    LastExecutionTime = metric.LastExecutionTime,
                    RecentErrors = metric.RecentErrors.ToArray()
                };
            }
        }

        /// <summary>
        /// 获取所有事务的统计信息
        /// </summary>
        /// <returns>所有事务统计信息</returns>
        public Dictionary<string, TransactionMetricDetails> GetAllMetrics()
        {
            var result = new Dictionary<string, TransactionMetricDetails>();

            foreach (var kvp in _metrics)
            {
                var details = GetTransactionMetrics(kvp.Key);
                if (details != null)
                {
                    result[kvp.Key] = details;
                }
            }

            return result;
        }

        /// <summary>
        /// 清除所有指标数据
        /// </summary>
        public void ClearMetrics()
        {
            lock (_lock)
            {
                _metrics.Clear();
                _totalTransactions = 0;
                _successfulTransactions = 0;
                _failedTransactions = 0;
            }
        }
    }

    /// <summary>
    /// 指标数据
    /// </summary>
    internal class MetricData
    {
        public string Name { get; set; } = string.Empty;
        public long TotalExecutions { get; set; } = 0;
        public long SuccessCount { get; set; } = 0;
        public long FailureCount { get; set; } = 0;
        public List<TimeSpan> ExecutionTimes { get; set; } = new();
        public DateTime? LastExecutionTime { get; set; }
        public List<string> RecentErrors { get; set; } = new();
    }

    /// <summary>
    /// 事务统计信息
    /// </summary>
    public class TransactionStatistics
    {
        public long TotalTransactions { get; set; }
        public long SuccessfulTransactions { get; set; }
        public long FailedTransactions { get; set; }
        public double SuccessRate { get; set; }
    }

    /// <summary>
    /// 事务详细指标
    /// </summary>
    public class TransactionMetricDetails
    {
        public string TransactionName { get; set; } = string.Empty;
        public long TotalExecutions { get; set; }
        public long SuccessCount { get; set; }
        public long FailureCount { get; set; }
        public double SuccessRate { get; set; }
        public TimeSpan AverageExecutionTime { get; set; }
        public TimeSpan MinExecutionTime { get; set; }
        public TimeSpan MaxExecutionTime { get; set; }
        public DateTime? LastExecutionTime { get; set; }
        public string[] RecentErrors { get; set; } = Array.Empty<string>();
    }
}

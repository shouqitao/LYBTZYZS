using System;
using System.Collections.Generic;

namespace LYBT.Desktop.Contracts.Performance
{
    /// <summary>
    /// 性能监控接口 - 提供应用程序性能指标收集能力
    /// </summary>
    public interface IPerformanceMonitor
    {
        /// <summary>
        /// 开始计时指定操作
        /// </summary>
        /// <param name="operationName">操作名称</param>
        void StartTiming(string operationName);

        /// <summary>
        /// 停止计时并记录性能指标
        /// </summary>
        /// <param name="operationName">操作名称</param>
        /// <returns>操作耗时（毫秒）</returns>
        long StopTiming(string operationName);

        /// <summary>
        /// 记录当前内存基线
        /// </summary>
        /// <param name="label">内存快照标签</param>
        /// <returns>当前内存使用量（字节）</returns>
        long RecordMemoryBaseline(string label);

        /// <summary>
        /// 获取所有已记录的内存快照
        /// </summary>
        IReadOnlyDictionary<string, long> GetMemorySnapshots();

        /// <summary>
        /// 获取指定操作的性能指标
        /// </summary>
        /// <param name="operationName">操作名称</param>
        /// <returns>性能指标，如果不存在则返回null</returns>
        PerformanceMetric? GetMetric(string operationName);

        /// <summary>
        /// 获取所有已完成的性能指标
        /// </summary>
        IReadOnlyCollection<PerformanceMetric> GetAllMetrics();

        /// <summary>
        /// 生成性能报告
        /// </summary>
        /// <returns>格式化的性能报告</returns>
        PerformanceReport GenerateReport();

        /// <summary>
        /// 清除所有记录的指标
        /// </summary>
        void Clear();

        /// <summary>
        /// 当性能指标被记录时触发的事件
        /// </summary>
        event EventHandler<PerformanceMetricRecordedEventArgs>? MetricRecorded;
    }

    /// <summary>
    /// 性能指标记录事件参数
    /// </summary>
    public class PerformanceMetricRecordedEventArgs : EventArgs
    {
        /// <summary>
        /// 被记录的性能指标
        /// </summary>
        public PerformanceMetric Metric { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public PerformanceMetricRecordedEventArgs(PerformanceMetric metric)
        {
            Metric = metric ?? throw new ArgumentNullException(nameof(metric));
        }
    }
}

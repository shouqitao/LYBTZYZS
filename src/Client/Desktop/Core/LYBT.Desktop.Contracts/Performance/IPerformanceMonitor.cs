using System;
using System.Collections.Generic;
using LYBT.Desktop.Contracts.Models;

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

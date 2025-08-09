using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace LYBT.WPF.Client.Core.Testing.Performance
{
    /// <summary>
    /// 性能基准测试接口
    /// 用于测量 WPF 应用的各项性能指标
    /// </summary>
    public interface IPerformanceBenchmark
    {
        /// <summary>
        /// 执行性能基准测试
        /// </summary>
        /// <param name="testName">测试名称</param>
        /// <param name="testAction">测试操作</param>
        /// <param name="iterations">迭代次数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>性能测试结果</returns>
        Task<PerformanceTestResult> RunBenchmarkAsync(
            string testName,
            Func<Task> testAction,
            int iterations = 100,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 测量内存使用情况
        /// </summary>
        /// <param name="action">要测量的操作</param>
        /// <returns>内存使用结果</returns>
        Task<MemoryUsageResult> MeasureMemoryUsageAsync(Func<Task> action);

        /// <summary>
        /// 测量UI响应时间
        /// </summary>
        /// <param name="uiAction">UI操作</param>
        /// <param name="expectedDelay">预期延迟(毫秒)</param>
        /// <returns>UI响应时间结果</returns>
        Task<UIResponseResult> MeasureUIResponseTimeAsync(
            Action uiAction, 
            int expectedDelay = 100);

        /// <summary>
        /// 对比测试两个不同的实现
        /// </summary>
        /// <param name="baselineTest">基准测试</param>
        /// <param name="optimizedTest">优化后测试</param>
        /// <param name="testName">测试名称</param>
        /// <param name="iterations">迭代次数</param>
        /// <returns>对比测试结果</returns>
        Task<ComparisonTestResult> CompareImplementationsAsync(
            Func<Task> baselineTest,
            Func<Task> optimizedTest,
            string testName,
            int iterations = 50);

        /// <summary>
        /// 生成性能报告
        /// </summary>
        /// <param name="results">测试结果集合</param>
        /// <returns>性能报告</returns>
        string GeneratePerformanceReport(IEnumerable<PerformanceTestResult> results);
    }

    /// <summary>
    /// 性能测试结果
    /// </summary>
    public class PerformanceTestResult
    {
        /// <summary>
        /// 测试名称
        /// </summary>
        public string TestName { get; set; } = string.Empty;

        /// <summary>
        /// 总执行时间(毫秒)
        /// </summary>
        public double TotalExecutionTimeMs { get; set; }

        /// <summary>
        /// 平均执行时间(毫秒)
        /// </summary>
        public double AverageExecutionTimeMs { get; set; }

        /// <summary>
        /// 最小执行时间(毫秒)
        /// </summary>
        public double MinExecutionTimeMs { get; set; }

        /// <summary>
        /// 最大执行时间(毫秒)
        /// </summary>
        public double MaxExecutionTimeMs { get; set; }

        /// <summary>
        /// 标准差
        /// </summary>
        public double StandardDeviation { get; set; }

        /// <summary>
        /// 迭代次数
        /// </summary>
        public int Iterations { get; set; }

        /// <summary>
        /// 每秒操作数
        /// </summary>
        public double OperationsPerSecond => Iterations / (TotalExecutionTimeMs / 1000.0);

        /// <summary>
        /// 测试开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 测试结束时间
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 成功的迭代数
        /// </summary>
        public int SuccessfulIterations { get; set; }

        /// <summary>
        /// 失败的迭代数
        /// </summary>
        public int FailedIterations { get; set; }

        /// <summary>
        /// 成功率
        /// </summary>
        public double SuccessRate => Iterations > 0 ? (double)SuccessfulIterations / Iterations : 0.0;

        /// <summary>
        /// 错误信息列表
        /// </summary>
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// 内存使用结果
    /// </summary>
    public class MemoryUsageResult
    {
        /// <summary>
        /// 执行前内存使用(MB)
        /// </summary>
        public double MemoryBeforeMB { get; set; }

        /// <summary>
        /// 执行后内存使用(MB)
        /// </summary>
        public double MemoryAfterMB { get; set; }

        /// <summary>
        /// 内存增长(MB)
        /// </summary>
        public double MemoryIncreaseMB => MemoryAfterMB - MemoryBeforeMB;

        /// <summary>
        /// 峰值内存使用(MB)
        /// </summary>
        public double PeakMemoryMB { get; set; }

        /// <summary>
        /// GC回收前内存(MB)
        /// </summary>
        public double MemoryBeforeGCMB { get; set; }

        /// <summary>
        /// GC回收后内存(MB)
        /// </summary>
        public double MemoryAfterGCMB { get; set; }

        /// <summary>
        /// GC回收的内存(MB)
        /// </summary>
        public double GCCollectedMB => MemoryBeforeGCMB - MemoryAfterGCMB;

        /// <summary>
        /// GC回收次数
        /// </summary>
        public Dictionary<int, int> GCCollections { get; set; } = new();

        /// <summary>
        /// 执行时间(毫秒)
        /// </summary>
        public double ExecutionTimeMs { get; set; }
    }

    /// <summary>
    /// UI响应时间结果
    /// </summary>
    public class UIResponseResult
    {
        /// <summary>
        /// UI操作响应时间(毫秒)
        /// </summary>
        public double ResponseTimeMs { get; set; }

        /// <summary>
        /// 渲染时间(毫秒)
        /// </summary>
        public double RenderTimeMs { get; set; }

        /// <summary>
        /// 布局时间(毫秒)
        /// </summary>
        public double LayoutTimeMs { get; set; }

        /// <summary>
        /// 是否超时
        /// </summary>
        public bool IsTimeout { get; set; }

        /// <summary>
        /// 预期延迟(毫秒)
        /// </summary>
        public int ExpectedDelayMs { get; set; }

        /// <summary>
        /// 性能等级 (优秀/良好/一般/差)
        /// </summary>
        public string PerformanceGrade 
        { 
            get
            {
                if (ResponseTimeMs <= 16) return "优秀"; // 60fps
                if (ResponseTimeMs <= 33) return "良好"; // 30fps
                if (ResponseTimeMs <= 100) return "一般";
                return "差";
            }
        }

        /// <summary>
        /// UI线程占用率
        /// </summary>
        public double UIThreadUtilization { get; set; }
    }

    /// <summary>
    /// 对比测试结果
    /// </summary>
    public class ComparisonTestResult
    {
        /// <summary>
        /// 测试名称
        /// </summary>
        public string TestName { get; set; } = string.Empty;

        /// <summary>
        /// 基准测试结果
        /// </summary>
        public PerformanceTestResult BaselineResult { get; set; } = new();

        /// <summary>
        /// 优化后测试结果
        /// </summary>
        public PerformanceTestResult OptimizedResult { get; set; } = new();

        /// <summary>
        /// 性能提升倍数
        /// </summary>
        public double ImprovementRatio => BaselineResult.AverageExecutionTimeMs > 0 
            ? BaselineResult.AverageExecutionTimeMs / OptimizedResult.AverageExecutionTimeMs 
            : 1.0;

        /// <summary>
        /// 性能提升百分比
        /// </summary>
        public double ImprovementPercentage => (ImprovementRatio - 1.0) * 100.0;

        /// <summary>
        /// 是否有显著改善
        /// </summary>
        public bool IsSignificantImprovement => ImprovementPercentage > 10.0; // 10%以上认为有显著改善

        /// <summary>
        /// 内存使用对比
        /// </summary>
        public MemoryComparisonResult? MemoryComparison { get; set; }

        /// <summary>
        /// 结论
        /// </summary>
        public string Conclusion 
        {
            get
            {
                if (ImprovementPercentage > 50) return "性能大幅提升";
                if (ImprovementPercentage > 20) return "性能显著提升";
                if (ImprovementPercentage > 10) return "性能有所改善";
                if (ImprovementPercentage > 0) return "性能轻微改善";
                if (ImprovementPercentage > -10) return "性能基本无变化";
                return "性能有所下降";
            }
        }
    }

    /// <summary>
    /// 内存对比结果
    /// </summary>
    public class MemoryComparisonResult
    {
        /// <summary>
        /// 基准内存使用(MB)
        /// </summary>
        public double BaselineMemoryMB { get; set; }

        /// <summary>
        /// 优化后内存使用(MB)
        /// </summary>
        public double OptimizedMemoryMB { get; set; }

        /// <summary>
        /// 内存节省(MB)
        /// </summary>
        public double MemorySavedMB => BaselineMemoryMB - OptimizedMemoryMB;

        /// <summary>
        /// 内存节省百分比
        /// </summary>
        public double MemorySavedPercentage => BaselineMemoryMB > 0 
            ? (MemorySavedMB / BaselineMemoryMB) * 100.0 
            : 0.0;
    }
}
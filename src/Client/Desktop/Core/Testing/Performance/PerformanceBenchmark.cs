using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace LYBT.Desktop.Core.Testing.Performance
{
    /// <summary>
    /// 高精度性能基准测试实现
    /// 专为 WPF 应用优化，支持UI线程和后台线程测试
    /// </summary>
    public class PerformanceBenchmark : IPerformanceBenchmark
    {
        private readonly Process _currentProcess;
        private readonly object _lockObject = new();

        public PerformanceBenchmark()
        {
            _currentProcess = Process.GetCurrentProcess();
        }

        public async Task<PerformanceTestResult> RunBenchmarkAsync(
            string testName, 
            Func<Task> testAction, 
            int iterations = 100, 
            CancellationToken cancellationToken = default)
        {
            var result = new PerformanceTestResult
            {
                TestName = testName,
                Iterations = iterations,
                StartTime = DateTime.Now
            };

            var executionTimes = new List<double>();
            var stopwatch = new Stopwatch();

            // 预热阶段 - 避免JIT编译影响测试结果
            try
            {
                for (int i = 0; i < Math.Min(5, iterations / 10); i++)
                {
                    await testAction();
                }
            }
            catch
            {
                // 预热失败不影响正式测试
            }

            // 强制GC，确保测试环境的一致性
            await ForceGarbageCollectionAsync();

            // 正式测试阶段
            for (int i = 0; i < iterations; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    stopwatch.Restart();
                    await testAction();
                    stopwatch.Stop();

                    executionTimes.Add(stopwatch.Elapsed.TotalMilliseconds);
                    result.SuccessfulIterations++;
                }
                catch (Exception ex)
                {
                    result.FailedIterations++;
                    result.Errors.Add($"Iteration {i + 1}: {ex.Message}");
                    
                    // 如果失败率过高，提前终止测试
                    if (result.FailedIterations > iterations * 0.2) // 20%失败率
                    {
                        break;
                    }
                }

                // 每10次迭代进行一次小憩，避免过度占用系统资源
                if (i > 0 && i % 10 == 0)
                {
                    await Task.Delay(1, cancellationToken);
                }
            }

            result.EndTime = DateTime.Now;

            // 计算统计数据
            if (executionTimes.Count > 0)
            {
                result.TotalExecutionTimeMs = executionTimes.Sum();
                result.AverageExecutionTimeMs = executionTimes.Average();
                result.MinExecutionTimeMs = executionTimes.Min();
                result.MaxExecutionTimeMs = executionTimes.Max();
                result.StandardDeviation = CalculateStandardDeviation(executionTimes);
            }

            return result;
        }

        public async Task<MemoryUsageResult> MeasureMemoryUsageAsync(Func<Task> action)
        {
            var result = new MemoryUsageResult();
            
            // 强制GC并等待完成
            await ForceGarbageCollectionAsync();

            // 记录执行前的内存状态
            result.MemoryBeforeMB = GetMemoryUsageMB();
            var gcCountsBefore = GetGCCollections();

            var stopwatch = Stopwatch.StartNew();
            var peakMemory = result.MemoryBeforeMB;

            // 启动内存监控任务
            var monitoringTask = Task.Run(async () =>
            {
                while (stopwatch.IsRunning)
                {
                    var currentMemory = GetMemoryUsageMB();
                    if (currentMemory > peakMemory)
                    {
                        peakMemory = currentMemory;
                    }
                    await Task.Delay(10); // 10ms监控间隔
                }
            });

            try
            {
                // 执行测试操作
                await action();
            }
            finally
            {
                stopwatch.Stop();
                result.ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds;
                
                // 停止监控
                await monitoringTask;
            }

            // 记录执行后的内存状态（GC前）
            result.MemoryBeforeGCMB = GetMemoryUsageMB();
            result.PeakMemoryMB = peakMemory;

            // 强制GC并测量回收效果
            await ForceGarbageCollectionAsync();
            
            result.MemoryAfterMB = GetMemoryUsageMB();
            result.MemoryAfterGCMB = result.MemoryAfterMB;

            // 记录GC统计信息
            var gcCountsAfter = GetGCCollections();
            result.GCCollections = CalculateGCDifference(gcCountsBefore, gcCountsAfter);

            return result;
        }

        public async Task<UIResponseResult> MeasureUIResponseTimeAsync(Action uiAction, int expectedDelay = 100)
        {
            var result = new UIResponseResult
            {
                ExpectedDelayMs = expectedDelay
            };

            if (Application.Current?.Dispatcher == null)
            {
                result.IsTimeout = true;
                result.ResponseTimeMs = double.MaxValue;
                return result;
            }

            var stopwatch = new Stopwatch();
            var renderStopwatch = new Stopwatch();
            var layoutStopwatch = new Stopwatch();
            var completed = false;

            // UI操作必须在UI线程执行
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                stopwatch.Start();
                renderStopwatch.Start();
                
                try
                {
                    uiAction();
                    
                    // 模拟布局计算
                    layoutStopwatch.Start();
                    Application.Current.Dispatcher.Invoke(() => { }, DispatcherPriority.Loaded);
                    layoutStopwatch.Stop();
                    
                    renderStopwatch.Stop();
                    completed = true;
                }
                catch
                {
                    result.IsTimeout = true;
                }
                finally
                {
                    stopwatch.Stop();
                }
            }, DispatcherPriority.Normal);

            result.ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds;
            result.RenderTimeMs = renderStopwatch.Elapsed.TotalMilliseconds;
            result.LayoutTimeMs = layoutStopwatch.Elapsed.TotalMilliseconds;
            
            // 检查是否超时
            if (!completed || result.ResponseTimeMs > expectedDelay)
            {
                result.IsTimeout = true;
            }

            // 估算UI线程占用率（简化版）
            result.UIThreadUtilization = Math.Min(100.0, (result.ResponseTimeMs / 16.67) * 100.0); // 基于60fps基准

            return result;
        }

        public async Task<ComparisonTestResult> CompareImplementationsAsync(
            Func<Task> baselineTest, 
            Func<Task> optimizedTest, 
            string testName, 
            int iterations = 50)
        {
            var result = new ComparisonTestResult
            {
                TestName = testName
            };

            // 测试基准实现
            result.BaselineResult = await RunBenchmarkAsync(
                $"{testName} - Baseline", 
                baselineTest, 
                iterations);

            // 小憩，让系统稳定
            await Task.Delay(100);

            // 测试优化实现
            result.OptimizedResult = await RunBenchmarkAsync(
                $"{testName} - Optimized", 
                optimizedTest, 
                iterations);

            // 内存对比测试
            var baselineMemory = await MeasureMemoryUsageAsync(baselineTest);
            var optimizedMemory = await MeasureMemoryUsageAsync(optimizedTest);

            result.MemoryComparison = new MemoryComparisonResult
            {
                BaselineMemoryMB = baselineMemory.MemoryIncreaseMB,
                OptimizedMemoryMB = optimizedMemory.MemoryIncreaseMB
            };

            return result;
        }

        public string GeneratePerformanceReport(IEnumerable<PerformanceTestResult> results)
        {
            var report = new System.Text.StringBuilder();
            var resultsList = results.ToList();

            report.AppendLine("🚀 性能基准测试报告");
            report.AppendLine("=".PadRight(50, '='));
            report.AppendLine($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"测试项目数: {resultsList.Count}");
            report.AppendLine();

            // 汇总统计
            if (resultsList.Count > 0)
            {
                var totalOperations = resultsList.Sum(r => r.Iterations);
                var totalTime = resultsList.Sum(r => r.TotalExecutionTimeMs);
                var avgSuccessRate = resultsList.Average(r => r.SuccessRate);

                report.AppendLine("📊 汇总统计");
                report.AppendLine("-".PadRight(30, '-'));
                report.AppendLine($"总操作数: {totalOperations:N0}");
                report.AppendLine($"总耗时: {totalTime:F2} ms");
                report.AppendLine($"平均成功率: {avgSuccessRate:P2}");
                report.AppendLine($"平均吞吐量: {(totalOperations / (totalTime / 1000.0)):F2} ops/sec");
                report.AppendLine();
            }

            // 详细结果
            report.AppendLine("📋 详细测试结果");
            report.AppendLine("-".PadRight(30, '-'));

            foreach (var result in resultsList.OrderBy(r => r.AverageExecutionTimeMs))
            {
                report.AppendLine($"测试: {result.TestName}");
                report.AppendLine($"  平均耗时: {result.AverageExecutionTimeMs:F2} ms");
                report.AppendLine($"  最小/最大: {result.MinExecutionTimeMs:F2} / {result.MaxExecutionTimeMs:F2} ms");
                report.AppendLine($"  标准差: {result.StandardDeviation:F2} ms");
                report.AppendLine($"  迭代次数: {result.Iterations}");
                report.AppendLine($"  成功率: {result.SuccessRate:P2}");
                report.AppendLine($"  吞吐量: {result.OperationsPerSecond:F2} ops/sec");

                if (result.Errors.Count > 0)
                {
                    report.AppendLine($"  错误数: {result.Errors.Count}");
                }

                // 性能等级评定
                var grade = GetPerformanceGrade(result.AverageExecutionTimeMs);
                report.AppendLine($"  性能等级: {grade}");
                report.AppendLine();
            }

            // 性能排行榜
            if (resultsList.Count > 1)
            {
                report.AppendLine("🏆 性能排行榜 (按平均耗时升序)");
                report.AppendLine("-".PadRight(40, '-'));
                
                for (int i = 0; i < resultsList.Count; i++)
                {
                    var result = resultsList.OrderBy(r => r.AverageExecutionTimeMs).ElementAt(i);
                    var medal = i == 0 ? "🥇" : i == 1 ? "🥈" : i == 2 ? "🥉" : "  ";
                    report.AppendLine($"{medal} {i + 1}. {result.TestName} - {result.AverageExecutionTimeMs:F2} ms");
                }
                report.AppendLine();
            }

            // 建议和结论
            report.AppendLine("💡 性能优化建议");
            report.AppendLine("-".PadRight(30, '-'));
            
            var slowTests = resultsList.Where(r => r.AverageExecutionTimeMs > 100).ToList();
            if (slowTests.Count > 0)
            {
                report.AppendLine("需要优化的慢速操作:");
                foreach (var test in slowTests)
                {
                    report.AppendLine($"• {test.TestName}: {test.AverageExecutionTimeMs:F2} ms");
                }
            }

            var highErrorTests = resultsList.Where(r => r.FailedIterations > 0).ToList();
            if (highErrorTests.Count > 0)
            {
                report.AppendLine("存在错误的测试:");
                foreach (var test in highErrorTests)
                {
                    report.AppendLine($"• {test.TestName}: {test.FailedIterations} 次失败");
                }
            }

            return report.ToString();
        }

        #region 私有辅助方法

        private async Task ForceGarbageCollectionAsync()
        {
            await Task.Run(() =>
            {
                GC.Collect(2, GCCollectionMode.Forced);
                GC.WaitForPendingFinalizers();
                GC.Collect(2, GCCollectionMode.Forced);
            });
        }

        private double GetMemoryUsageMB()
        {
            try
            {
                _currentProcess.Refresh();
                return _currentProcess.WorkingSet64 / 1024.0 / 1024.0;
            }
            catch
            {
                return 0;
            }
        }

        private Dictionary<int, int> GetGCCollections()
        {
            return new Dictionary<int, int>
            {
                { 0, GC.CollectionCount(0) },
                { 1, GC.CollectionCount(1) },
                { 2, GC.CollectionCount(2) }
            };
        }

        private Dictionary<int, int> CalculateGCDifference(Dictionary<int, int> before, Dictionary<int, int> after)
        {
            var result = new Dictionary<int, int>();
            foreach (var kvp in before)
            {
                result[kvp.Key] = after.GetValueOrDefault(kvp.Key, 0) - kvp.Value;
            }
            return result;
        }

        private static double CalculateStandardDeviation(IEnumerable<double> values)
        {
            var valuesList = values.ToList();
            if (valuesList.Count <= 1) return 0;

            var average = valuesList.Average();
            var sumOfSquares = valuesList.Sum(x => Math.Pow(x - average, 2));
            return Math.Sqrt(sumOfSquares / (valuesList.Count - 1));
        }

        private static string GetPerformanceGrade(double avgTimeMs)
        {
            return avgTimeMs switch
            {
                < 1 => "优秀 (A+)",
                < 10 => "良好 (A)",
                < 50 => "一般 (B)",
                < 100 => "较差 (C)",
                _ => "很差 (D)"
            };
        }

        #endregion
    }
}
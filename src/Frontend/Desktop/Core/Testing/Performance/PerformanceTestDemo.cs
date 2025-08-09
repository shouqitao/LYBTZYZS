using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace LYBT.WPF.Client.Core.Testing.Performance
{
    /// <summary>
    /// 性能测试演示程序
    /// 展示如何使用性能测试框架进行各种测试
    /// </summary>
    public class PerformanceTestDemo
    {
        /// <summary>
        /// 运行性能测试演示
        /// </summary>
        public static async Task RunPerformanceTestDemoAsync()
        {
            Debug.WriteLine("🎯 开始性能测试演示程序...");

            try
            {
                // 1. 快速性能检查演示
                await RunQuickPerformanceCheckDemo();

                // 2. 完整性能测试套件演示
                await RunCompleteTestSuiteDemo();

                // 3. 自定义性能测试演示
                await RunCustomPerformanceTestDemo();

                Debug.WriteLine("🎉 性能测试演示程序完成!");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ 演示程序执行失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 快速性能检查演示
        /// </summary>
        private static async Task RunQuickPerformanceCheckDemo()
        {
            Debug.WriteLine("\n⚡ === 快速性能检查演示 ===");

            var testRunner = new PerformanceTestRunner();
            var result = await testRunner.RunQuickPerformanceCheckAsync();

            if (result.Success)
            {
                Debug.WriteLine($"✅ 快速检查完成 - 性能等级: {result.PerformanceGrade}");
                Debug.WriteLine($"检查耗时: {result.Duration.TotalSeconds:F1} 秒");

                if (result.RenderingPerformance != null)
                {
                    Debug.WriteLine($"渲染性能提升: {result.RenderingPerformance.ImprovementPercentage:F1}%");
                    Debug.WriteLine($"结论: {result.RenderingPerformance.Conclusion}");
                }
            }
            else
            {
                Debug.WriteLine($"❌ 快速检查失败: {result.ErrorMessage}");
            }
        }

        /// <summary>
        /// 完整性能测试套件演示
        /// </summary>
        private static async Task RunCompleteTestSuiteDemo()
        {
            Debug.WriteLine("\n🚀 === 完整性能测试套件演示 ===");

            var testRunner = new PerformanceTestRunner();
            
            // 生成报告路径
            var reportPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                $"LYBT_性能测试报告_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

            Debug.WriteLine($"报告将保存到: {reportPath}");

            var result = await testRunner.RunCompleteTestSuiteAsync(reportPath);

            if (result.Success)
            {
                Debug.WriteLine("✅ 完整测试套件执行成功!");
                Debug.WriteLine($"总耗时: {result.TotalDuration.TotalMinutes:F1} 分钟");
                Debug.WriteLine($"报告已保存: {result.ReportPath}");

                // 显示关键性能指标
                if (result.VirtualizationReport != null)
                {
                    DisplayVirtualizationSummary(result.VirtualizationReport);
                }
            }
            else
            {
                Debug.WriteLine($"❌ 测试套件执行失败: {result.ErrorMessage}");
            }
        }

        /// <summary>
        /// 自定义性能测试演示
        /// </summary>
        private static async Task RunCustomPerformanceTestDemo()
        {
            Debug.WriteLine("\n🔧 === 自定义性能测试演示 ===");

            var benchmark = new PerformanceBenchmark();

            // 演示1: 简单的计算性能测试
            var mathTest = await benchmark.RunBenchmarkAsync(
                "数学运算性能",
                async () =>
                {
                    await Task.Run(() =>
                    {
                        double result = 0;
                        for (int i = 0; i < 10000; i++)
                        {
                            result += Math.Sin(i) * Math.Cos(i);
                        }
                    });
                },
                100);

            Debug.WriteLine($"数学运算测试 - 平均耗时: {mathTest.AverageExecutionTimeMs:F2}ms");
            Debug.WriteLine($"吞吐量: {mathTest.OperationsPerSecond:F0} ops/sec");

            // 演示2: 内存使用测试
            var memoryResult = await benchmark.MeasureMemoryUsageAsync(async () =>
            {
                await Task.Run(() =>
                {
                    // 分配大量对象
                    var objects = new object[50000];
                    for (int i = 0; i < objects.Length; i++)
                    {
                        objects[i] = new { Index = i, Value = Guid.NewGuid() };
                    }
                });
            });

            Debug.WriteLine($"内存测试 - 内存增长: {memoryResult.MemoryIncreaseMB:F2}MB");
            Debug.WriteLine($"峰值内存: {memoryResult.PeakMemoryMB:F2}MB");
            Debug.WriteLine($"GC回收: {memoryResult.GCCollectedMB:F2}MB");

            // 演示3: 对比测试
            var comparisonResult = await benchmark.CompareImplementationsAsync(
                // 基准实现：线性搜索
                async () =>
                {
                    await Task.Run(() =>
                    {
                        var data = new int[10000];
                        for (int i = 0; i < data.Length; i++)
                        {
                            data[i] = i;
                        }
                        
                        // 线性搜索
                        var target = 7777;
                        for (int i = 0; i < data.Length; i++)
                        {
                            if (data[i] == target)
                                break;
                        }
                    });
                },
                // 优化实现：二分搜索
                async () =>
                {
                    await Task.Run(() =>
                    {
                        var data = new int[10000];
                        for (int i = 0; i < data.Length; i++)
                        {
                            data[i] = i;
                        }
                        
                        // 二分搜索
                        Array.BinarySearch(data, 7777);
                    });
                },
                "搜索算法对比",
                50);

            Debug.WriteLine($"搜索算法对比 - 性能提升: {comparisonResult.ImprovementPercentage:F1}%");
            Debug.WriteLine($"结论: {comparisonResult.Conclusion}");
        }

        /// <summary>
        /// 显示虚拟化测试摘要
        /// </summary>
        private static void DisplayVirtualizationSummary(VirtualizationTestReport report)
        {
            Debug.WriteLine("\n📊 虚拟化测试摘要:");

            if (report.DataRenderingResults != null)
            {
                Debug.WriteLine($"• 数据渲染性能提升: {report.DataRenderingResults.ImprovementPercentage:F1}%");
            }

            if (report.ScrollingResults != null)
            {
                Debug.WriteLine($"• 滚动性能提升: {report.ScrollingResults.ImprovementPercentage:F1}%");
            }

            if (report.MemoryUsageResults != null)
            {
                var memorySaved = report.MemoryUsageResults.TraditionalMemory.MemoryIncreaseMB 
                                - report.MemoryUsageResults.VirtualizedMemory.MemoryIncreaseMB;
                Debug.WriteLine($"• 内存节省: {memorySaved:F1}MB");
            }

            if (report.CachePerformanceResults?.CacheStatistics != null)
            {
                Debug.WriteLine($"• 缓存命中率: {report.CachePerformanceResults.CacheStatistics.HitRatio:P1}");
            }

            if (!string.IsNullOrEmpty(report.TestError))
            {
                Debug.WriteLine($"⚠️ 测试错误: {report.TestError}");
            }
        }

        /// <summary>
        /// 创建性能测试示例 - 供其他开发者参考
        /// </summary>
        public static async Task CreatePerformanceTestExampleAsync()
        {
            Debug.WriteLine("📖 创建性能测试示例...");

            var benchmark = new PerformanceBenchmark();

            // 示例：测试集合操作性能
            var listVsArrayTest = await benchmark.CompareImplementationsAsync(
                // List<T> 操作
                async () =>
                {
                    await Task.Run(() =>
                    {
                        var list = new System.Collections.Generic.List<int>();
                        for (int i = 0; i < 10000; i++)
                        {
                            list.Add(i);
                        }
                        
                        // 随机访问测试
                        var random = new Random(12345);
                        for (int i = 0; i < 1000; i++)
                        {
                            var index = random.Next(list.Count);
                            var value = list[index];
                        }
                    });
                },
                // Array 操作
                async () =>
                {
                    await Task.Run(() =>
                    {
                        var array = new int[10000];
                        for (int i = 0; i < array.Length; i++)
                        {
                            array[i] = i;
                        }
                        
                        // 随机访问测试
                        var random = new Random(12345);
                        for (int i = 0; i < 1000; i++)
                        {
                            var index = random.Next(array.Length);
                            var value = array[index];
                        }
                    });
                },
                "List vs Array 性能对比");

            Debug.WriteLine($"集合性能测试结果: {listVsArrayTest.Conclusion}");
            Debug.WriteLine($"性能差异: {listVsArrayTest.ImprovementPercentage:F1}%");

            Debug.WriteLine("示例创建完成！");
        }

        /// <summary>
        /// 性能回归测试 - 确保优化不会导致性能下降
        /// </summary>
        public static async Task<bool> RunPerformanceRegressionTestAsync()
        {
            Debug.WriteLine("🔍 执行性能回归测试...");

            try
            {
                var testRunner = new PerformanceTestRunner();
                var result = await testRunner.RunQuickPerformanceCheckAsync();

                if (!result.Success)
                {
                    Debug.WriteLine($"❌ 回归测试失败: {result.ErrorMessage}");
                    return false;
                }

                // 检查性能是否符合预期阈值
                var performanceAcceptable = result.RenderingPerformance?.ImprovementPercentage >= 10; // 至少10%提升

                if (performanceAcceptable)
                {
                    Debug.WriteLine($"✅ 性能回归测试通过 - 性能等级: {result.PerformanceGrade}");
                    return true;
                }
                else
                {
                    Debug.WriteLine("⚠️ 性能回归测试警告 - 性能未达到预期阈值");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ 回归测试执行异常: {ex.Message}");
                return false;
            }
        }
    }
}
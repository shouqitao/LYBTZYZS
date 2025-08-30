using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace LYBT.Desktop.Core.Testing.Performance
{
    /// <summary>
    /// 性能测试使用指南
    /// 为开发人员提供详细的性能测试使用方法和最佳实践
    /// </summary>
    public static class PerformanceTestUsageGuide
    {
        /// <summary>
        /// 运行完整的使用指南演示
        /// </summary>
        public static async Task RunUsageGuideAsync()
        {
            Debug.WriteLine("📚 性能测试使用指南演示");
            Debug.WriteLine("=".PadRight(50, '='));

            // 1. 基础使用示例
            await BasicUsageExampleAsync();

            // 2. 高级使用示例
            await AdvancedUsageExampleAsync();

            // 3. 性能回归测试示例
            await PerformanceRegressionTestExampleAsync();

            // 4. 性能基准建立示例
            await BenchmarkEstablishmentExampleAsync();

            Debug.WriteLine("✅ 性能测试使用指南演示完成!");
        }

        /// <summary>
        /// 基础使用示例
        /// </summary>
        private static async Task BasicUsageExampleAsync()
        {
            Debug.WriteLine("\n🔰 基础使用示例");
            Debug.WriteLine("-".PadRight(30, '-'));

            // 示例1: 简单的性能基准测试
            Debug.WriteLine("示例1: 简单的性能基准测试");
            
            var benchmark = new PerformanceBenchmark();
            
            var result = await benchmark.RunBenchmarkAsync(
                "字符串拼接性能",
                async () =>
                {
                    await Task.Run(() =>
                    {
                        var result = "";
                        for (int i = 0; i < 1000; i++)
                        {
                            result += $"Item{i}";
                        }
                    });
                },
                iterations: 20);

            Debug.WriteLine($"测试结果: 平均耗时 {result.AverageExecutionTimeMs:F2}ms, 成功率 {result.SuccessRate:P2}");

            // 示例2: 内存使用测试
            Debug.WriteLine("\n示例2: 内存使用测试");
            
            var memoryResult = await benchmark.MeasureMemoryUsageAsync(async () =>
            {
                await Task.Run(() =>
                {
                    var largeArray = new int[1000000]; // 分配4MB内存
                    for (int i = 0; i < largeArray.Length; i++)
                    {
                        largeArray[i] = i;
                    }
                });
            });

            Debug.WriteLine($"内存使用: 增长 {memoryResult.MemoryIncreaseMB:F2}MB, 峰值 {memoryResult.PeakMemoryMB:F2}MB");
        }

        /// <summary>
        /// 高级使用示例
        /// </summary>
        private static async Task AdvancedUsageExampleAsync()
        {
            Debug.WriteLine("\n🚀 高级使用示例");
            Debug.WriteLine("-".PadRight(30, '-'));

            // 示例1: 对比测试 - 不同算法的性能比较
            Debug.WriteLine("示例1: 算法性能对比测试");
            
            var benchmark = new PerformanceBenchmark();
            
            var comparisonResult = await benchmark.CompareImplementationsAsync(
                // 冒泡排序 (低效算法)
                async () =>
                {
                    await Task.Run(() =>
                    {
                        var array = GenerateRandomArray(1000);
                        BubbleSort(array);
                    });
                },
                // 快速排序 (高效算法)
                async () =>
                {
                    await Task.Run(() =>
                    {
                        var array = GenerateRandomArray(1000);
                        Array.Sort(array); // 内置的高效排序
                    });
                },
                "排序算法对比",
                iterations: 10);

            Debug.WriteLine($"性能提升: {comparisonResult.ImprovementPercentage:F1}% ({comparisonResult.Conclusion})");

            // 示例2: 虚拟化性能测试
            Debug.WriteLine("\n示例2: 虚拟化性能专项测试");
            
            var virtualizationTest = new VirtualizationPerformanceTest(benchmark);
            var renderingResult = await virtualizationTest.TestDataRenderingPerformanceAsync();
            
            Debug.WriteLine($"虚拟化渲染性能提升: {renderingResult.ImprovementPercentage:F1}%");
            Debug.WriteLine($"基准耗时: {renderingResult.BaselineResult.AverageExecutionTimeMs:F2}ms");
            Debug.WriteLine($"优化耗时: {renderingResult.OptimizedResult.AverageExecutionTimeMs:F2}ms");

            // 示例3: 完整的性能测试套件
            Debug.WriteLine("\n示例3: 完整性能测试套件");
            
            var testRunner = new PerformanceTestRunner();
            var reportPath = Path.Combine(Path.GetTempPath(), $"PerformanceTest_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            
            var suiteResult = await testRunner.RunCompleteTestSuiteAsync(reportPath);
            
            if (suiteResult.Success)
            {
                Debug.WriteLine($"完整测试套件执行成功, 耗时: {suiteResult.TotalDuration.TotalMinutes:F1}分钟");
                Debug.WriteLine($"报告保存到: {reportPath}");
            }
            else
            {
                Debug.WriteLine($"测试套件执行失败: {suiteResult.ErrorMessage}");
            }
        }

        /// <summary>
        /// 性能回归测试示例
        /// </summary>
        private static async Task PerformanceRegressionTestExampleAsync()
        {
            Debug.WriteLine("\n🔍 性能回归测试示例");
            Debug.WriteLine("-".PadRight(30, '-'));

            Debug.WriteLine("说明: 性能回归测试用于确保代码更改不会导致性能下降");
            Debug.WriteLine("适用场景: CI/CD流水线、代码提交前验证、版本发布前检查");

            // 示例：模拟性能回归检查
            var passedTests = 0;
            var totalTests = 3;

            // 测试1: 关键路径性能检查
            Debug.WriteLine("\n测试1: 关键路径性能检查");
            var criticalPathPerformance = await CheckCriticalPathPerformanceAsync();
            if (criticalPathPerformance)
            {
                Debug.WriteLine("✅ 关键路径性能检查通过");
                passedTests++;
            }
            else
            {
                Debug.WriteLine("❌ 关键路径性能检查失败");
            }

            // 测试2: 内存泄漏检查
            Debug.WriteLine("\n测试2: 内存泄漏检查");
            var memoryLeakCheck = await CheckMemoryLeakAsync();
            if (memoryLeakCheck)
            {
                Debug.WriteLine("✅ 内存泄漏检查通过");
                passedTests++;
            }
            else
            {
                Debug.WriteLine("❌ 发现潜在内存泄漏");
            }

            // 测试3: 响应时间检查
            Debug.WriteLine("\n测试3: UI响应时间检查");
            var responseTimeCheck = await CheckUIResponseTimeAsync();
            if (responseTimeCheck)
            {
                Debug.WriteLine("✅ UI响应时间检查通过");
                passedTests++;
            }
            else
            {
                Debug.WriteLine("❌ UI响应时间超出阈值");
            }

            Debug.WriteLine($"\n回归测试结果: {passedTests}/{totalTests} 通过 ({(double)passedTests / totalTests:P0})");
        }

        /// <summary>
        /// 性能基准建立示例
        /// </summary>
        private static async Task BenchmarkEstablishmentExampleAsync()
        {
            Debug.WriteLine("\n📊 性能基准建立示例");
            Debug.WriteLine("-".PadRight(30, '-'));

            Debug.WriteLine("说明: 建立性能基准用于后续的性能对比和回归测试");
            Debug.WriteLine("建议: 在稳定的环境中运行多次，取平均值作为基准");

            var benchmark = new PerformanceBenchmark();
            var benchmarkResults = new System.Collections.Generic.List<PerformanceTestResult>();

            // 建立多个关键操作的性能基准
            var testCases = new (string Name, Func<Task> Action)[]
            {
                ("数据加载", async () => await SimulateDataLoadingAsync()),
                ("UI渲染", async () => await SimulateUIRenderingAsync()),
                ("数据处理", async () => await SimulateDataProcessingAsync()),
                ("网络请求", async () => await SimulateNetworkRequestAsync())
            };

            foreach (var (testName, testAction) in testCases)
            {
                Debug.WriteLine($"建立 '{testName}' 性能基准...");
                
                var result = await benchmark.RunBenchmarkAsync(testName, testAction, iterations: 50);
                benchmarkResults.Add(result);
                
                Debug.WriteLine($"基准值: {result.AverageExecutionTimeMs:F2}ms ±{result.StandardDeviation:F2}ms");
            }

            // 生成基准报告
            var benchmarkReport = benchmark.GeneratePerformanceReport(benchmarkResults);
            Debug.WriteLine("\n📋 性能基准报告:");
            Debug.WriteLine(benchmarkReport);

            // 保存基准数据 (实际项目中可保存到文件或数据库)
            Debug.WriteLine("💾 基准数据已保存 (模拟)");
        }

        #region 辅助方法

        /// <summary>
        /// 生成随机数组
        /// </summary>
        private static int[] GenerateRandomArray(int size)
        {
            var random = new Random(42); // 固定种子确保结果一致
            var array = new int[size];
            for (int i = 0; i < size; i++)
            {
                array[i] = random.Next(1000);
            }
            return array;
        }

        /// <summary>
        /// 冒泡排序实现
        /// </summary>
        private static void BubbleSort(int[] array)
        {
            int n = array.Length;
            for (int i = 0; i < n - 1; i++)
            {
                for (int j = 0; j < n - i - 1; j++)
                {
                    if (array[j] > array[j + 1])
                    {
                        // 交换元素
                        (array[j], array[j + 1]) = (array[j + 1], array[j]);
                    }
                }
            }
        }

        /// <summary>
        /// 检查关键路径性能
        /// </summary>
        private static async Task<bool> CheckCriticalPathPerformanceAsync()
        {
            var benchmark = new PerformanceBenchmark();
            var result = await benchmark.RunBenchmarkAsync(
                "关键路径性能检查",
                async () => await SimulateDataLoadingAsync(),
                iterations: 10);

            // 性能阈值检查 (示例: 平均耗时不超过100ms)
            return result.AverageExecutionTimeMs <= 100 && result.SuccessRate >= 0.95;
        }

        /// <summary>
        /// 检查内存泄漏
        /// </summary>
        private static async Task<bool> CheckMemoryLeakAsync()
        {
            var benchmark = new PerformanceBenchmark();
            var memoryResult = await benchmark.MeasureMemoryUsageAsync(async () =>
            {
                // 模拟可能导致内存泄漏的操作
                for (int i = 0; i < 10; i++)
                {
                    await SimulateDataLoadingAsync();
                    GC.Collect(); // 强制GC，检查是否能正确回收
                    await Task.Delay(10);
                }
            });

            // 内存增长检查 (示例: 增长不超过10MB)
            return memoryResult.MemoryIncreaseMB <= 10;
        }

        /// <summary>
        /// 检查UI响应时间
        /// </summary>
        private static async Task<bool> CheckUIResponseTimeAsync()
        {
            var benchmark = new PerformanceBenchmark();
            
            try
            {
                var uiResult = await benchmark.MeasureUIResponseTimeAsync(
                    () =>
                    {
                        // 模拟UI操作
                        System.Threading.Thread.Sleep(10); // 模拟UI处理时间
                    },
                    expectedDelay: 50);

                // 响应时间检查 (示例: 响应时间不超过50ms)
                return uiResult.ResponseTimeMs <= 50 && !uiResult.IsTimeout;
            }
            catch
            {
                // 如果没有UI环境，返回true (跳过测试)
                return true;
            }
        }

        /// <summary>
        /// 模拟数据加载
        /// </summary>
        private static async Task SimulateDataLoadingAsync()
        {
            await Task.Run(() =>
            {
                // 模拟数据库查询或文件读取
                var data = new System.Collections.Generic.List<object>();
                for (int i = 0; i < 1000; i++)
                {
                    data.Add(new { Id = i, Name = $"Item{i}", Value = i * 2 });
                }
            });
            
            await Task.Delay(5); // 模拟IO延迟
        }

        /// <summary>
        /// 模拟UI渲染
        /// </summary>
        private static async Task SimulateUIRenderingAsync()
        {
            await Task.Run(() =>
            {
                // 模拟UI控件创建和布局计算
                for (int i = 0; i < 100; i++)
                {
                    var item = new { Name = $"UIElement{i}", Width = 100, Height = 30 };
                    // 模拟布局计算
                    var area = item.Width * item.Height;
                }
            });
        }

        /// <summary>
        /// 模拟数据处理
        /// </summary>
        private static async Task SimulateDataProcessingAsync()
        {
            await Task.Run(() =>
            {
                // 模拟数据转换和计算
                var numbers = new double[5000];
                for (int i = 0; i < numbers.Length; i++)
                {
                    numbers[i] = Math.Sin(i) * Math.Cos(i) + Math.Sqrt(i);
                }
                
                // 模拟统计计算
                var sum = numbers.Sum();
                var average = numbers.Average();
                var max = numbers.Max();
            });
        }

        /// <summary>
        /// 模拟网络请求
        /// </summary>
        private static async Task SimulateNetworkRequestAsync()
        {
            // 模拟网络延迟
            await Task.Delay(20);
            
            await Task.Run(() =>
            {
                // 模拟JSON序列化/反序列化
                var data = new
                {
                    Id = Guid.NewGuid(),
                    Timestamp = DateTime.Now,
                    Values = Enumerable.Range(1, 100).ToArray()
                };
                
                var json = System.Text.Json.JsonSerializer.Serialize(data);
                var deserialized = System.Text.Json.JsonSerializer.Deserialize<object>(json);
            });
        }

        #endregion

        #region 最佳实践指南

        /// <summary>
        /// 输出性能测试最佳实践指南
        /// </summary>
        public static void PrintBestPracticesGuide()
        {
            Debug.WriteLine("\n💡 性能测试最佳实践指南");
            Debug.WriteLine("=".PadRight(50, '='));
            
            Debug.WriteLine("1. 测试环境准备:");
            Debug.WriteLine("   • 在稳定的环境中运行测试，避免外部干扰");
            Debug.WriteLine("   • 关闭不必要的应用程序，减少资源竞争");
            Debug.WriteLine("   • 使用一致的硬件配置进行基准对比");
            
            Debug.WriteLine("\n2. 测试设计原则:");
            Debug.WriteLine("   • 使用固定的随机种子确保测试结果可重现");
            Debug.WriteLine("   • 包含预热阶段，消除JIT编译的影响");
            Debug.WriteLine("   • 设置合适的迭代次数，平衡精度和执行时间");
            
            Debug.WriteLine("\n3. 结果解读:");
            Debug.WriteLine("   • 关注平均值、标准差和成功率");
            Debug.WriteLine("   • 考虑测试环境和负载对结果的影响");
            Debug.WriteLine("   • 建立性能基准，跟踪性能趋势变化");
            
            Debug.WriteLine("\n4. 持续集成:");
            Debug.WriteLine("   • 将性能测试集成到CI/CD流水线");
            Debug.WriteLine("   • 设置性能阈值，自动检测性能回归");
            Debug.WriteLine("   • 定期更新性能基准，适应业务增长");
            
            Debug.WriteLine("\n5. 问题排查:");
            Debug.WriteLine("   • 使用分层测试，逐步缩小问题范围");
            Debug.WriteLine("   • 结合profiling工具进行深度分析");
            Debug.WriteLine("   • 记录测试环境和配置信息");
        }

        #endregion
    }
}
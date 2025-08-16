using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LYBT.Desktop.Core.Testing.Performance
{
    /// <summary>
    /// 性能测试执行器
    /// 协调各种性能测试的执行并生成报告
    /// </summary>
    public class PerformanceTestRunner
    {
        private readonly IPerformanceBenchmark _benchmark;
        private readonly VirtualizationPerformanceTest _virtualizationTest;

        public PerformanceTestRunner()
        {
            _benchmark = new PerformanceBenchmark();
            _virtualizationTest = new VirtualizationPerformanceTest(_benchmark);
        }

        /// <summary>
        /// 执行完整的性能测试套件
        /// </summary>
        /// <param name="outputPath">报告输出路径</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>测试执行结果</returns>
        public async Task<PerformanceTestExecutionResult> RunCompleteTestSuiteAsync(
            string? outputPath = null, 
            CancellationToken cancellationToken = default)
        {
            var executionResult = new PerformanceTestExecutionResult
            {
                StartTime = DateTime.Now
            };

            Debug.WriteLine("🚀 开始执行完整性能测试套件...");
            
            try
            {
                // 1. 系统环境信息收集
                executionResult.SystemInfo = CollectSystemInfo();
                Debug.WriteLine("📋 系统信息收集完成");

                // 2. 虚拟化性能测试
                Debug.WriteLine("🔄 开始虚拟化性能测试...");
                executionResult.VirtualizationReport = await _virtualizationTest.RunCompleteTestSuiteAsync();
                Debug.WriteLine("✅ 虚拟化性能测试完成");

                // 3. 基础性能基准测试
                Debug.WriteLine("⚡ 开始基础性能基准测试...");
                executionResult.BasicBenchmarkResults = await RunBasicBenchmarkTestsAsync();
                Debug.WriteLine("✅ 基础性能基准测试完成");

                executionResult.EndTime = DateTime.Now;
                executionResult.TotalDuration = executionResult.EndTime - executionResult.StartTime;
                executionResult.Success = true;

                // 4. 生成综合报告
                Debug.WriteLine("📄 生成性能测试报告...");
                executionResult.ComprehensiveReport = GenerateComprehensiveReport(executionResult);

                // 5. 保存报告到文件
                if (!string.IsNullOrEmpty(outputPath))
                {
                    await SaveReportToFileAsync(executionResult.ComprehensiveReport, outputPath);
                    executionResult.ReportPath = outputPath;
                    Debug.WriteLine($"💾 报告已保存到: {outputPath}");
                }

                Debug.WriteLine("🎉 性能测试套件执行完成!");
            }
            catch (OperationCanceledException)
            {
                executionResult.Success = false;
                executionResult.ErrorMessage = "测试被用户取消";
                Debug.WriteLine("⏹️ 测试执行被取消");
            }
            catch (Exception ex)
            {
                executionResult.Success = false;
                executionResult.ErrorMessage = ex.Message;
                executionResult.Exception = ex;
                Debug.WriteLine($"❌ 测试执行失败: {ex.Message}");
            }

            return executionResult;
        }

        /// <summary>
        /// 快速性能检查 - 用于开发时的快速验证
        /// </summary>
        public async Task<QuickPerformanceCheckResult> RunQuickPerformanceCheckAsync()
        {
            Debug.WriteLine("⚡ 开始快速性能检查...");

            var result = new QuickPerformanceCheckResult
            {
                StartTime = DateTime.Now
            };

            try
            {
                // 基本渲染性能测试 - 小数据集
                var renderingTest = new VirtualizationPerformanceTest(_benchmark);
                var quickTest = await renderingTest.TestDataRenderingPerformanceAsync();

                result.RenderingPerformance = quickTest;
                result.PerformanceGrade = DeterminePerformanceGrade(quickTest);
                result.Success = true;

                Debug.WriteLine($"✅ 快速性能检查完成 - 等级: {result.PerformanceGrade}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                Debug.WriteLine($"❌ 快速性能检查失败: {ex.Message}");
            }

            result.EndTime = DateTime.Now;
            result.Duration = result.EndTime - result.StartTime;

            return result;
        }

        /// <summary>
        /// 运行基础性能基准测试
        /// </summary>
        private async Task<BasicBenchmarkResults> RunBasicBenchmarkTestsAsync()
        {
            var results = new BasicBenchmarkResults();

            // CPU密集型任务测试
            results.CpuIntensiveTest = await _benchmark.RunBenchmarkAsync(
                "CPU密集型任务",
                async () =>
                {
                    await Task.Run(() =>
                    {
                        // 模拟CPU密集型计算
                        var sum = 0.0;
                        for (int i = 0; i < 100000; i++)
                        {
                            sum += Math.Sqrt(i) * Math.Sin(i);
                        }
                    });
                },
                100);

            // 内存分配测试
            results.MemoryAllocationTest = await _benchmark.RunBenchmarkAsync(
                "内存分配测试",
                async () =>
                {
                    await Task.Run(() =>
                    {
                        // 分配和释放大量小对象
                        var objects = new object[10000];
                        for (int i = 0; i < objects.Length; i++)
                        {
                            objects[i] = new { Index = i, Value = Guid.NewGuid().ToString() };
                        }
                    });
                },
                50);

            // 异步操作测试
            results.AsyncOperationTest = await _benchmark.RunBenchmarkAsync(
                "异步操作测试",
                async () =>
                {
                    // 模拟异步IO操作
                    await Task.Delay(1);
                    
                    // 并发任务
                    var tasks = new Task[10];
                    for (int i = 0; i < tasks.Length; i++)
                    {
                        tasks[i] = Task.Run(async () => await Task.Delay(1));
                    }
                    await Task.WhenAll(tasks);
                },
                30);

            return results;
        }

        /// <summary>
        /// 收集系统信息
        /// </summary>
        private SystemInfo CollectSystemInfo()
        {
            var info = new SystemInfo();

            try
            {
                info.MachineName = Environment.MachineName;
                info.OperatingSystem = Environment.OSVersion.ToString();
                info.ProcessorCount = Environment.ProcessorCount;
                info.WorkingSet = Environment.WorkingSet / 1024 / 1024; // MB
                info.DotNetVersion = Environment.Version.ToString();
                info.Is64BitProcess = Environment.Is64BitProcess;
                info.Is64BitOperatingSystem = Environment.Is64BitOperatingSystem;

                // 获取当前进程信息
                using (var process = Process.GetCurrentProcess())
                {
                    info.ProcessName = process.ProcessName;
                    info.ProcessId = process.Id;
                    info.StartTime = process.StartTime;
                    info.WorkingSetMB = process.WorkingSet64 / 1024 / 1024;
                    info.PrivateMemoryMB = process.PrivateMemorySize64 / 1024 / 1024;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"收集系统信息时出错: {ex.Message}");
            }

            return info;
        }

        /// <summary>
        /// 生成综合报告
        /// </summary>
        private string GenerateComprehensiveReport(PerformanceTestExecutionResult result)
        {
            var report = new System.Text.StringBuilder();

            // 报告头部
            report.AppendLine("🚀 LYBT 系统性能测试综合报告");
            report.AppendLine("=".PadRight(80, '='));
            report.AppendLine($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"测试耗时: {result.TotalDuration.TotalMinutes:F1} 分钟");
            report.AppendLine($"测试状态: {(result.Success ? "✅ 成功" : "❌ 失败")}");
            
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                report.AppendLine($"错误信息: {result.ErrorMessage}");
            }
            
            report.AppendLine();

            // 系统环境信息
            if (result.SystemInfo != null)
            {
                report.AppendLine("🖥️ 系统环境信息");
                report.AppendLine("-".PadRight(50, '-'));
                report.AppendLine($"机器名称: {result.SystemInfo.MachineName}");
                report.AppendLine($"操作系统: {result.SystemInfo.OperatingSystem}");
                report.AppendLine($"处理器核心数: {result.SystemInfo.ProcessorCount}");
                report.AppendLine($"系统架构: {(result.SystemInfo.Is64BitOperatingSystem ? "64位" : "32位")}");
                report.AppendLine($"进程架构: {(result.SystemInfo.Is64BitProcess ? "64位" : "32位")}");
                report.AppendLine($".NET版本: {result.SystemInfo.DotNetVersion}");
                report.AppendLine($"工作集: {result.SystemInfo.WorkingSetMB} MB");
                report.AppendLine($"私有内存: {result.SystemInfo.PrivateMemoryMB} MB");
                report.AppendLine();
            }

            // 虚拟化性能测试结果
            if (result.VirtualizationReport != null)
            {
                report.AppendLine("🎯 虚拟化性能测试结果");
                report.AppendLine("-".PadRight(50, '-'));
                report.AppendLine(result.VirtualizationReport.GenerateSummaryReport());
                report.AppendLine();
            }

            // 基础基准测试结果
            if (result.BasicBenchmarkResults != null)
            {
                report.AppendLine("⚡ 基础性能基准测试");
                report.AppendLine("-".PadRight(50, '-'));
                
                var basicResults = new[]
                {
                    result.BasicBenchmarkResults.CpuIntensiveTest,
                    result.BasicBenchmarkResults.MemoryAllocationTest,
                    result.BasicBenchmarkResults.AsyncOperationTest
                };

                report.AppendLine(_benchmark.GeneratePerformanceReport(basicResults));
                report.AppendLine();
            }

            // 性能优化建议
            report.AppendLine("💡 性能优化建议");
            report.AppendLine("-".PadRight(50, '-'));
            report.AppendLine(GenerateOptimizationRecommendations(result));
            report.AppendLine();

            // 报告尾部
            report.AppendLine("📋 报告说明");
            report.AppendLine("-".PadRight(50, '-'));
            report.AppendLine("• 本报告基于当前系统环境和测试数据生成");
            report.AppendLine("• 性能结果可能因系统负载、硬件配置等因素而变化");
            report.AppendLine("• 建议在不同环境下多次测试以获得更准确的性能评估");
            report.AppendLine("• 优化建议仅供参考，具体实施需结合实际业务场景");

            return report.ToString();
        }

        /// <summary>
        /// 生成性能优化建议
        /// </summary>
        private string GenerateOptimizationRecommendations(PerformanceTestExecutionResult result)
        {
            var recommendations = new System.Text.StringBuilder();

            // 基于虚拟化测试结果的建议
            if (result.VirtualizationReport?.DataRenderingResults != null)
            {
                var improvement = result.VirtualizationReport.DataRenderingResults.ImprovementPercentage;
                
                if (improvement > 50)
                {
                    recommendations.AppendLine("✅ 虚拟化优化效果显著，建议在所有大数据量列表中使用虚拟化");
                }
                else if (improvement > 20)
                {
                    recommendations.AppendLine("⚠️ 虚拟化有一定效果，建议在数据量超过1000项的列表中使用");
                }
                else
                {
                    recommendations.AppendLine("❌ 虚拟化优化效果不明显，可能需要检查实现方式或数据结构");
                }
            }

            // 基于内存使用的建议
            if (result.VirtualizationReport?.MemoryUsageResults != null)
            {
                var memorySaved = result.VirtualizationReport.MemoryUsageResults.TraditionalMemory.MemoryIncreaseMB 
                                - result.VirtualizationReport.MemoryUsageResults.VirtualizedMemory.MemoryIncreaseMB;
                
                if (memorySaved > 100)
                {
                    recommendations.AppendLine("💾 虚拟化显著降低内存使用，强烈建议在内存敏感场景中使用");
                }
                else if (memorySaved > 20)
                {
                    recommendations.AppendLine("💾 虚拟化有效减少内存占用，推荐在移动设备或低内存环境中使用");
                }
            }

            // 基于缓存性能的建议
            if (result.VirtualizationReport?.CachePerformanceResults?.CacheStatistics != null)
            {
                var hitRatio = result.VirtualizationReport.CachePerformanceResults.CacheStatistics.HitRatio;
                
                if (hitRatio > 0.8)
                {
                    recommendations.AppendLine("🗄️ 缓存命中率良好，当前缓存策略有效");
                }
                else if (hitRatio > 0.5)
                {
                    recommendations.AppendLine("🗄️ 缓存命中率中等，建议优化预加载策略或调整缓存大小");
                }
                else
                {
                    recommendations.AppendLine("🗄️ 缓存命中率较低，建议检查数据访问模式并优化缓存策略");
                }
            }

            // 通用性能建议
            recommendations.AppendLine();
            recommendations.AppendLine("🔧 通用优化建议:");
            recommendations.AppendLine("• 对于超过500项的数据列表，启用虚拟化");
            recommendations.AppendLine("• 使用数据虚拟化时，配置合适的缓存大小和过期时间");
            recommendations.AppendLine("• 避免在UI线程中执行耗时操作");
            recommendations.AppendLine("• 定期监控内存使用情况，及时清理不必要的缓存");
            recommendations.AppendLine("• 考虑使用数据分页减少单次加载的数据量");

            return recommendations.ToString();
        }

        /// <summary>
        /// 保存报告到文件
        /// </summary>
        private async Task SaveReportToFileAsync(string report, string filePath)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllTextAsync(filePath, report, System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"保存报告失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 确定性能等级
        /// </summary>
        private string DeterminePerformanceGrade(ComparisonTestResult? result)
        {
            if (result == null) return "未知";

            var improvement = result.ImprovementPercentage;
            return improvement switch
            {
                > 80 => "优秀 (A+)",
                > 60 => "良好 (A)",
                > 40 => "中等 (B+)",
                > 20 => "一般 (B)",
                > 0 => "较差 (C)",
                _ => "很差 (D)"
            };
        }
    }

    #region 结果模型

    /// <summary>
    /// 性能测试执行结果
    /// </summary>
    public class PerformanceTestExecutionResult
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public Exception? Exception { get; set; }
        public string? ReportPath { get; set; }

        public SystemInfo? SystemInfo { get; set; }
        public VirtualizationTestReport? VirtualizationReport { get; set; }
        public BasicBenchmarkResults? BasicBenchmarkResults { get; set; }
        public string ComprehensiveReport { get; set; } = string.Empty;
    }

    /// <summary>
    /// 快速性能检查结果
    /// </summary>
    public class QuickPerformanceCheckResult
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }

        public ComparisonTestResult? RenderingPerformance { get; set; }
        public string PerformanceGrade { get; set; } = "未知";
    }

    /// <summary>
    /// 系统信息
    /// </summary>
    public class SystemInfo
    {
        public string MachineName { get; set; } = string.Empty;
        public string OperatingSystem { get; set; } = string.Empty;
        public int ProcessorCount { get; set; }
        public long WorkingSet { get; set; }
        public string DotNetVersion { get; set; } = string.Empty;
        public bool Is64BitProcess { get; set; }
        public bool Is64BitOperatingSystem { get; set; }

        public string ProcessName { get; set; } = string.Empty;
        public int ProcessId { get; set; }
        public DateTime StartTime { get; set; }
        public long WorkingSetMB { get; set; }
        public long PrivateMemoryMB { get; set; }
    }

    /// <summary>
    /// 基础基准测试结果
    /// </summary>
    public class BasicBenchmarkResults
    {
        public PerformanceTestResult CpuIntensiveTest { get; set; } = new();
        public PerformanceTestResult MemoryAllocationTest { get; set; } = new();
        public PerformanceTestResult AsyncOperationTest { get; set; } = new();
    }

    #endregion
}
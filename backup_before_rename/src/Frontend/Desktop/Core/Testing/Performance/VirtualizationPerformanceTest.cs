using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using LYBT.Desktop.Core.Services.Performance;

namespace LYBT.Desktop.Core.Testing.Performance
{
    /// <summary>
    /// 虚拟化性能专项测试
    /// 对比传统控件与虚拟化控件的性能差异
    /// </summary>
    public class VirtualizationPerformanceTest
    {
        private readonly IPerformanceBenchmark _benchmark;
        private readonly IDataPreloadService _preloadService;

        public VirtualizationPerformanceTest(IPerformanceBenchmark benchmark)
        {
            _benchmark = benchmark ?? throw new ArgumentNullException(nameof(benchmark));
            _preloadService = new DataPreloadService();
        }

        /// <summary>
        /// 执行完整的虚拟化性能测试套件
        /// </summary>
        public async Task<VirtualizationTestReport> RunCompleteTestSuiteAsync()
        {
            var report = new VirtualizationTestReport
            {
                TestStartTime = DateTime.Now
            };

            Debug.WriteLine("🚀 开始虚拟化性能测试套件...");

            try
            {
                // 1. 数据渲染性能测试
                report.DataRenderingResults = await TestDataRenderingPerformanceAsync();
                
                // 2. 滚动性能测试
                report.ScrollingResults = await TestScrollingPerformanceAsync();
                
                // 3. 内存使用测试
                report.MemoryUsageResults = await TestMemoryUsageAsync();
                
                // 4. 缓存性能测试
                report.CachePerformanceResults = await TestCachePerformanceAsync();
                
                // 5. 大数据集测试
                report.LargeDataSetResults = await TestLargeDataSetPerformanceAsync();

                report.TestEndTime = DateTime.Now;
                report.TotalTestDuration = report.TestEndTime - report.TestStartTime;

                Debug.WriteLine("✅ 虚拟化性能测试套件完成!");
            }
            catch (Exception ex)
            {
                report.TestError = ex.Message;
                Debug.WriteLine($"❌ 测试套件执行失败: {ex.Message}");
            }

            return report;
        }

        /// <summary>
        /// 测试数据渲染性能 - 传统 vs 虚拟化
        /// </summary>
        public async Task<ComparisonTestResult> TestDataRenderingPerformanceAsync()
        {
            Debug.WriteLine("📊 测试数据渲染性能...");

            var testData = GenerateTestData(1000); // 1000条测试数据

            // 传统ItemsControl测试
            async Task TraditionalRendering()
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var itemsControl = new ItemsControl();
                    var collection = new ObservableCollection<TestDataItem>(testData);
                    itemsControl.ItemsSource = collection;
                    
                    // 模拟数据绑定和渲染
                    var binding = new Binding("ItemsSource") { Source = itemsControl };
                    BindingOperations.SetBinding(itemsControl, ItemsControl.ItemsSourceProperty, binding);
                    
                    // 强制布局更新
                    itemsControl.Measure(new Size(800, 600));
                    itemsControl.Arrange(new Rect(0, 0, 800, 600));
                });
            }

            // 虚拟化ListBox测试
            async Task VirtualizedRendering()
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var listBox = new ListBox();
                    
                    // 配置虚拟化
                    VirtualizingPanel.SetIsVirtualizing(listBox, true);
                    VirtualizingPanel.SetVirtualizationMode(listBox, VirtualizationMode.Recycling);
                    ScrollViewer.SetCanContentScroll(listBox, true);
                    
                    var collection = new ObservableCollection<TestDataItem>(testData);
                    listBox.ItemsSource = collection;
                    
                    // 模拟数据绑定和渲染
                    var binding = new Binding("ItemsSource") { Source = listBox };
                    BindingOperations.SetBinding(listBox, ListBox.ItemsSourceProperty, binding);
                    
                    // 强制布局更新
                    listBox.Measure(new Size(800, 600));
                    listBox.Arrange(new Rect(0, 0, 800, 600));
                });
            }

            return await _benchmark.CompareImplementationsAsync(
                TraditionalRendering,
                VirtualizedRendering,
                "数据渲染性能对比",
                20); // 20次迭代
        }

        /// <summary>
        /// 测试滚动性能
        /// </summary>
        private async Task<ComparisonTestResult> TestScrollingPerformanceAsync()
        {
            Debug.WriteLine("🖱️ 测试滚动性能...");

            var testData = GenerateTestData(5000); // 5000条数据测试滚动

            async Task TraditionalScrolling()
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var scrollViewer = new ScrollViewer();
                    var itemsControl = new ItemsControl();
                    itemsControl.ItemsSource = new ObservableCollection<TestDataItem>(testData);
                    scrollViewer.Content = itemsControl;

                    // 模拟滚动操作
                    for (int i = 0; i < 100; i += 10)
                    {
                        scrollViewer.ScrollToVerticalOffset(i * 50);
                        // 强制布局更新
                        scrollViewer.UpdateLayout();
                    }
                });
            }

            async Task VirtualizedScrolling()
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var listBox = new ListBox();
                    VirtualizingPanel.SetIsVirtualizing(listBox, true);
                    VirtualizingPanel.SetVirtualizationMode(listBox, VirtualizationMode.Recycling);
                    listBox.ItemsSource = new ObservableCollection<TestDataItem>(testData);

                    // 模拟滚动操作
                    for (int i = 0; i < 100; i += 10)
                    {
                        listBox.ScrollIntoView(testData[Math.Min(i * 50, testData.Count - 1)]);
                        listBox.UpdateLayout();
                    }
                });
            }

            return await _benchmark.CompareImplementationsAsync(
                TraditionalScrolling,
                VirtualizedScrolling,
                "滚动性能对比",
                10);
        }

        /// <summary>
        /// 测试内存使用情况
        /// </summary>
        private async Task<MemoryComparisonTestResult> TestMemoryUsageAsync()
        {
            Debug.WriteLine("💾 测试内存使用情况...");

            var largeDataSet = GenerateTestData(10000); // 10000条数据

            var traditionalMemory = await _benchmark.MeasureMemoryUsageAsync(async () =>
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var itemsControl = new ItemsControl();
                    itemsControl.ItemsSource = new ObservableCollection<TestDataItem>(largeDataSet);
                    
                    // 强制渲染所有项目
                    itemsControl.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    itemsControl.Arrange(new Rect(itemsControl.DesiredSize));
                });
            });

            var virtualizedMemory = await _benchmark.MeasureMemoryUsageAsync(async () =>
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var listBox = new ListBox();
                    VirtualizingPanel.SetIsVirtualizing(listBox, true);
                    VirtualizingPanel.SetVirtualizationMode(listBox, VirtualizationMode.Recycling);
                    listBox.ItemsSource = new ObservableCollection<TestDataItem>(largeDataSet);
                    
                    // 只渲染可视区域
                    listBox.Measure(new Size(800, 600));
                    listBox.Arrange(new Rect(0, 0, 800, 600));
                });
            });

            return new MemoryComparisonTestResult
            {
                TraditionalMemory = traditionalMemory,
                VirtualizedMemory = virtualizedMemory,
                DataSetSize = largeDataSet.Count
            };
        }

        /// <summary>
        /// 测试缓存性能
        /// </summary>
        private async Task<CachePerformanceTestResult> TestCachePerformanceAsync()
        {
            Debug.WriteLine("🗄️ 测试缓存性能...");

            var result = new CachePerformanceTestResult();

            // 配置缓存
            _preloadService.ConfigureCache(maxMemoryMB: 100, cacheExpirationMinutes: 5, preloadMultiplier: 2.0);

            // 测试数据提供器
            var testDataProvider = CreateTestDataProvider();

            // 测试缓存命中性能
            var cacheHitTest = await _benchmark.RunBenchmarkAsync(
                "缓存命中测试",
                async () =>
                {
                    // 预填充缓存
                    await _preloadService.PreloadDataAsync("test_key", 0, 100, testDataProvider);
                    
                    // 测试缓存获取
                    for (int i = 0; i < 100; i++)
                    {
                        var item = _preloadService.GetCachedItem<TestDataItem>("test_key", i);
                    }
                },
                50);

            result.CacheHitPerformance = cacheHitTest;

            // 测试缓存未命中性能
            var cacheMissTest = await _benchmark.RunBenchmarkAsync(
                "缓存未命中测试",
                async () =>
                {
                    _preloadService.ClearExpiredCache("test_key");
                    
                    // 直接从数据提供器获取
                    var data = await testDataProvider(0, 100, CancellationToken.None);
                },
                20);

            result.CacheMissPerformance = cacheMissTest;

            // 获取缓存统计
            result.CacheStatistics = _preloadService.GetCacheStatistics();

            return result;
        }

        /// <summary>
        /// 测试大数据集性能
        /// </summary>
        private async Task<LargeDataSetTestResult> TestLargeDataSetPerformanceAsync()
        {
            Debug.WriteLine("📈 测试大数据集性能...");

            var dataSizes = new[] { 1000, 5000, 10000, 50000, 100000 };
            var results = new List<DataSetSizeResult>();

            foreach (var size in dataSizes)
            {
                Debug.WriteLine($"测试数据集大小: {size:N0} 项");

                var testData = GenerateTestData(size);
                
                var sizeResult = new DataSetSizeResult
                {
                    DataSetSize = size
                };

                // 测试传统控件
                sizeResult.TraditionalResult = await _benchmark.RunBenchmarkAsync(
                    $"传统控件-{size}项",
                    async () =>
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            var itemsControl = new ItemsControl();
                            itemsControl.ItemsSource = new ObservableCollection<TestDataItem>(testData.Take(Math.Min(1000, size)));
                            itemsControl.Measure(new Size(800, 600));
                        });
                    },
                    5); // 大数据集减少迭代次数

                // 测试虚拟化控件
                sizeResult.VirtualizedResult = await _benchmark.RunBenchmarkAsync(
                    $"虚拟化控件-{size}项",
                    async () =>
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            var listBox = new ListBox();
                            VirtualizingPanel.SetIsVirtualizing(listBox, true);
                            VirtualizingPanel.SetVirtualizationMode(listBox, VirtualizationMode.Recycling);
                            listBox.ItemsSource = new ObservableCollection<TestDataItem>(testData);
                            listBox.Measure(new Size(800, 600));
                        });
                    },
                    10);

                results.Add(sizeResult);

                // 大数据集之间的小憩，避免内存压力
                if (size >= 10000)
                {
                    GC.Collect();
                    await Task.Delay(1000);
                }
            }

            return new LargeDataSetTestResult
            {
                DataSetResults = results
            };
        }

        #region 辅助方法

        private List<TestDataItem> GenerateTestData(int count)
        {
            var random = new Random(12345); // 固定种子确保测试一致性
            var result = new List<TestDataItem>();

            for (int i = 0; i < count; i++)
            {
                result.Add(new TestDataItem
                {
                    Id = Guid.NewGuid(),
                    Name = $"测试项目_{i:D6}",
                    Description = $"这是第{i + 1}个测试项目的描述信息，用于模拟真实数据的复杂度。",
                    Price = (decimal)(random.NextDouble() * 1000),
                    IsEnabled = random.Next(2) == 0,
                    Category = $"分类_{random.Next(1, 11)}",
                    Tags = Enumerable.Range(0, random.Next(1, 6))
                        .Select(j => $"标签{j + 1}")
                        .ToList(),
                    CreatedTime = DateTime.Now.AddDays(-random.Next(0, 365)),
                    Value = random.Next(0, 1000)
                });
            }

            return result;
        }

        private Func<int, int, CancellationToken, Task<IList<object>>> CreateTestDataProvider()
        {
            var allData = GenerateTestData(10000).Cast<object>().ToList();

            return async (startIndex, count, cancellationToken) =>
            {
                // 模拟网络延迟
                await Task.Delay(10, cancellationToken);

                var endIndex = Math.Min(startIndex + count, allData.Count);
                var result = new List<object>();

                for (int i = startIndex; i < endIndex; i++)
                {
                    if (i < allData.Count)
                    {
                        result.Add(allData[i]);
                    }
                }

                return result;
            };
        }

        #endregion
    }

    #region 测试数据模型

    /// <summary>
    /// 测试数据项
    /// </summary>
    public class TestDataItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsEnabled { get; set; }
        public string Category { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public DateTime CreatedTime { get; set; }
        public int Value { get; set; }
    }

    #endregion

    #region 测试结果模型

    /// <summary>
    /// 虚拟化测试报告
    /// </summary>
    public class VirtualizationTestReport
    {
        public DateTime TestStartTime { get; set; }
        public DateTime TestEndTime { get; set; }
        public TimeSpan TotalTestDuration { get; set; }
        public string? TestError { get; set; }

        public ComparisonTestResult? DataRenderingResults { get; set; }
        public ComparisonTestResult? ScrollingResults { get; set; }
        public MemoryComparisonTestResult? MemoryUsageResults { get; set; }
        public CachePerformanceTestResult? CachePerformanceResults { get; set; }
        public LargeDataSetTestResult? LargeDataSetResults { get; set; }

        public string GenerateSummaryReport()
        {
            var report = new System.Text.StringBuilder();

            report.AppendLine("🎯 虚拟化性能测试报告汇总");
            report.AppendLine("=".PadRight(50, '='));
            report.AppendLine($"测试时间: {TestStartTime:yyyy-MM-dd HH:mm:ss} - {TestEndTime:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"测试耗时: {TotalTestDuration.TotalMinutes:F1} 分钟");
            report.AppendLine();

            if (!string.IsNullOrEmpty(TestError))
            {
                report.AppendLine($"❌ 测试错误: {TestError}");
                return report.ToString();
            }

            // 数据渲染性能汇总
            if (DataRenderingResults != null)
            {
                report.AppendLine("📊 数据渲染性能");
                report.AppendLine($"性能提升: {DataRenderingResults.ImprovementPercentage:F1}% ({DataRenderingResults.Conclusion})");
                report.AppendLine($"基准耗时: {DataRenderingResults.BaselineResult.AverageExecutionTimeMs:F2} ms");
                report.AppendLine($"优化耗时: {DataRenderingResults.OptimizedResult.AverageExecutionTimeMs:F2} ms");
                report.AppendLine();
            }

            // 滚动性能汇总
            if (ScrollingResults != null)
            {
                report.AppendLine("🖱️ 滚动性能");
                report.AppendLine($"性能提升: {ScrollingResults.ImprovementPercentage:F1}% ({ScrollingResults.Conclusion})");
                report.AppendLine($"基准耗时: {ScrollingResults.BaselineResult.AverageExecutionTimeMs:F2} ms");
                report.AppendLine($"优化耗时: {ScrollingResults.OptimizedResult.AverageExecutionTimeMs:F2} ms");
                report.AppendLine();
            }

            // 内存使用汇总
            if (MemoryUsageResults != null)
            {
                report.AppendLine("💾 内存使用");
                report.AppendLine($"传统方式: {MemoryUsageResults.TraditionalMemory.MemoryIncreaseMB:F1} MB");
                report.AppendLine($"虚拟化方式: {MemoryUsageResults.VirtualizedMemory.MemoryIncreaseMB:F1} MB");
                report.AppendLine($"内存节省: {MemoryUsageResults.TraditionalMemory.MemoryIncreaseMB - MemoryUsageResults.VirtualizedMemory.MemoryIncreaseMB:F1} MB");
                report.AppendLine();
            }

            // 缓存性能汇总
            if (CachePerformanceResults?.CacheStatistics != null)
            {
                report.AppendLine("🗄️ 缓存性能");
                report.AppendLine($"缓存命中率: {CachePerformanceResults.CacheStatistics.HitRatio:P2}");
                report.AppendLine($"缓存内存使用: {CachePerformanceResults.CacheStatistics.MemoryUsageMB:F1} MB");
                report.AppendLine();
            }

            report.AppendLine("✅ 测试完成");

            return report.ToString();
        }
    }

    /// <summary>
    /// 内存对比测试结果
    /// </summary>
    public class MemoryComparisonTestResult
    {
        public MemoryUsageResult TraditionalMemory { get; set; } = new();
        public MemoryUsageResult VirtualizedMemory { get; set; } = new();
        public int DataSetSize { get; set; }
    }

    /// <summary>
    /// 缓存性能测试结果
    /// </summary>
    public class CachePerformanceTestResult
    {
        public PerformanceTestResult CacheHitPerformance { get; set; } = new();
        public PerformanceTestResult CacheMissPerformance { get; set; } = new();
        public CacheStatistics CacheStatistics { get; set; } = new();
    }

    /// <summary>
    /// 大数据集测试结果
    /// </summary>
    public class LargeDataSetTestResult
    {
        public List<DataSetSizeResult> DataSetResults { get; set; } = new();
    }

    /// <summary>
    /// 数据集大小结果
    /// </summary>
    public class DataSetSizeResult
    {
        public int DataSetSize { get; set; }
        public PerformanceTestResult TraditionalResult { get; set; } = new();
        public PerformanceTestResult VirtualizedResult { get; set; } = new();
        
        public double PerformanceImprovement => 
            TraditionalResult.AverageExecutionTimeMs > 0 
                ? ((TraditionalResult.AverageExecutionTimeMs - VirtualizedResult.AverageExecutionTimeMs) / TraditionalResult.AverageExecutionTimeMs) * 100.0 
                : 0.0;
    }

    #endregion
}
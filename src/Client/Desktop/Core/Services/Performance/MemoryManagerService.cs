using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Core.Configuration;

namespace LYBT.Desktop.Core.Services.Performance
{
    /// <summary>
    /// UltraThink Phase 5.4: 内存管理服务实现
    /// 提供智能内存管理、泄漏检测和自动清理
    /// </summary>
    public class MemoryManagerService : IMemoryManagerService, IDisposable
    {
        private readonly ILogger<MemoryManagerService> _logger;
        private readonly IAppConfiguration _configuration;
        private readonly Timer _monitoringTimer;
        private readonly ConcurrentDictionary<string, IMemoryCleanable> _cleanableComponents = new();
        private readonly ConcurrentDictionary<string, LeakDetectionSession> _leakDetectionSessions = new();
        
        private MemoryThresholds _thresholds = new();
        private readonly object _lockObject = new object();
        private DateTime _lastCleanup = DateTime.MinValue;
        private int _consecutiveGC2Collections;
        private long _previousTotalMemory;

        public event EventHandler<MemoryWarningEventArgs>? MemoryWarning;
        public event EventHandler<MemoryCleanupEventArgs>? MemoryCleanup;

        public MemoryManagerService(ILogger<MemoryManagerService> logger, IAppConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            // 从配置加载阈值
            LoadThresholdsFromConfiguration();

            // 启动内存监控定时器
            _monitoringTimer = new Timer(MonitorMemory, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));

            _logger.LogInformation("内存管理服务已启动");
        }

        public async Task<MemoryCleanupResult> CleanupAsync(bool force = false)
        {
            var result = new MemoryCleanupResult
            {
                MemoryBeforeCleanup = GC.GetTotalMemory(false)
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // 检查是否需要清理
                if (!force && !ShouldPerformCleanup())
                {
                    result.Success = true;
                    result.ErrorMessage = "无需清理";
                    return result;
                }

                _logger.LogInformation("开始内存清理，强制: {Force}", force);

                // 清理已注册的组件
                await CleanupRegisteredComponentsAsync(result);

                // 执行GC清理
                if (force || result.ComponentsCleaned > 0)
                {
                    PerformGarbageCollection();
                }

                result.MemoryAfterCleanup = GC.GetTotalMemory(true);
                result.Success = true;
                _lastCleanup = DateTime.Now;

                _logger.LogInformation("内存清理完成，释放内存: {MemoryFreed:N0} bytes，清理组件: {ComponentsCleaned}",
                    result.MemoryFreed, result.ComponentsCleaned);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "内存清理失败");
            }
            finally
            {
                stopwatch.Stop();
                result.CleanupDuration = stopwatch.Elapsed;

                // 触发清理事件
                OnMemoryCleanup(result, force);
            }

            return result;
        }

        public void RegisterCleanableComponent(IMemoryCleanable component, string componentName)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));
            if (string.IsNullOrEmpty(componentName)) throw new ArgumentNullException(nameof(componentName));

            _cleanableComponents.AddOrUpdate(componentName, component, (key, existing) => component);
            _logger.LogDebug("已注册可清理组件: {ComponentName}", componentName);
        }

        public void UnregisterComponent(string componentName)
        {
            if (_cleanableComponents.TryRemove(componentName, out _))
            {
                _logger.LogDebug("已注销组件: {ComponentName}", componentName);
            }
        }

        public void StartLeakDetection(string sessionName)
        {
            var session = new LeakDetectionSession
            {
                SessionName = sessionName,
                StartTime = DateTime.Now,
                StartMemory = GC.GetTotalMemory(false),
                Snapshots = new List<MemorySnapshot>()
            };

            // 创建初始快照
            session.Snapshots.Add(CreateMemorySnapshot());

            _leakDetectionSessions.AddOrUpdate(sessionName, session, (key, existing) => session);
            _logger.LogInformation("开始内存泄漏检测: {SessionName}", sessionName);
        }

        public MemoryLeakReport StopLeakDetection(string sessionName)
        {
            if (!_leakDetectionSessions.TryRemove(sessionName, out var session))
            {
                throw new InvalidOperationException($"未找到泄漏检测会话: {sessionName}");
            }

            session.EndTime = DateTime.Now;
            session.EndMemory = GC.GetTotalMemory(false);

            // 创建最终快照
            session.Snapshots.Add(CreateMemorySnapshot());

            // 分析泄漏
            var report = AnalyzeMemoryLeaks(session);
            
            _logger.LogInformation("内存泄漏检测完成: {SessionName}，检测到 {LeakCount} 个疑似泄漏", 
                sessionName, report.SuspectedLeaks.Count);

            return report;
        }

        public MemoryUsageInfo GetMemoryUsage()
        {
            var process = Process.GetCurrentProcess();
            process.Refresh();

            var totalMemory = process.WorkingSet64;
            var managedMemory = GC.GetTotalMemory(false);
            var unmanagedMemory = totalMemory - managedMemory;

            var componentMemory = new Dictionary<string, long>();
            foreach (var kvp in _cleanableComponents)
            {
                try
                {
                    componentMemory[kvp.Key] = kvp.Value.GetEstimatedMemoryUsage();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "获取组件 {ComponentName} 内存使用失败", kvp.Key);
                }
            }

            return new MemoryUsageInfo
            {
                TotalMemory = totalMemory,
                ManagedMemory = managedMemory,
                UnmanagedMemory = unmanagedMemory,
                AvailableMemory = GetAvailableMemory(),
                MemoryUsagePercentage = CalculateMemoryUsagePercentage(totalMemory),
                Generation0Collections = GC.CollectionCount(0),
                Generation1Collections = GC.CollectionCount(1),
                Generation2Collections = GC.CollectionCount(2),
                ComponentMemory = componentMemory
            };
        }

        public void SetMemoryThresholds(MemoryThresholds thresholds)
        {
            _thresholds = thresholds ?? throw new ArgumentNullException(nameof(thresholds));
            _logger.LogInformation("内存阈值已更新");
        }

        #region 私有方法

        private void LoadThresholdsFromConfiguration()
        {
            _thresholds.MaxTotalMemory = _configuration.GetValue("Memory:MaxTotalMemory", _thresholds.MaxTotalMemory);
            _thresholds.MaxMemoryUsagePercentage = _configuration.GetValue("Memory:MaxUsagePercentage", _thresholds.MaxMemoryUsagePercentage);
            _thresholds.MaxComponentMemory = _configuration.GetValue("Memory:MaxComponentMemory", _thresholds.MaxComponentMemory);
        }

        private void MonitorMemory(object? state)
        {
            try
            {
                var memoryInfo = GetMemoryUsage();
                CheckMemoryThresholds(memoryInfo);
                UpdateLeakDetectionSessions(memoryInfo);
                MonitorGarbageCollection();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "内存监控失败");
            }
        }

        private void CheckMemoryThresholds(MemoryUsageInfo memoryInfo)
        {
            // 检查总内存阈值
            if (memoryInfo.TotalMemory > _thresholds.MaxTotalMemory)
            {
                OnMemoryWarning("TotalMemoryExceeded", 
                    $"总内存使用超过阈值: {FormatBytes(memoryInfo.TotalMemory)}", 
                    null, memoryInfo.TotalMemory, _thresholds.MaxTotalMemory, memoryInfo);
            }

            // 检查内存使用百分比
            if (memoryInfo.MemoryUsagePercentage > _thresholds.MaxMemoryUsagePercentage)
            {
                OnMemoryWarning("MemoryPercentageExceeded",
                    $"内存使用率超过阈值: {memoryInfo.MemoryUsagePercentage:F1}%",
                    null, (long)memoryInfo.MemoryUsagePercentage, (long)_thresholds.MaxMemoryUsagePercentage, memoryInfo);
            }

            // 检查组件内存阈值
            foreach (var kvp in memoryInfo.ComponentMemory)
            {
                if (kvp.Value > _thresholds.MaxComponentMemory)
                {
                    OnMemoryWarning("ComponentMemoryExceeded",
                        $"组件 '{kvp.Key}' 内存使用超过阈值: {FormatBytes(kvp.Value)}",
                        kvp.Key, kvp.Value, _thresholds.MaxComponentMemory, memoryInfo);
                }
            }
        }

        private void UpdateLeakDetectionSessions(MemoryUsageInfo memoryInfo)
        {
            var snapshot = CreateMemorySnapshot();
            
            foreach (var session in _leakDetectionSessions.Values)
            {
                session.Snapshots.Add(snapshot);
                
                // 限制快照数量
                if (session.Snapshots.Count > 100)
                {
                    session.Snapshots.RemoveAt(0);
                }
            }
        }

        private void MonitorGarbageCollection()
        {
            var currentGC2 = GC.CollectionCount(2);
            if (currentGC2 > _consecutiveGC2Collections)
            {
                _consecutiveGC2Collections = currentGC2;
            }

            // 检查连续的GC2收集
            if (_consecutiveGC2Collections > _thresholds.MaxConsecutiveGC2Collections)
            {
                OnMemoryWarning("ExcessiveGC2Collections",
                    $"连续的第2代垃圾收集过多: {_consecutiveGC2Collections}",
                    "GarbageCollector", _consecutiveGC2Collections, _thresholds.MaxConsecutiveGC2Collections, GetMemoryUsage());
                
                _consecutiveGC2Collections = 0; // 重置计数器
            }
        }

        private bool ShouldPerformCleanup()
        {
            var timeSinceLastCleanup = DateTime.Now - _lastCleanup;
            if (timeSinceLastCleanup < TimeSpan.FromMinutes(5)) return false;

            var memoryInfo = GetMemoryUsage();
            
            // 根据内存使用情况决定是否清理
            return memoryInfo.MemoryUsagePercentage > 70 || 
                   memoryInfo.TotalMemory > _thresholds.MaxTotalMemory * 0.8 ||
                   _consecutiveGC2Collections > 3;
        }

        private async Task CleanupRegisteredComponentsAsync(MemoryCleanupResult result)
        {
            var tasks = new List<Task>();

            foreach (var kvp in _cleanableComponents)
            {
                tasks.Add(CleanupComponentAsync(kvp.Key, kvp.Value, result));
            }

            await Task.WhenAll(tasks);
        }

        private async Task CleanupComponentAsync(string componentName, IMemoryCleanable component, MemoryCleanupResult result)
        {
            try
            {
                if (component.CanCleanup())
                {
                    await component.CleanupAsync();
                    result.CleanedComponents.Add(componentName);
                    result.ComponentsCleaned++;
                    _logger.LogDebug("已清理组件: {ComponentName}", componentName);
                }
            }
            catch (Exception ex)
            {
                result.FailedComponents.Add(componentName);
                _logger.LogWarning(ex, "清理组件失败: {ComponentName}", componentName);
            }
        }

        private static void PerformGarbageCollection()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private MemorySnapshot CreateMemorySnapshot()
        {
            var memoryInfo = GetMemoryUsage();
            return new MemorySnapshot
            {
                Timestamp = DateTime.Now,
                TotalMemory = memoryInfo.TotalMemory,
                ManagedMemory = memoryInfo.ManagedMemory,
                UnmanagedMemory = memoryInfo.UnmanagedMemory,
                Generation0Collections = memoryInfo.Generation0Collections,
                Generation1Collections = memoryInfo.Generation1Collections,
                Generation2Collections = memoryInfo.Generation2Collections,
                ComponentMemory = new Dictionary<string, long>(memoryInfo.ComponentMemory)
            };
        }

        private MemoryLeakReport AnalyzeMemoryLeaks(LeakDetectionSession session)
        {
            var report = new MemoryLeakReport
            {
                SessionName = session.SessionName,
                StartTime = session.StartTime,
                EndTime = session.EndTime,
                StartMemory = session.StartMemory,
                EndMemory = session.EndMemory,
                Snapshots = session.Snapshots
            };

            // 分析整体内存增长
            if (report.MemoryGrowth > 50 * 1024 * 1024) // 50MB增长阈值
            {
                var duration = session.EndTime - session.StartTime;
                var growthRate = report.MemoryGrowth / duration.TotalMinutes;

                if (growthRate > 1024 * 1024) // 1MB/分钟增长率
                {
                    report.SuspectedLeaks.Add(new SuspectedLeak
                    {
                        ComponentName = "System",
                        LeakType = "OverallMemoryGrowth",
                        MemoryGrowth = report.MemoryGrowth,
                        GrowthRate = growthRate,
                        Description = $"检测期间内存增长 {FormatBytes(report.MemoryGrowth)}",
                        Severity = growthRate > 5 * 1024 * 1024 ? "High" : "Medium",
                        Recommendations = new[]
                        {
                            "检查是否存在未释放的大对象",
                            "分析内存分配模式",
                            "考虑实施对象池模式"
                        }
                    });
                }
            }

            // 分析组件级别的内存泄漏
            AnalyzeComponentLeaks(session, report);

            return report;
        }

        private void AnalyzeComponentLeaks(LeakDetectionSession session, MemoryLeakReport report)
        {
            if (session.Snapshots.Count < 2) return;

            var firstSnapshot = session.Snapshots.First();
            var lastSnapshot = session.Snapshots.Last();

            foreach (var componentName in firstSnapshot.ComponentMemory.Keys)
            {
                if (!lastSnapshot.ComponentMemory.TryGetValue(componentName, out var endMemory))
                    continue;

                var startMemory = firstSnapshot.ComponentMemory[componentName];
                var memoryGrowth = endMemory - startMemory;

                if (memoryGrowth > 10 * 1024 * 1024) // 10MB组件增长阈值
                {
                    var duration = session.EndTime - session.StartTime;
                    var growthRate = memoryGrowth / duration.TotalMinutes;

                    report.SuspectedLeaks.Add(new SuspectedLeak
                    {
                        ComponentName = componentName,
                        LeakType = "ComponentMemoryGrowth",
                        MemoryGrowth = memoryGrowth,
                        GrowthRate = growthRate,
                        Description = $"组件 '{componentName}' 内存增长 {FormatBytes(memoryGrowth)}",
                        Severity = memoryGrowth > 50 * 1024 * 1024 ? "High" : "Medium",
                        Recommendations = new[]
                        {
                            $"检查组件 '{componentName}' 的资源释放",
                            "分析事件订阅是否正确取消",
                            "检查缓存策略是否合理"
                        }
                    });
                }
            }
        }

        private static long GetAvailableMemory()
        {
            try
            {
                using var process = Process.GetCurrentProcess();
                // 获取可用物理内存的简化实现
                var totalMemory = GC.GetTotalMemory(false);
                return Math.Max(totalMemory, 512 * 1024 * 1024); // 至少返回512MB
            }
            catch
            {
                return 1024 * 1024 * 1024; // 默认1GB
            }
        }

        private static double CalculateMemoryUsagePercentage(long usedMemory)
        {
            try
            {
                var availableMemory = GetAvailableMemory();
                var estimatedTotalMemory = availableMemory + usedMemory;
                return Math.Min((double)usedMemory / estimatedTotalMemory * 100, 100);
            }
            catch
            {
                return 50.0; // 默认50%
            }
        }

        private static string FormatBytes(long bytes)
        {
            const long k = 1024;
            if (bytes < k) return $"{bytes} B";
            if (bytes < k * k) return $"{bytes / k:F1} KB";
            if (bytes < k * k * k) return $"{bytes / (k * k):F1} MB";
            return $"{bytes / (k * k * k):F1} GB";
        }

        private void OnMemoryWarning(string warningType, string message, string? componentName, 
            long currentMemory, long thresholdMemory, MemoryUsageInfo memoryInfo)
        {
            var args = new MemoryWarningEventArgs
            {
                WarningType = warningType,
                Message = message,
                ComponentName = componentName,
                CurrentMemory = currentMemory,
                ThresholdMemory = thresholdMemory,
                MemoryInfo = memoryInfo
            };

            _logger.LogWarning("内存警告: {WarningType} - {Message}", warningType, message);
            MemoryWarning?.Invoke(this, args);
        }

        private void OnMemoryCleanup(MemoryCleanupResult result, bool wasForced)
        {
            var args = new MemoryCleanupEventArgs
            {
                Result = result,
                WasForced = wasForced,
                Trigger = wasForced ? "Manual" : "Automatic"
            };

            MemoryCleanup?.Invoke(this, args);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _monitoringTimer?.Dispose();
        }

        #endregion
    }

    /// <summary>
    /// 内存泄漏检测会话
    /// </summary>
    internal class LeakDetectionSession
    {
        public string SessionName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public long StartMemory { get; set; }
        public long EndMemory { get; set; }
        public List<MemorySnapshot> Snapshots { get; set; } = new();
    }
}
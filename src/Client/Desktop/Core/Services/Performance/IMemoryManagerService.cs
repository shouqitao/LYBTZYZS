using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.Desktop.Core.Services.Performance
{
    /// <summary>
    /// UltraThink Phase 5.4: 内存管理服务接口
    /// 提供智能内存管理和泄漏检测
    /// </summary>
    public interface IMemoryManagerService
    {
        /// <summary>
        /// 执行内存清理
        /// </summary>
        Task<MemoryCleanupResult> CleanupAsync(bool force = false);

        /// <summary>
        /// 注册可清理的组件
        /// </summary>
        void RegisterCleanableComponent(IMemoryCleanable component, string componentName);

        /// <summary>
        /// 注销组件
        /// </summary>
        void UnregisterComponent(string componentName);

        /// <summary>
        /// 开始内存泄漏检测
        /// </summary>
        void StartLeakDetection(string sessionName);

        /// <summary>
        /// 停止内存泄漏检测并获取报告
        /// </summary>
        MemoryLeakReport StopLeakDetection(string sessionName);

        /// <summary>
        /// 获取内存使用情况
        /// </summary>
        MemoryUsageInfo GetMemoryUsage();

        /// <summary>
        /// 设置内存阈值
        /// </summary>
        void SetMemoryThresholds(MemoryThresholds thresholds);

        /// <summary>
        /// 内存警告事件
        /// </summary>
        event EventHandler<MemoryWarningEventArgs> MemoryWarning;

        /// <summary>
        /// 内存清理事件
        /// </summary>
        event EventHandler<MemoryCleanupEventArgs> MemoryCleanup;
    }

    /// <summary>
    /// 可清理的组件接口
    /// </summary>
    public interface IMemoryCleanable
    {
        /// <summary>
        /// 执行内存清理
        /// </summary>
        Task CleanupAsync();

        /// <summary>
        /// 获取组件内存使用估算
        /// </summary>
        long GetEstimatedMemoryUsage();

        /// <summary>
        /// 检查是否可以清理
        /// </summary>
        bool CanCleanup();
    }

    /// <summary>
    /// 内存清理结果
    /// </summary>
    public class MemoryCleanupResult
    {
        public long MemoryBeforeCleanup { get; set; }
        public long MemoryAfterCleanup { get; set; }
        public long MemoryFreed => MemoryBeforeCleanup - MemoryAfterCleanup;
        public TimeSpan CleanupDuration { get; set; }
        public int ComponentsCleaned { get; set; }
        public List<string> CleanedComponents { get; set; } = new();
        public List<string> FailedComponents { get; set; } = new();
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 内存泄漏报告
    /// </summary>
    public class MemoryLeakReport
    {
        public string SessionName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public long StartMemory { get; set; }
        public long EndMemory { get; set; }
        public long MemoryGrowth => EndMemory - StartMemory;
        public List<MemorySnapshot> Snapshots { get; set; } = new();
        public List<SuspectedLeak> SuspectedLeaks { get; set; } = new();
        public bool HasLeaks => SuspectedLeaks.Any();
    }

    /// <summary>
    /// 内存快照
    /// </summary>
    public class MemorySnapshot
    {
        public DateTime Timestamp { get; set; }
        public long TotalMemory { get; set; }
        public long ManagedMemory { get; set; }
        public long UnmanagedMemory { get; set; }
        public int Generation0Collections { get; set; }
        public int Generation1Collections { get; set; }
        public int Generation2Collections { get; set; }
        public Dictionary<string, long> ComponentMemory { get; set; } = new();
    }

    /// <summary>
    /// 疑似内存泄漏
    /// </summary>
    public class SuspectedLeak
    {
        public string ComponentName { get; set; } = string.Empty;
        public string LeakType { get; set; } = string.Empty;
        public long MemoryGrowth { get; set; }
        public double GrowthRate { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string[] Recommendations { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// 内存使用信息
    /// </summary>
    public class MemoryUsageInfo
    {
        public long TotalMemory { get; set; }
        public long ManagedMemory { get; set; }
        public long UnmanagedMemory { get; set; }
        public long AvailableMemory { get; set; }
        public double MemoryUsagePercentage { get; set; }
        public int Generation0Collections { get; set; }
        public int Generation1Collections { get; set; }
        public int Generation2Collections { get; set; }
        public Dictionary<string, long> ComponentMemory { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 内存阈值
    /// </summary>
    public class MemoryThresholds
    {
        public long MaxTotalMemory { get; set; } = 1024 * 1024 * 1024; // 1GB
        public double MaxMemoryUsagePercentage { get; set; } = 80.0; // 80%
        public long MaxComponentMemory { get; set; } = 100 * 1024 * 1024; // 100MB
        public int MaxConsecutiveGC2Collections { get; set; } = 5;
        public TimeSpan LeakDetectionInterval { get; set; } = TimeSpan.FromMinutes(5);
        public double MinLeakGrowthRate { get; set; } = 10.0; // 10% per interval
    }

    /// <summary>
    /// 内存警告事件参数
    /// </summary>
    public class MemoryWarningEventArgs : EventArgs
    {
        public string WarningType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? ComponentName { get; set; }
        public long CurrentMemory { get; set; }
        public long ThresholdMemory { get; set; }
        public MemoryUsageInfo MemoryInfo { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 内存清理事件参数
    /// </summary>
    public class MemoryCleanupEventArgs : EventArgs
    {
        public MemoryCleanupResult Result { get; set; } = new();
        public bool WasForced { get; set; }
        public string Trigger { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;

namespace LYBT.Desktop.Core.Configuration
{
    /// <summary>
    /// UltraThink Phase 5.2: 应用配置接口
    /// 统一配置管理和访问
    /// </summary>
    public interface IAppConfiguration
    {
        /// <summary>
        /// API基础URL
        /// </summary>
        string ApiBaseUrl { get; }

        /// <summary>
        /// 连接超时时间（秒）
        /// </summary>
        int ConnectionTimeout { get; }

        /// <summary>
        /// 是否启用调试模式
        /// </summary>
        bool IsDebugMode { get; }

        /// <summary>
        /// 缓存配置
        /// </summary>
        CacheConfiguration Cache { get; }

        /// <summary>
        /// 日志配置
        /// </summary>
        LoggingConfiguration Logging { get; }

        /// <summary>
        /// 性能配置
        /// </summary>
        PerformanceConfiguration Performance { get; }

        /// <summary>
        /// 获取配置值
        /// </summary>
        T GetValue<T>(string key, T defaultValue = default!);

        /// <summary>
        /// 设置配置值
        /// </summary>
        void SetValue<T>(string key, T value);

        /// <summary>
        /// 检查配置键是否存在
        /// </summary>
        bool HasKey(string key);
    }

    /// <summary>
    /// 缓存配置
    /// </summary>
    public class CacheConfiguration
    {
        public int DefaultExpirationMinutes { get; set; } = 30;
        public int MaxSize { get; set; } = 1000;
        public double CompactionPercentage { get; set; } = 0.25;
        public int ScanFrequencyMinutes { get; set; } = 5;
    }

    /// <summary>
    /// 日志配置
    /// </summary>
    public class LoggingConfiguration
    {
        public string MinimumLevel { get; set; } = "Information";
        public bool EnableConsole { get; set; } = true;
        public bool EnableDebug { get; set; } = true;
        public bool EnableFile { get; set; } = false;
        public string LogFilePath { get; set; } = "logs/app.log";
    }

    /// <summary>
    /// 性能配置
    /// </summary>
    public class PerformanceConfiguration
    {
        public int MaxConcurrentRequests { get; set; } = 10;
        public int UIUpdateThrottleMs { get; set; } = 16; // ~60fps
        public bool EnableVirtualization { get; set; } = true;
        public int LazyLoadThreshold { get; set; } = 100;
        public int PreloadBatchSize { get; set; } = 20;
    }
}
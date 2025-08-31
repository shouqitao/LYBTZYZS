using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace LYBT.Desktop.Core.Configuration
{
    /// <summary>
    /// UltraThink Phase 5.2: 应用配置实现
    /// 支持文件配置、环境变量和运行时修改
    /// </summary>
    public class AppConfiguration : IAppConfiguration
    {
        private readonly IConfiguration _configuration;
        private readonly Dictionary<string, object> _runtimeValues = new();
        
        public string ApiBaseUrl => GetValue("ApiBaseUrl", "https://localhost:7001");
        public int ConnectionTimeout => GetValue("ConnectionTimeout", 30);
        public bool IsDebugMode => GetValue("IsDebugMode", false);
        
        public CacheConfiguration Cache { get; private set; } = null!;
        public LoggingConfiguration Logging { get; private set; } = null!;
        public PerformanceConfiguration Performance { get; private set; } = null!;

        private AppConfiguration(IConfiguration configuration)
        {
            _configuration = configuration;
            InitializeConfigurations();
        }

        /// <summary>
        /// 从文件加载配置
        /// </summary>
        public static AppConfiguration LoadFromFile(string configFilePath)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile(configFilePath, optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{GetEnvironmentName()}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            var configuration = builder.Build();
            return new AppConfiguration(configuration);
        }

        private static string GetEnvironmentName()
        {
#if DEBUG
            return "Development";
#else
            return "Production";
#endif
        }

        private void InitializeConfigurations()
        {
            Cache = _configuration.GetSection("Cache").Get<CacheConfiguration>() ?? new CacheConfiguration();
            Logging = _configuration.GetSection("Logging").Get<LoggingConfiguration>() ?? new LoggingConfiguration();
            Performance = _configuration.GetSection("Performance").Get<PerformanceConfiguration>() ?? new PerformanceConfiguration();

            // 在Debug模式下调整配置
#if DEBUG
            Logging.MinimumLevel = "Debug";
            Performance.EnableVirtualization = false; // 便于调试
#endif
        }

        public T GetValue<T>(string key, T? defaultValue = default)
        {
            // 优先从运行时值获取
            if (_runtimeValues.TryGetValue(key, out var runtimeValue) && runtimeValue is T typedValue)
            {
                return typedValue;
            }

            // 从配置文件获取
            var configValue = _configuration[key];
            if (configValue != null)
            {
                try
                {
                    if (typeof(T) == typeof(bool))
                    {
                        return (T)(object)bool.Parse(configValue);
                    }
                    if (typeof(T) == typeof(int))
                    {
                        return (T)(object)int.Parse(configValue);
                    }
                    if (typeof(T) == typeof(double))
                    {
                        return (T)(object)double.Parse(configValue);
                    }
                    if (typeof(T) == typeof(string))
                    {
                        return (T)(object)configValue;
                    }
                    
                    // 尝试JSON反序列化
                    return JsonSerializer.Deserialize<T>(configValue) ?? defaultValue!;
                }
                catch
                {
                    // 解析失败，返回默认值
                }
            }

            return defaultValue!;
        }

        public void SetValue<T>(string key, T value)
        {
            _runtimeValues[key] = value ?? (object)string.Empty;
        }

        public bool HasKey(string key)
        {
            return _runtimeValues.ContainsKey(key) || _configuration[key] != null;
        }

        /// <summary>
        /// 创建默认开发配置
        /// </summary>
        public static AppConfiguration CreateDefault()
        {
            var inMemoryConfig = new Dictionary<string, string>
            {
                {"ApiBaseUrl", "https://localhost:7001"},
                {"ConnectionTimeout", "30"},
                {"IsDebugMode", "true"},
                {"Cache:DefaultExpirationMinutes", "30"},
                {"Cache:MaxSize", "1000"},
                {"Cache:CompactionPercentage", "0.25"},
                {"Cache:ScanFrequencyMinutes", "5"},
                {"Logging:MinimumLevel", "Debug"},
                {"Logging:EnableConsole", "true"},
                {"Logging:EnableDebug", "true"},
                {"Logging:EnableFile", "false"},
                {"Performance:MaxConcurrentRequests", "10"},
                {"Performance:UIUpdateThrottleMs", "16"},
                {"Performance:EnableVirtualization", "false"},
                {"Performance:LazyLoadThreshold", "100"},
                {"Performance:PreloadBatchSize", "20"}
            };

            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemoryConfig.Select(kvp => new KeyValuePair<string, string?>(kvp.Key, kvp.Value)))
                .AddEnvironmentVariables();

            var configuration = builder.Build();
            return new AppConfiguration(configuration);
        }

        /// <summary>
        /// 获取环境特定的配置
        /// </summary>
        public static AppConfiguration ForEnvironment(string environment)
        {
            var configValues = environment.ToLowerInvariant() switch
            {
                "development" => CreateDevelopmentConfig(),
                "staging" => CreateStagingConfig(),
                "production" => CreateProductionConfig(),
                _ => CreateDevelopmentConfig()
            };

            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(configValues.Select(kvp => new KeyValuePair<string, string?>(kvp.Key, kvp.Value)))
                .AddEnvironmentVariables();

            return new AppConfiguration(builder.Build());
        }

        private static Dictionary<string, string> CreateDevelopmentConfig()
        {
            return new Dictionary<string, string>
            {
                {"ApiBaseUrl", "https://localhost:7001"},
                {"IsDebugMode", "true"},
                {"Logging:MinimumLevel", "Debug"},
                {"Performance:EnableVirtualization", "false"}
            };
        }

        private static Dictionary<string, string> CreateStagingConfig()
        {
            return new Dictionary<string, string>
            {
                {"ApiBaseUrl", "https://staging.lybt.com/api"},
                {"IsDebugMode", "false"},
                {"Logging:MinimumLevel", "Information"},
                {"Performance:EnableVirtualization", "true"}
            };
        }

        private static Dictionary<string, string> CreateProductionConfig()
        {
            return new Dictionary<string, string>
            {
                {"ApiBaseUrl", "https://api.lybt.com"},
                {"IsDebugMode", "false"},
                {"Logging:MinimumLevel", "Warning"},
                {"Logging:EnableFile", "true"},
                {"Performance:EnableVirtualization", "true"},
                {"Performance:MaxConcurrentRequests", "20"}
            };
        }
    }
}
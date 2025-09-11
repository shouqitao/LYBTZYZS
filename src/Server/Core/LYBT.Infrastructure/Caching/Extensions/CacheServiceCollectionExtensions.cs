#nullable enable

using LYBT.Infrastructure.Caching.Adapters;
using LYBT.Infrastructure.Caching.Configuration;
using LYBT.Infrastructure.Caching.Interfaces;
using LYBT.Shared.Interfaces.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Caching.Extensions
{
    /// <summary>
    /// 缓存服务注册扩展方法
    /// </summary>
    /// <remarks>
    /// <para>Phase 1: 缓存接口收口的服务注册扩展</para>
    /// <para>迁移支持: 同时支持新旧接口，确保平滑过渡</para>
    /// <para>配置统一: 使用UnifiedCacheOptions统一配置</para>
    /// <para>适配器模式: 提供新旧接口之间的适配</para>
    /// </remarks>
    public static class CacheServiceCollectionExtensions
    {
        /// <summary>
        /// 添加统一缓存服务 - Phase 1 主要注册方法
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddUnifiedCacheService(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // 1. 注册配置
            services.Configure<UnifiedCacheOptions>(
                configuration.GetSection(UnifiedCacheOptions.SectionName));

            // 2. 注册核心缓存服务
            services.AddMemoryCache();

            // 3. 注册统一缓存服务
            services.AddSingleton<ICacheService, MemoryCacheAdapter>();

            // 4. 注册兼容性适配器 (过渡期使用)
            services.AddSingleton<ISimplifiedCacheService>(provider =>
            {
                var cacheService = provider.GetRequiredService<ICacheService>();
                return new CacheServiceToSimplifiedAdapter(cacheService);
            });

            return services;
        }

        /// <summary>
        /// 添加统一缓存服务 - 带自定义配置
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configureOptions">配置委托</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddUnifiedCacheService(
            this IServiceCollection services,
            Action<UnifiedCacheOptions> configureOptions)
        {
            // 1. 注册配置
            services.Configure(configureOptions);

            // 2. 注册核心缓存服务
            services.AddMemoryCache();

            // 3. 注册统一缓存服务
            services.AddSingleton<ICacheService, MemoryCacheAdapter>();

            // 4. 注册兼容性适配器
            services.AddSingleton<ISimplifiedCacheService>(provider =>
            {
                var cacheService = provider.GetRequiredService<ICacheService>();
                return new CacheServiceToSimplifiedAdapter(cacheService);
            });

            return services;
        }

        /// <summary>
        /// 添加内存缓存适配器 - 适配现有IMemoryCache
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddMemoryCacheAdapter(this IServiceCollection services)
        {
            // 确保IMemoryCache已注册
            services.AddMemoryCache();

            // 注册内存缓存适配器
            services.AddSingleton<ICacheService>(provider =>
            {
                var memoryCache = provider.GetRequiredService<IMemoryCache>();
                var logger = provider.GetRequiredService<ILogger<MemoryCacheAdapter>>();
                return new MemoryCacheAdapter(memoryCache, logger);
            });

            return services;
        }

        /// <summary>
        /// 添加开发环境缓存配置
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDevelopmentCache(this IServiceCollection services)
        {
            return services.AddUnifiedCacheService(options =>
            {
                var devOptions = UnifiedCacheOptions.Development();
                options.CacheType = devOptions.CacheType;
                options.DefaultExpiryMinutes = devOptions.DefaultExpiryMinutes;
                options.Environment = devOptions.Environment;
                options.Memory = devOptions.Memory;
                options.Statistics = devOptions.Statistics;
                options.Performance = devOptions.Performance;
            });
        }

        /// <summary>
        /// 添加生产环境缓存配置
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddProductionCache(this IServiceCollection services)
        {
            return services.AddUnifiedCacheService(options =>
            {
                var prodOptions = UnifiedCacheOptions.Production();
                options.CacheType = prodOptions.CacheType;
                options.DefaultExpiryMinutes = prodOptions.DefaultExpiryMinutes;
                options.Environment = prodOptions.Environment;
                options.Memory = prodOptions.Memory;
                options.Statistics = prodOptions.Statistics;
                options.Performance = prodOptions.Performance;
            });
        }

        /// <summary>
        /// 添加高性能缓存配置
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddHighPerformanceCache(this IServiceCollection services)
        {
            return services.AddUnifiedCacheService(options =>
            {
                var perfOptions = UnifiedCacheOptions.HighPerformance();
                options.CacheType = perfOptions.CacheType;
                options.DefaultExpiryMinutes = perfOptions.DefaultExpiryMinutes;
                options.Environment = perfOptions.Environment;
                options.Memory = perfOptions.Memory;
                options.Statistics = perfOptions.Statistics;
                options.Performance = perfOptions.Performance;
            });
        }

        /// <summary>
        /// 替换现有ISimplifiedCacheService注册
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        /// <remarks>
        /// <para>迁移助手: 用于替换现有的ISimplifiedCacheService注册</para>
        /// <para>向后兼容: 保持ISimplifiedCacheService接口可用</para>
        /// <para>内部升级: 底层使用新的ICacheService实现</para>
        /// </remarks>
        public static IServiceCollection ReplaceSimplifiedCacheService(this IServiceCollection services)
        {
            // 移除现有的ISimplifiedCacheService注册（如果存在）
            var descriptorsToRemove = services
                .Where(d => d.ServiceType == typeof(ISimplifiedCacheService))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // 确保ICacheService已注册
            if (!services.Any(s => s.ServiceType == typeof(ICacheService)))
            {
                services.AddMemoryCacheAdapter();
            }

            // 重新注册ISimplifiedCacheService为适配器
            services.AddSingleton<ISimplifiedCacheService>(provider =>
            {
                var cacheService = provider.GetRequiredService<ICacheService>();
                return new CacheServiceToSimplifiedAdapter(cacheService);
            });

            return services;
        }

        /// <summary>
        /// 验证缓存服务配置
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>验证结果</returns>
        public static ValidationResult ValidateCacheConfiguration(this IServiceCollection services)
        {
            var errors = new List<string>();

            // 检查必需的服务是否已注册
            if (!services.Any(s => s.ServiceType == typeof(IMemoryCache)))
            {
                errors.Add("IMemoryCache service is not registered");
            }

            if (!services.Any(s => s.ServiceType == typeof(ICacheService)))
            {
                errors.Add("ICacheService is not registered");
            }

            // 检查配置选项
            var optionsDescriptor = services.FirstOrDefault(s =>
                s.ServiceType == typeof(Microsoft.Extensions.Options.IConfigureOptions<UnifiedCacheOptions>));

            if (optionsDescriptor == null)
            {
                errors.Add("UnifiedCacheOptions configuration is not registered");
            }

            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                Errors = errors
            };
        }

        /// <summary>
        /// 添加缓存预热服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddCacheWarmup(this IServiceCollection services)
        {
            services.AddHostedService<CacheWarmupHostedService>();
            return services;
        }
    }

    /// <summary>
    /// 缓存预热后台服务
    /// </summary>
    internal class CacheWarmupHostedService : Microsoft.Extensions.Hosting.IHostedService
    {
        private readonly ICacheService _cacheService;
        private readonly ILogger<CacheWarmupHostedService> _logger;

        public CacheWarmupHostedService(
            ICacheService cacheService,
            ILogger<CacheWarmupHostedService> logger)
        {
            _cacheService = cacheService;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting cache warmup...");

            try
            {
                // 预热常用缓存键
                await WarmupCommonCaches(cancellationToken);

                _logger.LogInformation("Cache warmup completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cache warmup failed");
            }
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Cache warmup service stopped");
            return Task.CompletedTask;
        }

        private async Task WarmupCommonCaches(CancellationToken cancellationToken)
        {
            // 预热系统配置缓存
            await _cacheService.SetAsync("system:config", new { initialized = true },
                TimeSpan.FromHours(1), cancellationToken);

            // 预热应用启动时间
            await _cacheService.SetAsync("system:startup", DateTime.UtcNow,
                TimeSpan.FromDays(1), cancellationToken);

            _logger.LogDebug("Warmed up {Count} cache entries", 2);
        }
    }
}

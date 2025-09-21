using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using LYBT.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace LYBT.WebAPI.Extensions;

/// <summary>
/// 性能优化配置扩展 - WebAPI性能调优
/// </summary>
public static class PerformanceOptimization
{
    /// <summary>
    /// 配置性能优化中间件和服务
    /// </summary>
    public static IServiceCollection ConfigurePerformanceOptimizations(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. 响应压缩
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
            {
                "application/json",
                "application/xml",
                "text/json",
                "text/xml"
            });
        });

        // 配置Brotli压缩级别
        services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Optimal;
        });

        // 配置Gzip压缩级别
        services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Optimal;
        });

        // 2. 响应缓存
        services.AddResponseCaching(options =>
        {
            options.MaximumBodySize = 1024 * 1024 * 10; // 10MB
            options.UseCaseSensitivePaths = false;
        });

        // 3. 输出缓存（.NET 7+）
        services.AddOutputCache(options =>
        {
            // 默认缓存策略
            options.AddBasePolicy(builder =>
            {
                builder.Expire(TimeSpan.FromSeconds(60))
                       .SetVaryByHeader("Accept-Language");
            });

            // 为健康检查端点添加缓存
            options.AddPolicy("HealthCheck", builder =>
            {
                builder.Expire(TimeSpan.FromSeconds(10));
            });

            // 为静态数据端点添加长缓存
            options.AddPolicy("StaticData", builder =>
            {
                builder.Expire(TimeSpan.FromMinutes(60))
                       .SetVaryByQuery("version");
            });
        });

        // 4. HTTP/2 支持
        services.Configure<KestrelServerOptions>(options =>
        {
            options.Limits.MaxConcurrentConnections = 100;
            options.Limits.MaxConcurrentUpgradedConnections = 100;
            options.Limits.MaxRequestBodySize = 30 * 1024 * 1024; // 30MB
            options.Limits.MinRequestBodyDataRate = new MinDataRate(
                bytesPerSecond: 240,
                gracePeriod: TimeSpan.FromSeconds(5));
            options.Limits.MinResponseDataRate = new MinDataRate(
                bytesPerSecond: 240,
                gracePeriod: TimeSpan.FromSeconds(5));
        });

        // 5. 线程池优化 - P3配置直读统一：使用IOptions<WebApiConfigurationOptions>
        // 注意：此处为了保持向后兼容，暂时保留直接配置读取
        // 后续应迁移到通过构造函数注入IOptions<WebApiConfigurationOptions>
        var minWorkerThreads = configuration.GetValue<int>("WebApiOptions:Performance:MinWorkerThreads", 50);
        var minIoThreads = configuration.GetValue<int>("WebApiOptions:Performance:MinIoThreads", 50);
        ThreadPool.SetMinThreads(minWorkerThreads, minIoThreads);

        // 6. 添加健康检查增强
        services.AddHealthChecks()
            .AddMemoryHealthCheck("memory", tags: new[] { "memory" });

        return services;
    }

    /// <summary>
    /// 应用性能优化中间件
    /// </summary>
    public static IApplicationBuilder UsePerformanceOptimizations(this IApplicationBuilder app)
    {
        // 响应压缩（必须在其他中间件之前）
        app.UseResponseCompression();

        // 响应缓存
        app.UseResponseCaching();

        // 输出缓存
        app.UseOutputCache();

        // 使用HTTP/2
        app.Use(async (context, next) =>
        {
            context.Response.Headers["X-HTTP-Version"] = context.Request.Protocol;
            await next();
        });

        return app;
    }

    /// <summary>
    /// 添加内存健康检查
    /// </summary>
    private static IHealthChecksBuilder AddMemoryHealthCheck(
        this IHealthChecksBuilder builder,
        string name,
        long? maximumMemoryBytes = null,
        params string[] tags)
    {
        var maxMemory = maximumMemoryBytes ?? 1024L * 1024L * 1024L; // 默认1GB

        builder.AddCheck(name, () =>
        {
            var allocated = GC.GetTotalMemory(forceFullCollection: false);
            var data = new Dictionary<string, object>
            {
                ["Allocated"] = allocated,
                ["Gen0Collections"] = GC.CollectionCount(0),
                ["Gen1Collections"] = GC.CollectionCount(1),
                ["Gen2Collections"] = GC.CollectionCount(2)
            };

            var status = allocated < maxMemory
                ? HealthStatus.Healthy
                : HealthStatus.Degraded;

            return new HealthCheckResult(
                status,
                description: $"Reports degraded status if allocated memory >= {maxMemory} bytes",
                data: data);
        }, tags);

        return builder;
    }
}
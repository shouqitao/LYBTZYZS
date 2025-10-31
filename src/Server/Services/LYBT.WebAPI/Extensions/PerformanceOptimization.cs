using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;

namespace LYBT.WebAPI.Extensions;

/// <summary>
/// 性能优化配置扩展 - WebAPI性能调优
/// Issue #1732 Phase 3: 简化性能配置，移除重复和过度设计
/// </summary>
public static class PerformanceOptimization
{
    /// <summary>
    /// 配置性能优化中间件和服务
    /// Issue #1732 Phase 3: 仅保留响应压缩配置
    /// 响应缓存、输出缓存、健康检查已在DatabaseServiceCollectionExtensions中配置
    /// </summary>
    public static IServiceCollection ConfigurePerformanceOptimizations(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 响应压缩（Brotli + Gzip）
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

        // Issue #1732 Phase 3: 移除以下过度设计和重复配置
        // ❌ 响应缓存配置 - DatabaseServiceCollectionExtensions已配置
        // ❌ 输出缓存配置 - DatabaseServiceCollectionExtensions已配置（策略更完整）
        // ❌ Kestrel限制配置 - MVP阶段使用默认值即可
        // ❌ 线程池手动配置 - MVP阶段信任.NET默认配置
        // ❌ 健康检查配置 - DatabaseServiceCollectionExtensions已配置

        return services;
    }

    /// <summary>
    /// 应用性能优化中间件
    /// Issue #1732 Phase 3: 仅保留响应压缩
    /// </summary>
    public static IApplicationBuilder UsePerformanceOptimizations(this IApplicationBuilder app)
    {
        // 响应压缩（必须在其他中间件之前）
        app.UseResponseCompression();

        // Issue #1732 Phase 3: 移除以下中间件调用
        // ❌ app.UseResponseCaching() - 在DatabaseServiceCollectionExtensions的中间件管道中配置
        // ❌ app.UseOutputCache() - 在DatabaseServiceCollectionExtensions的中间件管道中配置
        // ❌ HTTP/2版本头中间件 - 无实际用途，仅用于调试

        return app;
    }
}

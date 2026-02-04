using LYBT.Module.Sync.Interfaces;
using LYBT.Module.Sync.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Sync;

/// <summary>
/// 同步模块服务注册
/// OpenSpec: implement-data-sync
/// </summary>
public static class SyncModule
{
    /// <summary>
    /// 注册同步模块服务
    /// </summary>
    public static IServiceCollection AddSyncModule(this IServiceCollection services, IConfiguration configuration)
    {
        // 注册同步服务
        services.AddScoped<ISyncService, SyncService>();

        return services;
    }

    /// <summary>
    /// 配置同步模块中间件（如有需要）
    /// </summary>
    public static IApplicationBuilder UseSyncModule(this IApplicationBuilder app)
    {
        // 当前无特殊中间件需求
        return app;
    }
}

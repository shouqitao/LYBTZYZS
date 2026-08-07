using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Infrastructure.Services.CrossModule;

/// <summary>
/// 跨模块服务门面 DI 注册扩展。
/// </summary>
public static class CrossModuleServiceExtensions
{
    /// <summary>
    /// 注册统一跨模块服务门面（Core 层注册，避免模块注册顺序依赖）。
    /// </summary>
    public static IServiceCollection AddCrossModuleService(this IServiceCollection services)
    {
        services.AddScoped<ICrossModuleService, CrossModuleService>();
        return services;
    }
}

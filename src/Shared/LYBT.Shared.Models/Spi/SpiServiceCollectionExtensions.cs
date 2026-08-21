using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Shared.Models.Spi;

/// <summary>SPI 注册表 DI 扩展 — 各模块仅需注册 <see cref="IReportProvider"/> / <see cref="ICrossModuleReferenceChecker"/> 实现，注册表自动聚合。</summary>
public static class SpiServiceCollectionExtensions
{
    public static IServiceCollection AddSpiRegistries(this IServiceCollection services)
    {
        services.AddSingleton<ReportProviderRegistry>();
        services.AddSingleton<CrossModuleReferenceCheckerRegistry>();
        return services;
    }
}

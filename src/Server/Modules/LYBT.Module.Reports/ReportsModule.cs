using LYBT.Module.Reports.Interfaces;
using LYBT.Shared.Models.Spi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Reports;

/// <summary>
/// 报表模块服务注册。
/// </summary>
public static class ReportsModule
{
    /// <summary>
    /// 注册报表模块服务。
    /// </summary>
    public static IServiceCollection AddReportsModule(this IServiceCollection services, IConfiguration configuration)
    {
        // 仓储层
        services.AddScoped<IReportRepository, LYBT.Module.Reports.Infrastructure.ReportRepository>();

        // 服务层
        services.AddScoped<IReportService, Services.ReportService>();

        // SPI 扩展点：新增报表仅新增 IReportProvider 实现并注册（不改 ReportService）
        services.AddSingleton<IReportProvider, Spi.DailyIncomeReportProvider>();

        return services;
    }
}



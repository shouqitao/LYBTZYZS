using LYBT.Module.Reports.Infrastructure;
using LYBT.Module.Reports.Interfaces;
using LYBT.Shared.Configuration.Options.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        // Infrastructure层 - DbContext
        services.AddDbContext<ReportsDbContext>((sp, options) =>
        {
            var dbOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.UseSqlServer(dbOptions.ConnectionString);
        });

        // 仓储层
        services.AddScoped<IReportRepository, LYBT.Module.Reports.Infrastructure.ReportRepository>();

        return services;
    }
}



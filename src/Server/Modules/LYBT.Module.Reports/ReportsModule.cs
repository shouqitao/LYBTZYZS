using LYBT.Module.Reports.Infrastructure;
using LYBT.Module.Reports.Interfaces;
using LYBT.Shared.Configuration;
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
            var connectionString = ConnectionStringResolver.GetEffectiveConnectionString(dbOptions, configuration);
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("未配置数据库连接字符串");
            options.UseSqlServer(connectionString);
        });

        // 仓储层
        services.AddScoped<IReportRepository, LYBT.Module.Reports.Infrastructure.ReportRepository>();

        // 服务层
        services.AddScoped<IReportService, Services.ReportService>();

        return services;
    }
}



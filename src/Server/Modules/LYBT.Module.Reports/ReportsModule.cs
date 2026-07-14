using LYBT.Module.Reports.Application.Queries;
using LYBT.Module.Reports.Infrastructure;
using LYBT.Module.Reports.Interfaces;
using MediatR;
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

        // 仓储层 - Legacy（给旧ReportService使用）
        services.AddScoped<IReportRepository, LYBT.Module.Reports.Infrastructure.ReportRepository>();

        // Application层 - MediatR
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(GetDailyIncomeQuery).Assembly));

        return services;
    }
}



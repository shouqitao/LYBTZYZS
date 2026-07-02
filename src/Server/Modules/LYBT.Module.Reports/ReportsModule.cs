using LYBT.Module.Reports.Application.Queries;
using LYBT.Module.Reports.Infrastructure;
using LYBT.Module.Reports.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
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
        // Infrastructure层 - DbContext
        services.AddDbContext<ReportsDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // 仓储层 - Legacy（给旧ReportService使用）
        services.AddScoped<IReportRepository, LYBT.Module.Reports.Infrastructure.ReportRepository>();

        // Application层 - MediatR
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(GetDailyIncomeQuery).Assembly));

        return services;
    }

    /// <summary>
    /// 配置报表模块中间件。
    /// </summary>
    public static IApplicationBuilder UseReportsModule(this IApplicationBuilder app)
    {
        return app;
    }
}



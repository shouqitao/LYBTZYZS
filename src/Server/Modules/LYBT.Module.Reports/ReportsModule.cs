using LYBT.Module.Reports.Interfaces;
using LYBT.Module.Reports.Repositories;
using LYBT.Module.Reports.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Reports;

public static class ReportsModule
{
    public static IServiceCollection AddReportsModule(this IServiceCollection services)
    {
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IReportService, ReportService>();
        return services;
    }

    public static IApplicationBuilder UseReportsModule(this IApplicationBuilder app)
    {
        return app;
    }
}

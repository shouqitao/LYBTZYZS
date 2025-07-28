using LYBT.Module.Diagnostics.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Diagnostics {

    /// <summary>
    /// 诊断治疗模块服务注册
    /// </summary>
    public static class DiagnosticsModule {

        /// <summary>
        /// 添加诊断治疗模块服务
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="connectionString">数据库连接字符串</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDiagnosticsModule(this IServiceCollection services, string connectionString) {
            // 注册数据库上下文
            services.AddDbContext<DiagnosticDbContext>(options => {
                options.UseSqlServer(connectionString, sqlOptions => {
                    sqlOptions.MigrationsAssembly("LYBT.Module.Diagnostics");
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                });
                options.EnableSensitiveDataLogging(false);
                options.EnableServiceProviderCaching();
            });

            // TODO: 注册仓储和服务
            // services.AddScoped<IRegistrationRepository, RegistrationRepository>();
            // services.AddScoped<IRegistrationService, RegistrationService>();
            // services.AddScoped<IQueueingRepository, QueueingRepository>();
            // services.AddScoped<IQueueingService, QueueingService>();
            // services.AddScoped<IDiagnosisTreatmentRepository, DiagnosisTreatmentRepository>();
            // services.AddScoped<IDiagnosisTreatmentService, DiagnosisTreatmentService>();
            // services.AddScoped<IRecordRepository, RecordRepository>();
            // services.AddScoped<IRecordService, RecordService>();

            return services;
        }
    }
}
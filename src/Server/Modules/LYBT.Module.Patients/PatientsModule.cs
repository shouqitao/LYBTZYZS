using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Repositories;
using LYBT.Module.Patients.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Patients;

/// <summary>
/// Dependency registration for patients module.
/// </summary>
public static class PatientsModule
{

    /// <summary>
    /// Register patients module services (使用统一的 AppDbContext).
    /// </summary>
    public static IServiceCollection AddPatientsModule(this IServiceCollection services, string connectionString)
    {
        // 已改为使用统一的 AppDbContext，不再需要独立的 PatientsDbContext
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IPatientService, PatientService>();
        
        // 注册拆分后的专门服务
        services.AddScoped<PatientValidationService>();
        services.AddScoped<PatientArchiveService>();
        services.AddScoped<PatientStatisticsService>();
        
        return services;
    }
}
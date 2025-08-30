using LYBT.Shared.Interfaces.Services;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Repositories;
using LYBT.Module.Patients.Services;
using LYBT.Module.Patients.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Patients;

/// <summary>
/// Dependency registration for patients module.
/// </summary>
public static class PatientsModule
{

    /// <summary>
    /// Register patients module services - UltraThink三层架构
    /// </summary>
    public static IServiceCollection AddPatientsModule(this IServiceCollection services, string connectionString)
    {
        // 已改为使用统一的 AppDbContext，不再需要独立的 PatientsDbContext
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IPatientService, PatientService>();
        
        // UltraThink三层架构服务 - 核心重构
        services.AddScoped<PatientServiceCore>();
        services.AddScoped<PatientQueryService>();
        services.AddScoped<PatientBusinessService>();
        
        // 兼容性服务 - 逐步迁移
        services.AddScoped<PatientValidationService>();
        services.AddScoped<PatientArchiveService>();
        services.AddScoped<PatientStatisticsService>();
        
        // Helper类 - 保留以兼容现有代码（逐步废弃）
        services.AddScoped<PatientQueryHelper>();
        services.AddScoped<PatientValidationHelper>();
        services.AddScoped<PatientBusinessHelper>();
        
        return services;
    }
}
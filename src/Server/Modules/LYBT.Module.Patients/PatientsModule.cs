using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Repositories;
using LYBT.Module.Patients.Services;
using LYBT.Shared.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Module.Patients;

/// <summary>
/// 患者模块依赖注入注册入口（供主程序统一集成）
/// </summary>
public static class PatientsModule
{

    /// <summary>
    /// 注册本模块所有服务到 DI 容器（使用统一数据库上下文）
    /// UltraThink双层架构：Query(查询专业化) + Business(业务逻辑和CRUD)
    /// </summary>
    public static IServiceCollection AddPatientsModuleServices(this IServiceCollection services)
    {
        // 仓储层 - 使用OptimizedBaseRepository优化版本
        services.AddScoped<IPatientRepository, OptimizedPatientRepository>();

        // UltraThink双层架构服务 - 查询和业务逻辑分离
        services.AddScoped<IPatientQueryService, PatientQueryService>();
        services.AddScoped<IPatientBusinessService, PatientBusinessService>();

        // 主服务 - UltraThink纯委托模式，委托给专业服务层
        services.AddScoped<IPatientService, PatientService>();

        return services;
    }
}

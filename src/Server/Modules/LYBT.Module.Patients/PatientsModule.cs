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
    /// 简化架构：统一服务模式，合并查询和业务逻辑
    /// </summary>
    public static IServiceCollection AddPatientsModuleServices(this IServiceCollection services)
    {
        // 仓储层 - 使用OptimizedBaseRepository优化版本
        services.AddScoped<IPatientRepository, PatientRepository>();

        // 统一服务 - 合并查询和业务逻辑
        services.AddScoped<IPatientService, PatientService>();

        return services;
    }
}

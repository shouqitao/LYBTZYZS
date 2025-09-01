// UltraThink架构 - 模块化注册扩展
using LYBT.Module.Auth;
using LYBT.Module.Users;
using LYBT.Module.Patients;
using LYBT.Module.MedicalCase;
using LYBT.Module.Consultation;
using LYBT.Module.Prescriptions;
using LYBT.Module.Herbs;
using LYBT.Module.Formula;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.WebAPI.Extensions;

/// <summary>
/// 所有模块服务注入扩展
/// </summary>
public static class ServiceCollectionExtension
{
    /// <summary>
    /// 注册所有LYBT业务模块服务 - UltraThink模块化架构
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddAllModules(this IServiceCollection services)
    {
        // UltraThink模块化架构 - 统一使用AddXxxModule()扩展方法
        
        // 认证模块
        services.AddAuthModule();
        
        // 用户模块
        services.AddUsersModuleServices();
        
        // 患者模块
        services.AddPatientsModuleServices();
        
        // 医疗案例模块
        services.AddMedicalCaseModule();
        
        // 看诊模块
        services.AddConsultationModule();
        
        // 处方模块
        services.AddPrescriptionsModule();
        
        // 药材模块
        services.AddHerbsModule();
        
        // 验方模块
        services.AddFormulaModule();

        // 收银模块（原Billing）- 计划v2.0
        // 药房模块 - 计划v2.0  
        // 理疗室模块 - 计划v2.0
        return services;
    }

}
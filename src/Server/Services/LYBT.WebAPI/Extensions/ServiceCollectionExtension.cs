// TODO: UltraThink v2.0 Refactor - 暂时禁用有问题的模块using语句
// using LYBT.Module.Auth.Interfaces;
// using LYBT.Module.Auth.Repositories;
// using LYBT.Module.Auth.Services;
// 启用Consultation模块
using LYBT.Module.Consultation.Interfaces;
using LYBT.Module.Consultation.Repositories;
using LYBT.Module.Consultation.Services;
using LYBT.Module.Consultation.Helpers;
using LYBT.Module.Formula.Extensions;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Module.MedicalCase.Repositories;
using LYBT.Module.MedicalCase.Services;
using LYBT.Module.MedicalCase.Helpers;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Herbs.Repositories;
using LYBT.Module.Herbs.Services;
using LYBT.Module.Herbs.Helpers;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Repositories;
using LYBT.Module.Patients.Services;
using LYBT.Module.Patients.Helpers;
using LYBT.Module.Prescriptions;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Repositories;
using LYBT.Module.Users.Services;
using LYBT.Shared.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.WebAPI.Extensions;

/// <summary>
/// 所有模块服务注入扩展
/// </summary>
public static class ServiceCollectionExtension
{
    /// <summary>
    /// 注册所有LYBT业务模块服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddAllModules(this IServiceCollection services)
    {
        // 认证模块 - 已迁移到AuthModule.AddAuthModule()统一管理

        // 用户模块 - 已通过AddUsersModuleServices()在UnifiedServiceRegistration中注册

        // 患者模块
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<PatientValidationService>();
        services.AddScoped<PatientArchiveService>();
        services.AddScoped<PatientStatisticsService>();
        // 注册Helper类
        services.AddScoped<PatientQueryHelper>();
        services.AddScoped<PatientValidationHelper>();
        services.AddScoped<PatientBusinessHelper>();

        // 看诊模块 - 已启用
        services.AddScoped<IConsultationRepository, ConsultationRepository>();
        services.AddScoped<IConsultationService, ConsultationService>();
        // 注册Helper类
        services.AddScoped<ConsultationQueryHelper>();
        services.AddScoped<ConsultationValidationHelper>();
        services.AddScoped<ConsultationWorkflowHelper>();
        // 医疗案例模块 - 已启用
        services.AddScoped<IMedicalCaseRepository, MedicalCaseRepository>();
        services.AddScoped<IMedicalCaseService, MedicalCaseService>();
        // 注册Helper类
        services.AddScoped<MedicalCaseQueryHelper>();
        services.AddScoped<MedicalCaseValidationHelper>();
        services.AddScoped<MedicalCaseBusinessHelper>();

        // 药材模块
        services.AddScoped<IHerbService, HerbService>();
        services.AddScoped<IHerbRepository, HerbRepository>();
        // 注册Helper类
        services.AddScoped<HerbQueryHelper>();
        services.AddScoped<HerbValidationHelper>();
        services.AddScoped<HerbBusinessHelper>();

        // 验方模块（重构完成）
        services.AddFormulaModule();

        // 处方模块（使用模块化注册）
        services.AddPrescriptionsModule();

        // 收银模块（原Billing）
        // 药房模块
        // 理疗室模块
        return services;
    }

    /// <summary>
    /// 添加AutoMapper配置映射（已废弃，请使用 AddAutoMapperConfiguration）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    [Obsolete("请使用 AddAutoMapperConfiguration 替代")]
    public static IServiceCollection AddLybtAutoMapperProfiles(this IServiceCollection services)
    {
        // 调用新的配置方法
        return services.AddAutoMapperConfiguration();
    }
}
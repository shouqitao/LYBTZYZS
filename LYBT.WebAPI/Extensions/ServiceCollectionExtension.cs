using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Repositories;
using LYBT.Module.Auth.Services;
using LYBT.Module.DiagnosisTreatment.Interfaces;
using LYBT.Module.DiagnosisTreatment.Repositories;
using LYBT.Module.DiagnosisTreatment.Services;
using LYBT.Module.Doctors.Interfaces;
using LYBT.Module.Doctors.Repositories;
using LYBT.Module.Doctors.Services;
using LYBT.Module.FormulaTemplates.Interfaces;
using LYBT.Module.FormulaTemplates.Repositories;
using LYBT.Module.FormulaTemplates.Services;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Herbs.Repositories;
using LYBT.Module.Herbs.Services;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Repositories;
using LYBT.Module.Patients.Services;
using LYBT.Module.Prescriptions.Repositories;
using LYBT.Module.Prescriptions.Services;
using LYBT.Module.Queueing.Interfaces;
using LYBT.Module.Queueing.Repositories;
using LYBT.Module.Queueing.Services;
using LYBT.Module.Records.Interfaces;
using LYBT.Module.Records.Repositories;
using LYBT.Module.Records.Services;
using LYBT.Module.Registration.Interfaces;
using LYBT.Module.Registration.Repositories;
using LYBT.Module.Registration.Services;
using LYBT.Module.Sync.Interfaces;
using LYBT.Module.Sync.Repositories;
using LYBT.Module.Sync.Services;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Repositories;
using LYBT.Module.Users.Services;
using LYBT.Module.Billing.Interfaces;
using LYBT.Module.Billing.Repositories;
using LYBT.Module.Billing.Services;
using LYBT.Module.Pharmacy.Interfaces;
using LYBT.Module.Pharmacy.Repositories;
using LYBT.Module.Pharmacy.Services;
using LYBT.Module.TreatmentRoom.Interfaces;
using LYBT.Module.TreatmentRoom.Repositories;
using LYBT.Module.TreatmentRoom.Services;

namespace LYBT.WebAPI.Extensions;

/// <summary>
/// 所有模块服务注入扩展
/// </summary>
public static class ServiceCollectionExtension {

    /// <summary>
    /// 注册所有LYBT业务模块服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddLybtModules(this IServiceCollection services) {
        // 认证模块
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IAuthService, AuthService>();

        // 用户模块
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IUserRepository, UserRepository>();

        // 患者模块
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IPatientRepository, PatientRepository>();

        // 医生模块
        services.AddScoped<IDoctorService, DoctorService>();
        services.AddScoped<IDoctorRepository, DoctorRepository>();

        // 挂号模块
        services.AddScoped<IRegistrationService, RegistrationService>();
        services.AddScoped<IRegistrationRepository, RegistrationRepository>();

        // 排队模块
        services.AddScoped<IQueueingService, QueueingService>();
        services.AddScoped<IQueueingRepository, QueueingRepository>();

        // 诊疗模块
        services.AddScoped<IDiagnosisTreatmentService, DiagnosisTreatmentService>();
        services.AddScoped<IDiagnosisTreatmentRepository, DiagnosisTreatmentRepository>();

        // 药材模块
        services.AddScoped<IHerbService, HerbService>();
        services.AddScoped<IHerbRepository, HerbRepository>();

        // 经验方模板模块
        services.AddScoped<IFormulaTemplateService, FormulaTemplateService>();
        services.AddScoped<IFormulaTemplateRepository, FormulaTemplateRepository>();

        // 病历模块
        services.AddScoped<IRecordService, RecordService>();
        services.AddScoped<IRecordRepository, RecordRepository>();

        // 处方模块
        services.AddScoped<IPrescriptionService, PrescriptionService>();
        services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();

        // 收费模块
        services.AddScoped<IBillingService, BillingService>();
        services.AddScoped<IBillingRepository, BillingRepository>();

        // 药房模块
        services.AddScoped<IPharmacyService, PharmacyService>();
        services.AddScoped<IPharmacyRepository, PharmacyRepository>();

        // 理疗室模块
        services.AddScoped<ITreatmentRoomService, TreatmentRoomService>();
        services.AddScoped<ITreatmentRoomRepository, TreatmentRoomRepository>();

        // 同步模块
        services.AddScoped<ISyncService, SyncService>();
        services.AddScoped<ISyncRepository, SyncRepository>();

        return services;
    }

    /// <summary>
    /// 添加AutoMapper配置映射
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddLybtAutoMapperProfiles(this IServiceCollection services) {
        // 查找所有包含MappingProfile的程序集
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("LYBT.Module.") == true)
            .ToArray();

        services.AddAutoMapper(assemblies);
        
        return services;
    }
}
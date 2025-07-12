using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Repositories;
using LYBT.Module.Auth.Services;
using Microsoft.Extensions.DependencyInjection;
using LYBT.Module.Billing.Interfaces;
using LYBT.Module.Billing.Repositories;
using LYBT.Module.Billing.Services;
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
using LYBT.Module.Logs.Interfaces;
using LYBT.Module.Logs.Repositories;
using LYBT.Module.Logs.Services;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Repositories;
using LYBT.Module.Patients.Services;
using LYBT.Module.Prescriptions.Interfaces;
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
using LYBT.Module.Settings.Interfaces;
using LYBT.Module.Settings.Repositories;
using LYBT.Module.Settings.Services;
using LYBT.Module.Sync.Interfaces;
using LYBT.Module.Sync.Repositories;
using LYBT.Module.Sync.Services;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Repositories;
using LYBT.Module.Users.Services;

namespace LYBT.WebAPI.Extensions;

/// <summary>
/// 所有模块服务注入
/// </summary>
public static class ServiceCollectionExtension
{
    public static IServiceCollection AddLybtModules(this IServiceCollection services)
    {
        // 用户管理
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IAuthService, AuthService>();

        // 病人管理
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IPatientRepository, PatientRepository>();

        // 医生管理
        services.AddScoped<IDoctorService, DoctorService>();
        services.AddScoped<IDoctorRepository, DoctorRepository>();

        // 挂号管理
        services.AddScoped<IRegistrationService, RegistrationService>();
        services.AddScoped<IRegistrationRepository, RegistrationRepository>();

        // 排队管理
        services.AddScoped<IQueueingService, QueueingService>();
        services.AddScoped<IQueueingRepository, QueueingRepository>();

        // 诊疗管理
        services.AddScoped<IDiagnosisTreatmentService, DiagnosisTreatmentService>();
        services.AddScoped<IDiagnosisTreatmentRepository, DiagnosisTreatmentRepository>();

        // 药材管理
        services.AddScoped<IHerbService, HerbService>();
        services.AddScoped<IHerbRepository, HerbRepository>();

        // 经验方模板管理
        services.AddScoped<IFormulaTemplateService, FormulaTemplateService>();
        services.AddScoped<IFormulaTemplateRepository, FormulaTemplateRepository>();

        // 病历管理
        services.AddScoped<IRecordService, RecordService>();
        services.AddScoped<IRecordRepository, RecordRepository>();

        // 日志管理
        services.AddScoped<ILogService, LogService>();
        services.AddScoped<ILogRepository, LogRepository>();

        // 处方管理
        services.AddScoped<IPrescriptionService, PrescriptionService>();
        services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();

        // 同步管理
        services.AddScoped<ISyncService, SyncService>();
        services.AddScoped<ISyncRepository, SyncRepository>();

        // 设置管理
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();

        return services;
    }
}


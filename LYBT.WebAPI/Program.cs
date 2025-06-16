/// <summary>
/// WebAPI 程序入口，配置并启动应用
/// </summary>
using LYBT.Infrastructure;
using LYBT.Module.Users.Mapping;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Mapping;
using LYBT.Module.Patients.Repositories;
using LYBT.Module.Patients.Services;
using LYBT.Module.Doctors.Interfaces;
using LYBT.Module.Doctors.Mapping;
using LYBT.Module.Doctors.Repositories;
using LYBT.Module.Doctors.Services;
using LYBT.Module.Registration.Interfaces;
using LYBT.Module.Registration.Mapping;
using LYBT.Module.Registration.Repositories;
using LYBT.Module.Registration.Services;
using LYBT.Module.Queueing.Interfaces;
using LYBT.Module.Queueing.Mapping;
using LYBT.Module.Queueing.Repositories;
using LYBT.Module.Queueing.Services;
using LYBT.Module.DiagnosisTreatment.Interfaces;
using LYBT.Module.DiagnosisTreatment.Mapping;
using LYBT.Module.DiagnosisTreatment.Repositories;
using LYBT.Module.DiagnosisTreatment.Services;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Herbs.Mapping;
using LYBT.Module.Herbs.Repositories;
using LYBT.Module.Herbs.Services;
using LYBT.Module.FormulaTemplates.Interfaces;
using LYBT.Module.FormulaTemplates.Mapping;
using LYBT.Module.FormulaTemplates.Repositories;
using LYBT.Module.FormulaTemplates.Services;
using LYBT.Module.Logs.Mapping;
using LYBT.Module.Sync.Interfaces;
using LYBT.Module.Sync.Mapping;
using LYBT.Module.Sync.Repositories;
using LYBT.Module.Sync.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// =========== 1. 注册所有模块的 Service 和 Repository ===========

// 用户管理
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// 病人管理
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IPatientRepository, PatientRepository>();

// 医生管理
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();

// 挂号管理
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IRegistrationRepository, RegistrationRepository>();

// 排队管理
builder.Services.AddScoped<IQueueingService, QueueingService>();
builder.Services.AddScoped<IQueueingRepository, QueueingRepository>();

// 诊疗管理
builder.Services.AddScoped<IDiagnosisTreatmentService, DiagnosisTreatmentService>();
builder.Services.AddScoped<IDiagnosisTreatmentRepository, DiagnosisTreatmentRepository>();

// 药材管理
builder.Services.AddScoped<IHerbService, HerbService>();
builder.Services.AddScoped<IHerbRepository, HerbRepository>();

// 经验方模板管理
builder.Services.AddScoped<IFormulaTemplateService, FormulaTemplateService>();
builder.Services.AddScoped<IFormulaTemplateRepository, FormulaTemplateRepository>();

// 日志管理
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<ILogRepository, LogRepository>();

// 同步管理
builder.Services.AddScoped<ISyncService, SyncService>();
builder.Services.AddScoped<ISyncRepository, SyncRepository>();

// =========== 2. 注册所有模块的 AutoMapper 配置文件 ===========

builder.Services.AddAutoMapper(
    typeof(UserMappingProfile),
    typeof(PatientMappingProfile),
    typeof(DoctorMappingProfile),
    typeof(RegistrationMappingProfile),
    typeof(QueueingMappingProfile),
    typeof(DiagnosisTreatmentMappingProfile),
    typeof(HerbMappingProfile),
    typeof(FormulaTemplateMappingProfile),
    typeof(LogMappingProfile),
    typeof(SyncMappingProfile)
);

// =========== 3. 注册控制器和Swagger ===========

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => {
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "LYBT.WebAPI", Version = "v1" });
});

// =========== 4. 注册数据库上下文 ===========

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// =========== 5. 启动Web应用 ===========

var app = builder.Build();

if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();

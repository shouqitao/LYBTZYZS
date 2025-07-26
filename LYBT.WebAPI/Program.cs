/// <summary>
/// WebAPI 程序入口，配置并启动应用
/// </summary>
using LYBT.Infrastructure;
using LYBT.Infrastructure.Auth.Extensions;
using LYBT.Infrastructure.Exceptions;
using LYBT.Infrastructure.Helpers;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Repositories;
using LYBT.Module.Auth.Services;
using LYBT.Module.DiagnosisTreatment.Interfaces;
using LYBT.Module.DiagnosisTreatment.Mapping;
using LYBT.Module.DiagnosisTreatment.Repositories;
using LYBT.Module.DiagnosisTreatment.Services;
using LYBT.Module.Doctors.Interfaces;
using LYBT.Module.Doctors.Mapping;
using LYBT.Module.Doctors.Repositories;
using LYBT.Module.Doctors.Services;
using LYBT.Module.FormulaTemplates.Interfaces;
using LYBT.Module.FormulaTemplates.Mapping;
using LYBT.Module.FormulaTemplates.Repositories;
using LYBT.Module.FormulaTemplates.Services;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Herbs.Mapping;
using LYBT.Module.Herbs.Repositories;
using LYBT.Module.Herbs.Services;
using LYBT.Module.Logs.Interfaces;
using LYBT.Module.Logs.Mapping;
using LYBT.Module.Logs.Repositories;
using LYBT.Module.Logs.Services;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Mapping;
using LYBT.Module.Patients.Repositories;
using LYBT.Module.Patients.Services;
using LYBT.Module.Prescriptions.Mapping;
using LYBT.Module.Prescriptions.Repositories;
using LYBT.Module.Prescriptions.Services;
using LYBT.Module.Queueing.Interfaces;
using LYBT.Module.Queueing.Mapping;
using LYBT.Module.Queueing.Repositories;
using LYBT.Module.Queueing.Services;
using LYBT.Module.Records.Interfaces;
using LYBT.Module.Records.Repositories;
using LYBT.Module.Records.Services;
using LYBT.Module.Registration.Interfaces;
using LYBT.Module.Registration.Mapping;
using LYBT.Module.Registration.Repositories;
using LYBT.Module.Registration.Services;
using LYBT.Module.Settings.Interfaces;
using LYBT.Module.Settings.Mapping;
using LYBT.Module.Settings.Repositories;
using LYBT.Module.Settings.Services;
using LYBT.Module.Sync.Interfaces;
using LYBT.Module.Sync.Mapping;
using LYBT.Module.Sync.Repositories;
using LYBT.Module.Sync.Services;
using LYBT.Module.Users;
using LYBT.Module.Patients;
using LYBT.Module.Users.Interfaces;
using LYBT.Module.Users.Mapping;
using LYBT.Module.Users.Repositories;
using LYBT.Module.Users.Services;
using LYBT.WebAPI.Extensions;
using LYBT.Module.Users.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<UserOptions>(builder.Configuration.GetSection("UserDefaults"));

// =========== 1. 注册所有模块的 Service 和 Repository ===========

// 用户管理 registered via module
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();

// 病人管理 registered via module

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

// 病历管理
builder.Services.AddScoped<IRecordService, RecordService>();
builder.Services.AddScoped<IRecordRepository, RecordRepository>();

// 日志管理
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<ILogRepository, LogRepository>();

// 处方管理
builder.Services.AddScoped<IPrescriptionService, PrescriptionService>();
builder.Services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();

// 同步管理
builder.Services.AddScoped<ISyncService, SyncService>();
builder.Services.AddScoped<ISyncRepository, SyncRepository>();

// 设置管理
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<ISettingsRepository, SettingsRepository>();

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
    typeof(SyncMappingProfile),
    typeof(PrescriptionMappingProfile),
   typeof(SettingsMappingProfile)
);

// =========== 3. 注册控制器和Swagger ===========

builder.Services.AddControllers().AddJsonOptions(o => {
    o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddMemoryCache();

builder.Services.AddApiVersioning(opt => {
    opt.AssumeDefaultVersionWhenUnspecified = true;
    opt.DefaultApiVersion = new ApiVersion(1, 0);
    opt.ReportApiVersions = true;
    opt.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Version"));
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => {
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "LYBT.WebAPI", Version = "v1" });
    // Use full type names as schema IDs to avoid conflicts between classes
    // with the same name in different namespaces
    options.CustomSchemaIds(type => type.FullName);
});
builder.Services.AddCorsPolicy();

// =========== 4. 注册数据库上下文 ===========

var connection = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connection));
// Users module context
builder.Services.AddUsersModule(connection);
// Patients module context
builder.Services.AddPatientsModule(connection);

// =========== 5. JWT 认证配置 ===========
builder.Services.AddJwtAuthentication(builder.Configuration);

// =========== 6. 启动Web应用 ===========

var app = builder.Build();

// 注册全局异常处理中间件（放最前面）
app.UseMiddleware<ExceptionMiddleware>();

// ensure default admin user exists
using (var scope = app.Services.CreateScope()) {
    var context = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
    var opts = scope.ServiceProvider.GetRequiredService<IOptions<UserOptions>>();
    AdminSeeder.Seed(context, opts.Value.DefaultUserPassword);
}

if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
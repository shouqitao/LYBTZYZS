/// <summary>
/// WebAPI 程序入口，配置并启动应用
/// </summary>
using LYBT.Infrastructure;
using LYBT.Infrastructure.Logging;
using LYBT.Infrastructure.Configuration;
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

// =========== 1. 添加基础设施服务（统一日志、配置、缓存等） ===========
builder.Services.AddInfrastructure(builder.Configuration);

// =========== 2. 配置用户默认设置 ===========
builder.Services.Configure<UserOptions>(builder.Configuration.GetSection("UserDefaults"));

// =========== 3. 注册业务模块服务和仓储 ===========

// 认证模块
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();

// 医生模块
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();

// 挂号模块
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IRegistrationRepository, RegistrationRepository>();

// 排队模块
builder.Services.AddScoped<IQueueingService, QueueingService>();
builder.Services.AddScoped<IQueueingRepository, QueueingRepository>();

// 诊疗模块
builder.Services.AddScoped<IDiagnosisTreatmentService, DiagnosisTreatmentService>();
builder.Services.AddScoped<IDiagnosisTreatmentRepository, DiagnosisTreatmentRepository>();

// 药材模块
builder.Services.AddScoped<IHerbService, HerbService>();
builder.Services.AddScoped<IHerbRepository, HerbRepository>();

// 经验方模板模块
builder.Services.AddScoped<IFormulaTemplateService, FormulaTemplateService>();
builder.Services.AddScoped<IFormulaTemplateRepository, FormulaTemplateRepository>();

// 病历模块
builder.Services.AddScoped<IRecordService, RecordService>();
builder.Services.AddScoped<IRecordRepository, RecordRepository>();

// 处方模块
builder.Services.AddScoped<IPrescriptionService, PrescriptionService>();
builder.Services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();

// 同步模块
builder.Services.AddScoped<ISyncService, SyncService>();
builder.Services.AddScoped<ISyncRepository, SyncRepository>();

// 用户模块
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// 患者模块
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IPatientRepository, PatientRepository>();

// =========== 4. 注册AutoMapper配置 ===========
builder.Services.AddAutoMapper(
    typeof(UserMappingProfile),
    typeof(PatientMappingProfile),
    typeof(DoctorMappingProfile),
    typeof(RegistrationMappingProfile),
    typeof(QueueingMappingProfile),
    typeof(DiagnosisTreatmentMappingProfile),
    typeof(HerbMappingProfile),
    typeof(FormulaTemplateMappingProfile),
    typeof(SyncMappingProfile),
    typeof(PrescriptionMappingProfile)
);

// =========== 5. 添加控制器和JSON配置 ===========
builder.Services.AddControllers().AddJsonOptions(o => {
    o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// =========== 6. 添加Swagger文档 ===========
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => {
    options.SwaggerDoc("v1", new OpenApiInfo { 
        Title = "LYBT 中医诊所管理系统 API", 
        Version = "v1",
        Description = "统一基础设施架构的中医诊所管理系统API"
    });
    // 使用完整类型名作为Schema ID以避免同名类冲突
    options.CustomSchemaIds(type => type.FullName);
    
    // 添加JWT认证配置
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    options.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

// =========== 7. 注册业务模块数据库上下文 ===========
var connection = builder.Configuration.GetConnectionString("DefaultConnection");

// 用户模块数据库上下文
builder.Services.AddUsersModule(connection);

// 患者模块数据库上下文
builder.Services.AddPatientsModule(connection);

// =========== 8. 启动应用 ===========
var app = builder.Build();

// =========== 9. 初始化数据 ===========
using (var scope = app.Services.CreateScope()) {
    try {
        // 初始化默认管理员用户
        var context = scope.ServiceProvider.GetRequiredService<UserDbContext>();
        var opts = scope.ServiceProvider.GetRequiredService<IOptions<UserOptions>>();
        AdminSeeder.Seed(context, opts.Value.DefaultUserPassword);
        
        // 初始化统一配置服务的默认设置
        var configService = scope.ServiceProvider.GetRequiredService<IUnifiedConfigService>();
        await configService.InitializeDefaultGlobalSettingsAsync();
        
        // 记录应用启动日志
        var logService = scope.ServiceProvider.GetRequiredService<IUnifiedLogService>();
        await logService.LogInfoAsync("System", "应用程序启动成功", null, Guid.NewGuid().ToString());
    } catch (Exception ex) {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "应用程序初始化失败");
    }
}

// =========== 10. 配置中间件管道 ===========
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI(c => {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LYBT API v1");
        c.RoutePrefix = string.Empty; // 将Swagger UI设置为根路径
    });
}

app.UseCors("DefaultPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// =========== 11. 启动应用 ===========
app.Run();
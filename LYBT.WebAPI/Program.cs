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
using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Module.Prescriptions.Mapping;
using LYBT.Module.Prescriptions.Repositories;
using LYBT.Module.Prescriptions.Services;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Mapping;
using LYBT.Module.Patients.Repositories;
using LYBT.Module.Patients.Services;
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
using LYBT.Module.Settings.Repositories;
using LYBT.Module.Settings.Services;
using LYBT.Module.Sync.Interfaces;
using LYBT.Module.Sync.Mapping;
using LYBT.Module.Sync.Repositories;
using LYBT.Module.Sync.Services;
using LYBT.Module.Users.Mapping;
using LYBT.Module.Users;
using LYBT.WebAPI.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using LYBT.Module.Settings.Mapping;
using LYBT.Module.Users.Services;
using LYBT.Module.Users.Repositories;
using LYBT.Module.Users.Interfaces;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<UserOptions>(builder.Configuration.GetSection("UserDefaults"));

// =========== 1. 注册所有模块的 Service 和 Repository ===========
builder.Services.AddLybtModules();

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
   typeof(SettingsMappingProfile),
   typeof(PrescriptionMappingProfile)
);

// =========== 3. 注册控制器和Swagger ===========

builder.Services.AddControllers().AddJsonOptions(o => {
    o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
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

// =========== 5. JWT 认证配置 ===========
builder.Services.AddJwtAuthentication(builder.Configuration);

// =========== 6. 启动Web应用 ===========

var app = builder.Build();

// 注册全局异常处理中间件（放最前面）
app.UseMiddleware<ExceptionMiddleware>();

// ensure default admin user exists
using (var scope = app.Services.CreateScope()) {
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
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

/// <summary>
/// WebAPI 程序入口，配置并启动应用
/// </summary>
using LYBT.Infrastructure;
using LYBT.Infrastructure.Logging;
using LYBT.Infrastructure.Configuration;
using LYBT.WebAPI.Extensions;
using LYBT.WebAPI.Middleware;
using LYBT.Module.Users;
using LYBT.Module.Patients;
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

// =========== 3. 注册业务模块服务和仓储（使用扩展方法） ===========
builder.Services.AddLybtModules();

// =========== 4. 注册AutoMapper配置（使用扩展方法） ===========
builder.Services.AddLybtAutoMapperProfiles();

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
        // TODO: 实现AdminSeeder or move to UserService
        // var context = scope.ServiceProvider.GetRequiredService<UserDbContext>();
        // var opts = scope.ServiceProvider.GetRequiredService<IOptions<UserOptions>>();
        // AdminSeeder.Seed(context, opts.Value.DefaultUserPassword);
        
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

// 性能监控中间件（放在最前面以监控整个请求生命周期）
app.UsePerformanceMonitoring();

// 全局异常处理中间件
app.UseGlobalExceptionHandling();

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
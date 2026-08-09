using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LYBT.LocalWebAPI.Data;
using LYBT.LocalWebAPI.Auth;
using LYBT.Shared.Logging.Management;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Interfaces;
using LYBT.Infrastructure.Repositories;
using LYBT.Infrastructure.Services;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Infrastructure.Configuration.Stores;
using LYBT.Infrastructure.Validation;
using LYBT.Module.Identity;
using LYBT.Module.Patients;
using LYBT.Module.Herbs;
using LYBT.Module.Formulas;
using LYBT.Module.MedicalCases;
using LYBT.Module.Registrations;
using LYBT.Module.Reports;
using LYBT.Module.Identity.Services;
using LYBT.Shared.Configuration.Options.Common;
using LYBT.Shared.Configuration.Options.Server;
using LYBT.Shared.Models.Utilities.Security;
using LYBT.Entities.Users;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace LYBT.LocalWebAPI;

public static class LocalWebApiProgram
{
    public static WebApplicationBuilder CreateBuilder(string[]? args = null)
    {
        var builder = WebApplication.CreateBuilder(args ?? []);
        // 嵌入式本地服务始终按开发环境运行（密码使用 Desktop 配置，无生产环境变量要求）
        builder.WebHost.UseEnvironment("Development");
        return builder;
    }

    public static WebApplication CreateApplication(WebApplicationBuilder builder, string connectionString)
    {
        // DbContext — 使用 AppDbContext（与远程 WebAPI 一致，含审计自动化）
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        // 模块 DbContext（IdentityDbContext 等）与 AppDbContext 同库（ADR-0017 方案 A）
        builder.Services.AddOptions<DatabaseOptions>()
            .Configure(o => o.ConnectionString = connectionString);

        // 登录统一所需 Options（LoginCommandHandler 注入；Jwt 配置与 LocalJwtConfig 同节）
        builder.Services.AddOptions<JwtOptions>()
            .Bind(builder.Configuration.GetSection(JwtOptions.SectionName));
        builder.Services.AddOptions<SecurityOptions>()
            .Bind(builder.Configuration.GetSection(SecurityOptions.SectionName));
        builder.Services.AddOptions<LoginOptions>()
            .Bind(builder.Configuration.GetSection(LoginOptions.SectionName))
            .Configure(o =>
            {
                // A-31-C3a: 本地登录统一走 LoginCommandHandler，差异经 LoginOptions 控制
                o.IsLocal = true;
                o.LockoutEnabled = false; // 本地 Identity 已关闭锁定（int.MaxValue）
                o.AuditLevel = SecurityAuditLevel.Full;
            });

        // 系统日志仓储（只读查询，替代 DiagnosticsController 中的直接 DbContext 注入）
        builder.Services.AddScoped<ISystemLogRepository, SystemLogRepository>();

        builder.Services.AddHttpContextAccessor();

        // 本地运行时配置覆盖存储 — 落盘 {BaseDirectory}/config/runtime-overrides.json，重启不丢（A-18 P1-6，复用远程 JsonFileConfigurationStore 模式）
        var runtimeOverridesPath = Path.Combine(AppContext.BaseDirectory, "config", "runtime-overrides.json");
        builder.Services.AddSingleton<IConfigurationStore>(new JsonFileConfigurationStore(runtimeOverridesPath));

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(LYBT.LocalWebAPI.Controllers.HealthController).Assembly);

        builder.Services.AddSingleton<LoggingLevelManager>();

        // 注册模块 Service（与远程 WebAPI 使用相同的 Service/Repository 层）
        builder.Services.AddIdentityModule(builder.Configuration);
        builder.Services.AddPatientsModule(builder.Configuration);
        builder.Services.AddHerbsModule(builder.Configuration);
        builder.Services.AddFormulaModule(builder.Configuration);
        builder.Services.AddMedicalCaseModule(builder.Configuration);
        builder.Services.AddRegistrationModule(builder.Configuration);
        builder.Services.AddReportsModule(builder.Configuration);

        // LocalWebAPI CQRS Handlers（Auth）
        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(LocalWebApiProgram).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // 健康检查服务（复用 Server 基础设施层）
        builder.Services.AddScoped<IDbContextAccessor, DbContextAccessor>();
        builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();

        builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequireDigit = PasswordPolicyValidator.Policy.RequireDigit;
            options.Password.RequiredLength = PasswordPolicyValidator.Policy.MinLength;
            options.Password.RequireNonAlphanumeric = PasswordPolicyValidator.Policy.RequireSpecialChar;
            options.Password.RequireUppercase = PasswordPolicyValidator.Policy.RequireUppercase;
            options.Password.RequireLowercase = PasswordPolicyValidator.Policy.RequireLowercase;
            options.Lockout.MaxFailedAccessAttempts = int.MaxValue;
            options.Lockout.AllowedForNewUsers = false;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        // Register LocalJwtOptions from configuration
        builder.Services.AddOptions<LocalJwtOptions>()
            .Bind(builder.Configuration.GetSection(LocalJwtOptions.SectionName))
            .ValidateDataAnnotations();

        var localJwtOptions = builder.Configuration
            .GetSection(LocalJwtOptions.SectionName)
            .Get<LocalJwtOptions>()
            ?? new LocalJwtOptions();
        LocalJwtConfig.ConfigureServices(builder.Services, localJwtOptions);

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddFixedWindowLimiter("LocalLogin", opt =>
            {
                opt.PermitLimit = 5;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });
        });

        // Register DefaultPasswordOptions from configuration (required by IdentitySeedData)
        builder.Services.AddOptions<DefaultPasswordOptions>()
            .Bind(builder.Configuration.GetSection(DefaultPasswordOptions.SectionName))
            .ValidateDataAnnotations();

        var app = builder.Build();

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();
        app.MapControllers();

        return app;
    }

    public static async Task InitializeDatabaseAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
        await IdentitySeedData.SeedRolesAndAdminAsync(scope.ServiceProvider);
        await LocalWebApiSeedData.SeedAsync(dbContext, scope.ServiceProvider);
    }
}

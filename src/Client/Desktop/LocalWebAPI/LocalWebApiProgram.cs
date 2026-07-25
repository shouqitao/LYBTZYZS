using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LYBT.LocalWebAPI.Data;
using LYBT.LocalWebAPI.Auth;
using LYBT.Shared.Logging.Management;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Interfaces;
using LYBT.Infrastructure.Repositories;
using LYBT.Infrastructure.Services;
using LYBT.Module.Auth;
using LYBT.Module.Users;
using LYBT.Module.Patients;
using LYBT.Module.Herbs;
using LYBT.Module.Formulas;
using LYBT.Module.MedicalCases;
using LYBT.Module.Registration;
using LYBT.Module.Reports;
using LYBT.Module.Users.Services;
using LYBT.Shared.Configuration.Options.Server;
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
        return builder;
    }

    public static WebApplication CreateApplication(WebApplicationBuilder builder, string connectionString)
    {
        // DbContext — 使用 AppDbContext（与远程 WebAPI 一致，含审计自动化）
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        // 系统日志仓储（只读查询，替代 DiagnosticsController 中的直接 DbContext 注入）
        builder.Services.AddScoped<ISystemLogRepository, SystemLogRepository>();

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(LYBT.LocalWebAPI.Controllers.HealthController).Assembly);

        builder.Services.AddSingleton<LoggingLevelManager>();

        // 注册模块 Service（与远程 WebAPI 使用相同的 Service/Repository 层）
        builder.Services.AddAuthModule(builder.Configuration);
        builder.Services.AddUsersModule(builder.Configuration);
        builder.Services.AddPatientsModule();
        builder.Services.AddHerbsModule(builder.Configuration);
        builder.Services.AddFormulaModule(builder.Configuration);
        builder.Services.AddMedicalCaseModule();
        builder.Services.AddRegistrationModule();
        builder.Services.AddReportsModule(builder.Configuration);

        // LocalWebAPI CQRS Handlers（Auth + Diagnostics）
        builder.Services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(LocalWebApiProgram).Assembly));

        // 健康检查服务（复用 Server 基础设施层）
        builder.Services.AddScoped<IDbContextAccessor, DbContextAccessor>();
        builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();

        builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = true;
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
        await dbContext.Database.EnsureCreatedAsync();
        await IdentitySeedData.SeedRolesAndAdminAsync(scope.ServiceProvider);
        await LocalWebApiSeedData.SeedAsync(dbContext, scope.ServiceProvider);
    }

    public static async Task RunAsync(string[]? args, string connectionString)
    {
        var builder = CreateBuilder(args);
        var app = CreateApplication(builder, connectionString);
        await InitializeDatabaseAsync(app);
        await app.RunAsync();
    }
}

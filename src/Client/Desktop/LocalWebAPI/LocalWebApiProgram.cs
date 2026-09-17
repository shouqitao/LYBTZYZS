// P2-15-1 LocalWebApi 5100 硬编码已评估：端口冲突时 LocalWebApiProgram 启动失败已在 05-dual-mode.md 标注退避为 5101 自动重试（v2），当前单机单 Desktop 场景可接受
using System.Threading.RateLimiting;
using System.Text.Json.Serialization;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Serialization;
using LYBT.Infrastructure.Configuration.Stores;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Interfaces;
using LYBT.Infrastructure.Repositories;
using LYBT.Infrastructure.Services;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Infrastructure.Validation;
using LYBT.LocalWebAPI.Auth;
using LYBT.LocalWebAPI.Data;
using LYBT.Infrastructure.Hosting;
using LYBT.Module.Catalog;
using LYBT.Module.Identity;
using LYBT.Module.Identity.Services;
using LYBT.Module.MedicalCases;
using LYBT.Module.Patients;
using LYBT.Module.Registrations;
using LYBT.Module.Reports;
using LYBT.Shared.Configuration.Options.Common;
using LYBT.Shared.Configuration.Options.Server;
using LYBT.Shared.Logging.Management;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Utilities.Security;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LYBT.LocalWebAPI;

public static class LocalWebApiProgram
{
    public static WebApplicationBuilder CreateBuilder(string[]? args = null)
    {
        // desktop-di-fix 2026-08-14：显式指定内容根为应用基目录（WPF 进程工作目录可能是仓库根——
        // WebApplication.CreateBuilder 默认按当前目录加载 appsettings.json 会找不到 → Jwt 节缺失 →
        // LocalJwtOptions.SecretKey 空 → IDX10703 key length zero）；环境一并经 WebApplicationOptions 指定
        // （替代原 WebHost.UseEnvironment——CreateBuilder 后调用会抛 NotSupportedException）。
        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions
            {
                Args = args ?? [],
                ContentRootPath = AppContext.BaseDirectory,
                EnvironmentName = "Development",
            }
        );
        return builder;
    }

    public static WebApplication CreateApplication(
        WebApplicationBuilder builder,
        string connectionString
    )
    {
        // DbContext — 使用 AppDbContext（与远程 WebAPI 一致，含审计自动化）
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString)
        );

        // 模块 DbContext（IdentityDbContext 等）与 AppDbContext 同库（ADR-0017 方案 A）
        builder
            .Services.AddOptions<DatabaseOptions>()
            .Configure(o => o.ConnectionString = connectionString);

        // 登录统一所需 Options（LoginCommandHandler 注入；Jwt 配置与 LocalJwtConfig 同节）
        builder
            .Services.AddOptions<JwtOptions>()
            .Bind(builder.Configuration.GetSection(JwtOptions.SectionName));
        builder
            .Services.AddOptions<SecurityOptions>()
            .Bind(builder.Configuration.GetSection(SecurityOptions.SectionName));
        builder
            .Services.AddOptions<LoginOptions>()
            .Bind(builder.Configuration.GetSection(LoginOptions.SectionName))
            .Configure(o =>
            {
                // A-31-C3a: 本地登录统一走 LoginCommandHandler，差异经 LoginOptions 控制
                o.IsLocal = true;
                o.LockoutEnabled = true; // B1 (US-AUTH-002): 本地锁定对齐远程（5 次/15 分钟，LoginCommandHandler 业务锁定）
                o.AuditLevel = SecurityAuditLevel.Full;
            });

        // 系统日志仓储（只读查询，替代 DiagnosticsController 中的直接 DbContext 注入）
        builder.Services.AddScoped<ISystemLogRepository, SystemLogRepository>();

        builder.Services.AddHttpContextAccessor();

        // 本地运行时配置覆盖存储 — 落盘 {BaseDirectory}/config/runtime-overrides.json，重启不丢（A-18 P1-6，复用远程 JsonFileConfigurationStore 模式）
        var runtimeOverridesPath = Path.Combine(
            AppContext.BaseDirectory,
            "config",
            "runtime-overrides.json"
        );
        builder.Services.AddSingleton<IConfigurationStore>(
            new JsonFileConfigurationStore(runtimeOverridesPath)
        );

        builder
            .Services.AddControllers()
            .AddApplicationPart(typeof(LYBT.LocalWebAPI.Controllers.HealthController).Assembly)
            .AddJsonOptions(o =>
            {
                // ADR-0022：LocalWebAPI 枚举字符串化，与 Desktop 客户端（HttpApiClientBase）及
                // Remote WebAPI 契约一致（ASP.NET 默认枚举为数字，此处统一为字符串）。
                o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                // P0-9: 敏感数据脱敏（与 Remote ServiceCollectionExtensions 对齐，PII 脱敏）
                o.JsonSerializerOptions.Converters.Add(new SensitiveDataJsonConverterFactory());
            });

        builder.Services.AddSingleton<LoggingLevelManager>();

        // 方案 D：业务模块经 SharedHost 统一注册（Server 与 Local 同源，新增模块仅改 SharedHost 一处）
        builder.Services.AddSharedBusinessModules(builder.Configuration);

        // 共享基础设施（MemoryCache/OutputCache/CacheInvalidation/SignalR）经 SharedHost 统一
        builder.Services.AddSharedInfrastructure(builder.Configuration);

        // LocalWebAPI CQRS Handlers（Auth）
        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(LocalWebApiProgram).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // 健康检查服务（复用 Server 基础设施层）
        builder.Services.AddScoped<IDbContextAccessor, DbContextAccessor>();
        builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();

        builder
            .Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequireDigit = PasswordPolicyValidator.Policy.RequireDigit;
                options.Password.RequiredLength = PasswordPolicyValidator.Policy.MinLength;
                options.Password.RequireNonAlphanumeric = PasswordPolicyValidator
                    .Policy
                    .RequireSpecialChar;
                options.Password.RequireUppercase = PasswordPolicyValidator.Policy.RequireUppercase;
                options.Password.RequireLowercase = PasswordPolicyValidator.Policy.RequireLowercase;
                options.Lockout.MaxFailedAccessAttempts = 5; // B1 (US-AUTH-002): 对齐远程锁定阈值
                options.Lockout.AllowedForNewUsers = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        // Register LocalJwtOptions from configuration
        builder
            .Services.AddOptions<LocalJwtOptions>()
            .Bind(builder.Configuration.GetSection(LocalJwtOptions.SectionName))
            .ValidateDataAnnotations();

        var localJwtOptions =
            builder.Configuration.GetSection(LocalJwtOptions.SectionName).Get<LocalJwtOptions>()
            ?? new LocalJwtOptions();
        LocalJwtConfig.ConfigureServices(builder.Services, localJwtOptions);

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // R-15: 429 统一 ProblemDetails（RFC 7807 / X-3）——与 Remote OnRejected 同构
            options.OnRejected = async (context, cancellationToken) =>
            {
                var httpContext = context.HttpContext;
                var retryAfter = context.Lease.TryGetMetadata(
                    System.Threading.RateLimiting.MetadataName.RetryAfter, out var ra)
                    ? ra.TotalSeconds
                    : 60;

                httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                httpContext.Response.Headers.RetryAfter = ((int)retryAfter).ToString();

                await Results.Problem(
                    statusCode: StatusCodes.Status429TooManyRequests,
                    title: "Too Many Requests",
                    detail: ErrorMessages.Get(ErrorCode.RateLimitExceeded),
                    instance: httpContext.Request.Path,
                    extensions: new Dictionary<string, object?>
                    {
                        ["errorCode"] = ErrorCode.RateLimitExceeded.ToFormattedString(),
                        ["retryAfter"] = retryAfter
                    })
                    .ExecuteAsync(httpContext, cancellationToken);
            };

            // 登录/刷新：按来源 IP 分区，5 次/分钟（与 Remote 的 Login 策略同维度）
            options.AddPolicy(
                "LocalLogin",
                context =>
                    System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        }
                    )
            );

            // 写操作限流：与 Remote 的 ApiCalls 同名同维度（100 次/分钟/IP）。
            // 必须注册——共享的 BaseUsersController 的 batch-enable/batch-disable 标注了该策略名，
            // 缺失会在限流中间件解析策略时抛异常（500）。
            options.AddPolicy(
                "ApiCalls",
                context =>
                    System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        }
                    )
            );
        });

        // Register DefaultPasswordOptions from configuration (required by IdentitySeedData)
        // H-7: 校验未展开 ${} 占位符
        builder.Services.AddSingleton<Microsoft.Extensions.Options.IValidateOptions<DefaultPasswordOptions>,
            LYBT.Shared.Configuration.Validation.DefaultPasswordOptionsValidator>();
        builder
            .Services.AddOptions<DefaultPasswordOptions>()
            .Bind(builder.Configuration.GetSection(DefaultPasswordOptions.SectionName))
            .ValidateDataAnnotations();

        var app = builder.Build();

        // 中间件管道 — 经 SharedHost 统一（含 UseExceptionHandler 兜底）
        app.ConfigurePipeline(isLocal: true);

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

/// <summary>
/// 凌隐宝堂中医诊所诊疗系统 WebAPI 程序入口
/// UltraThink重构：采用统一服务注入管理，简化代码结构，提高可维护性
/// UltraThink v2.0 Security: 加载.env文件和环境变量替换支持
/// Issue #1077 Fix: 转换为传统Main方法确保WebApplicationFactory完全兼容性
/// Issue #1932: 配置文件整合 - 统一appsettings.json + .env环境变量模式
/// refactor-logging-system: 实现Serilog两阶段初始化(Bootstrap + Final Logger)
/// </summary>
using System.Security.Cryptography;
using System.Text;
using DotNetEnv;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Configuration.Services;
using LYBT.Infrastructure.Configuration.Stores;
using LYBT.Infrastructure.Configuration.Validation;
using LYBT.Infrastructure.Data;
using LYBT.Module.Identity.Services;
using LYBT.Shared.Configuration.Extensions;
using LYBT.Shared.Logging.Bootstrap;
using LYBT.Shared.Models.Utilities.Security;
using LYBT.WebAPI.Configuration;
using LYBT.WebAPI.Extensions;
using Microsoft.AspNetCore.Identity;
using Serilog;

/// <summary>
/// 凌隐宝堂中医诊所诊疗系统 WebAPI 程序入口
/// Issue #1077 修复：Program类移到全局命名空间确保WebApplicationFactory兼容性
/// A-31-C1: Serilog 两阶段初始化收敛至 LoggingBootstrap 统一入口
/// </summary>
public class Program
{
    // US-SHELL-024（2026-08-13 P2-07）: 单实例 Mutex（跨进程持有——防止双 WebAPI 并存写冲突）
    // Linux 语义：.NET 6+ 的 named Mutex 在 Linux 映射到 /tmp 下文件锁（flock）——Global\ 前缀
    // 在非 Windows 被忽略（Linux 无 session 概念），等价全局互斥——与 Desktop 单实例（US-SHELL-001）同模式
    private static Mutex? _instanceMutex;
    internal const string InstanceMutexName = @"Global\LYBTZYZS_WebAPI_Instance";

    public static async Task Main(string[] args)
    {
        // 修复Windows控制台中文乱码问题
        Console.OutputEncoding = Encoding.UTF8;

        // P1-3: 热更新抽至 WebApplicationBuilderExtensions.ApplyHotUpdateAsync()
        await WebApplicationBuilderExtensions.ApplyHotUpdateAsync();

        // Phase 1: Bootstrap Logger - 确保启动阶段异常能够被记录
        // refactor-logging-system: 测试环境使用普通Logger避免WebApplicationFactory"logger is already frozen"错误
        var environment =
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        var isTestEnvironment = environment == "Test";

        LoggingBootstrap.CreateBootstrapLogger(isTestEnvironment);

        // US-SHELL-024（2026-08-13 P2-07）: 单实例检查——已有实例在跑则拒绝启动（防跨脚本双开）
        // 位置：Bootstrap Logger 之后（能记日志）、任何写操作之前（尽早失败）
        // 仅 Production 生效（真实部署场景）；测试宿主（testhost.exe——WebApplicationFactory 多 host 测试
        // 进程内共享 Mutex 命名空间，同进程二次获取误判拒绝）与开发环境跳过
        // 测试宿主（testhost.exe——WebApplicationFactory 多 host 测试进程内共享 Mutex 命名空间，
        // 同进程二次获取误判拒绝——2026-08-13 P2-07 日志定位实证）与开发环境跳过
        var isTestHost = System.Diagnostics.Process.GetCurrentProcess().ProcessName.Contains(
            "testhost", StringComparison.OrdinalIgnoreCase);
        if (environment == "Production" && !isTestHost && !TryAcquireSingleInstance())
        {
            Log.Fatal(
                "[启动] 检测到已有 LYBT.WebAPI 实例在运行（Mutex={InstanceMutexName}）——拒绝启动（US-SHELL-024 单实例保护）",
                InstanceMutexName
            );
            Console.Error.WriteLine(
                "[启动] 已有实例在运行，拒绝启动（单实例保护——请先停止旧进程或用 start.sh 重启）"
            );
            Environment.Exit(1);
        }

        try
        {
            // US-LOG-008（2026-08-13）: 启动首条日志含版本/commit/env/pid——部署验证第一步（对照代码新旧）
            var informationalVersion = LoggingBootstrap.GetInformationalVersion();
            Log.Information(
                "[启动] LYBT.WebAPI v{InformationalVersion} (commit {Commit}) env={Environment} pid={Pid}",
                informationalVersion,
                LoggingBootstrap.GetCommitSha() ?? "unknown",
                environment,
                Environment.ProcessId
            );

            // 加载 .env 文件（如果存在）
            var envFile = environment == "Development" ? ".env.development" : ".env";
            var envPath = Path.Combine(Directory.GetCurrentDirectory(), envFile);
            if (File.Exists(envPath))
            {
                Env.Load(envPath);
                Log.Information("已加载环境变量文件: {EnvFile}", envFile);
            }
            // 配置闭环（2026-08-12）：环境配置文件缺失时自动生成默认模板（含占位符）——
            // 保证启动顺利走到配置校验器（提示注入），而非「配置空」隐晦错误
            EnsureEnvironmentConfigFiles(environment);

            var builder = WebApplication.CreateBuilder(args);

            // Windows 服务支持 - 必须在其他 Host 配置之前
            builder.Host.UseWindowsService(options =>
            {
                options.ServiceName = "LYBT-API";
            });

            // 配置主机和服务
            builder.Host.ConfigureEnvironmentAwareHosting();

            // Phase 2: Final Logger - 完整配置的生产级日志系统（LoggingBootstrap 统一入口）
            // refactor-logging-system: 使用LoggingLevelSwitch支持运行时动态调整
            builder.Host.AddLybtLogging(options =>
            {
                options.ApplicationName = "LYBT.WebAPI";
                options.UseMssqlSink = true;
            });

            // refactor-logging-system: 统一日志 DI 注册（ICorrelationIdProvider + LoggingLevelManager，供 DiagnosticsController 使用）
            builder.Services.AddLybtLogging();

            Log.Information("已切换到Final Logger，配置加载完成");

            // P1-3: 配置闭环抽至 WebApplicationBuilderExtensions.AddConfigurationClosedLoop()
            builder.AddConfigurationClosedLoop(environment);

            // 验证默认密码配置（所有环境）
            ValidateDefaultPasswordConfiguration(builder.Configuration, builder.Environment);

            // Issue Fix: AddIdentity MUST be registered BEFORE RegisterAllApplicationServices.
            // AddIdentity sets cookie auth as default scheme; RegisterAuthenticationServices
            // (called inside RegisterAllApplicationServices) overrides it with JWT Bearer.
            // If AddIdentity runs after, it overwrites the JWT default → 302 redirects.
            builder
                .Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
                {
                    options.Password.RequireDigit = PasswordPolicyValidator.Policy.RequireDigit;
                    options.Password.RequiredLength = PasswordPolicyValidator.Policy.MinLength;
                    options.Password.RequireNonAlphanumeric = PasswordPolicyValidator
                        .Policy
                        .RequireSpecialChar;
                    options.Password.RequireUppercase = PasswordPolicyValidator
                        .Policy
                        .RequireUppercase;
                    options.Password.RequireLowercase = PasswordPolicyValidator
                        .Policy
                        .RequireLowercase;
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                })
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            builder.Services.RegisterAllApplicationServices(
                builder.Configuration,
                builder.Environment
            );

            // US-REG-008: SignalR 实时通知（RegistrationHub）
            builder.Services.AddSignalR();

            // T5-P3-01: 所有环境验证 Critical 配置项
            var configValidator =
                new LYBT.Infrastructure.Configuration.Validation.ProductionConfigurationValidator(
                    builder.Configuration
                );
            var criticalMissing = configValidator.ValidateCriticalItems();
            if (criticalMissing.Count > 0)
            {
                foreach (var item in criticalMissing)
                {
                    Log.Warning("Critical 配置缺失: {ConfigItem}", item);
                }
            }

            // 所有环境: 验证 Important 配置项（降级为 Warning）
            var importantMissing = configValidator.ValidateImportantItems();
            foreach (var item in importantMissing)
            {
                Log.Warning("Important 配置缺失: {ConfigItem}", item);
            }

            // 生产环境: 全量验证（含 Important），失败终止启动
            if (builder.Environment.IsProduction())
            {
                try
                {
                    configValidator.ValidateOrThrow();
                    Log.Information(" Production 配置验证通过");
                }
                catch (LYBT.Infrastructure.Configuration.Validation.ProductionConfigurationException ex)
                {
                    Log.Fatal(ex, " Production 配置验证失败");
                    Console.Error.WriteLine(ex.Message);
                    Environment.Exit(1);
                }
            }

            // P1-3: Kestrel 端点抽至 WebApplicationBuilderExtensions.MapKestrelEndpoints()
            builder.MapKestrelEndpoints();

            var app = builder.Build();

            // 初始化应用服务
            try
            {
                await app.InitializeAllApplicationServices();
                await app.DisplayDatabaseStatusAsync();
                app.DisplayDevelopmentStartupInfo();

                using (var scope = app.Services.CreateScope())
                {
                    await IdentitySeedData.SeedRolesAndAdminAsync(scope.ServiceProvider);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "应用初始化过程中出现错误");
            }

            // 配置中间件
            app.ConfigureAllMiddleware();
            app.UseDevelopmentRequestLogging();

            // US-REG-008: 映射挂号实时通知 Hub
            app.MapHub<LYBT.Module.Registrations.Hubs.RegistrationHub>("/hubs/registration");

            Log.Information("应用配置完成，启动中...");
            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "应用程序启动失败");
            throw;
        }
        finally
        {
            // 释放单实例锁（P2-09 2026-08-14: 非持锁线程 ReleaseMutex 会抛
            // Object synchronization——进程退出自动释放，显式释放仅尽力而为）
            try
            {
                _instanceMutex?.ReleaseMutex();
            }
            catch (ApplicationException)
            {
                // 非持锁线程——忽略（进程退出自动释放）
            }
            _instanceMutex?.Dispose();
            Log.CloseAndFlush();
        }
    }

    /// <summary>获取单实例锁（US-SHELL-024——与 Desktop US-SHELL-001 同模式）</summary>
    private static bool TryAcquireSingleInstance() => TryAcquireSingleInstance(InstanceMutexName);

    /// <summary>从监听 URL（如 http://0.0.0.0:5000 / https://0.0.0.0:5001）解析端口（P2-09）</summary>
    private static int GetPort(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.IsDefaultPort == false)
            return uri.Port;
        return url.StartsWith("https", StringComparison.OrdinalIgnoreCase) ? 5001 : 5000;
    }

    /// <summary>
    /// 获取指定名称的单实例锁（internal 便于单测——US-SHELL-024 Mutex 语义验证）。
    /// 返回 true = 本次获取成功（调用方持有直至进程退出）；false = 已有实例持锁。
    /// </summary>
    internal static bool TryAcquireSingleInstance(string mutexName)
    {
        try
        {
            _instanceMutex = new Mutex(true, mutexName, out var createdNew);
            if (!createdNew)
            {
                _instanceMutex.Dispose();
                _instanceMutex = null;
                return false;
            }
            return true;
        }
        catch (AbandonedMutexException)
        {
            // 前实例崩溃未释放——Mutex 已自动接管，视为成功
            return true;
        }
        catch (Exception ex)
        {
            // Mutex 不可用（如权限/环境）——降级为日志警告继续启动（不阻断部署）
            Log.Warning(ex, "[启动] 单实例 Mutex 获取失败——降级继续（风险：可能双实例）");
            return true;
        }
    }

    /// <summary>
    /// 验证默认密码配置
    /// 开发环境：只验证存在和长度 >= 8，且不是常见弱密码
    /// 生产环境：完整密码复杂度验证
    /// </summary>
    /// <param name="configuration">应用程序配置</param>
    /// <param name="environment">主机环境</param>
    /// <exception cref="InvalidOperationException">当配置无效时抛出</exception>
    private static void ValidateDefaultPasswordConfiguration(
        IConfiguration configuration,
        IWebHostEnvironment environment
    )
    {
        var sysAdminPassword = configuration["DefaultPasswords:SysAdminPassword"];
        var newUserPassword = configuration["DefaultPasswords:NewUserPassword"];
        var systemAdminEmail = configuration["SystemAdmin:Email"];

        var missingConfigurations = new List<string>();

        if (string.IsNullOrWhiteSpace(sysAdminPassword))
            missingConfigurations.Add("DefaultPasswords:SysAdminPassword");

        if (string.IsNullOrWhiteSpace(newUserPassword))
            missingConfigurations.Add("DefaultPasswords:NewUserPassword");

        if (string.IsNullOrWhiteSpace(systemAdminEmail))
            missingConfigurations.Add("SystemAdmin:Email");

        if (missingConfigurations.Any())
        {
            var configList = string.Join(
                Environment.NewLine,
                missingConfigurations.Select(c => $"  - {c}")
            );
            throw new InvalidOperationException(
                $@"缺少必需的配置项：
{configList}

请在 appsettings.Development.json 或环境变量中配置。"
            );
        }

        // 验证长度
        if (sysAdminPassword!.Length < 8)
            throw new InvalidOperationException("系统管理员默认密码长度不能少于8位");

        if (newUserPassword!.Length < 8)
            throw new InvalidOperationException("新用户默认密码长度不能少于8位");

        // 生产环境：完整复杂度验证
        if (environment.IsProduction())
        {
            if (!PasswordPolicyValidator.Validate(sysAdminPassword, out var sysAdminErrors))
                throw new InvalidOperationException(
                    $"系统管理员密码不符合安全策略: {string.Join(", ", sysAdminErrors)}"
                );

            if (!PasswordPolicyValidator.Validate(newUserPassword, out var newUserErrors))
                throw new InvalidOperationException(
                    $"新用户密码不符合安全策略: {string.Join(", ", newUserErrors)}"
                );
        }
        // 开发环境：只验证不是明显弱密码
        else if (environment.IsDevelopment())
        {
            if (PasswordPolicyValidator.IsCommonWeakPassword(sysAdminPassword))
                throw new InvalidOperationException(
                    "开发环境密码也不能使用常见弱密码如 'password', '123456' 等"
                );

            if (PasswordPolicyValidator.IsCommonWeakPassword(newUserPassword))
                throw new InvalidOperationException("新用户默认密码不能使用常见弱密码");
        }

        Log.Information("默认密码配置验证通过 (环境: {Environment})", environment.EnvironmentName);
    }

    /// <summary>
    /// 环境配置文件缺失时自动生成默认模板（配置闭环——尽量保证正常顺利启动）。
    /// 生成到当前工作目录（与 CreateBuilder 的 AddJsonFile 加载路径一致）；
    /// 占位符语义 = 运维注入名（配置唯一化——双下划线变量名）。
    /// </summary>
    private static void EnsureEnvironmentConfigFiles(string environment, string? baseDir = null)
    {
        // ADR-0019: 生成到 config/ 子目录（与加载链一致——{BaseDir}/config/）
        var configDir = Path.Combine(baseDir ?? AppContext.BaseDirectory, "config");
        Directory.CreateDirectory(configDir);
        var cwd = configDir;
        var templates = new Dictionary<string, string>
        {
            ["appsettings.json"] = AppSettingsTemplate,
            [$"appsettings.{environment}.json"] =
                environment == "Production" ? ProductionTemplate : DevelopmentTemplate,
        };

        foreach (var (fileName, template) in templates)
        {
            var path = Path.Combine(cwd, fileName);
            if (File.Exists(path))
                continue;

            try
            {
                File.WriteAllText(path, template);
                Log.Information(
                    "配置闭环: 自动生成缺失配置文件 {File}（占位符待环境变量注入）",
                    fileName
                );
            }
            catch (Exception ex)
            {
                Log.Warning(
                    ex,
                    "配置闭环: 自动生成 {File} 失败（文件系统只读？）——启动继续",
                    fileName
                );
            }
        }
    }

    private static readonly string AppSettingsTemplate = """
        {
          "ConnectionStrings": {
            "DefaultConnection": "Server=${ConnectionStrings__DefaultConnection};Database=LYBTDB;User Id=${DB_USER};Password=${DB_PASSWORD};Encrypt=False;TrustServerCertificate=True;"
          },
          "Jwt": {
            "SecretKey": "${Jwt__SecretKey}",
            "Issuer": "LYBT.WebAPI",
            "Audience": "LYBT.Client",
            "AccessTokenExpirationMinutes": 480,
            "RefreshTokenExpirationDays": 7
          },
          "DefaultPasswords": {
            "SysAdminPassword": "${DefaultPasswords__SysAdminPassword}",
            "NewUserPassword": "${DefaultPasswords__NewUserPassword}",
            "ForceChangeOnFirstLogin": true
          },
          "SystemAdmin": {
            "AllowAutoCreateInProduction": false,
            "InitialSetupToken": "${SystemAdmin__InitialSetupToken}"
          },
          "Logging": {
            "LogLevel": {
              "Default": "Information",
              "Microsoft.AspNetCore": "Warning"
            }
          },
          "_comment": "自动生成模板——占位符由环境变量注入（配置唯一化：DefaultPasswords__SysAdminPassword 等双下划线名）"
        }
        """;

    private static readonly string ProductionTemplate = """
        {
          "Server": {
            "Endpoints": {
              "Http": { "Enabled": true, "Url": "http://0.0.0.0:5000" },
              "Https": { "Enabled": false, "Url": "https://0.0.0.0:5001", "Certificate": { "Path": "", "Password": "" } }
            }
          },
          "Jwt": {
            "SecretKey": "${Jwt__SecretKey}"
          },
          "DefaultPasswords": {
            "SysAdminPassword": "${DefaultPasswords__SysAdminPassword}",
            "NewUserPassword": "${DefaultPasswords__NewUserPassword}"
          },
          "SystemAdmin": {
            "AllowAutoCreateInProduction": false,
            "InitialSetupToken": "${SystemAdmin__InitialSetupToken}"
          },
          "DesktopUpdate": {
            "Enabled": true,
            "ReleasesPath": "C:\\Services\\LYBT-releases",
            "DownloadBaseUrl": "/releases",
            "FeedUrl": "http://your-server.example.com/releases"
          },
          "Cors": {
            "AllowedOrigins": ["http://your-server.example.com:5000"]
          },
          "_comment": "自动生成 Production 模板——按 01-deployment.md 发布清单替换占位符"
        }
        """;

    private static readonly string DevelopmentTemplate = """
        {
          "_comment": "自动生成 Development 模板——本地开发按需补充（连接串/JWT 等）"
        }
        """;

    /// <summary>P0-2：计算文件 SHA256（小写 hex），供热更新校验</summary>
    private static async Task<string> ComputeFileSha256Async(string path)
    {
        await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(fs);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

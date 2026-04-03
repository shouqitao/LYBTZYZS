/// <summary>
/// 凌隐宝堂中医诊所诊疗系统 WebAPI 程序入口
/// UltraThink重构：采用统一服务注入管理，简化代码结构，提高可维护性
/// UltraThink v2.0 Security: 加载.env文件和环境变量替换支持
/// Issue #1077 Fix: 转换为传统Main方法确保WebApplicationFactory完全兼容性
/// Issue #1932: 配置文件整合 - 统一appsettings.json + .env环境变量模式
/// refactor-logging-system: 实现Serilog两阶段初始化(Bootstrap + Final Logger)
/// </summary>
using System.Text;
using DotNetEnv;
using LYBT.Shared.Configuration.Extensions;
using LYBT.Shared.Logging.Extensions;
using LYBT.Shared.Logging.Management;
using LYBT.Shared.Utilities.Security;
using LYBT.WebAPI.Extensions;
using Serilog;
using Serilog.Events;

/// <summary>
/// 凌隐宝堂中医诊所诊疗系统 WebAPI 程序入口
/// Issue #1077 修复：Program类移到全局命名空间确保WebApplicationFactory兼容性
/// </summary>
public class Program
{
    // refactor-logging-system: 全局LoggingLevelManager实例，支持运行时动态调整
    private static readonly LoggingLevelManager LoggingLevelManager = new(LogEventLevel.Information);

    public static async Task Main(string[] args)
    {
        // 修复Windows控制台中文乱码问题
        Console.OutputEncoding = Encoding.UTF8;

        // Phase 1: Bootstrap Logger - 确保启动阶段异常能够被记录
        // 在try块外初始化，捕获配置加载阶段的任何异常
        // refactor-logging-system: 测试环境使用普通Logger避免WebApplicationFactory"logger is already frozen"错误
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        var isTestEnvironment = environment == "Test";

        if (!isTestEnvironment)
        {
            // 生产/开发环境使用Bootstrap Logger（支持两阶段初始化）
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .WriteTo.Console(
                    outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    path: "logs/bootstrap-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateBootstrapLogger();
        }
        else
        {
            // 测试环境使用简单Logger，避免Bootstrap Logger冻结问题
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Warning()
                .WriteTo.Console()
                .CreateLogger();
        }

        try
        {
            Log.Information("应用程序启动中...(Bootstrap Logger)");

            // 加载 .env 文件（如果存在）
            var envFile = environment == "Development" ? ".env.development" : ".env";
            var envPath = Path.Combine(Directory.GetCurrentDirectory(), envFile);
            if (File.Exists(envPath))
            {
                Env.Load(envPath);
                Log.Information("已加载环境变量文件: {EnvFile}", envFile);
            }
            var builder = WebApplication.CreateBuilder(args);

            // 配置主机和服务
            builder.Host.ConfigureEnvironmentAwareHosting();

            // Phase 2: Final Logger - 完整配置的生产级日志系统
            // 从配置文件读取，添加所有Enrichers和敏感数据脱敏
            // refactor-logging-system: 使用LoggingLevelSwitch支持运行时动态调整
            builder.Host.UseSerilog((context, services, configuration) =>
            {
                configuration
                    .MinimumLevel.ControlledBy(LoggingLevelManager.LevelSwitch)
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.WithMachineName()
                    .Enrich.WithThreadId()
                    .Enrich.WithProperty("Application", "LYBT.WebAPI")
                    .WithSensitiveDataMasking();

                // 测试环境跳过 SQL Server Sink，避免 AutoCreateSqlTable 与 EF 迁移冲突
                if (!context.HostingEnvironment.IsEnvironment("Test"))
                {
                    configuration.AddMSSqlServerSinkWithColumnOptions(
                        context.Configuration.GetConnectionString("DefaultConnection"));
                }
            });

            // refactor-logging-system: 注册LoggingLevelManager为单例，供AdminController使用
            builder.Services.AddSingleton(LoggingLevelManager);

            Log.Information("已切换到Final Logger，配置加载完成");

            // unify-configuration-system: 注册强类型配置
            builder.Services.AddLybtServerConfiguration(builder.Configuration);
            Log.Information("强类型配置注册完成");

            // 验证默认密码配置（所有环境）
            ValidateDefaultPasswordConfiguration(builder.Configuration, builder.Environment);

            builder.Services.RegisterAllApplicationServices(builder.Configuration, builder.Environment);

            // T5-P3-01: 所有环境验证 Critical 配置项
            var configValidator = new LYBT.Infrastructure.Configuration.Validation.ProductionConfigurationValidator(builder.Configuration);
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

            var app = builder.Build();

            // 初始化应用服务
            try
            {
                await app.InitializeAllApplicationServices();
                await app.DisplayDatabaseStatusAsync();
                app.DisplayDevelopmentStartupInfo();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "应用初始化过程中出现错误");
            }

            // 配置中间件
            app.ConfigureAllMiddleware();
            app.UseDevelopmentRequestLogging();

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
            Log.CloseAndFlush();
        }
    }

    /// <summary>
    /// 验证默认密码配置
    /// 确保必需的密码配置存在，如果缺少则启动失败
    /// </summary>
    /// <param name="configuration">应用程序配置</param>
    /// <exception cref="InvalidOperationException">当缺少必需的密码配置时抛出</exception>
    /// <summary>
    /// 验证默认密码配置
    /// 开发环境：只验证存在和长度 >= 8，且不是常见弱密码
    /// 生产环境：完整密码复杂度验证
    /// </summary>
    /// <param name="configuration">应用程序配置</param>
    /// <param name="environment">主机环境</param>
    /// <exception cref="InvalidOperationException">当配置无效时抛出</exception>
    private static void ValidateDefaultPasswordConfiguration(IConfiguration configuration, IWebHostEnvironment environment)
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
            var configList = string.Join(Environment.NewLine, missingConfigurations.Select(c => $"  - {c}"));
            throw new InvalidOperationException($@"缺少必需的配置项：
{configList}

请在 appsettings.Development.json 或环境变量中配置。");
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
                throw new InvalidOperationException($"系统管理员密码不符合安全策略: {string.Join(", ", sysAdminErrors)}");

            if (!PasswordPolicyValidator.Validate(newUserPassword, out var newUserErrors))
                throw new InvalidOperationException($"新用户密码不符合安全策略: {string.Join(", ", newUserErrors)}");
        }
        // 开发环境：只验证不是明显弱密码
        else if (environment.IsDevelopment())
        {
            if (PasswordPolicyValidator.IsCommonWeakPassword(sysAdminPassword))
                throw new InvalidOperationException("开发环境密码也不能使用常见弱密码如 'password', '123456' 等");

            if (PasswordPolicyValidator.IsCommonWeakPassword(newUserPassword))
                throw new InvalidOperationException("新用户默认密码不能使用常见弱密码");
        }

        Log.Information("默认密码配置验证通过 (环境: {Environment})", environment.EnvironmentName);
    }
}

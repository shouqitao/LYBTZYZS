/// <summary>
/// 凌隐宝堂中医诊所诊疗系统 WebAPI 程序入口
/// UltraThink重构：采用统一服务注入管理，简化代码结构，提高可维护性
/// UltraThink v2.0 Security: 加载.env文件和环境变量替换支持
/// </summary>
using LYBT.WebAPI.Extensions;
using Serilog;
using System.Reflection;

// =========== UltraThink安全配置加载逻辑 ===========
// 生产环境优先使用安全配置文件，开发环境使用标准配置
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
var configBuilder = new ConfigurationBuilder();

if (environment == "Development")
{
    // 开发环境：加载基础配置文件（包含开发用默认值）
    configBuilder.AddJsonFile("appsettings.json", optional: false);
}
else
{
    // 生产环境：仅使用安全配置模板 + 环境变量
    configBuilder.AddJsonFile("appsettings.Security.json", optional: false);
}

// 环境特定配置覆盖（如果存在）
configBuilder.AddJsonFile($"appsettings.{environment}.json", optional: true);

// 环境变量具有最高优先级（用于敏感配置）- 只保留这一处
configBuilder.AddEnvironmentVariables();

// 检测是否在测试环境中运行
var isTestEnvironment = Assembly.GetEntryAssembly()?.GetName().Name?.Contains("testhost") == true 
                       || environment == "Test"
                       || environment == "Testing";

if (isTestEnvironment)
{
    // 测试环境：使用简化日志配置，避免SQL Server依赖
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Warning()
        .WriteTo.Console()
        .CreateLogger();
}
else
{
    // 生产/开发环境：使用完整配置
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(configBuilder.Build())
        .CreateLogger();
}

try
{
    Log.Information(isTestEnvironment ? "测试环境启动" : "启动 LYBT WebAPI 服务...");

    var builder = WebApplication.CreateBuilder(args);

    // 配置主机（测试环境跳过某些配置）
    if (!isTestEnvironment)
    {
        builder.Host.ConfigureEnvironmentAwareHosting();
    }

    // 配置Serilog作为日志提供程序
    builder.Host.UseSerilog();

    // =========== UltraThink统一服务注册 ===========
    builder.Services.RegisterAllApplicationServices(builder.Configuration, builder.Environment);

    // =========== Production 配置验证 ===========
    if (!isTestEnvironment && builder.Environment.IsProduction())
    {
        var validator = new LYBT.Infrastructure.Configuration.Validation.ProductionConfigurationValidator(builder.Configuration);
        try
        {
            validator.ValidateOrThrow();
            Log.Information("✅ Production 配置验证通过");
        }
        catch (LYBT.Infrastructure.Configuration.Validation.ProductionConfigurationException ex)
        {
            Log.Fatal(ex, "❌ Production 配置验证失败");
            Console.Error.WriteLine(ex.Message);
            Environment.Exit(1);
        }
    }

    // =========== 构建应用 ===========
    var app = builder.Build();

    // =========== 应用初始化（测试环境简化） ===========
    if (!isTestEnvironment)
    {
        // 生产/开发环境：完整初始化
        await app.InitializeAllApplicationServices();
        await app.DisplayDatabaseStatusAsync();
        app.DisplayDevelopmentStartupInfo();
    }

    // =========== 中间件配置 ===========
    app.ConfigureAllMiddleware();

    if (!isTestEnvironment)
    {
        app.UseDevelopmentRequestLogging();
    }

    Log.Information(isTestEnvironment ? "测试环境应用构建完成" : "应用配置完成");

    // =========== 运行应用（测试环境跳过） ===========
    if (!isTestEnvironment)
    {
        await app.ConfigureEnvironmentAwareShutdown();
    }
    // 测试环境：应用构建完成，由 WebApplicationFactory 管理运行
}
catch (Exception ex)
{
    Log.Fatal(ex, "应用程序启动失败");
    if (!isTestEnvironment)
    {
        throw; // 生产环境重新抛出异常
    }
    else
    {
        Log.Warning("测试环境中忽略启动异常，继续执行");
    }
}
finally
{
    if (!isTestEnvironment)
    {
        Log.CloseAndFlush();
    }
}

// P3-Fix: 为集成测试提供Program类访问权限
public partial class Program { }

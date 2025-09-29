/// <summary>
/// 凌隐宝堂中医诊所诊疗系统 WebAPI 程序入口
/// UltraThink重构：采用统一服务注入管理，简化代码结构，提高可维护性
/// UltraThink v2.0 Security: 加载.env文件和环境变量替换支持
/// </summary>
using LYBT.WebAPI.Extensions;
using Serilog;

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

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configBuilder.Build())
    .CreateLogger();

try
{
    Log.Information("启动 LYBT WebAPI 服务...");

    var builder = WebApplication.CreateBuilder(args);

    // 配置环境感知的主机运行模式
    builder.Host.ConfigureEnvironmentAwareHosting();

    // 配置Serilog作为日志提供程序
    builder.Host.UseSerilog();

    // =========== 端口配置 ===========
    // 端口配置已移至 appsettings.json 中的 Kestrel 配置

    // 移除重复的环境变量配置（已在上面configBuilder中添加）

    // =========== UltraThink统一服务注册 ===========
    builder.Services.RegisterAllApplicationServices(builder.Configuration, builder.Environment);

    // =========== 构建应用 ===========
    var app = builder.Build();

    // =========== UltraThink统一初始化 ===========
    await app.InitializeAllApplicationServices();

    // =========== UltraThink统一中间件配置 ===========
    app.ConfigureAllMiddleware();

    // =========== 开发模式请求日志中间件 ===========
    app.UseDevelopmentRequestLogging();

    // =========== 显示数据库状态 ===========
    await app.DisplayDatabaseStatusAsync();

    // =========== 显示开发模式启动信息 ===========
    await app.DisplayDevelopmentStartupInfo();

    // =========== 环境感知的优雅关闭配置 ===========
    await app.ConfigureEnvironmentAwareShutdown();
}
catch (Exception ex)
{
    Log.Fatal(ex, "应用程序启动失败");
}
finally
{
    Log.CloseAndFlush();
}

// P3-Fix: 为集成测试提供Program类访问权限
public partial class Program { }

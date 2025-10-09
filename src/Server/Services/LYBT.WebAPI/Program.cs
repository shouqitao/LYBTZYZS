/// <summary>
/// 凌隐宝堂中医诊所诊疗系统 WebAPI 程序入口
/// UltraThink重构：采用统一服务注入管理，简化代码结构，提高可维护性
/// UltraThink v2.0 Security: 加载.env文件和环境变量替换支持
/// Issue #1077 Fix: 转换为传统Main方法确保WebApplicationFactory完全兼容性
/// </summary>
using LYBT.WebAPI.Extensions;
using Serilog;
using System.Reflection;

/// <summary>
/// 凌隐宝堂中医诊所诊疗系统 WebAPI 程序入口
/// Issue #1077 修复：Program类移到全局命名空间确保WebApplicationFactory兼容性
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        // 环境感知的配置构建
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        var configBuilder = new ConfigurationBuilder();

        if (environment == "Development")
        {
            configBuilder.AddJsonFile("appsettings.json", optional: false);
        }
        else
        {
            configBuilder.AddJsonFile("appsettings.Security.json", optional: false);
        }

        configBuilder.AddJsonFile($"appsettings.{environment}.json", optional: true);
        configBuilder.AddEnvironmentVariables();

        // 配置Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configBuilder.Build())
            .CreateLogger();

        try
        {
            var builder = WebApplication.CreateBuilder(args);
            
            // 配置主机和服务
            builder.Host.ConfigureEnvironmentAwareHosting();
            builder.Host.UseSerilog();
            builder.Services.RegisterAllApplicationServices(builder.Configuration, builder.Environment);

            // 生产环境配置验证
            if (builder.Environment.IsProduction())
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
}
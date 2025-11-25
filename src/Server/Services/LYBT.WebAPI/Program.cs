/// <summary>
/// 凌隐宝堂中医诊所诊疗系统 WebAPI 程序入口
/// UltraThink重构：采用统一服务注入管理，简化代码结构，提高可维护性
/// UltraThink v2.0 Security: 加载.env文件和环境变量替换支持
/// Issue #1077 Fix: 转换为传统Main方法确保WebApplicationFactory完全兼容性
/// Issue #1932: 配置文件整合 - 统一appsettings.json + .env环境变量模式
/// </summary>
using LYBT.WebAPI.Extensions;
using LYBT.Shared.Utilities.Security;
using Serilog;
using DotNetEnv;

/// <summary>
/// 凌隐宝堂中医诊所诊疗系统 WebAPI 程序入口
/// Issue #1077 修复：Program类移到全局命名空间确保WebApplicationFactory兼容性
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        // Issue #1932: 简化的配置加载逻辑
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        // 加载 .env 文件（如果存在）
        var envFile = environment == "Development" ? ".env.development" : ".env";
        var envPath = Path.Combine(Directory.GetCurrentDirectory(), envFile);
        if (File.Exists(envPath))
        {
            Env.Load(envPath);
        }

        // 配置构建
        var configBuilder = new ConfigurationBuilder();

        // 测试环境单独处理（使用SQLite内存数据库）
        if (environment == "Test")
        {
            configBuilder.AddJsonFile("appsettings.Test.json", optional: false);
        }
        else
        {
            // 统一使用 appsettings.json（包含环境变量占位符）
            configBuilder.AddJsonFile("appsettings.json", optional: false);
        }

        // 环境变量具有最高优先级，覆盖配置文件中的默认值
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
            
            // 验证默认密码配置（所有环境）
            ValidateDefaultPasswordConfiguration(builder.Configuration);
            
            builder.Services.RegisterAllApplicationServices(builder.Configuration, builder.Environment);

            // 生产环境配置验证
            if (builder.Environment.IsProduction())
            {
                var validator = new LYBT.Infrastructure.Configuration.Validation.ProductionConfigurationValidator(builder.Configuration);
                try
                {
                    validator.ValidateOrThrow();
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
    private static void ValidateDefaultPasswordConfiguration(IConfiguration configuration)
    {
        var sysAdminPassword = configuration["Lybt:DefaultPasswords:SysAdminPassword"];
        var newUserPassword = configuration["Lybt:DefaultPasswords:NewUserPassword"];
        var systemAdminEmail = configuration["Lybt:SystemAdmin:Email"];

        var missingConfigurations = new List<string>();

        if (string.IsNullOrWhiteSpace(sysAdminPassword))
        {
            missingConfigurations.Add("Lybt:DefaultPasswords:SysAdminPassword");
        }

        if (string.IsNullOrWhiteSpace(newUserPassword))
        {
            missingConfigurations.Add("Lybt:DefaultPasswords:NewUserPassword");
        }

        if (string.IsNullOrWhiteSpace(systemAdminEmail))
        {
            missingConfigurations.Add("Lybt:SystemAdmin:Email");
        }

        if (missingConfigurations.Any())
        {
            var configList = string.Join(Environment.NewLine, missingConfigurations.Select(config => $"  - {config}"));
            var errorMessage = $@"缺少必需的默认密码配置，应用程序无法启动。

缺少的配置项：
{configList}

解决方案：
1. 在 appsettings.json 中添加以下配置：
{{
  ""Lybt"": {{
    ""DefaultPasswords"": {{
      ""SysAdminPassword"": ""您的系统管理员默认密码"",
      ""NewUserPassword"": ""您的新用户默认密码""
    }},
    ""SystemAdmin"": {{
      ""Email"": ""admin@yourdomain.com""
    }}
  }}
}}

2. 或者在环境变量中设置：
  - LYBT__DEFAULTPASSWORDS__SYSADMINPASSWORD
  - LYBT__DEFAULTPASSWORDS__NEWUSERPASSWORD
  - LYBT__SYSTEMADMIN__EMAIL

3. 或者在 .env 文件中添加：
  LYBT_DEFAULTPASSWORDS_SYSADMINPASSWORD=您的系统管理员默认密码
  LYBT_DEFAULTPASSWORDS_NEWUSERPASSWORD=您的新用户默认密码
  LYBT_SYSTEMADMIN_EMAIL=admin@yourdomain.com

注意：密码不能为空，密码长度至少8位，建议包含大小写字母、数字和特殊字符。";

            throw new InvalidOperationException(errorMessage);
        }

        // 验证密码长度
        if (sysAdminPassword!.Length < 8)
        {
            throw new InvalidOperationException("系统管理员默认密码长度不能少于8位");
        }

        if (newUserPassword!.Length < 8)
        {
            throw new InvalidOperationException("新用户默认密码长度不能少于8位");
        }

        // 验证密码复杂度
        if (!PasswordPolicyValidator.Validate(sysAdminPassword, out var sysAdminErrors))
        {
            var errorMsg = string.Join(", ", sysAdminErrors);
            throw new InvalidOperationException($"系统管理员默认密码不符合安全策略: {errorMsg}");
        }

        if (!PasswordPolicyValidator.Validate(newUserPassword, out var newUserErrors))
        {
            var errorMsg = string.Join(", ", newUserErrors);
            throw new InvalidOperationException($"新用户默认密码不符合安全策略: {errorMsg}");
        }

        Log.Information("默认密码配置验证通过");
    }
}

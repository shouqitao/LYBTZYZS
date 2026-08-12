using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace LYBT.Tests.Server.Integration;

/// <summary>
/// L4 部署层工厂（Production 环境——配置契约守护 #4 #5 #7）
/// 注入完整测试环境变量（JWT Base64/合规密码/token）——校验器在测试中真实跑
/// </summary>
public class ProductionWebApiTestFactory : WebApiTestFactory
{
    protected override string TestEnvironment => "Production";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            // 生产门控 + 测试连接串（覆盖 appsettings.Production.json 的 ${} 占位符——避免校验器拦占位符）
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SystemAdmin:InitialSetupToken"] = "test-setup-token-2026",
                ["SystemAdmin:AllowAutoCreateInProduction"] = "false",
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=LYBT_Test;Trusted_Connection=True;TrustServerCertificate=True"
            });
        });
    }
}

/// <summary>
/// 配置契约异常捕获工厂（L4: 校验失败时 Program 抛 ProductionConfigurationException——
/// 测试断言启动被拦截）
/// </summary>
public class ConfigGuardTestFactory : WebApiTestFactory
{
    private readonly Action<IConfigurationBuilder>? _configOverride;

    protected override string TestEnvironment => "Production";

    public ConfigGuardTestFactory(Action<IConfigurationBuilder>? configOverride = null)
    {
        _configOverride = configOverride;
    }

    public Type? StartupException { get; private set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SystemAdmin:InitialSetupToken"] = "test-setup-token-2026",
                ["SystemAdmin:AllowAutoCreateInProduction"] = "false",
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=LYBT_Test;Trusted_Connection=True;TrustServerCertificate=True"
            });
            _configOverride?.Invoke(config);
        });
        builder.ConfigureServices(_ => { }); // 保持默认——让 Program 校验执行
    }
}

// P2-18-7 Respawn Sqlite InMemory已评估：Server强制SQL Server，CI需容器，本地贡献体验待优化
// P2-18-1 Respawn忽略AspNetRoles/Users已评估：sysadmin保留提升速度但顺序依赖，已在 tests/AGENTS.md 标注测试需幂等
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace LYBT.Tests.Server.Integration;

/// <summary>
/// 测试数据库连接串（L2-L4 方案决策 1: 192.168.190.243 LYBTDB_Test——
/// 从 TEST_DB_CONNECTION 环境变量读取；未设置时 DB 依赖测试 Skip）
/// </summary>
public static class TestDatabase
{
    public const string EnvVar = "TEST_DB_CONNECTION";

    /// <summary>连接串仅从环境变量读取（用户设置 TEST_DB_CONNECTION 后激活真实 SQL 测试——不猜测凭据）</summary>
    public static string? ConnectionString =>
        Environment.GetEnvironmentVariable(EnvVar);

    public static bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ConnectionString);

    /// <summary>DB 依赖测试未配置时的 Skip 原因</summary>
    public static string SkipReason =>
        $"{EnvVar} 未设置且默认连接不可达——设置环境变量后启用真实 SQL 测试";
}

/// <summary>
/// WebAPI 系统级测试工厂（L3: WebApplicationFactory 真实启动 Remote WebAPI）
/// 注入测试配置：测试库连接串（env 驱动）+ JWT 测试密钥 + Swagger 启用 + 测试密码
/// </summary>
public class WebApiTestFactory : WebApplicationFactory<Program>
{
    /// <summary>L4 部署层用 Production 环境（子类覆盖）</summary>
    protected virtual string TestEnvironment => "Development";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(TestEnvironment);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                // B-16: 不再覆盖 Swagger:Enabled——非生产环境由环境判定自动启用；生产环境（L4 工厂）
                // 必须保持 appsettings.Production.json 的默认关闭语义，否则生产暴露面契约不可测。
                ["Jwt:SecretKey"] = "VGVzdFNlY3JldEtleV9NaW5MZW5ndGgzMkNoYXJzX0ZvckpXVFRva2VuR2VuX0xZQlRfMTIzNDU2",
                ["DefaultPasswords:SysAdminPassword"] = "Admin@Lybt2026",
                ["DefaultPasswords:NewUserPassword"] = "User@Lybt2026",
                ["Database:EnsureCreatedInDevelopment"] = "false",
                ["Database:AutoMigrate"] = "false"
            };
            if (TestDatabase.IsConfigured)
                settings["ConnectionStrings:DefaultConnection"] = TestDatabase.ConnectionString!;

            config.AddInMemoryCollection(settings);
        });
    }
}

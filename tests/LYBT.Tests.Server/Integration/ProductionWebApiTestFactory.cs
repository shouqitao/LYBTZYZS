using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace LYBT.Tests.Server.Integration;

/// <summary>
/// L4 部署层工厂（Production 环境——配置契约守护 #4 #5 #7）
/// 注入完整测试环境变量（JWT Base64/合规密码/token）——校验器在测试中真实跑
///
/// 注意：注入用**进程环境变量**而非 in-memory——Program.AddConfigurationClosedLoop
/// 会移除宿主默认配置源（含 WebApplicationFactory 的 in-memory 覆盖）后重建 config/ 链，
/// in-memory 覆盖在重建中被丢弃，只有环境变量源存活（真实部署语义）。
/// </summary>
public class ProductionWebApiTestFactory : WebApiTestFactory
{
    protected override string TestEnvironment => "Production";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        // 进程级环境变量（记录原值，Dispose 还原——见 CreateClient 包装）
        // Production 校验要求：JWT/默认密码/连接串/SystemAdmin 全部注入，避免占位符拦截启动
        SetTestEnv(new Dictionary<string, string?>
        {
            ["SystemAdmin__InitialSetupToken"] = "test-setup-token-2026",
            ["SystemAdmin__AllowAutoCreateInProduction"] = "false",
            ["Jwt__SecretKey"] = "VGVzdFNlY3JldEtleV9NaW5MZW5ndGgzMkNoYXJzX0ZvckpXVFRva2VuR2VuX0xZQlRfMTIzNDU2",
            ["DefaultPasswords__SysAdminPassword"] = "Admin@Lybt2026",
            ["DefaultPasswords__NewUserPassword"] = "User@Lybt2026",
            ["ConnectionStrings__DefaultConnection"] = "Server=localhost;Database=LYBT_Test;Trusted_Connection=True;TrustServerCertificate=True"
        });
    }

    private readonly Dictionary<string, string?> _originalEnv = new();

    private void SetTestEnv(Dictionary<string, string?> vars)
    {
        foreach (var (key, value) in vars)
        {
            _originalEnv[key] = Environment.GetEnvironmentVariable(key);
            Environment.SetEnvironmentVariable(key, value);
        }
    }

    protected override void Dispose(bool disposing)
    {
        foreach (var (key, value) in _originalEnv)
            Environment.SetEnvironmentVariable(key, value);
        base.Dispose(disposing);
    }
}


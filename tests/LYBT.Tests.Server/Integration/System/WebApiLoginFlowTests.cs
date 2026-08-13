using System.Net;
using System.Net.Http.Json;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Tests.Server.Integration;

namespace LYBT.Tests.Server.Integration.System;

/// <summary>
/// L3 系统层补充（登录链路——需真实 SQL：TEST_DB_CONNECTION 配置后激活——
/// 拦截上线坑 #2（IdentityDbContext 映射——登录查询 Users 表列映射）+ 认证链）
/// </summary>
[Collection("WebApiHostTests")]
public class WebApiLoginFlowTests
{
    [Fact]
    public async Task Login_WithSeededAdmin_ReturnsToken_WhenDbConfigured()
    {
        if (!TestDatabase.IsConfigured)
            return; // 未配置 TEST_DB_CONNECTION——真实登录验证跳过（标注：需用户设置环境变量）

        var original = Environment.GetEnvironmentVariable("DefaultPasswords__SysAdminPassword");
        Environment.SetEnvironmentVariable("DefaultPasswords__SysAdminPassword", "Admin@Lybt2026");
        try
        {
            using var factory = new WebApiTestFactory();
            using var client = factory.CreateClient();

            var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
            {
                UserName = "admin",
                Password = "Admin@Lybt2026"
            });

            response.StatusCode.Should().Be(HttpStatusCode.OK,
                "种子 admin 登录应成功——#2 列映射正确（LastLoginAt 查询不抛）");
            var json = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
            json!.Data.Should().NotBeNull();
            json.Data!.Token.Should().NotBeNullOrWhiteSpace();
            json.Data.RefreshToken.Should().NotBeNullOrWhiteSpace("T4 P0#2: 登录必须签发刷新凭据");
        }
        finally
        {
            Environment.SetEnvironmentVariable("DefaultPasswords__SysAdminPassword", original);
        }
    }
}

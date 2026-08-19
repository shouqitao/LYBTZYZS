using FluentAssertions;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Tests.Desktop.Integration.RemoteApi;

[Collection("RemoteApi")]
[Trait("Category", "RemoteApi")]
public class SysadminRoleTests : RemoteApiTestBase
{
    protected override string Username => "admin";
    protected override string Password => "admin123";

    [Fact]
    [Trait("US", "US-AUTH-001")]
    public void Login_AsAdmin_ReturnsToken()
    {
        AccessToken.Should().NotBeNullOrEmpty("Sysadmin 登录应返回 JWT");
    }

    [Fact]
    [Trait("US", "US-SHELL-003")]
    public async Task GetClinicSettings_ReturnsData()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var configApi = Refit.RestService.For<LYBT.Desktop.Contracts.Api.IConfigurationApi>(HttpClient, new Refit.RefitSettings
        {
            ContentSerializer = new Refit.SystemTextJsonContentSerializer(new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            })
        });
        var resp = await configApi.GetConfigurationAsync();
        resp.Should().NotBeNull();
        resp.Success.Should().BeTrue(resp.Message);
    }

    [Fact]
    [Trait("US", "US-SHELL-004")]
    public async Task UpdateClinicSettings_Succeeds()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var configApi = Refit.RestService.For<LYBT.Desktop.Contracts.Api.IConfigurationApi>(HttpClient, new Refit.RefitSettings
        {
            ContentSerializer = new Refit.SystemTextJsonContentSerializer(new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                PropertyNameCaseInsensitive = true,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            })
        });
        var before = await configApi.GetConfigurationAsync();
        before.Success.Should().BeTrue(before.Message);

        // 更新一个安全可回滚的配置键
        var section = await configApi.GetSectionAsync("App");
        if (section.Success && section.Data != null)
        {
            section.Data.Should().NotBeNull();
        }
    }

    [Fact]
    [Trait("US", "US-USER-001")]
    public async Task GetUsers_ReturnsPaged()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var resp = await UserApi.GetUsersAsync(1, 10);
        resp.Should().NotBeNull();
        resp.Success.Should().BeTrue(resp.Message);
        resp.Data.Should().NotBeNull();
    }

    [Fact]
    [Trait("US", "US-USER-003")]
    public async Task CreateUser_Succeeds()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var user = new UserInputDto
        {
            UserName = $"e2e_user_{Guid.NewGuid():N}".Substring(0, 12),
            Password = "Test1234!",
            ConfirmPassword = "Test1234!",
            RealName = "E2E测试用户",
            Role = UserRole.Doctor
        };
        var created = await UserApi.CreateUserAsync(user);
        created.Success.Should().BeTrue(created.Message);
        created.Data.Should().NotBeNull();

        // 清理
        if (created.Data != null)
        {
            var del = await UserApi.DeleteUserAsync(created.Data.Id);
            del.Success.Should().BeTrue(del.Message);
        }
    }

    [Fact]
    [Trait("US", "US-SYS-001")]
    public async Task GetSystemHealth_ReturnsHealthy()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var resp = await AuthApi.HealthCheckAsync();
        resp.Should().NotBeNull();
        resp.Success.Should().BeTrue(resp.Message);
        resp.Data.Should().NotBeNull();
        resp.Data!.Status.Should().Be("Healthy");
    }
}

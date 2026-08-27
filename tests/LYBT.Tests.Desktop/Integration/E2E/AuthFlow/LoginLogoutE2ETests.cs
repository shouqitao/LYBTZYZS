// ---------------------------------------------------------------------------
// LoginLogoutE2ETests — US-AUTH-001 登录→Token→Claims→注销 全链路
// 真实链路：桌面 IApiClientIdentity → LocalWebAPI AuthController → Identity → LocalDB
// ---------------------------------------------------------------------------

using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Enums;

namespace LYBT.Tests.Desktop.E2E.AuthFlow;

[Collection("E2ELocal")]
public class LoginLogoutE2ETests : E2ETestBase
{
    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenAndUser()
    {
        var login = await LoginAsAdminAsync();

        Assert.False(string.IsNullOrEmpty(login.Token));
        Assert.Equal("admin", login.User.UserName);
        Assert.Equal(UserRole.Admin, login.User.Role);
        Assert.Equal(CommonStatus.Enabled, login.User.Status);
        Assert.True(login.ExpiresAt > DateTime.UtcNow);
        Assert.False(string.IsNullOrEmpty(login.RefreshToken));
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_Fails()
    {
        // 错误密码与不存在用户均返回 401（登录限流 5 次/分钟，两断言合并为一次测试）
        await AssertUnauthorizedAsync(() => IdentityApi.LoginAsync(
            new LoginRequest { UserName = "admin", Password = "Wrong-Password-123!" }));

        await AssertUnauthorizedAsync(() => IdentityApi.LoginAsync(
            new LoginRequest { UserName = "no_such_user_xyz", Password = "Whatever@123" }));
    }

    [Fact]
    public async Task ValidateToken_ReturnsClaims()
    {
        await LoginAsDoctorAsync();

        var result = await IdentityApi.ValidateTokenAsync();

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.True(result.Data!.IsValid);
        Assert.Equal("doctor", result.Data.Username);
        Assert.Equal("Doctor", result.Data.Role);
    }

    [Fact]
    public async Task Logout_WithValidSession_ReturnsSuccess()
    {
        var login = await LoginAsReceptionistAsync();

        var result = await IdentityApi.LogoutAsync(new LogoutRequest
        {
            UserName = login.User.UserName,
            RefreshToken = login.RefreshToken
        });

        Assert.True(result.Success, result.Message);
    }

    // 角色身份覆盖：admin（Login_WithValidCredentials）、doctor（ValidateToken）、
    // receptionist（Logout）、sysadmin（Configuration/Diagnostics 各测试）已覆盖 4 角色，
    // 不再单独登录 4 次（LocalWebAPI 登录限流 5 次/分钟）。
}

// ---------------------------------------------------------------------------
// TokenRefreshE2ETests — US-AUTH-002 过期→刷新→继续使用 全链路
// 真实链路：桌面 IApiClientIdentity → LocalWebAPI AuthController(refresh/auto-login) → LocalDB
// ---------------------------------------------------------------------------

using LYBT.Shared.Models.Contracts.Auth;
using System.Net.Http;

namespace LYBT.Tests.Desktop.E2E.AuthFlow;

[Collection("E2ELocal")]
public class TokenRefreshE2ETests : E2ETestBase
{
    [Fact]
    public async Task Refresh_WithValidRefreshToken_ReturnsNewToken()
    {
        var login = await LoginAsAdminAsync();

        var result = await IdentityApi.RefreshTokenAsync(new RefreshTokenRequest
        {
            RefreshToken = login.RefreshToken
        });
        // 登录响应契约：必须携带 refresh token 与过期时间
        Assert.False(string.IsNullOrEmpty(login.RefreshToken));
        Assert.True(login.ExpiresAt > DateTime.UtcNow);

        Assert.True(result.Success, result.Message);
        Assert.NotNull(result.Data);
        // 本地 refresh 契约：返回新访问令牌与过期时间（本地不轮换 refresh token，不断言其非空/不同）
        Assert.False(string.IsNullOrEmpty(result.Data!.Token));
        Assert.True(result.Data.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_Fails()
    {
        await LoginAsAdminAsync();

        await Assert.ThrowsAsync<HttpRequestException>(() => IdentityApi.RefreshTokenAsync(
            new RefreshTokenRequest { RefreshToken = "invalid-refresh-token" }));
    }

    [Fact]
    public async Task Login_WithRememberMe_ReturnsAutoLoginToken_AndAutoLoginSucceeds()
    {
        var login = await LoginAsAdminAsync();

        // 首次登录请求 RememberMe=true 获取自动登录令牌
        var rememberLogin = await IdentityApi.LoginAsync(new LoginRequest
        {
            UserName = "admin",
            Password = "Admin@123456",
            RememberMe = true
        });
        Assert.True(rememberLogin.Success, rememberLogin.Message);
        Assert.False(string.IsNullOrEmpty(rememberLogin.Data!.AutoLoginToken));

        // 使用自动登录令牌换新会话
        var autoLogin = await IdentityApi.LoginWithAutoTokenAsync(new AutoLoginRequest
        {
            UserName = "admin",
            AutoLoginToken = rememberLogin.Data.AutoLoginToken!
        });

        Assert.True(autoLogin.Success, autoLogin.Message);
        Assert.NotNull(autoLogin.Data);
        Assert.False(string.IsNullOrEmpty(autoLogin.Data!.Token));
    }

    [Fact]
    public async Task RefreshToken_ThenNewToken_WorksForAuthorizedCall()
    {
        var login = await LoginAsAdminAsync();

        var refreshed = await IdentityApi.RefreshTokenAsync(new RefreshTokenRequest
        {
            RefreshToken = login.RefreshToken
        });
        Assert.True(refreshed.Success);

        // 切换到刷新后的 Token，验证其仍可访问受保护端点
        SetToken(refreshed.Data!.Token);
        var validate = await IdentityApi.ValidateTokenAsync();

        Assert.True(validate.Success);
        Assert.True(validate.Data!.IsValid);
    }
}

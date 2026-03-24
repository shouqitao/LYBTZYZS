using System.ComponentModel;
using System.IdentityModel.Tokens.Jwt;
using System.Windows.Input;
using FluentAssertions;
using LYBT.Desktop.Auth.ViewModels;
using LYBT.Desktop.Foundation.Security;
using LYBT.Tests.Desktop.Integration.Fixtures;
using Xunit;

namespace LYBT.Tests.Desktop.Integration.Flows;

[Collection("WebApiIntegration")]
public class AuthenticationFlowTests : IDisposable
{
    private readonly WebApiFixture _fixture;
    private readonly RealTestComposition _composition;

    public AuthenticationFlowTests(WebApiFixture fixture)
    {
        _fixture = fixture;
        _composition = new RealTestComposition()
            .WithRealRefitClient(_fixture.ApiClient)
            .Build();
    }

    public void Dispose()
    {
        if (_composition.GetServiceProvider() is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    [StaFact]
    public async Task Login_WithValidCredentials_ReturnsRealToken()
    {
        // Arrange
        var loginVm = _composition.Resolve<LoginViewModel>();
        var tcs = new TaskCompletionSource<bool>();
        
        loginVm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(LoginViewModel.IsLoading) && !loginVm.IsLoading)
            {
                tcs.TrySetResult(true);
            }
        };
        
        loginVm.Username = WebApiFixture.TestDoctorUsername;
        loginVm.Password = WebApiFixture.TestDoctorPassword;

        // Act
        loginVm.LoginCommand.Execute(null);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));

        // Assert
        var tokenStore = _composition.Resolve<ITokenStorageService>();
        var token = await tokenStore.GetTokenAsync();
        token.Should().NotBeNullOrEmpty("登录成功后 Token 应该被保存");
        
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        
        jwtToken.Claims.Should().Contain(c => c.Type == "role" && c.Value == "Doctor", 
            "JWT 应该包含 role claim 且值为 Doctor");
        jwtToken.Claims.Should().Contain(c => c.Type == "unique_name" && c.Value == WebApiFixture.TestDoctorUsername,
            "JWT 应该包含 username claim");
        
        jwtToken.Issuer.Should().NotBeNullOrEmpty("JWT 应该有 Issuer");
        jwtToken.Audiences.Should().NotBeEmpty("JWT 应该有 Audience");
    }

    [StaFact]
    public async Task Login_WithInvalidCredentials_ShowsError()
    {
        // Arrange
        var loginVm = _composition.Resolve<LoginViewModel>();
        var tcs = new TaskCompletionSource<bool>();
        
        loginVm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(LoginViewModel.IsLoading) && !loginVm.IsLoading)
            {
                tcs.TrySetResult(true);
            }
        };
        
        loginVm.Username = "invalid_user";
        loginVm.Password = "wrong_password";

        // Act
        loginVm.LoginCommand.Execute(null);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));

        // Assert
        loginVm.ErrorMessage.Should().NotBeNullOrEmpty("无效凭据应该显示错误信息");
        
        var tokenStore = _composition.Resolve<ITokenStorageService>();
        var token = await tokenStore.GetTokenAsync();
        token.Should().BeNullOrEmpty("登录失败时不应该保存 Token");
    }

    [StaFact]
    public async Task Login_WithValidCredentials_UpdatesTokenStore()
    {
        // Arrange
        var tokenStore = _composition.Resolve<ITokenStorageService>();
        await tokenStore.ClearAuthenticationAsync();
        
        var loginVm = _composition.Resolve<LoginViewModel>();
        var tcs = new TaskCompletionSource<bool>();
        
        loginVm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(LoginViewModel.IsLoading) && !loginVm.IsLoading)
            {
                tcs.TrySetResult(true);
            }
        };
        
        loginVm.Username = WebApiFixture.TestDoctorUsername;
        loginVm.Password = WebApiFixture.TestDoctorPassword;

        // Act
        loginVm.LoginCommand.Execute(null);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));

        // Assert
        var savedResponse = await tokenStore.GetLoginResponseAsync();
        savedResponse.Should().NotBeNull("登录响应应该被保存到 TokenStore");
        savedResponse!.User.Should().NotBeNull("用户信息应该被保存");
        savedResponse.User.UserName.Should().Be(WebApiFixture.TestDoctorUsername);
        savedResponse.User.Role.Should().Be(LYBT.Shared.Models.Enums.UserRole.Doctor);
        
        var refreshToken = await tokenStore.GetRefreshTokenAsync();
        refreshToken.Should().NotBeNullOrEmpty("RefreshToken 应该被保存");
    }

    [StaFact]
    public async Task TokenValidator_ValidatesRealApiToken()
    {
        // Arrange
        var loginVm = _composition.Resolve<LoginViewModel>();
        var tcs = new TaskCompletionSource<bool>();
        
        loginVm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(LoginViewModel.IsLoading) && !loginVm.IsLoading)
            {
                tcs.TrySetResult(true);
            }
        };
        
        loginVm.Username = WebApiFixture.TestDoctorUsername;
        loginVm.Password = WebApiFixture.TestDoctorPassword;

        // Act - 登录
        loginVm.LoginCommand.Execute(null);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));

        // Assert - 验证 Token
        var tokenStore = _composition.Resolve<ITokenStorageService>();
        var token = await tokenStore.GetTokenAsync();
        
        var tokenValidator = _composition.Resolve<ITokenValidator>();
        var validationResult = await tokenValidator.ValidateTokenAsync(token!);
        
        validationResult.IsValid.Should().BeTrue("真实 API 返回的 Token 应该通过验证");
        validationResult.UserInfo.Should().NotBeNull("应该能提取用户信息");
        validationResult.UserInfo!.UserName.Should().Be(WebApiFixture.TestDoctorUsername);
        validationResult.UserInfo.Role.Should().Be("Doctor");
    }

    [StaFact]
    public async Task MultipleLogins_UpdatesToken()
    {
        // Arrange
        var tokenStore = _composition.Resolve<ITokenStorageService>();
        var loginVm = _composition.Resolve<LoginViewModel>();
        
        var tcs1 = new TaskCompletionSource<bool>();
        loginVm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(LoginViewModel.IsLoading) && !loginVm.IsLoading)
            {
                tcs1.TrySetResult(true);
            }
        };
        
        loginVm.Username = WebApiFixture.TestDoctorUsername;
        loginVm.Password = WebApiFixture.TestDoctorPassword;
        loginVm.LoginCommand.Execute(null);
        await tcs1.Task.WaitAsync(TimeSpan.FromSeconds(10));
        
        var firstToken = await tokenStore.GetTokenAsync();
        
        await Task.Delay(1000);
        
        // Act - 第二次登录
        await tokenStore.ClearAuthenticationAsync();
        
        var tcs2 = new TaskCompletionSource<bool>();
        var loginVm2 = _composition.Resolve<LoginViewModel>();
        loginVm2.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(LoginViewModel.IsLoading) && !loginVm2.IsLoading)
            {
                tcs2.TrySetResult(true);
            }
        };
        
        loginVm2.Username = WebApiFixture.TestDoctorUsername;
        loginVm2.Password = WebApiFixture.TestDoctorPassword;
        loginVm2.LoginCommand.Execute(null);
        await tcs2.Task.WaitAsync(TimeSpan.FromSeconds(10));
        
        var secondToken = await tokenStore.GetTokenAsync();
        
        // Assert
        secondToken.Should().NotBeNullOrEmpty("第二次登录也应该成功");
        secondToken.Should().NotBe(firstToken, "每次登录应该生成不同的 Token");
    }
}

using FluentAssertions;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Tests.Desktop.EndToEnd.Infrastructure;
using Refit;
using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.Tests.Desktop.EndToEnd.Foundation;

public class AuthNegativeTests : WebApiE2ETestBase
{
    private readonly ITestOutputHelper _output;

    public AuthNegativeTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Negative")]
    public async Task Login_EmptyUsername_ShouldFail()
    {
        var request = new LoginRequest { UserName = "", Password = "SomePass123!" };

        try
        {
            var response = await AuthApi.LoginAsync(request);
            response.Success.Should().BeFalse("空用户名不应登录成功");
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().BeOneOf(HttpStatusCode.UnprocessableEntity, HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Negative")]
    public async Task Login_EmptyPassword_ShouldFail()
    {
        var request = new LoginRequest { UserName = "sysadmin", Password = "" };

        try
        {
            var response = await AuthApi.LoginAsync(request);
            response.Success.Should().BeFalse("空密码不应登录成功");
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().BeOneOf(HttpStatusCode.UnprocessableEntity, HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Negative")]
    public async Task Login_WrongPassword_ShouldFail()
    {
        var request = new LoginRequest { UserName = "sysadmin", Password = "TotallyWrongPassword!" };

        try
        {
            var response = await AuthApi.LoginAsync(request);
            response.Success.Should().BeFalse("错误密码不应登录成功");
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Negative")]
    public async Task Login_NonexistentUser_ShouldFail()
    {
        var request = new LoginRequest
        {
            UserName = $"ghost_{Guid.NewGuid():N}",
            Password = "SomePass123!"
        };

        try
        {
            var response = await AuthApi.LoginAsync(request);
            response.Success.Should().BeFalse("不存在的用户不应登录成功");
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().BeOneOf(HttpStatusCode.UnprocessableEntity, HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "Negative")]
    public async Task RefreshToken_InvalidToken_ShouldFail()
    {
        var request = new RefreshTokenRequest { RefreshToken = "invalid-refresh-token-value" };

        try
        {
            var response = await AuthApi.RefreshTokenAsync(request);
            response.Success.Should().BeFalse("无效的RefreshToken不应刷新成功");
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
        }
    }
}

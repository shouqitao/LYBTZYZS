using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using LYBT.Module.Identity.Services;
using LYBT.Shared.Configuration.Options.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// 刷新令牌签发单测（AC-TEST P0#2 补齐：T4 修复「登录/刷新未签发 RefreshToken」——
/// 服务端签发侧原无单测；本测试守护 access=refresh 凭据链 + 过期令牌刷新 + 身份保留）
/// </summary>
public class RefreshTokenIssuanceTests
{
    private readonly JwtService _sut;
    private readonly JwtOptions _jwtOptions;

    public RefreshTokenIssuanceTests()
    {
        _jwtOptions = new JwtOptions
        {
            SecretKey = "TestSecretKeyForJWTAuthentication_32CharsLong!@#$",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpirationMinutes = 120,
            ClockSkewSeconds = 30
        };
        var environment = new TestWebHostEnvironment { EnvironmentName = "Development" };
        _sut = new JwtService(CreateOptionsMonitor(_jwtOptions), environment);
    }

    private static IOptionsMonitor<T> CreateOptionsMonitor<T>(T value) where T : class, new()
        => new TestOptionsMonitor<T>(Options.Create(value));

    [Fact]
    public void RefreshToken_WithValidExpiredToken_IssuesNewToken_WithRefreshTokenSet()
    {
        var userId = Guid.NewGuid().ToString();
        var token = _sut.GenerateToken(userId, "testuser", UserRole.Doctor);

        var result = _sut.RefreshToken(token);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Token.Should().NotBeNullOrWhiteSpace();
        // P0#2 核心断言：刷新响应必须携带刷新凭据（access token 即刷新凭据——客户端 TokenRefreshHandler 依赖）
        result.Value.RefreshToken.Should().Be(result.Value.Token);
        result.Value.User.Should().NotBeNull();
        result.Value.User!.Id.ToString().Should().Be(userId);
    }

    [Fact]
    public void RefreshToken_PreservesUserIdentity()
    {
        var token = _sut.GenerateToken("user-42", "doctor-a", UserRole.Doctor);

        var result = _sut.RefreshToken(token);

        var claims = new JwtSecurityTokenHandler().ReadJwtToken(result.Value!.Token);
        // JWT 输出短 claim 名（nameid/role）——从完整 URI 反查
        var principal = new JwtSecurityTokenHandler().ValidateToken(result.Value!.Token,
            new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                    System.Text.Encoding.UTF8.GetBytes("TestSecretKeyForJWTAuthentication_32CharsLong!@#$")),
                ValidateIssuer = true,
                ValidIssuer = "TestIssuer",
                ValidateAudience = true,
                ValidAudience = "TestAudience",
                ValidateLifetime = false
            }, out _);
        principal.FindFirst(ClaimTypes.NameIdentifier)!.Value.Should().Be("user-42");
        principal.FindFirst(ClaimTypes.Role)!.Value.Should().Be(UserRole.Doctor.ToString());
    }

    [Fact]
    public void RefreshToken_WithEmptyToken_ReturnsFailure()
    {
        var result = _sut.RefreshToken(string.Empty);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("令牌不能为空");
    }

    [Fact]
    public void RefreshToken_WithGarbageToken_ReturnsFailure()
    {
        var result = _sut.RefreshToken("not-a-jwt-token");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void RefreshToken_NewToken_DiffersFromOld()
    {
        var token = _sut.GenerateToken(Guid.NewGuid().ToString(), "u", UserRole.Doctor);

        var result = _sut.RefreshToken(token);

        result.Value!.Token.Should().NotBe(token); // 旋转——旧令牌不可复用
    }
}

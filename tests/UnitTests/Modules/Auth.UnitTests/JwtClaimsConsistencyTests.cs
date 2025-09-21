using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Module.Auth.Services;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace LYBT.Module.Auth.Tests;

public class JwtClaimsConsistencyTests
{
    private readonly JwtAuthenticationService _jwtService;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    public JwtClaimsConsistencyTests()
    {
        var mockLogger = new Mock<ILogger<JwtAuthenticationService>>();
        var jwtOptions = new JwtOptions
        {
            Secret = "ThisIsAVerySecureTestSecretKey12345678901234567890",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpireMinutes = 60,
            RememberMeExpireMinutes = 43200
        };
        var mockOptions = new Mock<IOptions<JwtOptions>>();
        mockOptions.Setup(x => x.Value).Returns(jwtOptions);

        _jwtService = new JwtAuthenticationService(mockOptions.Object, mockLogger.Object);
    }

    [Fact]
    public void GenerateToken_Should_Include_Both_ClaimTypes_And_JwtRegisteredClaims()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userName = "testuser";
        var role = UserRole.Admin;

        // Act
        var token = _jwtService.GenerateToken(userId, userName, role);
        var jwtToken = _tokenHandler.ReadJwtToken(token);
        var claims = jwtToken.Claims.ToList();

        // Assert - JWT标准声明
        claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == userId);
        claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == userName);
        claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
        claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Iat);

        // Assert - ClaimTypes标准声明
        claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == userId);
        claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == userName);
        claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == role.ToString());
    }

    [Fact]
    public void ExtractUserInfo_Should_Parse_Both_New_And_Legacy_Tokens()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userName = "testuser";
        var role = UserRole.Doctor;

        // Act - 测试新格式令牌
        var newToken = _jwtService.GenerateToken(userId, userName, role);
        var newUserInfo = _jwtService.ExtractUserInfo(newToken);

        // Assert
        newUserInfo.Should().NotBeNull();
        newUserInfo!.UserId.Should().Be(userId);
        newUserInfo.Username.Should().Be(userName);
        newUserInfo.Role.Should().Be(role);
    }

    [Fact]
    public void RefreshToken_Should_Work_With_Both_Token_Formats()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userName = "refreshuser";
        var role = UserRole.Admin;

        // Act
        var originalToken = _jwtService.GenerateToken(userId, userName, role);
        var refreshedToken = _jwtService.RefreshToken(originalToken);
        var refreshedUserInfo = _jwtService.ExtractUserInfo(refreshedToken);

        // Assert
        refreshedUserInfo.Should().NotBeNull();
        refreshedUserInfo!.UserId.Should().Be(userId);
        refreshedUserInfo.Username.Should().Be(userName);
        refreshedUserInfo.Role.Should().Be(role);
    }

    [Fact]
    public void ValidateToken_Should_Return_Principal_With_Standard_Claims()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userName = "validateuser";
        var role = UserRole.Doctor;

        // Act
        var token = _jwtService.GenerateToken(userId, userName, role);
        var principal = _jwtService.ValidateToken(token);

        // Assert
        principal.Should().NotBeNull();

        // 验证可以通过多种方式获取声明
        var nameIdentifierClaim = principal!.FindFirst(ClaimTypes.NameIdentifier);
        var nameClaim = principal.FindFirst(ClaimTypes.Name);
        var roleClaim = principal.FindFirst(ClaimTypes.Role);
        var subClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub);
        var uniqueNameClaim = principal.FindFirst(JwtRegisteredClaimNames.UniqueName);

        nameIdentifierClaim?.Value.Should().Be(userId);
        nameClaim?.Value.Should().Be(userName);
        roleClaim?.Value.Should().Be(role.ToString());
        subClaim?.Value.Should().Be(userId);
        uniqueNameClaim?.Value.Should().Be(userName);
    }

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.Doctor)]
    public void GenerateToken_Should_Correctly_Encode_Different_Roles(UserRole role)
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userName = "roleuser";

        // Act
        var token = _jwtService.GenerateToken(userId, userName, role);
        var jwtToken = _tokenHandler.ReadJwtToken(token);
        var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);

        // Assert
        roleClaim.Should().NotBeNull();
        roleClaim!.Value.Should().Be(role.ToString());
    }
}
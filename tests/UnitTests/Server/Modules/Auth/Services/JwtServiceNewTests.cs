using System.Security.Claims;
using FluentAssertions;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Module.Auth.Services;
using LYBT.Server.Tests.Common.TestBase;
using LYBT.Shared.Models.Enums;
using Moq;
using Xunit;

namespace LYBT.Server.Tests.Modules.Auth.Services;

/// <summary>
/// JWT服务测试 - 使用新测试基础设施
/// 基于InMemoryConfiguration成功模式的重构版本
/// </summary>
public class JwtServiceNewTests : BaseServiceTest<JwtService>
{
    public JwtServiceNewTests()
    {
        // BaseServiceTest已经处理了所有基础设置
    }

    protected override void RegisterTestServices(IServiceCollection services)
    {
        // 注册JwtService
        services.AddTransient<JwtService>();
    }

    [Fact]
    public void GenerateToken_WithValidUser_ReturnsValidToken()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userName = "testuser";
        var role = UserRole.Doctor;

        // Act
        var token = _sut.GenerateToken(userId, userName, role);

        // Assert
        token.Should().NotBeNullOrEmpty();
        token.Split('.').Should().HaveCount(3); // JWT格式: header.payload.signature
    }

    [Fact]
    public void GenerateToken_WithValidUser_IncludesUserId()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userName = "testuser";
        var role = UserRole.Doctor;

        // Act
        var token = _sut.GenerateToken(userId, userName, role);
        var principal = _sut.ValidateToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal!.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be(userId);
    }

    [Fact]
    public void GenerateToken_WithValidUser_IncludesUsername()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userName = "testuser";
        var role = UserRole.Doctor;

        // Act
        var token = _sut.GenerateToken(userId, userName, role);
        var principal = _sut.ValidateToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal!.FindFirst(ClaimTypes.Name)?.Value.Should().Be(userName);
    }

    [Fact]
    public void GenerateToken_WithValidUser_IncludesRoles()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userName = "testuser";
        var role = UserRole.Admin;

        // Act
        var token = _sut.GenerateToken(userId, userName, role);
        var principal = _sut.ValidateToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal!.FindFirst(ClaimTypes.Role)?.Value.Should().Be(role.ToString());
    }

    [Fact]
    public void GenerateToken_WithAdditionalClaims_IncludesAllClaims()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userName = "testuser";
        var role = UserRole.Admin;
        var additionalClaims = new Dictionary<string, string>
        {
            { "IsSuperAdmin", "true" },
            { "AuthSource", "AdminSecrets" }
        };

        // Act
        var token = _sut.GenerateToken(userId, userName, role, additionalClaims);
        var principal = _sut.ValidateToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal!.FindFirst("IsSuperAdmin")?.Value.Should().Be("true");
        principal.FindFirst("AuthSource")?.Value.Should().Be("AdminSecrets");
    }

    [Fact]
    public void GenerateToken_WithEmptyUserId_ThrowsArgumentException()
    {
        // Arrange
        var userName = "testuser";
        var role = UserRole.Doctor;

        // Act & Assert
        var act = () => _sut.GenerateToken("", userName, role);
        act.Should().Throw<ArgumentException>().WithMessage("*用户ID不能为空*");
    }

    [Fact]
    public void GenerateToken_WithEmptyUserName_ThrowsArgumentException()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var role = UserRole.Doctor;

        // Act & Assert
        var act = () => _sut.GenerateToken(userId, "", role);
        act.Should().Throw<ArgumentException>().WithMessage("*用户名不能为空*");
    }

    [Fact]
    public void ValidateToken_WithValidToken_ReturnsPrincipal()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userName = "testuser";
        var role = UserRole.Doctor;
        var token = _sut.GenerateToken(userId, userName, role);

        // Act
        var principal = _sut.ValidateToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal!.Identity.Should().NotBeNull();
        principal.Identity!.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void ValidateToken_WithNullToken_ReturnsNull()
    {
        // Arrange & Act
        var principal = _sut.ValidateToken(null);

        // Assert
        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithEmptyToken_ReturnsNull()
    {
        // Arrange & Act
        var principal = _sut.ValidateToken("");

        // Assert
        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithInvalidToken_ReturnsNull()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var principal = _sut.ValidateToken(invalidToken);

        // Assert
        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithTamperedToken_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userName = "testuser";
        var role = UserRole.Doctor;
        var token = _sut.GenerateToken(userId, userName, role);

        // 篡改token（修改最后一个字符）
        var tamperedToken = token.Substring(0, token.Length - 1) + "X";

        // Act
        var principal = _sut.ValidateToken(tamperedToken);

        // Assert
        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_ExtractsClaimsCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userName = "testuser";
        var role = UserRole.Doctor;
        var token = _sut.GenerateToken(userId, userName, role);

        // Act
        var principal = _sut.ValidateToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal!.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be(userId);
        principal.FindFirst(ClaimTypes.Name)?.Value.Should().Be(userName);
        principal.FindFirst(ClaimTypes.Role)?.Value.Should().Be(role.ToString());
    }

    [Fact]
    public void GenerateToken_IncludesIssuer()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userName = "testuser";
        var role = UserRole.Doctor;

        // Act
        var token = _sut.GenerateToken(userId, userName, role);
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        jwtToken.Issuer.Should().Be("LYBT-Test");
    }

    [Fact]
    public void GenerateToken_IncludesAudience()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userName = "testuser";
        var role = UserRole.Doctor;

        // Act
        var token = _sut.GenerateToken(userId, userName, role);
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        jwtToken.Audiences.Should().Contain("LYBT-TestUsers");
    }

    [Fact]
    public void GenerateToken_IncludesJti()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userName = "testuser";
        var role = UserRole.Doctor;

        // Act
        var token = _sut.GenerateToken(userId, userName, role);
        var principal = _sut.ValidateToken(token);

        // Assert
        principal.Should().NotBeNull();
        var jti = principal!.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
        jti.Should().NotBeNullOrEmpty();
        Guid.TryParse(jti, out _).Should().BeTrue();
    }

    [Fact]
    public void GenerateToken_TokenExpiresAfterConfiguredMinutes()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userName = "testuser";
        var role = UserRole.Doctor;

        // Act
        var token = _sut.GenerateToken(userId, userName, role);
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert - 配置为30分钟过期
        var expectedExpiry = DateTime.UtcNow.AddMinutes(30);
        jwtToken.ValidTo.Should().BeCloseTo(expectedExpiry, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void GenerateToken_SignsWithSecretKey()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userName = "testuser";
        var role = UserRole.Doctor;

        // Act
        var token = _sut.GenerateToken(userId, userName, role);

        // Assert - 验证token可以用正确的密钥验证
        var principal = _sut.ValidateToken(token);
        principal.Should().NotBeNull();
    }
}
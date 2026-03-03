using System.Security.Claims;
using FluentAssertions;
using LYBT.Module.Auth.Services;
using LYBT.Shared.Configuration.Options.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace LYBT.Tests.Unit.Modules.Auth.Services;

/// <summary>
/// JwtService 单元测试
/// Issue #864 - Phase 2.3: Auth 模块测试
/// unify-configuration-system: 迁移到 JwtOptions
/// </summary>
public class JwtServiceTests
{
    private readonly IOptions<JwtOptions> _options;
    private readonly IConfiguration _configuration;
    private readonly JwtService _sut;

    public JwtServiceTests()
    {
        _options = Substitute.For<IOptions<JwtOptions>>();
        // unify-configuration-system: 使用扁平化的 JwtOptions
        _options.Value.Returns(new JwtOptions
        {
            SecretKey = "ThisIsAVeryStrongSecretKeyForTesting123456789012345678901234567890",
            Issuer = "LYBT",
            Audience = "LYBTUsers",
            AccessTokenExpirationMinutes = 30,
            RefreshTokenExpirationDays = 7
        });

        _configuration = CreateMockConfiguration();
        _sut = new JwtService(_options, _configuration);
    }

    private static IConfiguration CreateMockConfiguration()
    {
        // unify-configuration-system: 使用扁平化配置路径 "Jwt:*"
        var configValues = new Dictionary<string, string>
        {
            ["Jwt:SecretKey"] = "ThisIsAVeryStrongSecretKeyForTesting123456789012345678901234567890",
            ["Jwt:Issuer"] = "LYBT",
            ["Jwt:Audience"] = "LYBTUsers",
            ["Jwt:AccessTokenExpirationMinutes"] = "30",
            ["Jwt:RefreshTokenExpirationDays"] = "7"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(configValues!)
            .Build();
    }

    #region Token 生成测试 (基础重载)

    [Fact]
    public void GenerateToken_WithUserDto_GeneratesValidToken()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userName = "testuser";
        var role = LYBT.Shared.Models.Enums.UserRole.Doctor;

        // Act
        var token = _sut.GenerateToken(userId, userName, role);

        // Assert
        token.Should().NotBeNullOrEmpty();
        token.Split('.').Should().HaveCount(3); // JWT 格式: header.payload.signature
    }

    [Fact]
    public void GenerateToken_WithUserDto_IncludesUserId()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userName = "testuser";
        var role = LYBT.Shared.Models.Enums.UserRole.Doctor;

        // Act
        var token = _sut.GenerateToken(userId, userName, role);
        var principal = _sut.ValidateToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal!.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be(userId);
    }

    [Fact]
    public void GenerateToken_WithUserDto_IncludesUsername()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userName = "testuser";
        var role = LYBT.Shared.Models.Enums.UserRole.Doctor;

        // Act
        var token = _sut.GenerateToken(userId, userName, role);
        var principal = _sut.ValidateToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal!.FindFirst(ClaimTypes.Name)?.Value.Should().Be(userName);
    }

    [Fact]
    public void GenerateToken_WithUserDto_IncludesRoles()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userName = "testuser";
        var role = LYBT.Shared.Models.Enums.UserRole.Doctor;

        // Act
        var token = _sut.GenerateToken(userId, userName, role);
        var principal = _sut.ValidateToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal!.FindFirst(ClaimTypes.Role)?.Value.Should().Be(role.ToString());
    }

    [Fact]
    public void GenerateToken_TokenExpiresAfter8Hours()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userName = "testuser";
        var role = LYBT.Shared.Models.Enums.UserRole.Doctor;

        // Act
        var token = _sut.GenerateToken(userId, userName, role);
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        // Issue #2244: 修改期望值匹配配置的30分钟（而非8小时）
        var expectedExpiry = DateTime.UtcNow.AddMinutes(30);
        jwtToken.ValidTo.Should().BeCloseTo(expectedExpiry, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void GenerateToken_WithEmptyUserId_ThrowsArgumentException()
    {
        // Arrange
        var userName = "testuser";
        var role = LYBT.Shared.Models.Enums.UserRole.Doctor;

        // Act & Assert
        var act = () => _sut.GenerateToken("", userName, role);
        act.Should().Throw<ArgumentException>().WithMessage("*用户ID不能为空*");
    }

    [Fact]
    public void GenerateToken_WithEmptyUserName_ThrowsArgumentException()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var role = LYBT.Shared.Models.Enums.UserRole.Doctor;

        // Act & Assert
        var act = () => _sut.GenerateToken(userId, "", role);
        act.Should().Throw<ArgumentException>().WithMessage("*用户名不能为空*");
    }

    #endregion

    #region Token 生成测试 (Claims重载)

    [Fact]
    public void GenerateToken_WithAdditionalClaims_IncludesAllClaims()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userName = "testuser";
        var role = LYBT.Shared.Models.Enums.UserRole.Admin;
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
    public void GenerateToken_WithNullAdditionalClaims_GeneratesBasicToken()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userName = "testuser";
        var role = LYBT.Shared.Models.Enums.UserRole.Doctor;

        // Act
        // Issue #2244: 强制转换null为Dictionary类型，明确调用5参数版本（避免重载歧义传给userType）
        var token = _sut.GenerateToken(userId, userName, role, (Dictionary<string, string>)null!);

        // Assert
        token.Should().NotBeNullOrEmpty();
        token.Split('.').Should().HaveCount(3);
    }

    [Fact]
    public void GenerateToken_IncludesIssuer()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userName = "testuser";
        var role = LYBT.Shared.Models.Enums.UserRole.Doctor;

        // Act
        var token = _sut.GenerateToken(userId, userName, role);
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        jwtToken.Issuer.Should().Be("LYBT");
    }

    [Fact]
    public void GenerateToken_IncludesAudience()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userName = "testuser";
        var role = LYBT.Shared.Models.Enums.UserRole.Doctor;

        // Act
        var token = _sut.GenerateToken(userId, userName, role);
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        jwtToken.Audiences.Should().Contain("LYBTUsers");
    }

    [Fact]
    public void GenerateToken_IncludesJti()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userName = "testuser";
        var role = LYBT.Shared.Models.Enums.UserRole.Doctor;

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
    public void GenerateToken_SignsWithSecretKey()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userName = "testuser";
        var role = LYBT.Shared.Models.Enums.UserRole.Doctor;

        // Act
        var token = _sut.GenerateToken(userId, userName, role);

        // Assert - 验证 token 可以用正确的密钥验证
        var principal = _sut.ValidateToken(token);
        principal.Should().NotBeNull();
    }

    #endregion

    #region Token 验证测试

    [Fact]
    public void ValidateToken_WithValidToken_ReturnsPrincipal()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userName = "testuser";
        var role = LYBT.Shared.Models.Enums.UserRole.Doctor;
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
        var role = LYBT.Shared.Models.Enums.UserRole.Doctor;
        var token = _sut.GenerateToken(userId, userName, role);

        // 篡改 token（在签名部分插入额外字符，确保签名无效）
        var parts = token.Split('.');
        var tamperedSignature = "INVALID" + parts[2].Substring(7);
        var tamperedToken = $"{parts[0]}.{parts[1]}.{tamperedSignature}";

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
        var role = LYBT.Shared.Models.Enums.UserRole.Doctor;
        var token = _sut.GenerateToken(userId, userName, role);

        // Act
        var principal = _sut.ValidateToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal!.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be(userId);
        principal.FindFirst(ClaimTypes.Name)?.Value.Should().Be(userName);
        principal.FindFirst(ClaimTypes.Role)?.Value.Should().Be(role.ToString());
    }

    #endregion

    #region 密钥验证测试

    [Fact]
    public void Constructor_WithStrongKey_DoesNotThrow()
    {
        // Arrange
        var options = Substitute.For<IOptions<JwtOptions>>();
        // unify-configuration-system: 使用扁平化的 JwtOptions
        options.Value.Returns(new JwtOptions
        {
            SecretKey = "ThisIsAVeryStrongSecretKeyForTesting123456789012345678901234567890",
            Issuer = "LYBT",
            Audience = "LYBTUsers"
        });

        var config = Substitute.For<IConfiguration>();
        config["Jwt:SecretKey"]
              .Returns("ThisIsAVeryStrongSecretKeyForTesting123456789012345678901234567890");

        // Act & Assert
        var act = () => new JwtService(options, config);
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithTooShortKey_ThrowsArgumentException()
    {
        // Arrange
        var options = Substitute.For<IOptions<JwtOptions>>();
        // unify-configuration-system: 使用扁平化的 JwtOptions
        options.Value.Returns(new JwtOptions
        {
            SecretKey = "TooShortKey123",
            Issuer = "LYBT",
            Audience = "LYBTUsers"
        });

        var config = Substitute.For<IConfiguration>();
        config["Jwt:SecretKey"].Returns("TooShortKey123");

        // Act & Assert
        var act = () => new JwtService(options, config);
        act.Should().Throw<ArgumentException>().WithMessage("*长度不足*");
    }

    [Fact]
    public void Constructor_WithMinimum32CharsKey_DoesNotThrow()
    {
        // Arrange
        var options = Substitute.For<IOptions<JwtOptions>>();
        // unify-configuration-system: 使用扁平化的 JwtOptions
        options.Value.Returns(new JwtOptions
        {
            SecretKey = "12345678901234567890123456789012", // 正好 32 字符
            Issuer = "LYBT",
            Audience = "LYBTUsers"
        });

        var config = Substitute.For<IConfiguration>();
        config["Jwt:SecretKey"]
              .Returns("12345678901234567890123456789012");

        // Act & Assert
        var act = () => new JwtService(options, config);
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithEmptySecretKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = Substitute.For<IOptions<JwtOptions>>();
        // unify-configuration-system: 使用扁平化的 JwtOptions
        options.Value.Returns(new JwtOptions
        {
            SecretKey = "",
            Issuer = "LYBT",
            Audience = "LYBTUsers"
        });

        var config = Substitute.For<IConfiguration>();
        config["Jwt:SecretKey"].Returns("");

        // Act & Assert
        var act = () => new JwtService(options, config);
        act.Should().Throw<InvalidOperationException>().WithMessage("*未配置*");
    }

    #endregion
}

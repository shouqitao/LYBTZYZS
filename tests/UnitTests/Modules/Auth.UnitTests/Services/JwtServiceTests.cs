using FluentAssertions;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Module.Auth.Services;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using Xunit;

namespace LYBT.Module.Auth.Tests.Services;

/// <summary>
/// JwtService 单元测试
/// Issue #864 - Phase 2.3: Auth 模块测试
/// </summary>
public class JwtServiceTests
{
    private readonly Mock<IOptions<LybtOptions>> _mockOptions;
    private readonly IConfiguration _configuration;
    private readonly JwtService _sut;

    public JwtServiceTests()
    {
        _mockOptions = new Mock<IOptions<LybtOptions>>();
        _mockOptions.Setup(o => o.Value).Returns(new LybtOptions
        {
            Authentication = new AuthenticationOptions
            {
                Jwt = new JwtConfiguration
                {
                    SecretKey = "ThisIsAVeryStrongSecretKeyForTesting123456789012345678901234567890",
                    Issuer = "LYBT",
                    Audience = "LYBTUsers",
                    AccessTokenExpirationMinutes = 30,
                    RefreshTokenExpirationDays = 7
                }
            }
        });
        
        _configuration = CreateMockConfiguration();
        _sut = new JwtService(_mockOptions.Object, _configuration);
    }

    private static IConfiguration CreateMockConfiguration()
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Lybt:Authentication:Jwt:SecretKey"]).Returns("ThisIsAVeryStrongSecretKeyForTesting123456789012345678901234567890");
        config.Setup(c => c["Lybt:Authentication:Jwt:Issuer"]).Returns("LYBT");
        config.Setup(c => c["Lybt:Authentication:Jwt:Audience"]).Returns("LYBTUsers");
        config.Setup(c => c["Lybt:Authentication:Jwt:AccessTokenExpirationMinutes"]).Returns("30");
        config.Setup(c => c["Lybt:Authentication:Jwt:RefreshTokenExpirationDays"]).Returns("7");
        return config.Object;
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
        var expectedExpiry = DateTime.UtcNow.AddHours(8);
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
        var token = _sut.GenerateToken(userId, userName, role, null);

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

        // 篡改 token（修改最后一个字符）
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
        var mockOptions = new Mock<IOptions<LybtOptions>>();
        mockOptions.Setup(o => o.Value).Returns(new LybtOptions
        {
            Authentication = new AuthenticationOptions
            {
                Jwt = new JwtConfiguration
                {
                    SecretKey = "ThisIsAVeryStrongSecretKeyForTesting123456789012345678901234567890",
                    Issuer = "LYBT",
                    Audience = "LYBTUsers"
                }
            }
        });

        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Lybt:Authentication:Jwt:SecretKey"])
              .Returns("ThisIsAVeryStrongSecretKeyForTesting123456789012345678901234567890");

        // Act & Assert
        var act = () => new JwtService(mockOptions.Object, config.Object);
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithTooShortKey_ThrowsArgumentException()
    {
        // Arrange
        var mockOptions = new Mock<IOptions<LybtOptions>>();
        mockOptions.Setup(o => o.Value).Returns(new LybtOptions
        {
            Authentication = new AuthenticationOptions
            {
                Jwt = new JwtConfiguration
                {
                    SecretKey = "TooShortKey123",
                    Issuer = "LYBT",
                    Audience = "LYBTUsers"
                }
            }
        });

        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Lybt:Authentication:Jwt:SecretKey"]).Returns("TooShortKey123");

        // Act & Assert
        var act = () => new JwtService(mockOptions.Object, config.Object);
        act.Should().Throw<ArgumentException>().WithMessage("*长度不足*");
    }

    [Fact]
    public void Constructor_WithMinimum32CharsKey_DoesNotThrow()
    {
        // Arrange
        var mockOptions = new Mock<IOptions<LybtOptions>>();
        mockOptions.Setup(o => o.Value).Returns(new LybtOptions
        {
            Authentication = new AuthenticationOptions
            {
                Jwt = new JwtConfiguration
                {
                    SecretKey = "12345678901234567890123456789012", // 正好 32 字符
                    Issuer = "LYBT",
                    Audience = "LYBTUsers"
                }
            }
        });

        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Lybt:Authentication:Jwt:SecretKey"])
              .Returns("12345678901234567890123456789012");

        // Act & Assert
        var act = () => new JwtService(mockOptions.Object, config.Object);
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithEmptySecretKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockOptions = new Mock<IOptions<LybtOptions>>();
        mockOptions.Setup(o => o.Value).Returns(new LybtOptions
        {
            Authentication = new AuthenticationOptions
            {
                Jwt = new JwtConfiguration
                {
                    SecretKey = "",
                    Issuer = "LYBT",
                    Audience = "LYBTUsers"
                }
            }
        });

        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Lybt:Authentication:Jwt:SecretKey"]).Returns("");

        // Act & Assert
        var act = () => new JwtService(mockOptions.Object, config.Object);
        act.Should().Throw<InvalidOperationException>().WithMessage("*未配置*");
    }

    #endregion
}

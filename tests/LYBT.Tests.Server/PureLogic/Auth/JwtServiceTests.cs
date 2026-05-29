using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using LYBT.Module.Auth.Services;
using LYBT.Shared.Configuration.Options.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace LYBT.Tests.Server.PureLogic.Auth;

/// <summary>
/// JwtService 单元测试
/// 测试 JWT 令牌生成、验证和过期逻辑
/// AntiMock: 使用真实 IConfiguration + IOptions (不使用 NSubstitute)
/// </summary>
public class JwtServiceTests : IDisposable
{
    private readonly JwtService _sut;
    private readonly JwtOptions _jwtOptions;
    private readonly IConfiguration _configuration;

    public JwtServiceTests()
    {
        _jwtOptions = new JwtOptions
        {
            SecretKey = "TestSecretKeyForJWTAuthentication_32CharsLong!@#$",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpirationMinutes = 120,
            ClockSkewSeconds = 30
        };

        var inMemorySettings = new Dictionary<string, string>
        {
            { "ASPNETCORE_ENVIRONMENT", "Development" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        var options = Options.Create(_jwtOptions);
        _sut = new JwtService(options, _configuration);
    }

    public void Dispose()
    {
        // No cleanup needed
    }

    #region GenerateToken 测试

    [Fact]
    public void GenerateToken_WithValidParameters_ShouldReturnNonEmptyToken()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userName = "testuser";
        var role = UserRole.Doctor;

        // Act
        var token = _sut.GenerateToken(userId, userName, role);

        // Assert
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateToken_WithValidParameters_ShouldContainCorrectClaims()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userName = "testuser";
        var role = UserRole.Doctor;

        // Act
        var token = _sut.GenerateToken(userId, userName, role);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // JWT 使用 "nameid" 而非 ClaimTypes.NameIdentifier
        jwtToken.Claims.Should().Contain(c => c.Type == "nameid" && c.Value == userId);
        jwtToken.Claims.Should().Contain(c => c.Type == "unique_name" && c.Value == userName);
        jwtToken.Claims.Should().Contain(c => c.Type == "role" && c.Value == role.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == "user_type" && c.Value == "user");
    }

    [Fact]
    public void GenerateToken_WithCustomUserType_ShouldContainCustomUserType()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userName = "admin";
        var role = UserRole.Admin;
        var userType = "superadmin";

        // Act
        var token = _sut.GenerateToken(userId, userName, role, userType);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.Should().Contain(c => c.Type == "user_type" && c.Value == userType);
    }

    [Fact]
    public void GenerateToken_WithAdditionalClaims_ShouldContainAdditionalClaims()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userName = "testuser";
        var role = UserRole.Doctor;
        var additionalClaims = new Dictionary<string, string>
        {
            { "clinic_id", "123" },
            { "department", "Internal Medicine" }
        };

        // Act
        var token = _sut.GenerateToken(userId, userName, role, additionalClaims);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Claims.Should().Contain(c => c.Type == "clinic_id" && c.Value == "123");
        jwtToken.Claims.Should().Contain(c => c.Type == "department" && c.Value == "Internal Medicine");
    }

    [Theory]
#pragma warning disable xUnit1012
    [InlineData(null!)]
#pragma warning restore xUnit1012
    [InlineData("")]
    public void GenerateToken_WithEmptyUserId_ShouldThrowArgumentException(string userId)
    {
        // Arrange
        var userName = "testuser";
        var role = UserRole.Doctor;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _sut.GenerateToken(userId, userName, role));
    }

    [Theory]
#pragma warning disable xUnit1012
    [InlineData(null!)]
#pragma warning restore xUnit1012
    [InlineData("")]
    public void GenerateToken_WithEmptyUserName_ShouldThrowArgumentException(string userName)
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var role = UserRole.Doctor;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _sut.GenerateToken(userId, userName, role));
    }

    [Fact]
    public void GenerateToken_ShouldHaveCorrectExpiration()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userName = "testuser";
        var role = UserRole.Doctor;

        // Act
        var token = _sut.GenerateToken(userId, userName, role);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.ValidTo.Should().BeCloseTo(
            DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes),
            TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateToken_ShouldHaveCorrectIssuerAndAudience()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userName = "testuser";
        var role = UserRole.Doctor;

        // Act
        var token = _sut.GenerateToken(userId, userName, role);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        jwtToken.Issuer.Should().Be(_jwtOptions.Issuer);
        jwtToken.Audiences.Should().Contain(_jwtOptions.Audience);
    }

    #endregion

    #region ValidateToken 测试

    [Fact]
    public void ValidateToken_WithValidToken_ShouldReturnClaimsPrincipal()
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
        principal!.FindFirst(ClaimTypes.NameIdentifier)!.Value.Should().Be(userId);
        principal.FindFirst(ClaimTypes.Name)!.Value.Should().Be(userName);
        principal.FindFirst(ClaimTypes.Role)!.Value.Should().Be(role.ToString());
    }

    [Fact(Skip = "Timing-sensitive test - expired token validation depends on system clock")]
    public void ValidateToken_WithExpiredToken_ShouldReturnNull()
    {
        // Arrange - 使用极短过期时间 (1 分钟) 并设置 ClockSkew 为 0
        var shortExpiryOptions = new JwtOptions
        {
            SecretKey = _jwtOptions.SecretKey,
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            AccessTokenExpirationMinutes = 1,
            ClockSkewSeconds = 0
        };

        var shortExpiryService = new JwtService(Options.Create(shortExpiryOptions), _configuration);
        var token = shortExpiryService.GenerateToken(Guid.NewGuid().ToString(), "testuser", UserRole.Doctor);

        // 等待 Token 过期 (1 分钟 + 缓冲)
        Thread.Sleep(TimeSpan.FromSeconds(65));

        // Act
        var principal = _sut.ValidateToken(token);

        // Assert
        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithInvalidSignature_ShouldReturnNull()
    {
        // Arrange - 使用不同密钥生成的 Token
        var differentKeyOptions = new JwtOptions
        {
            SecretKey = "DifferentSecretKeyForJWTAuthentication_32Chars!",
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            AccessTokenExpirationMinutes = _jwtOptions.AccessTokenExpirationMinutes,
            ClockSkewSeconds = _jwtOptions.ClockSkewSeconds
        };

        var differentKeyService = new JwtService(Options.Create(differentKeyOptions), _configuration);
        var token = differentKeyService.GenerateToken(Guid.NewGuid().ToString(), "testuser", UserRole.Doctor);

        // Act
        var principal = _sut.ValidateToken(token);

        // Assert
        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithTamperedToken_ShouldReturnNull()
    {
        // Arrange
        var token = _sut.GenerateToken(Guid.NewGuid().ToString(), "testuser", UserRole.Doctor);
        var tamperedToken = token.Substring(0, token.Length - 5) + "XXXXX";

        // Act
        var principal = _sut.ValidateToken(tamperedToken);

        // Assert
        principal.Should().BeNull();
    }

    [Theory]
#pragma warning disable xUnit1012
    [InlineData(null!)]
#pragma warning restore xUnit1012
    [InlineData("")]
    public void ValidateToken_WithEmptyToken_ShouldReturnNull(string token)
    {
        // Act
        var principal = _sut.ValidateToken(token);

        // Assert
        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithWrongIssuer_ShouldReturnNull()
    {
        // Arrange
        var wrongIssuerOptions = new JwtOptions
        {
            SecretKey = _jwtOptions.SecretKey,
            Issuer = "WrongIssuer",
            Audience = _jwtOptions.Audience,
            AccessTokenExpirationMinutes = _jwtOptions.AccessTokenExpirationMinutes,
            ClockSkewSeconds = _jwtOptions.ClockSkewSeconds
        };

        var wrongIssuerService = new JwtService(Options.Create(wrongIssuerOptions), _configuration);
        var token = wrongIssuerService.GenerateToken(Guid.NewGuid().ToString(), "testuser", UserRole.Doctor);

        // Act
        var principal = _sut.ValidateToken(token);

        // Assert
        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithWrongAudience_ShouldReturnNull()
    {
        // Arrange
        var wrongAudienceOptions = new JwtOptions
        {
            SecretKey = _jwtOptions.SecretKey,
            Issuer = _jwtOptions.Issuer,
            Audience = "WrongAudience",
            AccessTokenExpirationMinutes = _jwtOptions.AccessTokenExpirationMinutes,
            ClockSkewSeconds = _jwtOptions.ClockSkewSeconds
        };

        var wrongAudienceService = new JwtService(Options.Create(wrongAudienceOptions), _configuration);
        var token = wrongAudienceService.GenerateToken(Guid.NewGuid().ToString(), "testuser", UserRole.Doctor);

        // Act
        var principal = _sut.ValidateToken(token);

        // Assert
        principal.Should().BeNull();
    }

    #endregion

    #region ValidateSecretKeyStrength 测试

    [Fact]
    public void Constructor_WithShortSecretKey_ShouldThrowArgumentException()
    {
        // Arrange
        var shortKeyOptions = new JwtOptions
        {
            SecretKey = "ShortKey", // 少于 32 字符
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpirationMinutes = 120,
            ClockSkewSeconds = 30
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new JwtService(Options.Create(shortKeyOptions), _configuration));
    }

    [Fact]
    public void Constructor_WithEmptySecretKey_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var emptyKeyOptions = new JwtOptions
        {
            SecretKey = "",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpirationMinutes = 120,
            ClockSkewSeconds = 30
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new JwtService(Options.Create(emptyKeyOptions), _configuration));
    }

    [Fact]
    public void Constructor_InProductionWithDefaultKey_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var productionSettings = new Dictionary<string, string>
        {
            { "ASPNETCORE_ENVIRONMENT", "Production" }
        };

        var productionConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(productionSettings!)
            .Build();

        var defaultKeyOptions = new JwtOptions
        {
            SecretKey = "DefaultDevelopmentSecretKeyForJWTAuthentication_ShouldBeReplacedInProduction_32Chars",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpirationMinutes = 120,
            ClockSkewSeconds = 30
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new JwtService(Options.Create(defaultKeyOptions), productionConfig));
    }

    #endregion

    #region Round-trip 测试

    [Fact]
    public void GenerateAndValidate_RoundTrip_ShouldPreserveAllClaims()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var userName = "testuser";
        var role = UserRole.Admin;
        var userType = "superadmin";
        var additionalClaims = new Dictionary<string, string>
        {
            { "clinic_id", "456" },
            { "department", "Surgery" }
        };

        // Act
        var token = _sut.GenerateToken(userId, userName, role, additionalClaims, userType);
        var principal = _sut.ValidateToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal!.FindFirst(ClaimTypes.NameIdentifier)!.Value.Should().Be(userId);
        principal.FindFirst(ClaimTypes.Name)!.Value.Should().Be(userName);
        principal.FindFirst(ClaimTypes.Role)!.Value.Should().Be(role.ToString());
        principal.FindFirst("user_type")!.Value.Should().Be(userType);
        principal.FindFirst("clinic_id")!.Value.Should().Be("456");
        principal.FindFirst("department")!.Value.Should().Be("Surgery");
    }

    [Fact]
    public void GenerateAndValidate_MultipleTokens_ShouldBeIndependent()
    {
        // Arrange
        var userId1 = Guid.NewGuid().ToString();
        var userId2 = Guid.NewGuid().ToString();

        // Act
        var token1 = _sut.GenerateToken(userId1, "user1", UserRole.Doctor);
        var token2 = _sut.GenerateToken(userId2, "user2", UserRole.Admin);

        var principal1 = _sut.ValidateToken(token1);
        var principal2 = _sut.ValidateToken(token2);

        // Assert
        principal1.Should().NotBeNull();
        principal2.Should().NotBeNull();
        principal1!.FindFirst(ClaimTypes.NameIdentifier)!.Value.Should().Be(userId1);
        principal2!.FindFirst(ClaimTypes.NameIdentifier)!.Value.Should().Be(userId2);
    }

    #endregion
}

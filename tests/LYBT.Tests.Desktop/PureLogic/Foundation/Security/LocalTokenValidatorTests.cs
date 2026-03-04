using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using LYBT.Desktop.Foundation.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;

namespace LYBT.Tests.Desktop.PureLogic.Foundation.Security;

/// <summary>
/// LocalTokenValidator 单元测试
/// Issue #1866: 测试JWT本地验证（有效Token、过期Token、签名无效、缺少Claims、ClockSkew）
/// </summary>
public class LocalTokenValidatorTests
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<LocalTokenValidator> _logger;
    private readonly LocalTokenValidator _validator;

    private const string SecretKey = "your-test-secret-key-at-least-32-characters-long-for-testing";
    private const string Issuer = "LYBT.WebAPI";
    private const string Audience = "LYBT.Desktop";

    public LocalTokenValidatorTests()
    {
        _logger = Substitute.For<ILogger<LocalTokenValidator>>();

        // 使用内存配置，支持 GetSection().Bind()
        // JwtOptions.SectionName = "Jwt"
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:SecretKey"] = SecretKey,
            ["Jwt:Issuer"] = Issuer,
            ["Jwt:Audience"] = Audience,
            ["Jwt:ClockSkewSeconds"] = "300"
        });
        _configuration = configurationBuilder.Build();

        _validator = new LocalTokenValidator(_configuration, _logger);
    }

    /// <summary>
    /// 测试：有效Token验证成功
    /// </summary>
    [Fact]
    public async Task ValidateToken_ValidToken_ReturnsSuccess()
    {
        // Arrange
        var token = GenerateValidToken();

        // Act
        var result = await _validator.ValidateTokenAsync(token);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue("有效Token应该验证成功");
        result.UserInfo.Should().NotBeNull();
        result.UserInfo!.UserId.Should().NotBeEmpty();
        result.UserInfo.UserName.Should().Be("test_user");
        result.UserInfo.Role.Should().Be("Doctor");
        result.UserInfo.UserType.Should().Be("user");
        result.ErrorMessage.Should().BeNullOrEmpty();
    }

    /// <summary>
    /// 测试：过期Token验证失败
    /// </summary>
    [Fact]
    public async Task ValidateToken_ExpiredToken_ReturnsFailed()
    {
        // Arrange
        var token = GenerateExpiredToken();

        // Act
        var result = await _validator.ValidateTokenAsync(token);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse("过期Token应该验证失败");
        result.ErrorMessage.Should().Contain("过期", "错误消息应包含'过期'");
    }

    /// <summary>
    /// 测试：签名无效的Token验证失败
    /// </summary>
    [Fact]
    public async Task ValidateToken_InvalidSignature_ReturnsFailed()
    {
        // Arrange
        var token = GenerateTokenWithInvalidSignature();

        // Act
        var result = await _validator.ValidateTokenAsync(token);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse("签名无效Token应该验证失败");
        result.ErrorMessage.Should().Contain("签名", "错误消息应包含'签名'");
    }

    /// <summary>
    /// 测试：缺少必需Claims的Token验证失败
    /// </summary>
    [Fact]
    public async Task ValidateToken_MissingClaims_ReturnsFailed()
    {
        // Arrange
        var token = GenerateTokenWithMissingClaims();

        // Act
        var result = await _validator.ValidateTokenAsync(token);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse("缺少Claims的Token应该验证失败");
        result.ErrorMessage.Should().Contain("用户信息", "错误消息应提示缺少用户信息");
    }

    /// <summary>
    /// 测试：ClockSkew容差范围内的Token仍然有效
    /// </summary>
    [Fact]
    public async Task ValidateToken_ClockSkew_StillValid()
    {
        // Arrange - 生成一个在ClockSkew容差范围内过期的Token（过期1分钟，ClockSkew=5分钟）
        var token = GenerateTokenExpiringWithinClockSkew();

        // Act
        var result = await _validator.ValidateTokenAsync(token);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue("ClockSkew容差范围内的Token应该有效");
        result.UserInfo.Should().NotBeNull();
    }

    /// <summary>
    /// 测试：空Token验证失败
    /// </summary>
    [Fact]
    public async Task ValidateToken_EmptyToken_ReturnsFailed()
    {
        // Arrange
        var token = string.Empty;

        // Act
        var result = await _validator.ValidateTokenAsync(token);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse("空Token应该验证失败");
        result.ErrorMessage.Should().Contain("不能为空");
    }

    /// <summary>
    /// 测试：ValidateAndGetUserInfoAsync返回用户信息
    /// </summary>
    [Fact]
    public async Task ValidateAndGetUserInfoAsync_ValidToken_ReturnsUserInfo()
    {
        // Arrange
        var token = GenerateValidToken();

        // Act
        var userInfo = await _validator.ValidateAndGetUserInfoAsync(token);

        // Assert
        userInfo.Should().NotBeNull();
        userInfo!.UserName.Should().Be("test_user");
        userInfo.Role.Should().Be("Doctor");
    }

    /// <summary>
    /// 测试：ValidateAndGetUserInfoAsync对于无效Token返回null
    /// </summary>
    [Fact]
    public async Task ValidateAndGetUserInfoAsync_InvalidToken_ReturnsNull()
    {
        // Arrange
        var token = GenerateExpiredToken();

        // Act
        var userInfo = await _validator.ValidateAndGetUserInfoAsync(token);

        // Assert
        userInfo.Should().BeNull("无效Token应返回null");
    }

    #region Token Generation Helpers

    /// <summary>
    /// 生成有效的测试Token
    /// </summary>
    private string GenerateValidToken()
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(SecretKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, "test_user"),
                new Claim(ClaimTypes.Role, "Doctor"),
                new Claim("user_type", "user")
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = Issuer,
            Audience = Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            )
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// 生成已过期的Token（超出ClockSkew）
    /// </summary>
    private string GenerateExpiredToken()
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(SecretKey);

        var now = DateTime.UtcNow;
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, "test_user"),
                new Claim(ClaimTypes.Role, "Doctor"),
                new Claim("user_type", "user")
            }),
            NotBefore = now.AddMinutes(-30), // 30分钟前开始有效
            Expires = now.AddMinutes(-10), // 10分钟前过期（超出300秒ClockSkew）
            Issuer = Issuer,
            Audience = Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            )
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// 生成签名无效的Token（使用不同的密钥）
    /// </summary>
    private string GenerateTokenWithInvalidSignature()
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var wrongKey = Encoding.UTF8.GetBytes("wrong-secret-key-different-from-test-key-at-least-32-chars");

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, "test_user"),
                new Claim(ClaimTypes.Role, "Doctor"),
                new Claim("user_type", "user")
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = Issuer,
            Audience = Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(wrongKey),
                SecurityAlgorithms.HmacSha256Signature
            )
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// 生成缺少必需Claims的Token
    /// </summary>
    private string GenerateTokenWithMissingClaims()
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(SecretKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                // 缺少 ClaimTypes.NameIdentifier 和 ClaimTypes.Name
                new Claim(ClaimTypes.Role, "Doctor"),
                new Claim("user_type", "user")
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = Issuer,
            Audience = Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            )
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// 生成在ClockSkew容差范围内过期的Token
    /// </summary>
    private string GenerateTokenExpiringWithinClockSkew()
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(SecretKey);

        var now = DateTime.UtcNow;
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, "test_user"),
                new Claim(ClaimTypes.Role, "Doctor"),
                new Claim("user_type", "user")
            }),
            NotBefore = now.AddMinutes(-10), // 10分钟前开始有效
            Expires = now.AddMinutes(-1), // 1分钟前过期（在300秒ClockSkew容差内）
            Issuer = Issuer,
            Audience = Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            )
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    #endregion
}

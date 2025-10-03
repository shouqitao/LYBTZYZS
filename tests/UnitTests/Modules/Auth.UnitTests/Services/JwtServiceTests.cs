using FluentAssertions;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Module.Auth.Services;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace LYBT.Module.Auth.Tests.Services
{
    /// <summary>
    /// JwtService 单元测试
    /// 测试覆盖：Token 生成、验证、配置验证
    /// </summary>
    public class JwtServiceTests
    {
        private readonly string _validSecretKey = "ThisIsAVeryStrongSecretKeyForTestingPurposesOnly123456789";
        private readonly Mock<IOptions<LybtOptions>> _mockOptions;
        private readonly Mock<IConfiguration> _mockConfiguration;

        public JwtServiceTests()
        {
            // 设置默认的 LybtOptions 配置
            var lybtOptions = new LybtOptions
            {
                Authentication = new AuthenticationOptions
                {
                    Jwt = new JwtConfiguration
                    {
                        SecretKey = _validSecretKey,
                        Issuer = "https://lybt.local",
                        Audience = "https://lybt.local",
                        AccessTokenExpirationMinutes = 480 // 8小时
                    }
                }
            };

            _mockOptions = new Mock<IOptions<LybtOptions>>();
            _mockOptions.Setup(o => o.Value).Returns(lybtOptions);

            // 设置默认的 Configuration
            _mockConfiguration = new Mock<IConfiguration>();
            _mockConfiguration.Setup(c => c["Lybt:Authentication:Jwt:SecretKey"])
                .Returns(_validSecretKey);
        }

        #region Constructor & Validation Tests

        [Fact]
        public void Constructor_Should_Throw_When_Options_Is_Null()
        {
            // Act
            Action act = () => new JwtService(null!, _mockConfiguration.Object);

            // Assert - 实际抛出 NullReferenceException（访问 null.Value）
            act.Should().Throw<NullReferenceException>();
        }

        [Fact]
        public void Constructor_Should_Throw_When_Configuration_Is_Null()
        {
            // Act
            Action act = () => new JwtService(_mockOptions.Object, null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("configuration");
        }

        [Fact]
        public void Constructor_Should_Throw_When_SecretKey_Is_Not_Configured()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["Lybt:Authentication:Jwt:SecretKey"])
                .Returns((string?)null);

            // Act
            Action act = () => new JwtService(_mockOptions.Object, mockConfig.Object);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*JWT SecretKey 未配置*");
        }

        [Fact]
        public void Constructor_Should_Throw_When_SecretKey_Is_Too_Short()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["Lybt:Authentication:Jwt:SecretKey"])
                .Returns("TooShortKey"); // 只有 11 个字符

            // Act
            Action act = () => new JwtService(_mockOptions.Object, mockConfig.Object);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*JWT SecretKey 长度不足*");
        }

        [Fact]
        public void Constructor_Should_Succeed_When_SecretKey_Is_Valid()
        {
            // Act
            Action act = () => new JwtService(_mockOptions.Object, _mockConfiguration.Object);

            // Assert
            act.Should().NotThrow();
        }

        #endregion

        #region GenerateToken (Basic) Tests

        [Fact]
        public void GenerateToken_Should_Throw_When_UserId_Is_Empty()
        {
            // Arrange
            var sut = new JwtService(_mockOptions.Object, _mockConfiguration.Object);

            // Act
            Action act = () => sut.GenerateToken("", "TestUser", UserRole.Doctor);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithParameterName("userId")
                .WithMessage("*用户ID不能为空*");
        }

        [Fact]
        public void GenerateToken_Should_Throw_When_UserName_Is_Empty()
        {
            // Arrange
            var sut = new JwtService(_mockOptions.Object, _mockConfiguration.Object);

            // Act
            Action act = () => sut.GenerateToken("user-123", "", UserRole.Doctor);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithParameterName("userName")
                .WithMessage("*用户名不能为空*");
        }

        [Fact]
        public void GenerateToken_Should_Return_Valid_Token()
        {
            // Arrange
            var sut = new JwtService(_mockOptions.Object, _mockConfiguration.Object);

            // Act
            var token = sut.GenerateToken("user-123", "张三", UserRole.Doctor);

            // Assert
            token.Should().NotBeNullOrEmpty();
            token.Should().Contain(".");

            // 验证 token 格式：header.payload.signature
            var parts = token.Split('.');
            parts.Should().HaveCount(3);
        }

        [Fact]
        public void GenerateToken_Should_Include_Correct_Claims()
        {
            // Arrange
            var sut = new JwtService(_mockOptions.Object, _mockConfiguration.Object);
            var userId = "user-123";
            var userName = "张三";
            var role = UserRole.Admin;

            // Act
            var token = sut.GenerateToken(userId, userName, role);

            // Assert - 使用 ValidateToken 来验证 claims（模拟实际使用场景）
            var principal = sut.ValidateToken(token);

            principal.Should().NotBeNull();
            principal!.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be(userId);
            principal.FindFirst(ClaimTypes.Name)?.Value.Should().Be(userName);
            principal.FindFirst(ClaimTypes.Role)?.Value.Should().Be(role.ToString());

            // 验证 token 包含必需的标准 claims
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
            jwtToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Iat);
        }

        [Fact]
        public void GenerateToken_Should_Set_Correct_Expiration()
        {
            // Arrange
            var sut = new JwtService(_mockOptions.Object, _mockConfiguration.Object);
            var before = DateTime.UtcNow;

            // Act
            var token = sut.GenerateToken("user-123", "张三", UserRole.Doctor);

            // Assert
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            jwtToken.ValidTo.Should().BeCloseTo(before.AddHours(8), TimeSpan.FromMinutes(1));
        }

        #endregion

        #region GenerateToken (With Additional Claims) Tests

        [Fact]
        public void GenerateToken_WithAdditionalClaims_Should_Throw_When_UserId_Is_Empty()
        {
            // Arrange
            var sut = new JwtService(_mockOptions.Object, _mockConfiguration.Object);
            var additionalClaims = new Dictionary<string, string> { { "department", "IT" } };

            // Act
            Action act = () => sut.GenerateToken("", "TestUser", UserRole.Doctor, additionalClaims);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithParameterName("userId");
        }

        [Fact]
        public void GenerateToken_WithAdditionalClaims_Should_Throw_When_UserName_Is_Empty()
        {
            // Arrange
            var sut = new JwtService(_mockOptions.Object, _mockConfiguration.Object);
            var additionalClaims = new Dictionary<string, string> { { "department", "IT" } };

            // Act
            Action act = () => sut.GenerateToken("user-123", "", UserRole.Doctor, additionalClaims);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithParameterName("userName");
        }

        [Fact]
        public void GenerateToken_WithAdditionalClaims_Should_Include_Custom_Claims()
        {
            // Arrange
            var sut = new JwtService(_mockOptions.Object, _mockConfiguration.Object);
            var additionalClaims = new Dictionary<string, string>
            {
                { "department", "IT" },
                { "location", "Beijing" },
                { "employee_id", "EMP-001" }
            };

            // Act
            var token = sut.GenerateToken("user-123", "张三", UserRole.Doctor, additionalClaims);

            // Assert
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            jwtToken.Claims.Should().Contain(c => c.Type == "department" && c.Value == "IT");
            jwtToken.Claims.Should().Contain(c => c.Type == "location" && c.Value == "Beijing");
            jwtToken.Claims.Should().Contain(c => c.Type == "employee_id" && c.Value == "EMP-001");
        }

        [Fact]
        public void GenerateToken_WithAdditionalClaims_Should_Handle_Null_AdditionalClaims()
        {
            // Arrange
            var sut = new JwtService(_mockOptions.Object, _mockConfiguration.Object);

            // Act
            var token = sut.GenerateToken("user-123", "张三", UserRole.Doctor, null!);

            // Assert
            token.Should().NotBeNullOrEmpty();

            // 使用 ValidateToken 验证标准 claims 存在
            var principal = sut.ValidateToken(token);
            principal.Should().NotBeNull();
            principal!.FindFirst(ClaimTypes.NameIdentifier).Should().NotBeNull();
            principal.FindFirst(ClaimTypes.Name).Should().NotBeNull();
            principal.FindFirst(ClaimTypes.Role).Should().NotBeNull();
        }

        [Fact]
        public void GenerateToken_WithAdditionalClaims_Should_Handle_Empty_AdditionalClaims()
        {
            // Arrange
            var sut = new JwtService(_mockOptions.Object, _mockConfiguration.Object);
            var additionalClaims = new Dictionary<string, string>();

            // Act
            var token = sut.GenerateToken("user-123", "张三", UserRole.Doctor, additionalClaims);

            // Assert
            token.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region ValidateToken Tests

        [Fact]
        public void ValidateToken_Should_Return_Null_When_Token_Is_Empty()
        {
            // Arrange
            var sut = new JwtService(_mockOptions.Object, _mockConfiguration.Object);

            // Act
            var result = sut.ValidateToken("");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ValidateToken_Should_Return_Null_When_Token_Is_Null()
        {
            // Arrange
            var sut = new JwtService(_mockOptions.Object, _mockConfiguration.Object);

            // Act
            var result = sut.ValidateToken(null!);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ValidateToken_Should_Return_Null_When_Token_Format_Is_Invalid()
        {
            // Arrange
            var sut = new JwtService(_mockOptions.Object, _mockConfiguration.Object);

            // Act
            var result = sut.ValidateToken("invalid.token.format");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ValidateToken_Should_Return_Null_When_Token_Signature_Is_Invalid()
        {
            // Arrange
            var sut = new JwtService(_mockOptions.Object, _mockConfiguration.Object);

            // 生成一个 token，然后篡改签名
            var validToken = sut.GenerateToken("user-123", "张三", UserRole.Doctor);
            var parts = validToken.Split('.');
            var invalidToken = $"{parts[0]}.{parts[1]}.InvalidSignature123456";

            // Act
            var result = sut.ValidateToken(invalidToken);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ValidateToken_Should_Return_ClaimsPrincipal_When_Token_Is_Valid()
        {
            // Arrange
            var sut = new JwtService(_mockOptions.Object, _mockConfiguration.Object);
            var userId = "user-123";
            var userName = "张三";
            var role = UserRole.Admin;

            var token = sut.GenerateToken(userId, userName, role);

            // Act
            var principal = sut.ValidateToken(token);

            // Assert
            principal.Should().NotBeNull();
            principal!.Identity.Should().NotBeNull();
            principal.Identity!.IsAuthenticated.Should().BeTrue();

            principal.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be(userId);
            principal.FindFirst(ClaimTypes.Name)?.Value.Should().Be(userName);
            principal.FindFirst(ClaimTypes.Role)?.Value.Should().Be(role.ToString());
        }

        [Fact]
        public void ValidateToken_Should_Validate_Issuer_And_Audience()
        {
            // Arrange
            var sut = new JwtService(_mockOptions.Object, _mockConfiguration.Object);
            var token = sut.GenerateToken("user-123", "张三", UserRole.Doctor);

            // Act
            var principal = sut.ValidateToken(token);

            // Assert
            principal.Should().NotBeNull();

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            jwtToken.Issuer.Should().Be("https://lybt.local");
            jwtToken.Audiences.Should().Contain("https://lybt.local");
        }

        #endregion
    }
}

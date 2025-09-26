using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using FluentAssertions;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Infrastructure.Security;
using LYBT.Module.Auth.Services;
using LYBT.Shared.Models.Enums;
using LYBT.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace LYBT.Module.Auth.Tests.Services
{
    /// <summary>
    /// JWT认证服务单元测试
    /// 测试令牌生成、验证、刷新等核心安全功能
    /// </summary>
    public class JwtAuthenticationServiceTests : TestBase
    {
        private readonly JwtAuthenticationService _jwtService;
        private readonly JwtOptions _jwtOptions;
        private readonly Mock<ILogger<JwtAuthenticationService>> _loggerMock;
        private readonly Mock<IKeyManagementService> _keyManagementServiceMock;

        public JwtAuthenticationServiceTests()
        {
            // 配置JWT选项
            _jwtOptions = new JwtOptions
            {
                Secret = "J4CM3t5EsIA9COGVMpQJoAHfX/mgeIbKxrlbXNKfv34T6AGxRnD/2fRJmh932xWypxhjl0nm7whrsdK9PcY9fw==",
                Issuer = "LYBT.WebAPI.Test",
                Audience = "LYBT.Client.Test",
                ExpireMinutes = 30,
                RememberMeExpireMinutes = 10080,
                ClockSkewSeconds = 300
            };

            var optionsMock = new Mock<IOptions<JwtOptions>>();
            optionsMock.Setup(x => x.Value).Returns(_jwtOptions);

            _loggerMock = CreateLoggerMock<JwtAuthenticationService>();
            _keyManagementServiceMock = CreateMock<IKeyManagementService>();

            _jwtService = new JwtAuthenticationService(
                optionsMock.Object,
                _loggerMock.Object,
                _keyManagementServiceMock.Object);
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);

            // 注册JWT服务相关的依赖
            services.AddSingleton(_jwtOptions);
            services.AddSingleton(_jwtService);
        }

        [Fact]
        public void GenerateToken_WithValidParameters_ShouldReturnValidToken()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var userName = "testuser";
            var role = UserRole.Doctor;
            var rememberMe = false;

            // Act
            var token = _jwtService.GenerateToken(userId, userName, role, rememberMe);

            // Assert
            token.Should().NotBeNullOrEmpty();

            // 解析Token验证内容
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);

            jsonToken.Should().NotBeNull();
            jsonToken.Issuer.Should().Be(_jwtOptions.Issuer);
            jsonToken.Audiences.Should().Contain(_jwtOptions.Audience);

            // 验证Claims
            var claims = jsonToken.Claims.ToList();
            claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == userId);
            claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == userName);
            claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == role.ToString());
            claims.Should().Contain(c => c.Type == "role" && c.Value == role.ToString());

            // 验证过期时间
            var expectedExpiration = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpireMinutes);
            jsonToken.ValidTo.Should().BeCloseTo(expectedExpiration, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public void GenerateToken_WithRememberMe_ShouldHaveLongerExpiration()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var userName = "testuser";
            var role = UserRole.Admin;
            var rememberMe = true;

            // Act
            var token = _jwtService.GenerateToken(userId, userName, role, rememberMe);

            // Assert
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);

            // 验证"记住我"的过期时间（7天）
            var expectedExpiration = DateTime.UtcNow.AddMinutes(_jwtOptions.RememberMeExpireMinutes);
            jsonToken.ValidTo.Should().BeCloseTo(expectedExpiration, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public void GenerateToken_WithNullParameters_ShouldHandleGracefully()
        {
            // Arrange
            string userId = null;
            string userName = null;
            var role = UserRole.Pharmacist;

            // Act
            var token = _jwtService.GenerateToken(userId, userName, role, false);

            // Assert
            token.Should().NotBeNullOrEmpty();

            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);

            // 验证null参数被转换为空字符串
            var claims = jsonToken.Claims.ToList();
            claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == string.Empty);
            claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == string.Empty);
        }

        [Fact]
        public void ValidateToken_WithValidToken_ShouldReturnClaimsPrincipal()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var userName = "testuser";
            var role = UserRole.Doctor;
            var token = _jwtService.GenerateToken(userId, userName, role, false);

            // Act
            var principal = _jwtService.ValidateToken(token);

            // Assert
            principal.Should().NotBeNull();
            principal.Claims.Should().NotBeEmpty();

            // 验证Claims内容
            principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value.Should().Be(userId);
            principal.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value.Should().Be(userName);
            principal.FindFirst(ClaimTypes.Role)?.Value.Should().Be(role.ToString());
        }

        [Fact]
        public void ValidateToken_WithInvalidToken_ShouldReturnNull()
        {
            // Arrange
            var invalidToken = "invalid.token.here";

            // Act
            var principal = _jwtService.ValidateToken(invalidToken);

            // Assert
            principal.Should().BeNull();

            // 验证日志记录
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public void ValidateToken_WithExpiredToken_ShouldReturnNull()
        {
            // Arrange
            // 创建一个已过期的JWT选项
            var expiredOptions = new JwtOptions
            {
                Secret = _jwtOptions.Secret,
                Issuer = _jwtOptions.Issuer,
                Audience = _jwtOptions.Audience,
                ExpireMinutes = -1, // 负数表示立即过期
                RememberMeExpireMinutes = _jwtOptions.RememberMeExpireMinutes,
                ClockSkewSeconds = 0 // 不允许时钟偏差
            };

            var expiredOptionsMock = new Mock<IOptions<JwtOptions>>();
            expiredOptionsMock.Setup(x => x.Value).Returns(expiredOptions);

            var expiredJwtService = new JwtAuthenticationService(
                expiredOptionsMock.Object,
                _loggerMock.Object,
                _keyManagementServiceMock.Object);

            var token = expiredJwtService.GenerateToken("user", "name", UserRole.Doctor, false);

            // 等待一秒确保过期
            System.Threading.Thread.Sleep(1000);

            // Act
            var principal = _jwtService.ValidateToken(token);

            // Assert
            principal.Should().BeNull();
        }

        [Fact]
        public void RefreshToken_WithValidToken_ShouldGenerateNewToken()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var userName = "testuser";
            var role = UserRole.Admin;
            var originalToken = _jwtService.GenerateToken(userId, userName, role, false);

            // Act
            var newToken = _jwtService.RefreshToken(originalToken);

            // Assert
            newToken.Should().NotBeNullOrEmpty();
            newToken.Should().NotBe(originalToken); // 应该是新的令牌

            // 验证新令牌包含相同的用户信息
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(newToken);

            var claims = jsonToken.Claims.ToList();
            claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == userId);
            claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == userName);
            claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == role.ToString());
        }

        [Fact]
        public void RefreshToken_WithInvalidToken_ShouldThrowException()
        {
            // Arrange
            var invalidToken = "invalid.token.here";

            // Act & Assert
            Assert.Throws<System.IdentityModel.Tokens.Jwt.SecurityTokenException>(() =>
            {
                _jwtService.RefreshToken(invalidToken);
            });
        }

        [Fact]
        public void ExtractUserInfo_WithValidToken_ShouldReturnTokenUserInfo()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var userName = "testuser";
            var role = UserRole.Pharmacist;
            var token = _jwtService.GenerateToken(userId, userName, role, false);

            // Act
            var userInfo = _jwtService.ExtractUserInfo(token);

            // Assert
            userInfo.Should().NotBeNull();
            userInfo.UserId.Should().Be(userId);
            userInfo.Username.Should().Be(userName);
            userInfo.Role.Should().Be(role);
            userInfo.ExpiresAt.Should().BeCloseTo(
                DateTime.UtcNow.AddMinutes(_jwtOptions.ExpireMinutes),
                TimeSpan.FromMinutes(1));
        }

        [Fact]
        public void ExtractUserInfo_WithMalformedToken_ShouldReturnNull()
        {
            // Arrange
            var malformedToken = "not.a.valid.jwt.token";

            // Act
            var userInfo = _jwtService.ExtractUserInfo(malformedToken);

            // Assert
            userInfo.Should().BeNull();

            // 验证警告日志
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);
        }

        [Theory]
        [InlineData(UserRole.Admin)]
        [InlineData(UserRole.Doctor)]
        [InlineData(UserRole.Pharmacist)]
        public void GenerateToken_WithDifferentRoles_ShouldIncludeCorrectRoleClaim(UserRole role)
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var userName = "testuser";

            // Act
            var token = _jwtService.GenerateToken(userId, userName, role, false);

            // Assert
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);

            var roleClaims = jsonToken.Claims.Where(c => c.Type == ClaimTypes.Role || c.Type == "role").ToList();
            roleClaims.Should().HaveCount(2);
            roleClaims.Should().OnlyContain(c => c.Value == role.ToString());
        }

        [Fact]
        public void GenerateToken_ShouldIncludeUniqueJti()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var userName = "testuser";
            var role = UserRole.Doctor;

            // Act
            var token1 = _jwtService.GenerateToken(userId, userName, role, false);
            var token2 = _jwtService.GenerateToken(userId, userName, role, false);

            // Assert
            var handler = new JwtSecurityTokenHandler();
            var jsonToken1 = handler.ReadJwtToken(token1);
            var jsonToken2 = handler.ReadJwtToken(token2);

            var jti1 = jsonToken1.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
            var jti2 = jsonToken2.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;

            jti1.Should().NotBeNullOrEmpty();
            jti2.Should().NotBeNullOrEmpty();
            jti1.Should().NotBe(jti2); // 每个令牌应该有唯一的JTI
        }
    }
}

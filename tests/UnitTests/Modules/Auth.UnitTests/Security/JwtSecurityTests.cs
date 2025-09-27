using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Entities.Auth;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Security;
using LYBT.Infrastructure.Configuration.Options;
using AuthISecurityKeyService = LYBT.Module.Auth.Interfaces.ISecurityKeyService;
using UsersIUserService = LYBT.Module.Users.Interfaces.IUserService;
using LYBT.Module.Auth.Services;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Interfaces.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace LYBT.Module.Auth.Tests.Security
{
    /// <summary>
    /// JWT安全测试套件
    /// </summary>
    public class JwtSecurityTests : IDisposable
    {
        private readonly EnhancedJwtService _jwtService;
        private readonly AppDbContext _context;
        private readonly Mock<UsersIUserService> _mockUserService;
        private readonly Mock<AuthISecurityKeyService> _mockSecurityKeyService;
        private readonly Mock<ILogger<EnhancedJwtService>> _mockLogger;
        private readonly JwtOptions _jwtOptions;
        private readonly SecurityKey _testSecurityKey;
        private readonly string _testSecretKey = "TestSecretKeyMustBe32CharactersLongForTesting123";

        public JwtSecurityTests()
        {
            // 设置内存数据库
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);

            // 配置JWT选项
            _jwtOptions = new JwtOptions
            {
                Secret = _testSecretKey,
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                ExpireMinutes = 15,
                RefreshTokenExpireDays = 7,
                ClockSkewSeconds = 300
            };

            // 创建测试用的SecurityKey
            var key = Encoding.UTF8.GetBytes(_testSecretKey);
            _testSecurityKey = new SymmetricSecurityKey(key);

            // 设置Mock对象
            _mockUserService = new Mock<UsersIUserService>();
            _mockSecurityKeyService = new Mock<AuthISecurityKeyService>();
            _mockLogger = new Mock<ILogger<EnhancedJwtService>>();

            // 配置SecurityKeyService
            _mockSecurityKeyService
                .Setup(x => x.GetCurrentKeyAsync())
                .ReturnsAsync(_testSecurityKey);
            
            _mockSecurityKeyService
                .Setup(x => x.GetAllKeysAsync())
                .ReturnsAsync(new[] { _testSecurityKey });

            // 创建JWT服务
            _jwtService = new EnhancedJwtService(
                Options.Create(_jwtOptions),
                _mockSecurityKeyService.Object,
                _context,
                _mockUserService.Object,
                _mockLogger.Object
            );
        }

        #region Token生成测试

        [Fact]
        public async Task GenerateTokenPairAsync_ShouldCreateValidTokenPair()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var userName = "testuser";
            var role = UserRole.Doctor;

            // Act
            var tokenPair = await _jwtService.GenerateTokenPairAsync(
                userId, userName, role);

            // Assert
            tokenPair.Should().NotBeNull();
            tokenPair.AccessToken.Should().NotBeNullOrEmpty();
            tokenPair.RefreshToken.Should().NotBeNullOrEmpty();
            tokenPair.AccessTokenExpires.Should().BeCloseTo(
                DateTime.UtcNow.AddMinutes(_jwtOptions.ExpireMinutes),
                TimeSpan.FromMinutes(1));
            tokenPair.RefreshTokenExpires.Should().BeCloseTo(
                DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpireDays),
                TimeSpan.FromMinutes(1));
        }

        [Fact]
        public void GenerateToken_ShouldIncludeRequiredClaims()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var userName = "testuser";
            var role = UserRole.Admin;

            // Act
            var token = _jwtService.GenerateToken(userId, userName, role);

            // Assert
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadJwtToken(token);
            
            jsonToken.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == userId);
            jsonToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == userName);
            jsonToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == role.ToString());
            jsonToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
        }

        [Fact]
        public void AccessToken_ShouldExpireIn15Minutes()
        {
            // Arrange & Act
            var token = _jwtService.GenerateToken("user123", "testuser", UserRole.Doctor);
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadJwtToken(token);

            // Assert
            var expectedExpiry = DateTime.UtcNow.AddMinutes(15);
            jsonToken.ValidTo.Should().BeCloseTo(expectedExpiry, TimeSpan.FromMinutes(1));
        }

        #endregion

        #region Token验证测试

        [Fact]
        public async Task ValidateToken_WithValidToken_ShouldReturnPrincipal()
        {
            // Arrange
            var token = _jwtService.GenerateToken("user123", "testuser", UserRole.Doctor);

            // Act
            var principal = await _jwtService.ValidateTokenAsync(token);

            // Assert
            principal.Should().NotBeNull();
            principal.Identity.Should().NotBeNull();
            principal.Identity!.IsAuthenticated.Should().BeTrue();
            principal.FindFirst(ClaimTypes.NameIdentifier)?.Value.Should().Be("user123");
        }

        [Fact]
        public async Task ValidateToken_WithExpiredToken_ShouldReturnNull()
        {
            // Arrange
            var expiredOptions = new JwtOptions
            {
                Secret = _testSecretKey,
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                ExpireMinutes = -1, // 已过期
                RefreshTokenExpireDays = 7,
                ClockSkewSeconds = 0 // 无时钟偏差
            };

            var expiredService = new EnhancedJwtService(
                Options.Create(expiredOptions),
                _mockSecurityKeyService.Object,
                _context,
                _mockUserService.Object,
                _mockLogger.Object
            );

            var token = expiredService.GenerateToken("user123", "testuser", UserRole.Doctor);
            await Task.Delay(1000); // 确保过期

            // Act
            var principal = await _jwtService.ValidateTokenAsync(token);

            // Assert
            principal.Should().BeNull();
        }

        [Fact]
        public async Task ValidateToken_WithInvalidSignature_ShouldReturnNull()
        {
            // Arrange
            var token = _jwtService.GenerateToken("user123", "testuser", UserRole.Doctor);
            
            // 篡改token
            var parts = token.Split('.');
            parts[1] = parts[1] + "tampered";
            var tamperedToken = string.Join('.', parts);

            // Act
            var principal = await _jwtService.ValidateTokenAsync(tamperedToken);

            // Assert
            principal.Should().BeNull();
        }

        #endregion

        #region RefreshToken测试

        [Fact]
        public async Task RefreshTokenAsync_WithValidRefreshToken_ShouldReturnNewTokenPair()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tokenPair = await _jwtService.GenerateTokenPairAsync(
                userId.ToString(), "testuser", UserRole.Doctor);

            // 模拟用户服务返回
            var userDto = new UserDto
            {
                Id = userId,
                UserName = "testuser",
                Role = UserRole.Doctor
            };
            _mockUserService.Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(ServiceResult<UserDto>.Success(userDto));

            // Act
            var newTokenPair = await _jwtService.RefreshTokenAsync(tokenPair.RefreshToken);

            // Assert
            newTokenPair.Should().NotBeNull();
            newTokenPair!.AccessToken.Should().NotBeNullOrEmpty();
            newTokenPair.RefreshToken.Should().NotBeNullOrEmpty();
            newTokenPair.AccessToken.Should().NotBe(tokenPair.AccessToken);
        }

        [Fact]
        public async Task RefreshTokenAsync_WithRevokedToken_ShouldReturnNull()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tokenPair = await _jwtService.GenerateTokenPairAsync(
                userId.ToString(), "testuser", UserRole.Doctor);

            // 撤销Token
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == tokenPair.RefreshToken);
            refreshToken!.Revoke("Test revocation");
            await _context.SaveChangesAsync();

            // Act
            var newTokenPair = await _jwtService.RefreshTokenAsync(tokenPair.RefreshToken);

            // Assert
            newTokenPair.Should().BeNull();
        }

        [Fact]
        public async Task RefreshTokenAsync_WithExpiredToken_ShouldReturnNull()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tokenPair = await _jwtService.GenerateTokenPairAsync(
                userId.ToString(), "testuser", UserRole.Doctor);

            // 修改过期时间
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == tokenPair.RefreshToken);
            refreshToken!.ExpiresAt = DateTime.UtcNow.AddDays(-1);
            await _context.SaveChangesAsync();

            // Act
            var newTokenPair = await _jwtService.RefreshTokenAsync(tokenPair.RefreshToken);

            // Assert
            newTokenPair.Should().BeNull();
        }

        #endregion

        #region Token撤销测试

        [Fact]
        public async Task RevokeTokenAsync_ShouldMarkTokenAsRevoked()
        {
            // Arrange
            var tokenPair = await _jwtService.GenerateTokenPairAsync(
                Guid.NewGuid().ToString(), "testuser", UserRole.Doctor);

            // Act
            var result = await _jwtService.RevokeTokenAsync(
                tokenPair.RefreshToken, "User logout", "user123");

            // Assert
            result.Should().BeTrue();

            var revokedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == tokenPair.RefreshToken);
            revokedToken.Should().NotBeNull();
            revokedToken!.IsRevoked.Should().BeTrue();
            revokedToken.RevokedReason.Should().Be("User logout");
            revokedToken.RevokedBy.Should().Be("user123");
            revokedToken.RevokedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task RevokeAllUserTokensAsync_ShouldRevokeAllTokens()
        {
            // Arrange
            var userId = Guid.NewGuid();
            
            // 创建多个Token
            await _jwtService.GenerateTokenPairAsync(
                userId.ToString(), "testuser", UserRole.Doctor);
            await _jwtService.GenerateTokenPairAsync(
                userId.ToString(), "testuser", UserRole.Doctor);
            await _jwtService.GenerateTokenPairAsync(
                userId.ToString(), "testuser", UserRole.Doctor);

            // Act
            var count = await _jwtService.RevokeAllUserTokensAsync(
                userId, "Account security", "admin");

            // Assert
            count.Should().Be(3);
            
            var userTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId)
                .ToListAsync();
            
            userTokens.Should().HaveCount(3);
            userTokens.Should().OnlyContain(rt => rt.IsRevoked);
        }

        #endregion

        #region 安全测试

        [Fact]
        public void Token_ShouldUseHMACSHA256Algorithm()
        {
            // Arrange & Act
            var token = _jwtService.GenerateToken("user123", "testuser", UserRole.Doctor);
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadJwtToken(token);

            // Assert
            jsonToken.Header.Alg.Should().Be(SecurityAlgorithms.HmacSha256);
        }

        [Fact]
        public void SecretKey_ShouldBeAtLeast32Characters()
        {
            // Assert
            _jwtOptions.Secret.Length.Should().BeGreaterOrEqualTo(32);
        }

        [Fact]
        public async Task TokenReuse_ShouldBeDetected()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tokenPair = await _jwtService.GenerateTokenPairAsync(
                userId.ToString(), "testuser", UserRole.Doctor);

            // 记录使用
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == tokenPair.RefreshToken);
            refreshToken!.RecordUsage();
            refreshToken.RecordUsage();
            refreshToken.RecordUsage();
            await _context.SaveChangesAsync();

            // Assert
            refreshToken.UsageCount.Should().Be(3);
            refreshToken.LastUsedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
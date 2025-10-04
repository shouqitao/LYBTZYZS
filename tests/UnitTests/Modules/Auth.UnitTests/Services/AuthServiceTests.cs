using FluentAssertions;
using LYBT.Infrastructure.Data;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Services;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.Module.Auth.Tests.Services;

/// <summary>
/// AuthService 单元测试
/// Issue #864 - Phase 2.3: Auth 模块测试
/// </summary>
public class AuthServiceTests : IDisposable
{
    private readonly Mock<IJwtService> _mockJwtService;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<ILogger<AuthService>> _mockLogger;
    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _mockJwtService = new Mock<IJwtService>();
        _mockUserService = new Mock<IUserService>();
        _mockLogger = new Mock<ILogger<AuthService>>();

        // 使用 InMemory SQLite 创建真实 DbContext
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"DataSource=:memory:")
            .Options;
        _dbContext = new AppDbContext(options);
        _dbContext.Database.OpenConnection();
        _dbContext.Database.EnsureCreated();

        _configuration = CreateMockConfiguration();

        _sut = new AuthService(
            _mockJwtService.Object,
            _mockUserService.Object,
            _mockLogger.Object,
            _dbContext,
            _configuration
        );
    }

    private static IConfiguration CreateMockConfiguration()
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Auth:SuperAdmin:Username"]).Returns("sysadmin");
        config.Setup(c => c["Auth:SuperAdmin:Password"]).Returns("Admin@123");
        config.Setup(c => c["Jwt:AccessTokenExpirationMinutes"]).Returns("30");
        config.Setup(c => c["Jwt:RefreshTokenExpirationDays"]).Returns("7");
        return config.Object;
    }

    #region 超级管理员认证测试

    [Fact]
    public void IsSuperAdminCredentials_WithCorrectCredentials_ReturnsTrue()
    {
        // Arrange
        // TODO: 实现测试

        // Act

        // Assert
    }

    [Fact]
    public void IsSuperAdminCredentials_WithIncorrectUsername_ReturnsFalse()
    {
        // Arrange
        // TODO: 实现测试

        // Act

        // Assert
    }

    [Fact]
    public void IsSuperAdminCredentials_WithIncorrectPassword_ReturnsFalse()
    {
        // Arrange
        // TODO: 实现测试

        // Act

        // Assert
    }

    [Fact]
    public void IsSuperAdminCredentials_WithNullCredentials_ReturnsFalse()
    {
        // Arrange
        // TODO: 实现测试

        // Act

        // Assert
    }

    [Fact]
    public void IsSuperAdminCredentials_WhenConfigMissing_ReturnsFalse()
    {
        // Arrange
        // TODO: 实现测试

        // Act

        // Assert
    }

    [Fact]
    public async Task ChangeSysAdminPasswordAsync_UpdatesConfiguredPassword()
    {
        // Arrange
        // TODO: 实现测试

        // Act

        // Assert
    }

    #endregion

    #region 用户凭据验证测试

    [Fact]
    public async Task VerifyCredentialsAsync_WithValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var testUser = new UserDto
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            Role = LYBT.Shared.Models.Enums.UserRole.Doctor
        };

        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "Password123!"
        };

        _mockUserService.Setup(x => x.GetByUsernameAsync(request.Username))
            .ReturnsAsync(LYBT.Shared.Models.Contracts.Common.ServiceResult<UserDto>.Success(testUser));

        _mockUserService.Setup(x => x.ValidatePasswordAsync(testUser.Id, request.Password))
            .ReturnsAsync(LYBT.Shared.Models.Contracts.Common.ServiceResult<bool>.Success(true));

        // Act
        var result = await _sut.VerifyCredentialsAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(testUser.Id.ToString());
    }

    [Fact]
    public async Task VerifyCredentialsAsync_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "nonexistent",
            Password = "Password123!"
        };

        _mockUserService.Setup(x => x.GetByUsernameAsync(request.Username))
            .ReturnsAsync(LYBT.Shared.Models.Contracts.Common.ServiceResult<UserDto>.Failure("用户不存在"));

        // Act
        var result = await _sut.VerifyCredentialsAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("用户名或密码错误");
    }

    [Fact]
    public async Task VerifyCredentialsAsync_WithWrongPassword_ReturnsFailure()
    {
        // Arrange
        var testUser = new UserDto
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            Role = LYBT.Shared.Models.Enums.UserRole.Doctor
        };

        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "WrongPassword"
        };

        _mockUserService.Setup(x => x.GetByUsernameAsync(request.Username))
            .ReturnsAsync(LYBT.Shared.Models.Contracts.Common.ServiceResult<UserDto>.Success(testUser));

        _mockUserService.Setup(x => x.ValidatePasswordAsync(testUser.Id, request.Password))
            .ReturnsAsync(LYBT.Shared.Models.Contracts.Common.ServiceResult<bool>.Success(false));

        // Act
        var result = await _sut.VerifyCredentialsAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("用户名或密码错误");
    }

    [Fact]
    public async Task VerifyCredentialsAsync_WithLockedAccount_ReturnsFailure()
    {
        // Arrange
        // TODO: 实现测试

        // Act

        // Assert
    }

    [Fact]
    public async Task VerifyCredentialsAsync_WithDisabledAccount_ReturnsFailure()
    {
        // Arrange
        // TODO: 实现测试

        // Act

        // Assert
    }

    [Fact]
    public async Task VerifyCredentialsAsync_IncrementsFailedLoginCount_OnFailure()
    {
        // Arrange
        // TODO: 实现测试

        // Act

        // Assert
    }

    [Fact]
    public async Task VerifyCredentialsAsync_ResetsFailedLoginCount_OnSuccess()
    {
        // Arrange
        // TODO: 实现测试

        // Act

        // Assert
    }

    [Fact]
    public async Task VerifyCredentialsAsync_WithNullCredentials_ThrowsArgumentNullException()
    {
        // Arrange
        // TODO: 实现测试

        // Act & Assert
    }

    #endregion

    #region 登录流程测试

    [Fact]
    public async Task LoginAsync_WithValidUserCredentials_ReturnsToken()
    {
        // Arrange
        var testUser = new UserDto
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            RealName = "测试用户",
            Role = LYBT.Shared.Models.Enums.UserRole.Doctor,
            Email = "test@example.com"
        };

        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "Password123!"
        };

        var expectedToken = "test.jwt.token";

        _mockUserService.Setup(x => x.GetByUsernameAsync(request.Username))
            .ReturnsAsync(LYBT.Shared.Models.Contracts.Common.ServiceResult<UserDto>.Success(testUser));

        _mockUserService.Setup(x => x.ValidatePasswordAsync(testUser.Id, request.Password))
            .ReturnsAsync(LYBT.Shared.Models.Contracts.Common.ServiceResult<bool>.Success(true));

        _mockJwtService.Setup(x => x.GenerateToken(testUser.Id.ToString(), testUser.UserName, testUser.Role))
            .Returns(expectedToken);

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Token.Should().Be(expectedToken);
        result.Data.User.Should().BeEquivalentTo(testUser);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidCredentials_ReturnsFailure()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "wronguser",
            Password = "WrongPassword"
        };

        _mockUserService.Setup(x => x.GetByUsernameAsync(request.Username))
            .ReturnsAsync(LYBT.Shared.Models.Contracts.Common.ServiceResult<UserDto>.Failure("用户不存在"));

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task LoginAsync_GeneratesAccessToken()
    {
        // Arrange
        // TODO: 实现测试

        // Act

        // Assert
    }

    [Fact]
    public async Task LoginAsync_GeneratesRefreshToken()
    {
        // Arrange
        // TODO: 实现测试

        // Act

        // Assert
    }

    [Fact]
    public async Task LoginAsync_UpdatesLastLoginTime()
    {
        // Arrange
        // TODO: 实现测试

        // Act

        // Assert
    }

    [Fact]
    public async Task LoginAsync_SavesAuthentication()
    {
        // Arrange
        // TODO: 实现测试

        // Act

        // Assert
    }

    [Fact]
    public async Task LoginAsync_WithRememberMe_SetsLongerExpiration()
    {
        // Arrange
        // TODO: 实现测试

        // Act

        // Assert
    }

    [Fact]
    public async Task LoginAsync_WithoutRememberMe_SetsDefaultExpiration()
    {
        // Arrange
        // TODO: 实现测试

        // Act

        // Assert
    }

    [Fact]
    public async Task LoginAsync_WhenUserServiceFails_ReturnsFailure()
    {
        // Arrange
        // TODO: 实现测试

        // Act

        // Assert
    }

    [Fact]
    public async Task LoginAsync_WhenJwtServiceFails_ReturnsFailure()
    {
        // Arrange
        // TODO: 实现测试

        // Act

        // Assert
    }

    [Fact]
    public async Task LoginAsync_LogsLoginAttempt()
    {
        // Arrange
        // TODO: 实现测试

        // Act

        // Assert
    }

    #endregion

    #region 登出与Token管理测试

    [Fact]
    public async Task LogoutAsync_ReturnsSuccess()
    {
        // Arrange
        var request = new LogoutRequest();

        // Act
        var result = await _sut.LogoutAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Contain("登出成功");
    }

    [Fact]
    public async Task RefreshTokenAsync_ReturnsNotSupported()
    {
        // Arrange
        var refreshToken = "refresh.token.here";

        // Act
        var result = await _sut.RefreshTokenAsync(refreshToken);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("不支持");
    }

    [Fact]
    public async Task ValidateTokenAsync_WithValidToken_ReturnsSuccess()
    {
        // Arrange
        var token = "valid.token";
        var mockPrincipal = new System.Security.Claims.ClaimsPrincipal();

        _mockJwtService.Setup(x => x.ValidateToken(token))
            .Returns(mockPrincipal);

        // Act
        var result = await _sut.ValidateTokenAsync(token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateTokenAsync_WithInvalidToken_ReturnsFailure()
    {
        // Arrange
        var token = "invalid.token";

        _mockJwtService.Setup(x => x.ValidateToken(token))
            .Returns((System.Security.Claims.ClaimsPrincipal?)null);

        // Act
        var result = await _sut.ValidateTokenAsync(token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeTokenAsync_ReturnsSuccess()
    {
        // Arrange
        var request = new RevokeTokenRequest();

        // Act
        var result = await _sut.RevokeTokenAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region 会话管理测试

    [Fact]
    public async Task GetSessionInfoAsync_WithValidToken_ReturnsUserInfo()
    {
        // Arrange
        var token = "valid.token";
        var userId = Guid.NewGuid().ToString();
        var userName = "testuser";
        var role = "Doctor";

        var claims = new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, userName),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role)
        };

        var identity = new System.Security.Claims.ClaimsIdentity(claims, "TestAuth");
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);

        _mockJwtService.Setup(x => x.ValidateToken(token))
            .Returns(principal);

        // Act
        var result = await _sut.GetSessionInfoAsync(token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSessionInfoAsync_WithInvalidToken_ReturnsFailure()
    {
        // Arrange
        var token = "invalid.token";

        _mockJwtService.Setup(x => x.ValidateToken(token))
            .Returns((System.Security.Claims.ClaimsPrincipal?)null);

        // Act
        var result = await _sut.GetSessionInfoAsync(token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("令牌无效");
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    #endregion
}

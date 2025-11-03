using AutoMapper;
using FluentAssertions;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Services;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.Module.Auth.Tests.Services;

/// <summary>
/// AuthService 单元测试
/// Issue #864 - Phase 2.3: Auth 模块测试
/// Issue #1008 - 更新为匹配新的 AuthService 实现（IUserRepository + IMapper 替代 IUserService）
/// </summary>
public class AuthServiceTests : IDisposable
{
    private readonly Mock<IJwtService> _mockJwtService;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<AuthService>> _mockLogger;
    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _mockJwtService = new Mock<IJwtService>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<AuthService>>();

        // 使用 InMemory SQLite 创建真实 DbContext
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"DataSource=:memory:")
            .Options;
        _dbContext = new AppDbContext(options);
        _dbContext.Database.OpenConnection();
        _dbContext.Database.EnsureCreated();

        _configuration = CreateMockConfiguration();

        _sut = new AuthService(
            _mockJwtService.Object,
            _mockUserRepository.Object,
            _mockMapper.Object,
            _mockLogger.Object,
            _dbContext,
            _configuration
        );
    }

    private static IConfiguration CreateMockConfiguration()
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Lybt:Business:SystemAdmin:UserName"]).Returns("sysadmin");
        config.Setup(c => c["Lybt:Business:SystemAdmin:Email"]).Returns("admin@lybt.com");
        config.Setup(c => c["Jwt:AccessTokenExpirationMinutes"]).Returns("30");
        config.Setup(c => c["Jwt:RefreshTokenExpirationDays"]).Returns("7");
        return config.Object;
    }

    #region 用户凭据验证测试

    [Fact]
    public async Task VerifyCredentialsAsync_WithValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testUser = new User
        {
            Id = userId,
            UserName = "testuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!")
        };

        var request = new LoginRequest
        {
            UserName = "testuser",
            Password = "Password123!"
        };

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(request.UserName))
            .ReturnsAsync(testUser);

        // Act
        var result = await _sut.VerifyCredentialsAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(userId.ToString());
    }

    [Fact]
    public async Task VerifyCredentialsAsync_WithNonExistentUser_ReturnsFailure()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = "nonexistent",
            Password = "Password123!"
        };

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(request.UserName))
            .ReturnsAsync((User?)null);

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
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword")
        };

        var request = new LoginRequest
        {
            UserName = "testuser",
            Password = "WrongPassword"
        };

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(request.UserName))
            .ReturnsAsync(testUser);

        // Act
        var result = await _sut.VerifyCredentialsAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("用户名或密码错误");
    }

    [Fact]
    public async Task VerifyCredentialsAsync_WithEmptyUsername_ReturnsFailure()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = "",
            Password = "Password123!"
        };

        // Act
        var result = await _sut.VerifyCredentialsAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("用户名和密码不能为空");
    }

    [Fact]
    public async Task VerifyCredentialsAsync_WithEmptyPassword_ReturnsFailure()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = "testuser",
            Password = ""
        };

        // Act
        var result = await _sut.VerifyCredentialsAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("用户名和密码不能为空");
    }

    #endregion

    #region 登录流程测试

    [Fact]
    public async Task LoginAsync_WithValidUserCredentials_ReturnsToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testUser = new User
        {
            Id = userId,
            UserName = "testuser",
            RealName = "测试用户",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = UserRole.Doctor,
            Email = "test@example.com"
        };

        var testUserDto = new UserDto
        {
            Id = userId,
            UserName = "testuser",
            RealName = "测试用户",
            Role = UserRole.Doctor,
            Email = "test@example.com"
        };

        var request = new LoginRequest
        {
            UserName = "testuser",
            Password = "Password123!"
        };

        var expectedToken = "test.jwt.token";

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(request.UserName))
            .ReturnsAsync(testUser);

        _mockMapper.Setup(x => x.Map<UserDto>(testUser))
            .Returns(testUserDto);

        _mockJwtService.Setup(x => x.GenerateToken(
            testUserDto.Id.ToString(),
            testUserDto.UserName,
            testUserDto.Role))
            .Returns(expectedToken);

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Token.Should().Be(expectedToken);
        result.Data.User.Should().BeEquivalentTo(testUserDto);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidCredentials_ReturnsFailure()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = "wronguser",
            Password = "WrongPassword"
        };

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(request.UserName))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("用户名或密码错误");
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ReturnsFailure()
    {
        // Arrange
        var testUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = "testuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword")
        };

        var request = new LoginRequest
        {
            UserName = "testuser",
            Password = "WrongPassword"
        };

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(request.UserName))
            .ReturnsAsync(testUser);

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
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

    #endregion

    #region IDisposable Implementation

    public void Dispose()
    {
        _dbContext?.Dispose();
        GC.SuppressFinalize(this);
    }

    #endregion
}

using AutoMapper;
using FluentAssertions;
using LYBT.Entities.Auth;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Services;
using LYBT.Module.Users.Interfaces;
using LYBT.Shared.Models.Common;
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
/// Issue #1864 - 重新引入 IUserService 实现 Auth/User 职责分离
/// </summary>
public class AuthServiceTests : IDisposable
{
    private readonly Mock<IJwtService> _mockJwtService;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IUserService> _mockUserService; // Issue #1864: 职责分离
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<AuthService>> _mockLogger;
    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly Mock<ITokenRevocationService> _mockRevocationService;
    private readonly Mock<ISecurityAuditService> _mockAuditService;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _mockJwtService = new Mock<IJwtService>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockUserService = new Mock<IUserService>(); // Issue #1864: 职责分离
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<AuthService>>();
        _mockRevocationService = new Mock<ITokenRevocationService>();
        _mockAuditService = new Mock<ISecurityAuditService>();

        // Issue #1872: Setup audit service mock to return completed task
        _mockAuditService.Setup(x => x.LogAsync(It.IsAny<LYBT.Module.Auth.Models.SecurityAuditEvent>()))
            .Returns(Task.CompletedTask);

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
            _mockUserService.Object, // Issue #1864: 职责分离
            _mockMapper.Object,
            _mockLogger.Object,
            _dbContext,
            _configuration,
            _mockRevocationService.Object,
            _mockAuditService.Object
        );
    }

    private static IConfiguration CreateMockConfiguration()
    {
        var config = new Mock<IConfiguration>();
        // Issue #1872: 修正配置键以匹配AuthService实际使用的键
        config.Setup(c => c["Lybt:SystemAdmin:UserName"]).Returns("sysadmin");
        config.Setup(c => c["Lybt:SystemAdmin:Email"]).Returns("admin@lybt.com");
        config.Setup(c => c["Lybt:SystemAdmin:Username"]).Returns("admin"); // RefreshTokenAsync使用
        config.Setup(c => c.GetSection("Lybt:Jwt:ExpireMinutes").Value).Returns("15");
        config.Setup(c => c.GetSection("Lybt:Jwt:RefreshTokenExpirationDays").Value).Returns("7");
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

        var testUserDto = new UserDetailDto
        {
            Id = userId,
            UserName = "testuser"
        };

        var request = new LoginRequest
        {
            UserName = "testuser",
            Password = "Password123!"
        };

        // Issue #1864: Mock IUserService.ValidatePasswordAsync
        _mockUserService.Setup(x => x.ValidatePasswordAsync(request.UserName, request.Password))
            .ReturnsAsync(Result<UserDetailDto>.Success(testUserDto));

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

        // Issue #1864: Mock IUserService.ValidatePasswordAsync返回失败
        _mockUserService.Setup(x => x.ValidatePasswordAsync(request.UserName, request.Password))
            .ReturnsAsync(Result<UserDetailDto>.Failure("用户名或密码错误"));

        // Act
        var result = await _sut.VerifyCredentialsAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("用户名或密码错误");
    }

    [Fact]
    public async Task VerifyCredentialsAsync_WithWrongPassword_ReturnsFailure()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = "testuser",
            Password = "WrongPassword"
        };

        // Issue #1864: Mock IUserService.ValidatePasswordAsync返回失败
        _mockUserService.Setup(x => x.ValidatePasswordAsync(request.UserName, request.Password))
            .ReturnsAsync(Result<UserDetailDto>.Failure("用户名或密码错误"));

        // Act
        var result = await _sut.VerifyCredentialsAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("用户名或密码错误");
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
        result.ErrorMessage.Should().Contain("用户名和密码不能为空");
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
        result.ErrorMessage.Should().Contain("用户名和密码不能为空");
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

        // Issue #1872: 先将用户保存到数据库，因为RefreshToken有外键约束
        await _dbContext.Users.AddAsync(testUser);
        await _dbContext.SaveChangesAsync();

        var testUserDto = new UserDetailDto
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

        // Issue #1864: Mock IUserService.ValidatePasswordAsync for VerifyCredentialsAsync
        _mockUserService.Setup(x => x.ValidatePasswordAsync(request.UserName, request.Password))
            .ReturnsAsync(Result<UserDetailDto>.Success(testUserDto));

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(request.UserName))
            .ReturnsAsync(testUser);

        _mockMapper.Setup(x => x.Map<UserDetailDto>(testUser))
            .Returns(testUserDto);

        _mockJwtService.Setup(x => x.GenerateToken(
            testUserDto.Id.ToString(),
            testUserDto.UserName,
            testUserDto.Role,
            It.IsAny<string>())) // Issue #1861: userType参数
            .Returns(expectedToken);

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        // Issue #1872: 添加失败消息输出以便诊断
        result.IsSuccess.Should().BeTrue($"Login should succeed, but failed with: {result.ErrorMessage}");
        result.Data.Should().NotBeNull();
        result.Data!.Token.Should().Be(expectedToken);
        result.Data.User.Should().BeEquivalentTo(testUserDto);
        result.Data.RefreshToken.Should().NotBeNullOrEmpty(); // Issue #1872: 验证RefreshToken
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

        // Issue #1864: Mock IUserService.ValidatePasswordAsync返回失败
        _mockUserService.Setup(x => x.ValidatePasswordAsync(request.UserName, request.Password))
            .ReturnsAsync(Result<UserDetailDto>.Failure("用户名或密码错误"));

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("用户名或密码错误");
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ReturnsFailure()
    {
        // Arrange
        var request = new LoginRequest
        {
            UserName = "testuser",
            Password = "WrongPassword"
        };

        // Issue #1864: Mock IUserService.ValidatePasswordAsync返回失败
        _mockUserService.Setup(x => x.ValidatePasswordAsync(request.UserName, request.Password))
            .ReturnsAsync(Result<UserDetailDto>.Failure("用户名或密码错误"));

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
    public async Task RefreshTokenAsync_WithNonExistentToken_ReturnsFailure()
    {
        // Arrange
        var refreshToken = "nonexistent.refresh.token";

        // Act
        var result = await _sut.RefreshTokenAsync(refreshToken);

        // Assert
        // Issue #1872: RefreshTokenAsync现在实际实现了功能，不存在的token应返回失败
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("RefreshToken不存在");
    }

    [Fact]
    public async Task RefreshTokenAsync_RevokedToken_ShouldReturnFailure()
    {
        // Arrange - 创建已撤销的RefreshToken
        var userId = Guid.NewGuid();
        var testUser = new User
        {
            Id = userId,
            UserName = "testuser",
            RealName = "测试用户",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = UserRole.Doctor
        };

        await _dbContext.Users.AddAsync(testUser);

        var revokedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "revoked-refresh-token",
            UserId = userId,
            UserType = "user",
            Jti = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = true,
            RevokedAt = DateTime.UtcNow.AddHours(-1),
            RevokedReason = "Security issue",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        await _dbContext.RefreshTokens.AddAsync(revokedToken);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.RefreshTokenAsync(revokedToken.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("已撤销");
    }

    [Fact]
    public async Task RefreshTokenAsync_Success_ShouldMarkOldTokenAsUsedAndGenerateNewToken()
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

        await _dbContext.Users.AddAsync(testUser);

        var oldRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "old-refresh-token",
            UserId = userId,
            UserType = "user",
            Jti = Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        await _dbContext.RefreshTokens.AddAsync(oldRefreshToken);
        await _dbContext.SaveChangesAsync();

        var testUserDto = new UserDetailDto
        {
            Id = userId,
            UserName = "testuser",
            RealName = "测试用户",
            Role = UserRole.Doctor,
            Email = "test@example.com"
        };

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(testUser);

        _mockMapper.Setup(x => x.Map<UserDetailDto>(testUser))
            .Returns(testUserDto);

        _mockJwtService.Setup(x => x.GenerateToken(
            testUserDto.Id.ToString(),
            testUserDto.UserName,
            testUserDto.Role,
            It.IsAny<string>()))
            .Returns("new.jwt.token");

        // Act
        var result = await _sut.RefreshTokenAsync(oldRefreshToken.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Issue #1864 AUTH-007: 验证旧Token已被标记为已使用（而非撤销，用于重放攻击检测）
        var oldToken = await _dbContext.RefreshTokens
            .FirstAsync(t => t.Token == oldRefreshToken.Token);
        oldToken.IsUsed.Should().BeTrue();
        oldToken.ReplacedByToken.Should().NotBeNullOrEmpty();

        // 验证返回新Token
        result.Data.Should().NotBeNull();
        result.Data!.RefreshToken.Should().NotBe(oldRefreshToken.Token);
    }

    [Fact]
    public async Task LoginAsync_Success_ShouldRecordAuditLog()
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

        await _dbContext.Users.AddAsync(testUser);
        await _dbContext.SaveChangesAsync();

        var testUserDto = new UserDetailDto
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

        // Issue #1864: Mock IUserService.ValidatePasswordAsync for VerifyCredentialsAsync
        _mockUserService.Setup(x => x.ValidatePasswordAsync(request.UserName, request.Password))
            .ReturnsAsync(Result<UserDetailDto>.Success(testUserDto));

        _mockUserRepository.Setup(x => x.GetByUsernameAsync(request.UserName))
            .ReturnsAsync(testUser);

        _mockMapper.Setup(x => x.Map<UserDetailDto>(testUser))
            .Returns(testUserDto);

        _mockJwtService.Setup(x => x.GenerateToken(
            testUserDto.Id.ToString(),
            testUserDto.UserName,
            testUserDto.Role,
            It.IsAny<string>()))
            .Returns("test.jwt.token");

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // 验证审计服务被调用记录登录成功
        _mockAuditService.Verify(x => x.LogAsync(
            It.Is<LYBT.Module.Auth.Models.SecurityAuditEvent>(e =>
                e.EventType == "Login" &&
                e.UserId == userId &&
                e.UserName == "testuser" &&
                e.Success == true
            )), Times.Once);
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
        // Issue #1864: 无效Token返回Failure而不是Success(false)
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(LYBT.Shared.Models.Enums.AuthErrorCode.TokenInvalid);
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
        // Issue #1864: 使用AuthErrorCode.TokenInvalid映射的错误消息
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("登录凭据无效");
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

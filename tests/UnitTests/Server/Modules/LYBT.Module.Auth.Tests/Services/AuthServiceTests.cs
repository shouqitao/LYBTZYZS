using FluentAssertions;
using LYBT.Entities.Auth;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Services.CrossModule;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.DTOs.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Xunit;

namespace LYBT.Module.Auth.Tests.Services;

/// <summary>
/// AuthService 单元测试
/// 重构: 使用 IUserCrossModuleService 替代 IUserRepository + IUserService
/// </summary>
public class AuthServiceTests : IDisposable
{
    private readonly IJwtService _jwtService;
    private readonly IUserCrossModuleService _crossModuleQuery;
    private readonly ILogger<AuthService> _logger;
    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ITokenRevocationService _revocationService;
    private readonly ISecurityAuditService _auditService;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _jwtService = Substitute.For<IJwtService>();
        _crossModuleQuery = Substitute.For<IUserCrossModuleService>();
        _logger = Substitute.For<ILogger<AuthService>>();
        _revocationService = Substitute.For<ITokenRevocationService>();
        _auditService = Substitute.For<ISecurityAuditService>();

        // Setup audit service to return completed task
        _auditService.LogAsync(Arg.Any<LYBT.Module.Auth.Models.SecurityAuditEvent>())
            .Returns(Task.CompletedTask);

        // 使用 InMemory SQLite 创建真实 DbContext
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"DataSource=:memory:")
            .Options;
        _dbContext = new AppDbContext(options);
        _dbContext.Database.OpenConnection();
        _dbContext.Database.EnsureCreated();

        _configuration = CreateSubstituteConfiguration();

        _sut = new AuthService(
            _jwtService,
            _crossModuleQuery,
            _logger,
            _dbContext,
            _configuration,
            _revocationService,
            _auditService
        );
    }

    private static IConfiguration CreateSubstituteConfiguration()
    {
        var config = Substitute.For<IConfiguration>();
        config["Lybt:SystemAdmin:UserName"].Returns("sysadmin");
        config["Lybt:SystemAdmin:Email"].Returns("admin@lybt.com");
        config["Lybt:SystemAdmin:Username"].Returns("admin");
        config.GetSection("Lybt:Jwt:ExpireMinutes").Value.Returns("15");
        config.GetSection("Lybt:Jwt:RefreshTokenExpirationDays").Value.Returns("7");
        return config;
    }

    #region 用户凭据验证测试

    [Fact]
    public async Task VerifyCredentialsAsync_WithValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new LoginRequest
        {
            UserName = "testuser",
            Password = "Password123!"
        };

        _crossModuleQuery.GetUserByUsernameAsync(request.UserName)
            .Returns(new UserCredentialDto
            {
                Id = userId,
                UserName = "testuser",
                RealName = "",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!")
            });

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

        _crossModuleQuery.GetUserByUsernameAsync(request.UserName)
            .ReturnsNull();

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

        _crossModuleQuery.GetUserByUsernameAsync(request.UserName)
            .Returns(new UserCredentialDto
            {
                Id = Guid.NewGuid(),
                UserName = "testuser",
                RealName = "",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword")
            });

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
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("Password123!");

        var testUser = new User
        {
            Id = userId,
            UserName = "testuser",
            RealName = "测试用户",
            PasswordHash = passwordHash,
            Role = UserRole.Doctor,
            Email = "test@example.com"
        };

        // 先将用户保存到数据库，因为RefreshToken有外键约束
        await _dbContext.Users.AddAsync(testUser);
        await _dbContext.SaveChangesAsync();

        var userCredentialDto = new UserCredentialDto
        {
            Id = userId,
            UserName = "testuser",
            RealName = "测试用户",
            Role = UserRole.Doctor,
            Status = CommonStatus.Enabled,
            PasswordHash = passwordHash
        };

        var request = new LoginRequest
        {
            UserName = "testuser",
            Password = "Password123!"
        };

        var expectedToken = "test.jwt.token";

        // Setup: GetUserByUsernameAsync is called by both VerifyCredentialsAsync and LoginAsync
        _crossModuleQuery.GetUserByUsernameAsync(request.UserName)
            .Returns(userCredentialDto);

        _jwtService.GenerateToken(
            userId.ToString(),
            "testuser",
            UserRole.Doctor,
            Arg.Any<string>())
            .Returns(expectedToken);

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue($"Login should succeed, but failed with: {result.ErrorMessage}");
        result.Data.Should().NotBeNull();
        result.Data!.Token.Should().Be(expectedToken);
        result.Data.User.UserName.Should().Be("testuser");
        result.Data.User.RealName.Should().Be("测试用户");
        result.Data.User.Role.Should().Be(UserRole.Doctor);
        result.Data.RefreshToken.Should().NotBeNullOrEmpty();
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

        // User does not exist
        _crossModuleQuery.GetUserByUsernameAsync(request.UserName)
            .ReturnsNull();

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

        _crossModuleQuery.GetUserByUsernameAsync(request.UserName)
            .Returns(new UserCredentialDto
            {
                Id = Guid.NewGuid(),
                UserName = "testuser",
                RealName = "",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword")
            });

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

        _crossModuleQuery.GetUserBasicInfoAsync(userId)
            .Returns(new UserBasicDto
            {
                Id = userId,
                UserName = "testuser",
                RealName = "测试用户",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled
            });

        _jwtService.GenerateToken(
            userId.ToString(),
            "testuser",
            UserRole.Doctor,
            Arg.Any<string>())
            .Returns("new.jwt.token");

        // Act
        var result = await _sut.RefreshTokenAsync(oldRefreshToken.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // 验证旧Token已被标记为已使用（用于重放攻击检测）
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
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("Password123!");

        var testUser = new User
        {
            Id = userId,
            UserName = "testuser",
            RealName = "测试用户",
            PasswordHash = passwordHash,
            Role = UserRole.Doctor,
            Email = "test@example.com"
        };

        await _dbContext.Users.AddAsync(testUser);
        await _dbContext.SaveChangesAsync();

        var userCredentialDto = new UserCredentialDto
        {
            Id = userId,
            UserName = "testuser",
            RealName = "测试用户",
            Role = UserRole.Doctor,
            Status = CommonStatus.Enabled,
            PasswordHash = passwordHash
        };

        var request = new LoginRequest
        {
            UserName = "testuser",
            Password = "Password123!"
        };

        _crossModuleQuery.GetUserByUsernameAsync(request.UserName)
            .Returns(userCredentialDto);

        _jwtService.GenerateToken(
            userId.ToString(),
            "testuser",
            UserRole.Doctor,
            Arg.Any<string>())
            .Returns("test.jwt.token");

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // 验证审计服务被调用记录登录成功
        await _auditService.Received(1).LogAsync(
            Arg.Is<LYBT.Module.Auth.Models.SecurityAuditEvent>(e =>
                e.EventType == "Login" &&
                e.UserId == userId &&
                e.UserName == "testuser" &&
                e.Success == true
            ));
    }

    [Fact]
    public async Task ValidateTokenAsync_WithValidToken_ReturnsSuccess()
    {
        // Arrange
        var token = "valid.token";
        var mockPrincipal = new System.Security.Claims.ClaimsPrincipal();

        _jwtService.ValidateToken(token)
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

        _jwtService.ValidateToken(token)
            .Returns((System.Security.Claims.ClaimsPrincipal?)null);

        // Act
        var result = await _sut.ValidateTokenAsync(token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ModuleErrorCode.Should().Be(LYBT.Shared.Primitives.ErrorCodes.ErrorCode.AuthTokenInvalid);
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

        _jwtService.ValidateToken(token)
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

        _jwtService.ValidateToken(token)
            .Returns((System.Security.Claims.ClaimsPrincipal?)null);

        // Act
        var result = await _sut.GetSessionInfoAsync(token);

        // Assert
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

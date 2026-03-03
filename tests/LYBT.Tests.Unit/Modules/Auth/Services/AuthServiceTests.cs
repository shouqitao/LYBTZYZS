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

namespace LYBT.Tests.Unit.Modules.Auth.Services;

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
    private readonly ITokenManagementService _tokenManagement;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _jwtService = Substitute.For<IJwtService>();
        _crossModuleQuery = Substitute.For<IUserCrossModuleService>();
        _logger = Substitute.For<ILogger<AuthService>>();
        _revocationService = Substitute.For<ITokenRevocationService>();
        _auditService = Substitute.For<ISecurityAuditService>();
        _tokenManagement = Substitute.For<ITokenManagementService>();

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
            _auditService,
            _tokenManagement
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

        _tokenManagement.RefreshTokenAsync(refreshToken)
            .Returns(Result<LoginResponse>.Failure(
                LYBT.Shared.Primitives.ErrorCodes.ErrorCode.AuthRefreshTokenInvalid,
                "RefreshToken不存在"));

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
        var revokedTokenValue = "revoked-refresh-token";

        _tokenManagement.RefreshTokenAsync(revokedTokenValue)
            .Returns(Result<LoginResponse>.Failure(
                LYBT.Shared.Primitives.ErrorCodes.ErrorCode.AuthTokenRevoked,
                "RefreshToken已撤销"));

        // Act
        var result = await _sut.RefreshTokenAsync(revokedTokenValue);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("已撤销");
    }

    [Fact]
    public async Task RefreshTokenAsync_Success_ShouldReturnNewToken()
    {
        // Arrange
        var oldRefreshTokenValue = "old-refresh-token";
        var newRefreshTokenValue = "new-refresh-token";
        var userId = Guid.NewGuid();

        var successResponse = new LoginResponse
        {
            Token = "new.jwt.token",
            RefreshToken = newRefreshTokenValue,
            User = new UserDetailDto
            {
                Id = userId,
                UserName = "testuser",
                RealName = "测试用户",
                Role = UserRole.Doctor
            },
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };

        _tokenManagement.RefreshTokenAsync(oldRefreshTokenValue)
            .Returns(Result<LoginResponse>.Success(successResponse));

        // Act
        var result = await _sut.RefreshTokenAsync(oldRefreshTokenValue);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.RefreshToken.Should().NotBe(oldRefreshTokenValue);
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

        _tokenManagement.ValidateTokenAsync(token)
            .Returns(Result<bool>.Success(true));

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

        _tokenManagement.ValidateTokenAsync(token)
            .Returns(Result<bool>.Failure(
                LYBT.Shared.Primitives.ErrorCodes.ErrorCode.AuthTokenInvalid,
                "Token无效"));

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
        var sessionInfo = new { UserId = Guid.NewGuid().ToString(), UserName = "testuser", Role = "Doctor" };

        _tokenManagement.GetSessionInfoAsync(token)
            .Returns(Result<object>.Success(sessionInfo));

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

        _tokenManagement.GetSessionInfoAsync(token)
            .Returns(Result<object>.Failure(
                LYBT.Shared.Primitives.ErrorCodes.ErrorCode.AuthTokenInvalid,
                "登录凭据无效"));

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

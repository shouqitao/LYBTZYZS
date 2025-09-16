using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using LYBT.Module.Auth.Helpers;
using LYBT.Module.Auth.Repositories;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Options;
using LYBT.Shared.Models.Auth.Request;
using LYBT.Shared.Models.Enums;
using LYBT.Entities.Users;
using LYBT.Module.Auth.Services;

namespace LYBT.Module.Auth.Tests.Enhanced;

/// <summary>
/// 登录安全增强测试 - UltraThink Phase 3
/// 测试5次失败后锁定账户的安全机制
/// </summary>
public class LoginSecurityTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly AuthValidationHelper _authValidationHelper;
    private readonly IAuthRepository _authRepository;
    private readonly AuthOptions _authOptions;
    private readonly Mock<ILogger<AuthValidationHelper>> _mockLogger;
    private readonly Mock<SysAdminHandler> _mockSysAdminHandler;
    private readonly List<User> _testUsers;

    public LoginSecurityTests()
    {
        // 配置内存数据库
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        // 配置AuthOptions - 使用较短的锁定时间便于测试
        _authOptions = new AuthOptions
        {
            MaxFailedLoginAttempts = 5,
            AccountLockoutDuration = TimeSpan.FromMinutes(15),
            EnableDetailedLoginLogging = true,
            SupportedLoginTypes = new List<string> { "Password" }
        };

        var mockAuthOptions = new Mock<IOptions<AuthOptions>>();
        mockAuthOptions.Setup(x => x.Value).Returns(_authOptions);

        // 设置Mock服务
        _mockLogger = new Mock<ILogger<AuthValidationHelper>>();
        _mockSysAdminHandler = new Mock<SysAdminHandler>(null, null);
        _mockSysAdminHandler.Setup(x => x.IsSysAdmin(It.IsAny<string>())).Returns(false);

        // 创建Repository和Helper
        _authRepository = new AuthRepository(_context);
        _authValidationHelper = new AuthValidationHelper(
            _authRepository, 
            _mockSysAdminHandler.Object, 
            mockAuthOptions.Object,
            _mockLogger.Object
        );

        // 初始化测试数据
        _testUsers = CreateTestUsers();
        SeedTestData();
    }

    private List<User> CreateTestUsers()
    {
        return new List<User>
        {
            new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser1",
                RealName = "测试用户1",
                PasswordHash = "AQAAAAEAACcQAAAAEGwrB7Ri8YXw4qPHiXoTJQKoNccNRjvMFWuNi4W5YYp3DhIRRtxb0AHjD+WnzGLCmw==", // Test@123456
                Status = CommonStatus.Enabled,
                Role = UserRole.Doctor,
                FailedLoginCount = 0,
                LockoutEnd = null
            },
            new User
            {
                Id = Guid.NewGuid(),
                Username = "lockeduser",
                RealName = "已锁定用户",
                PasswordHash = "AQAAAAEAACcQAAAAEGwrB7Ri8YXw4qPHiXoTJQKoNccNRjvMFWuNi4W5YYp3DhIRRtxb0AHjD+WnzGLCmw==", // Test@123456
                Status = CommonStatus.Enabled,
                Role = UserRole.Receptionist,
                FailedLoginCount = 5,
                LockoutEnd = DateTime.UtcNow.AddMinutes(10) // 已锁定10分钟
            }
        };
    }

    private void SeedTestData()
    {
        _context.Users.AddRange(_testUsers);
        _context.SaveChanges();
    }

    #region 账户锁定检查测试

    [Fact]
    public async Task CheckAccountLockoutAsync_UnlockedUser_ShouldReturnSuccess()
    {
        // Arrange
        var user = _testUsers.First(u => u.Username == "testuser1");

        // Act
        var result = await _authValidationHelper.CheckAccountLockoutAsync(user.Username);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CheckAccountLockoutAsync_LockedUser_ShouldReturnFailure()
    {
        // Arrange
        var user = _testUsers.First(u => u.Username == "lockeduser");

        // Act
        var result = await _authValidationHelper.CheckAccountLockoutAsync(user.Username);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("账户已锁定");
        result.ErrorMessage.Should().Contain("剩余时间");
    }

    [Fact]
    public async Task CheckAccountLockoutAsync_NonExistentUser_ShouldReturnSuccess()
    {
        // Act
        var result = await _authValidationHelper.CheckAccountLockoutAsync("nonexistentuser");

        // Assert - 不存在的用户也返回成功，避免暴露用户存在性
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region 登录失败记录测试

    [Fact]
    public async Task RecordLoginFailureAsync_FirstFailure_ShouldIncrementCount()
    {
        // Arrange
        var user = _testUsers.First(u => u.Username == "testuser1");
        var originalCount = user.FailedLoginCount;

        // Act
        await _authValidationHelper.RecordLoginFailureAsync(user.Username);

        // Assert
        var updatedUser = await _context.Users.FirstAsync(u => u.Id == user.Id);
        updatedUser.FailedLoginCount.Should().Be(originalCount + 1);
        updatedUser.LockoutEnd.Should().BeNull(); // 未达到锁定阈值
    }

    [Fact]
    public async Task RecordLoginFailureAsync_FifthFailure_ShouldLockAccount()
    {
        // Arrange
        var user = _testUsers.First(u => u.Username == "testuser1");
        
        // 先记录4次失败
        for (int i = 0; i < 4; i++)
        {
            await _authValidationHelper.RecordLoginFailureAsync(user.Username);
        }

        // Act - 第5次失败应该触发锁定
        await _authValidationHelper.RecordLoginFailureAsync(user.Username);

        // Assert
        var updatedUser = await _context.Users.FirstAsync(u => u.Id == user.Id);
        updatedUser.FailedLoginCount.Should().Be(5);
        updatedUser.LockoutEnd.Should().NotBeNull();
        updatedUser.LockoutEnd.Should().BeAfter(DateTime.UtcNow.AddMinutes(14)); // 应该锁定约15分钟
    }

    [Fact]
    public async Task RecordLoginFailureAsync_SystemAdmin_ShouldNotRecord()
    {
        // Arrange
        _mockSysAdminHandler.Setup(x => x.IsSysAdmin("sysadmin")).Returns(true);

        // Act
        await _authValidationHelper.RecordLoginFailureAsync("sysadmin");

        // Assert - 系统管理员不应该被记录失败或锁定
        // 验证日志被调用但没有实际的失败记录
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("系统管理员登录失败但不启用锁定机制")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    #endregion

    #region 失败计数重置测试

    [Fact]
    public async Task ResetFailedAttemptsAsync_UserWithFailures_ShouldClearCountAndLockout()
    {
        // Arrange
        var user = _testUsers.First(u => u.Username == "testuser1");
        
        // 先设置一些失败记录
        await _authRepository.UpdateUserSecurityAsync(user.Id, 3, DateTime.UtcNow.AddMinutes(5));

        // Act
        await _authValidationHelper.ResetFailedAttemptsAsync(user.Username);

        // Assert
        var updatedUser = await _context.Users.FirstAsync(u => u.Id == user.Id);
        updatedUser.FailedLoginCount.Should().Be(0);
        updatedUser.LockoutEnd.Should().BeNull();
    }

    [Fact]
    public async Task ResetFailedAttemptsAsync_UserWithoutFailures_ShouldNotUpdate()
    {
        // Arrange
        var user = _testUsers.First(u => u.Username == "testuser1");
        // 确保用户没有失败记录
        await _authRepository.UpdateUserSecurityAsync(user.Id, 0, null);

        // Act
        await _authValidationHelper.ResetFailedAttemptsAsync(user.Username);

        // Assert - 应该没有实际的数据库更新操作
        var updatedUser = await _context.Users.FirstAsync(u => u.Id == user.Id);
        updatedUser.FailedLoginCount.Should().Be(0);
        updatedUser.LockoutEnd.Should().BeNull();
    }

    #endregion

    #region 完整登录流程测试

    [Fact]
    public async Task LoginFlow_CorrectPassword_ShouldResetFailures()
    {
        // Arrange
        var user = _testUsers.First(u => u.Username == "testuser1");
        
        // 先设置一些失败记录
        await _authRepository.UpdateUserSecurityAsync(user.Id, 2, null);
        
        var loginRequest = new LoginRequest
        {
            Username = user.Username,
            Password = "Test@123456", // 正确密码
            LoginType = "Password"
        };

        // Act
        var result = await _authValidationHelper.VerifyCredentialsInternalAsync(loginRequest);

        // Assert - 登录成功且失败计数被重置
        result.Should().Be(user.Username);
        
        var updatedUser = await _context.Users.FirstAsync(u => u.Id == user.Id);
        updatedUser.FailedLoginCount.Should().Be(0);
    }

    [Fact]
    public async Task LoginFlow_WrongPassword_ShouldIncrementFailures()
    {
        // Arrange
        var user = _testUsers.First(u => u.Username == "testuser1");
        var originalCount = user.FailedLoginCount;
        
        var loginRequest = new LoginRequest
        {
            Username = user.Username,
            Password = "WrongPassword",
            LoginType = "Password"
        };

        // Act
        var result = await _authValidationHelper.VerifyCredentialsInternalAsync(loginRequest);

        // Assert - 登录失败且失败计数增加
        result.Should().BeNull();
        
        var updatedUser = await _context.Users.FirstAsync(u => u.Id == user.Id);
        updatedUser.FailedLoginCount.Should().Be(originalCount + 1);
    }

    [Fact]
    public async Task LoginFlow_LockedAccount_ShouldRejectLogin()
    {
        // Arrange
        var lockedUser = _testUsers.First(u => u.Username == "lockeduser");
        
        var loginRequest = new LoginRequest
        {
            Username = lockedUser.Username,
            Password = "Test@123456", // 即使密码正确
            LoginType = "Password"
        };

        // Act
        var result = await _authValidationHelper.VerifyCredentialsInternalAsync(loginRequest);

        // Assert - 账户锁定时即使密码正确也应该拒绝登录
        result.Should().BeNull();
    }

    #endregion

    #region 边界条件测试

    [Fact]
    public async Task LoginSecurity_ExactlyMaxFailures_ShouldLockAccount()
    {
        // Arrange
        var user = _testUsers.First(u => u.Username == "testuser1");
        var maxAttempts = _authOptions.MaxFailedLoginAttempts;

        // Act - 记录精确的最大失败次数
        for (int i = 0; i < maxAttempts; i++)
        {
            await _authValidationHelper.RecordLoginFailureAsync(user.Username);
        }

        // Assert
        var updatedUser = await _context.Users.FirstAsync(u => u.Id == user.Id);
        updatedUser.FailedLoginCount.Should().Be(maxAttempts);
        updatedUser.LockoutEnd.Should().NotBeNull();
    }

    [Fact]
    public async Task LoginSecurity_LockoutDurationAccuracy_ShouldMatchConfiguration()
    {
        // Arrange
        var user = _testUsers.First(u => u.Username == "testuser1");
        var expectedDuration = _authOptions.AccountLockoutDuration;
        var beforeLockout = DateTime.UtcNow;

        // Act - 触发账户锁定
        for (int i = 0; i < _authOptions.MaxFailedLoginAttempts; i++)
        {
            await _authValidationHelper.RecordLoginFailureAsync(user.Username);
        }

        var afterLockout = DateTime.UtcNow;

        // Assert
        var updatedUser = await _context.Users.FirstAsync(u => u.Id == user.Id);
        var actualLockoutDuration = updatedUser.LockoutEnd!.Value - beforeLockout;
        var expectedMinDuration = expectedDuration.Subtract(TimeSpan.FromSeconds(1)); // 允许1秒误差
        var expectedMaxDuration = expectedDuration.Add(TimeSpan.FromSeconds(1));

        actualLockoutDuration.Should().BeGreaterOrEqualTo(expectedMinDuration);
        actualLockoutDuration.Should().BeLessOrEqualTo(expectedMaxDuration);
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}
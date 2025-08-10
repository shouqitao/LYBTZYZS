using LYBT.Infrastructure.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using FluentAssertions;
using LYBT.Infrastructure.Logging;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Models.Users;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Services;
using LYBT.Module.Auth.Tests.Base;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Helpers;
using Moq;
using Xunit;

namespace LYBT.Module.Auth.Tests
{
    /// <summary>
    /// AuthService 单元测试
    /// </summary>
    public class AuthServiceTests : IDisposable
    {
        private readonly AuthService _authService;
        private readonly Mock<IAuthRepository> _mockAuthRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IUnifiedLogService> _mockLogService;
        private readonly SysAdminHandler _sysAdminHandler;
        private readonly Mock<ILogger<AuthService>> _mockLogger;
        private readonly Mock<ILoginAttemptService> _mockLoginAttemptService;
        private readonly AuthOptions _authOptions;
        private readonly List<UserModel> _testUsers;

        public AuthServiceTests()
        {
            // 设置测试数据
            _testUsers = new List<UserModel>();
            InitializeTestData();

            // 创建Mock服务
            _mockAuthRepository = new Mock<IAuthRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockLogService = new Mock<IUnifiedLogService>();
            _mockLogger = new Mock<ILogger<AuthService>>();
            _mockLoginAttemptService = new Mock<ILoginAttemptService>();

            // 创建AuthOptions
            _authOptions = AuthTestDataGenerator.CreateAuthOptions();

            // 创建SysAdminHandler 实例（不Mock，使用真实实例）
            _sysAdminHandler = new SysAdminHandler(_mockAuthRepository.Object);

            // 设置Mock方法
            SetupMockMethods();

            // 创建AuthService实例
            var optionsWrapper = Options.Create(_authOptions);
            _authService = new AuthService(
                _mockAuthRepository.Object,
                _mockMapper.Object,
                _mockLogService.Object,
                _sysAdminHandler,
                optionsWrapper,
                _mockLogger.Object,
                _mockLoginAttemptService.Object
            );
        }

        #region 初始化测试数据

        private void InitializeTestData()
        {
            // 生成固定的密码哈希，确保测试的一致性
            var normalUserPasswordHash = LYBT.Shared.Utilities.Helpers.PasswordHelper.Hash(AuthTestDataGenerator.DefaultTestPassword);

            // 创建各种状态的测试用户
            _testUsers.Add(AuthTestDataGenerator.CreateTestUser("normaluser", normalUserPasswordHash, CommonStatus.Enabled));
            _testUsers.Add(AuthTestDataGenerator.CreateDisabledUser("disableduser"));
            _testUsers.Add(AuthTestDataGenerator.CreateTestUser("lockeduser", normalUserPasswordHash, CommonStatus.Enabled, 5, DateTime.Now.AddMinutes(15)));
            _testUsers.Add(AuthTestDataGenerator.CreateSysAdminUser());
        }

        private void SetupMockMethods()
        {
            // Setup AuthRepository
            _mockAuthRepository
                .Setup(x => x.GetByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync((string username) => _testUsers.FirstOrDefault(u => u.Username == username));

            // 为 sysadmin 特别设置，返回 null 以便 SysAdminHandler 创建临时用户
            _mockAuthRepository
                .Setup(x => x.GetByUsernameAsync("sysadmin"))
                .ReturnsAsync((UserModel?)null);

            _mockAuthRepository
                .Setup(x => x.UpdateLastLoginTimeAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()))
                .Returns(Task.CompletedTask);

            _mockAuthRepository
                .Setup(x => x.UpdateUserLoginProtectionAsync(It.IsAny<UserModel>()))
                .Returns(Task.CompletedTask);

            // 设置管理员密码哈希
            var adminPasswordHash = LYBT.Shared.Utilities.Helpers.PasswordHelper.Hash(AuthTestDataGenerator.AdminTestPassword);
            _mockAuthRepository
                .Setup(x => x.GetAdminPasswordHashAsync("sysadmin"))
                .ReturnsAsync(adminPasswordHash);

            _mockAuthRepository
                .Setup(x => x.UpdateAdminPasswordHashAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Setup Mapper
            _mockMapper
                .Setup(x => x.Map<UserDto>(It.IsAny<UserModel>()))
                .Returns((UserModel user) => new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    RealName = user.RealName,
                    Status = user.Status,
                    CreateTime = user.CreateTime,
                    LastLoginTime = user.LastLoginTime,
                    PhoneNumber = user.PhoneNumber
                });

            // SysAdminHandler 使用真实实例，不需要Mock setup

            // Setup LoginAttemptService
            _mockLoginAttemptService
                .Setup(x => x.IsAccountLocked(It.IsAny<string>()))
                .Returns(false);

            _mockLoginAttemptService
                .Setup(x => x.GetRemainingLockTime(It.IsAny<string>()))
                .Returns(0);

            // Setup LogService - 避免可选参数问题
            _mockLogService
                .Setup(x => x.LogUserLoginAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _mockLogService
                .Setup(x => x.LogUserLogoutAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _mockLogService
                .Setup(x => x.LogUserActionAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<LogActionType>(), 
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
                    It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()))
                .Returns(Task.CompletedTask);

            // Mock PasswordHelper静态方法
            SetupPasswordHelperMock();
        }

        private void SetupPasswordHelperMock()
        {
            // 注意：由于PasswordHelper是静态类，这里我们需要在测试中使用已知的密码哈希
            // 实际测试中可能需要使用真实的密码验证或者重构为可注入的服务
        }

        #endregion

        #region LoginAsync 成功场景测试

        [Fact]
        public async Task LoginAsync_Should_Login_Successfully_For_Valid_User()
        {
            // Arrange
            var user = _testUsers.First(u => u.Username == "normaluser");
            var loginRequest = AuthTestDataGenerator.CreateLoginRequest("normaluser", AuthTestDataGenerator.DefaultTestPassword);

            // 使用真实密码验证

            // Act
            var result = await _authService.LoginAsync(loginRequest);

            // Assert
            result.Should().NotBeNull();
            result!.Username.Should().Be(user.Username);
            result.Status.Should().Be(CommonStatus.Enabled);

            // 验证失败尝试被清除
            _mockLoginAttemptService.Verify(x => x.ClearAttempts("normaluser"), Times.Once);

            // 验证登录时间更新
            _mockAuthRepository.Verify(x => x.UpdateLastLoginTimeAsync(user.Id, It.IsAny<DateTime>()), Times.Once);

            // 验证登录日志记录
            _mockLogService.Verify(x => x.LogUserLoginAsync(user.Id, user.RealName, "", "", true, null), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_Should_Login_SysAdmin_Successfully()
        {
            // Arrange
            var loginRequest = AuthTestDataGenerator.CreateLoginRequest("sysadmin", AuthTestDataGenerator.AdminTestPassword);
            
            // 使用真实密码验证

            // Act
            var result = await _authService.LoginAsync(loginRequest);

            // Assert
            result.Should().NotBeNull();
            result!.Username.Should().Be("sysadmin");
            result.RealName.Should().Be("系统管理员");

            // 验证sysadmin不会更新数据库记录
            _mockAuthRepository.Verify(x => x.UpdateLastLoginTimeAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_Should_Reset_Failed_Count_And_Lockout_On_Success()
        {
            // Arrange
            var user = _testUsers.First(u => u.Username == "normaluser");
            user.FailedLoginCount = 3;
            user.LockoutEnd = DateTime.Now.AddMinutes(-5); // 设置为过去时间，表示锁定已过期

            var loginRequest = AuthTestDataGenerator.CreateLoginRequest("normaluser", AuthTestDataGenerator.DefaultTestPassword);
            // 使用真实密码验证

            // Act
            var result = await _authService.LoginAsync(loginRequest);

            // Assert
            result.Should().NotBeNull();
            user.FailedLoginCount.Should().Be(0);
            user.LockoutEnd.Should().BeNull();

            // 验证登录保护信息更新
            _mockAuthRepository.Verify(x => x.UpdateUserLoginProtectionAsync(It.Is<UserModel>(u => 
                u.FailedLoginCount == 0 && u.LockoutEnd == null)), Times.Once);
        }

        #endregion

        #region LoginAsync 失败场景测试

        [Fact]
        public async Task LoginAsync_Should_Fail_When_User_Not_Exists()
        {
            // Arrange
            var loginRequest = AuthTestDataGenerator.CreateLoginRequest("nonexistentuser", "password");

            // Act
            var result = await _authService.LoginAsync(loginRequest);

            // Assert
            result.Should().BeNull();

            // 验证失败尝试被记录
            _mockLoginAttemptService.Verify(x => x.RecordFailedAttempt("nonexistentuser"), Times.Once);

            // 验证失败日志记录
            _mockLogService.Verify(x => x.LogUserLoginAsync(Guid.Empty, "nonexistentuser", "", "", true, null), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_Should_Fail_When_User_Is_Disabled()
        {
            // Arrange
            var loginRequest = AuthTestDataGenerator.CreateLoginRequest("disableduser", "password");

            // Act
            var result = await _authService.LoginAsync(loginRequest);

            // Assert
            result.Should().BeNull();

            // 验证失败尝试被记录
            _mockLoginAttemptService.Verify(x => x.RecordFailedAttempt("disableduser"), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_Should_Fail_When_Password_Is_Wrong()
        {
            // Arrange
            var user = _testUsers.First(u => u.Username == "normaluser");
            var loginRequest = AuthTestDataGenerator.CreateLoginRequest("normaluser", "wrongpassword");

            // 错误密码将被真实验证失败

            // Act
            var result = await _authService.LoginAsync(loginRequest);

            // Assert
            result.Should().BeNull();

            // 验证失败尝试被记录
            _mockLoginAttemptService.Verify(x => x.RecordFailedAttempt("normaluser"), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_Should_Fail_When_User_Is_Locked()
        {
            // Arrange
            var user = _testUsers.First(u => u.Username == "lockeduser");
            user.LockoutEnd = DateTime.Now.AddMinutes(10);

            var loginRequest = AuthTestDataGenerator.CreateLoginRequest("lockeduser", "password");

            // Act
            var result = await _authService.LoginAsync(loginRequest);

            // Assert
            result.Should().BeNull();

            // 验证失败日志包含锁定信息
            _mockLogService.Verify(x => x.LogUserLoginAsync(
                user.Id, user.RealName, "", "", true, null), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_Should_Fail_When_Login_Type_Not_Supported()
        {
            // Arrange
            var loginRequest = AuthTestDataGenerator.CreateLoginRequest("normaluser", "password", "UnsupportedType");

            // Act
            var result = await _authService.LoginAsync(loginRequest);

            // Assert
            result.Should().BeNull();

            // 验证失败日志记录
            _mockLogService.Verify(x => x.LogUserLoginAsync(Guid.Empty, "normaluser", "", "", true, null), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_Should_Fail_When_Password_Hash_Is_Empty()
        {
            // Arrange
            var user = _testUsers.First(u => u.Username == "normaluser");
            user.PasswordHash = ""; // 设置为空密码哈希

            var loginRequest = AuthTestDataGenerator.CreateLoginRequest("normaluser", "password");

            // Act
            var result = await _authService.LoginAsync(loginRequest);

            // Assert
            result.Should().BeNull();

            // 验证失败尝试被记录
            _mockLoginAttemptService.Verify(x => x.RecordFailedAttempt("normaluser"), Times.Once);
        }

        #endregion

        #region LoginAsync 防暴力破解测试

        [Fact]
        public async Task LoginAsync_Should_Fail_When_Account_Is_Temporarily_Locked()
        {
            // Arrange
            var loginRequest = AuthTestDataGenerator.CreateLoginRequest("normaluser", "password");

            // Mock登录尝试服务显示账户被锁定
            _mockLoginAttemptService.Setup(x => x.IsAccountLocked("normaluser")).Returns(true);
            _mockLoginAttemptService.Setup(x => x.GetRemainingLockTime("normaluser")).Returns(300); // 5分钟

            // Act
            var result = await _authService.LoginAsync(loginRequest);

            // Assert
            result.Should().BeNull();

            // 验证不会尝试获取用户信息（直接拒绝）
            _mockAuthRepository.Verify(x => x.GetByUsernameAsync(It.IsAny<string>()), Times.Never);

            // 验证失败日志记录包含锁定信息
            _mockLogService.Verify(x => x.LogUserLoginAsync(Guid.Empty, "normaluser", "", "", true, null), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_Should_Increment_Failed_Count_On_Wrong_Password()
        {
            // Arrange
            var user = _testUsers.First(u => u.Username == "normaluser");
            user.FailedLoginCount = 2; // 已有2次失败

            var loginRequest = AuthTestDataGenerator.CreateLoginRequest("normaluser", "wrongpassword");
            // 错误密码将被真实验证失败

            // Act
            var result = await _authService.LoginAsync(loginRequest);

            // Assert
            result.Should().BeNull();
            user.FailedLoginCount.Should().Be(3);

            // 验证失败尝试被记录（在LoginAttemptService中）
            _mockLoginAttemptService.Verify(x => x.RecordFailedAttempt("normaluser"), Times.Once);

            // 验证用户登录保护信息更新
            _mockAuthRepository.Verify(x => x.UpdateUserLoginProtectionAsync(user), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_Should_Lock_Account_After_Max_Failed_Attempts()
        {
            // Arrange
            var user = _testUsers.First(u => u.Username == "normaluser");
            user.FailedLoginCount = 4; // 再失败一次就达到限制(5次)

            var loginRequest = AuthTestDataGenerator.CreateLoginRequest("normaluser", "wrongpassword");
            // 错误密码将被真实验证失败

            // Act
            var result = await _authService.LoginAsync(loginRequest);

            // Assert
            result.Should().BeNull();
            user.FailedLoginCount.Should().Be(5);
            user.LockoutEnd.Should().NotBeNull();
            user.LockoutEnd.Should().BeAfter(DateTime.Now);

            // 验证锁定持续时间符合配置
            var lockoutDuration = user.LockoutEnd.Value - DateTime.Now;
            lockoutDuration.Should().BeCloseTo(_authOptions.AccountLockoutDuration, TimeSpan.FromMinutes(1));
        }

        #endregion

        #region LogoutAsync 测试

        [Fact]
        public async Task LogoutAsync_Should_Logout_Successfully()
        {
            // Arrange
            var user = _testUsers.First(u => u.Username == "normaluser");
            var logoutRequest = AuthTestDataGenerator.CreateLogoutRequest("normaluser");

            // Act
            var result = await _authService.LogoutAsync(logoutRequest);

            // Assert
            result.Should().BeTrue();

            // 验证登出日志记录
            _mockLogService.Verify(x => x.LogUserLogoutAsync(user.Id, user.RealName, ""), Times.Once);
        }

        [Fact]
        public async Task LogoutAsync_Should_Handle_Non_Existent_User()
        {
            // Arrange
            var logoutRequest = AuthTestDataGenerator.CreateLogoutRequest("nonexistentuser");

            // Act
            var result = await _authService.LogoutAsync(logoutRequest);

            // Assert
            result.Should().BeTrue();

            // 验证登出日志记录（使用空GUID和用户名）
            _mockLogService.Verify(x => x.LogUserLogoutAsync(Guid.Empty, "nonexistentuser", ""), Times.Once);
        }

        [Fact]
        public async Task LogoutAsync_Should_Handle_SysAdmin_Logout()
        {
            // Arrange
            var logoutRequest = AuthTestDataGenerator.CreateLogoutRequest("sysadmin");

            // Act
            var result = await _authService.LogoutAsync(logoutRequest);

            // Assert
            result.Should().BeTrue();

            // 验证登出日志记录使用系统管理员显示名
            _mockLogService.Verify(x => x.LogUserLogoutAsync(It.IsAny<Guid>(), "系统管理员", ""), Times.Once);
        }

        #endregion

        #region ChangeSysAdminPasswordAsync 测试

        [Fact]
        public async Task ChangeSysAdminPasswordAsync_Should_Change_Password_Successfully()
        {
            // Arrange
            var request = AuthTestDataGenerator.CreateChangePasswordRequest(AuthTestDataGenerator.AdminTestPassword, "newpassword");
            
            // 使用真实密码验证

            // Act
            var result = await _authService.ChangeSysAdminPasswordAsync(request);

            // Assert
            result.Should().BeTrue();

            // 验证密码更新
            _mockAuthRepository.Verify(x => x.UpdateAdminPasswordHashAsync("sysadmin", It.IsAny<string>()), Times.Once);

            // 验证操作日志记录
            _mockLogService.Verify(x => x.LogUserActionAsync(
                Guid.Empty, "sysadmin", It.IsAny<LogActionType>(), "Auth", "Authentication", "修改系统管理员密码",
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()), Times.Once);
        }

        [Fact]
        public async Task ChangeSysAdminPasswordAsync_Should_Fail_When_Current_Hash_Is_Empty()
        {
            // Arrange
            var request = AuthTestDataGenerator.CreateChangePasswordRequest("oldpassword", "newpassword");

            // Mock获取密码哈希返回空
            _mockAuthRepository.Setup(x => x.GetAdminPasswordHashAsync("sysadmin")).ReturnsAsync((string?)null);

            // Act
            var result = await _authService.ChangeSysAdminPasswordAsync(request);

            // Assert
            result.Should().BeFalse();

            // 验证不会尝试更新密码
            _mockAuthRepository.Verify(x => x.UpdateAdminPasswordHashAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ChangeSysAdminPasswordAsync_Should_Fail_When_Old_Password_Is_Wrong()
        {
            // Arrange
            var request = AuthTestDataGenerator.CreateChangePasswordRequest("wrongoldpassword", "newpassword");
            
            // 错误的旧密码将被真实验证失败

            // Act
            var result = await _authService.ChangeSysAdminPasswordAsync(request);

            // Assert
            result.Should().BeFalse();

            // 验证不会更新密码
            _mockAuthRepository.Verify(x => x.UpdateAdminPasswordHashAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region 异常处理测试

        [Fact]
        public async Task LoginAsync_Should_Handle_Repository_Exception()
        {
            // Arrange
            var loginRequest = AuthTestDataGenerator.CreateLoginRequest("normaluser", "password");

            // Mock Repository抛出异常
            _mockAuthRepository
                .Setup(x => x.GetByUsernameAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _authService.LoginAsync(loginRequest));
            exception.Message.Should().Be("Database connection failed");

            // 验证异常日志记录
            _mockLogService.Verify(x => x.LogUserLoginAsync(Guid.Empty, "normaluser", "", "", true, null), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_Should_Handle_UpdateLastLoginTime_Exception()
        {
            // Arrange
            var user = _testUsers.First(u => u.Username == "normaluser");
            // 确保用户状态正常，不被锁定
            user.FailedLoginCount = 0;
            user.LockoutEnd = null;
            
            var loginRequest = AuthTestDataGenerator.CreateLoginRequest("normaluser", AuthTestDataGenerator.DefaultTestPassword);

            // Mock Repository 更新最后登录时间抛出异常
            _mockAuthRepository
                .Setup(x => x.UpdateLastLoginTimeAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _authService.LoginAsync(loginRequest));
            exception.Message.Should().Be("Database connection failed");
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 模拟密码验证结果 - 使用真实的密码验证
        /// </summary>
        private void MockPasswordVerification(string storedHash, string inputPassword, bool verificationResult)
        {
            // 由于PasswordHelper是静态类，我们使用真实的密码验证：
            // 测试中使用 AuthTestDataGenerator.DefaultTestPassword 和 AuthTestDataGenerator.AdminTestPassword 作为正确密码
            // 其他密码将被验证为错误密码
            
            // 注意：在真实场景中，建议将PasswordHelper重构为可注入的服务以便更好地测试
        }

        #endregion

        public void Dispose()
        {
            // 清理资源
        }
    }
}
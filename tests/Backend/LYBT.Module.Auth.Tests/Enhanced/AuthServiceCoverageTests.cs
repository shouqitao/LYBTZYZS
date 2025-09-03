using System;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using FluentAssertions;
using Moq;
using Xunit;
using LYBT.Infrastructure.Options;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Infrastructure.Logging;
using LYBT.Entities.Users;
using LYBT.Module.Auth.Services;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Helpers;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Helpers;

namespace LYBT.Module.Auth.Tests.Enhanced
{
    /// <summary>
    /// AuthService 增强测试类 - 专注于代码覆盖率提升
    /// 目标：覆盖重构后的 AuthService 核心认证逻辑
    /// </summary>
    public class AuthServiceCoverageTests : IDisposable
    {
        private readonly AuthService _authService;
        private readonly Mock<IAuthRepository> _mockAuthRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IUnifiedLogService> _mockLogService;
        private readonly Mock<ILoginAttemptService> _mockLoginAttemptService;
        private readonly Mock<AuthValidationHelper> _mockValidationHelper;
        private readonly Mock<AuthSessionHelper> _mockSessionHelper;
        private readonly Mock<AuthLoggingHelper> _mockLoggingHelper;
        private readonly AuthOptions _authOptions;
        private readonly SysAdminHandler _sysAdminHandler;

        public AuthServiceCoverageTests()
        {
            // 创建Mock服务
            _mockAuthRepository = new Mock<IAuthRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockLogService = new Mock<IUnifiedLogService>();
            _mockLoginAttemptService = new Mock<ILoginAttemptService>();
            _mockValidationHelper = new Mock<AuthValidationHelper>();
            _mockSessionHelper = new Mock<AuthSessionHelper>();
            _mockLoggingHelper = new Mock<AuthLoggingHelper>();

            // 创建AuthOptions
            _authOptions = new AuthOptions
            {
                MaxFailedAttempts = 5,
                AccountLockoutDuration = TimeSpan.FromMinutes(15),
                RequiredPasswordStrength = PasswordStrength.Medium,
                SessionTimeout = TimeSpan.FromHours(8)
            };

            // 创建SysAdminHandler
            _sysAdminHandler = new SysAdminHandler(_mockAuthRepository.Object);

            // 配置Mock
            SetupMockServices();

            // 创建AuthService实例
            var optionsWrapper = Options.Create(_authOptions);
            _authService = new AuthService(
                _mockAuthRepository.Object,
                _mockMapper.Object,
                _mockLogService.Object,
                _sysAdminHandler,
                optionsWrapper,
                NullLogger<AuthService>.Instance,
                _mockLoginAttemptService.Object
            );
        }

        #region Mock配置

        private void SetupMockServices()
        {
            // 配置Repository Mock
            _mockAuthRepository.Setup(x => x.GetByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync((string username) => CreateTestUser(username));

            _mockAuthRepository.Setup(x => x.GetAdminPasswordHashAsync("sysadmin"))
                .ReturnsAsync(PasswordHelper.Hash("Admin@123456"));

            _mockAuthRepository.Setup(x => x.UpdateLastLoginTimeAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()))
                .Returns(Task.CompletedTask);

            _mockAuthRepository.Setup(x => x.UpdateUserLoginProtectionAsync(It.IsAny<UserModel>()))
                .Returns(Task.CompletedTask);

            _mockAuthRepository.Setup(x => x.UpdateAdminPasswordHashAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // 配置Mapper Mock
            _mockMapper.Setup(x => x.Map<UserDto>(It.IsAny<UserModel>()))
                .Returns((UserModel user) => new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    RealName = user.RealName,
                    Status = user.Status,
                    CreateTime = user.CreateTime,
                    LastLoginTime = user.LastLoginTime
                });

            // 配置LoginAttempt Mock
            _mockLoginAttemptService.Setup(x => x.IsAccountLocked(It.IsAny<string>()))
                .Returns(false);

            _mockLoginAttemptService.Setup(x => x.GetRemainingLockTime(It.IsAny<string>()))
                .Returns(0);

            _mockLoginAttemptService.Setup(x => x.RecordFailedAttempt(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _mockLoginAttemptService.Setup(x => x.ClearAttempts(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // 配置LogService Mock
            _mockLogService.Setup(x => x.LogUserLoginAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _mockLogService.Setup(x => x.LogUserLogoutAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _mockLogService.Setup(x => x.LogUserActionAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<LogActionType>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<long>()))
                .Returns(Task.CompletedTask);
        }

        private UserModel? CreateTestUser(string username)
        {
            return username switch
            {
                "testuser" => new UserModel
                {
                    Id = Guid.NewGuid(),
                    Username = "testuser",
                    RealName = "测试用户",
                    PasswordHash = PasswordHelper.Hash("Test@123456"),
                    Status = CommonStatus.Enabled,
                    FailedLoginCount = 0,
                    LockoutEnd = null,
                    CreateTime = DateTime.Now,
                    LastLoginTime = DateTime.Now.AddDays(-1)
                },
                "disableduser" => new UserModel
                {
                    Id = Guid.NewGuid(),
                    Username = "disableduser",
                    RealName = "禁用用户",
                    PasswordHash = PasswordHelper.Hash("Test@123456"),
                    Status = CommonStatus.Disabled,
                    FailedLoginCount = 0,
                    LockoutEnd = null,
                    CreateTime = DateTime.Now
                },
                "lockeduser" => new UserModel
                {
                    Id = Guid.NewGuid(),
                    Username = "lockeduser",
                    RealName = "锁定用户",
                    PasswordHash = PasswordHelper.Hash("Test@123456"),
                    Status = CommonStatus.Enabled,
                    FailedLoginCount = 5,
                    LockoutEnd = DateTime.Now.AddMinutes(10),
                    CreateTime = DateTime.Now
                },
                "sysadmin" => null, // sysadmin 由 SysAdminHandler 处理
                _ => null
            };
        }

        #endregion

        #region 登录成功场景测试

        [Fact]
        public async Task LoginAsync_ValidUser_ShouldLoginSuccessfully()
        {
            // Arrange
            var request = new LoginRequestDto
            {
                Username = "testuser",
                Password = "Test@123456",
                RememberMe = false,
                LoginType = "Password",
                ClientIp = "127.0.0.1"
            };

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            result.Should().NotBeNull();
            result!.Username.Should().Be("testuser");
            result.RealName.Should().Be("测试用户");

            // 验证调用
            _mockLoginAttemptService.Verify(x => x.ClearAttempts("testuser"), Times.Once);
            _mockAuthRepository.Verify(x => x.UpdateLastLoginTimeAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_SysAdmin_ShouldLoginSuccessfully()
        {
            // Arrange
            var request = new LoginRequestDto
            {
                Username = "sysadmin",
                Password = "Admin@123456",
                RememberMe = false,
                LoginType = "Password",
                ClientIp = "127.0.0.1"
            };

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            result.Should().NotBeNull();
            result!.Username.Should().Be("sysadmin");
            result.RealName.Should().Be("系统管理员");
        }

        [Fact]
        public async Task LoginAsync_RememberMe_ShouldHandleRememberMeFlag()
        {
            // Arrange
            var request = new LoginRequestDto
            {
                Username = "testuser",
                Password = "Test@123456",
                RememberMe = true,
                LoginType = "Password",
                ClientIp = "127.0.0.1"
            };

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            result.Should().NotBeNull();
            result!.Username.Should().Be("testuser");
        }

        #endregion

        #region 登录失败场景测试

        [Fact]
        public async Task LoginAsync_NonExistentUser_ShouldReturnNull()
        {
            // Arrange
            var request = new LoginRequestDto
            {
                Username = "nonexistent",
                Password = "anypassword",
                LoginType = "Password",
                ClientIp = "127.0.0.1"
            };

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            result.Should().BeNull();
            _mockLoginAttemptService.Verify(x => x.RecordFailedAttempt("nonexistent"), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_DisabledUser_ShouldReturnNull()
        {
            // Arrange
            var request = new LoginRequestDto
            {
                Username = "disableduser",
                Password = "Test@123456",
                LoginType = "Password",
                ClientIp = "127.0.0.1"
            };

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            result.Should().BeNull();
            _mockLoginAttemptService.Verify(x => x.RecordFailedAttempt("disableduser"), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_WrongPassword_ShouldReturnNull()
        {
            // Arrange
            var request = new LoginRequestDto
            {
                Username = "testuser",
                Password = "wrongpassword",
                LoginType = "Password",
                ClientIp = "127.0.0.1"
            };

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            result.Should().BeNull();
            _mockLoginAttemptService.Verify(x => x.RecordFailedAttempt("testuser"), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_LockedUser_ShouldReturnNull()
        {
            // Arrange
            var request = new LoginRequestDto
            {
                Username = "lockeduser",
                Password = "Test@123456",
                LoginType = "Password",
                ClientIp = "127.0.0.1"
            };

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task LoginAsync_AccountTemporarilyLocked_ShouldReturnNull()
        {
            // Arrange
            _mockLoginAttemptService.Setup(x => x.IsAccountLocked("testuser")).Returns(true);
            _mockLoginAttemptService.Setup(x => x.GetRemainingLockTime("testuser")).Returns(300);

            var request = new LoginRequestDto
            {
                Username = "testuser",
                Password = "Test@123456",
                LoginType = "Password",
                ClientIp = "127.0.0.1"
            };

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task LoginAsync_UnsupportedLoginType_ShouldReturnNull()
        {
            // Arrange
            var request = new LoginRequestDto
            {
                Username = "testuser",
                Password = "Test@123456",
                LoginType = "UnsupportedType",
                ClientIp = "127.0.0.1"
            };

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task LoginAsync_EmptyPasswordHash_ShouldReturnNull()
        {
            // Arrange
            _mockAuthRepository.Setup(x => x.GetByUsernameAsync("testuser"))
                .ReturnsAsync(new UserModel
                {
                    Id = Guid.NewGuid(),
                    Username = "testuser",
                    RealName = "测试用户",
                    PasswordHash = "", // 空密码哈希
                    Status = CommonStatus.Enabled
                });

            var request = new LoginRequestDto
            {
                Username = "testuser",
                Password = "Test@123456",
                LoginType = "Password",
                ClientIp = "127.0.0.1"
            };

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region 防暴力破解测试

        [Fact]
        public async Task LoginAsync_MultipleFailedAttempts_ShouldIncrementFailedCount()
        {
            // Arrange
            var user = CreateTestUser("testuser")!;
            user.FailedLoginCount = 2;

            _mockAuthRepository.Setup(x => x.GetByUsernameAsync("testuser"))
                .ReturnsAsync(user);

            var request = new LoginRequestDto
            {
                Username = "testuser",
                Password = "wrongpassword",
                LoginType = "Password",
                ClientIp = "127.0.0.1"
            };

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            result.Should().BeNull();
            user.FailedLoginCount.Should().Be(3);
            _mockAuthRepository.Verify(x => x.UpdateUserLoginProtectionAsync(user), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_MaxFailedAttempts_ShouldLockAccount()
        {
            // Arrange
            var user = CreateTestUser("testuser")!;
            user.FailedLoginCount = 4; // 下次失败将达到最大值5

            _mockAuthRepository.Setup(x => x.GetByUsernameAsync("testuser"))
                .ReturnsAsync(user);

            var request = new LoginRequestDto
            {
                Username = "testuser",
                Password = "wrongpassword",
                LoginType = "Password",
                ClientIp = "127.0.0.1"
            };

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            result.Should().BeNull();
            user.FailedLoginCount.Should().Be(5);
            user.LockoutEnd.Should().NotBeNull();
            user.LockoutEnd.Should().BeAfter(DateTime.Now);
        }

        [Fact]
        public async Task LoginAsync_SuccessfulLoginAfterFailures_ShouldResetFailedCount()
        {
            // Arrange
            var user = CreateTestUser("testuser")!;
            user.FailedLoginCount = 3;
            user.LockoutEnd = DateTime.Now.AddMinutes(-5); // 过期的锁定

            _mockAuthRepository.Setup(x => x.GetByUsernameAsync("testuser"))
                .ReturnsAsync(user);

            var request = new LoginRequestDto
            {
                Username = "testuser",
                Password = "Test@123456",
                LoginType = "Password",
                ClientIp = "127.0.0.1"
            };

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            result.Should().NotBeNull();
            user.FailedLoginCount.Should().Be(0);
            user.LockoutEnd.Should().BeNull();
        }

        #endregion

        #region 登出测试

        [Fact]
        public async Task LogoutAsync_ValidUser_ShouldLogoutSuccessfully()
        {
            // Arrange
            var request = new LogoutRequestDto
            {
                Username = "testuser",
                ClientIp = "127.0.0.1"
            };

            // Act
            var result = await _authService.LogoutAsync(request);

            // Assert
            result.Should().BeTrue();
            _mockLogService.Verify(x => x.LogUserLogoutAsync(
                It.IsAny<Guid>(), "测试用户", "127.0.0.1"), Times.Once);
        }

        [Fact]
        public async Task LogoutAsync_NonExistentUser_ShouldStillReturnTrue()
        {
            // Arrange
            var request = new LogoutRequestDto
            {
                Username = "nonexistent",
                ClientIp = "127.0.0.1"
            };

            // Act
            var result = await _authService.LogoutAsync(request);

            // Assert
            result.Should().BeTrue();
            _mockLogService.Verify(x => x.LogUserLogoutAsync(
                Guid.Empty, "nonexistent", "127.0.0.1"), Times.Once);
        }

        [Fact]
        public async Task LogoutAsync_SysAdmin_ShouldLogoutSuccessfully()
        {
            // Arrange
            var request = new LogoutRequestDto
            {
                Username = "sysadmin",
                ClientIp = "127.0.0.1"
            };

            // Act
            var result = await _authService.LogoutAsync(request);

            // Assert
            result.Should().BeTrue();
            _mockLogService.Verify(x => x.LogUserLogoutAsync(
                It.IsAny<Guid>(), "系统管理员", "127.0.0.1"), Times.Once);
        }

        #endregion

        #region 系统管理员密码管理测试

        [Fact]
        public async Task ChangeSysAdminPasswordAsync_ValidOldPassword_ShouldChangeSuccessfully()
        {
            // Arrange
            var request = new ChangePasswordRequestDto
            {
                OldPassword = "Admin@123456",
                NewPassword = "NewAdmin@123456"
            };

            // Act
            var result = await _authService.ChangeSysAdminPasswordAsync(request);

            // Assert
            result.Should().BeTrue();
            _mockAuthRepository.Verify(x => x.UpdateAdminPasswordHashAsync("sysadmin", It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ChangeSysAdminPasswordAsync_WrongOldPassword_ShouldReturnFalse()
        {
            // Arrange
            var request = new ChangePasswordRequestDto
            {
                OldPassword = "WrongPassword",
                NewPassword = "NewAdmin@123456"
            };

            // Act
            var result = await _authService.ChangeSysAdminPasswordAsync(request);

            // Assert
            result.Should().BeFalse();
            _mockAuthRepository.Verify(x => x.UpdateAdminPasswordHashAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ChangeSysAdminPasswordAsync_EmptyCurrentHash_ShouldReturnFalse()
        {
            // Arrange
            _mockAuthRepository.Setup(x => x.GetAdminPasswordHashAsync("sysadmin"))
                .ReturnsAsync((string?)null);

            var request = new ChangePasswordRequestDto
            {
                OldPassword = "Admin@123456",
                NewPassword = "NewAdmin@123456"
            };

            // Act
            var result = await _authService.ChangeSysAdminPasswordAsync(request);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region 异常处理测试

        [Fact]
        public async Task LoginAsync_RepositoryException_ShouldThrowException()
        {
            // Arrange
            _mockAuthRepository.Setup(x => x.GetByUsernameAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Database connection failed"));

            var request = new LoginRequestDto
            {
                Username = "testuser",
                Password = "Test@123456",
                LoginType = "Password",
                ClientIp = "127.0.0.1"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _authService.LoginAsync(request));
            exception.Message.Should().Be("Database connection failed");
        }

        [Fact]
        public async Task LoginAsync_UpdateLastLoginTimeException_ShouldThrowException()
        {
            // Arrange
            _mockAuthRepository.Setup(x => x.UpdateLastLoginTimeAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()))
                .ThrowsAsync(new Exception("Update failed"));

            var request = new LoginRequestDto
            {
                Username = "testuser",
                Password = "Test@123456",
                LoginType = "Password",
                ClientIp = "127.0.0.1"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _authService.LoginAsync(request));
            exception.Message.Should().Be("Update failed");
        }

        [Fact]
        public async Task LogoutAsync_LogServiceException_ShouldThrowException()
        {
            // Arrange
            _mockLogService.Setup(x => x.LogUserLogoutAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Log service failed"));

            var request = new LogoutRequestDto
            {
                Username = "testuser",
                ClientIp = "127.0.0.1"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _authService.LogoutAsync(request));
            exception.Message.Should().Be("Log service failed");
        }

        #endregion

        #region 边界条件测试

        [Fact]
        public async Task LoginAsync_NullRequest_ShouldThrowException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _authService.LoginAsync(null!));
        }

        [Fact]
        public async Task LogoutAsync_NullRequest_ShouldThrowException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _authService.LogoutAsync(null!));
        }

        [Fact]
        public async Task ChangeSysAdminPasswordAsync_NullRequest_ShouldThrowException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _authService.ChangeSysAdminPasswordAsync(null!));
        }

        [Fact]
        public async Task LoginAsync_EmptyUsername_ShouldReturnNull()
        {
            // Arrange
            var request = new LoginRequestDto
            {
                Username = "",
                Password = "Test@123456",
                LoginType = "Password",
                ClientIp = "127.0.0.1"
            };

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task LoginAsync_EmptyPassword_ShouldReturnNull()
        {
            // Arrange
            var request = new LoginRequestDto
            {
                Username = "testuser",
                Password = "",
                LoginType = "Password",
                ClientIp = "127.0.0.1"
            };

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        public void Dispose()
        {
            // 清理资源
        }
    }
}
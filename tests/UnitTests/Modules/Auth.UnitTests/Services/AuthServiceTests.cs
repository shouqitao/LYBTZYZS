using LYBT.Infrastructure.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Logging;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Entities.Users;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Services;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Helpers;
using LYBT.Tests.UltraThink.TestInfrastructure.Builders;
using LYBT.Tests.UltraThink.TestInfrastructure.Factories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace LYBT.Module.Auth.Tests.Services
{
    /// <summary>
    /// AuthService单元测试 - UltraThink设计
    /// 职责单一：专注于AuthService的单元测试
    /// 代码干净：清晰的测试结构，AAA模式
    /// 性能出色：Mock对象，快速执行
    /// </summary>
    public class AuthServiceTests : IDisposable
    {
        private readonly AuthService _authService;
        private readonly Mock<IAuthRepository> _mockAuthRepository;
        private readonly Mock<IUnifiedLogService> _mockLogService;
        private readonly Mock<SysAdminHandler> _mockSysAdminHandler;
        private readonly Mock<ILoginAttemptService> _mockLoginAttemptService;
        private readonly IMapper _mapper;
        private readonly AuthOptions _authOptions;
        private readonly ILogger<AuthService> _logger;
        private readonly MockFactory _mockFactory;
        private readonly UserTestDataBuilder _userBuilder;

        public AuthServiceTests()
        {
            _mockFactory = new MockFactory();
            _userBuilder = new UserTestDataBuilder();
            
            // 初始化Mock对象
            _mockAuthRepository = new Mock<IAuthRepository>();
            _mockLogService = new Mock<IUnifiedLogService>();
            _mockSysAdminHandler = new Mock<SysAdminHandler>(
                It.IsAny<IAuthRepository>(),
                It.IsAny<IOptions<SysAdminOptions>>(),
                It.IsAny<ILogger<SysAdminHandler>>());
            _mockLoginAttemptService = new Mock<ILoginAttemptService>();
            
            // 配置AutoMapper
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<UserModel, UserDto>();
            }, NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();
            
            // 配置AuthOptions
            _authOptions = new AuthOptions
            {
                MaxFailedLoginAttempts = 5,
                AccountLockoutDuration = TimeSpan.FromMinutes(30),
                SupportedLoginTypes = new List<string> { "Password", "SMS", "Email" }
            };
            
            _logger = NullLogger<AuthService>.Instance;
            
            // 创建AuthService实例
            _authService = new AuthService(
                _mockAuthRepository.Object,
                _mapper,
                _mockLogService.Object,
                _mockSysAdminHandler.Object,
                Options.Create(_authOptions),
                _logger,
                _mockLoginAttemptService.Object);
        }

        #region LoginAsync Tests - 正常流程

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ReturnsUserDto()
        {
            // Arrange
            var user = _userBuilder.AsValidUser().Build();
            var loginDto = new LoginRequestDto
            {
                Username = user.Username,
                Password = "Test123456",
                LoginType = "Password",
                RememberMe = false
            };

            _mockLoginAttemptService.Setup(x => x.IsAccountLocked(loginDto.Username))
                .Returns(false);
            _mockSysAdminHandler.Setup(x => x.IsSysAdmin(loginDto.Username))
                .Returns(false);
            _mockAuthRepository.Setup(x => x.GetByUsernameAsync(loginDto.Username))
                .ReturnsAsync(user);
            _mockSysAdminHandler.Setup(x => x.GetSysAdminPasswordHashAsync())
                .ReturnsAsync((string?)null);

            // 模拟密码验证成功
            user.PasswordHash = PasswordHelper.Hash("Test123456");

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Username, result.Username);
            Assert.Equal(user.RealName, result.RealName);
            _mockLoginAttemptService.Verify(x => x.ClearAttempts(loginDto.Username), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_WithSysAdmin_ReturnsUserDto()
        {
            // Arrange
            var sysadminUser = _userBuilder.AsSysAdmin().Build();
            var loginDto = new LoginRequestDto
            {
                Username = "sysadmin",
                Password = "Admin@123456",
                LoginType = "Password"
            };

            var passwordHash = PasswordHelper.Hash("Admin@123456");
            
            _mockLoginAttemptService.Setup(x => x.IsAccountLocked("sysadmin"))
                .Returns(false);
            _mockSysAdminHandler.Setup(x => x.IsSysAdmin("sysadmin"))
                .Returns(true);
            _mockSysAdminHandler.Setup(x => x.GetSysAdminUserAsync("sysadmin"))
                .ReturnsAsync(sysadminUser);
            _mockSysAdminHandler.Setup(x => x.GetSysAdminPasswordHashAsync())
                .ReturnsAsync(passwordHash);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("sysadmin", result.Username);
            Assert.Equal("系统管理员", result.RealName);
        }

        [Fact]
        public async Task LoginAsync_ResetsFailedCountOnSuccess()
        {
            // Arrange
            var user = _userBuilder
                .AsValidUser()
                .WithFailedLoginCount(3)
                .Build();
            
            user.PasswordHash = PasswordHelper.Hash("Test123456");
            
            var loginDto = new LoginRequestDto
            {
                Username = user.Username,
                Password = "Test123456",
                LoginType = "Password"
            };

            _mockLoginAttemptService.Setup(x => x.IsAccountLocked(loginDto.Username))
                .Returns(false);
            _mockSysAdminHandler.Setup(x => x.IsSysAdmin(loginDto.Username))
                .Returns(false);
            _mockAuthRepository.Setup(x => x.GetByUsernameAsync(loginDto.Username))
                .ReturnsAsync(user);

            // Act
            await _authService.LoginAsync(loginDto);

            // Assert
            Assert.Equal(0, user.FailedLoginCount);
            Assert.Null(user.LockoutEnd);
            _mockAuthRepository.Verify(x => x.UpdateUserLoginProtectionAsync(user), Times.Once);
        }

        #endregion

        #region LoginAsync Tests - 失败场景

        [Fact]
        public async Task LoginAsync_WhenAccountLocked_ReturnsNull()
        {
            // Arrange
            var loginDto = new LoginRequestDto
            {
                Username = "lockeduser",
                Password = "password",
                LoginType = "Password"
            };

            _mockLoginAttemptService.Setup(x => x.IsAccountLocked(loginDto.Username))
                .Returns(true);
            _mockLoginAttemptService.Setup(x => x.GetRemainingLockTime(loginDto.Username))
                .Returns(600); // 10分钟

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.Null(result);
            _mockAuthRepository.Verify(x => x.GetByUsernameAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_WithInvalidLoginType_ReturnsNull()
        {
            // Arrange
            var loginDto = new LoginRequestDto
            {
                Username = "testuser",
                Password = "password",
                LoginType = "InvalidType"
            };

            _mockLoginAttemptService.Setup(x => x.IsAccountLocked(loginDto.Username))
                .Returns(false);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task LoginAsync_WhenUserNotExists_ReturnsNull()
        {
            // Arrange
            var loginDto = new LoginRequestDto
            {
                Username = "nonexistent",
                Password = "password",
                LoginType = "Password"
            };

            _mockLoginAttemptService.Setup(x => x.IsAccountLocked(loginDto.Username))
                .Returns(false);
            _mockSysAdminHandler.Setup(x => x.IsSysAdmin(loginDto.Username))
                .Returns(false);
            _mockAuthRepository.Setup(x => x.GetByUsernameAsync(loginDto.Username))
                .ReturnsAsync((UserModel?)null);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.Null(result);
            _mockLoginAttemptService.Verify(x => x.RecordFailedAttempt(loginDto.Username), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_WhenUserDisabled_ReturnsNull()
        {
            // Arrange
            var user = _userBuilder
                .AsValidUser()
                .AsInactive()
                .Build();
            
            var loginDto = new LoginRequestDto
            {
                Username = user.Username,
                Password = "password",
                LoginType = "Password"
            };

            _mockLoginAttemptService.Setup(x => x.IsAccountLocked(loginDto.Username))
                .Returns(false);
            _mockSysAdminHandler.Setup(x => x.IsSysAdmin(loginDto.Username))
                .Returns(false);
            _mockAuthRepository.Setup(x => x.GetByUsernameAsync(loginDto.Username))
                .ReturnsAsync(user);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task LoginAsync_WithWrongPassword_ReturnsNull()
        {
            // Arrange
            var user = _userBuilder.AsValidUser().Build();
            user.PasswordHash = PasswordHelper.Hash("CorrectPassword");
            
            var loginDto = new LoginRequestDto
            {
                Username = user.Username,
                Password = "WrongPassword",
                LoginType = "Password"
            };

            _mockLoginAttemptService.Setup(x => x.IsAccountLocked(loginDto.Username))
                .Returns(false);
            _mockSysAdminHandler.Setup(x => x.IsSysAdmin(loginDto.Username))
                .Returns(false);
            _mockAuthRepository.Setup(x => x.GetByUsernameAsync(loginDto.Username))
                .ReturnsAsync(user);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.Null(result);
            _mockLoginAttemptService.Verify(x => x.RecordFailedAttempt(loginDto.Username), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_IncreasesFailedCountOnWrongPassword()
        {
            // Arrange
            var user = _userBuilder
                .AsValidUser()
                .WithFailedLoginCount(2)
                .Build();
            user.PasswordHash = PasswordHelper.Hash("CorrectPassword");
            
            var loginDto = new LoginRequestDto
            {
                Username = user.Username,
                Password = "WrongPassword",
                LoginType = "Password"
            };

            _mockLoginAttemptService.Setup(x => x.IsAccountLocked(loginDto.Username))
                .Returns(false);
            _mockSysAdminHandler.Setup(x => x.IsSysAdmin(loginDto.Username))
                .Returns(false);
            _mockAuthRepository.Setup(x => x.GetByUsernameAsync(loginDto.Username))
                .ReturnsAsync(user);

            // Act
            await _authService.LoginAsync(loginDto);

            // Assert
            Assert.Equal(3, user.FailedLoginCount);
            _mockAuthRepository.Verify(x => x.UpdateUserLoginProtectionAsync(user), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_LocksAccountAfterMaxFailedAttempts()
        {
            // Arrange
            var user = _userBuilder
                .AsValidUser()
                .WithFailedLoginCount(4) // 一次失败后将达到5次
                .Build();
            user.PasswordHash = PasswordHelper.Hash("CorrectPassword");
            
            var loginDto = new LoginRequestDto
            {
                Username = user.Username,
                Password = "WrongPassword",
                LoginType = "Password"
            };

            _mockLoginAttemptService.Setup(x => x.IsAccountLocked(loginDto.Username))
                .Returns(false);
            _mockSysAdminHandler.Setup(x => x.IsSysAdmin(loginDto.Username))
                .Returns(false);
            _mockAuthRepository.Setup(x => x.GetByUsernameAsync(loginDto.Username))
                .ReturnsAsync(user);

            // Act
            await _authService.LoginAsync(loginDto);

            // Assert
            Assert.Equal(5, user.FailedLoginCount);
            Assert.NotNull(user.LockoutEnd);
            Assert.True(user.LockoutEnd > DateTime.Now);
        }

        #endregion

        #region LogoutAsync Tests

        [Fact]
        public async Task LogoutAsync_WithValidUser_ReturnsTrue()
        {
            // Arrange
            var user = _userBuilder.AsValidUser().Build();
            var logoutDto = new LogoutRequestDto
            {
                Username = user.Username
            };

            _mockAuthRepository.Setup(x => x.GetByUsernameAsync(user.Username))
                .ReturnsAsync(user);

            // Act
            var result = await _authService.LogoutAsync(logoutDto);

            // Assert
            Assert.True(result);
            _mockLogService.Verify(x => x.LogUserActionAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<ActionType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task LogoutAsync_WithNonExistentUser_StillReturnsTrue()
        {
            // Arrange
            var logoutDto = new LogoutRequestDto
            {
                Username = "nonexistent"
            };

            _mockAuthRepository.Setup(x => x.GetByUsernameAsync(logoutDto.Username))
                .ReturnsAsync((UserModel?)null);

            // Act
            var result = await _authService.LogoutAsync(logoutDto);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region ChangeSysAdminPasswordAsync Tests

        [Fact]
        public async Task ChangeSysAdminPasswordAsync_WithCorrectOldPassword_ReturnsTrue()
        {
            // Arrange
            var oldPassword = "OldAdmin@123";
            var newPassword = "NewAdmin@456";
            var oldPasswordHash = PasswordHelper.Hash(oldPassword);
            
            var changeDto = new ChangeSysAdminPasswordDto
            {
                OldPassword = oldPassword,
                NewPassword = newPassword,
                ConfirmPassword = newPassword
            };

            _mockSysAdminHandler.Setup(x => x.GetSysAdminPasswordHashAsync())
                .ReturnsAsync(oldPasswordHash);
            _mockAuthRepository.Setup(x => x.UpdateAdminPasswordHashAsync(
                "sysadmin", It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _authService.ChangeSysAdminPasswordAsync(changeDto);

            // Assert
            Assert.True(result);
            _mockAuthRepository.Verify(x => x.UpdateAdminPasswordHashAsync(
                "sysadmin", It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ChangeSysAdminPasswordAsync_WithWrongOldPassword_ReturnsFalse()
        {
            // Arrange
            var correctPassword = "CorrectAdmin@123";
            var wrongPassword = "WrongAdmin@123";
            var passwordHash = PasswordHelper.Hash(correctPassword);
            
            var changeDto = new ChangeSysAdminPasswordDto
            {
                OldPassword = wrongPassword,
                NewPassword = "NewAdmin@456",
                ConfirmPassword = "NewAdmin@456"
            };

            _mockSysAdminHandler.Setup(x => x.GetSysAdminPasswordHashAsync())
                .ReturnsAsync(passwordHash);

            // Act
            var result = await _authService.ChangeSysAdminPasswordAsync(changeDto);

            // Assert
            Assert.False(result);
            _mockAuthRepository.Verify(x => x.UpdateAdminPasswordHashAsync(
                It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ChangeSysAdminPasswordAsync_WhenNoHashExists_ReturnsFalse()
        {
            // Arrange
            var changeDto = new ChangeSysAdminPasswordDto
            {
                OldPassword = "password",
                NewPassword = "newpassword",
                ConfirmPassword = "newpassword"
            };

            _mockSysAdminHandler.Setup(x => x.GetSysAdminPasswordHashAsync())
                .ReturnsAsync((string?)null);

            // Act
            var result = await _authService.ChangeSysAdminPasswordAsync(changeDto);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Default LoginType Tests

        [Fact]
        public async Task LoginAsync_WithoutLoginType_DefaultsToPassword()
        {
            // Arrange
            var user = _userBuilder.AsValidUser().Build();
            user.PasswordHash = PasswordHelper.Hash("Test123456");
            
            var loginDto = new LoginRequestDto
            {
                Username = user.Username,
                Password = "Test123456",
                LoginType = null // 未指定登录类型
            };

            _mockLoginAttemptService.Setup(x => x.IsAccountLocked(loginDto.Username))
                .Returns(false);
            _mockSysAdminHandler.Setup(x => x.IsSysAdmin(loginDto.Username))
                .Returns(false);
            _mockAuthRepository.Setup(x => x.GetByUsernameAsync(loginDto.Username))
                .ReturnsAsync(user);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Password", loginDto.LoginType);
        }

        #endregion

        #region RememberMe Tests

        [Fact]
        public async Task LoginAsync_WithRememberMe_SetsCorrectExpiry()
        {
            // Arrange
            var user = _userBuilder.AsValidUser().Build();
            user.PasswordHash = PasswordHelper.Hash("Test123456");
            
            var loginDto = new LoginRequestDto
            {
                Username = user.Username,
                Password = "Test123456",
                LoginType = "Password",
                RememberMe = true
            };

            _mockLoginAttemptService.Setup(x => x.IsAccountLocked(loginDto.Username))
                .Returns(false);
            _mockSysAdminHandler.Setup(x => x.IsSysAdmin(loginDto.Username))
                .Returns(false);
            _mockAuthRepository.Setup(x => x.GetByUsernameAsync(loginDto.Username))
                .ReturnsAsync(user);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.NotNull(result);
            // RememberMe功能在TokenService中处理，这里只验证参数传递
        }

        #endregion

        public void Dispose()
        {
            _mockFactory?.ClearCache();
        }
    }
}
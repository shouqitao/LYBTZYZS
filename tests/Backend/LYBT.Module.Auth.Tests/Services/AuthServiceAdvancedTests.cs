using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Logging;
using LYBT.Infrastructure.Options;
using LYBT.Models.Users;
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
    /// AuthService高级测试用例 - UltraThink设计
    /// 包含并发测试、性能测试、复杂场景等
    /// </summary>
    public class AuthServiceAdvancedTests : IDisposable
    {
        private readonly AuthService _authService;
        private readonly Mock<IAuthRepository> _mockAuthRepository;
        private readonly Mock<IUnifiedLogService> _mockLogService;
        private readonly Mock<SysAdminHandler> _mockSysAdminHandler;
        private readonly Mock<ILoginAttemptService> _mockLoginAttemptService;
        private readonly IMapper _mapper;
        private readonly AuthOptions _authOptions;
        private readonly MockFactory _mockFactory;
        private readonly UserTestDataBuilder _userBuilder;

        public AuthServiceAdvancedTests()
        {
            _mockFactory = new MockFactory();
            _userBuilder = new UserTestDataBuilder();
            
            _mockAuthRepository = new Mock<IAuthRepository>();
            _mockLogService = new Mock<IUnifiedLogService>();
            _mockSysAdminHandler = new Mock<SysAdminHandler>(
                It.IsAny<IAuthRepository>(),
                It.IsAny<IOptions<SysAdminOptions>>(),
                It.IsAny<ILogger<SysAdminHandler>>());
            _mockLoginAttemptService = new Mock<ILoginAttemptService>();
            
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<UserModel, UserDto>();
            }, NullLoggerFactory.Instance);
            _mapper = config.CreateMapper();
            
            _authOptions = new AuthOptions
            {
                MaxFailedLoginAttempts = 5,
                AccountLockoutDuration = TimeSpan.FromMinutes(30),
                SupportedLoginTypes = new List<string> { "Password", "SMS", "Email" }
            };
            
            _authService = new AuthService(
                _mockAuthRepository.Object,
                _mapper,
                _mockLogService.Object,
                _mockSysAdminHandler.Object,
                Options.Create(_authOptions),
                NullLogger<AuthService>.Instance,
                _mockLoginAttemptService.Object);
        }

        #region Concurrent Login Tests

        [Fact]
        public async Task LoginAsync_ConcurrentLoginAttempts_HandlesCorrectly()
        {
            // Arrange
            var user = _userBuilder.AsValidUser().Build();
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

            // Act - 模拟10个并发登录
            var tasks = new List<Task<UserDto?>>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(_authService.LoginAsync(loginDto));
            }
            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.All(results, r => Assert.NotNull(r));
            _mockLoginAttemptService.Verify(x => x.ClearAttempts(loginDto.Username), Times.Exactly(10));
        }

        [Fact]
        public async Task LoginAsync_ConcurrentFailedAttempts_HandlesLockoutCorrectly()
        {
            // Arrange
            var user = _userBuilder
                .AsValidUser()
                .WithFailedLoginCount(0)
                .Build();
            user.PasswordHash = PasswordHelper.Hash("CorrectPassword");
            
            var loginDto = new LoginRequestDto
            {
                Username = user.Username,
                Password = "WrongPassword",
                LoginType = "Password"
            };

            var callCount = 0;
            _mockLoginAttemptService.Setup(x => x.IsAccountLocked(loginDto.Username))
                .Returns(false);
            _mockSysAdminHandler.Setup(x => x.IsSysAdmin(loginDto.Username))
                .Returns(false);
            _mockAuthRepository.Setup(x => x.GetByUsernameAsync(loginDto.Username))
                .ReturnsAsync(() =>
                {
                    // 模拟每次调用后失败次数递增
                    var currentUser = _userBuilder
                        .AsValidUser()
                        .WithUsername(user.Username)
                        .WithFailedLoginCount(callCount++)
                        .Build();
                    currentUser.PasswordHash = user.PasswordHash;
                    return currentUser;
                });

            // Act - 模拟6个并发失败登录（超过5次限制）
            var tasks = new List<Task<UserDto?>>();
            for (int i = 0; i < 6; i++)
            {
                tasks.Add(_authService.LoginAsync(loginDto));
            }
            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.All(results, r => Assert.Null(r));
            _mockLoginAttemptService.Verify(x => x.RecordFailedAttempt(loginDto.Username), Times.Exactly(6));
        }

        #endregion

        #region Edge Case Tests

        [Fact]
        public async Task LoginAsync_WithSpecialCharactersInUsername_HandlesCorrectly()
        {
            // Arrange
            var specialUsername = "user@domain.com";
            var user = _userBuilder
                .AsValidUser()
                .WithUsername(specialUsername)
                .Build();
            user.PasswordHash = PasswordHelper.Hash("Test123456");
            
            var loginDto = new LoginRequestDto
            {
                Username = specialUsername,
                Password = "Test123456",
                LoginType = "Password"
            };

            _mockLoginAttemptService.Setup(x => x.IsAccountLocked(specialUsername))
                .Returns(false);
            _mockSysAdminHandler.Setup(x => x.IsSysAdmin(specialUsername))
                .Returns(false);
            _mockAuthRepository.Setup(x => x.GetByUsernameAsync(specialUsername))
                .ReturnsAsync(user);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(specialUsername, result.Username);
        }

        [Fact]
        public async Task LoginAsync_WithVeryLongPassword_HandlesCorrectly()
        {
            // Arrange
            var longPassword = new string('a', 500);
            var user = _userBuilder.AsValidUser().Build();
            user.PasswordHash = PasswordHelper.Hash(longPassword);
            
            var loginDto = new LoginRequestDto
            {
                Username = user.Username,
                Password = longPassword,
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
            Assert.NotNull(result);
        }

        [Fact]
        public async Task LoginAsync_WithUnicodeUsername_HandlesCorrectly()
        {
            // Arrange
            var unicodeUsername = "用户名🌟";
            var user = _userBuilder
                .AsValidUser()
                .WithUsername(unicodeUsername)
                .Build();
            user.PasswordHash = PasswordHelper.Hash("Test123456");
            
            var loginDto = new LoginRequestDto
            {
                Username = unicodeUsername,
                Password = "Test123456",
                LoginType = "Password"
            };

            _mockLoginAttemptService.Setup(x => x.IsAccountLocked(unicodeUsername))
                .Returns(false);
            _mockSysAdminHandler.Setup(x => x.IsSysAdmin(unicodeUsername))
                .Returns(false);
            _mockAuthRepository.Setup(x => x.GetByUsernameAsync(unicodeUsername))
                .ReturnsAsync(user);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(unicodeUsername, result.Username);
        }

        #endregion

        #region Lockout Boundary Tests

        [Fact]
        public async Task LoginAsync_ExactlyAtMaxFailedAttempts_LocksAccount()
        {
            // Arrange
            var user = _userBuilder
                .AsValidUser()
                .WithFailedLoginCount(4) // 下一次失败将达到限制
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
            Assert.True(user.LockoutEnd.Value > DateTime.Now);
            Assert.True(user.LockoutEnd.Value <= DateTime.Now.Add(_authOptions.AccountLockoutDuration).AddSeconds(1));
        }

        [Fact]
        public async Task LoginAsync_JustBeforeLockoutExpires_StillLocked()
        {
            // Arrange
            var user = _userBuilder
                .AsValidUser()
                .AsLockedOut(0) // 锁定但即将过期
                .Build();
            user.LockoutEnd = DateTime.Now.AddSeconds(5); // 5秒后过期
            
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
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.Null(result); // 仍然被锁定
        }

        [Fact]
        public async Task LoginAsync_AfterLockoutExpires_AllowsLogin()
        {
            // Arrange
            var user = _userBuilder
                .AsValidUser()
                .WithFailedLoginCount(5)
                .WithLockoutEnd(DateTime.Now.AddSeconds(-1)) // 已过期
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
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.NotNull(result); // 允许登录
            Assert.Equal(0, user.FailedLoginCount); // 重置失败次数
            Assert.Null(user.LockoutEnd); // 清除锁定时间
        }

        #endregion

        #region Multiple Login Types Tests

        [Fact]
        public async Task LoginAsync_WithSMSLoginType_ValidatesCorrectly()
        {
            // Arrange
            var user = _userBuilder.AsValidUser().Build();
            user.PasswordHash = PasswordHelper.Hash("123456"); // SMS验证码
            
            var loginDto = new LoginRequestDto
            {
                Username = user.Username,
                Password = "123456",
                LoginType = "SMS"
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
        }

        [Fact]
        public async Task LoginAsync_WithEmailLoginType_ValidatesCorrectly()
        {
            // Arrange
            var user = _userBuilder.AsValidUser().Build();
            user.PasswordHash = PasswordHelper.Hash("654321"); // Email验证码
            
            var loginDto = new LoginRequestDto
            {
                Username = user.Username,
                Password = "654321",
                LoginType = "Email"
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
        }

        #endregion

        #region SysAdmin Special Cases

        [Fact]
        public async Task LoginAsync_SysAdminCannotBeLocked()
        {
            // Arrange
            var sysadmin = _userBuilder.AsSysAdmin().Build();
            sysadmin.FailedLoginCount = 10; // 超过限制
            sysadmin.LockoutEnd = DateTime.Now.AddHours(1); // 已锁定
            
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
                .ReturnsAsync(sysadmin);
            _mockSysAdminHandler.Setup(x => x.GetSysAdminPasswordHashAsync())
                .ReturnsAsync(passwordHash);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.NotNull(result); // sysadmin可以登录
            _mockAuthRepository.Verify(x => x.UpdateUserLoginProtectionAsync(It.IsAny<UserModel>()), Times.Never);
        }

        [Fact]
        public async Task ChangeSysAdminPasswordAsync_UpdatesPasswordCorrectly()
        {
            // Arrange
            var oldPassword = "OldAdmin@123";
            var newPassword = "NewAdmin@456";
            var oldHash = PasswordHelper.Hash(oldPassword);
            
            var changeDto = new ChangeSysAdminPasswordDto
            {
                OldPassword = oldPassword,
                NewPassword = newPassword,
                ConfirmPassword = newPassword
            };

            _mockSysAdminHandler.Setup(x => x.GetSysAdminPasswordHashAsync())
                .ReturnsAsync(oldHash);

            string? capturedHash = null;
            _mockAuthRepository.Setup(x => x.UpdateAdminPasswordHashAsync("sysadmin", It.IsAny<string>()))
                .Callback<string, string>((username, hash) => capturedHash = hash)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _authService.ChangeSysAdminPasswordAsync(changeDto);

            // Assert
            Assert.True(result);
            Assert.NotNull(capturedHash);
            Assert.True(PasswordHelper.Verify(capturedHash, newPassword));
        }

        #endregion

        #region Performance Tests

        [Fact]
        public async Task LoginAsync_WithManyFailedAttempts_PerformsEfficiently()
        {
            // Arrange
            var user = _userBuilder
                .AsValidUser()
                .WithFailedLoginCount(1000) // 极高的失败次数
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
            var startTime = DateTime.UtcNow;
            var result = await _authService.LoginAsync(loginDto);
            var duration = DateTime.UtcNow - startTime;

            // Assert
            Assert.NotNull(result);
            Assert.True(duration.TotalMilliseconds < 100, "登录应在100ms内完成");
        }

        [Fact]
        public async Task LoginAsync_RapidSuccessiveAttempts_HandlesCorrectly()
        {
            // Arrange
            var user = _userBuilder.AsValidUser().Build();
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

            // Act - 快速连续100次登录
            for (int i = 0; i < 100; i++)
            {
                var result = await _authService.LoginAsync(loginDto);
                Assert.NotNull(result);
            }

            // Assert
            _mockLoginAttemptService.Verify(x => x.ClearAttempts(loginDto.Username), Times.Exactly(100));
        }

        #endregion

        #region Null and Empty Value Tests

        [Fact]
        public async Task LoginAsync_WithEmptyUsername_ReturnsNull()
        {
            // Arrange
            var loginDto = new LoginRequestDto
            {
                Username = "",
                Password = "password",
                LoginType = "Password"
            };

            _mockLoginAttemptService.Setup(x => x.IsAccountLocked(""))
                .Returns(false);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task LoginAsync_WithEmptyPassword_HandlesCorrectly()
        {
            // Arrange
            var user = _userBuilder.AsValidUser().Build();
            user.PasswordHash = PasswordHelper.Hash("actualpassword");
            
            var loginDto = new LoginRequestDto
            {
                Username = user.Username,
                Password = "", // 空密码
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
        public async Task LoginAsync_UserWithoutPasswordHash_ReturnsNull()
        {
            // Arrange
            var user = _userBuilder.AsValidUser().Build();
            user.PasswordHash = null; // 没有密码哈希
            
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

        #endregion

        public void Dispose()
        {
            _mockFactory?.ClearCache();
        }
    }
}
using LYBT.Infrastructure.Options;
using System;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Logging;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Entities.Users;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Services;
using LYBT.Shared.Models.Contracts.Auth;
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
    /// AuthService异常处理测试 - UltraThink设计
    /// 专注于异常场景、错误恢复、边界条件等
    /// </summary>
    public class AuthServiceExceptionTests : IDisposable
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

        public AuthServiceExceptionTests()
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
                SupportedLoginTypes = new System.Collections.Generic.List<string> { "Password", "SMS", "Email" }
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

        #region Repository Exception Tests

        [Fact]
        public async Task LoginAsync_WhenRepositoryThrows_PropagatesException()
        {
            // Arrange
            var loginDto = new LoginRequestDto
            {
                Username = "testuser",
                Password = "password",
                LoginType = "Password"
            };

            _mockLoginAttemptService.Setup(x => x.IsAccountLocked(loginDto.Username))
                .Returns(false);
            _mockSysAdminHandler.Setup(x => x.IsSysAdmin(loginDto.Username))
                .Returns(false);
            _mockAuthRepository.Setup(x => x.GetByUsernameAsync(loginDto.Username))
                .ThrowsAsync(new InvalidOperationException("Database connection failed"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _authService.LoginAsync(loginDto));
        }

        [Fact]
        public async Task LoginAsync_WhenUpdateLoginTimeThrows_LogsAndContinues()
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
            _mockAuthRepository.Setup(x => x.UpdateLastLoginTimeAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()))
                .ThrowsAsync(new Exception("Update failed"));

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.NotNull(result); // 登录仍然成功
        }

        [Fact]
        public async Task LogoutAsync_WhenRepositoryThrows_StillReturnsTrue()
        {
            // Arrange
            var logoutDto = new LogoutRequestDto
            {
                Username = "testuser"
            };

            _mockAuthRepository.Setup(x => x.GetByUsernameAsync(logoutDto.Username))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _authService.LogoutAsync(logoutDto);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ChangeSysAdminPasswordAsync_WhenUpdateThrows_PropagatesException()
        {
            // Arrange
            var oldPassword = "OldPassword";
            var oldHash = PasswordHelper.Hash(oldPassword);
            
            var changeDto = new ChangeSysAdminPasswordDto
            {
                OldPassword = oldPassword,
                NewPassword = "NewPassword",
                ConfirmPassword = "NewPassword"
            };

            _mockSysAdminHandler.Setup(x => x.GetSysAdminPasswordHashAsync())
                .ReturnsAsync(oldHash);
            _mockAuthRepository.Setup(x => x.UpdateAdminPasswordHashAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Update failed"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(
                async () => await _authService.ChangeSysAdminPasswordAsync(changeDto));
        }

        #endregion

        #region LoginAttemptService Exception Tests

        [Fact]
        public async Task LoginAsync_WhenIsAccountLockedThrows_PropagatesException()
        {
            // Arrange
            var loginDto = new LoginRequestDto
            {
                Username = "testuser",
                Password = "password",
                LoginType = "Password"
            };

            _mockLoginAttemptService.Setup(x => x.IsAccountLocked(loginDto.Username))
                .Throws(new InvalidOperationException("Service unavailable"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _authService.LoginAsync(loginDto));
        }

        [Fact]
        public async Task LoginAsync_WhenRecordFailedAttemptThrows_ContinuesFlow()
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
            _mockLoginAttemptService.Setup(x => x.RecordFailedAttempt(loginDto.Username))
                .Throws(new Exception("Failed to record"));
            _mockSysAdminHandler.Setup(x => x.IsSysAdmin(loginDto.Username))
                .Returns(false);
            _mockAuthRepository.Setup(x => x.GetByUsernameAsync(loginDto.Username))
                .ReturnsAsync(user);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.Null(result); // 登录失败
            Assert.Equal(1, user.FailedLoginCount); // 仍然增加失败次数
        }

        [Fact]
        public async Task LoginAsync_WhenClearAttemptsThrows_StillReturnsSuccess()
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
            _mockLoginAttemptService.Setup(x => x.ClearAttempts(loginDto.Username))
                .Throws(new Exception("Failed to clear"));
            _mockSysAdminHandler.Setup(x => x.IsSysAdmin(loginDto.Username))
                .Returns(false);
            _mockAuthRepository.Setup(x => x.GetByUsernameAsync(loginDto.Username))
                .ReturnsAsync(user);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.NotNull(result); // 登录成功
        }

        #endregion

        #region SysAdminHandler Exception Tests

        [Fact]
        public async Task LoginAsync_WhenGetSysAdminUserThrows_ReturnsNull()
        {
            // Arrange
            var loginDto = new LoginRequestDto
            {
                Username = "sysadmin",
                Password = "password",
                LoginType = "Password"
            };

            _mockLoginAttemptService.Setup(x => x.IsAccountLocked("sysadmin"))
                .Returns(false);
            _mockSysAdminHandler.Setup(x => x.IsSysAdmin("sysadmin"))
                .Returns(true);
            _mockSysAdminHandler.Setup(x => x.GetSysAdminUserAsync("sysadmin"))
                .ThrowsAsync(new Exception("Failed to get sysadmin"));

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task LoginAsync_WhenGetSysAdminPasswordHashThrows_ReturnsNull()
        {
            // Arrange
            var sysadmin = _userBuilder.AsSysAdmin().Build();
            var loginDto = new LoginRequestDto
            {
                Username = "sysadmin",
                Password = "password",
                LoginType = "Password"
            };

            _mockLoginAttemptService.Setup(x => x.IsAccountLocked("sysadmin"))
                .Returns(false);
            _mockSysAdminHandler.Setup(x => x.IsSysAdmin("sysadmin"))
                .Returns(true);
            _mockSysAdminHandler.Setup(x => x.GetSysAdminUserAsync("sysadmin"))
                .ReturnsAsync(sysadmin);
            _mockSysAdminHandler.Setup(x => x.GetSysAdminPasswordHashAsync())
                .ThrowsAsync(new Exception("Failed to get hash"));

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region Logging Exception Tests

        [Fact]
        public async Task LoginAsync_WhenLoggingFails_StillCompletesLogin()
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
            _mockLogService.Setup(x => x.LogUserActionAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<ActionType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
                .ThrowsAsync(new Exception("Logging failed"));

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.NotNull(result); // 登录成功，即使日志失败
        }

        [Fact]
        public async Task LogoutAsync_WhenLoggingFails_StillReturnsTrue()
        {
            // Arrange
            var user = _userBuilder.AsValidUser().Build();
            var logoutDto = new LogoutRequestDto
            {
                Username = user.Username
            };

            _mockAuthRepository.Setup(x => x.GetByUsernameAsync(user.Username))
                .ReturnsAsync(user);
            _mockLogService.Setup(x => x.LogUserActionAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<ActionType>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
                .ThrowsAsync(new Exception("Logging failed"));

            // Act
            var result = await _authService.LogoutAsync(logoutDto);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region Null Reference Tests

        [Fact]
        public async Task LoginAsync_WithNullDto_ThrowsNullReferenceException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<NullReferenceException>(
                async () => await _authService.LoginAsync(null!));
        }

        [Fact]
        public async Task LogoutAsync_WithNullDto_ThrowsNullReferenceException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<NullReferenceException>(
                async () => await _authService.LogoutAsync(null!));
        }

        [Fact]
        public async Task ChangeSysAdminPasswordAsync_WithNullDto_ThrowsNullReferenceException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<NullReferenceException>(
                async () => await _authService.ChangeSysAdminPasswordAsync(null!));
        }

        #endregion

        #region Timeout and Performance Tests

        [Fact]
        public async Task LoginAsync_WithSlowRepository_CompletesWithinTimeout()
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
            
            // 模拟慢速数据库查询
            _mockAuthRepository.Setup(x => x.GetByUsernameAsync(loginDto.Username))
                .ReturnsAsync(() =>
                {
                    Task.Delay(100).Wait(); // 100ms延迟
                    return user;
                });

            // Act
            var startTime = DateTime.UtcNow;
            var result = await _authService.LoginAsync(loginDto);
            var duration = DateTime.UtcNow - startTime;

            // Assert
            Assert.NotNull(result);
            Assert.True(duration.TotalSeconds < 1, "登录应在1秒内完成");
        }

        #endregion

        #region Data Integrity Tests

        [Fact]
        public async Task LoginAsync_WithCorruptedUserData_HandlesGracefully()
        {
            // Arrange
            var user = new UserModel
            {
                Id = Guid.NewGuid(),
                Username = "corrupteduser",
                RealName = null!, // 损坏的数据
                PasswordHash = PasswordHelper.Hash("password"),
                Status = CommonStatus.Enabled,
                CreateTime = default, // 损坏的时间
                UpdateTime = default
            };
            
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
            Assert.NotNull(result);
            Assert.Equal(user.Username, result.Username);
        }

        [Fact]
        public async Task LoginAsync_WithInvalidGuid_HandlesCorrectly()
        {
            // Arrange
            var user = _userBuilder.AsValidUser().Build();
            user.Id = Guid.Empty; // 无效的GUID
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
            Assert.NotNull(result);
            Assert.Equal(Guid.Empty, result.Id); // 保持空GUID
        }

        #endregion

        #region Boundary Value Tests

        [Fact]
        public async Task LoginAsync_WithMaxIntFailedCount_HandlesCorrectly()
        {
            // Arrange
            var user = _userBuilder
                .AsValidUser()
                .WithFailedLoginCount(int.MaxValue)
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
            Assert.NotNull(result);
            Assert.Equal(0, user.FailedLoginCount); // 重置为0
        }

        [Fact]
        public async Task LoginAsync_WithMaxDateTimeLockout_HandlesCorrectly()
        {
            // Arrange
            var user = _userBuilder
                .AsValidUser()
                .WithLockoutEnd(DateTime.MaxValue)
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
            Assert.Null(result); // 永远锁定
        }

        [Fact]
        public async Task LoginAsync_WithMinDateTimeLockout_AllowsLogin()
        {
            // Arrange
            var user = _userBuilder
                .AsValidUser()
                .WithLockoutEnd(DateTime.MinValue)
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
            Assert.NotNull(result); // 允许登录（锁定已过期）
        }

        #endregion

        public void Dispose()
        {
            _mockFactory?.ClearCache();
        }
    }
}
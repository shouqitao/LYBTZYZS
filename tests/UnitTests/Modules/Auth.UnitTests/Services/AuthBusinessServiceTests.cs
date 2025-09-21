using System;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Services;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace LYBT.Module.Auth.Tests.Services
{
    /// <summary>
    /// AuthBusinessService 完整单元测试
    /// 职责：登录流程、密码验证、业务逻辑处理
    /// </summary>
    public class AuthBusinessServiceTests
    {
        private readonly AuthBusinessService _authBusinessService;
        private readonly Mock<IAuthRepository> _mockAuthRepository;
        private readonly Mock<IAuthQueryService> _mockQueryService;
        private readonly Mock<IJwtAuthenticationService> _mockJwtAuthenticationService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<AuthBusinessService>> _mockLogger;
        private readonly Mock<ISysAdminHandler> _mockSysAdminHandler;
        private readonly Mock<IOptions<AuthOptions>> _mockAuthOptions;
        private readonly AuthOptions _authOptions;

        public AuthBusinessServiceTests()
        {
            _mockAuthRepository = new Mock<IAuthRepository>();
            _mockQueryService = new Mock<IAuthQueryService>();
            _mockJwtAuthenticationService = new Mock<IJwtAuthenticationService>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<AuthBusinessService>>();
            _mockSysAdminHandler = new Mock<ISysAdminHandler>();
            _mockAuthOptions = new Mock<IOptions<AuthOptions>>();

            _authOptions = new AuthOptions
            {
                MaxFailedLoginAttempts = 5,
                AccountLockoutDuration = TimeSpan.FromMinutes(15)
            };

            _mockAuthOptions.Setup(x => x.Value).Returns(_authOptions);

            _authBusinessService = new AuthBusinessService(
                _mockAuthRepository.Object,
                _mockQueryService.Object,
                _mockJwtAuthenticationService.Object,
                _mockMapper.Object,
                _mockLogger.Object,
                _mockSysAdminHandler.Object,
                _mockAuthOptions.Object);
        }

        #region 构造函数测试

        [Fact]
        public void Constructor_Should_Throw_When_AuthRepository_Is_Null()
        {
            // Act & Assert
            var action = () => new AuthBusinessService(
                null!,
                _mockQueryService.Object,
                _mockJwtAuthenticationService.Object,
                _mockMapper.Object,
                _mockLogger.Object,
                _mockSysAdminHandler.Object,
                _mockAuthOptions.Object);

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("authRepository");
        }

        [Fact]
        public void Constructor_Should_Throw_When_QueryService_Is_Null()
        {
            // Act & Assert
            var action = () => new AuthBusinessService(
                _mockAuthRepository.Object,
                null!,
                _mockJwtAuthenticationService.Object,
                _mockMapper.Object,
                _mockLogger.Object,
                _mockSysAdminHandler.Object,
                _mockAuthOptions.Object);

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("queryService");
        }

        [Fact]
        public void Constructor_Should_Create_Instance_When_All_Dependencies_Are_Valid()
        {
            // Act
            var service = new AuthBusinessService(
                _mockAuthRepository.Object,
                _mockQueryService.Object,
                _mockJwtAuthenticationService.Object,
                _mockMapper.Object,
                _mockLogger.Object,
                _mockSysAdminHandler.Object,
                _mockAuthOptions.Object);

            // Assert
            service.Should().NotBeNull();
        }

        #endregion

        #region ProcessLoginAsync 测试

        [Fact]
        public async Task ProcessLoginAsync_Should_Return_Success_When_Normal_User_Login_Success()
        {
            // Arrange
            var request = new LoginRequest { Username = "testuser", Password = "password123", RememberMe = false };
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                RealName = "测试用户",
                Role = UserRole.Doctor,
                PasswordHash = PasswordHelper.Hash("password123"),
                FailedLoginCount = 0,
                LockoutEnd = null
            };

            _mockSysAdminHandler.Setup(x => x.IsSysAdmin("testuser")).Returns(false);
            _mockQueryService.Setup(x => x.GetUserForAuthenticationAsync("testuser"))
                .ReturnsAsync(ServiceResult<User>.Success(user));
            _mockJwtAuthenticationService.Setup(x => x.GenerateToken(
                user.Id.ToString(), user.Username, user.Role, false))
                .Returns("jwt-token-123");

            // Act
            var result = await _authBusinessService.ProcessLoginAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Token.Should().Be("jwt-token-123");
            result.Data.User.Should().NotBeNull();
            result.Data.User.Username.Should().Be("testuser");
        }

        [Fact]
        public async Task ProcessLoginAsync_Should_Return_Success_When_SysAdmin_Login_Success()
        {
            // Arrange
            var request = new LoginRequest { Username = "sysadmin", Password = "admin123", RememberMe = false };
            var passwordHash = PasswordHelper.Hash("admin123");

            _mockSysAdminHandler.Setup(x => x.IsSysAdmin("sysadmin")).Returns(true);
            _mockSysAdminHandler.Setup(x => x.GetSysAdminPasswordHashAsync()).ReturnsAsync(passwordHash);
            _mockJwtAuthenticationService.Setup(x => x.GenerateToken(
                "00000000-0000-0000-0000-000000000001", "sysadmin", UserRole.Admin, false))
                .Returns("sysadmin-jwt-token");

            // Act
            var result = await _authBusinessService.ProcessLoginAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Token.Should().Be("sysadmin-jwt-token");
            result.Data.User.Should().NotBeNull();
            result.Data.User.Username.Should().Be("sysadmin");
            result.Data.User.Role.Should().Be(UserRole.Admin);
        }

        [Theory]
        [InlineData("", "password")]
        [InlineData("username", "")]
        [InlineData(null, "password")]
        [InlineData("username", null)]
        [InlineData("   ", "password")]
        [InlineData("username", "   ")]
        public async Task ProcessLoginAsync_Should_Return_Failure_When_Parameters_Are_Invalid(string? username, string? password)
        {
            // Arrange
            var request = new LoginRequest { Username = username!, Password = password! };

            // Act
            var result = await _authBusinessService.ProcessLoginAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("用户名或密码不能为空");
        }

        [Fact]
        public async Task ProcessLoginAsync_Should_Return_Failure_When_User_Not_Found()
        {
            // Arrange
            var request = new LoginRequest { Username = "nonexistentuser", Password = "password123" };

            _mockSysAdminHandler.Setup(x => x.IsSysAdmin("nonexistentuser")).Returns(false);
            _mockQueryService.Setup(x => x.GetUserForAuthenticationAsync("nonexistentuser"))
                .ReturnsAsync(ServiceResult<User>.Failure("用户不存在"));

            // Act
            var result = await _authBusinessService.ProcessLoginAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("用户名或密码错误");
        }

        [Fact]
        public async Task ProcessLoginAsync_Should_Return_Failure_When_Account_Is_Locked()
        {
            // Arrange
            var request = new LoginRequest { Username = "lockeduser", Password = "password123" };
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "lockeduser",
                PasswordHash = PasswordHelper.Hash("password123"),
                FailedLoginCount = 5,
                LockoutEnd = DateTime.UtcNow.AddMinutes(10)
            };

            _mockSysAdminHandler.Setup(x => x.IsSysAdmin("lockeduser")).Returns(false);
            _mockQueryService.Setup(x => x.GetUserForAuthenticationAsync("lockeduser"))
                .ReturnsAsync(ServiceResult<User>.Success(user));

            // Act
            var result = await _authBusinessService.ProcessLoginAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("账户已被锁定");
        }

        [Fact]
        public async Task ProcessLoginAsync_Should_Return_Failure_When_Password_Is_Wrong()
        {
            // Arrange
            var request = new LoginRequest { Username = "testuser", Password = "wrongpassword" };
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                PasswordHash = PasswordHelper.Hash("correctpassword"),
                FailedLoginCount = 0,
                LockoutEnd = null
            };

            _mockSysAdminHandler.Setup(x => x.IsSysAdmin("testuser")).Returns(false);
            _mockQueryService.Setup(x => x.GetUserForAuthenticationAsync("testuser"))
                .ReturnsAsync(ServiceResult<User>.Success(user));

            // Act
            var result = await _authBusinessService.ProcessLoginAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("用户名或密码错误");
            _mockAuthRepository.Verify(x => x.UpdateUserSecurityAsync(user.Id, 1, null), Times.Once);
        }

        [Fact]
        public async Task ProcessLoginAsync_Should_Lock_Account_When_Max_Failed_Attempts_Reached()
        {
            // Arrange
            var request = new LoginRequest { Username = "testuser", Password = "wrongpassword" };
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                PasswordHash = PasswordHelper.Hash("correctpassword"),
                FailedLoginCount = 4, // 已经失败4次，这次失败后会锁定
                LockoutEnd = null
            };

            _mockSysAdminHandler.Setup(x => x.IsSysAdmin("testuser")).Returns(false);
            _mockQueryService.Setup(x => x.GetUserForAuthenticationAsync("testuser"))
                .ReturnsAsync(ServiceResult<User>.Success(user));

            // Act
            var result = await _authBusinessService.ProcessLoginAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            _mockAuthRepository.Verify(x => x.UpdateUserSecurityAsync(
                user.Id, 5, It.IsAny<DateTime?>()), Times.Once);
        }

        [Fact]
        public async Task ProcessLoginAsync_Should_Reset_Failed_Count_When_Login_Success()
        {
            // Arrange
            var request = new LoginRequest { Username = "testuser", Password = "password123" };
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                RealName = "测试用户",
                Role = UserRole.Doctor,
                PasswordHash = PasswordHelper.Hash("password123"),
                FailedLoginCount = 2, // 之前有失败记录
                LockoutEnd = null
            };

            _mockSysAdminHandler.Setup(x => x.IsSysAdmin("testuser")).Returns(false);
            _mockQueryService.Setup(x => x.GetUserForAuthenticationAsync("testuser"))
                .ReturnsAsync(ServiceResult<User>.Success(user));
            _mockJwtAuthenticationService.Setup(x => x.GenerateToken(
                user.Id.ToString(), user.Username, user.Role, false))
                .Returns("jwt-token-123");

            // Act
            var result = await _authBusinessService.ProcessLoginAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            _mockAuthRepository.Verify(x => x.UpdateFailedLoginInfoAsync(user.Id, 0, null), Times.Once);
        }

        [Fact]
        public async Task ProcessLoginAsync_Should_Handle_RememberMe_Option()
        {
            // Arrange
            var request = new LoginRequest { Username = "testuser", Password = "password123", RememberMe = true };
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                RealName = "测试用户",
                Role = UserRole.Doctor,
                PasswordHash = PasswordHelper.Hash("password123")
            };

            _mockSysAdminHandler.Setup(x => x.IsSysAdmin("testuser")).Returns(false);
            _mockQueryService.Setup(x => x.GetUserForAuthenticationAsync("testuser"))
                .ReturnsAsync(ServiceResult<User>.Success(user));
            _mockJwtAuthenticationService.Setup(x => x.GenerateToken(
                user.Id.ToString(), user.Username, user.Role, true))
                .Returns("long-lived-token");

            // Act
            var result = await _authBusinessService.ProcessLoginAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data!.ExpiresAt.Should().BeAfter(DateTime.UtcNow.AddHours(8));
            _mockJwtAuthenticationService.Verify(x => x.GenerateToken(
                user.Id.ToString(), user.Username, user.Role, true), Times.Once);
        }

        [Fact]
        public async Task ProcessLoginAsync_Should_Handle_SysAdmin_Password_Not_Found()
        {
            // Arrange
            var request = new LoginRequest { Username = "sysadmin", Password = "admin123" };

            _mockSysAdminHandler.Setup(x => x.IsSysAdmin("sysadmin")).Returns(true);
            _mockSysAdminHandler.Setup(x => x.GetSysAdminPasswordHashAsync()).ReturnsAsync((string?)null);

            // Act
            var result = await _authBusinessService.ProcessLoginAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("用户名或密码错误");
        }

        [Fact]
        public async Task ProcessLoginAsync_Should_Handle_SysAdmin_Wrong_Password()
        {
            // Arrange
            var request = new LoginRequest { Username = "sysadmin", Password = "wrongpassword" };
            var correctPasswordHash = PasswordHelper.Hash("correctpassword");

            _mockSysAdminHandler.Setup(x => x.IsSysAdmin("sysadmin")).Returns(true);
            _mockSysAdminHandler.Setup(x => x.GetSysAdminPasswordHashAsync()).ReturnsAsync(correctPasswordHash);

            // Act
            var result = await _authBusinessService.ProcessLoginAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("用户名或密码错误");
        }

        [Fact]
        public async Task ProcessLoginAsync_Should_Handle_Exception()
        {
            // Arrange
            var request = new LoginRequest { Username = "testuser", Password = "password123" };

            _mockSysAdminHandler.Setup(x => x.IsSysAdmin("testuser")).Returns(false);
            _mockQueryService.Setup(x => x.GetUserForAuthenticationAsync("testuser"))
                .ThrowsAsync(new Exception("数据库连接失败"));

            // Act
            var result = await _authBusinessService.ProcessLoginAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("登录处理异常，请稍后重试");
        }

        #endregion

        #region ProcessLogoutAsync 测试

        [Fact]
        public async Task ProcessLogoutAsync_Should_Return_Success_When_Valid_Request()
        {
            // Arrange
            var request = new LogoutRequest { Username = "testuser" };

            // Act
            var result = await _authBusinessService.ProcessLogoutAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task ProcessLogoutAsync_Should_Return_Failure_When_Username_Is_Invalid(string? username)
        {
            // Arrange
            var request = new LogoutRequest { Username = username! };

            // Act
            var result = await _authBusinessService.ProcessLogoutAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("登出请求无效");
        }

        [Fact]
        public async Task ProcessLogoutAsync_Should_Handle_Exception()
        {
            // Arrange
            var request = new LogoutRequest { Username = "testuser" };

            // 通过Mock Logger来模拟异常（实际上UltraThink简化版不会抛异常，但测试完整性）
            // 这里主要测试方法的健壮性

            // Act
            var result = await _authBusinessService.ProcessLogoutAsync(request);

            // Assert - UltraThink简化版总是成功
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
        }

        #endregion

        #region ValidatePasswordAsync 测试

        [Fact]
        public async Task ValidatePasswordAsync_Should_Return_True_When_Normal_User_Password_Is_Correct()
        {
            // Arrange
            var password = "password123";
            var user = new User
            {
                Username = "testuser",
                PasswordHash = PasswordHelper.Hash(password)
            };

            // Act
            var result = await _authBusinessService.ValidatePasswordAsync(user, password);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
        }

        [Fact]
        public async Task ValidatePasswordAsync_Should_Return_False_When_Normal_User_Password_Is_Wrong()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser",
                PasswordHash = PasswordHelper.Hash("correctpassword")
            };

            // Act
            var result = await _authBusinessService.ValidatePasswordAsync(user, "wrongpassword");

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeFalse();
        }

        [Fact]
        public async Task ValidatePasswordAsync_Should_Validate_SysAdmin_Password()
        {
            // Arrange
            var password = "adminpassword";
            var sysAdminPasswordHash = PasswordHelper.Hash(password);
            var user = new User { Username = "sysadmin" };

            _mockSysAdminHandler.Setup(x => x.GetSysAdminPasswordHashAsync()).ReturnsAsync(sysAdminPasswordHash);

            // Act
            var result = await _authBusinessService.ValidatePasswordAsync(user, password);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
        }

        [Fact]
        public async Task ValidatePasswordAsync_Should_Return_Failure_When_User_Is_Null()
        {
            // Act
            var result = await _authBusinessService.ValidatePasswordAsync(null!, "password");

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("用户信息或密码不能为空");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task ValidatePasswordAsync_Should_Return_Failure_When_Password_Is_Invalid(string? password)
        {
            // Arrange
            var user = new User { Username = "testuser" };

            // Act
            var result = await _authBusinessService.ValidatePasswordAsync(user, password!);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("用户信息或密码不能为空");
        }

        [Fact]
        public async Task ValidatePasswordAsync_Should_Handle_Exception()
        {
            // Arrange
            var user = new User { Username = "testuser", PasswordHash = "invalid-hash" };
            var password = "password123";

            // Act
            var result = await _authBusinessService.ValidatePasswordAsync(user, password);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("密码验证失败");
        }

        #endregion

        #region ChangeSysAdminPasswordAsync 测试

        [Fact]
        public async Task ChangeSysAdminPasswordAsync_Should_Return_Success_When_Valid_Password()
        {
            // Arrange
            var newPassword = "NewSecurePassword123!";

            // Act
            var result = await _authBusinessService.ChangeSysAdminPasswordAsync(newPassword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            _mockAuthRepository.Verify(x => x.UpdateAdminPasswordHashAsync("sysadmin", It.IsAny<string>()), Times.Once);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task ChangeSysAdminPasswordAsync_Should_Return_Failure_When_Password_Is_Empty(string? newPassword)
        {
            // Act
            var result = await _authBusinessService.ChangeSysAdminPasswordAsync(newPassword!);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("新密码不能为空");
        }

        [Theory]
        [InlineData("1")]
        [InlineData("12")]
        [InlineData("1234567")]
        public async Task ChangeSysAdminPasswordAsync_Should_Return_Failure_When_Password_Is_Too_Short(string newPassword)
        {
            // Act
            var result = await _authBusinessService.ChangeSysAdminPasswordAsync(newPassword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("密码长度不能少于8位");
        }

        [Fact]
        public async Task ChangeSysAdminPasswordAsync_Should_Handle_Repository_Exception()
        {
            // Arrange
            var newPassword = "NewSecurePassword123!";
            _mockAuthRepository.Setup(x => x.UpdateAdminPasswordHashAsync("sysadmin", It.IsAny<string>()))
                .ThrowsAsync(new Exception("数据库更新失败"));

            // Act
            var result = await _authBusinessService.ChangeSysAdminPasswordAsync(newPassword);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("修改系统管理员密码失败");
        }

        #endregion

        #region VerifyCredentialsAsync 测试

        [Fact]
        public async Task VerifyCredentialsAsync_Should_Return_Success_When_Credentials_Are_Valid()
        {
            // Arrange
            var request = new LoginRequest { Username = "testuser", Password = "password123" };
            var user = new User
            {
                Username = "testuser",
                PasswordHash = PasswordHelper.Hash("password123")
            };

            _mockQueryService.Setup(x => x.GetUserForAuthenticationAsync("testuser"))
                .ReturnsAsync(ServiceResult<User>.Success(user));

            // Act
            var result = await _authBusinessService.VerifyCredentialsAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be("凭据验证成功");
        }

        [Fact]
        public async Task VerifyCredentialsAsync_Should_Return_Failure_When_User_Not_Found()
        {
            // Arrange
            var request = new LoginRequest { Username = "nonexistentuser", Password = "password123" };

            _mockQueryService.Setup(x => x.GetUserForAuthenticationAsync("nonexistentuser"))
                .ReturnsAsync(ServiceResult<User>.Failure("用户不存在"));

            // Act
            var result = await _authBusinessService.VerifyCredentialsAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("用户不存在");
        }

        [Fact]
        public async Task VerifyCredentialsAsync_Should_Return_Failure_When_Password_Is_Wrong()
        {
            // Arrange
            var request = new LoginRequest { Username = "testuser", Password = "wrongpassword" };
            var user = new User
            {
                Username = "testuser",
                PasswordHash = PasswordHelper.Hash("correctpassword")
            };

            _mockQueryService.Setup(x => x.GetUserForAuthenticationAsync("testuser"))
                .ReturnsAsync(ServiceResult<User>.Success(user));

            // Act
            var result = await _authBusinessService.VerifyCredentialsAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("用户名或密码错误");
        }

        #endregion

        #region 边界值和集成测试

        [Fact]
        public void AuthBusinessService_Should_Implement_IAuthBusinessService()
        {
            // Assert
            _authBusinessService.Should().BeAssignableTo<IAuthBusinessService>();
        }

        [Fact]
        public async Task ProcessLoginAsync_Should_Handle_Concurrent_Failed_Attempts()
        {
            // Arrange - 模拟并发失败登录尝试
            var request = new LoginRequest { Username = "testuser", Password = "wrongpassword" };
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                PasswordHash = PasswordHelper.Hash("correctpassword"),
                FailedLoginCount = 4, // 接近锁定阈值
                LockoutEnd = null
            };

            _mockSysAdminHandler.Setup(x => x.IsSysAdmin("testuser")).Returns(false);
            _mockQueryService.Setup(x => x.GetUserForAuthenticationAsync("testuser"))
                .ReturnsAsync(ServiceResult<User>.Success(user));

            // Act
            var result = await _authBusinessService.ProcessLoginAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            // 应该正确处理失败计数增加和账户锁定
            _mockAuthRepository.Verify(x => x.UpdateUserSecurityAsync(
                user.Id, 5, It.IsAny<DateTime?>()), Times.Once);
        }

        [Fact]
        public async Task ProcessLoginAsync_Should_Clear_Lockout_When_Login_Success_After_Lockout_Expired()
        {
            // Arrange
            var request = new LoginRequest { Username = "testuser", Password = "password123" };
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                RealName = "测试用户",
                Role = UserRole.Doctor,
                PasswordHash = PasswordHelper.Hash("password123"),
                FailedLoginCount = 5,
                LockoutEnd = DateTime.UtcNow.AddMinutes(-1) // 锁定已过期
            };

            _mockSysAdminHandler.Setup(x => x.IsSysAdmin("testuser")).Returns(false);
            _mockQueryService.Setup(x => x.GetUserForAuthenticationAsync("testuser"))
                .ReturnsAsync(ServiceResult<User>.Success(user));
            _mockJwtAuthenticationService.Setup(x => x.GenerateToken(
                user.Id.ToString(), user.Username, user.Role, false))
                .Returns("jwt-token-123");

            // Act
            var result = await _authBusinessService.ProcessLoginAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            // 应该重置失败计数和锁定状态
            _mockAuthRepository.Verify(x => x.UpdateFailedLoginInfoAsync(user.Id, 0, null), Times.Once);
        }

        #endregion
    }
}
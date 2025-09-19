using System;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using Moq;
using Xunit;

namespace LYBT.Module.Auth.Tests
{
    /// <summary>
    /// AuthService 简化单元测试 - UltraThink双层架构适配
    /// 专注于测试核心认证功能，Mock QueryService和BusinessService
    /// </summary>
    public class SimpleAuthServiceTests
    {
        private readonly AuthService _authService;
        private readonly Mock<IAuthQueryService> _mockQueryService;
        private readonly Mock<IAuthBusinessService> _mockBusinessService;

        public SimpleAuthServiceTests()
        {
            // UltraThink双层架构Mock配置
            _mockQueryService = new Mock<IAuthQueryService>();
            _mockBusinessService = new Mock<IAuthBusinessService>();

            // 创建 AuthService 实例 (主Service委托模式)
            _authService = new AuthService(
                _mockQueryService.Object,
                _mockBusinessService.Object);
        }

        #region LoginAsync 测试

        [Fact]
        public async Task LoginAsync_Should_Return_Success_When_Valid_Credentials()
        {
            // Arrange
            var loginRequest = new LoginRequest { Username = "testuser", Password = "testpass123" };
            var expectedResponse = new LoginResponse 
            { 
                Token = "test-jwt-token", 
                RefreshToken = "refresh-token-123",
                User = new UserDto { Username = "testuser", RealName = "测试用户" }
            };

            _mockBusinessService
                .Setup(x => x.ProcessLoginAsync(loginRequest))
                .ReturnsAsync(ServiceResult<LoginResponse>.Success(expectedResponse));

            // Act
            var result = await _authService.LoginAsync(loginRequest);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Token.Should().Be("test-jwt-token");
            result.Data.User.Should().NotBeNull();
            result.Data.User.Username.Should().Be("testuser");
            result.Data.User.RealName.Should().Be("测试用户");
            result.Data.RefreshToken.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task LoginAsync_Should_Return_Failure_When_Invalid_Credentials()
        {
            // Arrange
            var loginRequest = new LoginRequest { Username = "invaliduser", Password = "wrongpass" };

            _mockBusinessService
                .Setup(x => x.ProcessLoginAsync(loginRequest))
                .ReturnsAsync(ServiceResult<LoginResponse>.Failure("用户名或密码错误"));

            // Act
            var result = await _authService.LoginAsync(loginRequest);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("用户名或密码错误");
        }

        #endregion

        #region LogoutAsync 测试

        [Fact]
        public async Task LogoutAsync_Should_Return_Success_When_Valid_Request()
        {
            // Arrange
            var logoutRequest = new LogoutRequest { Username = "testuser" };

            _mockBusinessService
                .Setup(x => x.ProcessLogoutAsync(logoutRequest))
                .ReturnsAsync(ServiceResult<bool>.Success(true));

            // Act
            var result = await _authService.LogoutAsync(logoutRequest);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
        }

        #endregion

        #region VerifyCredentialsAsync 测试

        [Fact]
        public async Task VerifyCredentialsAsync_Should_Return_Token_When_Valid()
        {
            // Arrange
            var loginRequest = new LoginRequest { Username = "validuser", Password = "validpass123" };

            _mockBusinessService
                .Setup(x => x.VerifyCredentialsAsync(loginRequest))
                .ReturnsAsync(ServiceResult<string>.Success("valid-token"));

            // Act
            var result = await _authService.VerifyCredentialsAsync(loginRequest);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be("valid-token");
        }

        #endregion

        #region ChangeSysAdminPasswordAsync 测试

        [Fact]
        public async Task ChangeSysAdminPasswordAsync_Should_Return_Success_When_Valid()
        {
            // Arrange
            var request = new ChangeSysAdminPassword { NewPassword = "NewSecurePassword123" };

            _mockBusinessService
                .Setup(x => x.ChangeSysAdminPasswordAsync("NewSecurePassword123"))
                .ReturnsAsync(ServiceResult<bool>.Success(true));

            // Act
            var result = await _authService.ChangeSysAdminPasswordAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
        }

        #endregion

        #region 异常分支和边界值测试 (成功经验应用)

        [Fact]
        public async Task LoginAsync_Should_Return_Failure_When_BusinessService_Fails()
        {
            // Arrange - 业务失败分支测试
            var loginRequest = new LoginRequest { Username = "testuser", Password = "testpass123" };

            _mockBusinessService
                .Setup(x => x.ProcessLoginAsync(loginRequest))
                .ReturnsAsync(ServiceResult<LoginResponse>.Failure("认证服务异常"));

            // Act
            var result = await _authService.LoginAsync(loginRequest);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("认证服务异常");
        }

        [Fact]
        public async Task LoginAsync_With_Empty_Username_Should_Return_Failure()
        {
            // Arrange - 空值测试
            var loginRequest = new LoginRequest { Username = "", Password = "testpass123" };

            _mockBusinessService
                .Setup(x => x.ProcessLoginAsync(loginRequest))
                .ReturnsAsync(ServiceResult<LoginResponse>.Failure("用户名不能为空"));

            // Act
            var result = await _authService.LoginAsync(loginRequest);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("用户名不能为空");
        }

        [Fact]
        public async Task LoginAsync_With_Empty_Password_Should_Return_Failure()
        {
            // Arrange - 空值测试
            var loginRequest = new LoginRequest { Username = "testuser", Password = "" };

            _mockBusinessService
                .Setup(x => x.ProcessLoginAsync(loginRequest))
                .ReturnsAsync(ServiceResult<LoginResponse>.Failure("密码不能为空"));

            // Act
            var result = await _authService.LoginAsync(loginRequest);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("密码不能为空");
        }

        [Fact]
        public async Task LoginAsync_With_Long_Username_Should_Handle_Gracefully()
        {
            // Arrange - 极端值测试：长用户名
            var longUsername = new string('a', 256); // 256字符长用户名
            var loginRequest = new LoginRequest { Username = longUsername, Password = "testpass123" };

            _mockBusinessService
                .Setup(x => x.ProcessLoginAsync(loginRequest))
                .ReturnsAsync(ServiceResult<LoginResponse>.Failure("用户名过长"));

            // Act
            var result = await _authService.LoginAsync(loginRequest);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("用户名过长");
        }

        [Fact]
        public async Task LogoutAsync_Should_Return_Failure_When_BusinessService_Fails()
        {
            // Arrange - 登出失败测试
            var logoutRequest = new LogoutRequest { Username = "testuser" };

            _mockBusinessService
                .Setup(x => x.ProcessLogoutAsync(logoutRequest))
                .ReturnsAsync(ServiceResult<bool>.Failure("登出失败"));

            // Act
            var result = await _authService.LogoutAsync(logoutRequest);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("登出失败");
        }

        [Fact]
        public async Task VerifyCredentialsAsync_Should_Return_Failure_When_BusinessService_Fails()
        {
            // Arrange - 凭据验证失败测试
            var loginRequest = new LoginRequest { Username = "testuser", Password = "testpass123" };

            _mockBusinessService
                .Setup(x => x.VerifyCredentialsAsync(loginRequest))
                .ReturnsAsync(ServiceResult<string>.Failure("凭据验证失败"));

            // Act
            var result = await _authService.VerifyCredentialsAsync(loginRequest);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("凭据验证失败");
        }

        [Fact]
        public async Task VerifyCredentialsAsync_With_Invalid_Token_Should_Return_Failure()
        {
            // Arrange - 无效令牌测试
            var loginRequest = new LoginRequest { Username = "testuser", Password = "invalidpass" };

            _mockBusinessService
                .Setup(x => x.VerifyCredentialsAsync(loginRequest))
                .ReturnsAsync(ServiceResult<string>.Failure("无效的用户凭据"));

            // Act
            var result = await _authService.VerifyCredentialsAsync(loginRequest);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("无效的用户凭据");
        }

        [Fact]
        public async Task ChangeSysAdminPasswordAsync_Should_Return_Failure_When_BusinessService_Fails()
        {
            // Arrange - 密码修改失败测试
            var request = new ChangeSysAdminPassword { NewPassword = "NewSecurePassword123" };

            _mockBusinessService
                .Setup(x => x.ChangeSysAdminPasswordAsync("NewSecurePassword123"))
                .ReturnsAsync(ServiceResult<bool>.Failure("密码修改失败"));

            // Act
            var result = await _authService.ChangeSysAdminPasswordAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("密码修改失败");
        }

        [Fact]
        public async Task ChangeSysAdminPasswordAsync_With_Weak_Password_Should_Return_Failure()
        {
            // Arrange - 弱密码测试
            var request = new ChangeSysAdminPassword { NewPassword = "123" };

            _mockBusinessService
                .Setup(x => x.ChangeSysAdminPasswordAsync("123"))
                .ReturnsAsync(ServiceResult<bool>.Failure("密码强度不足"));

            // Act
            var result = await _authService.ChangeSysAdminPasswordAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("密码强度不足");
        }

        #endregion
    }
}
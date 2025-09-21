using System;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Services;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Moq;
using Xunit;

namespace LYBT.Module.Auth.Tests.Services
{
    /// <summary>
    /// AuthService 完整单元测试 - UltraThink双层架构
    /// 主Service委托模式测试，验证所有委托调用的正确性
    /// 重点：测试委托逻辑，不测试具体业务实现（由QueryService和BusinessService负责）
    /// </summary>
    public class AuthServiceTests
    {
        private readonly AuthService _authService;
        private readonly Mock<IAuthQueryService> _mockQueryService;
        private readonly Mock<IAuthBusinessService> _mockBusinessService;

        public AuthServiceTests()
        {
            _mockQueryService = new Mock<IAuthQueryService>();
            _mockBusinessService = new Mock<IAuthBusinessService>();
            _authService = new AuthService(_mockQueryService.Object, _mockBusinessService.Object);
        }

        #region 构造函数测试

        [Fact]
        public void Constructor_Should_Throw_When_QueryService_Is_Null()
        {
            // Act & Assert
            var action = () => new AuthService(null!, _mockBusinessService.Object);
            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("queryService");
        }

        [Fact]
        public void Constructor_Should_Throw_When_BusinessService_Is_Null()
        {
            // Act & Assert
            var action = () => new AuthService(_mockQueryService.Object, null!);
            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("businessService");
        }

        [Fact]
        public void Constructor_Should_Create_Instance_When_Dependencies_Are_Valid()
        {
            // Act
            var service = new AuthService(_mockQueryService.Object, _mockBusinessService.Object);

            // Assert
            service.Should().NotBeNull();
        }

        #endregion

        #region VerifyCredentialsAsync 测试

        [Fact]
        public async Task VerifyCredentialsAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var request = new LoginRequest { Username = "testuser", Password = "password123" };
            var expectedResult = ServiceResult<string>.Success("credentials-verified");

            _mockBusinessService
                .Setup(x => x.VerifyCredentialsAsync(request))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _authService.VerifyCredentialsAsync(request);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.VerifyCredentialsAsync(request), Times.Once);
        }

        [Fact]
        public async Task VerifyCredentialsAsync_Should_Return_Failure_When_BusinessService_Fails()
        {
            // Arrange
            var request = new LoginRequest { Username = "invaliduser", Password = "wrongpass" };
            var expectedResult = ServiceResult<string>.Failure("认证失败");

            _mockBusinessService
                .Setup(x => x.VerifyCredentialsAsync(request))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _authService.VerifyCredentialsAsync(request);

            // Assert
            result.Should().BeSameAs(expectedResult);
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("认证失败");
        }

        [Fact]
        public async Task VerifyCredentialsAsync_Should_Handle_Null_Request()
        {
            // Arrange
            LoginRequest? request = null;
            var expectedResult = ServiceResult<string>.Failure("请求不能为空");

            _mockBusinessService
                .Setup(x => x.VerifyCredentialsAsync(request!))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _authService.VerifyCredentialsAsync(request!);

            // Assert
            result.Should().BeSameAs(expectedResult);
        }

        #endregion

        #region ChangeSysAdminPasswordAsync 测试

        [Fact]
        public async Task ChangeSysAdminPasswordAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var request = new ChangeSysAdminPassword { NewPassword = "NewSecurePassword123!" };
            var expectedResult = ServiceResult<bool>.Success(true);

            _mockBusinessService
                .Setup(x => x.ChangeSysAdminPasswordAsync("NewSecurePassword123!"))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _authService.ChangeSysAdminPasswordAsync(request);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.ChangeSysAdminPasswordAsync("NewSecurePassword123!"), Times.Once);
        }

        [Fact]
        public async Task ChangeSysAdminPasswordAsync_Should_Return_Failure_When_BusinessService_Fails()
        {
            // Arrange
            var request = new ChangeSysAdminPassword { NewPassword = "weak" };
            var expectedResult = ServiceResult<bool>.Failure("密码强度不足");

            _mockBusinessService
                .Setup(x => x.ChangeSysAdminPasswordAsync("weak"))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _authService.ChangeSysAdminPasswordAsync(request);

            // Assert
            result.Should().BeSameAs(expectedResult);
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("密码强度不足");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task ChangeSysAdminPasswordAsync_Should_Handle_Invalid_Password(string? newPassword)
        {
            // Arrange
            var request = new ChangeSysAdminPassword { NewPassword = newPassword! };
            var expectedResult = ServiceResult<bool>.Failure("新密码不能为空");

            _mockBusinessService
                .Setup(x => x.ChangeSysAdminPasswordAsync(newPassword!))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _authService.ChangeSysAdminPasswordAsync(request);

            // Assert
            result.Should().BeSameAs(expectedResult);
        }

        #endregion

        #region LoginAsync 测试

        [Fact]
        public async Task LoginAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var request = new LoginRequest { Username = "testuser", Password = "password123", RememberMe = false };
            var loginResponse = new LoginResponse
            {
                Token = "jwt-token-123",
                User = new UserDto { Username = "testuser", RealName = "测试用户", Role = UserRole.Doctor },
                ExpiresAt = DateTime.UtcNow.AddHours(8)
            };
            var expectedResult = ServiceResult<LoginResponse>.Success(loginResponse);

            _mockBusinessService
                .Setup(x => x.ProcessLoginAsync(request))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.ProcessLoginAsync(request), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_Should_Return_Failure_When_BusinessService_Fails()
        {
            // Arrange
            var request = new LoginRequest { Username = "invaliduser", Password = "wrongpass" };
            var expectedResult = ServiceResult<LoginResponse>.Failure("用户名或密码错误");

            _mockBusinessService
                .Setup(x => x.ProcessLoginAsync(request))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            result.Should().BeSameAs(expectedResult);
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("用户名或密码错误");
        }

        [Fact]
        public async Task LoginAsync_Should_Handle_RememberMe_Option()
        {
            // Arrange
            var request = new LoginRequest { Username = "testuser", Password = "password123", RememberMe = true };
            var loginResponse = new LoginResponse
            {
                Token = "long-lived-token",
                User = new UserDto { Username = "testuser", Role = UserRole.Doctor },
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };
            var expectedResult = ServiceResult<LoginResponse>.Success(loginResponse);

            _mockBusinessService
                .Setup(x => x.ProcessLoginAsync(request))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            result.Should().BeSameAs(expectedResult);
            result.Data!.ExpiresAt.Should().BeAfter(DateTime.UtcNow.AddHours(8));
        }

        #endregion

        #region LogoutAsync 测试

        [Fact]
        public async Task LogoutAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var request = new LogoutRequest { Username = "testuser" };
            var expectedResult = ServiceResult<bool>.Success(true);

            _mockBusinessService
                .Setup(x => x.ProcessLogoutAsync(request))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _authService.LogoutAsync(request);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockBusinessService.Verify(x => x.ProcessLogoutAsync(request), Times.Once);
        }

        [Fact]
        public async Task LogoutAsync_Should_Return_Failure_When_BusinessService_Fails()
        {
            // Arrange
            var request = new LogoutRequest { Username = "testuser" };
            var expectedResult = ServiceResult<bool>.Failure("登出失败");

            _mockBusinessService
                .Setup(x => x.ProcessLogoutAsync(request))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _authService.LogoutAsync(request);

            // Assert
            result.Should().BeSameAs(expectedResult);
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task LogoutAsync_Should_Handle_Empty_Username()
        {
            // Arrange
            var request = new LogoutRequest { Username = "" };
            var expectedResult = ServiceResult<bool>.Failure("用户名不能为空");

            _mockBusinessService
                .Setup(x => x.ProcessLogoutAsync(request))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _authService.LogoutAsync(request);

            // Assert
            result.Should().BeSameAs(expectedResult);
        }

        #endregion

        #region ValidateTokenAsync 测试

        [Fact]
        public async Task ValidateTokenAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var token = "valid-jwt-token";
            var expectedResult = ServiceResult<bool>.Success(true);

            _mockQueryService
                .Setup(x => x.ValidateTokenAsync(token))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _authService.ValidateTokenAsync(token);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.ValidateTokenAsync(token), Times.Once);
        }

        [Fact]
        public async Task ValidateTokenAsync_Should_Return_Failure_When_QueryService_Fails()
        {
            // Arrange
            var token = "invalid-token";
            var expectedResult = ServiceResult<bool>.Failure("Token无效");

            _mockQueryService
                .Setup(x => x.ValidateTokenAsync(token))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _authService.ValidateTokenAsync(token);

            // Assert
            result.Should().BeSameAs(expectedResult);
            result.IsSuccess.Should().BeFalse();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task ValidateTokenAsync_Should_Handle_Invalid_Token(string? token)
        {
            // Arrange
            var expectedResult = ServiceResult<bool>.Failure("Token不能为空");

            _mockQueryService
                .Setup(x => x.ValidateTokenAsync(token!))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _authService.ValidateTokenAsync(token!);

            // Assert
            result.Should().BeSameAs(expectedResult);
        }

        #endregion

        #region GetSessionInfoAsync 测试

        [Fact]
        public async Task GetSessionInfoAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var token = "valid-jwt-token";
            var sessionInfo = new
            {
                UserId = "user-123",
                Username = "testuser",
                Role = UserRole.Doctor,
                IsAuthenticated = true
            };
            var expectedResult = ServiceResult<object>.Success(sessionInfo);

            _mockQueryService
                .Setup(x => x.GetSessionInfoAsync(token))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _authService.GetSessionInfoAsync(token);

            // Assert
            result.Should().BeSameAs(expectedResult);
            _mockQueryService.Verify(x => x.GetSessionInfoAsync(token), Times.Once);
        }

        [Fact]
        public async Task GetSessionInfoAsync_Should_Return_Failure_When_QueryService_Fails()
        {
            // Arrange
            var token = "invalid-token";
            var expectedResult = ServiceResult<object>.Failure("无法获取会话信息");

            _mockQueryService
                .Setup(x => x.GetSessionInfoAsync(token))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _authService.GetSessionInfoAsync(token);

            // Assert
            result.Should().BeSameAs(expectedResult);
            result.IsSuccess.Should().BeFalse();
        }

        [Fact]
        public async Task GetSessionInfoAsync_Should_Handle_Expired_Token()
        {
            // Arrange
            var token = "expired-token";
            var expectedResult = ServiceResult<object>.Failure("Token已过期");

            _mockQueryService
                .Setup(x => x.GetSessionInfoAsync(token))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _authService.GetSessionInfoAsync(token);

            // Assert
            result.Should().BeSameAs(expectedResult);
        }

        #endregion

        #region RefreshTokenAsync 测试 (UltraThink简化版)

        [Fact]
        public async Task RefreshTokenAsync_Should_Return_Failure_Always()
        {
            // Arrange
            var refreshToken = "any-refresh-token";

            // Act
            var result = await _authService.RefreshTokenAsync(refreshToken);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("请重新登录以获取新的访问令牌");
        }

        [Theory]
        [InlineData("valid-refresh-token")]
        [InlineData("")]
        [InlineData(null)]
        public async Task RefreshTokenAsync_Should_Always_Return_Same_Failure_Message(string? refreshToken)
        {
            // Act
            var result = await _authService.RefreshTokenAsync(refreshToken!);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("请重新登录以获取新的访问令牌");
        }

        [Fact]
        public async Task RefreshTokenAsync_Should_Complete_Without_Dependencies()
        {
            // Arrange
            var refreshToken = "test-token";

            // Act & Assert - 不应该调用任何依赖服务
            var result = await _authService.RefreshTokenAsync(refreshToken);

            // Verify - 确保没有调用任何Mock服务
            _mockQueryService.Verify(x => x.ValidateTokenAsync(It.IsAny<string>()), Times.Never);
            _mockBusinessService.Verify(x => x.ProcessLoginAsync(It.IsAny<LoginRequest>()), Times.Never);
        }

        #endregion

        #region 边界值和异常测试

        [Fact]
        public async Task All_Methods_Should_Handle_Service_Dependencies_Correctly()
        {
            // Arrange - 确保所有依赖都正确注入
            var request = new LoginRequest { Username = "test", Password = "test" };

            _mockBusinessService
                .Setup(x => x.ProcessLoginAsync(It.IsAny<LoginRequest>()))
                .ReturnsAsync(ServiceResult<LoginResponse>.Success(new LoginResponse()));

            _mockQueryService
                .Setup(x => x.ValidateTokenAsync(It.IsAny<string>()))
                .ReturnsAsync(ServiceResult<bool>.Success(true));

            // Act & Assert - 所有方法都应该能正常调用
            await _authService.LoginAsync(request);
            await _authService.ValidateTokenAsync("token");
            await _authService.RefreshTokenAsync("refresh");

            // Verify
            _mockBusinessService.Verify(x => x.ProcessLoginAsync(request), Times.Once);
            _mockQueryService.Verify(x => x.ValidateTokenAsync("token"), Times.Once);
        }

        [Fact]
        public void AuthService_Should_Implement_IAuthService()
        {
            // Assert
            _authService.Should().BeAssignableTo<IAuthService>();
        }

        #endregion
    }
}
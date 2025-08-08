using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using LYBT.WebAPI.Controllers;
using LYBT.Module.Auth.Interfaces;
using LYBT.Infrastructure.Authentication;
using LYBT.Module.Auth.Services;
using LYBT.Shared.Models.Auth;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Enums;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using FluentAssertions;

namespace LYBT.WebAPI.Tests.Controllers
{
    /// <summary>
    /// AuthController 单元测试
    /// </summary>
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _authServiceMock;
        private readonly Mock<IJwtAuthenticationService> _jwtServiceMock;
        private readonly Mock<SysAdminHandler> _sysAdminHandlerMock;
        private readonly Mock<ILogger<AuthController>> _loggerMock;
        private readonly Mock<IMemoryCache> _cacheMock;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _authServiceMock = new Mock<IAuthService>();
            _jwtServiceMock = new Mock<IJwtAuthenticationService>();
            _sysAdminHandlerMock = new Mock<SysAdminHandler>();
            _loggerMock = new Mock<ILogger<AuthController>>();
            _cacheMock = new Mock<IMemoryCache>();

            _controller = new AuthController(
                _authServiceMock.Object,
                _jwtServiceMock.Object,
                _sysAdminHandlerMock.Object,
                _loggerMock.Object,
                _cacheMock.Object
            );
        }

        #region Login Tests

        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturnLoginResponse()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Username = "testuser",
                Password = "password123",
                RememberMe = false,
                LoginType = "Password"
            };

            var loginResult = new LoginResultDto
            {
                IsSuccess = true,
                Message = "登录成功",
                LoginInfo = new LoginInfoDto
                {
                    UserId = 1,
                    Username = "testuser",
                    Email = "test@example.com",
                    FullName = "测试用户",
                    Status = UserStatus.Active,
                    IsApproved = true
                }
            };

            var token = "mock-jwt-token";
            var refreshToken = "mock-refresh-token";

            _authServiceMock.Setup(x => x.LoginAsync(It.IsAny<LoginRequestDto>()))
                .ReturnsAsync(loginResult);
            _jwtServiceMock.Setup(x => x.GenerateToken(It.IsAny<LoginInfoDto>(), It.IsAny<bool>()))
                .Returns(token);
            _jwtServiceMock.Setup(x => x.GenerateRefreshToken())
                .Returns(refreshToken);

            // Act
            var result = await _controller.Login(loginRequest);

            // Assert
            result.Should().NotBeNull();
            var actionResult = result.Result as OkObjectResult;
            actionResult.Should().NotBeNull();
            actionResult!.StatusCode.Should().Be(200);

            var response = actionResult.Value as LoginResponse;
            response.Should().NotBeNull();
            response!.IsSuccess.Should().BeTrue();
            response.Token.Should().Be(token);
            response.RefreshToken.Should().Be(refreshToken);
            response.User.Should().NotBeNull();
            response.User.Username.Should().Be("testuser");
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Username = "invaliduser",
                Password = "wrongpassword",
                RememberMe = false,
                LoginType = "Password"
            };

            var loginResult = new LoginResultDto
            {
                IsSuccess = false,
                Message = "用户名或密码错误"
            };

            _authServiceMock.Setup(x => x.LoginAsync(It.IsAny<LoginRequestDto>()))
                .ReturnsAsync(loginResult);

            // Act
            var result = await _controller.Login(loginRequest);

            // Assert
            result.Should().NotBeNull();
            var actionResult = result.Result as UnauthorizedObjectResult;
            actionResult.Should().NotBeNull();
            actionResult!.StatusCode.Should().Be(401);

            var response = actionResult.Value as LoginResponse;
            response.Should().NotBeNull();
            response!.IsSuccess.Should().BeFalse();
            response.Message.Should().Be("用户名或密码错误");
        }

        [Fact]
        public async Task Login_WithNullRequest_ShouldReturnBadRequest()
        {
            // Act
            var result = await _controller.Login(null!);

            // Assert
            result.Should().NotBeNull();
            var actionResult = result.Result as BadRequestObjectResult;
            actionResult.Should().NotBeNull();
            actionResult!.StatusCode.Should().Be(400);
        }

        [Theory]
        [InlineData("", "password123")]
        [InlineData("username", "")]
        [InlineData(null, "password123")]
        [InlineData("username", null)]
        public async Task Login_WithInvalidInput_ShouldReturnBadRequest(string? username, string? password)
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Username = username!,
                Password = password!,
                RememberMe = false,
                LoginType = "Password"
            };

            // Act
            var result = await _controller.Login(loginRequest);

            // Assert
            result.Should().NotBeNull();
            var actionResult = result.Result as BadRequestObjectResult;
            actionResult.Should().NotBeNull();
            actionResult!.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task Login_WhenServiceThrowsException_ShouldReturnInternalServerError()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Username = "testuser",
                Password = "password123",
                RememberMe = false,
                LoginType = "Password"
            };

            _authServiceMock.Setup(x => x.LoginAsync(It.IsAny<LoginRequestDto>()))
                .ThrowsAsync(new Exception("数据库连接失败"));

            // Act
            var result = await _controller.Login(loginRequest);

            // Assert
            result.Should().NotBeNull();
            var actionResult = result.Result as ObjectResult;
            actionResult.Should().NotBeNull();
            actionResult!.StatusCode.Should().Be(500);
        }

        #endregion

        #region Logout Tests

        [Fact]
        public async Task Logout_WithAuthenticatedUser_ShouldReturnSuccess()
        {
            // Arrange
            var username = "testuser";
            SetupAuthenticatedUser(username);

            _authServiceMock.Setup(x => x.LogoutAsync(It.IsAny<LogoutRequestDto>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Logout();

            // Assert
            result.Should().NotBeNull();
            var actionResult = result as OkObjectResult;
            actionResult.Should().NotBeNull();
            actionResult!.StatusCode.Should().Be(200);

            _authServiceMock.Verify(x => x.LogoutAsync(It.Is<LogoutRequestDto>(dto => dto.Username == username)), Times.Once);
        }

        [Fact]
        public async Task Logout_WithNoAuthenticatedUser_ShouldReturnUnauthorized()
        {
            // Arrange - No authenticated user setup

            // Act
            var result = await _controller.Logout();

            // Assert
            result.Should().NotBeNull();
            var actionResult = result as UnauthorizedObjectResult;
            actionResult.Should().NotBeNull();
            actionResult!.StatusCode.Should().Be(401);
        }

        #endregion

        #region GetCurrentUser Tests

        [Fact]
        public void GetCurrentUser_WithAuthenticatedUser_ShouldReturnUserInfo()
        {
            // Arrange
            var username = "testuser";
            var userId = "123";
            var email = "test@example.com";
            var fullName = "测试用户";

            SetupAuthenticatedUser(username, userId, email, fullName);

            // Act
            var result = _controller.GetCurrentUser();

            // Assert
            result.Should().NotBeNull();
            var actionResult = result.Result as OkObjectResult;
            actionResult.Should().NotBeNull();
            actionResult!.StatusCode.Should().Be(200);

            var userInfo = actionResult.Value as UserInfo;
            userInfo.Should().NotBeNull();
            userInfo!.Username.Should().Be(username);
            userInfo.Email.Should().Be(email);
            userInfo.FullName.Should().Be(fullName);
        }

        [Fact]
        public void GetCurrentUser_WithNoAuthenticatedUser_ShouldReturnUnauthorized()
        {
            // Arrange - No authenticated user setup

            // Act
            var result = _controller.GetCurrentUser();

            // Assert
            result.Should().NotBeNull();
            var actionResult = result.Result as UnauthorizedObjectResult;
            actionResult.Should().NotBeNull();
            actionResult!.StatusCode.Should().Be(401);
        }

        #endregion

        #region RefreshToken Tests

        [Fact]
        public void RefreshToken_WithAuthenticatedUser_ShouldReturnNewToken()
        {
            // Arrange
            var username = "testuser";
            var newToken = "new-jwt-token";
            var newRefreshToken = "new-refresh-token";

            SetupAuthenticatedUser(username);

            _jwtServiceMock.Setup(x => x.GenerateToken(It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(newToken);
            _jwtServiceMock.Setup(x => x.GenerateRefreshToken())
                .Returns(newRefreshToken);

            // Act
            var result = _controller.RefreshToken();

            // Assert
            result.Should().NotBeNull();
            var actionResult = result as OkObjectResult;
            actionResult.Should().NotBeNull();
            actionResult!.StatusCode.Should().Be(200);

            var response = actionResult.Value;
            response.Should().NotBeNull();
            // 检查响应包含新的token
            var tokenProperty = response!.GetType().GetProperty("token");
            tokenProperty?.GetValue(response).Should().Be(newToken);
        }

        [Fact]
        public void RefreshToken_WithNoAuthenticatedUser_ShouldReturnUnauthorized()
        {
            // Arrange - No authenticated user setup

            // Act
            var result = _controller.RefreshToken();

            // Assert
            result.Should().NotBeNull();
            var actionResult = result as UnauthorizedObjectResult;
            actionResult.Should().NotBeNull();
            actionResult!.StatusCode.Should().Be(401);
        }

        #endregion

        #region ChangeSysAdminPassword Tests

        [Fact]
        public async Task ChangeSysAdminPassword_WithValidInput_ShouldReturnSuccess()
        {
            // Arrange
            var dto = new ChangeSysAdminPasswordDto
            {
                OldPassword = "oldpassword",
                NewPassword = "newpassword123"
            };

            _authServiceMock.Setup(x => x.ChangeSysAdminPasswordAsync(dto))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.ChangeSysAdminPassword(dto);

            // Assert
            result.Should().NotBeNull();
            var actionResult = result as OkObjectResult;
            actionResult.Should().NotBeNull();
            actionResult!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task ChangeSysAdminPassword_WithInvalidOldPassword_ShouldReturnBadRequest()
        {
            // Arrange
            var dto = new ChangeSysAdminPasswordDto
            {
                OldPassword = "wrongoldpassword",
                NewPassword = "newpassword123"
            };

            _authServiceMock.Setup(x => x.ChangeSysAdminPasswordAsync(dto))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.ChangeSysAdminPassword(dto);

            // Assert
            result.Should().NotBeNull();
            var actionResult = result as BadRequestObjectResult;
            actionResult.Should().NotBeNull();
            actionResult!.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task ChangeSysAdminPassword_WithNullInput_ShouldReturnBadRequest()
        {
            // Act
            var result = await _controller.ChangeSysAdminPassword(null!);

            // Assert
            result.Should().NotBeNull();
            var actionResult = result as BadRequestObjectResult;
            actionResult.Should().NotBeNull();
            actionResult!.StatusCode.Should().Be(400);
        }

        #endregion

        #region ChangePassword Tests

        [Fact]
        public async Task ChangePassword_WithValidInput_ShouldReturnSuccess()
        {
            // Arrange
            var username = "testuser";
            var dto = new ChangePasswordRequestDto
            {
                OldPassword = "oldpassword",
                NewPassword = "newpassword123"
            };

            SetupAuthenticatedUser(username);

            _authServiceMock.Setup(x => x.ChangePasswordAsync(username, dto.OldPassword, dto.NewPassword))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.ChangePassword(dto);

            // Assert
            result.Should().NotBeNull();
            var actionResult = result as OkObjectResult;
            actionResult.Should().NotBeNull();
            actionResult!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task ChangePassword_WithNoAuthenticatedUser_ShouldReturnUnauthorized()
        {
            // Arrange
            var dto = new ChangePasswordRequestDto
            {
                OldPassword = "oldpassword",
                NewPassword = "newpassword123"
            };

            // Act
            var result = await _controller.ChangePassword(dto);

            // Assert
            result.Should().NotBeNull();
            var actionResult = result as UnauthorizedObjectResult;
            actionResult.Should().NotBeNull();
            actionResult!.StatusCode.Should().Be(401);
        }

        [Fact]
        public async Task ChangePassword_WithInvalidOldPassword_ShouldReturnBadRequest()
        {
            // Arrange
            var username = "testuser";
            var dto = new ChangePasswordRequestDto
            {
                OldPassword = "wrongoldpassword",
                NewPassword = "newpassword123"
            };

            SetupAuthenticatedUser(username);

            _authServiceMock.Setup(x => x.ChangePasswordAsync(username, dto.OldPassword, dto.NewPassword))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.ChangePassword(dto);

            // Assert
            result.Should().NotBeNull();
            var actionResult = result as BadRequestObjectResult;
            actionResult.Should().NotBeNull();
            actionResult!.StatusCode.Should().Be(400);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 设置认证用户上下文
        /// </summary>
        private void SetupAuthenticatedUser(string username, string? userId = null, string? email = null, string? fullName = null)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, username),
                new(ClaimTypes.NameIdentifier, userId ?? "123"),
                new(ClaimTypes.Email, email ?? "test@example.com"),
                new("FullName", fullName ?? "测试用户")
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                User = principal
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        #endregion
    }

    #region Test Data Classes

    /// <summary>
    /// 测试用的密码修改请求DTO
    /// </summary>
    public class ChangePasswordRequestDto
    {
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    #endregion
}
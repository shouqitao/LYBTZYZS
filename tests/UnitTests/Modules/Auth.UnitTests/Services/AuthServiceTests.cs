using FluentAssertions;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Services;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using Moq;
using Xunit;

namespace LYBT.Module.Auth.Tests.Services
{
    /// <summary>
    /// AuthService 单元测试 - 100%方法覆盖
    /// </summary>
    public class AuthServiceTests
    {
        private readonly Mock<IAuthQueryService> _mockQueryService;
        private readonly Mock<IAuthBusinessService> _mockBusinessService;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _mockQueryService = new Mock<IAuthQueryService>();
            _mockBusinessService = new Mock<IAuthBusinessService>();
            _authService = new AuthService(_mockQueryService.Object, _mockBusinessService.Object);
        }

        #region VerifyCredentialsAsync Tests

        [Fact]
        public async Task VerifyCredentialsAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var request = new LoginRequest { Username = "test", Password = "pass" };
            var expectedResult = ServiceResult<string>.Success("token");
            _mockBusinessService.Setup(x => x.VerifyCredentialsAsync(request)).ReturnsAsync(expectedResult);

            // Act
            var result = await _authService.VerifyCredentialsAsync(request);

            // Assert
            result.Should().Be(expectedResult);
            _mockBusinessService.Verify(x => x.VerifyCredentialsAsync(request), Times.Once);
        }

        #endregion

        #region ChangeSysAdminPasswordAsync Tests

        [Fact]
        public async Task ChangeSysAdminPasswordAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var request = new ChangeSysAdminPassword { NewPassword = "newPass" };
            var expectedResult = ServiceResult<bool>.Success(true);
            _mockBusinessService.Setup(x => x.ChangeSysAdminPasswordAsync("newPass")).ReturnsAsync(expectedResult);

            // Act
            var result = await _authService.ChangeSysAdminPasswordAsync(request);

            // Assert
            result.Should().Be(expectedResult);
            _mockBusinessService.Verify(x => x.ChangeSysAdminPasswordAsync("newPass"), Times.Once);
        }

        #endregion

        #region LoginAsync Tests

        [Fact]
        public async Task LoginAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var request = new LoginRequest { Username = "user", Password = "pass" };
            var expectedResult = ServiceResult<LoginResponse>.Success(new LoginResponse());
            _mockBusinessService.Setup(x => x.ProcessLoginAsync(request)).ReturnsAsync(expectedResult);

            // Act
            var result = await _authService.LoginAsync(request);

            // Assert
            result.Should().Be(expectedResult);
            _mockBusinessService.Verify(x => x.ProcessLoginAsync(request), Times.Once);
        }

        #endregion

        #region LogoutAsync Tests

        [Fact]
        public async Task LogoutAsync_Should_Delegate_To_BusinessService()
        {
            // Arrange
            var request = new LogoutRequest { Username = "testuser" };
            var expectedResult = ServiceResult<bool>.Success(true);
            _mockBusinessService.Setup(x => x.ProcessLogoutAsync(request)).ReturnsAsync(expectedResult);

            // Act
            var result = await _authService.LogoutAsync(request);

            // Assert
            result.Should().Be(expectedResult);
            _mockBusinessService.Verify(x => x.ProcessLogoutAsync(request), Times.Once);
        }

        #endregion

        #region ValidateTokenAsync Tests

        [Fact]
        public async Task ValidateTokenAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var token = "test_token";
            var expectedResult = ServiceResult<bool>.Success(true);
            _mockQueryService.Setup(x => x.ValidateTokenAsync(token)).ReturnsAsync(expectedResult);

            // Act
            var result = await _authService.ValidateTokenAsync(token);

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.ValidateTokenAsync(token), Times.Once);
        }

        #endregion

        #region GetSessionInfoAsync Tests

        [Fact]
        public async Task GetSessionInfoAsync_Should_Delegate_To_QueryService()
        {
            // Arrange
            var token = "test_token";
            var expectedResult = ServiceResult<object>.Success(new { });
            _mockQueryService.Setup(x => x.GetSessionInfoAsync(token)).ReturnsAsync(expectedResult);

            // Act
            var result = await _authService.GetSessionInfoAsync(token);

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.GetSessionInfoAsync(token), Times.Once);
        }

        #endregion

        #region RefreshTokenAsync Tests

        [Fact]
        public async Task RefreshTokenAsync_Should_Return_Failure()
        {
            // Arrange
            var refreshToken = "refresh_token";

            // Act
            var result = await _authService.RefreshTokenAsync(refreshToken);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Be("请重新登录以获取新的访问令牌");
        }

        #endregion
    }
}
using System;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Services;
using LYBT.WPF.Client.Services.Interfaces;
using Moq;
using Xunit;
using LYBT.Shared.Models.Auth;

namespace LYBT.WPF.Client.Tests.Services
{
    /// <summary>
    /// 身份认证服务前端单元测试
    /// 测试核心认证功能的基本行为
    /// </summary>
    public class AuthenticationServiceTests
    {
        private readonly Mock<IAuthApiService> _mockAuthApiService;
        private readonly Mock<ITokenManager> _mockTokenManager;
        private readonly AuthenticationService _service;

        public AuthenticationServiceTests()
        {
            _mockAuthApiService = new Mock<IAuthApiService>();
            _mockTokenManager = new Mock<ITokenManager>();
            _service = new AuthenticationService(_mockAuthApiService.Object, _mockTokenManager.Object);
        }

        #region IsLoggedIn Property Tests

        [Fact]
        public void IsLoggedIn_InitialState_ReturnsFalse()
        {
            // Act
            var result = _service.IsLoggedIn;

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region ClearAuthInfo Tests

        [Fact]
        public void ClearAuthInfo_ClearsAllAuthenticationState()
        {
            // Act
            _service.ClearAuthInfo();

            // Assert
            _mockTokenManager.Verify(x => x.ClearToken(), Times.Once);
            _service.IsLoggedIn.Should().BeFalse();
        }

        #endregion

        #region GetToken Tests

        [Fact]
        public void GetToken_WhenTokenExists_ReturnsToken()
        {
            // Arrange
            const string expectedToken = "test-token";
            _mockTokenManager.Setup(x => x.GetToken())
                .Returns(expectedToken);

            // Act
            var result = _service.GetToken();

            // Assert
            result.Should().Be(expectedToken);
        }

        [Fact]
        public void GetToken_WhenNoToken_ReturnsNull()
        {
            // Arrange
            _mockTokenManager.Setup(x => x.GetToken())
                .Returns((string?)null);

            // Act
            var result = _service.GetToken();

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region CheckConnectionAsync Tests

        [Fact]
        public async Task CheckConnectionAsync_DoesNotThrowException()
        {
            // Act & Assert
            // 确保方法不会抛出异常（即使网络不可用）
            Func<Task> act = async () => await _service.CheckConnectionAsync();
            
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task CheckConnectionAsync_ReturnsBoolean()
        {
            // Act
            var result = await _service.CheckConnectionAsync();
            
            // Assert
            // 结果应该是boolean，不应该抛异常
            Assert.IsType<bool>(result);
        }

        #endregion

        #region GetCurrentUserAsync Tests

        [Fact]
        public async Task GetCurrentUserAsync_WhenNotLoggedIn_ReturnsNull()
        {
            // Arrange - 确保未登录状态
            _service.ClearAuthInfo();

            // Act
            var result = await _service.GetCurrentUserAsync();

            // Assert
            result.Should().BeNull();
        }

        #endregion
    }
}
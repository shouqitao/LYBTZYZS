using System;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Module.Auth.Services;
using LYBT.Module.Auth.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Core;
using LYBT.Entities.Users;
using Moq;
using Xunit;

namespace LYBT.Module.Auth.Tests.Services
{
    /// <summary>
    /// AuthService UltraThink委托模式测试
    /// 验证纯委托模式的正确性：Service → QueryService/BusinessService
    /// </summary>
    public class AuthServiceUltraThinkTests
    {
        private readonly AuthService _authService;
        private readonly Mock<IAuthQueryService> _mockQueryService;
        private readonly Mock<IAuthBusinessService> _mockBusinessService;

        public AuthServiceUltraThinkTests()
        {
            _mockQueryService = new Mock<IAuthQueryService>();
            _mockBusinessService = new Mock<IAuthBusinessService>();
            _authService = new AuthService(_mockQueryService.Object, _mockBusinessService.Object);
        }

        #region 构造函数测试

        [Fact]
        public void Constructor_WithNullQueryService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action action = () => new AuthService(null, _mockBusinessService.Object);
            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("queryService");
        }

        [Fact]
        public void Constructor_WithNullBusinessService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action action = () => new AuthService(_mockQueryService.Object, null);
            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("businessService");
        }

        [Fact]
        public void Constructor_WithValidServices_InitializesSuccessfully()
        {
            // Act & Assert
            var service = new AuthService(_mockQueryService.Object, _mockBusinessService.Object);
            service.Should().NotBeNull();
        }

        #endregion

        #region Query Operations Delegation Tests

        [Fact]
        public async Task ValidateTokenAsync_DelegatesToQueryService()
        {
            // Arrange
            var token = "valid-jwt-token";
            var expectedResult = ServiceResult<bool>.Success(true);
            
            _mockQueryService.Setup(x => x.ValidateTokenAsync(token))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _authService.ValidateTokenAsync(token);

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.ValidateTokenAsync(token), Times.Once);
        }

        [Fact]
        public async Task GetSessionInfoAsync_DelegatesToQueryService()
        {
            // Arrange
            var token = "valid-jwt-token";
            var sessionInfo = new { UserId = "123", Username = "testuser" };
            var expectedResult = ServiceResult<object>.Success(sessionInfo);
            
            _mockQueryService.Setup(x => x.GetSessionInfoAsync(token))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _authService.GetSessionInfoAsync(token);

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.GetSessionInfoAsync(token), Times.Once);
        }

        #endregion

        #region Business Operations Delegation Tests

        [Fact]
        public async Task LoginAsync_DelegatesToBusinessService()
        {
            // Arrange
            var loginRequest = new LoginRequest 
            { 
                Username = "testuser", 
                Password = "password123",
                RememberMe = false
            };
            var loginResponse = new LoginResponse 
            { 
                Token = "jwt-token",
                User = new UserDto { Username = "testuser" },
                ExpiresAt = DateTime.UtcNow.AddHours(8)
            };
            var expectedResult = ServiceResult<LoginResponse>.Success(loginResponse);
            
            _mockBusinessService.Setup(x => x.ProcessLoginAsync(loginRequest))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _authService.LoginAsync(loginRequest);

            // Assert
            result.Should().Be(expectedResult);
            _mockBusinessService.Verify(x => x.ProcessLoginAsync(loginRequest), Times.Once);
        }

        [Fact]
        public async Task LogoutAsync_DelegatesToBusinessService()
        {
            // Arrange
            var logoutRequest = new LogoutRequest { Username = "testuser" };
            var expectedResult = ServiceResult<bool>.Success(true);
            
            _mockBusinessService.Setup(x => x.ProcessLogoutAsync(logoutRequest))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _authService.LogoutAsync(logoutRequest);

            // Assert
            result.Should().Be(expectedResult);
            _mockBusinessService.Verify(x => x.ProcessLogoutAsync(logoutRequest), Times.Once);
        }

        [Fact]
        public async Task VerifyCredentialsAsync_DelegatesToBusinessService()
        {
            // Arrange
            var loginRequest = new LoginRequest 
            { 
                Username = "testuser", 
                Password = "password123" 
            };
            var expectedResult = ServiceResult<string>.Success("凭据验证成功");
            
            _mockBusinessService.Setup(x => x.VerifyCredentialsAsync(loginRequest))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _authService.VerifyCredentialsAsync(loginRequest);

            // Assert
            result.Should().Be(expectedResult);
            _mockBusinessService.Verify(x => x.VerifyCredentialsAsync(loginRequest), Times.Once);
        }

        [Fact]
        public async Task ChangeSysAdminPasswordAsync_DelegatesToBusinessService()
        {
            // Arrange
            var changeSysAdminPassword = new ChangeSysAdminPassword { NewPassword = "newpassword123" };
            var expectedResult = ServiceResult<bool>.Success(true);
            
            _mockBusinessService.Setup(x => x.ChangeSysAdminPasswordAsync(changeSysAdminPassword.NewPassword))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _authService.ChangeSysAdminPasswordAsync(changeSysAdminPassword);

            // Assert
            result.Should().Be(expectedResult);
            _mockBusinessService.Verify(x => x.ChangeSysAdminPasswordAsync(changeSysAdminPassword.NewPassword), Times.Once);
        }

        #endregion

        #region RefreshToken Special Case Tests

        [Fact]
        public async Task RefreshTokenAsync_ReturnsFailureResult_UltraThinkSimplified()
        {
            // Arrange
            var refreshToken = "refresh-token";

            // Act
            var result = await _authService.RefreshTokenAsync(refreshToken);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("请重新登录以获取新的访问令牌");
            
            // 验证没有调用任何依赖服务（因为这是内置逻辑）
            _mockQueryService.VerifyNoOtherCalls();
            _mockBusinessService.VerifyNoOtherCalls();
        }

        #endregion

        #region Edge Case Tests

        [Fact]
        public async Task LoginAsync_WithNullRequest_DelegatesToBusinessService()
        {
            // Arrange
            LoginRequest nullRequest = null;
            var expectedResult = ServiceResult<LoginResponse>.Failure("登录请求不能为空");
            
            _mockBusinessService.Setup(x => x.ProcessLoginAsync(nullRequest))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _authService.LoginAsync(nullRequest);

            // Assert
            result.Should().Be(expectedResult);
            _mockBusinessService.Verify(x => x.ProcessLoginAsync(nullRequest), Times.Once);
        }

        [Fact]
        public async Task ValidateTokenAsync_WithEmptyToken_DelegatesToQueryService()
        {
            // Arrange
            var emptyToken = "";
            var expectedResult = ServiceResult<bool>.Failure("Token不能为空");
            
            _mockQueryService.Setup(x => x.ValidateTokenAsync(emptyToken))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _authService.ValidateTokenAsync(emptyToken);

            // Assert
            result.Should().Be(expectedResult);
            _mockQueryService.Verify(x => x.ValidateTokenAsync(emptyToken), Times.Once);
        }

        #endregion

        #region 架构验证测试

        [Fact]
        public void Service_FollowsUltraThinkDelegationPattern()
        {
            // 验证Service类遵循纯委托模式
            var serviceType = typeof(AuthService);
            
            // 1. 验证构造函数只依赖QueryService和BusinessService
            var constructor = serviceType.GetConstructors()[0];
            var parameters = constructor.GetParameters();
            
            parameters.Should().HaveCount(2);
            parameters[0].ParameterType.Name.Should().Be("IAuthQueryService");
            parameters[1].ParameterType.Name.Should().Be("IAuthBusinessService");
        }

        [Fact]
        public void Service_HasCorrectDependencyInjection()
        {
            // 验证依赖注入的正确性
            var service = new AuthService(_mockQueryService.Object, _mockBusinessService.Object);
            service.Should().NotBeNull();
            
            // 验证服务不为null说明构造函数正确处理了依赖
            _mockQueryService.Should().NotBeNull();
            _mockBusinessService.Should().NotBeNull();
        }

        [Fact]
        public void Service_ImplementsCorrectInterface()
        {
            // 验证AuthService实现了IAuthService接口
            var serviceType = typeof(AuthService);
            var interfaceType = typeof(LYBT.Shared.Interfaces.Services.IAuthService);
            
            serviceType.GetInterfaces().Should().Contain(interfaceType);
        }

        #endregion

        #region Method Coverage Verification Tests

        [Fact]
        public void Service_HasAllRequiredMethods()
        {
            // 验证AuthService包含所有必需的方法
            var serviceType = typeof(AuthService);
            var methods = serviceType.GetMethods();
            
            // 验证关键方法存在
            methods.Should().Contain(m => m.Name == "LoginAsync");
            methods.Should().Contain(m => m.Name == "LogoutAsync");
            methods.Should().Contain(m => m.Name == "ValidateTokenAsync");
            methods.Should().Contain(m => m.Name == "GetSessionInfoAsync");
            methods.Should().Contain(m => m.Name == "VerifyCredentialsAsync");
            methods.Should().Contain(m => m.Name == "ChangeSysAdminPasswordAsync");
            methods.Should().Contain(m => m.Name == "RefreshTokenAsync");
        }

        #endregion

        #region Integration Pattern Tests

        [Fact]
        public async Task CompleteAuthenticationFlow_DelegatesCorrectly()
        {
            // Arrange - 模拟完整认证流程
            var loginRequest = new LoginRequest 
            { 
                Username = "testuser", 
                Password = "password123",
                RememberMe = true
            };
            
            var loginResponse = new LoginResponse 
            { 
                Token = "jwt-token-123",
                User = new UserDto { Id = Guid.NewGuid(), Username = "testuser", Role = "Doctor" },
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };
            
            var sessionInfo = new 
            { 
                UserId = loginResponse.User.Id.ToString(),
                Username = "testuser",
                Role = "Doctor",
                IsAuthenticated = true
            };
            
            // Setup mocks
            _mockBusinessService.Setup(x => x.ProcessLoginAsync(loginRequest))
                .ReturnsAsync(ServiceResult<LoginResponse>.Success(loginResponse));
                
            _mockQueryService.Setup(x => x.ValidateTokenAsync(loginResponse.Token))
                .ReturnsAsync(ServiceResult<bool>.Success(true));
                
            _mockQueryService.Setup(x => x.GetSessionInfoAsync(loginResponse.Token))
                .ReturnsAsync(ServiceResult<object>.Success(sessionInfo));

            // Act - 执行完整流程
            var loginResult = await _authService.LoginAsync(loginRequest);
            var validateResult = await _authService.ValidateTokenAsync(loginResponse.Token);
            var sessionResult = await _authService.GetSessionInfoAsync(loginResponse.Token);

            // Assert - 验证每步都正确委托
            loginResult.IsSuccess.Should().BeTrue();
            loginResult.Data.Should().Be(loginResponse);
            
            validateResult.IsSuccess.Should().BeTrue();
            validateResult.Data.Should().BeTrue();
            
            sessionResult.IsSuccess.Should().BeTrue();
            sessionResult.Data.Should().Be(sessionInfo);

            // Verify all delegations occurred exactly once
            _mockBusinessService.Verify(x => x.ProcessLoginAsync(loginRequest), Times.Once);
            _mockQueryService.Verify(x => x.ValidateTokenAsync(loginResponse.Token), Times.Once);
            _mockQueryService.Verify(x => x.GetSessionInfoAsync(loginResponse.Token), Times.Once);
        }

        #endregion
    }
}
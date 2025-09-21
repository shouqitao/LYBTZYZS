using System;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Configuration.Options;
using LYBT.Module.Auth.Interfaces;
using LYBT.Module.Auth.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace LYBT.Module.Auth.Tests.Services
{
    /// <summary>
    /// AuthQueryService 完整单元测试
    /// 职责：用户查询、Token验证、会话信息获取
    /// </summary>
    public class AuthQueryServiceTests
    {
        private readonly AuthQueryService _authQueryService;
        private readonly Mock<IAuthRepository> _mockAuthRepository;
        private readonly Mock<IJwtAuthenticationService> _mockJwtAuthenticationService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<AuthQueryService>> _mockLogger;
        private readonly Mock<ISysAdminHandler> _mockSysAdminHandler;
        private readonly Mock<IOptions<SysAdminOptions>> _mockSysAdminOptions;
        private readonly SysAdminOptions _sysAdminOptions;

        public AuthQueryServiceTests()
        {
            _mockAuthRepository = new Mock<IAuthRepository>();
            _mockJwtAuthenticationService = new Mock<IJwtAuthenticationService>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<AuthQueryService>>();
            _mockSysAdminHandler = new Mock<ISysAdminHandler>();
            _mockSysAdminOptions = new Mock<IOptions<SysAdminOptions>>();

            _sysAdminOptions = new SysAdminOptions
            {
                Username = "sysadmin",
                DefaultPassword = "LybtAdmin2025@SecurePass!"
            };

            _mockSysAdminOptions.Setup(x => x.Value).Returns(_sysAdminOptions);

            _authQueryService = new AuthQueryService(
                _mockAuthRepository.Object,
                _mockJwtAuthenticationService.Object,
                _mockMapper.Object,
                _mockLogger.Object,
                _mockSysAdminHandler.Object,
                _mockSysAdminOptions.Object);
        }

        #region 构造函数测试

        [Fact]
        public void Constructor_Should_Throw_When_AuthRepository_Is_Null()
        {
            // Act & Assert
            var action = () => new AuthQueryService(
                null!,
                _mockJwtAuthenticationService.Object,
                _mockMapper.Object,
                _mockLogger.Object,
                _mockSysAdminHandler.Object,
                _mockSysAdminOptions.Object);

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("authRepository");
        }

        [Fact]
        public void Constructor_Should_Throw_When_JwtAuthenticationService_Is_Null()
        {
            // Act & Assert
            var action = () => new AuthQueryService(
                _mockAuthRepository.Object,
                null!,
                _mockMapper.Object,
                _mockLogger.Object,
                _mockSysAdminHandler.Object,
                _mockSysAdminOptions.Object);

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("jwtAuthenticationService");
        }

        [Fact]
        public void Constructor_Should_Throw_When_Mapper_Is_Null()
        {
            // Act & Assert
            var action = () => new AuthQueryService(
                _mockAuthRepository.Object,
                _mockJwtAuthenticationService.Object,
                null!,
                _mockLogger.Object,
                _mockSysAdminHandler.Object,
                _mockSysAdminOptions.Object);

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("mapper");
        }

        [Fact]
        public void Constructor_Should_Throw_When_Logger_Is_Null()
        {
            // Act & Assert
            var action = () => new AuthQueryService(
                _mockAuthRepository.Object,
                _mockJwtAuthenticationService.Object,
                _mockMapper.Object,
                null!,
                _mockSysAdminHandler.Object,
                _mockSysAdminOptions.Object);

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Fact]
        public void Constructor_Should_Throw_When_SysAdminHandler_Is_Null()
        {
            // Act & Assert
            var action = () => new AuthQueryService(
                _mockAuthRepository.Object,
                _mockJwtAuthenticationService.Object,
                _mockMapper.Object,
                _mockLogger.Object,
                null!,
                _mockSysAdminOptions.Object);

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("sysAdminHandler");
        }

        [Fact]
        public void Constructor_Should_Throw_When_SysAdminOptions_Is_Null()
        {
            // Act & Assert
            var action = () => new AuthQueryService(
                _mockAuthRepository.Object,
                _mockJwtAuthenticationService.Object,
                _mockMapper.Object,
                _mockLogger.Object,
                _mockSysAdminHandler.Object,
                null!);

            action.Should().Throw<ArgumentNullException>()
                .WithParameterName("sysAdminOptions");
        }

        #endregion

        #region GetUserForAuthenticationAsync 测试

        [Fact]
        public async Task GetUserForAuthenticationAsync_Should_Return_User_When_Valid()
        {
            // Arrange
            var username = "testuser";
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                RealName = "测试用户",
                Role = UserRole.Doctor
            };

            _mockSysAdminHandler.Setup(x => x.IsSysAdmin(username)).Returns(false);
            _mockAuthRepository.Setup(x => x.GetByUsernameAsync(username)).ReturnsAsync(user);

            // Act
            var result = await _authQueryService.GetUserForAuthenticationAsync(username);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Username.Should().Be(username);
            result.Data.RealName.Should().Be("测试用户");
        }

        [Fact]
        public async Task GetUserForAuthenticationAsync_Should_Return_Failure_When_Username_Is_Empty()
        {
            // Act
            var result = await _authQueryService.GetUserForAuthenticationAsync("");

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("用户名不能为空");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetUserForAuthenticationAsync_Should_Return_Failure_When_Username_Is_Invalid(string? username)
        {
            // Act
            var result = await _authQueryService.GetUserForAuthenticationAsync(username!);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("用户名不能为空");
        }

        [Fact]
        public async Task GetUserForAuthenticationAsync_Should_Return_Failure_When_User_Is_SysAdmin()
        {
            // Arrange
            var username = "sysadmin";
            _mockSysAdminHandler.Setup(x => x.IsSysAdmin(username)).Returns(true);

            // Act
            var result = await _authQueryService.GetUserForAuthenticationAsync(username);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("超级管理员走独立认证流程，此方法不处理");
            _mockAuthRepository.Verify(x => x.GetByUsernameAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetUserForAuthenticationAsync_Should_Return_Failure_When_User_Not_Found()
        {
            // Arrange
            var username = "nonexistentuser";
            _mockSysAdminHandler.Setup(x => x.IsSysAdmin(username)).Returns(false);
            _mockAuthRepository.Setup(x => x.GetByUsernameAsync(username)).ReturnsAsync((User?)null);

            // Act
            var result = await _authQueryService.GetUserForAuthenticationAsync(username);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("用户不存在");
        }

        [Fact]
        public async Task GetUserForAuthenticationAsync_Should_Handle_Repository_Exception()
        {
            // Arrange
            var username = "testuser";
            _mockSysAdminHandler.Setup(x => x.IsSysAdmin(username)).Returns(false);
            _mockAuthRepository.Setup(x => x.GetByUsernameAsync(username))
                .ThrowsAsync(new Exception("数据库连接失败"));

            // Act
            var result = await _authQueryService.GetUserForAuthenticationAsync(username);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("获取用户信息失败");
        }

        #endregion

        #region ValidateTokenAsync 测试

        [Fact]
        public async Task ValidateTokenAsync_Should_Return_True_When_Token_Is_Valid()
        {
            // Arrange
            var token = "valid-jwt-token";
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity("test"));

            _mockJwtAuthenticationService.Setup(x => x.ValidateToken(token)).Returns(claimsPrincipal);

            // Act
            var result = await _authQueryService.ValidateTokenAsync(token);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateTokenAsync_Should_Return_False_When_Token_Is_Invalid()
        {
            // Arrange
            var token = "invalid-token";
            _mockJwtAuthenticationService.Setup(x => x.ValidateToken(token)).Returns((ClaimsPrincipal?)null);

            // Act
            var result = await _authQueryService.ValidateTokenAsync(token);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeFalse();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task ValidateTokenAsync_Should_Return_Failure_When_Token_Is_Empty(string? token)
        {
            // Act
            var result = await _authQueryService.ValidateTokenAsync(token!);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Token不能为空");
        }

        [Fact]
        public async Task ValidateTokenAsync_Should_Handle_JwtService_Exception()
        {
            // Arrange
            var token = "problematic-token";
            _mockJwtAuthenticationService.Setup(x => x.ValidateToken(token))
                .Throws(new Exception("JWT解析失败"));

            // Act
            var result = await _authQueryService.ValidateTokenAsync(token);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Token验证失败");
        }

        #endregion

        #region GetSessionInfoAsync 测试

        [Fact]
        public async Task GetSessionInfoAsync_Should_Return_SessionInfo_When_Valid()
        {
            // Arrange
            var token = "valid-token";
            var userId = Guid.NewGuid().ToString();
            var userDto = new UserDto
            {
                Id = Guid.Parse(userId),
                Username = "testuser",
                RealName = "测试用户",
                Role = UserRole.Doctor
            };

            // Mock token validation
            _mockJwtAuthenticationService.Setup(x => x.ValidateToken(token))
                .Returns(new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim("sub", userId)
                })));

            // Mock get current user
            _mockAuthRepository.Setup(x => x.GetByIdAsync(Guid.Parse(userId)))
                .ReturnsAsync(new User { Id = Guid.Parse(userId), Username = "testuser" });
            _mockMapper.Setup(x => x.Map<UserDto>(It.IsAny<User>())).Returns(userDto);

            // Act
            var result = await _authQueryService.GetSessionInfoAsync(token);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task GetSessionInfoAsync_Should_Return_Failure_When_Token_Is_Empty()
        {
            // Act
            var result = await _authQueryService.GetSessionInfoAsync("");

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Token不能为空");
        }

        [Fact]
        public async Task GetSessionInfoAsync_Should_Return_Failure_When_Token_Validation_Fails()
        {
            // Arrange
            var token = "invalid-token";
            _mockJwtAuthenticationService.Setup(x => x.ValidateToken(token)).Returns((ClaimsPrincipal?)null);

            // Act
            var result = await _authQueryService.GetSessionInfoAsync(token);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Token验证失败");
        }

        [Fact]
        public async Task GetSessionInfoAsync_Should_Return_Failure_When_Cannot_Extract_UserId()
        {
            // Arrange
            var token = "token-without-userid";
            _mockJwtAuthenticationService.Setup(x => x.ValidateToken(token))
                .Returns(new ClaimsPrincipal(new ClaimsIdentity()));

            // Act
            var result = await _authQueryService.GetSessionInfoAsync(token);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("无法从Token中提取用户信息");
        }

        [Fact]
        public async Task GetSessionInfoAsync_Should_Handle_Exception()
        {
            // Arrange
            var token = "valid-token";
            _mockJwtAuthenticationService.Setup(x => x.ValidateToken(token))
                .Throws(new Exception("服务异常"));

            // Act
            var result = await _authQueryService.GetSessionInfoAsync(token);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("获取会话信息失败");
        }

        #endregion

        #region GetCurrentUserAsync 测试

        [Fact]
        public async Task GetCurrentUserAsync_Should_Return_SysAdmin_When_UserId_Is_SysAdmin_Guid()
        {
            // Arrange
            var sysAdminUserId = "00000000-0000-0000-0000-000000000001";

            // Act
            var result = await _authQueryService.GetCurrentUserAsync(sysAdminUserId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Username.Should().Be("sysadmin");
            result.Data.RealName.Should().Be("系统管理员");
            result.Data.Role.Should().Be(UserRole.Admin);
            result.Data.Status.Should().Be(CommonStatus.Enabled);
        }

        [Fact]
        public async Task GetCurrentUserAsync_Should_Return_User_When_UserId_Is_Valid_Guid()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            var user = new User
            {
                Id = Guid.Parse(userId),
                Username = "testuser",
                RealName = "测试用户",
                Role = UserRole.Doctor
            };
            var userDto = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                RealName = user.RealName,
                Role = user.Role
            };

            _mockAuthRepository.Setup(x => x.GetByIdAsync(Guid.Parse(userId))).ReturnsAsync(user);
            _mockMapper.Setup(x => x.Map<UserDto>(user)).Returns(userDto);

            // Act
            var result = await _authQueryService.GetCurrentUserAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Username.Should().Be("testuser");
            result.Data.RealName.Should().Be("测试用户");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task GetCurrentUserAsync_Should_Return_Failure_When_UserId_Is_Empty(string? userId)
        {
            // Act
            var result = await _authQueryService.GetCurrentUserAsync(userId!);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("用户ID不能为空");
        }

        [Fact]
        public async Task GetCurrentUserAsync_Should_Return_Failure_When_UserId_Is_Invalid_Guid()
        {
            // Arrange
            var invalidUserId = "invalid-guid";

            // Act
            var result = await _authQueryService.GetCurrentUserAsync(invalidUserId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("用户ID格式无效");
        }

        [Fact]
        public async Task GetCurrentUserAsync_Should_Return_Failure_When_User_Not_Found()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            _mockAuthRepository.Setup(x => x.GetByIdAsync(Guid.Parse(userId))).ReturnsAsync((User?)null);

            // Act
            var result = await _authQueryService.GetCurrentUserAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("用户不存在");
        }

        [Fact]
        public async Task GetCurrentUserAsync_Should_Handle_Repository_Exception()
        {
            // Arrange
            var userId = Guid.NewGuid().ToString();
            _mockAuthRepository.Setup(x => x.GetByIdAsync(Guid.Parse(userId)))
                .ThrowsAsync(new Exception("数据库异常"));

            // Act
            var result = await _authQueryService.GetCurrentUserAsync(userId);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("获取当前用户失败");
        }

        #endregion

        #region ExtractUserIdFromToken 测试

        [Fact]
        public void ExtractUserIdFromToken_Should_Return_UserId_When_Token_Is_Valid()
        {
            // Arrange
            var token = "valid-token";
            var expectedUserId = "user-123";
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("sub", expectedUserId)
            }));

            _mockJwtAuthenticationService.Setup(x => x.ValidateToken(token)).Returns(claimsPrincipal);

            // Act
            var result = _authQueryService.ExtractUserIdFromToken(token);

            // Assert
            result.Should().Be(expectedUserId);
        }

        [Fact]
        public void ExtractUserIdFromToken_Should_Return_Empty_When_Token_Is_Invalid()
        {
            // Arrange
            var token = "invalid-token";
            _mockJwtAuthenticationService.Setup(x => x.ValidateToken(token)).Returns((ClaimsPrincipal?)null);

            // Act
            var result = _authQueryService.ExtractUserIdFromToken(token);

            // Assert
            result.Should().Be(string.Empty);
        }

        [Fact]
        public void ExtractUserIdFromToken_Should_Return_Empty_When_No_Sub_Claim()
        {
            // Arrange
            var token = "token-without-sub";
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("name", "testuser")
            }));

            _mockJwtAuthenticationService.Setup(x => x.ValidateToken(token)).Returns(claimsPrincipal);

            // Act
            var result = _authQueryService.ExtractUserIdFromToken(token);

            // Assert
            result.Should().Be(string.Empty);
        }

        [Fact]
        public void ExtractUserIdFromToken_Should_Handle_Exception()
        {
            // Arrange
            var token = "problematic-token";
            _mockJwtAuthenticationService.Setup(x => x.ValidateToken(token))
                .Throws(new Exception("JWT异常"));

            // Act
            var result = _authQueryService.ExtractUserIdFromToken(token);

            // Assert
            result.Should().Be(string.Empty);
        }

        #endregion

        #region 边界值和集成测试

        [Fact]
        public async Task GetSessionInfoAsync_Should_Handle_SysAdmin_Token()
        {
            // Arrange
            var token = "sysadmin-token";
            var sysAdminUserId = "00000000-0000-0000-0000-000000000001";

            _mockJwtAuthenticationService.Setup(x => x.ValidateToken(token))
                .Returns(new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim("sub", sysAdminUserId)
                })));

            // Act
            var result = await _authQueryService.GetSessionInfoAsync(token);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        [Fact]
        public void AuthQueryService_Should_Implement_IAuthQueryService()
        {
            // Assert
            _authQueryService.Should().BeAssignableTo<IAuthQueryService>();
        }

        #endregion
    }
}
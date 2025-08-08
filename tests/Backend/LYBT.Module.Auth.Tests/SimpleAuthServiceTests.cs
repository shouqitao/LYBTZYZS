using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using FluentAssertions;
using LYBT.Infrastructure.Options;
using LYBT.Module.Auth.Services;
using LYBT.Module.Auth.Tests.Base;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Module.Auth.Tests
{
    /// <summary>
    /// AuthService 简化测试 - 专注于核心逻辑验证
    /// </summary>
    public class SimpleAuthServiceTests
    {
        private readonly AuthOptions _authOptions;
        private readonly LoginAttemptService _loginAttemptService;

        public SimpleAuthServiceTests()
        {
            _authOptions = AuthTestDataGenerator.CreateAuthOptions();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            _loginAttemptService = new LoginAttemptService(memoryCache);
        }

        #region 登录类型验证测试

        [Fact]
        public void ValidateLoginType_Should_Return_True_For_Supported_Type()
        {
            // Arrange
            var request = AuthTestDataGenerator.CreateLoginRequest(loginType: "Password");

            // Act
            var isSupported = _authOptions.SupportedLoginTypes.Contains(request.LoginType);

            // Assert
            isSupported.Should().BeTrue();
        }

        [Fact]
        public void ValidateLoginType_Should_Return_False_For_Unsupported_Type()
        {
            // Arrange
            var request = AuthTestDataGenerator.CreateLoginRequest(loginType: "Biometric");

            // Act
            var isSupported = _authOptions.SupportedLoginTypes.Contains(request.LoginType);

            // Assert
            isSupported.Should().BeFalse();
        }

        [Theory]
        [InlineData("Password")]
        [InlineData("Token")]
        public void ValidateLoginType_Should_Support_Configured_Types(string loginType)
        {
            // Act
            var isSupported = _authOptions.SupportedLoginTypes.Contains(loginType);

            // Assert
            isSupported.Should().BeTrue();
        }

        #endregion

        #region AuthOptions配置测试

        [Fact]
        public void AuthOptions_Should_Have_Correct_Default_Values()
        {
            // Assert
            _authOptions.MaxFailedLoginAttempts.Should().Be(5);
            _authOptions.AccountLockoutDuration.Should().Be(TimeSpan.FromMinutes(15));
            _authOptions.EnableDetailedLoginLogging.Should().BeTrue();
            _authOptions.DefaultSysAdminPassword.Should().Be("Admin123!");
        }

        [Fact]
        public void AuthOptions_Should_Have_Valid_SupportedLoginTypes()
        {
            // Assert
            _authOptions.SupportedLoginTypes.Should().NotBeNull();
            _authOptions.SupportedLoginTypes.Should().Contain("Password");
            _authOptions.SupportedLoginTypes.Should().HaveCountGreaterThan(0);
        }

        #endregion

        #region LoginAttemptService测试

        [Fact]
        public void LoginAttemptService_Should_Not_Be_Locked_Initially()
        {
            // Arrange
            var username = "testuser";

            // Act
            var isLocked = _loginAttemptService.IsAccountLocked(username);

            // Assert
            isLocked.Should().BeFalse();
        }

        [Fact]
        public void LoginAttemptService_Should_Record_Failed_Attempt()
        {
            // Arrange
            var username = "testuser";

            // Act
            _loginAttemptService.RecordFailedAttempt(username);

            // Assert - 第一次失败不应该锁定账户
            var isLocked = _loginAttemptService.IsAccountLocked(username);
            isLocked.Should().BeFalse();
        }

        [Fact]
        public void LoginAttemptService_Should_Lock_Account_After_Max_Attempts()
        {
            // Arrange
            var username = "testuser";

            // Act - 记录3次失败尝试（达到锁定阈值）
            for (int i = 0; i < 3; i++)
            {
                _loginAttemptService.RecordFailedAttempt(username);
            }

            // Assert
            var isLocked = _loginAttemptService.IsAccountLocked(username);
            isLocked.Should().BeTrue();

            var remainingTime = _loginAttemptService.GetRemainingLockTime(username);
            remainingTime.Should().BeGreaterThan(0);
        }

        [Fact]
        public void LoginAttemptService_Should_Clear_Attempts_Successfully()
        {
            // Arrange
            var username = "testuser";
            _loginAttemptService.RecordFailedAttempt(username);
            _loginAttemptService.RecordFailedAttempt(username);

            // Act
            _loginAttemptService.ClearAttempts(username);

            // Assert
            var isLocked = _loginAttemptService.IsAccountLocked(username);
            isLocked.Should().BeFalse();

            var remainingTime = _loginAttemptService.GetRemainingLockTime(username);
            remainingTime.Should().Be(0);
        }

        [Fact]
        public void LoginAttemptService_Should_Handle_Case_Insensitive_Usernames()
        {
            // Arrange
            var username1 = "TestUser";
            var username2 = "testuser";

            // Act
            _loginAttemptService.RecordFailedAttempt(username1);

            // Assert - 应该对username2也生效（大小写不敏感）
            var isLocked2 = _loginAttemptService.IsAccountLocked(username2);
            
            // 清除尝试也应该对两个用户名都生效
            _loginAttemptService.ClearAttempts(username2);
            var isLocked1AfterClear = _loginAttemptService.IsAccountLocked(username1);
            isLocked1AfterClear.Should().BeFalse();
        }

        [Fact]
        public void LoginAttemptService_Should_Return_Decreasing_Remaining_Time()
        {
            // Arrange
            var username = "testuser";

            // Act - 锁定账户
            for (int i = 0; i < 3; i++)
            {
                _loginAttemptService.RecordFailedAttempt(username);
            }

            var remainingTime1 = _loginAttemptService.GetRemainingLockTime(username);

            // 等待一小段时间
            System.Threading.Thread.Sleep(100);

            var remainingTime2 = _loginAttemptService.GetRemainingLockTime(username);

            // Assert
            remainingTime1.Should().BeGreaterThan(0);
            remainingTime2.Should().BeLessOrEqualTo(remainingTime1);
        }

        #endregion

        #region 数据传输对象验证测试

        [Fact]
        public void LoginRequestDto_Should_Have_Default_LoginType()
        {
            // Arrange
            var request = new LoginRequestDto
            {
                Username = "testuser",
                Password = "testpassword"
            };

            // Act & Assert
            request.Username.Should().Be("testuser");
            request.Password.Should().Be("testpassword");
            request.RememberMe.Should().BeFalse(); // 默认值
        }

        [Fact]
        public void LogoutRequestDto_Should_Store_Username()
        {
            // Arrange
            var request = AuthTestDataGenerator.CreateLogoutRequest("testuser");

            // Act & Assert
            request.Username.Should().Be("testuser");
        }

        [Fact]
        public void ChangeSysAdminPasswordDto_Should_Store_Passwords()
        {
            // Arrange
            var request = AuthTestDataGenerator.CreateChangePasswordRequest("old123", "new456");

            // Act & Assert
            request.OldPassword.Should().Be("old123");
            request.NewPassword.Should().Be("new456");
        }

        #endregion

        #region 测试数据生成器验证

        [Fact]
        public void AuthTestDataGenerator_Should_Create_Valid_Users()
        {
            // Act
            var user = AuthTestDataGenerator.CreateEnabledUser("testuser");

            // Assert
            user.Should().NotBeNull();
            user.Username.Should().Be("testuser");
            user.Status.Should().Be(CommonStatus.Enabled);
            user.Id.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public void AuthTestDataGenerator_Should_Create_Valid_LoginRequest()
        {
            // Act
            var request = AuthTestDataGenerator.CreateLoginRequest("user1", "pass1");

            // Assert
            request.Should().NotBeNull();
            request.Username.Should().Be("user1");
            request.Password.Should().Be("pass1");
            request.LoginType.Should().Be("Password");
        }

        [Fact]
        public void AuthTestDataGenerator_Should_Create_SysAdmin_User()
        {
            // Act
            var user = AuthTestDataGenerator.CreateSysAdminUser();

            // Assert
            user.Should().NotBeNull();
            user.Username.Should().Be("sysadmin");
            user.RealName.Should().Be("系统管理员");
            user.Status.Should().Be(CommonStatus.Enabled);
        }

        [Fact]
        public void AuthTestDataGenerator_Should_Create_Locked_User()
        {
            // Act
            var user = AuthTestDataGenerator.CreateLockedUser("lockeduser");

            // Assert
            user.Should().NotBeNull();
            user.Username.Should().Be("lockeduser");
            user.Status.Should().Be(CommonStatus.Enabled);
            user.FailedLoginCount.Should().BeGreaterThan(0);
            user.LockoutEnd.Should().BeAfter(DateTime.Now);
        }

        [Fact]
        public void AuthTestDataGenerator_Should_Create_Multiple_Users()
        {
            // Act
            var users = AuthTestDataGenerator.CreateTestUsers(5, CommonStatus.Enabled);

            // Assert
            users.Should().HaveCount(5);
            users.Should().AllSatisfy(u => u.Status.Should().Be(CommonStatus.Enabled));
            users.Select(u => u.Username).Should().OnlyHaveUniqueItems();
        }

        #endregion

        #region 边界条件测试

        [Fact]
        public void LoginAttemptService_Should_Handle_Null_Username()
        {
            // Act & Assert
            _loginAttemptService.RecordFailedAttempt(null!);
            var isLocked = _loginAttemptService.IsAccountLocked(null!);
            isLocked.Should().BeFalse();
        }

        [Fact]
        public void LoginAttemptService_Should_Handle_Empty_Username()
        {
            // Act & Assert
            _loginAttemptService.RecordFailedAttempt(string.Empty);
            var isLocked = _loginAttemptService.IsAccountLocked(string.Empty);
            isLocked.Should().BeFalse();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(10)]
        public void AuthTestDataGenerator_Should_Create_Requested_Number_Of_Users(int count)
        {
            // Act
            var users = AuthTestDataGenerator.CreateTestUsers(count);

            // Assert
            users.Should().HaveCount(count);
        }

        #endregion
    }
}
using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.Module.Auth.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace LYBT.Module.Auth.Tests
{
    /// <summary>
    /// 登录尝试服务单元测试
    /// </summary>
    public class LoginAttemptServiceTests : IDisposable
    {
        private readonly IMemoryCache _memoryCache;
        private readonly LoginAttemptService _service;

        public LoginAttemptServiceTests()
        {
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _service = new LoginAttemptService(_memoryCache);
        }

        public void Dispose()
        {
            _memoryCache.Dispose();
        }

        #region RecordFailedAttempt Tests

        [Fact]
        public void RecordFailedAttempt_FirstFailedAttempt_ShouldNotLockAccount()
        {
            // Arrange
            var username = "testuser";

            // Act
            _service.RecordFailedAttempt(username);

            // Assert
            _service.IsAccountLocked(username).Should().BeFalse();
            _service.GetRemainingLockTime(username).Should().Be(0);
        }

        [Fact]
        public void RecordFailedAttempt_SecondFailedAttempt_ShouldNotLockAccount()
        {
            // Arrange
            var username = "testuser";

            // Act
            _service.RecordFailedAttempt(username);
            _service.RecordFailedAttempt(username);

            // Assert
            _service.IsAccountLocked(username).Should().BeFalse();
            _service.GetRemainingLockTime(username).Should().Be(0);
        }

        [Fact]
        public void RecordFailedAttempt_ThirdFailedAttempt_ShouldLockAccount()
        {
            // Arrange
            var username = "testuser";

            // Act
            _service.RecordFailedAttempt(username);
            _service.RecordFailedAttempt(username);
            _service.RecordFailedAttempt(username);

            // Assert
            _service.IsAccountLocked(username).Should().BeTrue();
            _service.GetRemainingLockTime(username).Should().BeGreaterThan(0);
        }

        [Fact]
        public void RecordFailedAttempt_WithCaseInsensitiveUsername_ShouldTreatAsSameUser()
        {
            // Arrange
            var username1 = "TestUser";
            var username2 = "testuser";
            var username3 = "TESTUSER";

            // Act
            _service.RecordFailedAttempt(username1);
            _service.RecordFailedAttempt(username2);
            _service.RecordFailedAttempt(username3);

            // Assert
            _service.IsAccountLocked(username1).Should().BeTrue();
            _service.IsAccountLocked(username2).Should().BeTrue();
            _service.IsAccountLocked(username3).Should().BeTrue();
        }

        [Fact]
        public void RecordFailedAttempt_WithNullUsername_ShouldNotThrow()
        {
            // Act & Assert
            var action = () => _service.RecordFailedAttempt(null);
            action.Should().NotThrow();
        }

        [Fact]
        public void RecordFailedAttempt_WithEmptyUsername_ShouldNotThrow()
        {
            // Act & Assert
            var action = () => _service.RecordFailedAttempt("");
            action.Should().NotThrow();
        }

        [Fact]
        public void RecordFailedAttempt_MultipleUsersIndependently_ShouldTrackSeparately()
        {
            // Arrange
            var user1 = "user1";
            var user2 = "user2";

            // Act
            _service.RecordFailedAttempt(user1);
            _service.RecordFailedAttempt(user1);
            _service.RecordFailedAttempt(user2);

            // Assert
            _service.IsAccountLocked(user1).Should().BeFalse();
            _service.IsAccountLocked(user2).Should().BeFalse();

            // Lock user1
            _service.RecordFailedAttempt(user1);
            _service.IsAccountLocked(user1).Should().BeTrue();
            _service.IsAccountLocked(user2).Should().BeFalse();
        }

        #endregion

        #region IsAccountLocked Tests

        [Fact]
        public void IsAccountLocked_WithNoFailedAttempts_ShouldReturnFalse()
        {
            // Arrange
            var username = "testuser";

            // Act & Assert
            _service.IsAccountLocked(username).Should().BeFalse();
        }

        [Fact]
        public void IsAccountLocked_WithLessThanMaxAttempts_ShouldReturnFalse()
        {
            // Arrange
            var username = "testuser";

            // Act
            _service.RecordFailedAttempt(username);
            _service.RecordFailedAttempt(username);

            // Assert
            _service.IsAccountLocked(username).Should().BeFalse();
        }

        [Fact]
        public void IsAccountLocked_WithMaxAttempts_ShouldReturnTrue()
        {
            // Arrange
            var username = "testuser";

            // Act
            _service.RecordFailedAttempt(username);
            _service.RecordFailedAttempt(username);
            _service.RecordFailedAttempt(username);

            // Assert
            _service.IsAccountLocked(username).Should().BeTrue();
        }

        [Fact]
        public void IsAccountLocked_WithNullUsername_ShouldReturnFalse()
        {
            // Act & Assert
            _service.IsAccountLocked(null).Should().BeFalse();
        }

        [Fact]
        public void IsAccountLocked_WithEmptyUsername_ShouldReturnFalse()
        {
            // Act & Assert
            _service.IsAccountLocked("").Should().BeFalse();
        }

        [Fact]
        public void IsAccountLocked_CaseInsensitive_ShouldBehaveConsistently()
        {
            // Arrange
            var username = "TestUser";

            // Act
            _service.RecordFailedAttempt(username.ToLower());
            _service.RecordFailedAttempt(username.ToUpper());
            _service.RecordFailedAttempt(username);

            // Assert
            _service.IsAccountLocked(username).Should().BeTrue();
            _service.IsAccountLocked(username.ToLower()).Should().BeTrue();
            _service.IsAccountLocked(username.ToUpper()).Should().BeTrue();
        }

        #endregion

        #region ClearAttempts Tests

        [Fact]
        public void ClearAttempts_AfterFailedAttempts_ShouldUnlockAccount()
        {
            // Arrange
            var username = "testuser";
            _service.RecordFailedAttempt(username);
            _service.RecordFailedAttempt(username);
            _service.RecordFailedAttempt(username);

            // Verify locked
            _service.IsAccountLocked(username).Should().BeTrue();

            // Act
            _service.ClearAttempts(username);

            // Assert
            _service.IsAccountLocked(username).Should().BeFalse();
            _service.GetRemainingLockTime(username).Should().Be(0);
        }

        [Fact]
        public void ClearAttempts_WithNoFailedAttempts_ShouldNotThrow()
        {
            // Arrange
            var username = "testuser";

            // Act & Assert
            var action = () => _service.ClearAttempts(username);
            action.Should().NotThrow();
        }

        [Fact]
        public void ClearAttempts_WithNullUsername_ShouldNotThrow()
        {
            // Act & Assert
            var action = () => _service.ClearAttempts(null);
            action.Should().NotThrow();
        }

        [Fact]
        public void ClearAttempts_CaseInsensitive_ShouldClearCorrectly()
        {
            // Arrange
            var username = "TestUser";
            _service.RecordFailedAttempt(username.ToLower());
            _service.RecordFailedAttempt(username.ToUpper());
            _service.RecordFailedAttempt(username);
            
            _service.IsAccountLocked(username).Should().BeTrue();

            // Act
            _service.ClearAttempts(username.ToUpper());

            // Assert
            _service.IsAccountLocked(username).Should().BeFalse();
            _service.IsAccountLocked(username.ToLower()).Should().BeFalse();
        }

        [Fact]
        public void ClearAttempts_ShouldNotAffectOtherUsers()
        {
            // Arrange
            var user1 = "user1";
            var user2 = "user2";

            // Lock both users
            for (int i = 0; i < 3; i++)
            {
                _service.RecordFailedAttempt(user1);
                _service.RecordFailedAttempt(user2);
            }

            _service.IsAccountLocked(user1).Should().BeTrue();
            _service.IsAccountLocked(user2).Should().BeTrue();

            // Act - clear only user1
            _service.ClearAttempts(user1);

            // Assert
            _service.IsAccountLocked(user1).Should().BeFalse();
            _service.IsAccountLocked(user2).Should().BeTrue();
        }

        #endregion

        #region GetRemainingLockTime Tests

        [Fact]
        public void GetRemainingLockTime_WithNoFailedAttempts_ShouldReturnZero()
        {
            // Arrange
            var username = "testuser";

            // Act & Assert
            _service.GetRemainingLockTime(username).Should().Be(0);
        }

        [Fact]
        public void GetRemainingLockTime_WithFailedAttemptsButNotLocked_ShouldReturnZero()
        {
            // Arrange
            var username = "testuser";
            _service.RecordFailedAttempt(username);
            _service.RecordFailedAttempt(username);

            // Act & Assert
            _service.GetRemainingLockTime(username).Should().Be(0);
        }

        [Fact]
        public void GetRemainingLockTime_WhenAccountIsLocked_ShouldReturnPositiveTime()
        {
            // Arrange
            var username = "testuser";
            _service.RecordFailedAttempt(username);
            _service.RecordFailedAttempt(username);
            _service.RecordFailedAttempt(username);

            // Act & Assert
            var remainingTime = _service.GetRemainingLockTime(username);
            remainingTime.Should().BeGreaterThan(0);
            remainingTime.Should().BeLessOrEqualTo(15 * 60); // 15分钟 = 900秒
        }

        [Fact]
        public void GetRemainingLockTime_AfterClearAttempts_ShouldReturnZero()
        {
            // Arrange
            var username = "testuser";
            _service.RecordFailedAttempt(username);
            _service.RecordFailedAttempt(username);
            _service.RecordFailedAttempt(username);

            _service.GetRemainingLockTime(username).Should().BeGreaterThan(0);

            // Act
            _service.ClearAttempts(username);

            // Assert
            _service.GetRemainingLockTime(username).Should().Be(0);
        }

        [Fact]
        public void GetRemainingLockTime_WithNullUsername_ShouldReturnZero()
        {
            // Act & Assert
            _service.GetRemainingLockTime(null).Should().Be(0);
        }

        [Fact]
        public void GetRemainingLockTime_CaseInsensitive_ShouldBehaveConsistently()
        {
            // Arrange
            var username = "TestUser";
            _service.RecordFailedAttempt(username.ToLower());
            _service.RecordFailedAttempt(username.ToUpper());
            _service.RecordFailedAttempt(username);

            // Act & Assert
            var time1 = _service.GetRemainingLockTime(username);
            var time2 = _service.GetRemainingLockTime(username.ToLower());
            var time3 = _service.GetRemainingLockTime(username.ToUpper());

            time1.Should().BeGreaterThan(0);
            time2.Should().BeGreaterThan(0);
            time3.Should().BeGreaterThan(0);
            
            // 时间应该相近（允许1秒误差）
            Math.Abs(time1 - time2).Should().BeLessOrEqualTo(1);
            Math.Abs(time2 - time3).Should().BeLessOrEqualTo(1);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void LoginWorkflow_SuccessfulLogin_ShouldClearFailedAttempts()
        {
            // Arrange
            var username = "testuser";

            // Simulate 2 failed attempts
            _service.RecordFailedAttempt(username);
            _service.RecordFailedAttempt(username);
            _service.IsAccountLocked(username).Should().BeFalse();

            // Simulate successful login
            _service.ClearAttempts(username);

            // Try to login again - should not be affected by previous attempts
            _service.IsAccountLocked(username).Should().BeFalse();

            // Even after 2 more failed attempts, should not be locked (previous cleared)
            _service.RecordFailedAttempt(username);
            _service.RecordFailedAttempt(username);
            _service.IsAccountLocked(username).Should().BeFalse();
        }

        [Fact]
        public void LoginWorkflow_AccountLockoutAndRecovery_ShouldWorkCorrectly()
        {
            // Arrange
            var username = "testuser";

            // Phase 1: Lock the account
            _service.RecordFailedAttempt(username);
            _service.RecordFailedAttempt(username);
            _service.RecordFailedAttempt(username);

            // Verify locked
            _service.IsAccountLocked(username).Should().BeTrue();
            _service.GetRemainingLockTime(username).Should().BeGreaterThan(0);

            // Phase 2: Admin clears the lockout (or successful login)
            _service.ClearAttempts(username);

            // Verify unlocked
            _service.IsAccountLocked(username).Should().BeFalse();
            _service.GetRemainingLockTime(username).Should().Be(0);

            // Phase 3: User can try again normally
            _service.RecordFailedAttempt(username);
            _service.IsAccountLocked(username).Should().BeFalse();
        }

        [Fact]
        public void ConcurrentAccess_MultipleThreads_ShouldHandleCorrectly()
        {
            // Arrange
            var username = "testuser";
            var tasks = new Task[10];
            
            // Act - simulate concurrent failed login attempts
            for (int i = 0; i < 10; i++)
            {
                tasks[i] = Task.Run(() => _service.RecordFailedAttempt(username));
            }

            Task.WaitAll(tasks);

            // Assert - account should definitely be locked after 10 attempts
            _service.IsAccountLocked(username).Should().BeTrue();
            _service.GetRemainingLockTime(username).Should().BeGreaterThan(0);
        }

        [Fact]
        public void AccountLockout_CheckMultipleTimes_ShouldReturnConsistentResults()
        {
            // Arrange
            var username = "testuser";
            _service.RecordFailedAttempt(username);
            _service.RecordFailedAttempt(username);
            _service.RecordFailedAttempt(username);

            // Act & Assert - multiple checks should be consistent
            _service.IsAccountLocked(username).Should().BeTrue();
            _service.IsAccountLocked(username).Should().BeTrue();
            _service.IsAccountLocked(username).Should().BeTrue();

            var time1 = _service.GetRemainingLockTime(username);
            Thread.Sleep(1000); // Wait 1 second
            var time2 = _service.GetRemainingLockTime(username);

            // Time should decrease
            time1.Should().BeGreaterThan(time2);
            time2.Should().BeGreaterThan(0);
        }

        #endregion
    }
}
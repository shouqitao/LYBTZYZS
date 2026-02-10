using FluentAssertions;
using LYBT.Entities.Auth;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Tests.Unit.Entities.Auth
{
    /// <summary>
    /// AuthSession实体单元测试 - 测试认证会话实体的所有属性和默认值
    /// </summary>
    public class AuthSessionModelTests
    {
        [Fact]
        public void Constructor_ShouldInitializePropertiesWithDefaultValues()
        {
            // Arrange & Act
            var authSession = new AuthSession();

            // Assert
            authSession.Id.Should().Be(Guid.Empty);
            authSession.UserId.Should().Be(Guid.Empty);
            authSession.TokenHash.Should().Be(string.Empty);
            authSession.LoginTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            authSession.LogoutTime.Should().BeNull();
            authSession.ExpiryTime.Should().Be(default(DateTime));
            authSession.IpAddress.Should().Be(string.Empty);
            authSession.UserAgent.Should().BeNull();
            authSession.IsRevoked.Should().BeFalse();
            authSession.Status.Should().Be(CommonStatus.Enabled);
        }

        [Fact]
        public void Id_PropertyCanBeSetAndGet()
        {
            // Arrange
            var authSession = new AuthSession();
            var testId = Guid.NewGuid();

            // Act
            authSession.Id = testId;

            // Assert
            authSession.Id.Should().Be(testId);
        }

        [Fact]
        public void UserId_PropertyCanBeSetAndGet()
        {
            // Arrange
            var authSession = new AuthSession();
            var testUserId = Guid.NewGuid();

            // Act
            authSession.UserId = testUserId;

            // Assert
            authSession.UserId.Should().Be(testUserId);
        }

        [Fact]
        public void TokenHash_PropertyCanBeSetAndGet()
        {
            // Arrange
            var authSession = new AuthSession();
            const string testTokenHash = "hashed_token_12345";

            // Act
            authSession.TokenHash = testTokenHash;

            // Assert
            authSession.TokenHash.Should().Be(testTokenHash);
        }

        [Fact]
        public void LoginTime_PropertyCanBeSetAndGet()
        {
            // Arrange
            var authSession = new AuthSession();
            var testLoginTime = new DateTime(2024, 1, 1, 9, 0, 0);

            // Act
            authSession.LoginTime = testLoginTime;

            // Assert
            authSession.LoginTime.Should().Be(testLoginTime);
        }

        [Fact]
        public void LogoutTime_PropertyCanBeSetAndGet()
        {
            // Arrange
            var authSession = new AuthSession();
            var testLogoutTime = new DateTime(2024, 1, 1, 17, 0, 0);

            // Act
            authSession.LogoutTime = testLogoutTime;

            // Assert
            authSession.LogoutTime.Should().Be(testLogoutTime);
        }

        [Fact]
        public void ExpiryTime_PropertyCanBeSetAndGet()
        {
            // Arrange
            var authSession = new AuthSession();
            var testExpiryTime = new DateTime(2024, 1, 2, 9, 0, 0);

            // Act
            authSession.ExpiryTime = testExpiryTime;

            // Assert
            authSession.ExpiryTime.Should().Be(testExpiryTime);
        }

        [Fact]
        public void IpAddress_PropertyCanBeSetAndGet()
        {
            // Arrange
            var authSession = new AuthSession();
            const string testIpAddress = "192.168.1.100";

            // Act
            authSession.IpAddress = testIpAddress;

            // Assert
            authSession.IpAddress.Should().Be(testIpAddress);
        }

        [Fact]
        public void UserAgent_PropertyCanBeSetAndGet()
        {
            // Arrange
            var authSession = new AuthSession();
            const string testUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";

            // Act
            authSession.UserAgent = testUserAgent;

            // Assert
            authSession.UserAgent.Should().Be(testUserAgent);
        }

        [Fact]
        public void IsRevoked_PropertyCanBeSetAndGet()
        {
            // Arrange
            var authSession = new AuthSession();

            // Act
            authSession.IsRevoked = true;

            // Assert
            authSession.IsRevoked.Should().BeTrue();
        }

        [Fact]
        public void Status_PropertyCanBeSetAndGet()
        {
            // Arrange
            var authSession = new AuthSession();

            // Act & Assert
            authSession.Status = CommonStatus.Disabled;
            authSession.Status.Should().Be(CommonStatus.Disabled);

            authSession.Status = CommonStatus.Enabled;
            authSession.Status.Should().Be(CommonStatus.Enabled);
        }

        [Fact]
        public void LogoutTime_CanBeSetToNull()
        {
            // Arrange
            var authSession = new AuthSession();

            // Act
            authSession.LogoutTime = null;

            // Assert
            authSession.LogoutTime.Should().BeNull();
        }

        [Fact]
        public void UserAgent_CanBeSetToNull()
        {
            // Arrange
            var authSession = new AuthSession();

            // Act
            authSession.UserAgent = null;

            // Assert
            authSession.UserAgent.Should().BeNull();
        }

        [Fact]
        public void IsRevoked_DefaultValueShouldBeFalse()
        {
            // Arrange & Act
            var authSession = new AuthSession();

            // Assert
            authSession.IsRevoked.Should().BeFalse();
        }

        [Fact]
        public void Status_DefaultValueShouldBeEnabled()
        {
            // Arrange & Act
            var authSession = new AuthSession();

            // Assert
            authSession.Status.Should().Be(CommonStatus.Enabled);
        }

        [Fact]
        public void CreateCompleteAuthSession_ShouldSetAllProperties()
        {
            // Arrange
            var authSession = new AuthSession();
            var sessionId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var loginTime = DateTime.Now;
            var expiryTime = loginTime.AddHours(8);

            // Act
            authSession.Id = sessionId;
            authSession.UserId = userId;
            authSession.TokenHash = "secure_token_hash_123";
            authSession.LoginTime = loginTime;
            authSession.ExpiryTime = expiryTime;
            authSession.IpAddress = "192.168.1.50";
            authSession.UserAgent = "LYBT Desktop Client v1.0";
            authSession.IsRevoked = false;
            authSession.Status = CommonStatus.Enabled;

            // Assert
            authSession.Id.Should().Be(sessionId);
            authSession.UserId.Should().Be(userId);
            authSession.TokenHash.Should().Be("secure_token_hash_123");
            authSession.LoginTime.Should().Be(loginTime);
            authSession.ExpiryTime.Should().Be(expiryTime);
            authSession.IpAddress.Should().Be("192.168.1.50");
            authSession.UserAgent.Should().Be("LYBT Desktop Client v1.0");
            authSession.IsRevoked.Should().BeFalse();
            authSession.Status.Should().Be(CommonStatus.Enabled);
        }

        [Fact]
        public void MultipleInstances_ShouldBeIndependent()
        {
            // Arrange & Act
            var session1 = new AuthSession
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                TokenHash = "token1",
                IpAddress = "192.168.1.1"
            };

            var session2 = new AuthSession
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                TokenHash = "token2",
                IpAddress = "192.168.1.2"
            };

            // Assert
            session1.Id.Should().NotBe(session2.Id);
            session1.UserId.Should().NotBe(session2.UserId);
            session1.TokenHash.Should().NotBe(session2.TokenHash);
            session1.IpAddress.Should().NotBe(session2.IpAddress);
        }

        [Fact]
        public void SessionRevocation_ShouldUpdateRevokedStatus()
        {
            // Arrange
            var authSession = new AuthSession
            {
                IsRevoked = false,
                Status = CommonStatus.Enabled
            };

            // Act
            authSession.IsRevoked = true;
            authSession.Status = CommonStatus.Disabled;

            // Assert
            authSession.IsRevoked.Should().BeTrue();
            authSession.Status.Should().Be(CommonStatus.Disabled);
        }

        [Fact]
        public void SessionLogout_ShouldSetLogoutTime()
        {
            // Arrange
            var authSession = new AuthSession();
            var logoutTime = DateTime.Now;

            // Act
            authSession.LogoutTime = logoutTime;

            // Assert
            authSession.LogoutTime.Should().Be(logoutTime);
        }
    }
}
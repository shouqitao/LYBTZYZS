using FluentAssertions;
using LYBT.Entities.Users;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Entities.Tests.Users
{
    /// <summary>
    /// User实体单元测试 - 测试用户实体的所有属性和默认值
    /// </summary>
    public class UserModelTests
    {
        [Fact]
        public void Constructor_ShouldInitializePropertiesWithDefaultValues()
        {
            // Arrange & Act
            var user = new User();

            // Assert
            user.Id.Should().Be(Guid.Empty);
            user.Username.Should().Be(string.Empty);
            user.RealName.Should().Be(string.Empty);
            user.PinYinCode.Should().BeNull();
            user.PhoneNumber.Should().BeNull();
            user.Email.Should().BeNull();
            user.Role.Should().Be(UserRole.Doctor);
            user.Status.Should().Be(CommonStatus.Enabled);
            user.PasswordHash.Should().Be(string.Empty);
            user.FailedLoginCount.Should().Be(0);
            user.LockoutEnd.Should().BeNull();
            user.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            user.UpdatedAt.Should().BeNull();
            user.LastLoginTime.Should().BeNull();
            user.Remark.Should().BeNull();
            user.RowVersion.Should().NotBeNull();
            user.RowVersion.Should().HaveCount(8);
        }

        [Fact]
        public void Id_PropertyCanBeSetAndGet()
        {
            // Arrange
            var user = new User();
            var testId = Guid.NewGuid();

            // Act
            user.Id = testId;

            // Assert
            user.Id.Should().Be(testId);
        }

        [Fact]
        public void Username_PropertyCanBeSetAndGet()
        {
            // Arrange
            var user = new User();
            const string testUsername = "testuser";

            // Act
            user.Username = testUsername;

            // Assert
            user.Username.Should().Be(testUsername);
        }

        [Fact]
        public void RealName_PropertyCanBeSetAndGet()
        {
            // Arrange
            var user = new User();
            const string testRealName = "张三";

            // Act
            user.RealName = testRealName;

            // Assert
            user.RealName.Should().Be(testRealName);
        }

        [Fact]
        public void PinYinCode_PropertyCanBeSetAndGet()
        {
            // Arrange
            var user = new User();
            const string testPinYinCode = "zs";

            // Act
            user.PinYinCode = testPinYinCode;

            // Assert
            user.PinYinCode.Should().Be(testPinYinCode);
        }

        [Fact]
        public void PhoneNumber_PropertyCanBeSetAndGet()
        {
            // Arrange
            var user = new User();
            const string testPhoneNumber = "13800138000";

            // Act
            user.PhoneNumber = testPhoneNumber;

            // Assert
            user.PhoneNumber.Should().Be(testPhoneNumber);
        }

        [Fact]
        public void Email_PropertyCanBeSetAndGet()
        {
            // Arrange
            var user = new User();
            const string testEmail = "test@example.com";

            // Act
            user.Email = testEmail;

            // Assert
            user.Email.Should().Be(testEmail);
        }

        [Fact]
        public void Role_PropertyCanBeSetAndGet()
        {
            // Arrange
            var user = new User();

            // Act & Assert
            user.Role = UserRole.Admin;
            user.Role.Should().Be(UserRole.Admin);

            user.Role = UserRole.Doctor;
            user.Role.Should().Be(UserRole.Doctor);
        }

        [Fact]
        public void Status_PropertyCanBeSetAndGet()
        {
            // Arrange
            var user = new User();

            // Act & Assert
            user.Status = CommonStatus.Disabled;
            user.Status.Should().Be(CommonStatus.Disabled);

            user.Status = CommonStatus.Enabled;
            user.Status.Should().Be(CommonStatus.Enabled);
        }

        [Fact]
        public void PasswordHash_PropertyCanBeSetAndGet()
        {
            // Arrange
            var user = new User();
            const string testPasswordHash = "hashed_password_123";

            // Act
            user.PasswordHash = testPasswordHash;

            // Assert
            user.PasswordHash.Should().Be(testPasswordHash);
        }

        [Fact]
        public void FailedLoginCount_PropertyCanBeSetAndGet()
        {
            // Arrange
            var user = new User();
            const int testFailedCount = 3;

            // Act
            user.FailedLoginCount = testFailedCount;

            // Assert
            user.FailedLoginCount.Should().Be(testFailedCount);
        }

        [Fact]
        public void LockoutEnd_PropertyCanBeSetAndGet()
        {
            // Arrange
            var user = new User();
            var testLockoutEnd = DateTime.Now.AddHours(1);

            // Act
            user.LockoutEnd = testLockoutEnd;

            // Assert
            user.LockoutEnd.Should().Be(testLockoutEnd);
        }

        [Fact]
        public void CreatedAt_PropertyCanBeSetAndGet()
        {
            // Arrange
            var user = new User();
            var testTime = new DateTime(2024, 1, 1, 10, 0, 0);

            // Act
            user.CreatedAt = testTime;

            // Assert
            user.CreatedAt.Should().Be(testTime);
        }

        [Fact]
        public void UpdatedAt_PropertyCanBeSetAndGet()
        {
            // Arrange
            var user = new User();
            var testTime = new DateTime(2024, 1, 2, 15, 30, 0);

            // Act
            user.UpdatedAt = testTime;

            // Assert
            user.UpdatedAt.Should().Be(testTime);
        }

        [Fact]
        public void LastLoginTime_PropertyCanBeSetAndGet()
        {
            // Arrange
            var user = new User();
            var testTime = new DateTime(2024, 1, 3, 9, 15, 0);

            // Act
            user.LastLoginTime = testTime;

            // Assert
            user.LastLoginTime.Should().Be(testTime);
        }

        [Fact]
        public void Remark_PropertyCanBeSetAndGet()
        {
            // Arrange
            var user = new User();
            const string testRemark = "测试备注信息";

            // Act
            user.Remark = testRemark;

            // Assert
            user.Remark.Should().Be(testRemark);
        }

        [Fact]
        public void RowVersion_PropertyCanBeSetAndGet()
        {
            // Arrange
            var user = new User();
            var testVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            // Act
            user.RowVersion = testVersion;

            // Assert
            user.RowVersion.Should().BeEquivalentTo(testVersion);
        }

        [Fact]
        public void NullableProperties_CanBeSetToNull()
        {
            // Arrange
            var user = new User();

            // Act
            user.PinYinCode = null;
            user.PhoneNumber = null;
            user.Email = null;
            user.LockoutEnd = null;
            user.UpdatedAt = null;
            user.LastLoginTime = null;
            user.Remark = null;

            // Assert
            user.PinYinCode.Should().BeNull();
            user.PhoneNumber.Should().BeNull();
            user.Email.Should().BeNull();
            user.LockoutEnd.Should().BeNull();
            user.UpdatedAt.Should().BeNull();
            user.LastLoginTime.Should().BeNull();
            user.Remark.Should().BeNull();
        }

        [Fact]
        public void CreateCompleteUser_ShouldSetAllProperties()
        {
            // Arrange
            var user = new User();
            var userId = Guid.NewGuid();
            var createdTime = DateTime.Now;

            // Act
            user.Id = userId;
            user.Username = "doctor001";
            user.RealName = "李医生";
            user.PinYinCode = "lys";
            user.PhoneNumber = "13912345678";
            user.Email = "doctor@lybt.com";
            user.Role = UserRole.Doctor;
            user.Status = CommonStatus.Enabled;
            user.PasswordHash = "hashed_password";
            user.FailedLoginCount = 0;
            user.CreatedAt = createdTime;
            user.Remark = "优秀医生";

            // Assert
            user.Id.Should().Be(userId);
            user.Username.Should().Be("doctor001");
            user.RealName.Should().Be("李医生");
            user.PinYinCode.Should().Be("lys");
            user.PhoneNumber.Should().Be("13912345678");
            user.Email.Should().Be("doctor@lybt.com");
            user.Role.Should().Be(UserRole.Doctor);
            user.Status.Should().Be(CommonStatus.Enabled);
            user.PasswordHash.Should().Be("hashed_password");
            user.FailedLoginCount.Should().Be(0);
            user.CreatedAt.Should().Be(createdTime);
            user.Remark.Should().Be("优秀医生");
        }
    }
}
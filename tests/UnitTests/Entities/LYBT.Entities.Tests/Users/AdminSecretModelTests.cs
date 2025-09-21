using FluentAssertions;
using LYBT.Entities.Users;
using Xunit;

namespace LYBT.Entities.Tests.Users
{
    /// <summary>
    /// AdminSecretModel实体单元测试 - 测试管理员密码模型的所有属性
    /// </summary>
    public class AdminSecretModelTests
    {
        [Fact]
        public void Constructor_ShouldInitializePropertiesWithDefaultValues()
        {
            // Arrange & Act
            var adminSecret = new AdminSecretModel();

            // Assert
            adminSecret.Id.Should().Be(Guid.Empty);
            adminSecret.PasswordHash.Should().Be(string.Empty);
        }

        [Fact]
        public void Id_PropertyCanBeSetAndGet()
        {
            // Arrange
            var adminSecret = new AdminSecretModel();
            var testId = Guid.NewGuid();

            // Act
            adminSecret.Id = testId;

            // Assert
            adminSecret.Id.Should().Be(testId);
        }

        [Fact]
        public void PasswordHash_PropertyCanBeSetAndGet()
        {
            // Arrange
            var adminSecret = new AdminSecretModel();
            const string testPasswordHash = "admin_hashed_password_123";

            // Act
            adminSecret.PasswordHash = testPasswordHash;

            // Assert
            adminSecret.PasswordHash.Should().Be(testPasswordHash);
        }

        [Fact]
        public void PasswordHash_CanBeSetToEmptyString()
        {
            // Arrange
            var adminSecret = new AdminSecretModel();

            // Act
            adminSecret.PasswordHash = string.Empty;

            // Assert
            adminSecret.PasswordHash.Should().Be(string.Empty);
        }

        [Fact]
        public void CreateCompleteAdminSecret_ShouldSetAllProperties()
        {
            // Arrange
            var adminSecret = new AdminSecretModel();
            var adminId = Guid.NewGuid();
            const string passwordHash = "securely_hashed_admin_password";

            // Act
            adminSecret.Id = adminId;
            adminSecret.PasswordHash = passwordHash;

            // Assert
            adminSecret.Id.Should().Be(adminId);
            adminSecret.PasswordHash.Should().Be(passwordHash);
        }

        [Fact]
        public void MultipleInstances_ShouldHaveUniqueIds()
        {
            // Arrange & Act
            var admin1 = new AdminSecretModel { Id = Guid.NewGuid() };
            var admin2 = new AdminSecretModel { Id = Guid.NewGuid() };

            // Assert
            admin1.Id.Should().NotBe(admin2.Id);
        }

        [Fact]
        public void PasswordHash_ShouldAcceptLongStrings()
        {
            // Arrange
            var adminSecret = new AdminSecretModel();
            var longPasswordHash = new string('a', 500); // 500个字符的密码哈希

            // Act
            adminSecret.PasswordHash = longPasswordHash;

            // Assert
            adminSecret.PasswordHash.Should().Be(longPasswordHash);
            adminSecret.PasswordHash.Should().HaveLength(500);
        }

        [Fact]
        public void PasswordHash_ShouldHandleSpecialCharacters()
        {
            // Arrange
            var adminSecret = new AdminSecretModel();
            const string specialCharHash = "hash_with_special_chars_!@#$%^&*()_+-={}[]|\\:;\"'<>?,./";

            // Act
            adminSecret.PasswordHash = specialCharHash;

            // Assert
            adminSecret.PasswordHash.Should().Be(specialCharHash);
        }

        [Fact]
        public void Properties_AreIndependent()
        {
            // Arrange
            var adminSecret = new AdminSecretModel();
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            const string hash1 = "hash1";
            const string hash2 = "hash2";

            // Act
            adminSecret.Id = id1;
            adminSecret.PasswordHash = hash1;

            // Verify initial state
            adminSecret.Id.Should().Be(id1);
            adminSecret.PasswordHash.Should().Be(hash1);

            // Change values
            adminSecret.Id = id2;
            adminSecret.PasswordHash = hash2;

            // Assert
            adminSecret.Id.Should().Be(id2);
            adminSecret.PasswordHash.Should().Be(hash2);
        }
    }
}
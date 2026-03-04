using FluentAssertions;
using LYBT.Entities.Common;
using Xunit;

namespace LYBT.Tests.Server.PureLogic.Entities.Common
{
    /// <summary>
    /// BaseEntity单元测试 - 测试实体基类的所有属性和默认值
    /// </summary>
    public class BaseEntityTests
    {
        private class TestEntity : BaseEntity
        {
            public string Name { get; set; } = string.Empty;
        }

        [Fact]
        public void Constructor_ShouldInitializePropertiesWithDefaultValues()
        {
            // Arrange & Act
            var entity = new TestEntity();

            // Assert
            entity.Id.Should().NotBe(Guid.Empty);
            entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            entity.UpdatedAt.Should().BeNull();
            entity.CreatedBy.Should().BeNull();
            entity.UpdatedBy.Should().BeNull();
            entity.RowVersion.Should().BeNull(); // RowVersion 由 EF Core 管理，默认为 null
            entity.IsDeleted.Should().BeFalse();
        }

        [Fact]
        public void Id_PropertyCanBeSetAndGet()
        {
            // Arrange
            var entity = new TestEntity();
            var newId = Guid.NewGuid();

            // Act
            entity.Id = newId;

            // Assert
            entity.Id.Should().Be(newId);
        }

        [Fact]
        public void CreatedAt_PropertyCanBeSetAndGet()
        {
            // Arrange
            var entity = new TestEntity();
            var testDate = new DateTime(2024, 1, 1, 12, 0, 0);

            // Act
            entity.CreatedAt = testDate;

            // Assert
            entity.CreatedAt.Should().Be(testDate);
        }

        [Fact]
        public void UpdatedAt_PropertyCanBeSetAndGet()
        {
            // Arrange
            var entity = new TestEntity();
            var testDate = new DateTime(2024, 1, 2, 14, 30, 0);

            // Act
            entity.UpdatedAt = testDate;

            // Assert
            entity.UpdatedAt.Should().Be(testDate);
        }

        [Fact]
        public void CreatedBy_PropertyCanBeSetAndGet()
        {
            // Arrange
            var entity = new TestEntity();
            var userId = Guid.NewGuid();

            // Act
            entity.CreatedBy = userId;

            // Assert
            entity.CreatedBy.Should().Be(userId);
        }

        [Fact]
        public void UpdatedBy_PropertyCanBeSetAndGet()
        {
            // Arrange
            var entity = new TestEntity();
            var userId = Guid.NewGuid();

            // Act
            entity.UpdatedBy = userId;

            // Assert
            entity.UpdatedBy.Should().Be(userId);
        }

        [Fact]
        public void RowVersion_PropertyCanBeSetAndGet()
        {
            // Arrange
            var entity = new TestEntity();
            var version = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            // Act
            entity.RowVersion = version;

            // Assert
            entity.RowVersion.Should().BeEquivalentTo(version);
        }

        [Fact]
        public void IsDeleted_PropertyCanBeSetAndGet()
        {
            // Arrange
            var entity = new TestEntity();

            // Act
            entity.IsDeleted = true;

            // Assert
            entity.IsDeleted.Should().BeTrue();
        }

        [Fact]
        public void IsDeleted_DefaultValueShouldBeFalse()
        {
            // Arrange & Act
            var entity = new TestEntity();

            // Assert
            entity.IsDeleted.Should().BeFalse();
        }

        [Fact]
        public void RowVersion_DefaultValueShouldBeNull()
        {
            // Arrange & Act
            var entity = new TestEntity();

            // Assert - RowVersion 由 EF Core [Timestamp] 管理，内存中默认为 null
            entity.RowVersion.Should().BeNull();
        }

        [Fact]
        public void Id_ShouldBeUniqueForDifferentInstances()
        {
            // Arrange & Act
            var entity1 = new TestEntity();
            var entity2 = new TestEntity();

            // Assert
            entity1.Id.Should().NotBe(entity2.Id);
        }

        [Fact]
        public void NullableProperties_CanBeSetToNull()
        {
            // Arrange
            var entity = new TestEntity();

            // Act
            entity.UpdatedAt = null;
            entity.CreatedBy = null;
            entity.UpdatedBy = null;

            // Assert
            entity.UpdatedAt.Should().BeNull();
            entity.CreatedBy.Should().BeNull();
            entity.UpdatedBy.Should().BeNull();
        }
    }
}
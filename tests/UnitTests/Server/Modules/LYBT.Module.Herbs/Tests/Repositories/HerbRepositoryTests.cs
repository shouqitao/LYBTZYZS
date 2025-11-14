using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Herbs.Repositories;
using LYBT.Infrastructure.Data;
using LYBT.Entities.Herbs;
using LYBT.Shared.Models.Contracts.Common;
using FluentAssertions;
using Xunit;

namespace LYBT.Module.Herbs.Tests.Repositories
{
    /// <summary>
    /// HerbRepository单元测试
    /// 测试药材Repository的CRUD操作和搜索功能
    /// </summary>
    public class HerbRepositoryTests : TestBase
    {
        private readonly Mock<AppDbContext> _mockContext;
        private readonly Mock<DbSet<Herb>> _mockDbSet;
        private readonly Mock<ILogger<HerbRepository>> _mockLogger;
        private readonly HerbRepository _repository;
        private readonly List<Herb> _testHerbs;

        public HerbRepositoryTests()
        {
            _testHerbs = CreateTestHerbs();
            _mockDbSet = CreateMockDbSet(_testHerbs);
            _mockContext = new Mock<AppDbContext>();
            _mockContext.Setup(c => c.Set<Herb>()).Returns(_mockDbSet.Object);

            _mockLogger = CreateLoggerMock<HerbRepository>();

            _repository = new HerbRepository(_mockContext.Object, _mockLogger.Object);
        }

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_WithExistingHerb_ShouldReturnHerb()
        {
            // Arrange
            var herbId = _testHerbs.First().Id;

            // Act
            var result = await _repository.GetByIdAsync(herbId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(herbId);
            result.Name.Should().Be(_testHerbs.First().Name);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistingHerb_ShouldReturnNull()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();

            // Act
            var result = await _repository.GetByIdAsync(nonExistingId);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region GetPagedAsync Tests

        [Fact]
        public async Task GetPagedAsync_WithKeywordSearch_ShouldReturnFilteredResults()
        {
            // Arrange
            var pageNumber = 1;
            var pageSize = 10;
            var keyword = "人参";

            // Act
            var result = await _repository.GetPagedAsync(pageNumber, pageSize, keyword);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().NotBeEmpty();
            result.TotalCount.Should().BeGreaterThan(0);
            result.CurrentPage.Should().Be(pageNumber);
            result.PageSize.Should().Be(pageSize);

            // 验证搜索过滤是否正确
            result.Items.All(h =>
                h.Name.Contains(keyword) ||
                (h.PinYinCode != null && h.PinYinCode.Contains(keyword)) ||
                (h.Category != null && h.Category.Contains(keyword)) ||
                (h.Effects != null && h.Effects.Contains(keyword))
            ).Should().BeTrue();
        }

        [Fact]
        public async Task GetPagedAsync_WithEmptyKeyword_ShouldReturnAllHerbs()
        {
            // Arrange
            var pageNumber = 1;
            var pageSize = 20;

            // Act
            var result = await _repository.GetPagedAsync(pageNumber, pageSize, null);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(_testHerbs.Count(h => !h.IsDeleted));
            result.TotalCount.Should().Be(_testHerbs.Count(h => !h.IsDeleted));
        }

        [Fact]
        public async Task GetPagedAsync_WithCategorySearch_ShouldReturnFilteredResults()
        {
            // Arrange
            var pageNumber = 1;
            var pageSize = 10;
            var categoryKeyword = "补气";

            // Act
            var result = await _repository.GetPagedAsync(pageNumber, pageSize, categoryKeyword);

            // Assert
            result.Should().NotBeNull();
            result.Items.All(h => h.Category.Contains(categoryKeyword)).Should().BeTrue();
        }

        #endregion

        #region FindAsync Tests

        [Fact]
        public async Task FindAsync_WithMatchingCondition_ShouldReturnMatchingHerbs()
        {
            // Arrange
            var keyword = "人参";

            // Act
            var result = await _repository.FindAsync(h => h.Name.Contains(keyword));

            // Assert
            result.Should().NotBeNull();
            result.All(h => h.Name.Contains(keyword) && !h.IsDeleted).Should().BeTrue();
        }

        [Fact]
        public async Task FindAsync_WithCategoryCondition_ShouldReturnHerbsInCategory()
        {
            // Arrange
            var category = "补气药";

            // Act
            var result = await _repository.FindAsync(h => h.Category == category);

            // Assert
            result.Should().NotBeNull();
            result.All(h => h.Category == category && !h.IsDeleted).Should().BeTrue();
        }

        #endregion

        #region GetSingleAsync Tests

        [Fact]
        public async Task GetSingleAsync_WithUniqueName_ShouldReturnHerb()
        {
            // Arrange
            var targetHerb = _testHerbs.First(h => !h.IsDeleted);

            // Act
            var result = await _repository.GetSingleAsync(h => h.Name == targetHerb.Name);

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be(targetHerb.Name);
        }

        [Fact]
        public async Task GetSingleAsync_WithNonExistingName_ShouldReturnNull()
        {
            // Arrange
            var nonExistingName = "不存在的药材";

            // Act
            var result = await _repository.GetSingleAsync(h => h.Name == nonExistingName);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region AddAsync Tests

        [Fact]
        public async Task AddAsync_WithValidHerb_ShouldAddHerb()
        {
            // Arrange
            var newHerb = CreateTestHerb();

            // Act
            var result = await _repository.AddAsync(newHerb);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(newHerb.Id);

            _mockDbSet.Verify(m => m.AddAsync(It.IsAny<Herb>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task AddAsync_WithNullHerb_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.AddAsync(null!));
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_WithValidHerb_ShouldUpdateHerb()
        {
            // Arrange
            var herb = _testHerbs.First();
            herb.UpdatedAt = DateTime.UtcNow;

            // Act
            var result = await _repository.UpdateAsync(herb);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(herb.Id);

            _mockDbSet.Verify(m => m.Update(It.IsAny<Herb>()), Times.Once);
            _mockContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_WithExistingHerb_ShouldReturnTrue()
        {
            // Arrange
            var herbId = _testHerbs.First().Id;
            var herbToDelete = _testHerbs.First(h => h.Id == herbId);

            _mockDbSet.Setup(m => m.FindAsync(It.IsAny<object[]>())).ReturnsAsync(herbToDelete);

            // Act
            var result = await _repository.DeleteAsync(herbId);

            // Assert
            result.Should().BeTrue();
            herbToDelete.IsDeleted.Should().BeTrue();

            _mockContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WithNonExistingHerb_ShouldReturnFalse()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();

            _mockDbSet.Setup(m => m.FindAsync(It.IsAny<object[]>())).ReturnsAsync((Herb?)null);

            // Act
            var result = await _repository.DeleteAsync(nonExistingId);

            // Assert
            result.Should().BeFalse();

            _mockContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        #endregion

        #region ExistsAsync Tests

        [Fact]
        public async Task ExistsAsync_WithExistingHerb_ShouldReturnTrue()
        {
            // Arrange
            var herbId = _testHerbs.First().Id;

            // Act
            var result = await _repository.ExistsAsync(herbId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_WithNonExistingHerb_ShouldReturnFalse()
        {
            // Arrange
            var nonExistingId = Guid.NewGuid();

            // Act
            var result = await _repository.ExistsAsync(nonExistingId);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region CountAsync Tests

        [Fact]
        public async Task CountAsync_ShouldReturnNonDeletedHerbCount()
        {
            // Act
            var result = await _repository.CountAsync();

            // Assert
            result.Should().Be(_testHerbs.Count(h => !h.IsDeleted));
        }

        #endregion

        #region SearchByCategoryAsync Tests (HerbRepository特定方法)

        [Fact]
        public async Task SearchByCategoryAsync_WithValidCategory_ShouldReturnHerbsInCategory()
        {
            // Arrange
            var category = "补气药";

            // Act
            var result = await _repository.SearchByCategoryAsync(category);

            // Assert
            result.Should().NotBeNull();
            result.All(h => h.Category == category && !h.IsDeleted).Should().BeTrue();
        }

        [Fact]
        public async Task SearchByCategoryAsync_WithEmptyCategory_ShouldReturnEmptyList()
        {
            // Arrange
            var emptyCategory = "";

            // Act
            var result = await _repository.SearchByCategoryAsync(emptyCategory);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        #endregion

        #region GetByPinYinCodeAsync Tests (HerbRepository特定方法)

        [Fact]
        public async Task GetByPinYinCodeAsync_WithValidPinYinCode_ShouldReturnHerb()
        {
            // Arrange
            var pinYinCode = "rs";
            var expectedHerb = _testHerbs.FirstOrDefault(h => h.PinYinCode == pinYinCode && !h.IsDeleted);

            // Act
            var result = await _repository.GetByPinYinCodeAsync(pinYinCode);

            // Assert
            result.Should().NotBeNull();
            result!.PinYinCode.Should().Be(pinYinCode);
        }

        [Fact]
        public async Task GetByPinYinCodeAsync_WithNonExistingPinYinCode_ShouldReturnNull()
        {
            // Arrange
            var nonExistingPinYinCode = "xyz";

            // Act
            var result = await _repository.GetByPinYinCodeAsync(nonExistingPinYinCode);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region Helper Methods

        private List<Herb> CreateTestHerbs()
        {
            return new List<Herb>
            {
                new Herb
                {
                    Id = Guid.NewGuid(),
                    Name = "人参",
                    PinYinCode = "rs",
                    Category = "补气药",
                    Effects = "大补元气，复脉固脱，补脾益肺，生津养血，安神益智",
                    Properties = "甘、微苦，微温。归脾、肺、心、肾经。",
                    UsageDosage = "3~9g，大量可用至30g",
                    Contraindications = "实热证、湿热证忌服",
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Herb
                {
                    Id = Guid.NewGuid(),
                    Name = "黄芪",
                    PinYinCode = "hq",
                    Category = "补气药",
                    Effects = "补气升阳，固表止汗，利水消肿，托毒生肌",
                    Properties = "甘，微温。归脾、肺经。",
                    UsageDosage = "9~30g",
                    Contraindications = "表实邪盛、气滞湿阻、食积停滞、痈疽初起或溃后热毒尚盛者",
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Herb
                {
                    Id = Guid.NewGuid(),
                    Name = "当归",
                    PinYinCode = "dg",
                    Category = "补血药",
                    Effects = "补血活血，调经止痛，润肠通便",
                    Properties = "甘、辛，温。归肝、心、脾经。",
                    UsageDosage = "6~12g",
                    Contraindications = "湿盛中满、大便溏泄者",
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Herb
                {
                    Id = Guid.NewGuid(),
                    Name = "已删除的药材",
                    PinYinCode = "ysc",
                    Category = "其他",
                    Effects = "测试用",
                    Properties = "测试",
                    UsageDosage = "测试",
                    Contraindications = "测试",
                    IsDeleted = true, // 已删除的药材
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };
        }

        private Herb CreateTestHerb()
        {
            return new Herb
            {
                Id = Guid.NewGuid(),
                Name = $"测试药材_{Guid.NewGuid():N}",
                PinYinCode = "cs",
                Category = "测试药",
                Effects = "测试功效",
                Properties = "测试药性",
                UsageDosage = "10~15g",
                Contraindications = "无",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        #endregion
    }
}
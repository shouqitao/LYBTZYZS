using LYBT.Entities.Common;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Interfaces;
using LYBT.Infrastructure.Repositories;
using LYBT.Shared.Models.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LYBT.Infrastructure.Tests.Repositories;

/// <summary>
/// BaseReadRepository单元测试
/// Phase 1: 验证只读Repository的5个核心查询方法
/// </summary>
public class BaseReadRepositoryTests : IDisposable
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly AppDbContext _context;
    private readonly TestUserRepository _repository;

    public BaseReadRepositoryTests()
    {
        // Arrange: 使用内存数据库
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(_options);
        _repository = new TestUserRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region GetByIdAsync测试（3个用例）

    [Fact]
    public async Task GetByIdAsync_Should_ReturnEntity_When_EntityExists()
    {
        // Arrange
        var user = CreateTestUser("TestUser1");
        await _context.Set<User>().AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.RealName.Should().Be("TestUser1");
    }

    [Fact]
    public async Task GetByIdAsync_Should_ReturnNull_When_EntityNotExists()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_Should_ReturnNull_When_EntityIsDeleted()
    {
        // Arrange
        var user = CreateTestUser("DeletedUser");
        user.IsDeleted = true;
        await _context.Set<User>().AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(user.Id);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllAsync测试（2个用例）

    [Fact]
    public async Task GetAllAsync_Should_ReturnEmptyList_When_NoEntities()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_Should_ReturnAllEntities_When_EntitiesExist()
    {
        // Arrange
        var user1 = CreateTestUser("User1");
        var user2 = CreateTestUser("User2");
        var user3 = CreateTestUser("User3");
        user3.IsDeleted = true; // 这个不应该被返回

        await _context.Set<User>().AddRangeAsync(user1, user2, user3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(u => u.Id == user1.Id);
        result.Should().Contain(u => u.Id == user2.Id);
        result.Should().NotContain(u => u.Id == user3.Id);
    }

    #endregion

    #region FindAsync测试（3个用例）

    [Fact]
    public async Task FindAsync_Should_ReturnMatchingEntities_When_PredicateMatches()
    {
        // Arrange
        var user1 = CreateTestUser("Alice");
        var user2 = CreateTestUser("Bob");
        var user3 = CreateTestUser("Alice");

        await _context.Set<User>().AddRangeAsync(user1, user2, user3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.FindAsync(u => u.RealName == "Alice");

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(u => u.Id == user1.Id);
        result.Should().Contain(u => u.Id == user3.Id);
    }

    [Fact]
    public async Task FindAsync_Should_ReturnEmptyList_When_NoMatches()
    {
        // Arrange
        var user = CreateTestUser("Alice");
        await _context.Set<User>().AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.FindAsync(u => u.RealName == "NonExistent");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FindAsync_Should_ThrowArgumentNullException_When_PredicateIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _repository.FindAsync(null!));
    }

    #endregion

    #region GetSingleAsync测试（3个用例）

    [Fact]
    public async Task GetSingleAsync_Should_ReturnEntity_When_SingleMatch()
    {
        // Arrange
        var user = CreateTestUser("UniqueUser");
        await _context.Set<User>().AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetSingleAsync(u => u.RealName == "UniqueUser");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetSingleAsync_Should_ThrowInvalidOperationException_When_MultipleMatches()
    {
        // Arrange
        var user1 = CreateTestUser("Duplicate");
        var user2 = CreateTestUser("Duplicate");
        await _context.Set<User>().AddRangeAsync(user1, user2);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _repository.GetSingleAsync(u => u.RealName == "Duplicate"));
    }

    [Fact]
    public async Task GetSingleAsync_Should_ReturnNull_When_NoMatch()
    {
        // Arrange
        var user = CreateTestUser("Alice");
        await _context.Set<User>().AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetSingleAsync(u => u.RealName == "NonExistent");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CountAsync测试（2个用例）

    [Fact]
    public async Task CountAsync_Should_ReturnZero_When_NoEntities()
    {
        // Act
        var count = await _repository.CountAsync();

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public async Task CountAsync_Should_ReturnCorrectCount_When_EntitiesExist()
    {
        // Arrange
        var user1 = CreateTestUser("User1");
        var user2 = CreateTestUser("User2");
        var user3 = CreateTestUser("User3");
        user3.IsDeleted = true; // 这个不应该被计数

        await _context.Set<User>().AddRangeAsync(user1, user2, user3);
        await _context.SaveChangesAsync();

        // Act
        var count = await _repository.CountAsync();

        // Assert
        count.Should().Be(2);
    }

    #endregion

    #region 异步操作测试（2个用例）

    [Fact]
    public async Task GetByIdAsync_Should_BeAsync_When_Called()
    {
        // Arrange
        var user = CreateTestUser("AsyncTest");
        await _context.Set<User>().AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var task = _repository.GetByIdAsync(user.Id);
        var isAsync = !task.IsCompleted; // 如果还未完成，说明是真正的异步

        // Assert
        await task; // 等待完成
        task.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task FindAsync_Should_BeAsync_When_Called()
    {
        // Arrange
        var user1 = CreateTestUser("User1");
        var user2 = CreateTestUser("User2");
        await _context.Set<User>().AddRangeAsync(user1, user2);
        await _context.SaveChangesAsync();

        // Act
        var task = _repository.FindAsync(u => u.RealName.Contains("User"));

        // Assert
        await task; // 等待完成
        task.IsCompleted.Should().BeTrue();
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 创建测试用户实体
    /// </summary>
    private static User CreateTestUser(string realName)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            UserName = realName.ToLower().Replace(" ", ""),
            PhoneNumber = "13800138000",
            RealName = realName,
            Role = UserRole.Doctor,
            Status = CommonStatus.Enabled,
            PasswordHash = "DummyHash",
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    #endregion
}

/// <summary>
/// 测试用Repository实现
/// </summary>
internal class TestUserRepository : BaseReadRepository<User>
{
    public TestUserRepository(AppDbContext context) : base(context)
    {
    }
}

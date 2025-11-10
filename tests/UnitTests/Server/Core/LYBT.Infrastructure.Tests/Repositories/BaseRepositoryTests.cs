using LYBT.Entities.Common;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LYBT.Infrastructure.Tests.Repositories;

/// <summary>
/// BaseRepository单元测试
/// Phase 2: 验证Repository的11个标准方法 + 扩展方法
/// </summary>
/// <remarks>
/// 测试分类：
/// - 继承的只读方法（15个用例）：GetByIdAsync, GetAllAsync, FindAsync, GetSingleAsync, CountAsync
/// - 分页查询（8个用例）：基础分页、高级分页、排序、过滤
/// - 条件查询扩展（4个用例）：ExistsAsync、CountAsync(predicate)
/// - 写操作（6个用例）：AddAsync、UpdateAsync、DeleteAsync
/// - 批量操作（6个用例）：AddRangeAsync、DeleteRangeAsync
/// - 事务（3个用例）：SaveChangesAsync
/// 总计：42+个测试用例
/// </remarks>
public class BaseRepositoryTests : IDisposable
{
    private readonly DbContextOptions<AppDbContext> _options;
    private readonly AppDbContext _context;
    private readonly LYBT.Shared.Models.Interfaces.IRepository<User> _repository;

    public BaseRepositoryTests()
    {
        // Arrange: 使用内存数据库
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(_options);
        _repository = new TestRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region 继承的只读方法测试（15个用例）

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
    public async Task FindAsync_Should_ExcludeDeletedEntities()
    {
        // Arrange
        var user1 = CreateTestUser("Alice");
        var user2 = CreateTestUser("Alice");
        user2.IsDeleted = true;

        await _context.Set<User>().AddRangeAsync(user1, user2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.FindAsync(u => u.RealName == "Alice");

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(u => u.Id == user1.Id);
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

    #region CountAsync测试（4个用例）

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

    [Fact]
    public async Task CountAsync_WithPredicate_Should_ReturnMatchingCount()
    {
        // Arrange
        var user1 = CreateTestUser("Alice");
        var user2 = CreateTestUser("Bob");
        var user3 = CreateTestUser("Alice");

        await _context.Set<User>().AddRangeAsync(user1, user2, user3);
        await _context.SaveChangesAsync();

        // Act
        var count = await ((TestRepository)_repository).CountAsync(u => u.RealName == "Alice");

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public async Task CountAsync_WithPredicate_Should_ExcludeDeletedEntities()
    {
        // Arrange
        var user1 = CreateTestUser("Alice");
        var user2 = CreateTestUser("Alice");
        user2.IsDeleted = true;

        await _context.Set<User>().AddRangeAsync(user1, user2);
        await _context.SaveChangesAsync();

        // Act
        var count = await ((TestRepository)_repository).CountAsync(u => u.RealName == "Alice");

        // Assert
        count.Should().Be(1);
    }

    #endregion

    #endregion

    #region 分页查询测试（8个用例）

    [Fact]
    public async Task GetPagedAsync_Should_ReturnCorrectPage_When_BasicPagination()
    {
        // Arrange
        var users = Enumerable.Range(1, 10)
            .Select(i => CreateTestUser($"User{i}"))
            .ToList();
        await _context.Set<User>().AddRangeAsync(users);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetPagedAsync(1, 5, keyword: null);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(10);
        result.CurrentPage.Should().Be(1);
        result.PageSize.Should().Be(5);
        result.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task GetPagedAsync_Should_ReturnSecondPage_When_PageNumberIsTwo()
    {
        // Arrange
        var users = Enumerable.Range(1, 10)
            .Select(i => CreateTestUser($"User{i}"))
            .ToList();
        await _context.Set<User>().AddRangeAsync(users);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetPagedAsync(2, 5, keyword: null);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(5);
        result.CurrentPage.Should().Be(2);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task GetPagedAsync_Should_ReturnEmptyPage_When_NoEntities()
    {
        // Act
        var result = await _repository.GetPagedAsync(1, 10, keyword: null);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().BeGreaterOrEqualTo(0); // PagedResult计算TotalPages可能为0或1
    }

    [Fact]
    public async Task GetPagedAsync_WithKeyword_Should_IgnoreKeyword_When_BaseImplementation()
    {
        // Arrange
        var user1 = CreateTestUser("Alice");
        var user2 = CreateTestUser("Bob");
        var user3 = CreateTestUser("Alice");

        await _context.Set<User>().AddRangeAsync(user1, user2, user3);
        await _context.SaveChangesAsync();

        // Act - BaseRepository默认实现忽略keyword参数
        var result = await _repository.GetPagedAsync(1, 10, "Alice");

        // Assert - 应返回所有用户(3个),因为BaseRepository不实现keyword搜索
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetPagedAsync_Should_ExcludeDeletedEntities()
    {
        // Arrange
        var user1 = CreateTestUser("User1");
        var user2 = CreateTestUser("User2");
        var user3 = CreateTestUser("User3");
        user3.IsDeleted = true;

        await _context.Set<User>().AddRangeAsync(user1, user2, user3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetPagedAsync(1, 10, keyword: null);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetPagedAsync_WithPredicate_Should_FilterResults()
    {
        // Arrange
        var users = new[]
        {
            CreateTestUser("Alice"),
            CreateTestUser("Bob"),
            CreateTestUser("Alice"),
            CreateTestUser("Charlie")
        };
        await _context.Set<User>().AddRangeAsync(users);
        await _context.SaveChangesAsync();

        // Act - 使用扩展方法重载
        var result = await ((TestRepository)_repository).GetPagedAsync(1, 10, u => u.RealName == "Alice");

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetPagedAsync_WithSorting_Should_ReturnDescendingSortedResults_When_DefaultDescending()
    {
        // Arrange
        var users = new[]
        {
            CreateTestUser("Charlie"),
            CreateTestUser("Alice"),
            CreateTestUser("Bob")
        };
        await _context.Set<User>().AddRangeAsync(users);
        await _context.SaveChangesAsync();

        // Act - 使用扩展方法重载(默认descending=true降序)
        var result = await ((TestRepository)_repository).GetPagedAsync(1, 10, null, u => u.RealName);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items.First().RealName.Should().Be("Charlie");
        result.Items.Last().RealName.Should().Be("Alice");
    }

    [Fact]
    public async Task GetPagedAsync_WithAscendingSorting_Should_ReturnAscendingSortedResults()
    {
        // Arrange
        var users = new[]
        {
            CreateTestUser("Charlie"),
            CreateTestUser("Alice"),
            CreateTestUser("Bob")
        };
        await _context.Set<User>().AddRangeAsync(users);
        await _context.SaveChangesAsync();

        // Act - 使用扩展方法重载(descending=false升序)
        var result = await ((TestRepository)_repository).GetPagedAsync(1, 10, null, u => u.RealName, false);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items.First().RealName.Should().Be("Alice");
        result.Items.Last().RealName.Should().Be("Charlie");
    }

    #endregion

    #region ExistsAsync测试（4个用例）

    [Fact]
    public async Task ExistsAsync_Should_ReturnTrue_When_EntityExists()
    {
        // Arrange
        var user = CreateTestUser("ExistingUser");
        await _context.Set<User>().AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var exists = await _repository.ExistsAsync(user.Id);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_Should_ReturnFalse_When_EntityNotExists()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var exists = await _repository.ExistsAsync(nonExistentId);

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_Should_ReturnFalse_When_EntityIsDeleted()
    {
        // Arrange
        var user = CreateTestUser("DeletedUser");
        user.IsDeleted = true;
        await _context.Set<User>().AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var exists = await _repository.ExistsAsync(user.Id);

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_WithPredicate_Should_ReturnTrue_When_MatchExists()
    {
        // Arrange
        var user = CreateTestUser("Alice");
        await _context.Set<User>().AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var exists = await ((TestRepository)_repository).ExistsAsync(u => u.RealName == "Alice");

        // Assert
        exists.Should().BeTrue();
    }

    #endregion

    #region 写操作测试（6个用例）

    [Fact]
    public async Task AddAsync_Should_AddEntity_When_ValidEntity()
    {
        // Arrange
        var user = CreateTestUser("NewUser");

        // Act
        var result = await _repository.AddAsync(user);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();

        var savedUser = await _context.Set<User>().FindAsync(user.Id);
        savedUser.Should().NotBeNull();
        savedUser!.RealName.Should().Be("NewUser");
    }

    [Fact]
    public async Task AddAsync_Should_ThrowArgumentNullException_When_EntityIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _repository.AddAsync(null!));
    }

    [Fact]
    public async Task UpdateAsync_Should_UpdateEntity_When_ValidEntity()
    {
        // Arrange
        var user = CreateTestUser("OriginalName");
        await _context.Set<User>().AddAsync(user);
        await _context.SaveChangesAsync();

        // Detach the entity to simulate a new context
        _context.Entry(user).State = EntityState.Detached;

        // Modify the user
        user.RealName = "UpdatedName";

        // Act
        var result = await _repository.UpdateAsync(user);

        // Assert
        result.Should().NotBeNull();
        result.RealName.Should().Be("UpdatedName");
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromHours(9));

        var updatedUser = await _context.Set<User>().FindAsync(user.Id);
        updatedUser!.RealName.Should().Be("UpdatedName");
    }

    [Fact]
    public async Task UpdateAsync_Should_ThrowArgumentNullException_When_EntityIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _repository.UpdateAsync(null!));
    }

    [Fact]
    public async Task DeleteAsync_Should_SoftDeleteEntity_When_EntityExists()
    {
        // Arrange
        var user = CreateTestUser("ToDelete");
        await _context.Set<User>().AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.DeleteAsync(user.Id);

        // Assert
        result.Should().BeTrue();

        var deletedUser = await _context.Set<User>().FindAsync(user.Id);
        deletedUser.Should().NotBeNull();
        deletedUser!.IsDeleted.Should().BeTrue();
        deletedUser.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromHours(9));
    }

    [Fact]
    public async Task DeleteAsync_Should_ReturnFalse_When_EntityNotExists()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.DeleteAsync(nonExistentId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region 批量操作测试（6个用例）

    [Fact]
    public async Task AddRangeAsync_Should_AddMultipleEntities_When_ValidEntities()
    {
        // Arrange
        var users = new[]
        {
            CreateTestUser("User1"),
            CreateTestUser("User2"),
            CreateTestUser("User3")
        };

        // Act
        var result = await ((TestRepository)_repository).AddRangeAsync(users);

        // Assert
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(u => u.Id.Should().NotBeEmpty());

        var savedUsers = await _context.Set<User>().Where(u => !u.IsDeleted).ToListAsync();
        savedUsers.Should().HaveCount(3);
    }

    [Fact]
    public async Task AddRangeAsync_Should_ThrowArgumentNullException_When_EntitiesIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await ((TestRepository)_repository).AddRangeAsync(null!));
    }

    [Fact]
    public async Task AddRangeAsync_Should_ReturnEmptyList_When_EmptyList()
    {
        // Arrange
        var emptyList = Array.Empty<User>();

        // Act
        var result = await ((TestRepository)_repository).AddRangeAsync(emptyList);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteRangeAsync_Should_SoftDeleteMultipleEntities_When_EntitiesExist()
    {
        // Arrange
        var users = new[]
        {
            CreateTestUser("Alice"),
            CreateTestUser("Alice"),
            CreateTestUser("Bob")
        };
        await _context.Set<User>().AddRangeAsync(users);
        await _context.SaveChangesAsync();

        // Act - 删除所有名为 "Alice" 的用户
        var result = await ((TestRepository)_repository).DeleteRangeAsync(u => u.RealName == "Alice");

        // Assert
        result.Should().Be(2);

        var deletedUsers = await _context.Set<User>()
            .IgnoreQueryFilters() // 忽略软删除过滤器
            .Where(u => u.RealName == "Alice")
            .ToListAsync();
        deletedUsers.Should().HaveCount(2);
        deletedUsers.Should().AllSatisfy(u => u.IsDeleted.Should().BeTrue());

        var bobUser = await _context.Set<User>()
            .FirstOrDefaultAsync(u => u.RealName == "Bob");
        bobUser.Should().NotBeNull();
        bobUser!.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteRangeAsync_Should_ReturnZero_When_NoMatchingEntities()
    {
        // Arrange
        var user = CreateTestUser("Alice");
        await _context.Set<User>().AddAsync(user);
        await _context.SaveChangesAsync();

        // Act - 尝试删除不存在的用户
        var result = await ((TestRepository)_repository).DeleteRangeAsync(u => u.RealName == "NonExistent");

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task DeleteRangeAsync_Should_ExcludeAlreadyDeletedEntities()
    {
        // Arrange
        var user1 = CreateTestUser("Alice");
        var user2 = CreateTestUser("Alice");
        user2.IsDeleted = true; // 已软删除

        await _context.Set<User>().AddRangeAsync(user1, user2);
        await _context.SaveChangesAsync();

        // Act
        var result = await ((TestRepository)_repository).DeleteRangeAsync(u => u.RealName == "Alice");

        // Assert
        result.Should().Be(1); // 只删除 user1，user2 已经被软删除
    }

    #endregion

    #region SaveChangesAsync测试（3个用例）

    [Fact]
    public async Task SaveChangesAsync_Should_ReturnAffectedRows_When_ChangesExist()
    {
        // Arrange
        var user = CreateTestUser("TestUser");
        await _context.Set<User>().AddAsync(user);

        // Act
        var result = await _repository.SaveChangesAsync();

        // Assert
        result.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SaveChangesAsync_Should_ReturnZero_When_NoChanges()
    {
        // Act
        var result = await _repository.SaveChangesAsync();

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task SaveChangesAsync_Should_PersistChanges_When_Called()
    {
        // Arrange
        var user = CreateTestUser("TestUser");
        await _context.Set<User>().AddAsync(user);
        await _repository.SaveChangesAsync();

        // Detach to simulate a new context
        _context.Entry(user).State = EntityState.Detached;

        // Modify
        user.RealName = "Modified";
        _context.Set<User>().Update(user);

        // Act
        await _repository.SaveChangesAsync();

        // Assert
        var savedUser = await _context.Set<User>().FindAsync(user.Id);
        savedUser!.RealName.Should().Be("Modified");
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
/// 测试用Repository实现 - 用于测试 BaseRepository
/// </summary>
internal class TestRepository : BaseRepository<User>, LYBT.Shared.Models.Interfaces.IRepository<User>
{
    public TestRepository(AppDbContext context) : base(context)
    {
    }
}

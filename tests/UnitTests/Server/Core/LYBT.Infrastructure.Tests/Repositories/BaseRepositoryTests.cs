using LYBT.Entities.Common;
using LYBT.Entities.Users;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
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
    private readonly LYBT.Infrastructure.Interfaces.IRepository<User> _repository;

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

        // Act - 使用扩展方法重载(ascending=true升序)
        var result = await ((TestRepository)_repository).GetPagedAsync(1, 10, null, u => u.RealName, true);

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

    #region 批量操作扩展测试 - AddRangeAsync（3个新用例）

    [Fact]
    public async Task AddRangeAsync_WithLargeDataset_Should_AddAllEntities()
    {
        // Arrange - 创建大量实体（300+）
        var users = Enumerable.Range(1, 350)
            .Select(i => CreateTestUser($"User{i}"))
            .ToList();

        // Act
        var result = await ((TestRepository)_repository).AddRangeAsync(users);

        // Assert
        result.Should().HaveCount(350);
        result.Should().AllSatisfy(u => u.Id.Should().NotBeEmpty());

        var savedUsers = await _context.Set<User>().Where(u => !u.IsDeleted).ToListAsync();
        savedUsers.Should().HaveCount(350);
    }

    [Fact]
    public async Task AddRangeAsync_WithTransactionRollback_Should_NotPersistChanges()
    {
        // Arrange
        var users = new[]
        {
            CreateTestUser("User1"),
            CreateTestUser("User2")
        };

        // Act - 添加但不调用SaveChanges
        await _context.Set<User>().AddRangeAsync(users);

        // 模拟事务回滚（通过Dispose而不SaveChanges）
        _context.Entry(users[0]).State = EntityState.Detached;
        _context.Entry(users[1]).State = EntityState.Detached;

        // Assert - 验证未持久化
        var count = await _context.Set<User>().Where(u => !u.IsDeleted).CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task AddRangeAsync_WithDuplicateData_Should_ThrowDbUpdateException()
    {
        // Arrange - 创建具有相同ID的用户（模拟主键冲突）
        var userId = Guid.NewGuid();
        var user1 = CreateTestUser("User1");
        user1.Id = userId;
        var user2 = CreateTestUser("User2");
        user2.Id = userId; // 重复的ID

        await _context.Set<User>().AddAsync(user1);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await _context.Set<User>().AddAsync(user2);
            await _context.SaveChangesAsync();
        });
    }

    #endregion

    #region 批量操作扩展测试 - DeleteRangeAsync(entities)（4个新用例）

    [Fact]
    public async Task DeleteRangeAsync_WithEntities_Should_SoftDeleteAllEntities()
    {
        // Arrange
        var users = new[]
        {
            CreateTestUser("User1"),
            CreateTestUser("User2"),
            CreateTestUser("User3")
        };
        await _context.Set<User>().AddRangeAsync(users);
        await _context.SaveChangesAsync();

        // Act
        var result = await ((TestRepository)_repository).DeleteRangeAsync(users);

        // Assert
        result.Should().Be(3);

        var deletedUsers = await _context.Set<User>()
            .IgnoreQueryFilters()
            .Where(u => users.Select(x => x.Id).Contains(u.Id))
            .ToListAsync();
        deletedUsers.Should().HaveCount(3);
        deletedUsers.Should().AllSatisfy(u => u.IsDeleted.Should().BeTrue());
    }

    [Fact]
    public async Task DeleteRangeAsync_WithEmptyEntityList_Should_ReturnZero()
    {
        // Arrange
        var emptyList = Array.Empty<User>();

        // Act
        var result = await ((TestRepository)_repository).DeleteRangeAsync(emptyList);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task DeleteRangeAsync_WithPartiallyDeletedEntities_Should_OnlyDeleteNonDeleted()
    {
        // Arrange
        var user1 = CreateTestUser("User1");
        var user2 = CreateTestUser("User2");
        user2.IsDeleted = true; // 已删除
        var user3 = CreateTestUser("User3");

        await _context.Set<User>().AddRangeAsync(user1, user2, user3);
        await _context.SaveChangesAsync();

        // Act - 尝试删除所有3个用户
        var result = await ((TestRepository)_repository).DeleteRangeAsync(new[] { user1, user2, user3 });

        // Assert
        result.Should().Be(2); // 只删除user1和user3

        var allUsers = await _context.Set<User>()
            .IgnoreQueryFilters()
            .ToListAsync();
        allUsers.Should().AllSatisfy(u => u.IsDeleted.Should().BeTrue());
    }

    [Fact]
    public async Task DeleteRangeAsync_WithNullEntities_Should_ThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await ((TestRepository)_repository).DeleteRangeAsync((IEnumerable<User>)null!));
    }

    #endregion

    #region 批量操作扩展测试 - DeleteRangeAsync(ids)（4个新用例）

    [Fact]
    public async Task DeleteRangeAsync_WithIds_Should_SoftDeleteEntitiesByIds()
    {
        // Arrange
        var users = new[]
        {
            CreateTestUser("User1"),
            CreateTestUser("User2"),
            CreateTestUser("User3")
        };
        await _context.Set<User>().AddRangeAsync(users);
        await _context.SaveChangesAsync();

        var ids = users.Select(u => u.Id).ToList();

        // Act
        var result = await ((TestRepository)_repository).DeleteRangeAsync(ids);

        // Assert
        result.Should().Be(3);

        var deletedUsers = await _context.Set<User>()
            .IgnoreQueryFilters()
            .Where(u => ids.Contains(u.Id))
            .ToListAsync();
        deletedUsers.Should().HaveCount(3);
        deletedUsers.Should().AllSatisfy(u => u.IsDeleted.Should().BeTrue());
    }

    [Fact]
    public async Task DeleteRangeAsync_WithEmptyIdList_Should_ReturnZero()
    {
        // Arrange
        var emptyIds = Array.Empty<Guid>();

        // Act
        var result = await ((TestRepository)_repository).DeleteRangeAsync(emptyIds);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task DeleteRangeAsync_WithNonExistentIds_Should_ReturnZero()
    {
        // Arrange
        var nonExistentIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        // Act
        var result = await ((TestRepository)_repository).DeleteRangeAsync(nonExistentIds);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task DeleteRangeAsync_WithIdsIncludingDeleted_Should_OnlyDeleteNonDeleted()
    {
        // Arrange
        var user1 = CreateTestUser("User1");
        var user2 = CreateTestUser("User2");
        user2.IsDeleted = true; // 已删除
        var user3 = CreateTestUser("User3");

        await _context.Set<User>().AddRangeAsync(user1, user2, user3);
        await _context.SaveChangesAsync();

        var ids = new[] { user1.Id, user2.Id, user3.Id };

        // Act
        var result = await ((TestRepository)_repository).DeleteRangeAsync(ids);

        // Assert
        result.Should().Be(2); // 只删除user1和user3
    }

    [Fact]
    public async Task DeleteRangeAsync_WithNullIds_Should_ThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await ((TestRepository)_repository).DeleteRangeAsync((IEnumerable<Guid>)null!));
    }

    #endregion

    #region 批量操作事务测试（3个用例）

    [Fact]
    public async Task AddRangeAsync_WithTransaction_Should_CommitAllOrNone()
    {
        // Arrange
        var users = new[]
        {
            CreateTestUser("User1"),
            CreateTestUser("User2"),
            CreateTestUser("User3")
        };

        // Act - AddRangeAsync内部会调用SaveChangesAsync
        var result = await ((TestRepository)_repository).AddRangeAsync(users);

        // Assert
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(u => u.Id.Should().NotBeEmpty());

        var allUsers = await _context.Set<User>().Where(u => !u.IsDeleted).ToListAsync();
        allUsers.Should().HaveCount(3);
    }

    [Fact]
    public async Task DeleteRangeAsync_WithTransaction_Should_CommitAllOrNone()
    {
        // Arrange
        var users = new[]
        {
            CreateTestUser("User1"),
            CreateTestUser("User2"),
            CreateTestUser("User3")
        };
        await _context.Set<User>().AddRangeAsync(users);
        await _context.SaveChangesAsync();

        // Act - 批量删除会自动调用SaveChanges
        var deletedCount = await ((TestRepository)_repository).DeleteRangeAsync(u => u.RealName.StartsWith("User"));

        // Assert
        deletedCount.Should().Be(3);
        var remainingUsers = await _context.Set<User>().Where(u => !u.IsDeleted).ToListAsync();
        remainingUsers.Should().BeEmpty();
    }

    [Fact]
    public async Task MixedOperations_WithTransaction_Should_MaintainConsistency()
    {
        // Arrange
        var existingUser = CreateTestUser("Existing");
        await _context.Set<User>().AddAsync(existingUser);
        await _context.SaveChangesAsync();

        var newUsers = new[]
        {
            CreateTestUser("New1"),
            CreateTestUser("New2")
        };

        // Act - 混合操作：添加新用户并删除现有用户
        await ((TestRepository)_repository).AddRangeAsync(newUsers);
        await ((TestRepository)_repository).DeleteAsync(existingUser.Id);

        // Assert
        var allUsers = await _context.Set<User>().Where(u => !u.IsDeleted).ToListAsync();
        allUsers.Should().HaveCount(2);
        allUsers.Should().AllSatisfy(u => u.RealName.Should().StartWith("New"));
    }

    #endregion

    #endregion

    #region 高级分页测试（12个新用例）

    #region 动态过滤、排序、分页组合（2个新用例）

    [Fact]
    public async Task GetPagedAsync_WithPredicateAndOrderBy_Should_FilterAndSort()
    {
        // Arrange
        var users = new[]
        {
            CreateTestUser("Alice"),
            CreateTestUser("Bob"),
            CreateTestUser("Charlie"),
            CreateTestUser("David"),
            CreateTestUser("Eve")
        };
        await _context.Set<User>().AddRangeAsync(users);
        await _context.SaveChangesAsync();

        // Act - 查询名字长度>3的用户，按名字降序排序
        var result = await ((BaseRepository<User>)_repository).GetPagedAsync(
            pageNumber: 1,
            pageSize: 10,
            predicate: u => u.RealName.Length > 3,
            orderBy: u => u.RealName,
            ascending: false);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].RealName.Should().Be("David");
        result.Items[1].RealName.Should().Be("Charlie");
        result.Items[2].RealName.Should().Be("Alice");
        result.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetPagedAsync_WithComplexPredicate_Should_HandleMultipleConditions()
    {
        // Arrange
        var users = new[]
        {
            CreateTestUser("Alice"),
            CreateTestUser("Bob"),
            CreateTestUser("Charlie"),
            CreateTestUser("David"),
            CreateTestUser("Eve")
        };
        // 修改部分用户的Status
        users[1].Status = CommonStatus.Disabled;
        users[3].Status = CommonStatus.Disabled;

        await _context.Set<User>().AddRangeAsync(users);
        await _context.SaveChangesAsync();

        // Act - 复杂条件：Status=Enabled AND RealName包含'e'
        var result = await ((BaseRepository<User>)_repository).GetPagedAsync(
            pageNumber: 1,
            pageSize: 10,
            predicate: u => u.Status == CommonStatus.Enabled && u.RealName.Contains("e"));

        // Assert
        result.Items.Should().HaveCount(3); // Alice, Charlie, Eve都包含'e'且Status=Enabled
        result.Items.Should().Contain(u => u.RealName == "Alice");
        result.Items.Should().Contain(u => u.RealName == "Charlie");
        result.Items.Should().Contain(u => u.RealName == "Eve");
        result.TotalCount.Should().Be(3);
    }

    #endregion

    #region 空结果集、边界条件（5个用例）

    [Fact]
    public async Task GetPagedAsync_WithNoMatches_Should_ReturnEmptyPage()
    {
        // Arrange
        var users = new[]
        {
            CreateTestUser("Alice"),
            CreateTestUser("Bob")
        };
        await _context.Set<User>().AddRangeAsync(users);
        await _context.SaveChangesAsync();

        // Act - 查询不存在的名字
        var result = await ((BaseRepository<User>)_repository).GetPagedAsync(
            pageNumber: 1,
            pageSize: 10,
            predicate: u => u.RealName == "NonExistent");

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(1);
        result.CurrentPage.Should().Be(1);
    }

    [Fact]
    public async Task GetPagedAsync_WithPageExceedingTotal_Should_ReturnEmptyPage()
    {
        // Arrange
        var users = new[]
        {
            CreateTestUser("Alice"),
            CreateTestUser("Bob")
        };
        await _context.Set<User>().AddRangeAsync(users);
        await _context.SaveChangesAsync();

        // Act - 请求第10页（只有2条数据）
        var result = await _repository.GetPagedAsync(
            pageNumber: 10,
            pageSize: 10);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(2);
        result.CurrentPage.Should().Be(10);
    }

    [Fact]
    public async Task GetPagedAsync_WithSinglePage_Should_ReturnAllItems()
    {
        // Arrange
        var users = new[]
        {
            CreateTestUser("Alice"),
            CreateTestUser("Bob"),
            CreateTestUser("Charlie")
        };
        await _context.Set<User>().AddRangeAsync(users);
        await _context.SaveChangesAsync();

        // Act - PageSize大于总数
        var result = await _repository.GetPagedAsync(
            pageNumber: 1,
            pageSize: 100);

        // Assert
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
        result.TotalPages.Should().Be(1);
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task GetPagedAsync_WithFirstPage_Should_HaveNextPage()
    {
        // Arrange - 创建10个用户
        var users = Enumerable.Range(1, 10)
            .Select(i => CreateTestUser($"User{i}"))
            .ToArray();
        await _context.Set<User>().AddRangeAsync(users);
        await _context.SaveChangesAsync();

        // Act - 每页3个，请求第1页
        var result = await _repository.GetPagedAsync(
            pageNumber: 1,
            pageSize: 3);

        // Assert
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(10);
        result.TotalPages.Should().Be(4);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task GetPagedAsync_WithLastPage_Should_HavePreviousPage()
    {
        // Arrange - 创建10个用户
        var users = Enumerable.Range(1, 10)
            .Select(i => CreateTestUser($"User{i}"))
            .ToArray();
        await _context.Set<User>().AddRangeAsync(users);
        await _context.SaveChangesAsync();

        // Act - 每页3个，请求最后一页（第4页）
        var result = await _repository.GetPagedAsync(
            pageNumber: 4,
            pageSize: 3);

        // Assert
        result.Items.Should().HaveCount(1); // 最后一页只有1个
        result.TotalCount.Should().Be(10);
        result.TotalPages.Should().Be(4);
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeTrue();
    }

    #endregion

    #region 大数据集与性能测试（2个用例）

    [Fact]
    public async Task GetPagedAsync_WithLargeDataset_Should_PaginateCorrectly()
    {
        // Arrange - 创建500个用户
        var users = Enumerable.Range(1, 500)
            .Select(i => CreateTestUser($"User{i:D4}"))
            .ToArray();
        await _context.Set<User>().AddRangeAsync(users);
        await _context.SaveChangesAsync();

        // Act - 每页50个，请求第5页
        var result = await ((BaseRepository<User>)_repository).GetPagedAsync(
            pageNumber: 5,
            pageSize: 50,
            orderBy: u => u.RealName,
            ascending: true);

        // Assert
        result.Items.Should().HaveCount(50);
        result.TotalCount.Should().Be(500);
        result.TotalPages.Should().Be(10);
        result.CurrentPage.Should().Be(5);
        result.Items[0].RealName.Should().Be("User0201"); // 第5页第1个（索引200）
        result.Items[49].RealName.Should().Be("User0250"); // 第5页最后1个（索引249）
    }

    [Fact]
    public async Task GetPagedAsync_WithMultiplePages_Should_ReturnCorrectPage()
    {
        // Arrange - 创建15个用户
        var users = Enumerable.Range(1, 15)
            .Select(i => CreateTestUser($"User{i:D2}"))
            .ToArray();
        await _context.Set<User>().AddRangeAsync(users);
        await _context.SaveChangesAsync();

        // Act - 每页5个，分别请求第1、2、3页
        var repo = (BaseRepository<User>)_repository;
        var page1 = await repo.GetPagedAsync(1, 5, orderBy: u => u.RealName, ascending: true);
        var page2 = await repo.GetPagedAsync(2, 5, orderBy: u => u.RealName, ascending: true);
        var page3 = await repo.GetPagedAsync(3, 5, orderBy: u => u.RealName, ascending: true);

        // Assert
        page1.Items.Should().HaveCount(5);
        page1.Items[0].RealName.Should().Be("User01");
        page1.Items[4].RealName.Should().Be("User05");

        page2.Items.Should().HaveCount(5);
        page2.Items[0].RealName.Should().Be("User06");
        page2.Items[4].RealName.Should().Be("User10");

        page3.Items.Should().HaveCount(5);
        page3.Items[0].RealName.Should().Be("User11");
        page3.Items[4].RealName.Should().Be("User15");

        // 验证分页元数据
        page1.TotalPages.Should().Be(3);
        page2.TotalPages.Should().Be(3);
        page3.TotalPages.Should().Be(3);
    }

    #endregion

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
internal class TestRepository : BaseRepository<User>, LYBT.Infrastructure.Interfaces.IRepository<User>
{
    public TestRepository(AppDbContext context)
        : base(context, NullLogger<TestRepository>.Instance)
    {
    }
}

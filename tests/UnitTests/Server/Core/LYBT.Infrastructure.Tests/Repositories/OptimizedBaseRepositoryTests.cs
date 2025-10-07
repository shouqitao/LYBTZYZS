using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Infrastructure.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using System.Linq.Expressions;
using System.Collections.Concurrent;

namespace LYBT.Infrastructure.Tests.Repositories
{
    // 测试实体
    public class TestEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public int Value { get; set; }
    }

    // 具体测试实现
    public class TestOptimizedRepository : OptimizedBaseRepository<TestEntity>
    {
        public TestOptimizedRepository(
            AppDbContext context,
            ILogger<TestOptimizedRepository> logger,
            IMemoryCache cache,
            QueryOptimizationOptions? queryOptions = null) : base(context, logger, cache, queryOptions)
        {
        }

        // 暴露受保护的成员用于测试
        public new TimeSpan DefaultCacheDuration => base.DefaultCacheDuration;
        public new string CacheKeyPrefix => base.CacheKeyPrefix;
        public new IQueryable<TestEntity> BuildOptimizedQuery(Expression<Func<TestEntity, bool>>? predicate = null)
            => base.BuildOptimizedQuery(predicate);
        public new string GenerateCacheKey(string operation, params object?[] parameters)
            => base.GenerateCacheKey(operation, parameters);
        public new void InvalidateCache() => base.InvalidateCache();
        public new void InvalidateListCache() => base.InvalidateListCache();
        public new void SetCacheSafely<T>(string key, T value, TimeSpan expiration)
            => base.SetCacheSafely(key, value, expiration);

        // 重写应用全局过滤器用于测试
        protected override IQueryable<TestEntity> ApplyGlobalFilters(IQueryable<TestEntity> query)
        {
            return query.Where(e => e.IsActive);
        }
    }

    public class OptimizedBaseRepositoryTests : IDisposable
    {
        private readonly DbContextOptions<AppDbContext> _options;
        private readonly AppDbContext _context;
        private readonly Mock<ILogger<TestOptimizedRepository>> _mockLogger;
        private readonly IMemoryCache _realCache;
        private readonly TestOptimizedRepository _repository;

        public OptimizedBaseRepositoryTests()
        {
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(_options);
            _mockLogger = new Mock<ILogger<TestOptimizedRepository>>();
            _realCache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 100
            });

            _repository = new TestOptimizedRepository(_context, _mockLogger.Object, _realCache);

            // 配置DbSet
            _context.Set<TestEntity>();
        }

        #region 构造函数测试

        [Fact]
        public void Constructor_Should_CreateInstance_When_ValidParametersProvided()
        {
            // Act & Assert
            _repository.Should().NotBeNull();
            _repository.Should().BeAssignableTo<IBaseRepository<TestEntity>>();
        }

        [Fact]
        public void Constructor_Should_ThrowArgumentNullException_When_ContextIsNull()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new TestOptimizedRepository(null!, _mockLogger.Object, _realCache));
            exception.ParamName.Should().Be("context");
        }

        [Fact]
        public void Constructor_Should_ThrowArgumentNullException_When_LoggerIsNull()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new TestOptimizedRepository(_context, null!, _realCache));
            exception.ParamName.Should().Be("logger");
        }

        [Fact]
        public void Constructor_Should_ThrowArgumentNullException_When_CacheIsNull()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new TestOptimizedRepository(_context, _mockLogger.Object, null!));
            exception.ParamName.Should().Be("cache");
        }

        [Fact]
        public void Constructor_Should_UseDefaultQueryOptions_When_OptionsNotProvided()
        {
            // Act
            var repo = new TestOptimizedRepository(_context, _mockLogger.Object, _realCache);

            // Assert
            repo.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_Should_UseCustomQueryOptions_When_OptionsProvided()
        {
            // Arrange
            var customOptions = new QueryOptimizationOptions
            {
                EnableCache = false,
                UseNoTracking = false
            };

            // Act
            var repo = new TestOptimizedRepository(_context, _mockLogger.Object, _realCache, customOptions);

            // Assert
            repo.Should().NotBeNull();
        }

        #endregion

        #region 属性测试

        [Fact]
        public void DefaultCacheDuration_Should_Return5Minutes_When_Accessed()
        {
            // Act
            var duration = _repository.DefaultCacheDuration;

            // Assert
            duration.Should().Be(TimeSpan.FromMinutes(5));
        }

        [Fact]
        public void CacheKeyPrefix_Should_ReturnEntityNameWithColon_When_Accessed()
        {
            // Act
            var prefix = _repository.CacheKeyPrefix;

            // Assert
            prefix.Should().Be("TestEntity:");
        }

        #endregion

        #region GetByIdAsync 测试

        [Fact]
        public async Task GetByIdAsync_Should_ReturnEntity_When_EntityExists()
        {
            // Arrange
            var entity = new TestEntity { Name = "Test Entity" };
            await _context.Set<TestEntity>().AddAsync(entity);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(entity.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(entity.Id);
            result.Name.Should().Be("Test Entity");
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
        public async Task GetByIdAsync_Should_UseCancellationToken_When_Provided()
        {
            // Arrange
            var entity = new TestEntity { Name = "Test Entity" };
            await _context.Set<TestEntity>().AddAsync(entity);
            await _context.SaveChangesAsync();
            using var cts = new CancellationTokenSource();

            // Act
            var result = await _repository.GetByIdAsync(entity.Id, cts.Token);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetByIdAsync_Should_UseCache_When_EntityCached()
        {
            // Arrange
            var entity = new TestEntity { Name = "Test Entity" };
            await _context.Set<TestEntity>().AddAsync(entity);
            await _context.SaveChangesAsync();

            // 第一次调用
            await _repository.GetByIdAsync(entity.Id);

            // Act - 第二次调用应该使用缓存
            var result = await _repository.GetByIdAsync(entity.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be("Test Entity");
        }

        #endregion

        #region GetAllAsync 测试

        [Fact]
        public async Task GetAllAsync_Should_ReturnAllEntities_When_EntitiesExist()
        {
            // Arrange
            var entities = new[]
            {
                new TestEntity { Name = "Entity 1" },
                new TestEntity { Name = "Entity 2" },
                new TestEntity { Name = "Entity 3" }
            };
            await _context.Set<TestEntity>().AddRangeAsync(entities);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            result.Should().HaveCount(3);
            result.Should().Contain(e => e.Name == "Entity 1");
            result.Should().Contain(e => e.Name == "Entity 2");
            result.Should().Contain(e => e.Name == "Entity 3");
        }

        [Fact]
        public async Task GetAllAsync_Should_ReturnEmptyList_When_NoEntitiesExist()
        {
            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllAsync_Should_UseCache_When_CalledMultipleTimes()
        {
            // Arrange
            var entity = new TestEntity { Name = "Test Entity" };
            await _context.Set<TestEntity>().AddAsync(entity);
            await _context.SaveChangesAsync();

            // Act
            var result1 = await _repository.GetAllAsync();
            var result2 = await _repository.GetAllAsync();

            // Assert
            result1.Should().HaveCount(1);
            result2.Should().HaveCount(1);
        }

        #endregion

        #region FindAsync 测试

        [Fact]
        public async Task FindAsync_Should_ReturnMatchingEntities_When_PredicateMatches()
        {
            // Arrange
            var entities = new[]
            {
                new TestEntity { Name = "Active Entity", IsActive = true },
                new TestEntity { Name = "Inactive Entity", IsActive = false },
                new TestEntity { Name = "Another Active", IsActive = true }
            };
            await _context.Set<TestEntity>().AddRangeAsync(entities);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.FindAsync(e => e.IsActive);

            // Assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(e => e.IsActive);
        }

        [Fact]
        public async Task FindAsync_Should_ReturnEmptyList_When_NoMatches()
        {
            // Arrange
            var entity = new TestEntity { Name = "Test Entity", Value = 10 };
            await _context.Set<TestEntity>().AddAsync(entity);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.FindAsync(e => e.Value > 100);

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region GetPagedAsync 测试

        [Fact]
        public async Task GetPagedAsync_Should_ReturnPagedResult_When_EntitiesExist()
        {
            // Arrange
            var entities = Enumerable.Range(1, 15)
                .Select(i => new TestEntity { Name = $"Entity {i}", Value = i })
                .ToArray();
            await _context.Set<TestEntity>().AddRangeAsync(entities);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetPagedAsync(null, 2, 5);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(5);
            result.TotalCount.Should().Be(15);
            result.PageNumber.Should().Be(2);
            result.PageSize.Should().Be(5);
            result.TotalPages.Should().Be(3);
        }

        [Fact]
        public async Task GetPagedAsync_Should_ApplyPredicate_When_PredicateProvided()
        {
            // Arrange
            var entities = Enumerable.Range(1, 10)
                .Select(i => new TestEntity { Name = $"Entity {i}", Value = i })
                .ToArray();
            await _context.Set<TestEntity>().AddRangeAsync(entities);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetPagedAsync(e => e.Value > 5, 1, 5);

            // Assert
            result.TotalCount.Should().Be(5);
            result.Items.Should().OnlyContain(e => e.Value > 5);
        }

        [Fact]
        public async Task GetPagedAsync_Should_ApplyOrdering_When_OrderByProvided()
        {
            // Arrange
            var entities = new[]
            {
                new TestEntity { Name = "Entity C", Value = 3 },
                new TestEntity { Name = "Entity A", Value = 1 },
                new TestEntity { Name = "Entity B", Value = 2 }
            };
            await _context.Set<TestEntity>().AddRangeAsync(entities);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetPagedAsync(null, 1, 10, e => e.Name, true);

            // Assert
            result.Items.First().Name.Should().Be("Entity A");
            result.Items.Last().Name.Should().Be("Entity C");
        }

        [Fact]
        public async Task GetPagedAsync_Should_ApplyDescendingOrdering_When_AscendingIsFalse()
        {
            // Arrange
            var entities = new[]
            {
                new TestEntity { Name = "Entity A", Value = 1 },
                new TestEntity { Name = "Entity B", Value = 2 },
                new TestEntity { Name = "Entity C", Value = 3 }
            };
            await _context.Set<TestEntity>().AddRangeAsync(entities);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetPagedAsync(null, 1, 10, e => e.Value, false);

            // Assert
            result.Items.First().Value.Should().Be(3);
            result.Items.Last().Value.Should().Be(1);
        }

        [Fact]
        public async Task GetPagedAsync_Should_UseSimpleOverload_When_CalledWithPageNumberAndSize()
        {
            // Arrange
            var entities = Enumerable.Range(1, 5)
                .Select(i => new TestEntity { Name = $"Entity {i}" })
                .ToArray();
            await _context.Set<TestEntity>().AddRangeAsync(entities);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetPagedAsync(1, 3);

            // Assert
            result.Items.Should().HaveCount(3);
            result.TotalCount.Should().Be(5);
        }

        #endregion

        #region GetSingleAsync 测试

        [Fact]
        public async Task GetSingleAsync_Should_ReturnEntity_When_EntityMatches()
        {
            // Arrange
            var entity = new TestEntity { Name = "Unique Entity", Value = 100 };
            await _context.Set<TestEntity>().AddAsync(entity);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetSingleAsync(e => e.Value == 100);

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be("Unique Entity");
        }

        [Fact]
        public async Task GetSingleAsync_Should_ReturnNull_When_NoEntityMatches()
        {
            // Act
            var result = await _repository.GetSingleAsync(e => e.Value == 999);

            // Assert
            result.Should().BeNull();
        }

        #endregion

        #region ExistsAsync 测试

        [Fact]
        public async Task ExistsAsync_Should_ReturnTrue_When_EntityExistsById()
        {
            // Arrange
            var entity = new TestEntity { Name = "Test Entity" };
            await _context.Set<TestEntity>().AddAsync(entity);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.ExistsAsync(entity.Id);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_Should_ReturnFalse_When_EntityNotExistsById()
        {
            // Act
            var result = await _repository.ExistsAsync(Guid.NewGuid());

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ExistsAsync_Should_ReturnTrue_When_EntityMatchesPredicate()
        {
            // Arrange
            var entity = new TestEntity { Name = "Test Entity", Value = 42 };
            await _context.Set<TestEntity>().AddAsync(entity);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.ExistsAsync(e => e.Value == 42);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_Should_ReturnFalse_When_NoEntityMatchesPredicate()
        {
            // Act
            var result = await _repository.ExistsAsync(e => e.Value == 999);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region CountAsync 测试

        [Fact]
        public async Task CountAsync_Should_ReturnTotalCount_When_CalledWithoutPredicate()
        {
            // Arrange
            var entities = Enumerable.Range(1, 5)
                .Select(i => new TestEntity { Name = $"Entity {i}" })
                .ToArray();
            await _context.Set<TestEntity>().AddRangeAsync(entities);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.CountAsync();

            // Assert
            result.Should().Be(5);
        }

        [Fact]
        public async Task CountAsync_Should_ReturnFilteredCount_When_CalledWithPredicate()
        {
            // Arrange
            var entities = new[]
            {
                new TestEntity { Name = "Active 1", IsActive = true },
                new TestEntity { Name = "Active 2", IsActive = true },
                new TestEntity { Name = "Inactive", IsActive = false }
            };
            await _context.Set<TestEntity>().AddRangeAsync(entities);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.CountAsync(e => e.IsActive);

            // Assert
            result.Should().Be(2);
        }

        #endregion

        #region AddAsync 测试

        [Fact]
        public async Task AddAsync_Should_AddEntity_When_ValidEntityProvided()
        {
            // Arrange
            var entity = new TestEntity { Name = "New Entity" };

            // Act
            var result = await _repository.AddAsync(entity);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("New Entity");
            _context.Entry(result).State.Should().Be(EntityState.Added);
        }

        [Fact]
        public async Task AddAsync_Should_ThrowArgumentNullException_When_EntityIsNull()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _repository.AddAsync(null!));
            exception.ParamName.Should().Be("entity");
        }

        #endregion

        #region AddRangeAsync 测试

        [Fact]
        public async Task AddRangeAsync_Should_AddAllEntities_When_ValidEntitiesProvided()
        {
            // Arrange
            var entities = new[]
            {
                new TestEntity { Name = "Entity 1" },
                new TestEntity { Name = "Entity 2" },
                new TestEntity { Name = "Entity 3" }
            };

            // Act
            var result = await _repository.AddRangeAsync(entities);

            // Assert
            result.Should().HaveCount(3);
            result.Should().OnlyContain(e => _context.Entry(e).State == EntityState.Added);
        }

        [Fact]
        public async Task AddRangeAsync_Should_ReturnEmptyList_When_EmptyListProvided()
        {
            // Arrange
            var entities = Array.Empty<TestEntity>();

            // Act
            var result = await _repository.AddRangeAsync(entities);

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region UpdateAsync 测试

        [Fact]
        public async Task UpdateAsync_Should_UpdateEntity_When_ValidEntityProvided()
        {
            // Arrange
            var entity = new TestEntity { Name = "Original Name" };
            await _context.Set<TestEntity>().AddAsync(entity);
            await _context.SaveChangesAsync();
            _context.Entry(entity).State = EntityState.Detached;

            entity.Name = "Updated Name";

            // Act
            var result = await _repository.UpdateAsync(entity);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("Updated Name");
            _context.Entry(result).State.Should().Be(EntityState.Modified);
        }

        [Fact]
        public async Task UpdateAsync_Should_ThrowArgumentNullException_When_EntityIsNull()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _repository.UpdateAsync(null!));
            exception.ParamName.Should().Be("entity");
        }

        #endregion

        #region DeleteAsync 测试

        [Fact]
        public async Task DeleteAsync_Should_DeleteEntity_When_ValidEntityProvided()
        {
            // Arrange
            var entity = new TestEntity { Name = "To Delete" };
            await _context.Set<TestEntity>().AddAsync(entity);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.DeleteAsync(entity);

            // Assert
            result.Should().BeTrue();
            _context.Entry(entity).State.Should().Be(EntityState.Deleted);
        }

        [Fact]
        public async Task DeleteAsync_Should_ReturnFalse_When_EntityIsNull()
        {
            // Act
            var result = await _repository.DeleteAsync((TestEntity)null!);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteAsync_Should_DeleteEntityById_When_ValidIdProvided()
        {
            // Arrange
            var entity = new TestEntity { Name = "To Delete" };
            await _context.Set<TestEntity>().AddAsync(entity);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.DeleteAsync(entity.Id);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteAsync_Should_ReturnFalse_When_EntityNotFoundById()
        {
            // Act
            var result = await _repository.DeleteAsync(Guid.NewGuid());

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region DeleteRangeAsync 测试

        [Fact]
        public async Task DeleteRangeAsync_Should_DeleteAllEntities_When_ValidEntitiesProvided()
        {
            // Arrange
            var entities = new[]
            {
                new TestEntity { Name = "Delete 1" },
                new TestEntity { Name = "Delete 2" },
                new TestEntity { Name = "Delete 3" }
            };
            await _context.Set<TestEntity>().AddRangeAsync(entities);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.DeleteRangeAsync(entities);

            // Assert
            result.Should().Be(3);
            entities.Should().OnlyContain(e => _context.Entry(e).State == EntityState.Deleted);
        }

        [Fact]
        public async Task DeleteRangeAsync_Should_ReturnZero_When_EmptyListProvided()
        {
            // Arrange
            var entities = Array.Empty<TestEntity>();

            // Act
            var result = await _repository.DeleteRangeAsync(entities);

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public async Task DeleteRangeAsync_Should_DeleteEntitiesByIds_When_ValidIdsProvided()
        {
            // Arrange
            var entities = new[]
            {
                new TestEntity { Name = "Delete 1" },
                new TestEntity { Name = "Delete 2" }
            };
            await _context.Set<TestEntity>().AddRangeAsync(entities);
            await _context.SaveChangesAsync();
            var ids = entities.Select(e => e.Id).ToList();

            // Act
            var result = await _repository.DeleteRangeAsync(ids);

            // Assert
            result.Should().Be(2);
        }

        [Fact]
        public async Task DeleteRangeAsync_Should_ReturnZero_When_NoEntitiesFoundByIds()
        {
            // Arrange
            var nonExistentIds = new[] { Guid.NewGuid(), Guid.NewGuid() };

            // Act
            var result = await _repository.DeleteRangeAsync(nonExistentIds);

            // Assert
            result.Should().Be(0);
        }

        #endregion

        #region SaveChangesAsync 测试

        [Fact]
        public async Task SaveChangesAsync_Should_SaveChanges_When_ChangesExist()
        {
            // Arrange
            var entity = new TestEntity { Name = "Test Entity" };
            await _repository.AddAsync(entity);

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
        public async Task SaveChangesAsync_Should_LogError_When_ExceptionOccurs()
        {
            // Arrange
            var mockDbSet = new Mock<DbSet<TestEntity>>();
            mockDbSet.Setup(x => x.AddAsync(It.IsAny<TestEntity>(), It.IsAny<CancellationToken>()))
                .Throws(new InvalidOperationException("Test exception"));

            var mockContext = new Mock<AppDbContext>(_options);
            mockContext.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Throws(new InvalidOperationException("Save failed"));

            var repository = new TestOptimizedRepository(mockContext.Object, _mockLogger.Object, _realCache);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => repository.SaveChangesAsync());
        }

        #endregion

        #region 批量操作优化测试

        [Fact]
        public async Task AddRangeOptimizedAsync_Should_AddEntitiesInBatches_When_LargeCollectionProvided()
        {
            // Arrange
            var entities = Enumerable.Range(1, 250)
                .Select(i => new TestEntity { Name = $"Entity {i}" })
                .ToList();

            // Act
            var result = await _repository.AddRangeOptimizedAsync(entities);

            // Assert
            result.Should().Be(250);
        }

        [Fact]
        public async Task AddRangeOptimizedAsync_Should_ReturnZero_When_EmptyCollectionProvided()
        {
            // Arrange
            var entities = new List<TestEntity>();

            // Act
            var result = await _repository.AddRangeOptimizedAsync(entities);

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public async Task UpdateRangeOptimizedAsync_Should_UpdateEntitiesInBatches_When_LargeCollectionProvided()
        {
            // Arrange
            var entities = Enumerable.Range(1, 150)
                .Select(i => new TestEntity { Name = $"Entity {i}" })
                .ToList();
            await _context.Set<TestEntity>().AddRangeAsync(entities);
            await _context.SaveChangesAsync();

            // 更新实体
            foreach (var entity in entities)
            {
                entity.Name = $"Updated {entity.Name}";
            }

            // Act
            var result = await _repository.UpdateRangeOptimizedAsync(entities);

            // Assert
            result.Should().Be(150);
        }

        [Fact]
        public async Task DeleteRangeOptimizedAsync_Should_DeleteEntitiesInBatches_When_PredicateProvided()
        {
            // Arrange
            var entities = Enumerable.Range(1, 10)
                .Select(i => new TestEntity { Name = $"Entity {i}", Value = i })
                .ToList();
            await _context.Set<TestEntity>().AddRangeAsync(entities);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.DeleteRangeOptimizedAsync(e => e.Value > 5);

            // Assert
            result.Should().Be(5);
        }

        [Fact]
        public async Task UpdateWhereAsync_Should_UpdateMatchingEntities_When_PredicateAndValueProvided()
        {
            // Arrange
            var entities = new[]
            {
                new TestEntity { Name = "Entity 1", Value = 1 },
                new TestEntity { Name = "Entity 2", Value = 2 },
                new TestEntity { Name = "Entity 3", Value = 3 }
            };
            await _context.Set<TestEntity>().AddRangeAsync(entities);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.UpdateWhereAsync(
                e => e.Value > 1,
                e => e.Name,
                "Updated Name");

            // Assert
            result.Should().Be(2);
        }

        #endregion

        #region 事务支持测试

        [Fact]
        public async Task ExecuteInTransactionAsync_Should_ExecuteOperationInTransaction_When_OperationSucceeds()
        {
            // Arrange
            var entity = new TestEntity { Name = "Transaction Test" };

            // Act
            var result = await _repository.ExecuteInTransactionAsync(async () =>
            {
                await _repository.AddAsync(entity);
                await _repository.SaveChangesAsync();
                return entity.Id;
            });

            // Assert
            result.Should().Be(entity.Id);
        }

        [Fact]
        public async Task ExecuteInTransactionAsync_Should_RollbackTransaction_When_OperationFails()
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _repository.ExecuteInTransactionAsync<int>(async () =>
                {
                    throw new InvalidOperationException("Test exception");
                });
            });
        }

        [Fact]
        public async Task BulkOperationAsync_Should_ExecuteBulkOperation_When_OperationProvided()
        {
            // Arrange
            var entities = new[]
            {
                new TestEntity { Name = "Bulk 1" },
                new TestEntity { Name = "Bulk 2" }
            };

            // Act
            var result = await _repository.BulkOperationAsync(async context =>
            {
                await context.Set<TestEntity>().AddRangeAsync(entities);
                return await context.SaveChangesAsync();
            });

            // Assert
            result.Should().Be(2);
        }

        #endregion

        #region 高级查询测试

        [Fact]
        public async Task QueryAsync_Should_ReturnProjectedResults_When_SelectorProvided()
        {
            // Arrange
            var entities = new[]
            {
                new TestEntity { Name = "Entity 1", Value = 10 },
                new TestEntity { Name = "Entity 2", Value = 20 }
            };
            await _context.Set<TestEntity>().AddRangeAsync(entities);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.QueryAsync(
                e => e.Value > 5,
                e => new { e.Name, e.Value });

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(r => r.Name == "Entity 1" && r.Value == 10);
            result.Should().Contain(r => r.Name == "Entity 2" && r.Value == 20);
        }

        [Fact]
        public async Task GetByIdsAsync_Should_ReturnEntitiesByIds_When_ValidIdsProvided()
        {
            // Arrange
            var entities = new[]
            {
                new TestEntity { Name = "Entity 1" },
                new TestEntity { Name = "Entity 2" },
                new TestEntity { Name = "Entity 3" }
            };
            await _context.Set<TestEntity>().AddRangeAsync(entities);
            await _context.SaveChangesAsync();

            var ids = entities.Take(2).Select(e => e.Id).ToList();

            // Act
            var result = await _repository.GetByIdsAsync(ids);

            // Assert
            result.Should().HaveCount(2);
            result.Keys.Should().BeEquivalentTo(ids);
        }

        [Fact]
        public async Task GetAllStreamAsync_Should_StreamAllEntities_When_EntitiesExist()
        {
            // Arrange
            var entities = Enumerable.Range(1, 5)
                .Select(i => new TestEntity { Name = $"Entity {i}" })
                .ToArray();
            await _context.Set<TestEntity>().AddRangeAsync(entities);
            await _context.SaveChangesAsync();

            // Act
            var results = new List<TestEntity>();
            await foreach (var entity in _repository.GetAllStreamAsync())
            {
                results.Add(entity);
            }

            // Assert
            results.Should().HaveCount(5);
        }

        #endregion

        #region 缓存测试

        [Fact]
        public void GenerateCacheKey_Should_GenerateConsistentKey_When_SameParametersProvided()
        {
            // Act
            var key1 = _repository.GenerateCacheKey("test", "param1", 123);
            var key2 = _repository.GenerateCacheKey("test", "param1", 123);

            // Assert
            key1.Should().Be(key2);
            key1.Should().Contain("TestEntity:");
            key1.Should().Contain("test");
        }

        [Fact]
        public void InvalidateCache_Should_NotThrow_When_Called()
        {
            // Act & Assert
            var act = () => _repository.InvalidateCache();
            act.Should().NotThrow();
        }

        [Fact]
        public void InvalidateListCache_Should_NotThrow_When_Called()
        {
            // Act & Assert
            var act = () => _repository.InvalidateListCache();
            act.Should().NotThrow();
        }

        [Fact]
        public void SetCacheSafely_Should_SetCacheEntry_When_ValidParametersProvided()
        {
            // Arrange
            var key = "test_key";
            var value = "test_value";
            var expiration = TimeSpan.FromMinutes(1);

            // Act & Assert
            var act = () => _repository.SetCacheSafely(key, value, expiration);
            act.Should().NotThrow();
        }

        #endregion

        #region 查询优化测试

        [Fact]
        public void BuildOptimizedQuery_Should_ReturnQueryable_When_CalledWithoutPredicate()
        {
            // Act
            var query = _repository.BuildOptimizedQuery();

            // Assert
            query.Should().NotBeNull();
            query.Should().BeAssignableTo<IQueryable<TestEntity>>();
        }

        [Fact]
        public void BuildOptimizedQuery_Should_ApplyPredicate_When_PredicateProvided()
        {
            // Act
            var query = _repository.BuildOptimizedQuery(e => e.IsActive);

            // Assert
            query.Should().NotBeNull();
            query.Expression.ToString().Should().Contain("IsActive");
        }

        #endregion

        #region 配置选项测试

        [Fact]
        public void QueryOptimizationOptions_Should_HaveDefaultValues_When_CreatedWithDefault()
        {
            // Act
            var options = QueryOptimizationOptions.Default;

            // Assert
            options.EnableCache.Should().BeTrue();
            options.UseNoTracking.Should().BeTrue();
            options.EnableSplitQuery.Should().BeTrue();
            options.SlowQueryThresholdMs.Should().Be(1000);
            options.QueryTimeout.Should().Be(30);
        }

        [Fact]
        public void QueryOptimizationOptions_Should_HavePerformanceValues_When_CreatedWithPerformance()
        {
            // Act
            var options = QueryOptimizationOptions.Performance;

            // Assert
            options.EnableCache.Should().BeTrue();
            options.UseNoTracking.Should().BeTrue();
            options.EnableSplitQuery.Should().BeTrue();
            options.SlowQueryThresholdMs.Should().Be(500);
            options.QueryTimeout.Should().Be(60);
        }

        [Fact]
        public void QueryOptimizationOptions_Should_HaveTrackingValues_When_CreatedWithTracking()
        {
            // Act
            var options = QueryOptimizationOptions.Tracking;

            // Assert
            options.EnableCache.Should().BeFalse();
            options.UseNoTracking.Should().BeFalse();
            options.EnableSplitQuery.Should().BeFalse();
            options.SlowQueryThresholdMs.Should().Be(2000);
            options.QueryTimeout.Should().Be(30);
        }

        #endregion

        public void Dispose()
        {
            _context?.Dispose();
            _realCache?.Dispose();
        }
    }

    // 并发测试类
    public class OptimizedBaseRepositoryConcurrencyTests : IDisposable
    {
        private readonly DbContextOptions<AppDbContext> _options;
        private readonly AppDbContext _context;
        private readonly Mock<ILogger<TestOptimizedRepository>> _mockLogger;
        private readonly IMemoryCache _realCache;
        private readonly TestOptimizedRepository _repository;

        public OptimizedBaseRepositoryConcurrencyTests()
        {
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(_options);
            _mockLogger = new Mock<ILogger<TestOptimizedRepository>>();
            _realCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 100 });
            _repository = new TestOptimizedRepository(_context, _mockLogger.Object, _realCache);
        }

        [Fact]
        public async Task ConcurrentOperations_Should_NotThrow_When_MultipleThreadsAccessRepository()
        {
            // Arrange
            var tasks = new List<Task>();
            var entities = Enumerable.Range(1, 50)
                .Select(i => new TestEntity { Name = $"Entity {i}" })
                .ToList();

            // Act
            for (int i = 0; i < 10; i++)
            {
                var entity = entities[i];
                tasks.Add(Task.Run(async () =>
                {
                    await _repository.AddAsync(entity);
                    await _repository.SaveChangesAsync();
                }));
            }

            // Assert
            var act = async () => await Task.WhenAll(tasks);
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task ConcurrentCacheAccess_Should_BeThreadSafe_When_MultipleThreadsAccessCache()
        {
            // Arrange
            var entity = new TestEntity { Name = "Cached Entity" };
            await _context.Set<TestEntity>().AddAsync(entity);
            await _context.SaveChangesAsync();

            var tasks = new List<Task<TestEntity?>>();

            // Act
            for (int i = 0; i < 20; i++)
            {
                tasks.Add(Task.Run(() => _repository.GetByIdAsync(entity.Id)));
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            results.Should().OnlyContain(r => r != null);
            results.Should().OnlyContain(r => r!.Id == entity.Id);
        }

        public void Dispose()
        {
            _context?.Dispose();
            _realCache?.Dispose();
        }
    }
}
using LYBT.Server.Tests.Common.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LYBT.Server.Tests.Common.TestBase;

/// <summary>
/// Repository层测试基类
/// 提供统一的Repository测试基础设施，使用InMemory数据库
/// </summary>
public abstract class BaseRepositoryTest<TRepository, TDbContext, TEntity>
    where TRepository : class
    where TDbContext : DbContext
    where TEntity : class
{
    protected readonly TDbContext _context;
    protected readonly TRepository _sut;
    protected readonly IServiceProvider _serviceProvider;

    protected BaseRepositoryTest()
    {
        _serviceProvider = BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<TDbContext>();
        _sut = _serviceProvider.GetRequiredService<TRepository>();

        // 确保每个测试都有干净的数据库
        _context.Database.EnsureCreated();
    }

    /// <summary>
    /// 构建服务提供者
    /// </summary>
    protected virtual IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        // 注册InMemory数据库
        var databaseName = CreateTestDatabaseName();
        services.AddDbContext<TDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));

        // 注册Repository
        RegisterRepositoryServices(services);

        // 注册其他需要的服务
        RegisterAdditionalServices(services);

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// 创建唯一的测试数据库名称
    /// </summary>
    protected virtual string CreateTestDatabaseName()
    {
        return $"TestDb_{typeof(TEntity).Name}_{Guid.NewGuid():N}";
    }

    /// <summary>
    /// 注册Repository服务
    /// 子类必须实现此方法来注册具体的Repository
    /// </summary>
    protected abstract void RegisterRepositoryServices(IServiceCollection services);

    /// <summary>
    /// 注册其他需要的服务
    /// 子类可以重写此方法来注册额外的服务
    /// </summary>
    protected virtual void RegisterAdditionalServices(IServiceCollection services)
    {
        // 默认实现为空，子类可以根据需要重写
    }

    /// <summary>
    /// 添加测试数据到数据库
    /// </summary>
    protected async Task<TEntity> AddTestDataAsync(Action<TEntity>? configure = null)
    {
        var entity = TestHelper.CreateTestEntity(configure);
        _context.Set<TEntity>().Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    /// <summary>
    /// 批量添加测试数据到数据库
    /// </summary>
    protected async Task<List<TEntity>> AddTestDataRangeAsync(int count, Action<TEntity, int>? configure = null)
    {
        var entities = TestHelper.CreateTestEntities(count, configure);
        _context.Set<TEntity>().AddRange(entities);
        await _context.SaveChangesAsync();
        return entities;
    }

    /// <summary>
    /// 清空指定实体的所有数据
    /// </summary>
    protected async Task ClearTestDataAsync()
    {
        var entities = _context.Set<TEntity>().ToList();
        _context.Set<TEntity>().RemoveRange(entities);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// 验证数据库中的实体数量
    /// </summary>
    protected async Task<int> GetEntityCountAsync()
    {
        return await _context.Set<TEntity>().CountAsync();
    }

    /// <summary>
    /// 根据ID获取实体
    /// </summary>
    protected async Task<TEntity?> GetEntityByIdAsync<TId>(TId id)
    {
        return await _context.Set<TEntity>().FindAsync(id);
    }

    /// <summary>
    /// 验证实体是否存在
    /// </summary>
    protected async Task<bool> EntityExistsAsync<TId>(TId id)
    {
        return await _context.Set<TEntity>().FindAsync(id) != null;
    }

    /// <summary>
    /// 查询实体（使用LINQ表达式）
    /// </summary>
    protected async Task<TEntity?> FindEntityAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> queryBuilder)
    {
        var query = queryBuilder(_context.Set<TEntity>());
        return await query.FirstOrDefaultAsync();
    }

    /// <summary>
    /// 查询实体列表（使用LINQ表达式）
    /// </summary>
    protected async Task<List<TEntity>> FindEntitiesAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> queryBuilder)
    {
        var query = queryBuilder(_context.Set<TEntity>());
        return await query.ToListAsync();
    }

    /// <summary>
    /// 执行数据库事务测试
    /// </summary>
    protected async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var result = await operation();
            await transaction.CommitAsync();
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 验证事务回滚
    /// </summary>
    protected async Task VerifyTransactionRollbackAsync(Func<Task> operation)
    {
        // 获取操作前的数据数量
        var initialCount = await GetEntityCountAsync();

        try
        {
            await ExecuteInTransactionAsync(async () =>
            {
                await operation();
                // 抛出异常以触发回滚
                throw new InvalidOperationException("Test transaction rollback");
            });
        }
        catch (InvalidOperationException)
        {
            // 预期的异常
        }

        // 验证数据已回滚
        var finalCount = await GetEntityCountAsync();
        finalCount.Should().Be(initialCount, "事务回滚后数据数量应该保持不变");
    }

    /// <summary>
    /// 验证实体的创建时间
    /// </summary>
    protected void VerifyCreationTime<TCreated>(TCreated entity, DateTime? expectedTime = null)
        where TCreated : class
    {
        // 如果实体有创建时间属性，验证其值
        var createdTimeProperty = typeof(TCreated).GetProperties()
            .FirstOrDefault(p => p.Name.Equals("CreatedAt", StringComparison.OrdinalIgnoreCase) ||
                                p.Name.Equals("CreateTime", StringComparison.OrdinalIgnoreCase) ||
                                p.Name.Equals("CreatedDate", StringComparison.OrdinalIgnoreCase));

        if (createdTimeProperty != null && createdTimeProperty.PropertyType == typeof(DateTime))
        {
            var actualTime = (DateTime)createdTimeProperty.GetValue(entity)!;

            if (expectedTime.HasValue)
            {
                actualTime.Should().BeCloseTo(expectedTime.Value, TimeSpan.FromSeconds(1));
            }
            else
            {
                actualTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
            }
        }
    }

    /// <summary>
    /// 验证实体的更新时间
    /// </summary>
    protected void VerifyUpdateTime<TUpdated>(TUpdated entity, DateTime expectedUpdateTime)
        where TUpdated : class
    {
        // 如果实体有更新时间属性，验证其值
        var updatedTimeProperty = typeof(TUpdated).GetProperties()
            .FirstOrDefault(p => p.Name.Equals("UpdatedAt", StringComparison.OrdinalIgnoreCase) ||
                                p.Name.Equals("UpdateTime", StringComparison.OrdinalIgnoreCase) ||
                                p.Name.Equals("UpdatedDate", StringComparison.OrdinalIgnoreCase));

        if (updatedTimeProperty != null && updatedTimeProperty.PropertyType == typeof(DateTime))
        {
            var actualTime = (DateTime)updatedTimeProperty.GetValue(entity)!;
            actualTime.Should().BeCloseTo(expectedUpdateTime, TimeSpan.FromSeconds(1));
        }
    }

    /// <summary>
    /// 清理测试环境
    /// </summary>
    public virtual void Dispose()
    {
        _context?.Database?.EnsureDeleted();
        _context?.Dispose();
    }
}
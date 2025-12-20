using System.Linq.Expressions;
using LYBT.Entities.Common;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Infrastructure.Repositories;

/// <summary>
/// 只读Repository基类 - 用于从属实体模块
/// 实现IReadRepository接口的5个核心查询方法
/// </summary>
/// <typeparam name="TEntity">实体类型，必须继承自BaseEntity</typeparam>
/// <remarks>
/// 适用场景：
/// - 从属实体模块（Consultation, Prescription）
/// - 写操作通过聚合根（MedicalCase）完成
/// - 符合DDD聚合根边界原则（AR-001）
/// 
/// 设计特点：
/// - 所有方法标记为virtual，允许子类重写
/// - 自动应用软删除过滤（!e.IsDeleted）
/// - 使用EF Core LINQ实现查询逻辑
/// - 构造函数注入AppDbContext和ILogger
/// </remarks>
public abstract class BaseReadRepository<TEntity> : IReadRepository<TEntity>
    where TEntity : BaseEntity
{
    /// <summary>
    /// 数据库上下文
    /// </summary>
    protected readonly AppDbContext _context;

    /// <summary>
    /// 实体DbSet
    /// </summary>
    protected readonly DbSet<TEntity> DbSet;

    /// <summary>
    /// 日志记录器
    /// </summary>
    protected readonly ILogger _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="context">数据库上下文</param>
    /// <param name="logger">日志记录器</param>
    protected BaseReadRepository(AppDbContext context, ILogger logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        DbSet = _context.Set<TEntity>();
    }

    /// <summary>
    /// 根据ID获取实体
    /// </summary>
    /// <param name="id">实体唯一标识符</param>
    /// <returns>找到的实体，不存在或已删除则返回null</returns>
    public virtual async Task<TEntity?> GetByIdAsync(Guid id)
    {
        return await DbSet
            .Where(e => !e.IsDeleted && e.Id == id)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// 获取所有实体
    /// </summary>
    /// <returns>所有未删除的实体集合</returns>
    public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        return await DbSet
            .Where(e => !e.IsDeleted)
            .ToListAsync();
    }

    /// <summary>
    /// 根据条件查询实体
    /// </summary>
    /// <param name="predicate">查询条件表达式</param>
    /// <returns>符合条件的未删除实体集合</returns>
    public virtual async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return await DbSet
            .Where(e => !e.IsDeleted)
            .Where(predicate)
            .ToListAsync();
    }

    /// <summary>
    /// 根据条件获取单个实体
    /// </summary>
    /// <param name="predicate">查询条件表达式</param>
    /// <returns>符合条件的实体，不存在或已删除则返回null</returns>
    /// <exception cref="InvalidOperationException">找到多个匹配实体时抛出</exception>
    public virtual async Task<TEntity?> GetSingleAsync(Expression<Func<TEntity, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return await DbSet
            .Where(e => !e.IsDeleted)
            .Where(predicate)
            .SingleOrDefaultAsync();
    }

    /// <summary>
    /// 统计实体总数量
    /// </summary>
    /// <returns>未删除的实体总数</returns>
    public virtual async Task<long> CountAsync()
    {
        return await DbSet
            .Where(e => !e.IsDeleted)
            .LongCountAsync();
    }
}

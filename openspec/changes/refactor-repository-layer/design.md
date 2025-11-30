# Design: refactor-repository-layer（最优实现）

## Context

当前仓库层架构：
- **BaseRepository<T>**: 聚合根CRUD实现（670+ lines）
- **BaseReadRepository<T>**: 从属实体只读访问（114 lines）
- **接口位置**: LYBT.Shared.Models.Interfaces（错误位置）
- **6个模块仓库**: 各自重写GetPagedAsync，代码重复严重

## Goals / Non-Goals

### Goals
- 接口移至正确位置（Infrastructure层）
- 引入模板方法模式消除分页代码重复
- 统一所有构造函数签名
- 移除实体别名，统一命名

### Non-Goals
- 不引入UnitOfWork模式（当前不需要）
- 不引入CQRS（过度设计）
- 不改变聚合根边界

## Decisions

### DD-001: 接口位置重组

**决策**: 将IRepository/IReadRepository移至Infrastructure层

```
Before:
src/Shared/LYBT.Shared.Models/Interfaces/
├── IRepository.cs
└── IReadRepository.cs

After:
src/Server/Core/LYBT.Infrastructure/Interfaces/
├── IRepository.cs
└── IReadRepository.cs
```

**理由**:
- 接口仅Server端使用，不应在Shared层
- 遵循依赖方向原则
- Desktop端不需要这些接口

### DD-002: 模板方法模式

**决策**: BaseRepository引入模板方法模式处理分页查询

```csharp
public abstract class BaseRepository<TEntity> : IRepository<TEntity>
    where TEntity : class, IEntity
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<TEntity> _dbSet;
    protected readonly ILogger _logger;

    protected BaseRepository(AppDbContext context, ILogger logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<TEntity>();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 模板方法：子类覆盖提供关键字过滤逻辑
    /// </summary>
    protected virtual IQueryable<TEntity> ApplyKeywordFilter(
        IQueryable<TEntity> query,
        string keyword)
    {
        return query; // 默认不过滤
    }

    /// <summary>
    /// 模板方法：子类覆盖提供默认排序
    /// </summary>
    protected virtual IQueryable<TEntity> ApplyDefaultOrdering(
        IQueryable<TEntity> query)
    {
        // 默认按CreatedAt降序
        return query.OrderByDescending(e => EF.Property<DateTime>(e, "CreatedAt"));
    }

    /// <summary>
    /// 统一分页实现 - 子类不再需要重写
    /// </summary>
    public virtual async Task<PagedResult<TEntity>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? keyword = null)
    {
        var query = _dbSet.AsNoTracking()
            .Where(e => !EF.Property<bool>(e, "IsDeleted"));

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = ApplyKeywordFilter(query, keyword.Trim());
        }

        query = ApplyDefaultOrdering(query);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<TEntity>(items, totalCount, pageNumber, pageSize);
    }
}
```

**理由**:
- 消除6处重复的分页实现
- 子类只需提供过滤/排序逻辑（~10行 vs ~50行）
- 遵循OCP原则

### DD-003: 子类简化示例

**PatientRepository改进**:

```csharp
// Before: ~100 lines
public class PatientRepository : BaseRepository<Patient>, IPatientRepository
{
    public PatientRepository(AppDbContext context, ILogger<PatientRepository> logger)
        : base(context, logger) { }

    // 50行重复的GetPagedAsync代码...
    public override async Task<PagedResult<Patient>> GetPagedAsync(...)
    {
        // 完整的分页实现...
    }

    // 其他业务方法...
}

// After: ~40 lines
public class PatientRepository : BaseRepository<Patient>, IPatientRepository
{
    public PatientRepository(AppDbContext context, ILogger<PatientRepository> logger)
        : base(context, logger) { }

    protected override IQueryable<Patient> ApplyKeywordFilter(
        IQueryable<Patient> query, string keyword)
    {
        return query.Where(p =>
            p.Name.Contains(keyword) ||
            (p.PinYinCode != null && p.PinYinCode.Contains(keyword)));
    }

    protected override IQueryable<Patient> ApplyDefaultOrdering(
        IQueryable<Patient> query)
    {
        return query.OrderBy(p => p.Name);
    }

    // 其他业务方法...
}
```

### DD-004: BaseReadRepository统一

**决策**: BaseReadRepository同步采用必须Logger参数

```csharp
public abstract class BaseReadRepository<TEntity> : IReadRepository<TEntity>
    where TEntity : class, IEntity
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<TEntity> DbSet;
    protected readonly ILogger _logger;

    protected BaseReadRepository(AppDbContext context, ILogger logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        DbSet = context.Set<TEntity>();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // 5个标准只读方法...
}
```

### DD-005: 实体别名策略（修订）

**决策**: 默认移除using别名，但对命名空间冲突场景保留别名

**实施过程发现**：
在实施过程中发现，当实体类名与模块命名空间同名时，会产生编译冲突：
- `Formula`实体 vs `LYBT.Module.Formula`命名空间
- `Consultation`实体 vs `LYBT.Module.Consultation`命名空间
- `MedicalCase`实体 vs `LYBT.Module.MedicalCase`命名空间

**最终决策**:

```csharp
// 无冲突场景 - 直接使用实体名
using LYBT.Entities.Patients;
internal class PatientRepository : BaseRepository<Patient>

// 存在冲突场景 - 保留实体别名
using FormulaEntity = LYBT.Entities.Formulas.Formula;
/// <remarks>
/// 使用FormulaEntity别名避免与LYBT.Module.Formula命名空间冲突
/// </remarks>
internal class FormulaRepository : BaseRepository<FormulaEntity>
```

**受影响的Repository**:
| Repository | 实体名 | 模块命名空间 | 是否需要别名 |
|------------|--------|--------------|--------------|
| PatientRepository | Patient | LYBT.Module.Patients | 否 |
| UserRepository | User | LYBT.Module.Users | 否 |
| HerbRepository | Herb | LYBT.Module.Herbs | 否 |
| FormulaRepository | Formula | LYBT.Module.Formula | **是** |
| ConsultationRepository | Consultation | LYBT.Module.Consultation | **是** |
| MedicalCaseRepository | MedicalCase | LYBT.Module.MedicalCase | **是** |
| PrescriptionRepository | Prescription | LYBT.Module.Prescriptions | 否 |

**理由**:
- 保持代码可编译性
- 在remarks中说明别名原因，保持代码可读性
- 无冲突场景继续使用简短名称

## Migration Plan

### Step 1: 创建新接口位置
1. 在Infrastructure层创建Interfaces目录
2. 复制IRepository.cs和IReadRepository.cs
3. 更新命名空间

### Step 2: 更新BaseRepository/BaseReadRepository
1. 更新using引用
2. 添加模板方法
3. 重构GetPagedAsync

### Step 3: 更新所有子类Repository
1. 更新using引用
2. 移除GetPagedAsync重写
3. 添加ApplyKeywordFilter/ApplyDefaultOrdering覆盖
4. 移除实体别名
5. 统一构造函数

### Step 4: 删除旧接口
1. 删除Shared层的IRepository.cs
2. 删除Shared层的IReadRepository.cs

### Step 5: 更新测试和DI
1. 更新所有单元测试Mock
2. 验证DI注册
3. 运行全部测试

## Risks / Trade-offs

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| 大量文件修改 | 100% | 中 | 分步提交，每步验证 |
| 测试失败 | 高 | 低 | 同步更新测试 |
| 运行时异常 | 低 | 中 | 集成测试全覆盖 |

## Open Questions

无（已全部决策）

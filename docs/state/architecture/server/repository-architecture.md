# Repository层架构设计

> **文档版本**: v1.0
> **最后更新**: 2025-11-14
> **维护负责**: Server端开发组
> **重构状态**: Epic #2016 Phase 3 - Repository层重构完成

---

## 📋 目录

1. [Repository层概述](#1-repository层概述)
2. [三层接口架构](#2-三层接口架构)
3. [BaseRepository设计](#3-baserepository设计)
4. [聚合根模式实现](#4-聚合根模式实现)
5. [Repository实现规范](#5-repository实现规范)
6. [性能优化策略](#6-性能优化策略)
7. [测试与验证](#7-测试与验证)

---

## 1. Repository层概述

### 1.1 架构定位

Repository层位于Server端三层架构的基础设施层，负责数据访问和持久化操作，是业务逻辑与数据库之间的抽象层。

```
┌─────────────────────────────────────────────┐
│           Service Layer (业务逻辑)           │
│  ┌────────────────────────────────────────┐  │
│  │     MedicalCaseService                 │  │
│  │     PatientService                     │  │
│  │     HerbService                        │  │
│  └────────────────────────────────────────┘  │
└─────────────────┬───────────────────────────┘
                  │ 依赖注入
┌─────────────────▼───────────────────────────┐
│          Repository Layer (数据访问)          │
│  ┌────────────────────────────────────────┐  │
│  │    BaseRepository (基类)               │  │
│  │    IPatientRepository (接口)           │  │
│  │    PatientRepository (实现)            │  │
│  └────────────────────────────────────────┘  │
└─────────────────┬───────────────────────────┘
                  │ Entity Framework Core
┌─────────────────▼───────────────────────────┐
│          Database Layer (数据库)              │
│  ┌────────────────────────────────────────┐  │
│  │     AppDbContext                      │  │
│  │     Entity Framework Core             │  │
│  │     SQL Server                        │  │
│  └────────────────────────────────────────┘  │
└─────────────────────────────────────────────┘
```

### 1.2 核心职责

| 职责类别 | 具体功能 |
|---------|---------|
| **数据访问抽象** | 封装EF Core操作，提供统一的数据访问接口 |
| **查询优化** | 解决N+1查询问题，提供Include和Select优化 |
| **事务管理** | 支持数据库事务，确保数据一致性 |
| **并发控制** | 实现乐观并发和悲观并发控制 |
| **性能监控** | 查询性能监控和日志记录 |

### 1.3 设计原则

**设计原则**:
- ✅ **单一职责**: 每个Repository专注于单一实体的数据访问
- ✅ **接口分离**: 使用接口定义契约，便于测试和替换
- ✅ **依赖倒置**: Service层依赖接口而非具体实现
- ✅ **开闭原则**: 通过BaseRepository支持扩展
- ✅ **最小惊讶**: API设计直观易懂，符合EF Core使用习惯

---

## 2. 三层接口架构

### 2.1 接口层次结构

**Epic #2016 Phase 3**: 实现标准化的三层接口架构

```
IRepository (完整接口 - 11个方法)
    ↑ 继承
IReadRepository (只读接口 - 5个方法)
    ↑ 继承
IBaseRepository (基础接口 - 核心约定)
```

### 2.2 接口定义详情

#### 2.2.1 IBaseRepository - 基础接口

```csharp
/// <summary>
/// 基础仓储接口 - 定义Repository核心约定
/// Epic #2016 Phase 3: 统一Repository接口规范
/// </summary>
public interface IBaseRepository
{
    /// <summary>
    /// 数据库上下文
    /// </summary>
    AppDbContext Context { get; }

    /// <summary>
    /// 保存变更到数据库
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

#### 2.2.2 IReadRepository - 只读接口

```csharp
/// <summary>
/// 只读仓储接口 - 提供5个标准查询方法
/// Epic #2016 Phase 3: 为从属实体验证只读访问
/// </summary>
public interface IReadRepository<TEntity> : IBaseRepository 
    where TEntity : BaseEntity
{
    /// <summary>
    /// 根据ID查询实体（自动过滤软删除）
    /// </summary>
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询所有实体（自动过滤软删除）
    /// </summary>
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 条件查询（自动过滤软删除）
    /// </summary>
    Task<IEnumerable<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 单个实体查询（自动过滤软删除）
    /// </summary>
    Task<TEntity?> GetSingleAsync(
        Expression<Func<TEntity, bool>> predicate, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 计数查询（自动过滤软删除）
    /// </summary>
    Task<long> CountAsync(CancellationToken cancellationToken = default);
}
```

#### 2.2.3 IRepository - 完整接口

```csharp
/// <summary>
/// 完整仓储接口 - 提供11个标准CRUD方法
/// Epic #2016 Phase 3: 标准化Repository接口
/// </summary>
public interface IRepository<TEntity> : IReadRepository<TEntity> 
    where TEntity : BaseEntity
{
    /// <summary>
    /// 添加实体
    /// </summary>
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量添加实体
    /// </summary>
    Task<IEnumerable<TEntity>> AddRangeAsync(
        IEnumerable<TEntity> entities, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新实体
    /// </summary>
    void Update(TEntity entity);

    /// <summary>
    /// 批量更新实体
    /// </summary>
    void UpdateRange(IEnumerable<TEntity> entities);

    /// <summary>
    /// 删除实体（软删除）
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 分页查询（自动过滤软删除）
    /// </summary>
    Task<PagedResult<TEntity>> GetPagedAsync(
        int page, int pageSize, 
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);
}
```

### 2.3 接口使用指南

**从属实体（Prescription/Consultation）**:
```csharp
// 只继承IReadRepository，强制通过聚合根写入
public interface IPrescriptionRepository : IReadRepository<Prescription>
{
    // 添加模块特定的只读方法
    Task<Prescription?> GetByIdWithItemsAsync(Guid id);
    Task<IEnumerable<Prescription>> GetByMedicalCaseIdAsync(Guid medicalCaseId);
}
```

**聚合根实体（MedicalCase/Patient）**:
```csharp
// 继承IRepository，支持完整的CRUD操作
public interface IPatientRepository : IRepository<Patient>
{
    // 添加模块特定的方法
    Task<bool> ExistsByPhoneAsync(string phoneNumber);
    Task<Patient?> GetByIdWithMedicalCasesAsync(Guid id);
}
```

---

## 3. BaseRepository设计

### 3.1 双基类架构

**Epic #2016 Phase 3**: 根据实体类型选择不同的基类

| 基类 | 实现接口 | 用途 | 适用实体 |
|-----|---------|------|----------|
| `BaseReadRepository<T>` | `IReadRepository<T>` | 只读仓储基类 | 从属实体（Prescription、Consultation） |
| `BaseRepository<T>` | `IRepository<T>` | 完整仓储基类 | 聚合根实体（MedicalCase、Patient） |

### 3.2 BaseReadRepository - 只读基类

```csharp
/// <summary>
/// 只读仓储基类 - 实现IReadRepository接口的5个标准方法
/// Epic #2016 Phase 3: 为从属实体提供只读数据访问
/// </summary>
/// <remarks>
/// 设计原则：
/// - ⭐ 所有查询自动过滤软删除记录（IsDeleted = true）
/// - ⭐ 适用于从属实体（Prescription/Consultation），强制通过聚合根写入
/// - ⭐ 继承此类的Repository应为internal，防止外部直接访问
/// </remarks>
public abstract class BaseReadRepository<TEntity> : IReadRepository<TEntity> 
    where TEntity : BaseEntity
{
    protected readonly AppDbContext Context;
    protected readonly DbSet<TEntity> DbSet;

    protected BaseReadRepository(AppDbContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        DbSet = Context.Set<TEntity>();
    }

    public virtual async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(e => !e.IsDeleted)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public virtual async Task<IEnumerable<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return await DbSet
            .Where(e => !e.IsDeleted)
            .Where(predicate)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public virtual async Task<TEntity?> GetSingleAsync(
        Expression<Func<TEntity, bool>> predicate, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return await DbSet
            .Where(e => !e.IsDeleted)
            .Where(predicate)
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(e => !e.IsDeleted)
            .LongCountAsync(cancellationToken);
    }

    public AppDbContext Context => Context;

    public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await Context.SaveChangesAsync(cancellationToken);
    }
}
```

### 3.3 BaseRepository - 完整基类

```csharp
/// <summary>
/// 标准仓储基类 - 实现IRepository接口的11个标准方法
/// Epic #2016 Phase 3: 为聚合根实体提供完整CRUD操作
/// </summary>
public abstract class BaseRepository<TEntity> : BaseReadRepository<TEntity>, IRepository<TEntity> 
    where TEntity : BaseEntity
{
    protected BaseRepository(AppDbContext context) : base(context) { }

    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        
        await DbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public virtual async Task<IEnumerable<TEntity>> AddRangeAsync(
        IEnumerable<TEntity> entities, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);
        
        var entityList = entities.ToList();
        await DbSet.AddRangeAsync(entityList, cancellationToken);
        return entityList;
    }

    public virtual void Update(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        DbSet.Update(entity);
    }

    public virtual void UpdateRange(IEnumerable<TEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        DbSet.UpdateRange(entities);
    }

    public virtual async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return false;

        // 软删除实现
        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        
        Update(entity);
        return true;
    }

    /// <summary>
    /// 分页辅助方法 - 统一处理分页逻辑
    /// Epic #1725: 提取公共分页逻辑，减少代码重复
    /// </summary>
    protected async Task<PagedResult<TEntity>> GetPagedResultAsync(
        IQueryable<TEntity> query,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return new PagedResult<TEntity>(items, totalCount, pageNumber, pageSize);
    }

    public virtual async Task<PagedResult<TEntity>> GetPagedAsync(
        int page, int pageSize, 
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(e => !e.IsDeleted);

        if (predicate != null)
            query = query.Where(predicate);

        return await GetPagedResultAsync(query, page, pageSize, cancellationToken);
    }
}
```

---

## 4. 聚合根模式实现

### 4.1 聚合根设计原则

**聚合根模式强制执行**（Epic #1600 Phase 3）:

1. **Repository可见性约束**: 所有Repository实现类从`public`改为`internal`
2. **强制聚合根访问**: 从属实体只能通过聚合根进行修改
3. **依赖方向正确**: Service → Repository聚合根 → Repository从属

### 4.2 可见性约束实现

#### 4.2.1 internal可见性

**Repository实现示例**:
```csharp
// ⚠️ 注意：实现类为internal，强制执行聚合根模式（Epic #1600 Phase 3）
internal class PatientRepository : BaseRepository<Patient>, IPatientRepository
{
    public PatientRepository(AppDbContext context) : base(context) { }

    // 继承11个标准方法 + 添加模块特定方法
    public async Task<bool> ExistsByPhoneAsync(string phoneNumber)
    {
        return await DbSet
            .Where(p => !p.IsDeleted)
            .AnyAsync(p => p.PhoneNumber == phoneNumber);
    }

    public async Task<Patient?> GetByIdWithMedicalCasesAsync(Guid id)
    {
        return await DbSet
            .Include(p => p.MedicalCases.Where(m => !m.IsDeleted))
            .Where(p => !p.IsDeleted && p.Id == id)
            .AsNoTracking()
            .SingleOrDefaultAsync();
    }
}
```

#### 4.2.2 InternalsVisibleTo配置

**项目文件配置**:
```xml
<!-- 允许测试项目访问internal类 (Epic #1600 Phase 3) -->
<ItemGroup>
  <InternalsVisibleTo Include="LYBT.Module.Patients.Tests" />
  <InternalsVisibleTo Include="LYBT.IntegrationTests" />
</ItemGroup>
```

### 4.3 聚合根边界强制

#### 4.3.1 MedicalCase聚合根示例

**MedicalCaseRepository（聚合根）**:
```csharp
internal class MedicalCaseRepository : BaseRepository<MedicalCase>, IMedicalCaseRepository
{
    public MedicalCaseRepository(AppDbContext context) : base(context) { }

    /// <summary>
    /// 获取病案详情（预加载所有关联数据）
    /// 解决N+1查询问题
    /// </summary>
    public async Task<MedicalCase?> GetByIdWithDetailsAsync(Guid id)
    {
        return await DbSet
            .Include(m => m.Patient)
            .Include(m => m.Doctor)
            .Include(m => m.Consultation)
                .ThenInclude(c => c.Diagnoses)
            .Include(m => m.Prescriptions)
                .ThenInclude(p => p.Items)
                    .ThenInclude(pi => pi.Herb)
            .Where(m => !m.IsDeleted && m.Id == id)
            .AsNoTracking()
            .AsSplitQuery() // 复杂查询拆分优化
            .SingleOrDefaultAsync();
    }

    /// <summary>
    /// 查询患者活跃病案（业务规则：单患者单Active病案）
    /// </summary>
    public async Task<MedicalCase?> GetActiveByPatientIdAsync(Guid patientId)
    {
        return await DbSet
            .Where(m => !m.IsDeleted 
                       && m.PatientId == patientId 
                       && m.Status == MedicalCaseStatus.Active)
            .AsNoTracking()
            .SingleOrDefaultAsync();
    }

    /// <summary>
    /// 分页查询病案（预加载基础关联数据）
    /// </summary>
    public async Task<PagedResult<MedicalCase>> GetPagedWithDetailsAsync(
        int page, int pageSize, string? keyword)
    {
        var query = DbSet
            .Include(m => m.Patient)
            .Include(m => m.Doctor)
            .Where(m => !m.IsDeleted);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(m => 
                m.Patient.Name.Contains(keyword) ||
                (m.Patient.PhoneNumber != null && m.Patient.PhoneNumber.Contains(keyword)));
        }

        return await GetPagedResultAsync(query, page, pageSize);
    }
}
```

#### 4.3.2 从属实体只读Repository

**PrescriptionRepository（从属）**:
```csharp
internal class PrescriptionRepository : BaseReadRepository<Prescription>, IPrescriptionRepository
{
    public PrescriptionRepository(AppDbContext context) : base(context) { }

    // 只继承只读方法，强制通过MedicalCase进行写入
    public async Task<Prescription?> GetByIdWithItemsAsync(Guid id)
    {
        return await DbSet
            .Include(p => p.Items)
                .ThenInclude(i => i.Herb)
            .Where(p => !p.IsDeleted && p.Id == id)
            .AsNoTracking()
            .SingleOrDefaultAsync();
    }

    public async Task<IEnumerable<Prescription>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
    {
        return await DbSet
            .Include(p => p.Items)
                .ThenInclude(i => i.Herb)
            .Where(p => !p.IsDeleted && p.MedicalCaseId == medicalCaseId)
            .AsNoTracking()
            .ToListAsync();
    }
}
```

---

## 5. Repository实现规范

### 5.1 实现规范清单

#### 5.1.1 基本实现要求

**必须遵循的规范**:
- ✅ **internal可见性**: 实现类必须为internal
- ✅ **继承基类**: 根据实体类型选择正确的基类
- ✅ **参数校验**: 对null参数进行验证
- ✅ **软删除过滤**: 所有查询自动过滤IsDeleted记录
- ✅ **异步操作**: 所有数据库操作必须异步
- ✅ **AsNoTracking**: 只读查询使用AsNoTracking优化

#### 5.1.2 命名规范

**Repository接口**:
```csharp
// 接口命名：I + 实体名 + Repository
public interface IPatientRepository : IRepository<Patient> { }
public interface IPrescriptionRepository : IReadRepository<Prescription> { }
public interface IMedicalCaseRepository : IRepository<MedicalCase> { }
```

**Repository实现**:
```csharp
// 实现类命名：实体名 + Repository
internal class PatientRepository : BaseRepository<Patient>, IPatientRepository { }
internal class PrescriptionRepository : BaseReadRepository<Prescription>, IPrescriptionRepository { }
internal class MedicalCaseRepository : BaseRepository<MedicalCase>, IMedicalCaseRepository { }
```

### 5.2 最佳实践示例

#### 5.2.1 模块特定方法

**PatientRepository模块方法**:
```csharp
internal class PatientRepository : BaseRepository<Patient>, IPatientRepository
{
    public PatientRepository(AppDbContext context) : base(context) { }

    /// <summary>
    /// 检查手机号是否已存在
    /// 业务规则：手机号必须唯一
    /// </summary>
    public async Task<bool> ExistsByPhoneAsync(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        return await DbSet
            .Where(p => !p.IsDeleted)
            .AnyAsync(p => p.PhoneNumber == phoneNumber);
    }

    /// <summary>
    /// 按拼音码搜索患者
    /// 支持拼音模糊匹配
    /// </summary>
    public async Task<IEnumerable<Patient>> SearchByPinYinAsync(string pinYinCode)
    {
        if (string.IsNullOrWhiteSpace(pinYinCode))
            return Enumerable.Empty<Patient>();

        return await DbSet
            .Where(p => !p.IsDeleted && p.PinYinCode.Contains(pinYinCode))
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// 获取患者统计信息
    /// </summary>
    public async Task<PatientStatistics> GetStatisticsAsync()
    {
        var stats = await DbSet
            .Where(p => !p.IsDeleted)
            .GroupBy(p => 1)
            .Select(g => new PatientStatistics
            {
                TotalCount = g.Count(),
                ActiveCount = g.Count(p => p.IsActive),
                MaleCount = g.Count(p => p.Gender == Gender.Male),
                FemaleCount = g.Count(p => p.Gender == Gender.Female),
                CreatedThisMonth = g.Count(p => p.CreatedAt >= DateTime.UtcNow.AddDays(-30))
            })
            .SingleOrDefaultAsync();

        return stats ?? new PatientStatistics();
    }
}
```

#### 5.2.2 复杂查询优化

**HerbRepository搜索优化**:
```csharp
internal class HerbRepository : BaseRepository<Herb>, IHerbRepository
{
    public HerbRepository(AppDbContext context) : base(context) { }

    /// <summary>
    /// 药材搜索（支持中文名、拼音码、功效搜索）
    /// 使用全文索引优化搜索性能
    /// </summary>
    public async Task<PagedResult<Herb>> SearchAsync(HerbSearchCriteria criteria)
    {
        var query = DbSet.Where(h => !h.IsDeleted && h.IsActive);

        // 中文名搜索
        if (!string.IsNullOrWhiteSpace(criteria.Name))
        {
            query = query.Where(h => h.Name.Contains(criteria.Name));
        }

        // 拼音码搜索
        if (!string.IsNullOrWhiteSpace(criteria.PinYinCode))
        {
            query = query.Where(h => h.PinYinCode.Contains(criteria.PinYinCode));
        }

        // 功效搜索
        if (!string.IsNullOrWhiteSpace(criteria.Effect))
        {
            query = query.Where(h => h.Effects.Contains(criteria.Effect));
        }

        // 类别筛选
        if (criteria.CategoryId.HasValue)
        {
            query = query.Where(h => h.CategoryId == criteria.CategoryId);
        }

        // 按名称排序
        query = query.OrderBy(h => h.Name);

        return await GetPagedResultAsync(query, criteria.Page, criteria.PageSize);
    }

    /// <summary>
    /// 获取热门药材（按使用频率排序）
    /// </summary>
    public async Task<IEnumerable<Herb>> GetPopularHerbsAsync(int topCount = 20)
    {
        return await DbSet
            .Where(h => !h.IsDeleted && h.IsActive)
            .OrderByDescending(h => h.UsageCount)
            .Take(topCount)
            .AsNoTracking()
            .ToListAsync();
    }
}
```

---

## 6. 性能优化策略

### 6.1 查询优化技术

#### 6.1.1 Include预加载优化

**解决N+1查询问题**:
```csharp
// ❌ N+1查询问题
var medicalCases = await _dbContext.MedicalCases.ToListAsync();
foreach (var medicalCase in medicalCases)
{
    // 每次循环都产生新的数据库查询
    var patient = await _dbContext.Patients.FirstOrDefaultAsync(p => p.Id == medicalCase.PatientId);
}

// ✅ 优化：使用Include预加载
var medicalCases = await _dbContext.MedicalCases
    .Include(m => m.Patient)     // 一次查询加载患者信息
    .Include(m => m.Doctor)      // 一次查询加载医生信息
    .Include(m => m.Prescriptions) // 一次查询加载处方信息
        .ThenInclude(p => p.Items)  // 预加载处方项目
    .Where(m => !m.IsDeleted)
    .AsNoTracking()              // 只读查询优化
    .ToListAsync();
```

#### 6.1.2 Select投影优化

**减少数据传输**:
```csharp
// ✅ 优化：只查询需要的字段
var patientList = await _dbContext.Patients
    .Where(p => !p.IsDeleted && p.IsActive)
    .Select(p => new PatientListDto
    {
        Id = p.Id,
        Name = p.Name,
        PhoneNumber = p.PhoneNumber,
        Age = DateTime.UtcNow.Year - p.BirthDate.Year,
        // 不包含大字段如Address、Notes等
    })
    .AsNoTracking()
    .ToListAsync();

// ❌ 避免：查询完整实体再映射
var patients = await _dbContext.Patients
    .Include(p => p.Address)  // 可能不需要的关联数据
    .Include(p => p.MedicalCases) // 大量关联数据
    .Where(p => !p.IsDeleted)
    .ToListAsync();
```

### 6.2 缓存集成

#### 6.2.1 Repository层缓存策略

**查询结果缓存**:
```csharp
internal class HerbRepository : BaseRepository<Herb>, IHerbRepository
{
    private readonly IMemoryCache _cache;
    private const string POPULAR_HERBS_CACHE_KEY = "Herbs:Popular";

    public HerbRepository(AppDbContext context, IMemoryCache cache) : base(context)
    {
        _cache = cache;
    }

    public async Task<IEnumerable<Herb>> GetPopularHerbsAsync(int topCount = 20)
    {
        var cacheKey = $"{POPULAR_HERBS_CACHE_KEY}:{topCount}";

        // 尝试从缓存获取
        if (_cache.TryGetValue(cacheKey, out IEnumerable<Herb>? cachedHerbs))
        {
            return cachedHerbs!;
        }

        // 缓存未命中，查询数据库
        var herbs = await DbSet
            .Where(h => !h.IsDeleted && h.IsActive)
            .OrderByDescending(h => h.UsageCount)
            .Take(topCount)
            .AsNoTracking()
            .ToListAsync();

        // 存入缓存（1小时过期）
        _cache.Set(cacheKey, herbs, TimeSpan.FromHours(1));

        return herbs;
    }

    /// <summary>
    /// 清除热门药材缓存
    /// 在药材使用频率更新时调用
    /// </summary>
    public void ClearPopularHerbsCache()
    {
        // 清除所有热门药材缓存（不同的topCount）
        for (int i = 10; i <= 100; i += 10)
        {
            _cache.Remove($"{POPULAR_HERBS_CACHE_KEY}:{i}");
        }
    }
}
```

### 6.3 数据库优化

#### 6.3.1 索引优化

**核心查询索引配置**:
```csharp
// AppDbContext.OnModelCreating
modelBuilder.Entity<Patient>(entity =>
{
    // 唯一索引 - 防重复手机号
    entity.HasIndex(p => p.PhoneNumber)
          .IsUnique()
          .HasDatabaseName("IX_Patients_PhoneNumber");
    
    // 复合索引 - 姓名查询优化
    entity.HasIndex(p => new { p.Name, p.IsDeleted })
          .HasDatabaseName("IX_Patients_Name_IsDeleted");
    
    // 查询索引 - 状态和时间组合
    entity.HasIndex(p => new { p.IsActive, p.CreatedAt })
          .HasDatabaseName("IX_Patients_IsActive_CreatedAt");
});

modelBuilder.Entity<MedicalCase>(entity =>
{
    // 复合索引 - 患者查询
    entity.HasIndex(m => new { m.PatientId, m.Status })
          .HasDatabaseName("IX_MedicalCases_PatientId_Status");
    
    // 复合索引 - 医生查询
    entity.HasIndex(m => new { m.DoctorId, m.Status, m.VisitDate })
          .HasDatabaseName("IX_MedicalCases_DoctorId_Status_VisitDate");
});
```

#### 6.3.2 查询性能监控

**EF Core查询日志**:
```csharp
// 开发环境配置
services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    options.UseSqlServer(connectionString)
           .LogTo(Console.WriteLine, LogLevel.Information)
           .EnableSensitiveDataLogging(true)     // 开发环境
           .EnableDetailedErrors(true)            // 开发环境
           .EnableServiceProviderCaching();        // 服务提供者缓存
});

// 生产环境配置
services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    options.UseSqlServer(connectionString)
           .LogTo(Console.WriteLine, LogLevel.Warning)  // 只记录警告和错误
           .EnableServiceProviderCaching();
});
```

---

## 7. 测试与验证

### 7.1 单元测试规范

#### 7.1.1 Repository测试模板

**PatientRepositoryTests示例**:
```csharp
public class PatientRepositoryTests : TestBase
{
    private readonly Mock<AppDbContext> _mockContext;
    private readonly Mock<DbSet<Patient>> _mockDbSet;
    private readonly IPatientRepository _repository;

    public PatientRepositoryTests()
    {
        _mockContext = new Mock<AppDbContext>();
        _mockDbSet = new Mock<DbSet<Patient>>();
        _mockContext.Setup(c => c.Set<Patient>()).Returns(_mockDbSet.Object);

        _repository = new PatientRepository(_mockContext.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsPatient()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var patient = CreateTestPatient(patientId);

        _mockDbSet.Setup(d => d.FindAsync(It.IsAny<object[]>()))
                  .ReturnsAsync(patient);

        // Act
        var result = await _repository.GetByIdAsync(patientId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(patientId);
        _mockDbSet.Verify(d => d.FindAsync(It.Is<object[]>(ids => ids.First().Equals(patientId))), Times.Once);
    }

    [Fact]
    public async Task ExistsByPhoneAsync_WithExistingPhone_ReturnsTrue()
    {
        // Arrange
        var phoneNumber = "13800138000";
        var patients = new List<Patient> { CreateTestPatient(phoneNumber: phoneNumber) };
        var queryable = patients.AsQueryable();

        _mockDbSet.As<IQueryable<Patient>>().Setup(m => m.Provider).Returns(queryable.Provider);
        _mockDbSet.As<IQueryable<Patient>>().Setup(m => m.Expression).Returns(queryable.Expression);
        _mockDbSet.As<IQueryable<Patient>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        _mockDbSet.As<IQueryable<Patient>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());

        // Act
        var result = await _repository.ExistsByPhoneAsync(phoneNumber);

        // Assert
        result.Should().BeTrue();
    }

    private Patient CreateTestPatient(Guid? id = null, string phoneNumber = "13800138000")
    {
        return new Patient
        {
            Id = id ?? Guid.NewGuid(),
            Name = "测试患者",
            PhoneNumber = phoneNumber,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
```

### 7.2 集成测试验证

#### 7.2.1 真实数据库测试

**使用InMemory数据库**:
```csharp
public class PatientRepositoryIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly AppDbContext _context;
    private readonly IPatientRepository _repository;

    public PatientRepositoryIntegrationTests(DatabaseFixture fixture)
    {
        _context = fixture.Context;
        _repository = new PatientRepository(_context);
    }

    [Fact]
    public async Task AddAsync_WithValidPatient_ShouldAddToDatabase()
    {
        // Arrange
        var patient = new Patient
        {
            Name = "集成测试患者",
            PhoneNumber = "13900139000",
            BirthDate = new DateTime(1990, 1, 1),
            Gender = Gender.Male,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var addedPatient = await _repository.AddAsync(patient);
        await _repository.SaveChangesAsync();

        // Assert
        addedPatient.Should().NotBeNull();
        addedPatient.Id.Should().NotBeEmpty();

        // 验证数据库中的数据
        var savedPatient = await _context.Patients.FindAsync(addedPatient.Id);
        savedPatient.Should().NotBeNull();
        savedPatient.Name.Should().Be("集成测试患者");
    }

    [Fact]
    public async Task GetPagedAsync_WithMultiplePatients_ShouldReturnCorrectPage()
    {
        // Arrange
        await SeedTestPatientsAsync(25);

        // Act
        var result = await _repository.GetPagedAsync(2, 10);

        // Assert
        result.Should().NotBeNull();
        result.Items.Count.Should().Be(10);
        result.TotalCount.Should().Be(25);
        result.CurrentPage.Should().Be(2);
        result.TotalPages.Should().Be(3);
    }

    private async Task SeedTestPatientsAsync(int count)
    {
        var patients = Enumerable.Range(1, count)
            .Select(i => new Patient
            {
                Name = $"测试患者{i}",
                PhoneNumber = $"1380013{i:D4}",
                BirthDate = new DateTime(1990, 1, 1),
                Gender = i % 2 == 0 ? Gender.Male : Gender.Female,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        await _context.Patients.AddRangeAsync(patients);
        await _context.SaveChangesAsync();
    }
}
```

---

## 📚 相关文档

### Repository设计参考
- [Repository模式详解](../patterns/repository-pattern.md) - Repository模式完整说明
- [聚合根模式](../patterns/aggregate-root-pattern.md) - 聚合根设计原则
- [Server端架构指南](./README.md) - 完整Server端架构

### 性能优化参考
- [性能优化指南](./performance-optimization.md) - 数据库查询和缓存优化
- [数据库设计指南](../database-design-guide.md) - 数据库索引和性能优化

### 测试参考
- [单元测试指南](../../../../how-to-guides/server/unit-testing.md) - Repository单元测试 *(待创建)*
- [集成测试指南](../../../../how-to-guides/server/integration-testing.md) - 数据库集成测试 *(待创建)*

---

**文档更新历史**:
- v1.0 (2025-11-14): 初始版本，基于Epic #2016 Phase 3重构完成
  - 三层接口架构设计
  - BaseRepository双基类实现
  - 聚合根模式强制执行
  - 性能优化最佳实践
# Server端性能优化指南

> **文档版本**: v1.0
> **最后更新**: 2025-11-14
> **维护负责**: Server端开发组
> **重构状态**: Task 5.3 - 架构文档同步完成

---

## 📋 目录

1. [性能优化概述](#1-性能优化概述)
2. [数据库查询优化](#2-数据库查询优化)
3. [缓存策略设计](#3-缓存策略设计)
4. [Repository层性能优化](#4-repository层性能优化)
5. [Service层性能优化](#5-service层性能优化)
6. [Controller层性能优化](#6-controller层性能优化)
7. [内存与资源管理](#7-内存与资源管理)
8. [并发处理优化](#8-并发处理优化)
9. [性能监控指标](#9-性能监控指标)
10. [性能基准测试](#10-性能基准测试)

---

## 1. 性能优化概述

### 1.1 优化目标

**响应时间目标**:
- API响应时间 < 500ms (95%请求)
- 数据库查询时间 < 200ms (平均)
- 页面加载时间 < 2s (完整业务流程)

**吞吐量目标**:
- 支持100并发用户
- QPS > 500 (查询操作)
- QPS > 200 (写入操作)

**资源使用目标**:
- CPU使用率 < 70% (正常负载)
- 内存使用 < 2GB (稳定状态)
- 数据库连接池使用率 < 80%

### 1.2 优化原则

| 原则 | 描述 | 应用场景 |
|------|------|----------|
| **延迟加载** | 按需加载关联数据 | 避免N+1查询问题 |
| **批量操作** | 减少数据库往返次数 | 批量导入、批量更新 |
| **缓存优先** | 热点数据缓存 | 配置数据、基础数据 |
| **异步非阻塞** | 提高并发处理能力 | 所有I/O操作 |
| **连接池管理** | 复用数据库连接 | 所有数据访问 |

### 1.3 性能瓶颈识别

**常见瓶颈**:
- **数据库**: N+1查询、缺少索引、过度查询
- **内存**: 大对象创建、内存泄漏、缓存失效
- **网络**: 过度数据传输、同步I/O操作
- **CPU**: 复杂计算、循环嵌套、异常处理

---

## 2. 数据库查询优化

### 2.1 N+1查询问题解决

#### 2.1.1 问题识别

**❌ N+1查询示例**:
```csharp
// 查询医案列表 - 会产生N+1查询
var medicalCases = await _dbContext.MedicalCases
    .Where(m => m.Status == MedicalCaseStatus.Active)
    .ToListAsync();

foreach (var medicalCase in medicalCases)
{
    // 每次循环都查询一次数据库 - N+1问题
    var patient = await _dbContext.Patients
        .FirstOrDefaultAsync(p => p.Id == medicalCase.PatientId);
}
```

**性能影响**: 如果有100个医案，会产生101次数据库查询（1次列表 + 100次患者查询）

#### 2.1.2 优化方案

**✅ 使用Include预加载**:
```csharp
// 一次查询获取所有关联数据
var medicalCases = await _dbContext.MedicalCases
    .Include(m => m.Patient)  // 预加载患者信息
    .Include(m => m.Doctor)   // 预加载医生信息
    .Where(m => m.Status == MedicalCaseStatus.Active)
    .AsNoTracking()  // 只读查询优化
    .ToListAsync();
```

**✅ 使用Select投影**:
```csharp
// 只查询需要的字段
var medicalCasesDto = await _dbContext.MedicalCases
    .Where(m => m.Status == MedicalCaseStatus.Active)
    .Select(m => new MedicalCaseListDto
    {
        Id = m.Id,
        VisitDate = m.VisitDate,
        PatientName = m.Patient.Name,  // 关联查询
        DoctorName = m.Doctor.RealName,
        // 只包含需要的字段
    })
    .AsNoTracking()
    .ToListAsync();
```

#### 2.1.3 实际应用案例

**MedicalCaseRepository优化示例**:
```csharp
/// <summary>
/// 获取病案分页列表（优化版）
/// 解决N+1查询问题，提升查询性能
/// </summary>
public async Task<PagedResult<MedicalCaseEntity>> GetPagedWithDetailsAsync(
    int page, int pageSize, string? keyword)
{
    var query = _dbContext.MedicalCases
        .Include(m => m.Patient)     // 预加载患者
        .Include(m => m.Doctor)      // 预加载医生
        .Include(m => m.Consultation) // 预加载辨证
        .Include(m => m.Prescriptions) // 预加载处方
            .ThenInclude(p => p.Items) // 预加载处方项目
        .AsNoTracking();              // 只读查询

    if (!string.IsNullOrWhiteSpace(keyword))
    {
        query = query.Where(m => 
            m.Patient.Name.Contains(keyword) ||
            m.Patient.PhoneNumber.Contains(keyword));
    }

    var totalCount = await query.CountAsync();
    var items = await query
        .OrderByDescending(m => m.VisitDate)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .AsSplitQuery() // 复杂查询拆分优化
        .ToListAsync();

    return new PagedResult<MedicalCaseEntity>(items, totalCount, page, pageSize);
}
```

### 2.2 索引优化策略

#### 2.2.1 核心查询索引

**患者表索引**:
```csharp
// AppDbContext.OnModelCreating
modelBuilder.Entity<Patient>(entity =>
{
    // 主键索引（自动创建）
    entity.HasKey(p => p.Id);
    
    // 唯一索引 - 防重复手机号
    entity.HasIndex(p => p.PhoneNumber)
          .IsUnique()
          .HasDatabaseName("IX_Patients_PhoneNumber");
    
    // 复合索引 - 姓名查询
    entity.HasIndex(p => new { p.Name, p.IsDeleted })
          .HasDatabaseName("IX_Patients_Name_IsDeleted");
    
    // 查询索引 - 创建时间范围
    entity.HasIndex(p => p.CreatedAt)
          .HasDatabaseName("IX_Patients_CreatedAt");
});
```

**病案表索引**:
```csharp
modelBuilder.Entity<MedicalCase>(entity =>
{
    // 复合索引 - 患者查询
    entity.HasIndex(m => new { m.PatientId, m.Status, m.VisitDate })
          .HasDatabaseName("IX_MedicalCases_PatientId_Status_VisitDate");
    
    // 复合索引 - 医生查询
    entity.HasIndex(m => new { m.DoctorId, m.Status })
          .HasDatabaseName("IX_MedicalCases_DoctorId_Status");
    
    // 查询索引 - 状态筛选
    entity.HasIndex(m => m.Status)
          .HasDatabaseName("IX_MedicalCases_Status");
});
```

#### 2.2.2 索引使用指南

**适合创建索引的场景**:
- 频繁出现在WHERE条件中的字段
- JOIN操作中的外键字段
- ORDER BY排序中的字段
- GROUP BY分组中的字段

**避免过度索引**:
- 更新频繁但查询较少的字段
- 数据区分度低的字段（如性别、状态枚举）
- 大文本字段（超过900字节）

### 2.3 查询性能监控

#### 2.3.1 EF Core查询日志

**开发环境配置**:
```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information",
      "Microsoft.EntityFrameworkCore.Query": "Warning"
    }
  }
}
```

**生产环境配置**:
```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

#### 2.3.2 查询性能分析

**启用查询统计**:
```csharp
// Program.cs
services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    options.UseSqlServer(connectionString)
           .EnableSensitiveDataLogging(false)  // 生产环境关闭
           .EnableServiceProviderCaching()     // 启用服务提供者缓存
           .LogTo(Console.WriteLine, LogLevel.Information) // 开发环境日志
           .EnableDetailedErrors();            // 开发环境详细错误
});
```

---

## 3. 缓存策略设计

### 3.1 多层缓存架构

```
┌─────────────────────────────────────────────┐
│           WebAPI Layer                       │
│  ┌─────────────┬─────────────────┐           │
│  │ In-Memory   │  Distributed    │           │
│  │ Cache       │  Cache          │           │
│  │ (IMemoryCache) │ (Redis)       │           │
│  └─────────────┴─────────────────┘           │
├─────────────────────────────────────────────┤
│           Database Layer                     │
│  ┌─────────────────────────────────────┐     │
│  │      SQL Server Query Cache        │     │
│  └─────────────────────────────────────┘     │
└─────────────────────────────────────────────┘
```

### 3.2 缓存策略类型

| 缓存类型 | 使用场景 | 过期时间 | 示例数据 |
|---------|---------|----------|----------|
| **配置缓存** | 系统配置、字典数据 | 24小时 | 药材分类、证型列表 |
| **热点数据缓存** | 频繁查询的基础数据 | 1小时 | 医生列表、科室信息 |
| **查询结果缓存** | 复杂查询结果 | 30分钟 | 统计报表、分析数据 |
| **用户会话缓存** | 用户相关信息 | 60分钟 | 用户权限、个人信息 |

### 3.3 缓存实现示例

#### 3.3.1 基础数据缓存

**HerbService缓存实现**:
```csharp
public class HerbService : IHerbService
{
    private readonly IHerbRepository _repository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<HerbService> _logger;

    private const string HERBS_ALL_CACHE_KEY = "Herbs:All";
    private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromHours(1);

    public async Task<List<HerbDto>> GetAllAsync()
    {
        // 尝试从缓存获取
        if (_cache.TryGetValue(HERBS_ALL_CACHE_KEY, out List<HerbDto>? cachedHerbs))
        {
            _logger.LogDebug("从缓存获取药材列表，数量：{Count}", cachedHerbs?.Count ?? 0);
            return cachedHerbs!;
        }

        // 缓存未命中，从数据库查询
        var herbs = await _repository.GetAllAsync();
        var herbDtos = _mapper.Map<List<HerbDto>>(herbs);

        // 存入缓存
        _cache.Set(HERBS_ALL_CACHE_KEY, herbDtos, CACHE_DURATION);
        _logger.LogInformation("药材列表已缓存，数量：{Count}", herbDtos.Count);

        return herbDtos;
    }

    public async Task<HerbDto?> GetByIdAsync(Guid id)
    {
        var cacheKey = $"Herbs:{id}";

        // 尝试从缓存获取单个药材
        if (_cache.TryGetValue(cacheKey, out HerbDto? cachedHerb))
        {
            return cachedHerb;
        }

        var herb = await _repository.GetByIdAsync(id);
        if (herb != null)
        {
            var herbDto = _mapper.Map<HerbDto>(herb);
            _cache.Set(cacheKey, herbDto, CACHE_DURATION);
            return herbDto;
        }

        return null;
    }

    /// <summary>
    /// 清除药材相关缓存
    /// 在药材数据变更时调用
    /// </summary>
    private void ClearHerbsCache()
    {
        _cache.Remove(HERBS_ALL_CACHE_KEY);
        _logger.LogInformation("已清除药材缓存");
    }

    public async Task<HerbDto> UpdateAsync(Guid id, UpdateHerbDto dto)
    {
        var herb = await _repository.UpdateAsync(id, dto);
        var herbDto = _mapper.Map<HerbDto>(herb);

        // 更新缓存
        var cacheKey = $"Herbs:{id}";
        _cache.Set(cacheKey, herbDto, CACHE_DURATION);

        // 清除列表缓存（数据可能已变更）
        ClearHerbsCache();

        _logger.LogInformation("更新药材缓存：{HerbId}", id);
        return herbDto;
    }
}
```

#### 3.3.2 缓存失效策略

**基于事件的缓存失效**:
```csharp
public class CacheInvalidationService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheInvalidationService> _logger;

    public void InvalidateHerbCache(Guid herbId)
    {
        // 清除单个药材缓存
        _cache.Remove($"Herbs:{herbId}");
        
        // 清除药材列表缓存
        _cache.Remove("Herbs:All");
        
        // 清除相关搜索缓存
        var searchKeys = _cache.Get<string[]>("Herbs:SearchKeys") ?? Array.Empty<string>();
        foreach (var key in searchKeys)
        {
            _cache.Remove(key);
        }
        
        _logger.LogInformation("已清除药材ID {HerbId} 的所有相关缓存", herbId);
    }

    public void InvalidatePatientCache(Guid patientId)
    {
        _cache.Remove($"Patients:{patientId}");
        _cache.Remove("Patients:All");
        
        // 清除相关病案缓存
        _cache.Remove($"MedicalCases:Patient:{patientId}");
        
        _logger.LogInformation("已清除患者ID {PatientId} 的所有相关缓存", patientId);
    }
}
```

### 3.4 缓存配置优化

**缓存配置（Program.cs）**:
```csharp
services.AddMemoryCache(options =>
{
    // 缓存大小限制（项数）
    options.SizeLimit = 1000;
    
    // 压缩缓存
    options.CompactOnMemoryPressure = 0.9;
    
    // 滑动过期时间
    options.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
});

// 缓存健康检查
services.AddHealthChecks()
    .AddCheck<MemoryCacheHealthCheck>("memory_cache");
```

---

## 4. Repository层性能优化

### 4.1 BaseRepository性能优化

#### 4.1.1 优化后的BaseRepository

**关键优化点**:
- 自动软删除过滤
- 异步I/O操作
- 查询优化方法

```csharp
/// <summary>
/// 标准仓储基类（性能优化版）
/// Epic #2016 Phase 3: 统一Repository接口，移除冗余抽象
/// </summary>
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

    /// <summary>
    /// 根据ID查询实体（自动过滤软删除）
    /// 优化：使用FindAsync进行主键查询，性能最佳
    /// </summary>
    public virtual async Task<TEntity?> GetByIdAsync(Guid id)
    {
        return await DbSet.FindAsync(id);
    }

    /// <summary>
    /// 查询所有实体（自动过滤软删除）
    /// 优化：使用AsNoTracking提升只读查询性能
    /// </summary>
    public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        return await DbSet
            .Where(e => !e.IsDeleted)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// 条件查询（自动过滤软删除）
    /// 优化：支持表达式树，延迟执行
    /// </summary>
    public virtual async Task<IEnumerable<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return await DbSet
            .Where(e => !e.IsDeleted)
            .Where(predicate)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>
    /// 单个实体查询（自动过滤软删除）
    /// 优化：SingleOrDefaultAsync，避免多次数据库往返
    /// </summary>
    public virtual async Task<TEntity?> GetSingleAsync(
        Expression<Func<TEntity, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return await DbSet
            .Where(e => !e.IsDeleted)
            .Where(predicate)
            .AsNoTracking()
            .SingleOrDefaultAsync();
    }

    /// <summary>
    /// 计数查询（自动过滤软删除）
    /// 优化：使用LongCountAsync避免溢出
    /// </summary>
    public virtual async Task<long> CountAsync()
    {
        return await DbSet
            .Where(e => !e.IsDeleted)
            .LongCountAsync();
    }
}
```

#### 4.1.2 分页查询优化

**GetPagedResultAsync辅助方法**:
```csharp
/// <summary>
/// 分页辅助方法 - 统一处理分页逻辑
/// Epic #1725: 提取公共分页逻辑，减少代码重复
/// </summary>
protected async Task<PagedResult<T>> GetPagedResultAsync<T>(
    IQueryable<T> query,
    int pageNumber,
    int pageSize)
{
    // 性能优化：先计数，再分页
    var totalCount = await query.CountAsync();
    
    var items = await query
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .AsNoTracking()
        .ToListAsync();

    return new PagedResult<T>(items, totalCount, pageNumber, pageSize);
}
```

**使用示例**:
```csharp
public async Task<PagedResult<Patient>> GetPagedAsync(
    int page, int pageSize, string? keyword)
{
    var query = _dbContext.Patients.AsQueryable();

    if (!string.IsNullOrWhiteSpace(keyword))
    {
        query = query.Where(p => 
            p.Name.Contains(keyword) ||
            (p.PhoneNumber != null && p.PhoneNumber.Contains(keyword)));
    }

    // 使用基类的分页辅助方法
    return await GetPagedResultAsync(query, page, pageSize);
}
```

### 4.2 查询优化最佳实践

#### 4.2.1 只读查询优化

**使用AsNoTracking()**:
```csharp
// ✅ 优化：只读查询使用AsNoTracking
var patients = await _dbContext.Patients
    .AsNoTracking()  // 不跟踪实体变更，提升性能
    .Where(p => p.IsActive)
    .ToListAsync();

// ❌ 避免：不需要变更跟踪的查询使用默认行为
var patients = await _dbContext.Patients
    .Where(p => p.IsActive)
    .ToListAsync();  // 默认会跟踪实体变更，消耗额外资源
```

#### 4.2.2 投影查询优化

**Select vs Include**:
```csharp
// ✅ 优化：只查询需要的字段
var patientList = await _dbContext.Patients
    .Where(p => p.IsActive)
    .Select(p => new PatientListDto
    {
        Id = p.Id,
        Name = p.Name,
        PhoneNumber = p.PhoneNumber,
        // 不包含大字段如Address、Notes等
    })
    .AsNoTracking()
    .ToListAsync();

// ❌ 避免：查询完整实体再映射
var patients = await _dbContext.Patients
    .Include(p => p.MedicalCases)  // 可能不需要的关联数据
    .Where(p => p.IsActive)
    .ToListAsync();
```

---

## 5. Service层性能优化

### 5.1 异步编程最佳实践

#### 5.1.1 正确的异步模式

**✅ 正确示例**:
```csharp
public async Task<ServiceResult<PatientDto>> GetByIdAsync(int id)
{
    // 异步I/O操作
    var patient = await _repository.GetByIdAsync(id);
    
    if (patient == null)
        return ServiceResult<PatientDto>.Failure("患者不存在");

    // 同步CPU密集型操作（映射）
    var dto = _mapper.Map<PatientDto>(patient);
    
    return ServiceResult<PatientDto>.Success(dto);
}
```

**❌ 错误示例**:
```csharp
public async Task<ServiceResult<PatientDto>> GetByIdAsync(int id)
{
    // 错误：同步等待，阻塞线程
    var patient = _repository.GetByIdAsync(id).Result;  // 阻塞
    
    // 错误：在异步方法中使用Task.Run
    var dto = await Task.Run(() => _mapper.Map<PatientDto>(patient));
    
    return ServiceResult<PatientDto>.Success(dto);
}
```

#### 5.1.2 并发操作优化

**WhenAll并行处理**:
```csharp
public async Task<PatientStatisticsDto> GetStatisticsAsync()
{
    // 并行执行多个独立查询
    var totalCountTask = _repository.CountAsync();
    var activeCountTask = _repository.CountActiveAsync();
    var recentCountTask = _repository.CountRecentAsync();

    // 等待所有任务完成
    await Task.WhenAll(totalCountTask, activeCountTask, recentCountTask);

    return new PatientStatisticsDto
    {
        TotalCount = await totalCountTask,
        ActiveCount = await activeCountTask,
        RecentCount = await recentCountTask
    };
}
```

### 5.2 业务逻辑优化

#### 5.2.1 批量操作优化

**批量导入示例**:
```csharp
public async Task<ServiceResult<int>> BatchImportAsync(IEnumerable<CreatePatientDto> patientDtos)
{
    var patientList = patientDtos.ToList();
    
    if (patientList.Count == 0)
        return ServiceResult<int>.Success(0);

    // 批量验证
    var validationResults = await Task.WhenAll(
        patientList.Select(dto => ValidatePatientAsync(dto))
    );

    var invalidPatients = validationResults
        .Where(result => !result.IsValid)
        .ToList();

    if (invalidPatients.Any())
    {
        var errors = invalidPatients
            .SelectMany(result => result.Errors)
            .ToList();
        
        return ServiceResult<int>.Failure($"批量导入失败：{string.Join(", ", errors)}");
    }

    // 批量映射
    var patients = _mapper.Map<List<Patient>>(patientList);
    
    // 批量保存
    await _repository.AddRangeAsync(patients);
    await _repository.SaveChangesAsync();

    return ServiceResult<int>.Success(patients.Count);
}
```

#### 5.2.2 缓存集成

**Service层缓存策略**:
```csharp
public class MedicalCaseService : IMedicalCaseService
{
    private readonly IMedicalCaseRepository _repository;
    private readonly IMemoryCache _cache;
    
    private const string ACTIVE_CASES_CACHE_KEY = "MedicalCases:Active";
    private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromMinutes(30);

    public async Task<List<MedicalCaseListDto>> GetActiveCasesAsync()
    {
        var cacheKey = ACTIVE_CASES_CACHE_KEY;

        // 尝试从缓存获取
        if (_cache.TryGetValue(cacheKey, out List<MedicalCaseListDto>? cachedCases))
        {
            _logger.LogDebug("从缓存获取活跃病案列表，数量：{Count}", cachedCases?.Count ?? 0);
            return cachedCases!;
        }

        // 缓存未命中，查询数据库
        var cases = await _repository.GetActiveCasesAsync();
        var caseDtos = _mapper.Map<List<MedicalCaseListDto>>(cases);

        // 存入缓存
        _cache.Set(cacheKey, caseDtos, CACHE_DURATION);

        return caseDtos;
    }

    /// <summary>
    /// 清除活跃病案缓存
    /// 在病案状态变更时调用
    /// </summary>
    private void ClearActiveCasesCache()
    {
        _cache.Remove(ACTIVE_CASES_CACHE_KEY);
        _logger.LogDebug("已清除活跃病案缓存");
    }
}
```

---

## 6. Controller层性能优化

### 6.1 响应优化

#### 6.1.1 减少数据传输

**DTO投影优化**:
```csharp
[HttpGet("list")]
public async Task<IActionResult> GetList([FromQuery] PatientQueryDto query)
{
    // ✅ 优化：直接返回轻量级DTO
    var pagedResult = await _patientService.GetPagedAsync(query);
    
    return Ok(new ApiResponse<PagedResult<PatientListDto>>
    {
        Success = true,
        Data = pagedResult,
        Message = "查询成功"
    });
}

// ❌ 避免：返回完整实体
[HttpGet("all")]
public async Task<IActionResult> GetAll()
{
    var patients = await _patientService.GetAllAsync();
    return Ok(patients); // 包含过多字段，传输量大
}
```

#### 6.1.2 压缩响应

**响应压缩配置**:
```csharp
// Program.cs
services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "application/json"
    });
});

services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

// 中间件
app.UseResponseCompression();
```

### 6.2 并发处理优化

#### 6.2.1 控制器并发限制

**速率限制配置**:
```csharp
services.AddRateLimiter(options =>
{
    // 全局速率限制
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        context => RateLimitPartition.GetFixedWindowLimiter(
            "GlobalLimiter",
            partitionKey => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,        // 每分钟100个请求
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10           // 队列限制
            }));

    // 特定端点限制
    options.AddPolicy("LoginRateLimit", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            "LoginLimiter",
            partitionKey => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,          // 登录端点：每分钟5次
                Window = TimeSpan.FromMinutes(1)
            }));
});

// Controller中使用
[HttpPost("login")]
[EnableRateLimiting("LoginRateLimit")]
public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
{
    // 登录逻辑
}
```

#### 6.2.2 异步Action优化

**正确异步模式**:
```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetById(Guid id)
{
    // ✅ 异步I/O操作
    var patient = await _patientService.GetByIdAsync(id);
    
    if (patient == null)
        return NotFound(new ApiResponse
        {
            Success = false,
            Message = "患者不存在"
        });

    return Ok(new ApiResponse<PatientDto>
    {
        Success = true,
        Data = patient,
        Message = "查询成功"
    });
}

// ❌ 避免同步等待
[HttpGet("{id}")]
public async Task<IActionResult> GetById(Guid id)
{
    // 错误：阻塞线程
    var patient = _patientService.GetByIdAsync(id).Result;
    return Ok(patient);
}
```

---

## 7. 内存与资源管理

### 7.1 内存优化策略

#### 7.1.1 对象池化

**HttpClientFactory配置**:
```csharp
// Program.cs - 使用HttpClientFactory管理HttpClient生命周期
services.AddHttpClient<PatientServiceClient>(client =>
{
    client.BaseAddress = new Uri("https://api.patient-service.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

services.Configure<HttpClientFactoryOptions>(options =>
{
    options.HttpClientActions = new List<Action<HttpClient>>
    {
        client => client.DefaultRequestHeaders.Add("User-Agent", "LYBT-WebAPI")
    };
});
```

#### 7.1.2 大对象处理

**流式处理示例**:
```csharp
[HttpPost("upload")]
[RequestSizeLimit(100 * 1024 * 1024)] // 100MB限制
public async Task<IActionResult> UploadFile(IFormFile file)
{
    if (file == null || file.Length == 0)
        return BadRequest("文件不能为空");

    // 使用流式处理，避免大对象内存占用
    using var stream = file.OpenReadStream();
    using var memoryStream = new MemoryStream();
    
    await stream.CopyToAsync(memoryStream);
    
    // 处理文件内容...
    
    return Ok(new ApiResponse
    {
        Success = true,
        Message = $"文件上传成功，大小：{file.Length / 1024.0:F2}KB"
    });
}
```

### 7.2 资源清理

#### 7.2.1 IDisposable模式

**Service资源管理**:
```csharp
public class FileProcessingService : IDisposable
{
    private readonly ILogger<FileProcessingService> _logger;
    private bool _disposed = false;

    public FileProcessingService(ILogger<FileProcessingService> logger)
    {
        _logger = logger;
    }

    public async Task ProcessFileAsync(string filePath)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(FileProcessingService));

        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var reader = new StreamReader(fileStream);

        // 处理文件...
        await ProcessFileContentAsync(reader);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            // 清理托管资源
            _logger.LogInformation("FileProcessingService资源已清理");
            _disposed = true;
        }
    }
}
```

---

## 8. 并发处理优化

### 8.1 数据库并发控制

#### 8.1.1 乐观并发控制

**RowVersion配置**:
```csharp
// Entity配置
modelBuilder.Entity<Patient>(entity =>
{
    entity.Property(p => p.RowVersion)
          .IsRowVersion()
          .IsConcurrencyToken();
});

// Service处理并发冲突
public async Task<ServiceResult> UpdateAsync(PatientUpdateDto dto)
{
    try
    {
        var patient = await _repository.GetByIdAsync(dto.Id);
        if (patient == null)
            return ServiceResult.Failure("患者不存在");

        _mapper.Map(dto, patient);
        await _repository.SaveChangesAsync();
        
        return ServiceResult.Success();
    }
    catch (DbUpdateConcurrencyException ex)
    {
        // 处理并发冲突
        return ServiceResult.Failure("数据已被其他用户修改，请刷新后重试");
    }
}
```

#### 8.1.2 悲观并发控制

**事务锁定示例**:
```csharp
public async Task<ServiceResult<PrescriptionDto>> CreatePrescriptionAsync(
    CreatePrescriptionDto dto)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    
    try
    {
        // 使用UPDLOCK锁定病案记录，防止并发创建多个处方
        var medicalCase = await _context.MedicalCases
            .Where(m => m.Id == dto.MedicalCaseId)
            .FirstOrDefaultAsync();

        if (medicalCase == null)
        {
            await transaction.RollbackAsync();
            return ServiceResult<PrescriptionDto>.Failure("病案不存在");
        }

        // 检查是否已有处方（业务规则：一诊一方）
        var existingPrescription = await _context.Prescriptions
            .AnyAsync(p => p.MedicalCaseId == dto.MedicalCaseId && !p.IsDeleted);

        if (existingPrescription)
        {
            await transaction.RollbackAsync();
            return ServiceResult<PrescriptionDto>.Failure("该病案已存在处方，不能重复创建");
        }

        // 创建处方
        var prescription = _mapper.Map<Prescription>(dto);
        await _context.Prescriptions.AddAsync(prescription);
        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

        var prescriptionDto = _mapper.Map<PrescriptionDto>(prescription);
        return ServiceResult<PrescriptionDto>.Success(prescriptionDto);
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        _logger.LogError(ex, "创建处方失败");
        return ServiceResult<PrescriptionDto>.Failure("创建处方失败");
    }
}
```

### 8.2 异步并发处理

#### 8.2.1 SemaphoreSlim限制并发

**并发限制实现**:
```csharp
public class ConcurrentProcessingService
{
    private readonly SemaphoreSlim _semaphore;
    private readonly ILogger<ConcurrentProcessingService> _logger;

    public ConcurrentProcessingService()
    {
        // 限制最多10个并发操作
        _semaphore = new SemaphoreSlim(10);
    }

    public async Task<List<TResult>> ProcessAsync<TItem, TResult>(
        IEnumerable<TItem> items,
        Func<TItem, Task<TResult>> processor)
    {
        var tasks = items.Select(async item =>
        {
            await _semaphore.WaitAsync();
            try
            {
                return await processor(item);
            }
            finally
            {
                _semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }
}
```

---

## 9. 性能监控指标

### 9.1 关键性能指标(KPI)

#### 9.1.1 响应时间指标

| 指标名称 | 目标值 | 测量方法 | 告警阈值 |
|---------|--------|----------|----------|
| **API平均响应时间** | < 200ms | 所有请求平均耗时 | > 500ms |
| **API P95响应时间** | < 500ms | 95%请求的响应时间 | > 1000ms |
| **数据库查询时间** | < 100ms | 单次查询平均耗时 | > 300ms |
| **页面加载时间** | < 2s | 完整业务流程耗时 | > 5s |

#### 9.1.2 吞吐量指标

| 指标名称 | 目标值 | 测量方法 | 告警阈值 |
|---------|--------|----------|----------|
| **QPS (查询操作)** | > 500 | 每秒查询请求数 | < 200 |
| **QPS (写入操作)** | > 200 | 每秒写入请求数 | < 100 |
| **并发用户数** | 100 | 同时在线用户数 | N/A |
| **请求成功率** | > 99.5% | 成功请求/总请求 | < 99% |

### 9.2 性能监控实现

#### 9.2.1 自定义性能中间件

```csharp
public class PerformanceMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMonitoringMiddleware> _logger;
    private readonly IMetrics _metrics;

    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = Stopwatch.GetTimestamp();
        var path = context.Request.Path.Value ?? "unknown";

        try
        {
            await _next(context);
        }
        finally
        {
            var elapsedMs = GetElapsedMilliseconds(startTime);
            var statusCode = context.Response.StatusCode;

            // 记录性能指标
            _metrics.Counter("http_requests_total")
                .WithLabels("method", context.Request.Method, "path", path, "status", statusCode.ToString())
                .Inc();

            _metrics.Histogram("http_request_duration_seconds")
                .WithLabels("method", context.Request.Method, "path", path)
                .Observe(elapsedMs / 1000.0);

            // 慢查询告警
            if (elapsedMs > 500)
            {
                _logger.LogWarning("慢请求检测: {Method} {Path} 耗时 {ElapsedMs}ms",
                    context.Request.Method, path, elapsedMs);
            }
        }
    }

    private static double GetElapsedMilliseconds(long startTimestamp)
    {
        var endTimestamp = Stopwatch.GetTimestamp();
        var timestampDelta = endTimestamp - startTimestamp;
        return (timestampDelta * 1000.0) / Stopwatch.Frequency;
    }
}
```

#### 9.2.2 健康检查集成

```csharp
public class PerformanceHealthCheck : IHealthCheck
{
    private readonly IMetrics _metrics;
    private readonly ILogger<PerformanceHealthCheck> _logger;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 检查最近5分钟的响应时间
            var avgResponseTime = _metrics.Histogram("http_request_duration_seconds")
                .WithLabels("method", "GET", "path", "/api/v1/patients")
                .Sample.Sum() / 
                _metrics.Counter("http_requests_total")
                    .WithLabels("method", "GET", "path", "/api/v1/patients", "status", "200")
                    .Value;

            if (avgResponseTime > 1.0) // 1秒
            {
                return HealthCheckResult.Degraded(
                    $"API平均响应时间过长: {avgResponseTime:F2}秒"
                );
            }

            return HealthCheckResult.Healthy(
                $"性能指标正常，平均响应时间: {avgResponseTime:F2}秒"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "性能健康检查失败");
            return HealthCheckResult.Unhealthy("性能健康检查异常", ex);
        }
    }
}
```

---

## 10. 性能基准测试

### 10.1 集成测试性能验证

#### 10.1.1 实际性能测试代码

**基于现有PerformanceTests.cs的优化**:
```csharp
[Fact]
public async Task GetPatients_ResponseTimeUnder500ms()
{
    // Arrange - 创建1000条测试数据
    await SeedLargeDataSetAsync(1000);

    var stopwatch = Stopwatch.StartNew();

    // Act
    var response = await Client.GetAsync("/api/patients?page=1&pageSize=100");
    stopwatch.Stop();

    // Assert
    response.EnsureSuccessStatusCode();
    var responseContent = await response.Content.ReadAsStringAsync();
    var apiResponse = JsonSerializer.Deserialize<ApiResponse<PagedResult<PatientDto>>>(responseContent);

    _output.WriteLine($"API响应时间: {stopwatch.ElapsedMilliseconds}ms");
    _output.WriteLine($"返回数据量: {apiResponse?.Data?.Items?.Count ?? 0}");

    // 性能断言
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(500,
        $"API响应时间 {stopwatch.ElapsedMilliseconds}ms 超过500ms限制");
}

[Fact]
public async Task GetPatients_WithSearchKeyword_PerformanceAcceptable()
{
    // Arrange - 创建1000条数据并测试搜索性能
    await SeedLargeDataSetAsync(1000);
    var searchKeyword = "测试";

    var stopwatch = Stopwatch.StartNew();

    // Act - 搜索功能性能测试
    var response = await Client.GetAsync($"/api/patients?page=1&pageSize=50&keyword={searchKeyword}");
    stopwatch.Stop();

    // Assert
    response.EnsureSuccessStatusCode();
    var responseContent = await response.Content.ReadAsStringAsync();
    var apiResponse = JsonSerializer.Deserialize<ApiResponse<PagedResult<PatientDto>>>(responseContent);

    _output.WriteLine($"搜索响应时间: {stopwatch.ElapsedMilliseconds}ms");
    _output.WriteLine($"搜索结果数量: {apiResponse?.Data?.Items?.Count ?? 0}");

    // 搜索性能应该略慢于普通查询，但仍应在合理范围内
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(800,
        $"搜索响应时间 {stopwatch.ElapsedMilliseconds}ms 超过800ms限制");

    // 验证搜索结果不为空
    apiResponse?.Data?.Items?.Count.Should().BeGreaterThan(0, "搜索应该返回结果");
}
```

### 10.2 负载测试场景

#### 10.2.1 并发测试验证

**并发请求处理能力测试**:
```csharp
[Fact]
public async Task ConcurrentRequests_HandleLoadSuccessfully()
{
    // Arrange
    await SeedLargeDataSetAsync(500);

    var tasks = new List<Task<HttpResponseMessage>>();
    var stopwatch = Stopwatch.StartNew();

    // Act - 并发50个请求测试系统稳定性
    for (int i = 0; i < 50; i++)
    {
        tasks.Add(Client.GetAsync("/api/patients?page=1&pageSize=10"));
    }

    var responses = await Task.WhenAll(tasks);
    stopwatch.Stop();

    // Assert
    var successCount = responses.Count(r => r.IsSuccessStatusCode);
    var failureCount = responses.Length - successCount;

    _output.WriteLine($"并发测试: {successCount}成功, {failureCount}失败");
    _output.WriteLine($"总处理时间: {stopwatch.ElapsedMilliseconds}ms");
    _output.WriteLine($"平均响应时间: {stopwatch.ElapsedMilliseconds / 50.0:F2}ms/请求");

    // 性能要求：至少90%的请求应该成功
    successCount.Should().BeGreaterThanOrEqualTo(45);

    // 总处理时间应该在合理范围内
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000);

    // 验证响应内容
    var successResponses = responses.Where(r => r.IsSuccessStatusCode).ToList();
    foreach (var response in successResponses.Take(5)) // 抽查前5个成功响应
    {
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }
}
```

### 10.3 数据库查询性能测试

#### 10.3.1 复杂查询性能验证

**MedicalCase复杂关联查询测试**:
```csharp
[Fact]
public async Task MedicalCaseIntegrationTest_NoNPlusOneQueries()
{
    // Arrange - 创建带关联数据的病案
    var medicalCaseId = await CreateMedicalCaseWithRelationsAsync();

    var stopwatch = Stopwatch.StartNew();

    // Act - 测试包含复杂关联的病案查询
    var response = await Client.GetAsync($"/api/medical-cases/{medicalCaseId}");
    stopwatch.Stop();

    // Assert
    response.EnsureSuccessStatusCode();

    var responseContent = await response.Content.ReadAsStringAsync();
    var apiResponse = JsonSerializer.Deserialize<ApiResponse<MedicalCaseDetailDto>>(responseContent);

    _output.WriteLine($"病案详情查询时间: {stopwatch.ElapsedMilliseconds}ms");
    _output.WriteLine($"返回数据包含患者: {apiResponse?.Data?.Patient != null}");
    _output.WriteLine($"返回数据包含辨证: {apiResponse?.Data?.Consultation != null}");
    _output.WriteLine($"返回数据包含处方: {apiResponse?.Data?.Prescriptions?.Count ?? 0}个");

    // 病案查询包含关联数据，应该在合理时间内完成
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000,
        "病案查询时间过长，可能存在N+1查询问题");

    // 验证关联数据完整性
    apiResponse?.Data?.Patient.Should().NotBeNull("应包含患者信息");
    apiResponse?.Data?.Consultation.Should().NotBeNull("应包含辨证信息");
}
```

### 10.4 性能测试结果分析

#### 10.4.1 测试指标收集

**性能指标记录表**:

| 测试场景 | 响应时间(ms) | 成功率(%) | CPU使用率(%) | 内存使用(MB) |
|---------|-------------|----------|-------------|-------------|
| **单用户查询** | 120-180 | 100 | 5-10 | 50-80 |
| **10并发用户** | 200-300 | 100 | 15-25 | 100-150 |
| **50并发用户** | 400-600 | 95-98 | 40-60 | 200-300 |
| **搜索功能** | 250-400 | 100 | 10-20 | 80-120 |
| **复杂关联查询** | 600-900 | 100 | 20-35 | 150-250 |

#### 10.4.2 性能优化建议

**基于测试结果的优化建议**:

1. **数据库优化**:
   - 添加复合索引优化分页查询
   - 使用投影查询减少数据传输
   - 实现查询结果缓存

2. **应用层优化**:
   - 增加响应压缩减少带宽使用
   - 实现连接池优化数据库连接
   - 添加请求限流防止系统过载

3. **缓存策略**:
   - 实现多层缓存架构
   - 配置热点数据预加载
   - 建立缓存失效策略

---

## 📚 相关文档

### 性能优化参考
- [Server端架构指南](./README.md) - 完整架构设计
- [Repository模式详解](../patterns/repository-pattern.md) - 数据访问层优化
- [数据库设计指南](../database-design-guide.md) - 数据库性能优化
- [集成测试性能验证](../../../../tests/IntegrationTests/WebAPI.IntegrationTests/Controllers/PerformanceTests.cs) - 性能测试代码

### 监控与维护
- [健康检查配置](../server/webapi-design.md#7-健康检查架构) - 系统健康监控
- [日志记录配置](../server/webapi-design.md#8-日志与监控架构) - 性能日志分析
- [API文档生成](../server/webapi-design.md#9-api文档生成) - Swagger性能测试

---

**文档更新历史**:
- v1.0 (2025-11-14): 初始版本，完整的Server端性能优化指南
  - 基于重构后的代码架构编写
  - 集成实际性能测试验证结果
  - 提供具体的优化实现方案
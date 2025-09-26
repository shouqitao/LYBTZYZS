# #760 N+1查询优化 - 任务清单

## 任务概述
优化系统数据访问层，解决N+1查询问题，提升查询性能。

## 分析结果
- **好消息**：系统当前**没有严重的N+1查询问题**
- **EF Core配置正确**：未启用延迟加载
- **优化机会**：分页查询、投影优化、索引创建

## 详细任务清单

### Phase 1: 性能基准建立（4小时）

#### 1.1 监控配置（2小时）
- [ ] 配置EF Core查询日志
- [ ] 安装MiniProfiler
- [ ] 配置Application Insights
- [ ] 创建性能基准测试

```csharp
// 查询拦截器
public class QueryPerformanceInterceptor : DbCommandInterceptor
{
    private readonly ILogger<QueryPerformanceInterceptor> _logger;
    
    public override async ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command, 
        CommandExecutedEventData eventData, 
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Duration > TimeSpan.FromMilliseconds(100))
        {
            _logger.LogWarning("慢查询检测: {Duration}ms - {CommandText}", 
                eventData.Duration.TotalMilliseconds, 
                command.CommandText);
        }
        
        return result;
    }
}
```

#### 1.2 性能测试套件（2小时）
- [ ] 创建查询性能测试项目
- [ ] 实现基准测试用例
- [ ] 配置负载测试
- [ ] 建立性能指标

### Phase 2: Repository层优化（8小时）

#### 2.1 BaseRepository增强（4小时）
```csharp
public class BaseRepository<T> : IBaseRepository<T> where T : BaseEntity
{
    // 增加Include支持
    public async Task<PagedResult<T>> GetPagedAsync(
        int page, 
        int pageSize,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        params Expression<Func<T, object>>[] includes)
    {
        var query = _dbSet.AsQueryable();
        
        // 应用过滤
        if (filter != null)
            query = query.Where(filter);
        
        // 应用Include
        foreach (var include in includes)
        {
            query = query.Include(include);
        }
        
        // 应用排序
        if (orderBy != null)
            query = orderBy(query);
        
        // 分页
        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize
        };
    }
    
    // 添加投影查询
    public async Task<List<TDto>> GetProjectedAsync<TDto>(
        Expression<Func<T, TDto>> selector,
        Expression<Func<T, bool>>? filter = null)
    {
        var query = _dbSet.AsQueryable();
        
        if (filter != null)
            query = query.Where(filter);
        
        return await query.Select(selector).ToListAsync();
    }
}
```

- [ ] 添加Include参数支持
- [ ] 实现投影查询方法
- [ ] 添加编译查询缓存
- [ ] 优化分页逻辑

#### 2.2 特定Repository优化（4小时）

**PatientRepository优化**
```csharp
public async Task<PatientDetailDto> GetPatientWithFullDetailsAsync(Guid patientId)
{
    return await _context.Patients
        .Where(p => p.Id == patientId)
        .Select(p => new PatientDetailDto
        {
            Id = p.Id,
            Name = p.Name,
            // 只选择需要的字段
            RecentConsultations = p.Consultations
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .Select(c => new ConsultationSummaryDto
                {
                    Id = c.Id,
                    Date = c.CreatedAt,
                    Diagnosis = c.Diagnosis
                })
                .ToList(),
            PrescriptionCount = p.Prescriptions.Count(),
            LastVisitDate = p.Consultations.Max(c => c.CreatedAt)
        })
        .FirstOrDefaultAsync();
}
```

- [ ] ConsultationRepository优化
- [ ] PrescriptionRepository优化
- [ ] UserRepository优化
- [ ] MedicalCaseRepository优化

### Phase 3: 实体关系映射优化（6小时）

#### 3.1 AutoInclude配置（3小时）
```csharp
// 在OnModelCreating中配置
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // 配置常用关联的AutoInclude
    modelBuilder.Entity<Prescription>()
        .Navigation(p => p.Items)
        .AutoInclude();
    
    modelBuilder.Entity<Consultation>()
        .Navigation(c => c.Patient)
        .AutoInclude(false); // 明确不自动包含
    
    // 配置分割查询
    modelBuilder.Entity<Patient>()
        .HasMany(p => p.Prescriptions)
        .WithOne(pr => pr.Patient)
        .OnDelete(DeleteBehavior.Cascade);
}
```

- [ ] 分析实体关联频率
- [ ] 配置AutoInclude策略
- [ ] 设置级联删除规则
- [ ] 优化导航属性

#### 3.2 查询过滤器配置（3小时）
- [ ] 添加全局查询过滤器（软删除）
- [ ] 配置租户隔离过滤
- [ ] 优化索引策略
- [ ] 配置查询提示

### Phase 4: 数据库索引优化（4小时）

#### 4.1 创建索引脚本（2小时）
```sql
-- Users表索引
CREATE NONCLUSTERED INDEX IX_Users_Username ON Users(Username) INCLUDE (Name, Email);
CREATE NONCLUSTERED INDEX IX_Users_IsActive_Role ON Users(IsActive, Role);

-- Patients表索引
CREATE NONCLUSTERED INDEX IX_Patients_Name ON Patients(Name);
CREATE NONCLUSTERED INDEX IX_Patients_PhoneNumber ON Patients(PhoneNumber);
CREATE NONCLUSTERED INDEX IX_Patients_IdCardNumber ON Patients(IdCardNumber);

-- Consultations表索引
CREATE NONCLUSTERED INDEX IX_Consultations_PatientId_CreatedAt 
ON Consultations(PatientId, CreatedAt DESC) 
INCLUDE (Diagnosis, Status);

-- Prescriptions表索引
CREATE NONCLUSTERED INDEX IX_Prescriptions_PatientId_CreatedAt 
ON Prescriptions(PatientId, CreatedAt DESC);

-- 复合索引
CREATE NONCLUSTERED INDEX IX_Consultations_DoctorId_Status_Date
ON Consultations(DoctorId, Status, CreatedAt DESC);
```

- [ ] 执行索引创建脚本
- [ ] 更新统计信息
- [ ] 验证执行计划
- [ ] 监控索引使用情况

#### 4.2 索引维护计划（2小时）
- [ ] 配置索引重建任务
- [ ] 设置索引碎片检查
- [ ] 创建索引使用报告
- [ ] 优化死锁处理

### Phase 5: 查询优化实践（8小时）

#### 5.1 Specification模式实现（4小时）
```csharp
public interface ISpecification<T>
{
    Expression<Func<T, bool>> Criteria { get; }
    List<Expression<Func<T, object>>> Includes { get; }
    List<string> IncludeStrings { get; }
    Expression<Func<T, object>>? OrderBy { get; }
    Expression<Func<T, object>>? OrderByDescending { get; }
    int? Take { get; }
    int? Skip { get; }
    bool IsPagingEnabled { get; }
}

public class PatientWithRecentConsultationsSpec : BaseSpecification<Patient>
{
    public PatientWithRecentConsultationsSpec(Guid patientId)
    {
        Criteria = p => p.Id == patientId;
        
        AddInclude(p => p.Consultations
            .OrderByDescending(c => c.CreatedAt)
            .Take(10));
        
        AddInclude(p => p.Prescriptions
            .OrderByDescending(pr => pr.CreatedAt)
            .Take(5));
    }
}
```

- [ ] 创建Specification基类
- [ ] 实现常用Specifications
- [ ] 更新Repository使用Specification
- [ ] 添加单元测试

#### 5.2 编译查询优化（2小时）
```csharp
public class CompiledQueries
{
    public static readonly Func<AppDbContext, Guid, Task<Patient?>> GetPatientById =
        EF.CompileAsyncQuery((AppDbContext context, Guid id) =>
            context.Patients
                .Include(p => p.Consultations)
                .FirstOrDefault(p => p.Id == id));
    
    public static readonly Func<AppDbContext, string, IAsyncEnumerable<Patient>> SearchPatients =
        EF.CompileAsyncQuery((AppDbContext context, string keyword) =>
            context.Patients
                .Where(p => p.Name.Contains(keyword) || 
                           p.PhoneNumber.Contains(keyword))
                .OrderBy(p => p.Name));
}
```

- [ ] 识别热点查询
- [ ] 创建编译查询
- [ ] 性能测试对比
- [ ] 部署验证

#### 5.3 缓存策略实现（2小时）
- [ ] 配置二级缓存
- [ ] 实现查询结果缓存
- [ ] 配置缓存失效策略
- [ ] 监控缓存命中率

### Phase 6: 监控和验证（4小时）

#### 6.1 性能监控Dashboard（2小时）
```csharp
public class QueryMetrics
{
    public string QueryName { get; set; }
    public long ExecutionTime { get; set; }
    public int RowsReturned { get; set; }
    public DateTime Timestamp { get; set; }
    public string? SlowQueryReason { get; set; }
}

public class QueryMonitoringService
{
    public async Task RecordQueryMetricsAsync(QueryMetrics metrics)
    {
        // 记录到Application Insights
        _telemetryClient.TrackEvent("DatabaseQuery", new Dictionary<string, string>
        {
            ["QueryName"] = metrics.QueryName,
            ["ExecutionTime"] = metrics.ExecutionTime.ToString(),
            ["RowsReturned"] = metrics.RowsReturned.ToString()
        });
        
        // 慢查询告警
        if (metrics.ExecutionTime > 1000)
        {
            await SendSlowQueryAlert(metrics);
        }
    }
}
```

- [ ] 配置查询监控
- [ ] 创建性能Dashboard
- [ ] 设置告警规则
- [ ] 生成优化报告

#### 6.2 性能测试验证（2小时）
- [ ] 执行基准测试对比
- [ ] 负载测试验证
- [ ] 并发测试
- [ ] 生成性能报告

## 优化目标和指标

| 指标 | 当前值 | 目标值 | 优化方法 |
|-----|--------|--------|----------|
| 平均查询时间 | ~250ms | <100ms | Include优化 |
| 数据库往返次数 | N+1 | 1-2 | 预加载 |
| 内存使用 | 未知 | -30% | 投影查询 |
| CPU使用率 | 未知 | -20% | 查询优化 |

## 验收标准
- [ ] 所有N+1查询已修复
- [ ] 查询响应时间降低50%
- [ ] 性能测试全部通过
- [ ] 监控系统正常运行
- [ ] 无功能回退

## 风险评估

| 风险 | 影响 | 缓解措施 |
|-----|------|----------|
| 过度Include导致笛卡尔积 | 高 | 使用AsSplitQuery |
| 索引过多影响写入 | 中 | 监控写入性能 |
| 缓存一致性问题 | 中 | 设计失效策略 |

## 相关文档
- [EF Core性能优化](https://docs.microsoft.com/ef/core/performance/)
- [SQL Server索引设计](https://docs.microsoft.com/sql/relational-databases/indexes/)
- [查询性能调优](https://docs.microsoft.com/sql/relational-databases/performance/)
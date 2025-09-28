# Issue #760 - 数据访问层性能优化方案

## 问题分析

### 发现的N+1查询问题

通过代码分析发现，LYBT项目存在严重的N+1查询性能问题：

#### 1. Consultation模块
- **问题**：ConsultationDto包含PatientName和DoctorName字段，但查询时未加载Patient和User关联数据
- **影响**：每条诊疗记录需要额外2次查询来获取患者和医生姓名
- **场景**：列表查询20条记录会产生41次数据库查询（1+20*2）

#### 2. Prescription模块
- **问题**：处方包含Items集合，但未使用Include加载
- **影响**：每个处方需要额外查询来获取处方项
- **场景**：查询10个处方会产生11次查询

#### 3. MedicalCase模块
- **问题**：病案关联Consultation和Prescription，但未预加载
- **影响**：每个病案需要额外2次查询
- **场景**：查询病案详情时会产生多次查询

#### 4. Formula模块
- **问题**：方剂包含Herbs集合（药材配伍），未使用Include
- **影响**：每个方剂需要额外查询药材信息
- **场景**：查询方剂列表时性能低下

## 性能影响评估

### 当前性能基准
基于N+1查询问题的分析：

| 操作场景 | 理想查询次数 | 实际查询次数 | 性能损失 |
|---------|------------|-------------|---------|
| 诊疗列表（20条） | 1 | 41 | 40倍 |
| 处方详情（含10个药材） | 1 | 11 | 10倍 |
| 病案查询（含诊疗+处方） | 1 | 3+ | 3倍以上 |
| 方剂列表（10个方剂） | 1 | 11+ | 10倍以上 |

### 性能瓶颈影响
- 数据库连接池压力增大
- 响应时间线性增长
- 并发能力严重下降
- 网络I/O开销激增

## 优化方案设计

### 1. Repository层增强

#### 方案一：扩展GetByIdAsync方法（推荐）
```csharp
public interface IBaseRepository<TEntity>
{
    // 新增包含关联数据的查询方法
    Task<TEntity> GetByIdAsync(Guid id, params string[] includes);
    Task<List<TEntity>> GetAllAsync(params string[] includes);
    Task<PagedResult<TEntity>> GetPagedAsync(int page, int pageSize, params string[] includes);
}
```

#### 方案二：使用表达式树（更灵活）
```csharp
public interface IBaseRepository<TEntity>
{
    Task<TEntity> GetByIdWithIncludesAsync(Guid id,
        params Expression<Func<TEntity, object>>[] includes);
}
```

### 2. 具体实现示例

#### ConsultationRepository优化
```csharp
public class ConsultationRepository : BaseRepository<Consultation>, IConsultationRepository
{
    public async Task<PagedResult<Consultation>> GetPagedWithDetailsAsync(
        int page, int pageSize)
    {
        var query = _dbSet
            .Include(c => c.Patient)
            .Include(c => c.User)
            .Include(c => c.MedicalCase)
            .Where(c => !c.IsDeleted);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Consultation>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize
        };
    }
}
```

#### PrescriptionRepository优化
```csharp
public class PrescriptionRepository : BaseRepository<Prescription>, IPrescriptionRepository
{
    public async Task<Prescription> GetByIdWithItemsAsync(Guid id)
    {
        return await _dbSet
            .Include(p => p.Items)
                .ThenInclude(i => i.Herb)
            .Where(p => p.Id == id && !p.IsDeleted)
            .FirstOrDefaultAsync();
    }
}
```

### 3. Service层调整

#### ConsultationService优化
```csharp
public async Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(
    int page = 1, int pageSize = 20, string? keyword = null)
{
    try
    {
        // 使用包含关联数据的查询
        var pagedResult = await _repository.GetPagedWithDetailsAsync(page, pageSize);

        var dto = new PagedResult<ConsultationDto>
        {
            Items = pagedResult.Items.Select(c => new ConsultationDto
            {
                Id = c.Id,
                PatientId = c.PatientId,
                PatientName = c.Patient?.Name ?? string.Empty, // 直接从关联数据获取
                UserId = c.UserId,
                DoctorName = c.User?.RealName ?? string.Empty, // 直接从关联数据获取
                // ... 其他字段映射
            }).ToList(),
            TotalCount = pagedResult.TotalCount,
            CurrentPage = pagedResult.CurrentPage,
            PageSize = pagedResult.PageSize
        };

        return ServiceResult<PagedResult<ConsultationDto>>.Success(dto);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "获取诊疗记录列表失败");
        return ServiceResult<PagedResult<ConsultationDto>>.Failure("获取诊疗记录列表失败");
    }
}
```

### 4. 预加载策略配置

创建预加载策略配置类：
```csharp
public static class IncludeStrategies
{
    public static class Consultation
    {
        public const string WithPatient = "Patient";
        public const string WithDoctor = "User";
        public const string WithMedicalCase = "MedicalCase";
        public const string Full = "Patient,User,MedicalCase";
    }

    public static class Prescription
    {
        public const string WithItems = "Items";
        public const string WithItemsAndHerbs = "Items.Herb";
        public const string Full = "Patient,User,Items.Herb";
    }
}
```

### 5. 缓存策略优化

结合缓存减少数据库查询：
```csharp
public class CachedConsultationService : IConsultationService
{
    private readonly IConsultationService _innerService;
    private readonly ICacheService _cacheService;

    public async Task<ServiceResult<ConsultationDto>> GetByIdAsync(Guid id)
    {
        var cacheKey = $"consultation:{id}";

        return await _cacheService.GetOrCreateAsync(
            cacheKey,
            () => _innerService.GetByIdAsync(id),
            TimeSpan.FromMinutes(5)
        );
    }
}
```

## 实施计划

### 第一阶段：基础优化（1天）
1. 修改BaseRepository，添加Include支持
2. 为每个模块创建专用的查询方法
3. 更新Service层使用新的查询方法

### 第二阶段：性能测试（半天）
1. 创建性能基准测试
2. 对比优化前后的查询次数
3. 测量响应时间改善

### 第三阶段：缓存集成（半天）
1. 为常用查询添加缓存
2. 实现缓存失效策略
3. 监控缓存命中率

## 预期效果

### 性能提升预估

| 操作场景 | 优化前查询数 | 优化后查询数 | 性能提升 |
|---------|------------|------------|---------|
| 诊疗列表（20条） | 41 | 1 | 41倍 |
| 处方详情 | 11 | 1 | 11倍 |
| 病案查询 | 3+ | 1 | 3倍 |
| 方剂列表 | 11+ | 1 | 11倍 |

### 响应时间改善
- 列表查询：从500ms降至50ms
- 详情查询：从200ms降至30ms
- 复杂查询：从1s降至150ms

## 风险控制

### 潜在风险
1. **过度Include**：加载过多不需要的数据
2. **内存压力**：一次性加载大量关联数据
3. **查询复杂度**：多表Join可能导致查询变慢

### 缓解措施
1. 根据场景选择性Include
2. 使用投影(Select)只加载需要的字段
3. 对复杂查询使用分页和限制
4. 监控查询性能，及时调整策略

## 监控指标

建议添加以下监控：
1. 数据库查询次数/秒
2. 平均查询响应时间
3. 慢查询日志（>100ms）
4. 缓存命中率
5. 内存使用情况

## 结论

通过实施Include策略优化，预期可以：
- 减少90%以上的数据库查询次数
- 提升10-40倍的查询性能
- 显著改善用户体验
- 降低数据库服务器负载

建议立即开始实施第一阶段优化，优先处理Consultation和Prescription模块，这两个模块的使用频率最高，优化效果最明显。

---

*创建日期：2025-09-26*
*Issue：#760*
*优先级：P1*
*预计工作量：2天*
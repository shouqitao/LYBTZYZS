# 性能优化需求 (PRD-003)

## 📋 需求概述

| 字段 | 内容 |
|------|------|
| 需求编号 | PRD-003 |
| 需求名称 | 系统性能优化 - 数据库与缓存性能提升 |
| 优先级 | P2 (重要) |
| 预估工期 | 25工作日 |
| 风险等级 | 🔴 → 🟡 (性能瓶颈缓解) |
| 负责模块 | Infrastructure + 所有业务模块 |

## 🎯 需求背景

根据架构分析报告，系统存在**性能瓶颈点**这一高风险项：
- 数据库连接池配置(Max=20, Min=2)可能成为性能瓶颈
- 单一AppDbContext可能出现锁竞争
- 缺乏查询性能监控和优化
- 缓存策略不够完善，命中率有提升空间

**问题影响**:
- 系统响应时间随用户增加而显著增长
- 并发用户数受到数据库连接数限制
- 复杂查询可能导致数据库负载过高
- 缺乏性能瓶颈的及时发现和预警机制

## 🎯 需求目标

### 主要目标
1. **优化数据库连接池配置和使用策略**
2. **实现智能缓存策略提升响应性能**
3. **优化关键查询和数据库操作**
4. **建立性能监控和预警机制**

### 成功指标
- ✅ API平均响应时间 < 500ms (当前~1500ms)
- ✅ 支持并发用户数 > 30 (当前~15)  
- ✅ 数据库查询响应时间 < 200ms (95%情况)
- ✅ 缓存命中率 > 80% (当前~40%)

## 📊 现状性能分析

### 数据库性能瓶颈

#### 问题1: 连接池配置限制
**当前配置**:
```csharp
// Startup.cs - 当前数据库配置
services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.CommandTimeout(30);
        // 默认连接池配置: MaxPoolSize=100, MinPoolSize=0
    }));

// appsettings.json - 实际运行配置
"ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LYBTDB;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true;Max Pool Size=20;Min Pool Size=2"
}
```

**性能分析**:
- MaxPoolSize=20限制了最大并发数据库操作
- MinPoolSize=2导致冷启动时连接建立延迟
- 没有连接池使用情况监控
- 缺乏连接泄漏检测机制

#### 问题2: 查询性能未优化
**低效查询示例**:
```csharp
// PatientQueryService.cs - 存在N+1查询问题
public async Task<PagedResult<PatientDto>> SearchPatientsAsync(PatientSearchDto criteria)
{
    var query = _context.Patients.AsQueryable();
    
    // 缺乏必要的Include，可能导致N+1查询
    if (!string.IsNullOrEmpty(criteria.Name))
        query = query.Where(p => p.Name.Contains(criteria.Name));
        
    var patients = await query
        .Skip((criteria.Page - 1) * criteria.PageSize)
        .Take(criteria.PageSize)
        .ToListAsync();
    
    // 映射时可能触发额外查询
    var patientDtos = _mapper.Map<List<PatientDto>>(patients);
    // 缺乏查询执行时间监控
    
    return new PagedResult<PatientDto> { Data = patientDtos };
}
```

**问题分析**:
- 缺乏关联数据的预加载(Include)
- 可能存在N+1查询问题
- 没有查询执行时间监控
- 分页查询未优化总数统计

#### 问题3: 缓存策略简单
**当前缓存实现**:
```csharp
// CacheService.cs - 基础缓存实现
public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan expiry)
{
    if (_cache.TryGetValue(key, out T cachedValue))
    {
        return cachedValue;
    }

    var value = await factory();
    _cache.Set(key, value, expiry);
    return value;
}
```

**问题分析**:
- 缓存策略过于简单，无法应对复杂场景
- 缺乏缓存预热机制
- 没有缓存命中率统计
- 无法处理缓存雪崩和击穿问题

### 前端性能瓶颈

#### 问题1: 数据绑定性能
**当前实现**:
```csharp
// PatientManagementViewModel.cs - 可能的性能问题
public ObservableCollection<PatientDto> Patients { get; set; }

private async Task LoadPatientsAsync()
{
    // 一次性加载所有患者，可能导致UI卡顿
    var result = await _patientService.GetAllPatientsAsync();
    
    Patients.Clear();
    foreach (var patient in result.Data)
    {
        Patients.Add(patient); // 每次Add触发UI更新，性能差
    }
}
```

**问题分析**:
- 大数据量一次性加载导致UI响应慢
- ObservableCollection频繁更新影响性能
- 缺乏虚拟化和分页加载机制

## 🔧 解决方案设计

### 数据库性能优化方案

#### 1. 连接池配置优化

```csharp
public static class DatabaseConfiguration
{
    public static IServiceCollection AddOptimizedDatabase(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                // 查询超时优化
                sqlOptions.CommandTimeout(60);
                
                // 启用查询拆分以避免笛卡尔积
                sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                
                // 启用敏感数据日志 (仅开发环境)
                sqlOptions.EnableSensitiveDataLogging(
                    configuration.GetValue<bool>("Logging:EnableSensitiveDataLogging"));
            });

            // 启用查询跟踪和性能监控
            options.EnableServiceProviderCaching();
            options.EnableSensitiveDataLogging(false); // 生产环境关闭
            options.LogTo(Console.WriteLine, LogLevel.Information);
        });

        // 连接池监控服务
        services.AddSingleton<IConnectionPoolMonitor, ConnectionPoolMonitor>();
        
        return services;
    }
}

// 优化后的连接字符串
"DefaultConnection": "Server=localhost;Database=LYBTDB;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true;Max Pool Size=50;Min Pool Size=5;Pooling=true;Connection Timeout=30;Command Timeout=60"
```

#### 2. 查询性能优化

```csharp
// 优化后的查询服务基类
public abstract class BaseQueryService<TEntity, TDto>
{
    protected readonly AppDbContext _context;
    protected readonly IMapper _mapper;
    protected readonly ILogger _logger;
    protected readonly ICacheService _cache;

    protected virtual IQueryable<TEntity> ApplyIncludes(IQueryable<TEntity> query)
    {
        // 子类重写以添加必要的Include
        return query;
    }

    protected async Task<PagedResult<TDto>> GetPagedAsync<TSearch>(
        TSearch criteria, 
        Func<IQueryable<TEntity>, TSearch, IQueryable<TEntity>> applyFilter,
        string cachePrefix = null) where TSearch : BaseSearchCriteria
    {
        var cacheKey = $"{cachePrefix}_{criteria.GetHashCode()}";
        
        return await _cache.GetOrSetAsync(cacheKey, async () =>
        {
            var stopwatch = Stopwatch.StartNew();
            
            var query = _context.Set<TEntity>().AsNoTracking();
            query = ApplyIncludes(query);
            query = applyFilter(query, criteria);

            // 优化的总数统计：仅在需要时执行
            var totalCount = criteria.NeedTotalCount 
                ? await query.CountAsync() 
                : 0;

            var data = await query
                .Skip((criteria.Page - 1) * criteria.PageSize)
                .Take(criteria.PageSize)
                .ToListAsync();

            stopwatch.Stop();
            _logger.LogInformation($"查询执行时间: {stopwatch.ElapsedMilliseconds}ms, 记录数: {data.Count}");

            return new PagedResult<TDto>
            {
                Data = _mapper.Map<List<TDto>>(data),
                TotalCount = totalCount,
                Page = criteria.Page,
                PageSize = criteria.PageSize
            };
        }, TimeSpan.FromMinutes(5));
    }
}

// 患者查询服务优化实现
public class OptimizedPatientQueryService : BaseQueryService<Patient, PatientDto>, IPatientQueryService
{
    protected override IQueryable<Patient> ApplyIncludes(IQueryable<Patient> query)
    {
        return query.Include(p => p.MedicalCases)
                   .ThenInclude(mc => mc.Consultation);
    }

    public async Task<PagedResult<PatientDto>> SearchPatientsAsync(PatientSearchDto criteria)
    {
        return await GetPagedAsync(criteria, (query, search) =>
        {
            if (!string.IsNullOrEmpty(search.Name))
                query = query.Where(p => EF.Functions.Like(p.Name, $"%{search.Name}%"));
                
            if (!string.IsNullOrEmpty(search.Phone))
                query = query.Where(p => p.Phone.Contains(search.Phone));
                
            if (search.AgeMin.HasValue)
                query = query.Where(p => p.Age >= search.AgeMin.Value);
                
            return query.OrderByDescending(p => p.CreateTime);
        }, "patients_search");
    }
}
```

#### 3. 智能缓存系统

```csharp
public interface IEnhancedCacheService : ICacheService
{
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, CacheOptions options);
    Task InvalidatePatternAsync(string pattern);
    Task WarmUpAsync(string[] keys, Func<string, Task<object>>[] factories);
    CacheStatistics GetStatistics();
}

public class CacheOptions
{
    public TimeSpan Expiry { get; set; } = TimeSpan.FromMinutes(30);
    public CachePriority Priority { get; set; } = CachePriority.Normal;
    public bool SlidingExpiration { get; set; } = false;
    public string[] Tags { get; set; } = Array.Empty<string>();
    public bool PreventCacheStampede { get; set; } = true;
}

public class EnhancedCacheService : IEnhancedCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<EnhancedCacheService> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly CacheStatistics _statistics = new();

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, CacheOptions options)
    {
        // 记录缓存尝试
        _statistics.TotalRequests++;
        
        if (_cache.TryGetValue(key, out T cachedValue))
        {
            _statistics.Hits++;
            return cachedValue;
        }

        // 防止缓存击穿：同一时间只有一个线程执行工厂方法
        if (options.PreventCacheStampede)
        {
            await _semaphore.WaitAsync();
            try
            {
                // 再次检查缓存（可能其他线程已经填充）
                if (_cache.TryGetValue(key, out cachedValue))
                {
                    _statistics.Hits++;
                    return cachedValue;
                }

                // 执行工厂方法获取数据
                var value = await factory();
                SetCache(key, value, options);
                
                _statistics.Misses++;
                return value;
            }
            finally
            {
                _semaphore.Release();
            }
        }
        else
        {
            var value = await factory();
            SetCache(key, value, options);
            
            _statistics.Misses++;
            return value;
        }
    }

    private void SetCache<T>(string key, T value, CacheOptions options)
    {
        var memoryCacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = options.SlidingExpiration ? null : options.Expiry,
            SlidingExpiration = options.SlidingExpiration ? options.Expiry : null,
            Priority = options.Priority,
        };

        // 添加标签支持批量失效
        foreach (var tag in options.Tags)
        {
            memoryCacheOptions.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration
            {
                EvictionCallback = (key, value, reason, state) => 
                {
                    _logger.LogDebug($"缓存项被清除: {key}, 原因: {reason}");
                }
            });
        }

        _cache.Set(key, value, memoryCacheOptions);
    }

    public async Task WarmUpAsync(string[] keys, Func<string, Task<object>>[] factories)
    {
        var warmUpTasks = keys.Zip(factories, async (key, factory) =>
        {
            try
            {
                if (!_cache.TryGetValue(key, out _))
                {
                    var value = await factory(key);
                    _cache.Set(key, value, TimeSpan.FromHours(1));
                    _logger.LogInformation($"缓存预热成功: {key}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"缓存预热失败: {key}");
            }
        });

        await Task.WhenAll(warmUpTasks);
    }
}
```

### 前端性能优化方案

#### 1. 虚拟化数据绑定

```csharp
// 优化后的ViewModel基类
public abstract class BaseCollectionViewModel<T> : BaseViewModel
{
    private readonly ObservableCollection<T> _items = new();
    private readonly CollectionViewSource _collectionViewSource = new();
    
    public ICollectionView Items { get; private set; }
    
    protected BaseCollectionViewModel()
    {
        _collectionViewSource.Source = _items;
        Items = _collectionViewSource.View;
        
        // 启用虚拟化
        Items.GroupDescriptions.Add(new PropertyGroupDescription());
    }
    
    protected async Task LoadDataAsync<TSearch>(
        TSearch criteria, 
        Func<TSearch, Task<PagedResult<T>>> loadFunc,
        bool append = false)
    {
        try
        {
            IsBusy = true;
            
            var result = await loadFunc(criteria);
            
            if (!append)
            {
                _items.Clear();
            }
            
            // 批量添加以减少UI更新次数
            await Task.Run(() =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var item in result.Data)
                    {
                        _items.Add(item);
                    }
                });
            });
        }
        finally
        {
            IsBusy = false;
        }
    }
}

// 患者管理ViewModel优化
public class OptimizedPatientManagementViewModel : BaseCollectionViewModel<PatientDto>
{
    private readonly IPatientService _patientService;
    private PatientSearchDto _currentCriteria = new() { Page = 1, PageSize = 50 };
    
    public AsyncRelayCommand LoadMoreCommand { get; }
    public AsyncRelayCommand RefreshCommand { get; }
    
    protected override async Task OnInitializedAsync()
    {
        await LoadPatientsAsync();
        
        // 预加载下一页数据
        _ = Task.Run(async () =>
        {
            await Task.Delay(2000); // 延迟2秒预加载
            await PreloadNextPageAsync();
        });
    }
    
    private async Task LoadPatientsAsync(bool append = false)
    {
        await LoadDataAsync(_currentCriteria, 
            criteria => _patientService.SearchPatientsAsync(criteria), 
            append);
    }
    
    private async Task PreloadNextPageAsync()
    {
        var nextPageCriteria = _currentCriteria with { Page = _currentCriteria.Page + 1 };
        await _patientService.SearchPatientsAsync(nextPageCriteria); // 预加载到缓存
    }
}
```

## 📝 详细需求规格

### 功能需求

#### FR-001: 数据库连接池优化
- **连接池配置**: MaxPoolSize=50, MinPoolSize=5, 连接超时30秒
- **监控功能**: 连接池使用率监控，连接泄漏检测
- **告警机制**: 连接池使用率>80%时告警
- **自动调优**: 根据负载动态调整连接池参数

#### FR-002: 查询性能优化  
- **查询优化**: 消除N+1查询，优化Include策略
- **分页优化**: 智能总数统计，仅在需要时执行COUNT
- **索引建议**: 基于查询模式的索引优化建议
- **查询监控**: 慢查询检测和优化建议

#### FR-003: 智能缓存系统
- **多级缓存**: L1内存缓存 + L2分布式缓存预留接口
- **缓存策略**: 根据数据特性自动选择缓存策略
- **预热机制**: 系统启动时预加载热点数据
- **失效策略**: 支持按标签批量失效，防止缓存雪崩

#### FR-004: 前端性能优化
- **虚拟化**: 大数据量列表虚拟化显示
- **懒加载**: 图片和非关键数据懒加载
- **预加载**: 智能预加载下一页数据
- **防抖优化**: 搜索防抖，减少无效请求

### 非功能需求

#### NFR-001: 性能指标
- **API响应时间**: 平均 < 500ms, 95% < 1000ms
- **数据库查询**: 95% < 200ms, 99% < 500ms  
- **并发用户**: 支持50并发用户稳定运行
- **缓存命中率**: > 80%

#### NFR-002: 资源使用
- **内存使用**: 缓存占用内存 < 500MB
- **CPU使用**: 正常负载下 < 30%
- **数据库连接**: 峰值使用率 < 80%
- **网络带宽**: 优化传输数据量，减少50%网络开销

#### NFR-003: 扩展性要求
- **水平扩展**: 支持多实例部署
- **缓存扩展**: 支持Redis等分布式缓存接入
- **数据库扩展**: 支持读写分离预留接口
- **CDN支持**: 静态资源CDN部署支持

## 🔧 技术实现

### 开发任务分解

#### 任务1: 数据库性能优化 (10天)
- [ ] 优化连接池配置和监控
- [ ] 实现查询性能基类和监控
- [ ] 添加慢查询日志和分析
- [ ] 创建数据库性能测试套件

**交付物**:
- `DatabaseConfiguration.cs` - 优化的数据库配置
- `BaseQueryService.cs` - 查询服务基类  
- `QueryPerformanceMonitor.cs` - 查询性能监控
- `ConnectionPoolMonitor.cs` - 连接池监控

#### 任务2: 智能缓存系统 (8天)
- [ ] 实现增强缓存服务
- [ ] 添加缓存统计和监控
- [ ] 实现缓存预热机制
- [ ] 创建缓存管理界面

**交付物**:
- `EnhancedCacheService.cs` - 智能缓存服务
- `CacheStatistics.cs` - 缓存统计
- `CacheWarmUpService.cs` - 缓存预热服务
- 缓存监控API端点

#### 任务3: 前端性能优化 (7天)
- [ ] 实现虚拟化数据绑定基类
- [ ] 优化关键ViewModel性能
- [ ] 添加懒加载和预加载机制
- [ ] 实现前端性能监控

**交付物**:
- `BaseCollectionViewModel.cs` - 优化的集合ViewModel
- `VirtualizationBehavior.cs` - 虚拟化行为
- `LazyLoadingService.cs` - 懒加载服务
- 前端性能监控组件

### 关键实现代码

#### 连接池监控实现
```csharp
public class ConnectionPoolMonitor : IConnectionPoolMonitor, IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ConnectionPoolMonitor> _logger;
    private Timer _monitoringTimer;
    private readonly ConnectionPoolStatistics _statistics = new();

    public async Task<ConnectionPoolStatistics> GetStatisticsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        try
        {
            // 测试连接池状态
            var stopwatch = Stopwatch.StartNew();
            await context.Database.OpenConnectionAsync();
            await context.Database.CloseConnectionAsync();
            stopwatch.Stop();
            
            _statistics.LastConnectionTime = stopwatch.Elapsed;
            _statistics.LastCheckTime = DateTime.UtcNow;
            _statistics.IsHealthy = stopwatch.ElapsedMilliseconds < 1000;
            
            return _statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "连接池健康检查失败");
            _statistics.IsHealthy = false;
            _statistics.LastError = ex.Message;
            return _statistics;
        }
    }
    
    private async void MonitorConnectionPool(object state)
    {
        var stats = await GetStatisticsAsync();
        
        // 性能告警检查
        if (stats.LastConnectionTime.TotalMilliseconds > 500)
        {
            _logger.LogWarning($"数据库连接缓慢: {stats.LastConnectionTime.TotalMilliseconds}ms");
        }
        
        if (!stats.IsHealthy)
        {
            _logger.LogError($"连接池健康检查失败: {stats.LastError}");
        }
    }
}
```

#### 查询性能监控实现
```csharp
public class QueryPerformanceInterceptor : DbCommandInterceptor
{
    private readonly ILogger<QueryPerformanceInterceptor> _logger;
    private readonly IMetricsCollector _metrics;

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        eventData.Context.Items["QueryStartTime"] = stopwatch;
        
        return await base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override async ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context.Items.TryGetValue("QueryStartTime", out var stopwatchObj) 
            && stopwatchObj is Stopwatch stopwatch)
        {
            stopwatch.Stop();
            var executionTime = stopwatch.ElapsedMilliseconds;
            
            // 记录查询性能指标
            _metrics.RecordQueryExecutionTime(command.CommandText, executionTime);
            
            // 慢查询告警
            if (executionTime > 1000)
            {
                _logger.LogWarning($"慢查询检测: {executionTime}ms - {command.CommandText}");
            }
        }
        
        return await base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }
}
```

## 🧪 测试策略

### 性能测试

#### 负载测试场景
- [ ] **并发用户测试**: 模拟50个并发用户操作
- [ ] **数据量测试**: 10万+患者记录的查询性能
- [ ] **长时间运行**: 24小时稳定性测试
- [ ] **内存泄漏测试**: 长期运行内存使用监控

#### 基准测试
```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class PatientQueryBenchmark
{
    private IPatientQueryService _optimizedService;
    private IPatientQueryService _originalService;
    
    [Benchmark(Baseline = true)]
    public async Task<PagedResult<PatientDto>> OriginalQuery()
    {
        return await _originalService.SearchPatientsAsync(new PatientSearchDto 
        { 
            Name = "张", 
            Page = 1, 
            PageSize = 20 
        });
    }
    
    [Benchmark]
    public async Task<PagedResult<PatientDto>> OptimizedQuery()
    {
        return await _optimizedService.SearchPatientsAsync(new PatientSearchDto 
        { 
            Name = "张", 
            Page = 1, 
            PageSize = 20 
        });
    }
}
```

### 缓存测试

#### 缓存效果验证
- [ ] **命中率测试**: 验证缓存命中率达到预期
- [ ] **失效测试**: 验证缓存失效机制正确性
- [ ] **并发测试**: 多线程并发访问缓存的正确性
- [ ] **内存使用**: 缓存占用内存控制在预期范围

## 📊 验收标准

### 性能验收指标
- [ ] **响应时间改善**: API平均响应时间减少60%以上
- [ ] **并发能力提升**: 支持并发用户数提升100%以上  
- [ ] **缓存效果**: 缓存命中率达到80%以上
- [ ] **资源使用优化**: 数据库连接池使用率稳定在70%以下

### 稳定性验收
- [ ] **24小时稳定运行**: 无内存泄漏，无性能衰减
- [ ] **压力测试通过**: 峰值负载下系统稳定运行
- [ ] **监控数据完整**: 所有性能指标正常记录和展示
- [ ] **告警机制有效**: 性能异常时及时告警

### 用户体验验收
- [ ] **界面响应流畅**: 大数据量列表滚动流畅
- [ ] **搜索体验良好**: 搜索结果快速返回，无卡顿
- [ ] **数据加载体验**: 分页和懒加载无感知切换
- [ ] **系统稳定性**: 长时间使用无明显性能下降

## 🚀 部署和监控

### 部署策略
1. **Phase 1**: 数据库性能优化和监控部署
2. **Phase 2**: 缓存系统部署和预热
3. **Phase 3**: 前端性能优化部署
4. **Phase 4**: 性能监控和告警配置

### 性能监控指标
- **响应时间统计**: API响应时间分布和趋势
- **数据库性能**: 查询执行时间，连接池使用率
- **缓存性能**: 命中率，失效率，内存使用
- **系统资源**: CPU, 内存，磁盘IO使用情况

### 告警配置
- API响应时间 > 2秒持续1分钟
- 数据库连接池使用率 > 80%持续5分钟
- 缓存命中率 < 60%持续10分钟  
- 系统内存使用率 > 85%持续5分钟

---

## 📞 项目信息

**需求负责人**: Senior .NET Architecture Analyst  
**开发预估**: 25工作日  
**性能测试**: 5工作日  
**发布时间**: Phase 2 实施期  
**风险等级**: 🔴 → 🟡 (性能瓶颈显著缓解)

**依赖项目**: 无强依赖，建议在PRD-001和PRD-002完成后实施以避免性能测试干扰
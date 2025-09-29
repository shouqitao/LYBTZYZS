# Issue #805: 实现响应缓存和输出缓存策略

## 📋 问题描述
系统缺乏有效的缓存策略，导致：
- 重复查询静态数据（草药列表、配方模板）
- 未启用响应缓存，增加服务器负载
- 缺少输出缓存配置
- 内存缓存使用单一，未分层

## 🎯 优化目标
- 静态数据缓存命中率达到90%
- API响应时间降低40%
- 减少数据库查询50%
- 内存使用优化20%

## 📁 涉及文件和具体修改

### 1. Program.cs - 启用缓存中间件
**文件路径**: `src/Server/Services/LYBT.WebAPI/Program.cs`

#### 添加响应缓存和输出缓存
```csharp
// 修改前：缺少缓存配置

// 修改后：完整缓存配置
var builder = WebApplication.CreateBuilder(args);

// 响应缓存
builder.Services.AddResponseCaching(options =>
{
    options.MaximumBodySize = 100_000_000;  // 100MB
    options.UseCaseSensitivePaths = false;
});

// 输出缓存（.NET 7+）
builder.Services.AddOutputCache(options =>
{
    // 默认策略
    options.AddBasePolicy(builder =>
        builder.Expire(TimeSpan.FromMinutes(5)));

    // 草药数据缓存1小时
    options.AddPolicy("HerbsCache", builder =>
        builder.Expire(TimeSpan.FromHours(1))
               .Tag("herbs"));

    // 配方模板缓存2小时
    options.AddPolicy("FormulasCache", builder =>
        builder.Expire(TimeSpan.FromHours(2))
               .Tag("formulas"));

    // 用户权限缓存10分钟
    options.AddPolicy("UserPermissionsCache", builder =>
        builder.Expire(TimeSpan.FromMinutes(10))
               .Tag("permissions")
               .VaryByValue(ctx =>
                   ctx.User.Identity?.Name ?? "anonymous"));
});

var app = builder.Build();

// 中间件顺序很重要
app.UseResponseCaching();  // 在路由之前
app.UseOutputCache();      // 在认证之前
```

### 2. HerbsController.cs
**文件路径**: `src/Server/Services/LYBT.WebAPI/Controllers/HerbsController.cs`

#### 添加缓存标记
```csharp
// 修改前
[HttpGet]
public async Task<IActionResult> GetAllHerbs()
{
    var herbs = await _herbService.GetAllAsync();
    return Ok(herbs);
}

// 修改后
[HttpGet]
[ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
[OutputCache(PolicyName = "HerbsCache")]
public async Task<IActionResult> GetAllHerbs(
    CancellationToken cancellationToken = default)
{
    var herbs = await _herbService.GetAllAsync(cancellationToken);

    // 添加缓存相关头
    Response.Headers.Add("X-Cache-Tag", "herbs");
    Response.Headers.Add("X-Cache-Duration", "3600");

    return Ok(herbs);
}

[HttpGet("{id}")]
[ResponseCache(Duration = 600, VaryByQueryKeys = new[] { "id" })]
public async Task<IActionResult> GetHerbById(
    int id,
    CancellationToken cancellationToken = default)
{
    var herb = await _herbService.GetByIdAsync(id, cancellationToken);
    if (herb == null)
        return NotFound();

    return Ok(herb);
}
```

### 3. FormulasController.cs
**文件路径**: `src/Server/Services/LYBT.WebAPI/Controllers/FormulasController.cs`

```csharp
// 修改后：添加输出缓存
[HttpGet("templates")]
[OutputCache(PolicyName = "FormulasCache")]
public async Task<IActionResult> GetFormulaTemplates(
    CancellationToken cancellationToken = default)
{
    var templates = await _formulaService.GetTemplatesAsync(cancellationToken);
    return Ok(templates);
}

[HttpGet("common")]
[ResponseCache(Duration = 7200, Location = ResponseCacheLocation.Client)]
[OutputCache(Duration = 7200)]
public async Task<IActionResult> GetCommonFormulas(
    CancellationToken cancellationToken = default)
{
    var formulas = await _formulaService.GetCommonFormulasAsync(cancellationToken);
    return Ok(formulas);
}
```

### 4. UsersController.cs
**文件路径**: `src/Server/Services/LYBT.WebAPI/Controllers/UsersController.cs`

```csharp
// 用户权限缓存
[HttpGet("{userId}/permissions")]
[OutputCache(PolicyName = "UserPermissionsCache")]
public async Task<IActionResult> GetUserPermissions(
    int userId,
    CancellationToken cancellationToken = default)
{
    var permissions = await _userService.GetPermissionsAsync(userId, cancellationToken);
    return Ok(permissions);
}

// 不缓存敏感数据
[HttpGet("current")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public async Task<IActionResult> GetCurrentUser()
{
    // 用户个人信息不缓存
    var user = await _userService.GetCurrentUserAsync();
    return Ok(user);
}
```

### 5. CacheService.cs - 实现分层缓存
**文件路径**: `src/Server/Core/LYBT.Infrastructure/Services/CacheService.cs`

```csharp
// 新建缓存服务
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default);
}

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly Dictionary<string, HashSet<string>> _taggedKeys;

    public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
        _taggedKeys = new Dictionary<string, HashSet<string>>();
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out T? value))
        {
            _logger.LogDebug("缓存命中: {Key}", key);
            return Task.FromResult(value);
        }

        _logger.LogDebug("缓存未命中: {Key}", key);
        return Task.FromResult(default(T));
    }

    public Task SetAsync<T>(string key, T value, TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        var options = new MemoryCacheEntryOptions
        {
            SlidingExpiration = expiration,
            Priority = CacheItemPriority.Normal
        };

        _cache.Set(key, value, options);
        _logger.LogDebug("缓存设置: {Key}, 过期时间: {Expiration}", key, expiration);

        return Task.CompletedTask;
    }

    public Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        if (_taggedKeys.TryGetValue(tag, out var keys))
        {
            foreach (var key in keys)
            {
                _cache.Remove(key);
            }
            _taggedKeys.Remove(tag);
            _logger.LogInformation("清除标签 {Tag} 的 {Count} 个缓存项", tag, keys.Count);
        }

        return Task.CompletedTask;
    }
}
```

### 6. HerbService.cs - 使用缓存
**文件路径**: `src/Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs`

```csharp
// 修改前
public async Task<List<HerbDto>> GetAllAsync()
{
    var herbs = await _repository.GetAllAsync();
    return _mapper.Map<List<HerbDto>>(herbs);
}

// 修改后
public async Task<List<HerbDto>> GetAllAsync(CancellationToken cancellationToken = default)
{
    const string cacheKey = "herbs:all";

    // 尝试从缓存获取
    var cached = await _cache.GetAsync<List<HerbDto>>(cacheKey, cancellationToken);
    if (cached != null)
    {
        return cached;
    }

    // 从数据库获取
    var herbs = await _repository.GetAllAsync(cancellationToken);
    var dto = _mapper.Map<List<HerbDto>>(herbs);

    // 写入缓存
    await _cache.SetAsync(cacheKey, dto, TimeSpan.FromHours(1), cancellationToken);

    return dto;
}

// 更新时清除缓存
public async Task<HerbDto> UpdateAsync(int id, UpdateHerbDto dto,
    CancellationToken cancellationToken = default)
{
    var result = await _repository.UpdateAsync(id, dto, cancellationToken);

    // 清除相关缓存
    await _cache.RemoveAsync("herbs:all", cancellationToken);
    await _cache.RemoveAsync($"herbs:{id}", cancellationToken);

    return result;
}
```

### 7. 缓存预热
**文件路径**: `src/Server/Services/LYBT.WebAPI/Services/CacheWarmupService.cs`

```csharp
public class CacheWarmupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CacheWarmupService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 等待应用启动
        await Task.Delay(5000, stoppingToken);

        using var scope = _serviceProvider.CreateScope();
        var herbService = scope.ServiceProvider.GetRequiredService<IHerbService>();
        var formulaService = scope.ServiceProvider.GetRequiredService<IFormulaService>();

        _logger.LogInformation("开始缓存预热...");

        try
        {
            // 预加载常用数据
            var tasks = new[]
            {
                herbService.GetAllAsync(stoppingToken),
                formulaService.GetTemplatesAsync(stoppingToken),
                formulaService.GetCommonFormulasAsync(stoppingToken)
            };

            await Task.WhenAll(tasks);
            _logger.LogInformation("缓存预热完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "缓存预热失败");
        }
    }
}

// 在Program.cs注册
builder.Services.AddHostedService<CacheWarmupService>();
```

### 8. 缓存监控端点
**文件路径**: `src/Server/Services/LYBT.WebAPI/Controllers/CacheController.cs`

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class CacheController : ControllerBase
{
    private readonly ICacheService _cache;
    private readonly IOutputCacheStore _outputCache;

    [HttpPost("clear/{tag}")]
    public async Task<IActionResult> ClearByTag(
        string tag,
        CancellationToken cancellationToken = default)
    {
        await _cache.RemoveByTagAsync(tag, cancellationToken);
        await _outputCache.EvictByTagAsync(tag, cancellationToken);

        return Ok(new { message = $"已清除标签 {tag} 的缓存" });
    }

    [HttpGet("stats")]
    public IActionResult GetCacheStats()
    {
        // 返回缓存统计信息
        return Ok(new
        {
            memoryCache = new
            {
                count = _cache.GetCurrentStatistics()?.CurrentEntryCount ?? 0,
                sizeBytes = _cache.GetCurrentStatistics()?.CurrentEstimatedSize ?? 0
            },
            outputCache = new
            {
                // 输出缓存统计
            }
        });
    }
}
```

## ✅ 验收标准
1. 响应缓存和输出缓存配置完成
2. 控制器方法添加适当的缓存标记
3. 实现分层缓存服务
4. 缓存预热机制工作正常
5. 缓存失效策略正确
6. 性能测试显示响应时间降低40%

## 🔧 实施步骤
1. [ ] 配置响应缓存和输出缓存中间件
2. [ ] 为控制器方法添加缓存属性
3. [ ] 实现ICacheService接口
4. [ ] 在Service层集成缓存
5. [ ] 实现缓存预热服务
6. [ ] 添加缓存管理端点
7. [ ] 性能测试验证

## 📊 预期效果
- 静态数据查询：100次/分钟 → 10次/分钟
- API平均响应：150ms → 90ms
- 内存使用：+20MB（缓存数据）
- 数据库负载：降低50%

## 🏷️ 标签
`performance` `caching` `optimization` `mvp`

## 📎 相关文档
- [Response Caching in ASP.NET Core](https://docs.microsoft.com/aspnet/core/performance/caching/response)
- [Output Caching in .NET 7](https://docs.microsoft.com/aspnet/core/performance/caching/output)

---
**优先级**: P1（高）
**预估工时**: 1天
**负责人**: 待分配
**状态**: 待开始
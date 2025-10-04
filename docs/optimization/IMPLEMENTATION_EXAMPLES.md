# Server端优化实施示例代码

## 1. EF Core查询优化示例

### 优化PatientRepository

```csharp
// src/Server/Modules/LYBT.Module.Patients/Repositories/PatientRepository.cs

public class OptimizedPatientRepository : IPatientRepository
{
    private readonly AppDbContext _context;

    // 优化：只读查询使用AsNoTracking
    public async Task<List<PatientDto>> GetPatientsAsync(int pageNumber, int pageSize)
    {
        return await _context.Patients
            .AsNoTracking()  // 不跟踪实体变化
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PatientDto  // 投影，只查询需要的字段
            {
                Id = p.Id,
                Name = p.Name,
                Gender = p.Gender,
                Age = p.Age,
                Phone = p.Phone,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();
    }

    // 优化：预加载关联数据，避免N+1查询
    public async Task<PatientDetailDto> GetPatientWithConsultationsAsync(int patientId)
    {
        return await _context.Patients
            .AsNoTracking()
            .Include(p => p.Consultations)
                .ThenInclude(c => c.Prescriptions)
            .Where(p => p.Id == patientId)
            .Select(p => new PatientDetailDto
            {
                Id = p.Id,
                Name = p.Name,
                // 使用投影避免加载整个实体
                RecentConsultations = p.Consultations
                    .OrderByDescending(c => c.ConsultationDate)
                    .Take(5)
                    .Select(c => new ConsultationSummaryDto
                    {
                        Id = c.Id,
                        Date = c.ConsultationDate,
                        ChiefComplaint = c.ChiefComplaint
                    }).ToList()
            })
            .FirstOrDefaultAsync();
    }

    // 优化：批量更新使用ExecuteUpdateAsync
    public async Task<int> UpdatePatientsStatusAsync(List<int> patientIds, string newStatus)
    {
        return await _context.Patients
            .Where(p => patientIds.Contains(p.Id))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.Status, newStatus)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow));
    }

    // 优化：使用编译查询提升频繁查询性能
    private static readonly Func<AppDbContext, string, Task<Patient>> _getPatientByPhone =
        EF.CompileAsyncQuery((AppDbContext context, string phone) =>
            context.Patients.AsNoTracking()
                .FirstOrDefault(p => p.Phone == phone));

    public Task<Patient> GetPatientByPhoneAsync(string phone)
    {
        return _getPatientByPhone(_context, phone);
    }
}
```

## 2. 异步编程优化示例

### 优化ConsultationService

```csharp
// src/Server/Modules/LYBT.Module.Consultation/Services/ConsultationService.cs

public class OptimizedConsultationService : IConsultationService
{
    private readonly IConsultationRepository _consultationRepo;
    private readonly IPatientRepository _patientRepo;
    private readonly IPrescriptionRepository _prescriptionRepo;
    private readonly ICacheService _cache;

    // 优化：并行执行独立的异步操作
    public async Task<ConsultationDetailsDto> GetConsultationDetailsAsync(int consultationId)
    {
        // 并行获取所有需要的数据
        var tasks = new Task<object>[]
        {
            GetConsultationAsync(consultationId).ContinueWith(t => (object)t.Result),
            GetPatientHistoryAsync(consultationId).ContinueWith(t => (object)t.Result),
            GetPrescriptionsAsync(consultationId).ContinueWith(t => (object)t.Result)
        };

        var results = await Task.WhenAll(tasks);

        return new ConsultationDetailsDto
        {
            Consultation = (ConsultationDto)results[0],
            PatientHistory = (PatientHistoryDto)results[1],
            Prescriptions = (List<PrescriptionDto>)results[2]
        };
    }

    // 优化：使用ValueTask减少分配（热路径优化）
    public ValueTask<bool> ValidateConsultationAsync(int consultationId)
    {
        // 先检查缓存
        if (_cache.TryGetValue($"consultation_valid_{consultationId}", out bool isValid))
        {
            return new ValueTask<bool>(isValid);  // 同步返回，无分配
        }

        // 缓存未命中，执行异步验证
        return new ValueTask<bool>(ValidateFromDatabaseAsync(consultationId));
    }

    // 优化：使用IAsyncEnumerable处理大数据集
    public async IAsyncEnumerable<ConsultationExportDto> ExportConsultationsAsync(
        DateTime startDate,
        DateTime endDate,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var pageSize = 100;
        var pageNumber = 1;

        while (!cancellationToken.IsCancellationRequested)
        {
            var consultations = await _consultationRepo.GetPagedAsync(
                startDate, endDate, pageNumber, pageSize, cancellationToken);

            if (!consultations.Any())
                yield break;

            foreach (var consultation in consultations)
            {
                yield return MapToExportDto(consultation);
            }

            pageNumber++;
        }
    }

    // 优化：正确使用ConfigureAwait
    private async Task<bool> ValidateFromDatabaseAsync(int consultationId)
    {
        var consultation = await _consultationRepo
            .GetByIdAsync(consultationId)
            .ConfigureAwait(false);  // 库代码不需要捕获上下文

        var isValid = consultation != null && consultation.Status == "Active";

        // 更新缓存
        await _cache.SetAsync($"consultation_valid_{consultationId}", isValid,
            TimeSpan.FromMinutes(5)).ConfigureAwait(false);

        return isValid;
    }
}
```

## 3. 缓存策略优化示例

### 实现多层缓存策略

```csharp
// src/Server/Core/LYBT.Infrastructure/Caching/OptimizedCacheService.cs

public class OptimizedCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;  // 为未来扩展准备
    private readonly ILogger<OptimizedCacheService> _logger;

    // 响应缓存属性
    public class CacheableAttribute : ActionFilterAttribute
    {
        private readonly int _duration;
        private readonly string _varyByQuery;

        public CacheableAttribute(int duration = 300, string varyByQuery = "")
        {
            _duration = duration;
            _varyByQuery = varyByQuery;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var cacheKey = GenerateCacheKey(context);
            var cache = context.HttpContext.RequestServices.GetService<ICacheService>();

            if (cache.TryGetValue(cacheKey, out var cachedResult))
            {
                context.Result = new OkObjectResult(cachedResult);
                context.HttpContext.Response.Headers.Add("X-Cache", "HIT");
                return;
            }

            context.HttpContext.Items["CacheKey"] = cacheKey;
            context.HttpContext.Items["CacheDuration"] = _duration;
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Result is OkObjectResult okResult &&
                context.HttpContext.Items["CacheKey"] is string cacheKey)
            {
                var cache = context.HttpContext.RequestServices.GetService<ICacheService>();
                var duration = (int)context.HttpContext.Items["CacheDuration"];

                cache.Set(cacheKey, okResult.Value, TimeSpan.FromSeconds(duration));
                context.HttpContext.Response.Headers.Add("X-Cache", "MISS");
            }
        }
    }

    // 缓存预热
    public async Task WarmupCacheAsync()
    {
        var warmupTasks = new List<Task>
        {
            CacheHerbsAsync(),
            CacheFormulasAsync(),
            CacheCommonPatientsAsync()
        };

        await Task.WhenAll(warmupTasks);
        _logger.LogInformation("缓存预热完成");
    }

    // 缓存失效策略
    public async Task InvalidateAsync(string tag)
    {
        var keysToRemove = _taggedKeys.Where(k => k.Value.Contains(tag))
                                      .Select(k => k.Key)
                                      .ToList();

        foreach (var key in keysToRemove)
        {
            _memoryCache.Remove(key);
        }

        _logger.LogInformation($"已失效标签 {tag} 的 {keysToRemove.Count} 个缓存项");
    }
}
```

## 4. 中间件优化示例

### 优化的中间件管道配置

```csharp
// src/Server/Services/LYBT.WebAPI/Program.cs

var builder = WebApplication.CreateBuilder(args);

// 配置服务
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(builder =>
        builder.Expire(TimeSpan.FromSeconds(60)));

    options.AddPolicy("StaticData", builder =>
        builder.Expire(TimeSpan.FromHours(24))
               .Tag("static"));
});

var app = builder.Build();

// 优化的中间件顺序
app.UseExceptionHandler("/error");  // 1. 异常处理最外层

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();  // 2. 安全头
}

app.UseHttpsRedirection();  // 3. HTTPS重定向
app.UseResponseCompression();  // 4. 响应压缩
app.UseStaticFiles();  // 5. 静态文件

app.UseRouting();  // 6. 路由

app.UseRateLimiter();  // 7. 速率限制
app.UseResponseCaching();  // 8. 响应缓存
app.UseOutputCache();  // 9. 输出缓存

app.UseAuthentication();  // 10. 认证
app.UseAuthorization();  // 11. 授权

app.MapControllers();  // 12. 控制器端点

// 性能监控中间件
app.Use(async (context, next) =>
{
    var stopwatch = Stopwatch.StartNew();

    context.Response.OnStarting(() =>
    {
        context.Response.Headers.Add("X-Response-Time",
            $"{stopwatch.ElapsedMilliseconds}ms");
        return Task.CompletedTask;
    });

    await next();
});

app.Run();
```

## 5. 控制器优化示例

### 优化HerbsController

```csharp
// src/Server/Services/LYBT.WebAPI/Controllers/HerbsController.cs

[ApiController]
[Route("api/[controller]")]
public class OptimizedHerbsController : ControllerBase
{
    private readonly IHerbService _herbService;
    private readonly ILogger<OptimizedHerbsController> _logger;

    // 响应缓存 + 输出缓存
    [HttpGet]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    [OutputCache(PolicyName = "StaticData")]
    public async Task<IActionResult> GetAllHerbs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        // 参数验证
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 50;

        var herbs = await _herbService.GetPagedHerbsAsync(page, pageSize, cancellationToken);

        // 添加分页头
        Response.Headers.Add("X-Pagination-Page", page.ToString());
        Response.Headers.Add("X-Pagination-PageSize", pageSize.ToString());

        return Ok(herbs);
    }

    // 批量操作端点
    [HttpPost("batch")]
    [ProducesResponseType(typeof(BatchResult), 200)]
    public async Task<IActionResult> BatchCreateHerbs(
        [FromBody] List<CreateHerbDto> herbs,
        CancellationToken cancellationToken = default)
    {
        if (herbs == null || !herbs.Any())
            return BadRequest("No herbs provided");

        if (herbs.Count > 100)
            return BadRequest("Maximum 100 herbs per batch");

        var result = await _herbService.BatchCreateAsync(herbs, cancellationToken);

        return Ok(new BatchResult
        {
            Succeeded = result.SuccessCount,
            Failed = result.FailureCount,
            Errors = result.Errors
        });
    }

    // 使用IAsyncEnumerable流式返回
    [HttpGet("export")]
    public async IAsyncEnumerable<HerbExportDto> ExportHerbs(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var herb in _herbService.ExportAllHerbsAsync(cancellationToken))
        {
            yield return herb;
        }
    }
}
```

## 6. 启动性能优化

### 优化Program.cs启动配置

```csharp
// src/Server/Services/LYBT.WebAPI/Program.cs

// 使用源生成器提升启动性能
[assembly: System.Runtime.CompilerServices.ModuleInitializer]
internal static class ModuleInitializer
{
    public static void Initialize()
    {
        // 预JIT关键路径
        RuntimeHelpers.PrepareMethod(typeof(PatientService).GetMethod("GetPatientsAsync").MethodHandle);
    }
}

var builder = WebApplication.CreateBuilder(args);

// 启用服务验证（开发环境）
if (builder.Environment.IsDevelopment())
{
    builder.Host.UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    });
}

// 配置JSON序列化（源生成器）
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

// 优化的数据库连接配置
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(3);
        sqlOptions.CommandTimeout(30);
        // 启用查询拆分以提升性能
        sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    });

    // 生产环境禁用敏感日志
    if (!builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging(false);
    }

    // 启用服务提供程序缓存
    options.EnableServiceProviderCaching();

    // 启用线程安全
    options.EnableThreadSafetyChecks();
});

var app = builder.Build();

// 预热关键服务
if (!app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
        await cacheService.WarmupCacheAsync();
    }
}

app.Run();
```

---

这些示例代码可以直接应用到LYBT项目中，每个优化点都有具体的实施路径和预期效果。建议按照优先级逐步实施并验证效果。
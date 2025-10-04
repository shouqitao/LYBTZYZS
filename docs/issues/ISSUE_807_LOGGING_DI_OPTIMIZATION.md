# Issue #807: 优化日志配置和依赖注入生命周期

## 📋 问题描述
系统存在以下问题：
- 日志级别设置不当，产生大量无用日志
- 依赖注入生命周期混乱，导致内存泄漏
- 缺少结构化日志
- Service定位器反模式使用
- 启动时间过长

## 🎯 优化目标
- 减少日志I/O开销50%
- 正确配置服务生命周期
- 启动时间优化30%
- 消除内存泄漏

## 📁 涉及文件和具体修改

### 1. 日志配置优化
**文件路径**: `src/Server/Services/LYBT.WebAPI/appsettings.json`

#### 调整日志级别
```json
// appsettings.Development.json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Information",
        "Microsoft.EntityFrameworkCore": "Warning",
        "Microsoft.AspNetCore": "Information"
      }
    }
  }
}

// appsettings.Production.json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Warning",  // 生产环境提高级别
      "Override": {
        "Microsoft": "Error",
        "Microsoft.EntityFrameworkCore": "Error",
        "Microsoft.AspNetCore": "Warning",
        "LYBT": "Information"  // 业务日志保持Information
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/log-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7,  // 只保留7天
          "fileSizeLimitBytes": 10485760,  // 10MB
          "buffered": true,  // 缓冲写入
          "flushToDiskInterval": "00:00:05"  // 5秒刷新
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  }
}
```

### 2. Program.cs - Serilog配置
**文件路径**: `src/Server/Services/LYBT.WebAPI/Program.cs`

```csharp
// 修改前：基础日志配置

// 修改后：优化的结构化日志
var builder = WebApplication.CreateBuilder(args);

// 配置Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "LYBT.WebAPI")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .CreateLogger();

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .WriteTo.Conditional(
        // 仅在生产环境写入文件
        condition: _ => !context.HostingEnvironment.IsDevelopment(),
        configureSink: writeTo => writeTo.File(
            path: "logs/log-.txt",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"))
    .WriteTo.Conditional(
        // 开发环境输出到控制台
        condition: _ => context.HostingEnvironment.IsDevelopment(),
        configureSink: writeTo => writeTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")));

try
{
    Log.Information("应用程序启动中...");
    var app = builder.Build();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "应用程序启动失败");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
```

### 3. 依赖注入生命周期优化
**文件路径**: `src/Server/Services/LYBT.WebAPI/Extensions/ServiceCollectionExtensions.cs`

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        // Repository层 - Scoped（与DbContext生命周期一致）
        services.Scan(scan => scan
            .FromAssemblyOf<PatientRepository>()
            .AddClasses(classes => classes.Where(type => type.Name.EndsWith("Repository")))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        // Service层 - Scoped（有状态）
        services.Scan(scan => scan
            .FromAssemblyOf<PatientService>()
            .AddClasses(classes => classes.Where(type => type.Name.EndsWith("Service")))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        // Validator - Transient（无状态）
        services.Scan(scan => scan
            .FromAssemblyOf<PatientValidator>()
            .AddClasses(classes => classes.Where(type => type.Name.EndsWith("Validator")))
            .AsImplementedInterfaces()
            .WithTransientLifetime());

        // 工具类 - Singleton（无状态且线程安全）
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        return services;
    }

    public static IServiceCollection AddOptimizedDbContext(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(3);
                sqlOptions.CommandTimeout(30);
                // 使用查询拆分提升性能
                sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });

            // 启用服务提供程序缓存
            options.EnableServiceProviderCaching();

            // 启用线程安全检查
            options.EnableThreadSafetyChecks();

            // 生产环境禁用敏感数据日志
            if (!services.BuildServiceProvider()
                .GetRequiredService<IWebHostEnvironment>()
                .IsDevelopment())
            {
                options.EnableSensitiveDataLogging(false);
            }
        });

        // 注册DbContext工厂（用于后台任务）
        services.AddDbContextFactory<AppDbContext>(lifetime: ServiceLifetime.Scoped);

        return services;
    }
}
```

### 4. 消除Service Locator反模式
**文件路径**: `src/Server/Modules/LYBT.Module.Users/Services/UserService.cs`

```csharp
// 修改前：Service Locator反模式
public class UserService : IUserService
{
    private readonly IServiceProvider _serviceProvider;

    public UserService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<User> CreateUserAsync(CreateUserDto dto)
    {
        // 错误：运行时解析依赖
        var repository = _serviceProvider.GetRequiredService<IUserRepository>();
        var validator = _serviceProvider.GetRequiredService<IUserValidator>();
        // ...
    }
}

// 修改后：构造函数注入
public class UserService : IUserService
{
    private readonly IUserRepository _repository;
    private readonly IUserValidator _validator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository repository,
        IUserValidator validator,
        IPasswordHasher passwordHasher,
        ILogger<UserService> logger)
    {
        _repository = repository;
        _validator = validator;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<User> CreateUserAsync(CreateUserDto dto)
    {
        // 直接使用注入的依赖
        var validationResult = await _validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("用户创建验证失败: {@Errors}", validationResult.Errors);
            throw new ValidationException(validationResult.Errors);
        }

        var hashedPassword = _passwordHasher.Hash(dto.Password);
        // ...
    }
}
```

### 5. 优化启动性能
**文件路径**: `src/Server/Services/LYBT.WebAPI/Program.cs`

```csharp
// 使用源生成器提升启动性能
[JsonSerializable(typeof(PatientDto))]
[JsonSerializable(typeof(ConsultationDto))]
[JsonSerializable(typeof(PrescriptionDto))]
[JsonSerializable(typeof(HerbDto))]
[JsonSerializable(typeof(ErrorResponse))]
internal partial class AppJsonSerializerContext : JsonSerializerContext { }

var builder = WebApplication.CreateBuilder(args);

// 配置JSON序列化使用源生成器
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

// 启用编译时验证
if (builder.Environment.IsDevelopment())
{
    builder.Host.UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;  // 构建时验证DI配置
    });
}

// 延迟加载非关键服务
builder.Services.AddHostedService<DeferredInitializationService>();
```

### 6. 结构化日志示例
**文件路径**: `src/Server/Modules/LYBT.Module.Patients/Services/PatientService.cs`

```csharp
public class PatientService : IPatientService
{
    private readonly ILogger<PatientService> _logger;

    // 修改前：字符串插值日志
    public async Task<Patient> GetPatientAsync(int id)
    {
        _logger.LogInformation($"获取患者 {id}");
        // ...
    }

    // 修改后：结构化日志
    public async Task<Patient> GetPatientAsync(int id)
    {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["PatientId"] = id,
            ["Operation"] = "GetPatient"
        }))
        {
            _logger.LogInformation("开始获取患者信息");

            try
            {
                var patient = await _repository.GetByIdAsync(id);

                if (patient == null)
                {
                    _logger.LogWarning("患者不存在 {PatientId}", id);
                    throw new NotFoundException($"患者 {id} 不存在");
                }

                _logger.LogInformation(
                    "成功获取患者信息 {PatientId}, 姓名: {PatientName}",
                    id,
                    patient.Name);

                return patient;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "获取患者信息失败 {PatientId}", id);
                throw;
            }
        }
    }
}
```

### 7. 后台服务优化
**文件路径**: `src/Server/Services/LYBT.WebAPI/Services/DeferredInitializationService.cs`

```csharp
public class DeferredInitializationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DeferredInitializationService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 延迟5秒，让主要服务先启动
        await Task.Delay(5000, stoppingToken);

        _logger.LogInformation("开始延迟初始化...");

        using var scope = _serviceProvider.CreateScope();

        try
        {
            // 预热EF Core查询
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            _ = await dbContext.Herbs.AsNoTracking().Take(1).ToListAsync(stoppingToken);

            // 预加载常用数据到缓存
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();
            await cacheService.WarmupAsync(stoppingToken);

            _logger.LogInformation("延迟初始化完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "延迟初始化失败");
        }
    }
}
```

### 8. 内存泄漏修复
**文件路径**: `src/Server/Core/LYBT.Infrastructure/Services/DisposableService.cs`

```csharp
// 修改前：未正确释放资源
public class FileWatcherService
{
    private FileSystemWatcher _watcher;

    public void StartWatching(string path)
    {
        _watcher = new FileSystemWatcher(path);
        _watcher.EnableRaisingEvents = true;
    }
}

// 修改后：实现IDisposable
public class FileWatcherService : IDisposable
{
    private FileSystemWatcher? _watcher;
    private bool _disposed;

    public void StartWatching(string path)
    {
        _watcher = new FileSystemWatcher(path);
        _watcher.EnableRaisingEvents = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _watcher?.Dispose();
            }
            _disposed = true;
        }
    }
}
```

### 9. 使用键控服务（.NET 8新特性）
**文件路径**: `src/Server/Services/LYBT.WebAPI/Extensions/KeyedServicesExtensions.cs`

```csharp
public static class KeyedServicesExtensions
{
    public static IServiceCollection AddKeyedServices(this IServiceCollection services)
    {
        // 注册不同的通知服务
        services.AddKeyedScoped<INotificationService, EmailNotificationService>("email");
        services.AddKeyedScoped<INotificationService, SmsNotificationService>("sms");
        services.AddKeyedScoped<INotificationService, WeChatNotificationService>("wechat");

        // 注册不同的缓存策略
        services.AddKeyedSingleton<ICacheStrategy, MemoryCacheStrategy>("memory");
        services.AddKeyedSingleton<ICacheStrategy, RedisCacheStrategy>("redis");

        return services;
    }
}

// 使用键控服务
public class NotificationController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;

    [HttpPost("send")]
    public async Task<IActionResult> SendNotification(
        [FromBody] NotificationRequest request)
    {
        // 根据类型获取对应的服务
        var notificationService = _serviceProvider.GetRequiredKeyedService<INotificationService>(
            request.Type.ToLower());

        await notificationService.SendAsync(request.Message);
        return Ok();
    }
}
```

## ✅ 验收标准
1. 日志输出减少50%以上
2. 所有服务生命周期正确配置
3. 消除Service Locator反模式
4. 启动时间缩短30%
5. 内存泄漏问题解决
6. 结构化日志实施

## 🔧 实施步骤
1. [ ] 调整日志级别配置
2. [ ] 配置Serilog结构化日志
3. [ ] 审查并修正DI生命周期
4. [ ] 消除Service Locator
5. [ ] 实现延迟初始化
6. [ ] 修复内存泄漏
7. [ ] 性能测试验证

## 📊 预期效果
- 日志I/O：减少50%
- 启动时间：5秒 → 3秒
- 内存占用：稳定在150MB以内
- GC压力：降低40%

## 🏷️ 标签
`performance` `logging` `dependency-injection` `optimization` `mvp`

## 📎 相关文档
- [Serilog Best Practices](https://github.com/serilog/serilog/wiki)
- [DI in ASP.NET Core](https://docs.microsoft.com/aspnet/core/fundamentals/dependency-injection)
- [.NET 8 Performance](https://devblogs.microsoft.com/dotnet/performance-improvements-in-net-8)

---
**优先级**: P1（高）
**预估工时**: 1天
**负责人**: 待分配
**状态**: 待开始
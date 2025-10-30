# LYBT.Core.EventBus - 事件总线与模块管理核心库

## 📦 项目定位

- **层级**:Server端
- **类型**:核心库(事件总线 + 模块管理)
- **职责**:提供Server端模块化架构的核心基础设施,包括进程内事件总线(In-Memory Event Bus)和完整的模块生命周期管理系统。支持模块注册/启动/停止/健康检查/依赖分析,实现业务模块间的松耦合通信,为8个业务模块提供统一的事件发布订阅机制和模块协调能力。

## 📂 代码结构

```
LYBT.Core.EventBus/
├── Abstractions/
│   ├── IEventBus.cs                 # 事件总线接口(7个方法)
│   │   ├── PublishAsync()            # 发布事件
│   │   ├── Subscribe()               # 订阅事件(2个重载)
│   │   ├── Unsubscribe()             # 取消订阅
│   │   ├── GetSubscriptionCount()    # 获取订阅数
│   │   ├── GetRegisteredEventTypes() # 获取已注册事件类型
│   │   └── ClearSubscriptions()      # 清除所有订阅
│   ├── IIntegrationEvent.cs         # 集成事件接口
│   └── IIntegrationEventHandler.cs  # 事件处理器接口
├── Events/
│   └── IntegrationEventBase.cs      # 事件基类(EventId, Timestamp)
├── Implementation/
│   └── InMemoryEventBus.cs          # 进程内事件总线实现
│       ├── PublishAsync()            # 发布实现(异步并行处理)
│       ├── Subscribe()               # 订阅实现(支持泛型和委托)
│       ├── ProcessEventAsync()       # 事件处理(统计+重试)
│       └── GetStatistics()           # 统计信息(发布/处理/失败)
├── Module/
│   ├── IModule.cs                   # 模块接口
│   ├── IModuleLifecycle.cs          # 模块生命周期接口
│   ├── IModuleManager.cs            # 模块管理器接口(28个方法)
│   │   ├── RegisterModuleAsync()     # 注册模块
│   │   ├── StartAllModulesAsync()    # 启动所有模块
│   │   ├── StopAllModulesAsync()     # 停止所有模块
│   │   ├── StartModuleAsync()        # 启动单个模块
│   │   ├── StopModuleAsync()         # 停止单个模块
│   │   ├── RestartModuleAsync()      # 重启模块
│   │   ├── CheckDependencies()       # 检查依赖关系
│   │   ├── ResolveStartupOrder()     # 解析启动顺序
│   │   ├── ValidateModule()          # 验证模块
│   │   ├── GetModuleHealthAsync()    # 获取模块健康状态
│   │   ├── EnableModuleAsync()       # 启用模块
│   │   └── DisableModuleAsync()      # 禁用模块
│   ├── ModuleBase.cs                # 模块基类
│   ├── ModuleDescriptor.cs          # 模块描述符(元数据)
│   ├── ModuleState.cs               # 模块状态枚举
│   ├── ModuleCategory.cs            # 模块分类枚举
│   ├── ModuleHealthStatus.cs        # 模块健康状态枚举
│   ├── ModuleValidationResult.cs    # 模块验证结果
│   ├── Events/
│   │   ├── ModuleRegisteredEvent.cs       # 模块注册事件
│   │   ├── ModuleUnregisteredEvent.cs     # 模块注销事件
│   │   ├── ModuleStateChangedEvent.cs     # 模块状态变更事件
│   │   ├── ModuleHealthChangedEvent.cs    # 模块健康变更事件
│   │   └── ModuleDependencyEvent.cs       # 模块依赖事件
│   └── Communication/
│       └── ModuleCommunicationExample.cs  # 模块间通信示例
├── Services/
│   └── EventBusHostedService.cs     # 事件总线后台服务
└── Extensions/
    └── ServiceCollectionExtensions.cs # 依赖注入扩展方法
```

**说明**:
- **Abstractions/**:事件总线核心抽象(IEventBus + 事件/处理器接口)
- **Implementation/**:InMemoryEventBus实现(进程内异步事件总线)
- **Module/**:完整的模块化架构支持(生命周期管理+健康检查+依赖分析)
- **Module/Events/**:5个模块事件(注册/注销/状态变更/健康变更/依赖)

## 🔗 依赖关系

### 依赖的项目
- **无内部项目依赖** - 纯基础库

### 被依赖项目
1. **LYBT.Module.*** - 8个业务模块使用事件总线进行模块间通信
2. **LYBT.WebAPI** - API服务使用模块管理器初始化和管理所有业务模块

### NuGet包
- **Microsoft.Extensions.DependencyInjection.Abstractions** - 依赖注入抽象
- **Microsoft.Extensions.Logging.Abstractions** - 日志抽象
- **Microsoft.Extensions.Hosting.Abstractions** - 后台服务抽象(IHostedService)
- **Microsoft.AspNetCore.Http.Abstractions** - HTTP上下文抽象
- **Microsoft.AspNetCore.Hosting.Abstractions** - 主机抽象
- **Microsoft.Extensions.Configuration.Abstractions** - 配置抽象

## 🛠 技术栈

- **.NET 8**:基础框架
- **In-Memory Event Bus**:进程内事件总线(无外部MQ依赖,符合MVP原则)
- **模块化架构**:支持模块动态注册/启动/停止/健康检查
- **异步编程**:全异步事件处理(async/await)
- **泛型编程**:类型安全的事件订阅和发布
- **依赖分析**:模块依赖关系检查和启动顺序解析

## 🚀 快速开始

此项目是一个类库,无法独立运行。

```bash
# 构建此项目
dotnet build src/Server/Core/LYBT.Core.EventBus/LYBT.Core.EventBus.csproj
```

**集成说明**:

### 1. 注册事件总线(在Startup.cs中)
```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // 注册事件总线(扩展方法)
        services.AddEventBus();

        // 或手动注册
        services.AddSingleton<IEventBus, InMemoryEventBus>();
        services.AddHostedService<EventBusHostedService>();
    }
}
```

### 2. 定义集成事件(跨模块通信)
```csharp
using LYBT.Core.EventBus.Events;

// 患者创建事件(Patients模块发布)
public class PatientCreatedEvent : IntegrationEventBase
{
    public int PatientId { get; set; }
    public string PatientName { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### 3. 发布事件(在业务模块中)
```csharp
public class PatientService : IPatientService
{
    private readonly IEventBus _eventBus;
    private readonly IPatientRepository _repository;

    public PatientService(IEventBus eventBus, IPatientRepository repository)
    {
        _eventBus = eventBus;
        _repository = repository;
    }

    public async Task<PatientDto> CreateAsync(CreatePatientRequest request)
    {
        // 创建患者
        var patient = await _repository.AddAsync(new Patient { ... });

        // 发布事件(通知其他模块)
        await _eventBus.PublishAsync(new PatientCreatedEvent
        {
            PatientId = patient.Id,
            PatientName = patient.Name,
            CreatedAt = DateTime.UtcNow
        });

        return patient;
    }
}
```

### 4. 订阅事件(在其他模块中)
```csharp
public class MedicalCaseModule : ModuleBase
{
    private readonly IEventBus _eventBus;

    public MedicalCaseModule(IEventBus eventBus, ILogger<MedicalCaseModule> logger)
        : base(logger)
    {
        _eventBus = eventBus;
    }

    protected override async Task OnStartAsync()
    {
        // 方式1:订阅到处理器类
        _eventBus.Subscribe<PatientCreatedEvent, PatientCreatedEventHandler>();

        // 方式2:订阅到委托
        _eventBus.Subscribe<PatientCreatedEvent>(async (evt) =>
        {
            Logger.LogInformation($"患者{evt.PatientName}已创建,准备初始化病案...");
            // 业务逻辑:为新患者创建初始病案
            await InitializeMedicalCaseAsync(evt.PatientId);
        });
    }
}

// 方式1的处理器实现
public class PatientCreatedEventHandler : IIntegrationEventHandler<PatientCreatedEvent>
{
    private readonly IMedicalCaseService _service;

    public PatientCreatedEventHandler(IMedicalCaseService service)
    {
        _service = service;
    }

    public async Task HandleAsync(PatientCreatedEvent @event)
    {
        // 为新患者创建初始病案
        await _service.CreateAsync(new CreateMedicalCaseRequest
        {
            PatientId = @event.PatientId,
            Status = MedicalCaseStatus.Draft
        });
    }
}
```

### 5. 模块化架构示例(模块注册与生命周期)
```csharp
// Step 1: 定义业务模块
public class AuthModule : ModuleBase
{
    public AuthModule(ILogger<AuthModule> logger) : base(logger) { }

    public override ModuleDescriptor GetDescriptor() => new()
    {
        Id = "LYBT.Module.Auth",
        Name = "认证模块",
        Category = ModuleCategory.Core,
        Dependencies = new List<string>(), // 无依赖
        Tags = new[] { "Auth", "Security" }
    };

    protected override async Task OnStartAsync()
    {
        Logger.LogInformation("认证模块启动中...");
        // 初始化逻辑(如连接数据库、加载配置)
        await Task.CompletedTask;
    }

    protected override async Task OnStopAsync()
    {
        Logger.LogInformation("认证模块停止中...");
        // 清理资源
        await Task.CompletedTask;
    }
}

// Step 2: 注册模块到模块管理器
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IModuleManager, ModuleManager>();
    }

    public async Task Configure(IApplicationBuilder app, IModuleManager moduleManager)
    {
        // 注册所有业务模块
        await moduleManager.RegisterModuleAsync(new AuthModule(loggerFactory.CreateLogger<AuthModule>()));
        await moduleManager.RegisterModuleAsync(new PatientsModule(...));
        // 其他模块...

        // 检查依赖关系
        var dependencies = moduleManager.CheckDependencies();
        if (!dependencies.IsValid)
        {
            throw new Exception($"模块依赖检查失败:{dependencies.ErrorMessage}");
        }

        // 解析启动顺序(根据依赖关系)
        var startupOrder = moduleManager.ResolveStartupOrder();

        // 启动所有模块(按依赖顺序)
        await moduleManager.StartAllModulesAsync();

        // 订阅模块状态变更事件
        moduleManager.ModuleStateChanged += (sender, args) =>
        {
            Logger.LogInformation($"模块{args.ModuleId}状态变更:{args.OldState} → {args.NewState}");
        };
    }
}
```

## 📚 详细文档

- **完整模块文档**:[docs/reference/modules/eventbus/](../../../../docs/reference/modules/eventbus/) *(待创建)*
- **架构设计**:[docs/explanation/architecture/server/eventbus-design.md](../../../../docs/explanation/architecture/server/eventbus-design.md) *(待创建)*
- **开发指南**:[docs/how-to-guides/server/eventbus-integration.md](../../../../docs/how-to-guides/server/eventbus-integration.md) *(待创建)*
- **模块化架构**:[docs/explanation/architecture/server/README.md](../../../../docs/explanation/architecture/server/README.md) - 参见"模块化架构"章节

---

**最后更新**:2025-10-29
**维护负责**:Server端开发组

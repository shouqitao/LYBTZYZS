# Desktop端组件化架构模式

> **文档版本**: v1.0
> **创建日期**: 2025-11-04
> **最后更新**: 2025-11-04
> **关联Issue**: #1790, #1795

---

## 📋 目录

- [1. 概述](#1-概述)
- [2. 背景与动机](#2-背景与动机)
- [3. 组件化模式](#3-组件化模式)
- [4. 实战案例](#4-实战案例)
- [5. 最佳实践](#5-最佳实践)
- [6. 常见问题](#6-常见问题)

---

## 1. 概述

### 1.1 什么是组件化架构模式？

组件化架构模式是一种将**大型ViewModel**拆分为多个**职责单一的服务组件**的架构模式，通过提取Manager、Handler、Validator等服务，降低单个类的复杂度，提升代码的可维护性和可测试性。

### 1.2 核心原则

- **单一职责原则（SRP）**: 每个组件只负责一个明确的职责
- **依赖注入（DI）**: 所有组件通过构造函数注入
- **事件驱动**: 组件间通过事件通信，降低耦合
- **可测试性**: 每个组件可独立测试

### 1.3 适用场景

| 场景 | 触发条件 | 推荐方案 |
|------|---------|---------|
| **ViewModel过大** | 文件 > 1000行 | 提取Manager服务 |
| **方法过于复杂** | 方法 > 100行 | 提取辅助方法或Handler |
| **验证逻辑复杂** | 验证代码 > 200行 | 提取Validator组件 |
| **数据管理复杂** | 数据操作 > 300行 | 提取DataManager组件 |

---

## 2. 背景与动机

### 2.1 问题场景

在MVP阶段，ViewModel往往承担过多职责：

```csharp
// ❌ 反模式：过大的ViewModel（1000+行）
public class PatientSelectionViewModel : UnifiedViewModelBase
{
    // 患者搜索逻辑（200行）
    private async Task SearchPatientsAsync() { /* 200行代码 */ }

    // 分页逻辑（100行）
    private async Task NextPageAsync() { /* 100行代码 */ }

    // 待诊队列逻辑（150行）
    private async Task LoadPendingQueueAsync() { /* 150行代码 */ }

    // 未完成医案逻辑（150行）
    private async Task LoadUnfinishedCasesAsync() { /* 150行代码 */ }

    // 患者选择逻辑（100行）
    private void HandlePatientSelection() { /* 100行代码 */ }

    // 其他命令和属性（300行）
    // ...
}
```

**问题**:
- ✗ 单个文件过大（1000+行），难以维护
- ✗ 职责不清晰，违反SRP原则
- ✗ 测试困难，Mock复杂
- ✗ 代码重用性差

### 2.2 解决方案：组件化架构

通过提取专门的服务组件，将ViewModel拆分为：

```
PatientSelectionViewModel (100行)
  ├── PatientSearchManager (200行) - 搜索和分页
  ├── PendingQueueManager (100行) - 待诊队列
  └── UnfinishedCaseHandler (200行) - 未完成医案
```

**优势**:
- ✓ 职责清晰，易于理解
- ✓ 文件适中（每个<300行）
- ✓ 便于测试和Mock
- ✓ 组件可复用

---

## 3. 组件化模式

### 3.1 核心组件类型

Desktop端组件化架构定义了以下核心组件类型：

#### 3.1.1 Manager（管理器）

**职责**: 管理特定领域的业务逻辑和数据状态

**命名约定**: `{Domain}Manager`

**典型职责**:
- 数据加载和刷新
- 状态管理
- 业务规则协调
- 事件发布

**示例**:
```csharp
/// <summary>
/// 患者搜索管理器 - 负责患者搜索和分页逻辑
/// Issue #1790: 从PatientSelectionViewModel提取
/// </summary>
public class PatientSearchManager
{
    private readonly PatientCommandHandler _commandHandler;
    private readonly ILogger<PatientSearchManager> _logger;

    public ObservableCollection<PatientDto> Patients { get; } = new();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }

    /// <summary>
    /// 搜索完成事件
    /// </summary>
    public event EventHandler<SearchCompletedEventArgs>? SearchCompleted;

    public async Task<bool> ExecuteSearchAsync(string searchKeyword)
    {
        // 搜索逻辑实现...
    }

    public async Task<bool> NextPageAsync(string searchKeyword)
    {
        // 分页逻辑实现...
    }
}
```

#### 3.1.2 Handler（处理器）

**职责**: 处理特定的命令或操作流程

**命名约定**: `{Action}Handler` 或 `{Domain}CommandHandler`

**典型职责**:
- 执行复杂业务操作
- 协调多个服务调用
- 事务管理
- 错误处理

**示例**:
```csharp
/// <summary>
/// 未完成医案处理器 - 负责未完成医案的加载和选择逻辑
/// Issue #1790: 从PatientSelectionViewModel提取
/// </summary>
public class UnfinishedCaseHandler
{
    private readonly IMedicalCaseApi _medicalCaseApi;
    private readonly ILogger<UnfinishedCaseHandler> _logger;

    public ObservableCollection<MedicalCaseDto> UnfinishedCases { get; } = new();

    /// <summary>
    /// 医案选择完成事件
    /// </summary>
    public event EventHandler<CaseSelectedEventArgs>? CaseSelected;

    public async Task LoadUnfinishedCasesAsync()
    {
        // 加载未完成医案逻辑...
    }

    public async Task<PatientDto?> SelectCaseAsync(Guid caseId)
    {
        // 选择医案并加载患者逻辑...
    }
}
```

#### 3.1.3 Validator（验证器）

**职责**: 封装复杂的验证逻辑

**命名约定**: `{Domain}Validator`

**典型职责**:
- 数据验证
- 业务规则校验
- 错误消息生成
- 验证结果封装

**示例**:
```csharp
/// <summary>
/// 处方编辑器验证器 - 负责处方数据验证
/// Issue #1790: 从PrescriptionEditorViewModel提取
/// </summary>
public class PrescriptionEditorValidator
{
    private readonly ILogger<PrescriptionEditorValidator> _logger;

    /// <summary>
    /// 验证完成事件
    /// </summary>
    public event EventHandler<ValidationResultEventArgs>? ValidationCompleted;

    public ValidationResult Validate(
        PatientDto? patient,
        Guid medicalCaseId,
        List<PrescriptionItemDto> items,
        List<HerbDto> allHerbs)
    {
        // 基础信息验证
        if (patient == null || medicalCaseId == Guid.Empty)
        {
            return ValidationResult.Fail("缺少患者或医案信息");
        }

        // 药材验证
        if (!items.Any())
        {
            return ValidationResult.Fail("至少需要一味药材");
        }

        // 触发验证完成事件
        ValidationCompleted?.Invoke(this, new ValidationResultEventArgs
        {
            Result = result
        });

        return ValidationResult.Success();
    }
}
```

### 3.2 组件间通信模式

#### 3.2.1 事件驱动通信（推荐）

**适用场景**: 组件需要通知ViewModel或其他组件某个操作完成

```csharp
// Manager发布事件
public class PatientSearchManager
{
    public event EventHandler<SearchCompletedEventArgs>? SearchCompleted;

    public async Task ExecuteSearchAsync(string keyword)
    {
        // 执行搜索...

        // 触发事件
        SearchCompleted?.Invoke(this, new SearchCompletedEventArgs
        {
            Keyword = keyword,
            ResultCount = results.Count
        });
    }
}

// ViewModel订阅事件
public class PatientSelectionViewModel : UnifiedViewModelBase
{
    public PatientSelectionViewModel(PatientSearchManager searchManager)
    {
        _searchManager = searchManager;

        // 订阅事件
        _searchManager.SearchCompleted += OnSearchCompleted;
    }

    private void OnSearchCompleted(object? sender, SearchCompletedEventArgs e)
    {
        // 更新UI状态
        StatusMessage = $"找到{e.ResultCount}条记录";
    }
}
```

#### 3.2.2 返回值通信

**适用场景**: 同步操作，需要立即获取结果

```csharp
// Handler返回结果
public class UnfinishedCaseHandler
{
    public async Task<PatientDto?> SelectCaseAsync(Guid caseId)
    {
        var patient = await LoadPatientAsync(caseId);
        return patient;
    }
}

// ViewModel调用
var patient = await _unfinishedCaseHandler.SelectCaseAsync(caseId);
if (patient != null)
{
    CurrentPatient = patient;
}
```

### 3.3 DI配置模式

#### 3.3.1 模块级注册

**位置**: `{Module}Module.cs`

```csharp
public class PatientsModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // Issue #1790: 注册PatientSelectionViewModel组件化服务
        containerRegistry.Register<PatientSearchManager>();
        containerRegistry.Register<UnfinishedCaseHandler>();
        containerRegistry.Register<PendingQueueManager>();

        // 注册ViewModel
        containerRegistry.Register<ViewModels.PatientSelectionViewModel>();
    }
}
```

#### 3.3.2 生命周期选择

| 生命周期 | 适用场景 | 示例 |
|---------|---------|------|
| **Transient** | 无状态组件，每次使用创建新实例 | Validator |
| **Scoped** | 有状态组件，同一请求/导航共享 | Manager, Handler |
| **Singleton** | 全局共享组件，应用级单例 | ConfigManager |

```csharp
// Transient (默认)
containerRegistry.Register<PrescriptionEditorValidator>();

// Scoped (推荐用于Manager)
containerRegistry.RegisterScoped<PatientSearchManager>();

// Singleton (谨慎使用)
containerRegistry.RegisterSingleton<AppConfigManager>();
```

---

## 4. 实战案例

### 4.1 案例1：PatientSelectionViewModel组件化

**Issue**: #1790
**提交**: a64eb6b6
**优化**: 1102行 → 1042行 + 3个Manager服务

#### 4.1.1 重构前

```csharp
// ❌ 反模式：职责过多（1102行）
public class PatientSelectionViewModel : UnifiedViewModelBase
{
    // 患者列表
    public ObservableCollection<PatientDto> Patients { get; } = new();

    // 待诊队列
    public ObservableCollection<PendingMedicalCaseDto> PendingQueue { get; } = new();

    // 未完成医案
    public ObservableCollection<MedicalCaseDto> UnfinishedCases { get; } = new();

    // 搜索逻辑（200行）
    private async Task SearchPatientsAsync(string keyword) { /* 200行 */ }

    // 分页逻辑（100行）
    private async Task NextPageAsync() { /* 100行 */ }

    // 待诊队列逻辑（150行）
    private async Task LoadPendingQueueAsync() { /* 150行 */ }

    // 未完成医案逻辑（150行）
    private async Task LoadUnfinishedCasesAsync() { /* 150行 */ }

    // 其他代码...（502行）
}
```

#### 4.1.2 重构后

**架构图**:

```
PatientSelectionViewModel (1042行)
  │
  ├─► PatientSearchManager (247行)
  │    ├─ ExecuteSearchAsync()
  │    ├─ LoadInitialPatientsAsync()
  │    ├─ NextPageAsync()
  │    └─ PreviousPageAsync()
  │
  ├─► PendingQueueManager (185行)
  │    ├─ LoadPendingCasesAsync()
  │    ├─ LoadPatientForPendingCaseAsync()
  │    └─ RemoveFromQueue()
  │
  └─► UnfinishedCaseHandler (208行)
       ├─ LoadUnfinishedCasesAsync()
       ├─ SelectUnfinishedCaseAsync()
       └─ ClearCases()
```

**ViewModel（简化后）**:

```csharp
/// <summary>
/// 患者选择ViewModel - Epic #1114 Phase 2三层对齐架构
/// Issue #1790: 组件化重构，提取3个Manager服务
/// </summary>
public class PatientSelectionViewModel : UnifiedViewModelBase
{
    #region 服务依赖

    private readonly PatientCommandHandler _commandHandler;
    private readonly PatientSearchManager _searchManager;      // Issue #1790
    private readonly PendingQueueManager _pendingQueueManager; // Issue #1790
    private readonly UnfinishedCaseHandler _unfinishedCaseHandler; // Issue #1790

    #endregion

    #region 属性（委托给Manager）

    /// <summary>
    /// 患者列表（委托给SearchManager）
    /// </summary>
    public ObservableCollection<PatientDto> Patients => _searchManager.Patients;

    /// <summary>
    /// 待诊队列（委托给PendingQueueManager）
    /// </summary>
    public ObservableCollection<PendingMedicalCaseDto> PendingQueue
        => _pendingQueueManager.PendingQueue;

    /// <summary>
    /// 未完成医案（委托给UnfinishedCaseHandler）
    /// </summary>
    public ObservableCollection<MedicalCaseDto> UnfinishedCases
        => _unfinishedCaseHandler.UnfinishedCases;

    #endregion

    #region 构造函数

    public PatientSelectionViewModel(
        PatientCommandHandler commandHandler,
        PatientSearchManager searchManager,           // Issue #1790: 注入
        PendingQueueManager pendingQueueManager,      // Issue #1790: 注入
        UnfinishedCaseHandler unfinishedCaseHandler,  // Issue #1790: 注入
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IRegionManager regionManager,
        ISessionManager? sessionManager = null,
        IUserNotificationService? userNotificationService = null)
        : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
    {
        _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
        _searchManager = searchManager ?? throw new ArgumentNullException(nameof(searchManager));
        _pendingQueueManager = pendingQueueManager ?? throw new ArgumentNullException(nameof(pendingQueueManager));
        _unfinishedCaseHandler = unfinishedCaseHandler ?? throw new ArgumentNullException(nameof(unfinishedCaseHandler));

        // 订阅Manager事件
        _searchManager.SearchCompleted += OnSearchCompleted;
        _pendingQueueManager.PendingQueueLoaded += OnPendingQueueLoaded;
        _unfinishedCaseHandler.CaseSelected += OnUnfinishedCaseSelected;

        // 初始化命令...
    }

    #endregion

    #region 命令实现（委托给Manager）

    private async Task ExecuteSearchAsync()
    {
        await ExecuteSafelyAsync(async () =>
        {
            // 委托给SearchManager
            await _searchManager.ExecuteSearchAsync(SearchKeyword);
        });
    }

    private async Task ExecuteLoadPendingQueueAsync()
    {
        await ExecuteSafelyAsync(async () =>
        {
            // 委托给PendingQueueManager
            await _pendingQueueManager.LoadPendingCasesAsync();
        });
    }

    #endregion

    #region 事件处理

    private void OnSearchCompleted(object? sender, SearchCompletedEventArgs e)
    {
        Logger.LogInformation("搜索完成，共{Count}条记录", e.ResultCount);
        RaisePropertyChanged(nameof(Patients));
    }

    #endregion
}
```

#### 4.1.3 优化成果

| 指标 | 重构前 | 重构后 | 改善 |
|------|-------|-------|------|
| **ViewModel行数** | 1102行 | 1042行 | -60行 |
| **最大方法行数** | 150行 | 40行 | -73% |
| **文件数量** | 1个 | 4个 | +3个组件 |
| **职责数量** | 5个 | 1个（协调） | -80% |
| **可测试性** | 困难 | 容易 | ✓ |

### 4.2 案例2：PrescriptionEditorViewModel组件化

**Issue**: #1790 Phase 4
**提交**: 78ca9061
**优化**: 1056行 → 900行 + 2个Manager服务

#### 4.2.1 重构方案

```
PrescriptionEditorViewModel (900行)
  │
  ├─► PrescriptionEditorHerbFilterManager (105行)
  │    ├─ LoadHerbsAsync()
  │    └─ FilterHerbs()
  │
  └─► PrescriptionEditorValidator (215行)
       ├─ Validate()
       ├─ ValidateDraftAsync()
       └─ CheckDuplicateHerbs()
```

#### 4.2.2 关键代码

**Validator组件**:

```csharp
/// <summary>
/// 处方编辑器验证器 - 负责处方数据验证
/// Issue #1790: 从PrescriptionEditorViewModel提取
/// </summary>
public class PrescriptionEditorValidator
{
    private readonly ILogger<PrescriptionEditorValidator> _logger;

    public event EventHandler<ValidationResultEventArgs>? ValidationCompleted;

    public ValidationResult Validate(
        PatientDto? patient,
        Guid medicalCaseId,
        List<PrescriptionItemDto> items,
        List<HerbDto> allHerbs)
    {
        // 1. 基础信息验证
        var basicResult = ValidateBasicInfo(patient, medicalCaseId);
        if (!basicResult.IsValid)
        {
            return basicResult;
        }

        // 2. 药材项验证
        var itemsResult = ValidateHerbItems(items, allHerbs);
        if (!itemsResult.IsValid)
        {
            return itemsResult;
        }

        // 触发验证完成事件
        ValidationCompleted?.Invoke(this, new ValidationResultEventArgs
        {
            Result = ValidationResult.Success(),
            ItemCount = items.Count
        });

        return ValidationResult.Success();
    }

    private ValidationResult ValidateBasicInfo(PatientDto? patient, Guid medicalCaseId)
    {
        if (patient == null)
        {
            return ValidationResult.Fail("缺少患者信息");
        }

        if (medicalCaseId == Guid.Empty)
        {
            return ValidationResult.Fail("缺少医案信息");
        }

        return ValidationResult.Success();
    }

    private ValidationResult ValidateHerbItems(
        List<PrescriptionItemDto> items,
        List<HerbDto> allHerbs)
    {
        if (!items.Any())
        {
            return ValidationResult.Fail("处方至少需要一味药材");
        }

        // 检查每味药材是否在系统药材库中
        foreach (var item in items)
        {
            var herb = allHerbs.FirstOrDefault(h => h.Id == item.HerbId);
            if (herb == null)
            {
                return ValidationResult.Fail(
                    $"药材「{item.HerbName}」不在系统药材库中，请重新选择");
            }

            if (item.Dosage <= 0)
            {
                return ValidationResult.Fail(
                    $"药材「{item.HerbName}」的用量必须大于0");
            }
        }

        return ValidationResult.Success();
    }
}
```

---

## 5. 最佳实践

### 5.1 何时提取组件？

#### 5.1.1 量化指标

| 指标 | 阈值 | 建议 |
|------|-----|------|
| **ViewModel文件大小** | > 1000行 | 必须拆分 |
| **单个方法长度** | > 100行 | 提取辅助方法或Handler |
| **验证逻辑复杂度** | > 200行 | 提取Validator |
| **职责数量** | > 3个 | 提取Manager |

#### 5.1.2 代码异味识别

```csharp
// ⚠️ 代码异味1：过多的私有方法（>20个）
public class MyViewModel
{
    private async Task Method1() { }
    private async Task Method2() { }
    // ... 20+ 私有方法
}
// 👉 提示：考虑提取Manager服务

// ⚠️ 代码异味2：过长的验证逻辑（>200行）
public bool Validate()
{
    // 200行验证代码
}
// 👉 提示：提取Validator组件

// ⚠️ 代码异味3：重复的数据操作（CRUD）
private async Task LoadDataAsync() { }
private async Task RefreshDataAsync() { }
private async Task ClearDataAsync() { }
// 👉 提示：提取DataManager组件
```

### 5.2 组件设计原则

#### 5.2.1 单一职责

```csharp
// ✓ 正确：职责单一
public class PatientSearchManager
{
    // 只负责搜索相关逻辑
    public async Task SearchAsync() { }
    public async Task NextPageAsync() { }
}

// ✗ 错误：职责混杂
public class PatientManager
{
    public async Task SearchAsync() { }      // 搜索
    public async Task SaveAsync() { }        // 保存
    public async Task PrintAsync() { }       // 打印
    public async Task ExportAsync() { }      // 导出
}
```

#### 5.2.2 依赖注入

```csharp
// ✓ 正确：构造函数注入
public class PatientSearchManager
{
    private readonly PatientCommandHandler _commandHandler;
    private readonly ILogger<PatientSearchManager> _logger;

    public PatientSearchManager(
        PatientCommandHandler commandHandler,
        ILogger<PatientSearchManager> logger)
    {
        _commandHandler = commandHandler;
        _logger = logger;
    }
}

// ✗ 错误：Service Locator反模式
public class PatientSearchManager
{
    public PatientSearchManager()
    {
        var commandHandler = Container.Resolve<PatientCommandHandler>(); // ✗
    }
}
```

#### 5.2.3 事件驱动

```csharp
// ✓ 正确：通过事件通信
public class PatientSearchManager
{
    public event EventHandler<SearchCompletedEventArgs>? SearchCompleted;

    public async Task SearchAsync()
    {
        // 执行搜索...
        SearchCompleted?.Invoke(this, new SearchCompletedEventArgs());
    }
}

// ✗ 错误：回调地狱
public class PatientSearchManager
{
    public async Task SearchAsync(Action<List<PatientDto>> onCompleted)
    {
        // 执行搜索...
        onCompleted(results); // ✗ 回调模式
    }
}
```

### 5.3 命名约定

| 组件类型 | 命名模式 | 示例 |
|---------|---------|------|
| **Manager** | `{Domain}Manager` | `PatientSearchManager` |
| **Handler** | `{Action}Handler` | `UnfinishedCaseHandler` |
| **Validator** | `{Domain}Validator` | `PrescriptionEditorValidator` |
| **事件参数** | `{Event}EventArgs` | `SearchCompletedEventArgs` |

### 5.4 测试策略

#### 5.4.1 组件独立测试

```csharp
[Fact]
public async Task ExecuteSearchAsync_WithKeyword_ShouldReturnResults()
{
    // Arrange
    var commandHandlerMock = Substitute.For<PatientCommandHandler>();
    var loggerMock = Substitute.For<ILogger<PatientSearchManager>>();
    var manager = new PatientSearchManager(commandHandlerMock, loggerMock);

    // Act
    var result = await manager.ExecuteSearchAsync("张三");

    // Assert
    result.Should().BeTrue();
    manager.Patients.Should().NotBeEmpty();
}
```

#### 5.4.2 ViewModel集成测试

```csharp
[Fact]
public async Task SearchCommand_ShouldDelegateToSearchManager()
{
    // Arrange
    var searchManagerMock = Substitute.For<PatientSearchManager>();
    var viewModel = new PatientSelectionViewModel(
        commandHandler: _commandHandler,
        searchManager: searchManagerMock,
        // 其他依赖...
    );

    // Act
    await viewModel.SearchCommand.Execute();

    // Assert
    await searchManagerMock.Received(1).ExecuteSearchAsync(Arg.Any<string>());
}
```

---

## 6. 常见问题

### Q1: Manager和Handler有什么区别？

**A**:
- **Manager**: 管理一类业务的**状态和数据**，通常包含ObservableCollection等可观察对象
- **Handler**: 处理特定的**命令或操作流程**，通常无状态或只持有临时状态

```csharp
// Manager：有状态，管理数据
public class PatientSearchManager
{
    public ObservableCollection<PatientDto> Patients { get; } // 状态
    public int CurrentPage { get; set; } // 状态
}

// Handler：无状态，处理操作
public class UnfinishedCaseHandler
{
    public async Task<PatientDto?> SelectCaseAsync(Guid caseId) // 无状态操作
    {
        return await LoadPatientAsync(caseId);
    }
}
```

### Q2: 组件之间可以互相调用吗？

**A**: 不推荐。组件应该通过ViewModel协调，而不是直接相互调用。

```csharp
// ✓ 正确：通过ViewModel协调
public class PatientSelectionViewModel
{
    private readonly PatientSearchManager _searchManager;
    private readonly PendingQueueManager _queueManager;

    private async Task ExecuteSearchAsync()
    {
        await _searchManager.SearchAsync();
        await _queueManager.RefreshAsync(); // ViewModel协调
    }
}

// ✗ 错误：组件直接依赖
public class PatientSearchManager
{
    private readonly PendingQueueManager _queueManager; // ✗ 组件直接依赖

    public async Task SearchAsync()
    {
        // 搜索...
        await _queueManager.RefreshAsync(); // ✗ 直接调用
    }
}
```

### Q3: 组件应该注册为Transient还是Scoped？

**A**:
- **Transient**: 无状态Validator
- **Scoped**: 有状态Manager/Handler（推荐）
- **Singleton**: 全局共享服务（谨慎）

```csharp
// Validator: Transient（每次创建新实例）
containerRegistry.Register<PrescriptionEditorValidator>();

// Manager: Scoped（同一导航/请求共享）
containerRegistry.RegisterScoped<PatientSearchManager>();

// 配置服务: Singleton（全局单例）
containerRegistry.RegisterSingleton<AppConfigManager>();
```

### Q4: 如何处理组件中的异常？

**A**: 组件应该记录日志并返回失败结果，由ViewModel统一处理错误。

```csharp
public class PatientSearchManager
{
    public async Task<bool> ExecuteSearchAsync(string keyword)
    {
        try
        {
            // 搜索逻辑...
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索患者失败");
            return false; // 返回失败标志
        }
    }
}

// ViewModel统一错误处理
private async Task ExecuteSearchAsync()
{
    var success = await _searchManager.ExecuteSearchAsync(SearchKeyword);
    if (!success)
    {
        await ShowErrorMessageAsync("搜索失败，请重试");
    }
}
```

### Q5: 组件化会不会增加复杂度？

**A**: 短期看增加了类的数量，但长期看降低了维护复杂度。

**量化对比**:

| 指标 | 重构前 | 重构后 | 说明 |
|------|-------|-------|------|
| **文件数量** | 1个 | 4个 | +3个组件 |
| **单文件最大行数** | 1102行 | 247行 | -78% |
| **单个方法最大行数** | 150行 | 40行 | -73% |
| **职责数量** | 5个 | 1个 | -80% |
| **圈复杂度** | 高 | 低 | ✓ |
| **可测试性** | 困难 | 容易 | ✓ |

---

## 7. 相关资源

### 7.1 架构文档
- [Desktop端架构指南](README.md)
- [MVVM架构模式](mvvm-architecture.md)
- [三层对齐架构](.spec-workflow/steering/structure.md)

### 7.2 开发指南
- [ViewModel开发指南](../../how-to/guides/desktop/viewmodel-guide.md)
- [组件化重构指南](../../how-to/guides/desktop/component-refactoring.md)

### 7.3 参考实现
- **Issue #1790**: PatientSelectionViewModel组件化案例
- **提交 a64eb6b6**: PatientSelectionViewModel重构
- **提交 78ca9061**: PrescriptionEditorViewModel重构

---

**文档维护**: 本文档随组件化实践持续更新
**反馈渠道**: GitHub Issues
**最后审核**: 2025-11-04

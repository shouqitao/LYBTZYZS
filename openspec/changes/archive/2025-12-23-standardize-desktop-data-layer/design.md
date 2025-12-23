# Design: standardize-desktop-data-layer

## 架构概述

本设计文档定义Desktop业务模块的标准化数据分层架构，确保所有模块遵循一致的设计模式。

## 1. 模块分类与职责

### 1.1 模块类型定义

```
┌─────────────────────────────────────────────────────────────────┐
│                     Desktop 业务模块分类                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌──────────────────┐  ┌──────────────────┐  ┌────────────────┐│
│  │  独立实体模块      │  │   聚合根模块       │  │  从属实体模块   ││
│  │  (Standalone)     │  │   (Aggregate)     │  │  (Dependent)  ││
│  ├──────────────────┤  ├──────────────────┤  ├────────────────┤│
│  │ • Patients       │  │ • MedicalCase    │  │ • Consultation ││
│  │ • Users          │  │ • Formula        │  │ • Prescriptions││
│  │ • Herbs          │  │                  │  │                ││
│  ├──────────────────┤  ├──────────────────┤  ├────────────────┤│
│  │ 特征:            │  │ 特征:            │  │ 特征:          ││
│  │ • 独立Repository │  │ • Repository     │  │ • 无Repository ││
│  │ • 无DataManager  │  │ • DataManager    │  │ • CommandHandler│
│  │ • 完整CRUD       │  │ • 管理子实体     │  │ • 依赖父聚合   ││
│  └──────────────────┘  └──────────────────┘  └────────────────┘│
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 1.2 各类型职责说明

| 类型 | Repository | DataManager | CommandHandler | 数据持久化 |
|------|-----------|-------------|----------------|-----------|
| **独立实体** | 必须 | 可选(复杂场景) | 无 | 自身负责 |
| **聚合根** | 必须 | 必须 | 无 | 管理聚合内所有实体 |
| **从属实体** | 无 | 无 | 必须 | 通过父聚合 |

## 2. Repository层设计

### 2.1 接口层次结构

```
IRepository<TDetail, TList, TInput>  (基础接口)
    │
    ├── IPatientRepository           (独立实体)
    │       └── PatientRepository : RepositoryBase<...>
    │
    ├── IMedicalCaseRepository       (聚合根)
    │       └── MedicalCaseRepository : RepositoryBase<...>
    │
    └── IHerbRepository              (独立实体)
            └── HerbRepository : RepositoryBase<...>
```

### 2.2 标准Repository接口

```csharp
/// <summary>
/// 标准Repository接口
/// </summary>
/// <typeparam name="TDetail">详情DTO类型</typeparam>
/// <typeparam name="TList">列表DTO类型</typeparam>
/// <typeparam name="TInput">输入DTO类型</typeparam>
public interface IRepository<TDetail, TList, TInput>
    where TDetail : class
    where TList : class
    where TInput : class
{
    /// <summary>
    /// 分页查询
    /// </summary>
    Task<PagedResult<TList>> GetPagedAsync(
        int page,
        int pageSize,
        string? keyword = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据ID获取详情
    /// </summary>
    Task<TDetail?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建实体
    /// </summary>
    Task<TDetail> CreateAsync(TInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新实体
    /// </summary>
    Task<TDetail> UpdateAsync(Guid id, TInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除实体
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
```

### 2.3 RepositoryBase实现模板

```csharp
/// <summary>
/// Repository基类，封装通用API调用逻辑
/// </summary>
public abstract class RepositoryBase<TDetail, TList, TInput> : IRepository<TDetail, TList, TInput>
    where TDetail : class
    where TList : class
    where TInput : class
{
    protected readonly ILogger Logger;

    protected RepositoryBase(ILogger logger)
    {
        Logger = logger;
    }

    public async Task<PagedResult<TList>> GetPagedAsync(
        int page,
        int pageSize,
        string? keyword = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogDebug("获取分页数据: page={Page}, pageSize={PageSize}", page, pageSize);
            var response = await CallApiGetPagedAsync(page, pageSize, keyword, cancellationToken);
            return response.Content ?? new PagedResult<TList>();
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "获取分页数据失败");
            throw;
        }
    }

    // 子类实现API调用
    protected abstract Task<ApiResponse<PagedResult<TList>>> CallApiGetPagedAsync(
        int page, int pageSize, string? keyword, CancellationToken cancellationToken);
    protected abstract Task<ApiResponse<TDetail>> CallApiGetByIdAsync(Guid id, CancellationToken cancellationToken);
    protected abstract Task<ApiResponse<TDetail>> CallApiCreateAsync(TInput input, CancellationToken cancellationToken);
    protected abstract Task<ApiResponse<TDetail>> CallApiUpdateAsync(Guid id, TInput input, CancellationToken cancellationToken);
    protected abstract Task<ApiResponse> CallApiDeleteAsync(Guid id, CancellationToken cancellationToken);
}
```

## 3. DataManager层设计

### 3.1 DataManager职责

DataManager用于**聚合根模块**，负责：

1. **聚合状态管理**：维护当前聚合的完整状态
2. **子实体协调**：管理聚合内部的子实体关系
3. **脏数据追踪**：判断是否有未保存的变更
4. **一致性保存**：确保聚合内所有变更原子性保存

### 3.2 标准DataManager接口

```csharp
/// <summary>
/// 聚合根DataManager接口
/// </summary>
/// <typeparam name="TDetail">聚合根详情DTO</typeparam>
public interface IDataManager<TDetail> where TDetail : class
{
    /// <summary>
    /// 当前加载的聚合根
    /// </summary>
    TDetail? Current { get; }

    /// <summary>
    /// 是否有未保存的变更
    /// </summary>
    bool IsDirty { get; }

    /// <summary>
    /// 是否正在加载
    /// </summary>
    bool IsLoading { get; }

    /// <summary>
    /// 加载聚合根
    /// </summary>
    Task<TDetail?> LoadAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存聚合根及其所有子实体
    /// </summary>
    Task<TDetail> SaveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 清除当前状态
    /// </summary>
    void Clear();

    /// <summary>
    /// 聚合状态变更事件
    /// </summary>
    event EventHandler<DataManagerStateChangedEventArgs>? StateChanged;
}
```

### 3.3 MedicalCase聚合示例

```csharp
/// <summary>
/// 医案聚合根DataManager
/// </summary>
public interface IMedicalCaseDataManager : IDataManager<MedicalCaseDetailDto>
{
    /// <summary>
    /// 当前诊断（聚合内子实体）
    /// </summary>
    ConsultationDetailDto? CurrentConsultation { get; }

    /// <summary>
    /// 当前处方（聚合内子实体）
    /// </summary>
    PrescriptionDetailDto? CurrentPrescription { get; }

    /// <summary>
    /// 更新诊断
    /// </summary>
    void UpdateConsultation(ConsultationInputDto input);

    /// <summary>
    /// 更新处方
    /// </summary>
    void UpdatePrescription(PrescriptionInputDto input);
}
```

## 4. CommandHandler设计（从属实体）

### 4.1 从属实体访问模式

从属实体模块（Consultation/Prescriptions）通过CommandHandler访问父聚合的DataManager：

```
┌─────────────────────────────────────────────────────────────────┐
│                    从属实体访问模式                               │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ConsultationFormView                                            │
│        ↓ Binding                                                 │
│  ConsultationFormViewModel                                       │
│        ↓ 注入                                                    │
│  IConsultationCommandHandler                                     │
│        ↓ 依赖                                                    │
│  IMedicalCaseDataManager ←──── 聚合根管理器                       │
│        ↓                                                         │
│  MedicalCaseRepository                                           │
│        ↓                                                         │
│  IMedicalCaseApi → Backend                                       │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 4.2 CommandHandler接口规范

```csharp
/// <summary>
/// 从属实体命令处理器接口
/// </summary>
/// <typeparam name="TInput">输入DTO类型</typeparam>
public interface ICommandHandler<TInput> where TInput : class
{
    /// <summary>
    /// 是否可执行命令（父聚合已加载）
    /// </summary>
    bool CanExecute { get; }

    /// <summary>
    /// 更新子实体（标记脏数据，不立即保存）
    /// </summary>
    void Update(TInput input);

    /// <summary>
    /// 验证输入
    /// </summary>
    ValidationResult Validate(TInput input);
}

/// <summary>
/// 诊断命令处理器
/// </summary>
public interface IConsultationCommandHandler : ICommandHandler<ConsultationInputDto>
{
    /// <summary>
    /// 获取当前诊断数据
    /// </summary>
    ConsultationDetailDto? Current { get; }
}
```

## 5. DTO分层规范

### 5.1 DTO命名规则

| 后缀 | 用途 | 示例 |
|------|------|------|
| `ListDto` | 列表查询响应，轻量级 | `PatientListDto` |
| `DetailDto` | 详情查询响应，完整字段 | `PatientDetailDto` |
| `InputDto` | 创建/更新请求 | `PatientInputDto` |
| `SummaryDto` | 聚合内嵌入的简化DTO | `ConsultationSummaryDto` |

### 5.2 DTO层次关系

```
┌─────────────────────────────────────────────────────────────────┐
│                      DTO 层次结构                                │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─ Shared层 ─────────────────────────────────────────────────┐ │
│  │                                                             │ │
│  │  LYBT.Shared.Dtos/                                          │ │
│  │  ├── Common/                                                │ │
│  │  │   ├── PagedResult<T>.cs      # 分页结果                  │ │
│  │  │   └── ValidationResult.cs    # 验证结果                  │ │
│  │  │                                                          │ │
│  │  ├── Patient/                                               │ │
│  │  │   ├── PatientListDto.cs      # 列表                      │ │
│  │  │   ├── PatientDetailDto.cs    # 详情                      │ │
│  │  │   └── PatientInputDto.cs     # 输入                      │ │
│  │  │                                                          │ │
│  │  ├── MedicalCase/               # 聚合根DTO                 │ │
│  │  │   ├── MedicalCaseListDto.cs                              │ │
│  │  │   ├── MedicalCaseDetailDto.cs # 包含子实体               │ │
│  │  │   │   ├── Consultation: ConsultationDetailDto            │ │
│  │  │   │   └── Prescription: PrescriptionDetailDto            │ │
│  │  │   └── MedicalCaseInputDto.cs                             │ │
│  │  │                                                          │ │
│  │  └── Consultation/              # 从属实体DTO               │ │
│  │      ├── ConsultationDetailDto.cs                           │ │
│  │      ├── ConsultationInputDto.cs                            │ │
│  │      └── ConsultationSummaryDto.cs  # 用于列表嵌入          │ │
│  │                                                             │ │
│  └─────────────────────────────────────────────────────────────┘ │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 5.3 聚合根DTO设计

```csharp
/// <summary>
/// 医案详情DTO（聚合根）
/// </summary>
public class MedicalCaseDetailDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public MedicalCaseStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];

    // 聚合内子实体（完整DTO）
    public ConsultationDetailDto? Consultation { get; set; }
    public PrescriptionDetailDto? Prescription { get; set; }
}

/// <summary>
/// 医案列表DTO（轻量级）
/// </summary>
public class MedicalCaseListDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public MedicalCaseStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    // 聚合内子实体（Summary，仅关键字段）
    public ConsultationSummaryDto? ConsultationSummary { get; set; }
}
```

## 6. Models层规范

### 6.1 目录结构

```
Models/
├── [Entity]DetailModel.cs       # 详情UI模型（可编辑，支持验证）
├── [Entity]ViewState.cs         # 视图状态（编辑模式、选中状态等）
├── [Entity]EditContext.cs       # 编辑上下文（可选，复杂场景）
└── Items/
    ├── [Entity]Item.cs          # 列表项模型（只读）
    └── [Entity]SelectableItem.cs # 可选择列表项（支持多选）
```

### 6.2 命名约定

| 类型 | 命名模式 | 职责 | 继承 |
|------|---------|------|------|
| **DetailModel** | `{Entity}DetailModel` | 详情编辑，双向绑定 | `BindableBase` |
| **ViewState** | `{Entity}ViewState` | 视图状态管理 | `BindableBase` |
| **Item** | `{Entity}Item` | 列表展示，只读 | 无（POCO） |
| **SelectableItem** | `{Entity}SelectableItem` | 可选择列表项 | `BindableBase` |

### 6.3 示例实现

```csharp
// PatientDetailModel.cs - 详情UI模型
public class PatientDetailModel : BindableBase
{
    private string _name = string.Empty;
    private int _age;
    private string _gender = string.Empty;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public int Age
    {
        get => _age;
        set => SetProperty(ref _age, value);
    }

    // 从DTO创建
    public static PatientDetailModel FromDto(PatientDetailDto dto)
    {
        return new PatientDetailModel
        {
            Name = dto.Name,
            Age = dto.Age,
            Gender = dto.Gender
        };
    }

    // 转换为InputDto
    public PatientInputDto ToInputDto()
    {
        return new PatientInputDto
        {
            Name = Name,
            Age = Age,
            Gender = Gender
        };
    }
}

// PatientItem.cs - 列表项模型
public class PatientItem
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Age { get; init; }
    public string Gender { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }

    public static PatientItem FromListDto(PatientListDto dto)
    {
        return new PatientItem
        {
            Id = dto.Id,
            Name = dto.Name,
            Age = dto.Age,
            Gender = dto.Gender,
            CreatedAt = dto.CreatedAt
        };
    }
}

// PatientViewState.cs - 视图状态
public class PatientViewState : BindableBase
{
    private bool _isEditing;
    private bool _isLoading;
    private string? _errorMessage;

    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }
}
```

## 7. 数据流规范

### 7.1 独立实体模块数据流

```
┌─────────────────────────────────────────────────────────────────┐
│                   独立实体模块数据流                              │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  View (XAML)                                                     │
│      │                                                           │
│      │ {Binding Items}                                           │
│      │ {Binding SelectedItem}                                    │
│      │ {Binding DetailModel}                                     │
│      ↓                                                           │
│  ViewModel                                                       │
│      │ IPatientRepository                                        │
│      │                                                           │
│      │ 加载列表:                                                  │
│      │   var paged = await _repository.GetPagedAsync(...)        │
│      │   Items = paged.Items.Select(PatientItem.FromListDto)     │
│      │                                                           │
│      │ 加载详情:                                                  │
│      │   var detail = await _repository.GetByIdAsync(id)         │
│      │   DetailModel = PatientDetailModel.FromDto(detail)        │
│      │                                                           │
│      │ 保存:                                                      │
│      │   var input = DetailModel.ToInputDto()                    │
│      │   await _repository.UpdateAsync(id, input)                │
│      ↓                                                           │
│  Repository (PatientRepository)                                  │
│      │ IPatientApi                                               │
│      ↓                                                           │
│  API Client → Backend                                            │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 7.2 聚合根模块数据流

```
┌─────────────────────────────────────────────────────────────────┐
│                   聚合根模块数据流                                │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  View (XAML)                                                     │
│      │                                                           │
│      │ {Binding Current}                                         │
│      │ {Binding CurrentConsultation}                             │
│      │ {Binding CurrentPrescription}                             │
│      ↓                                                           │
│  ViewModel                                                       │
│      │ IMedicalCaseDataManager                                   │
│      │                                                           │
│      │ 加载聚合:                                                  │
│      │   await _dataManager.LoadAsync(id)                        │
│      │   // Current, CurrentConsultation, CurrentPrescription    │
│      │   // 自动可用                                              │
│      │                                                           │
│      │ 更新子实体:                                                │
│      │   _dataManager.UpdateConsultation(input)                  │
│      │   _dataManager.UpdatePrescription(input)                  │
│      │   // 标记脏数据，不立即保存                                 │
│      │                                                           │
│      │ 保存聚合:                                                  │
│      │   await _dataManager.SaveAsync()                          │
│      │   // 一次保存所有变更                                       │
│      ↓                                                           │
│  DataManager (MedicalCaseDataManager)                            │
│      │ IMedicalCaseRepository                                    │
│      ↓                                                           │
│  Repository (MedicalCaseRepository)                              │
│      │ IMedicalCaseApi                                           │
│      ↓                                                           │
│  API Client → Backend                                            │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 7.3 从属实体模块数据流

```
┌─────────────────────────────────────────────────────────────────┐
│                   从属实体模块数据流                              │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ConsultationFormView (XAML)                                     │
│      │                                                           │
│      │ {Binding Current}                                         │
│      │ {Binding SaveCommand}                                     │
│      ↓                                                           │
│  ConsultationFormViewModel                                       │
│      │ IConsultationCommandHandler                               │
│      │                                                           │
│      │ 获取当前数据:                                              │
│      │   var current = _commandHandler.Current                   │
│      │   // 来自父聚合 DataManager.CurrentConsultation           │
│      │                                                           │
│      │ 更新数据:                                                  │
│      │   _commandHandler.Update(input)                           │
│      │   // 标记父聚合脏数据                                       │
│      │                                                           │
│      │ 保存（通过父聚合）:                                        │
│      │   // 由MedicalCaseDetailViewModel调用                      │
│      │   // _dataManager.SaveAsync()                             │
│      ↓                                                           │
│  ConsultationCommandHandler                                      │
│      │ IMedicalCaseDataManager                                   │
│      ↓                                                           │
│  MedicalCaseDataManager                                          │
│      │ IMedicalCaseRepository                                    │
│      ↓                                                           │
│  API → Backend                                                   │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

## 8. 接口注册规范

### 8.1 注册位置

| 类型 | 注册位置 | 生命周期 |
|------|---------|---------|
| Repository | 模块 `{Module}Module.cs` | Singleton |
| DataManager | 模块 `{Module}Module.cs` | Singleton |
| CommandHandler | 模块 `{Module}Module.cs` | Singleton |
| ViewModel | 模块 `{Module}Module.cs` | Transient |

### 8.2 示例注册代码

```csharp
// PatientsModule.cs
public class PatientsModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // Repository - 独立实体
        containerRegistry.RegisterSingleton<IPatientRepository, PatientRepository>();

        // ViewModel
        containerRegistry.Register<PatientDetailViewModel>();
        containerRegistry.Register<PatientMasterDetailViewModel>();

        // 导航
        containerRegistry.RegisterForNavigation<PatientDetailView>();
    }
}

// MedicalCaseModule.cs
public class MedicalCaseModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // Repository - 聚合根
        containerRegistry.RegisterSingleton<IMedicalCaseRepository, MedicalCaseRepository>();

        // DataManager - 聚合状态管理
        containerRegistry.RegisterSingleton<IMedicalCaseDataManager, MedicalCaseDataManager>();

        // ViewModel
        containerRegistry.Register<MedicalCaseDetailViewModel>();
    }
}

// ConsultationModule.cs
public class ConsultationModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // CommandHandler - 从属实体
        containerRegistry.RegisterSingleton<IConsultationCommandHandler, ConsultationCommandHandler>();

        // ViewModel
        containerRegistry.Register<ConsultationFormViewModel>();
    }
}
```

## 9. 测试策略

### 9.1 Repository测试

```csharp
public class PatientRepositoryTests
{
    [Fact]
    public async Task GetByIdAsync_ExistingPatient_ReturnsDetail()
    {
        // Arrange
        var mockApi = new Mock<IPatientApi>();
        mockApi.Setup(api => api.GetPatientByIdAsync(It.IsAny<Guid>()))
               .ReturnsAsync(new ApiResponse<PatientDetailDto>(new PatientDetailDto { Id = Guid.NewGuid() }));

        var repository = new PatientRepository(mockApi.Object, Mock.Of<ILogger<PatientRepository>>());

        // Act
        var result = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.NotNull(result);
    }
}
```

### 9.2 DataManager测试

```csharp
public class MedicalCaseDataManagerTests
{
    [Fact]
    public async Task SaveAsync_WithDirtyData_CallsRepositoryOnce()
    {
        // Arrange
        var mockRepo = new Mock<IMedicalCaseRepository>();
        var dataManager = new MedicalCaseDataManager(mockRepo.Object, Mock.Of<ILogger<MedicalCaseDataManager>>());

        await dataManager.LoadAsync(Guid.NewGuid());
        dataManager.UpdateConsultation(new ConsultationInputDto { /* ... */ });

        // Act
        await dataManager.SaveAsync();

        // Assert
        mockRepo.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<MedicalCaseInputDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

### 9.3 CommandHandler测试

```csharp
public class ConsultationCommandHandlerTests
{
    [Fact]
    public void Update_WhenParentLoaded_MarksDirty()
    {
        // Arrange
        var mockDataManager = new Mock<IMedicalCaseDataManager>();
        mockDataManager.Setup(dm => dm.Current).Returns(new MedicalCaseDetailDto());

        var handler = new ConsultationCommandHandler(mockDataManager.Object);

        // Act
        handler.Update(new ConsultationInputDto { /* ... */ });

        // Assert
        mockDataManager.Verify(dm => dm.UpdateConsultation(It.IsAny<ConsultationInputDto>()), Times.Once);
    }
}
```

## 10. 迁移指南

### 10.1 独立实体模块迁移

1. 确保Repository继承RepositoryBase
2. 确保ViewModel依赖IRepository（非IApi）
3. 添加Models层（DetailModel, Item）
4. 更新DI注册

### 10.2 从属实体模块迁移

1. 创建CommandHandler接口和实现
2. 移除直接API调用
3. 更新ViewModel依赖CommandHandler
4. 确保通过父聚合DataManager保存

### 10.3 检查清单

- [ ] Repository实现RepositoryBase
- [ ] DTO命名符合规范
- [ ] Models层结构完整
- [ ] DI注册正确
- [ ] 单元测试覆盖

# client-layer-architecture Spec Delta

## MODIFIED Requirements

### Requirement: CLI-002 Modules层职责

Modules层(8个项目) SHALL 实现业务UI功能。

**标准目录结构**:
```
LYBT.Desktop.{Domain}/
├── {Domain}Module.cs              # Prism模块注册
├── Data/                          # 数据访问层
│   ├── I{Entity}Repository.cs     # Repository接口
│   ├── {Entity}Repository.cs      # Repository实现
│   ├── I{Entity}DataManager.cs    # DataManager接口(聚合根模块)
│   ├── {Entity}DataManager.cs     # DataManager实现(聚合根模块)
│   ├── I{Entity}CommandHandler.cs # CommandHandler接口(从属实体模块)
│   └── {Entity}CommandHandler.cs  # CommandHandler实现(从属实体模块)
├── Models/                        # UI模型
│   ├── {Entity}DetailModel.cs     # Master-Detail UI模型(可编辑)
│   ├── {Entity}ViewState.cs       # 视图状态管理
│   └── Items/
│       └── {Entity}Item.cs        # 列表项模型(只读)
├── Views/                         # XAML视图
│   ├── {Feature}View.xaml
│   └── Dialogs/                   # 弹窗视图
├── ViewModels/                    # ViewModel
│   ├── {Feature}ViewModel.cs
│   ├── Components/                # ViewModel组件 (当ViewModel > 500行时必需)
│   │   └── {Entity}Validator.cs
│   └── Dialogs/                   # 弹窗ViewModel
└── Services/                      # 客户端服务(可选)
```

**模块类型**:

| 类型 | 数据访问层 | 典型模块 |
|------|-----------|----------|
| 独立实体模块 | Repository | Patients, Herbs |
| 聚合根模块 | Repository + DataManager | MedicalCase, Formula |
| 从属实体模块 | CommandHandler (通过父聚合) | Consultation, Prescriptions |

#### Scenario: 创建独立实体模块数据层
- **WHEN** 模块管理独立实体
- **THEN** SHALL 创建Data/目录包含Repository
- **AND** Repository SHALL 继承RepositoryBase<TDetail, TList, TInput>
- **AND** SHALL 注册为Singleton

#### Scenario: 创建聚合根模块数据层
- **WHEN** 模块管理聚合根实体
- **THEN** SHALL 创建Data/目录包含Repository和DataManager
- **AND** DataManager SHALL 管理聚合内子实体状态
- **AND** DataManager SHALL 注册为Scoped

#### Scenario: 创建从属实体模块数据层
- **WHEN** 模块管理聚合内从属实体
- **THEN** SHALL 创建Data/目录包含CommandHandler
- **AND** CommandHandler SHALL 依赖父聚合的DataManager
- **AND** CommandHandler SHALL 注册为Transient

#### Scenario: 创建业务视图
- **WHEN** 需要新增功能界面
- **THEN** SHALL 创建{Feature}View.xaml和{Feature}ViewModel.cs
- **AND** View SHALL 只包含XAML声明
- **AND** ViewModel SHALL 继承UnifiedViewModelBase

#### Scenario: ViewModel需要Components
- **WHEN** ViewModel超过500行
- **THEN** SHALL 创建ViewModels/Components/目录
- **AND** SHALL 提取Validator组件
- **AND** 数据管理组件SHALL在Data/目录

---

### Requirement: CLI-006 模块注册规范

模块 SHALL 通过标准方式注册。

**注册内容**:
- Repository (Singleton) - 独立实体和聚合根模块
- DataManager (Scoped) - 聚合根模块
- CommandHandler (Transient) - 从属实体模块
- ViewModel (Transient)
- View导航 (RegisterForNavigation)
- Dialog (RegisterDialog)

**独立实体模块示例**:
```csharp
public class PatientsModule : IModule
{
    public void OnInitialized(IContainerProvider containerProvider) { }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // Repository (Singleton)
        containerRegistry.RegisterSingleton<IPatientsRepository, PatientsRepository>();

        // ViewModel
        containerRegistry.Register<PatientListViewModel>();

        // View导航
        containerRegistry.RegisterForNavigation<PatientListView>();
    }
}
```

**聚合根模块示例**:
```csharp
public class MedicalCaseModule : IModule
{
    public void OnInitialized(IContainerProvider containerProvider) { }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // Repository (Singleton)
        containerRegistry.RegisterSingleton<IMedicalCaseRepository, MedicalCaseRepository>();

        // DataManager (Scoped)
        containerRegistry.RegisterScoped<IMedicalCaseDataManager, MedicalCaseDataManager>();

        // ViewModel
        containerRegistry.Register<MedicalCaseDetailViewModel>();

        // View导航
        containerRegistry.RegisterForNavigation<MedicalCaseDetailView>();
    }
}
```

**从属实体模块示例**:
```csharp
public class ConsultationModule : IModule
{
    public void OnInitialized(IContainerProvider containerProvider) { }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // CommandHandler (Transient)
        containerRegistry.Register<IConsultationCommandHandler, ConsultationCommandHandler>();

        // ViewModel
        containerRegistry.Register<ConsultationEditViewModel>();

        // View导航
        containerRegistry.RegisterForNavigation<ConsultationEditView>();
    }
}
```

#### Scenario: 注册Repository
- **WHEN** 模块有数据访问需求(独立或聚合根)
- **THEN** SHALL 使用RegisterSingleton注册Repository
- **AND** SHALL 注册接口到实现

#### Scenario: 注册DataManager
- **WHEN** 模块是聚合根模块
- **THEN** SHALL 使用RegisterScoped注册DataManager
- **AND** SHALL 注册接口到实现

#### Scenario: 注册CommandHandler
- **WHEN** 模块是从属实体模块
- **THEN** SHALL 使用Register注册CommandHandler
- **AND** SHALL 依赖父聚合的DataManager接口

#### Scenario: 注册导航视图
- **WHEN** 视图需要参与Region导航
- **THEN** SHALL 使用RegisterForNavigation注册
- **AND** View SHALL 自动关联同名ViewModel

#### Scenario: 注册对话框
- **WHEN** 需要模态对话框
- **THEN** SHALL 使用RegisterDialog注册
- **AND** SHALL 指定View和ViewModel类型

---

## ADDED Requirements

### Requirement: CLI-007 Models层命名规范

Models层 SHALL 遵循统一的命名和结构规范。

**命名规则**:

| 类型 | 命名模式 | 用途 | 位置 |
|------|---------|------|------|
| DetailModel | `{Entity}DetailModel` | Master-Detail UI模型，支持编辑 | Models/ |
| Item | `{Entity}Item` | 列表项模型，只读 | Models/Items/ |
| ViewState | `{Entity}ViewState` | 视图状态管理 | Models/ |

**DetailModel规范**:
```csharp
public class PatientDetailModel : BindableBase
{
    // 从DTO映射的属性
    public Guid Id { get; set; }
    public string Name { get; set; }

    // UI状态属性
    public bool IsModified { get; private set; }

    // 映射方法
    public static PatientDetailModel FromDto(PatientDetailDto dto);
    public PatientInputDto ToInputDto();
}
```

**Item规范**:
```csharp
public class PatientItem : BindableBase
{
    // 只读属性
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string DisplayText { get; init; }

    // 从ListDto映射
    public static PatientItem FromDto(PatientListDto dto);
}
```

**ViewState规范**:
```csharp
public class PatientViewState : BindableBase
{
    public bool IsEditing { get; set; }
    public bool HasChanges { get; set; }
    public string CurrentFilter { get; set; }
}
```

#### Scenario: 创建DetailModel
- **WHEN** 需要可编辑的UI模型
- **THEN** SHALL 创建{Entity}DetailModel.cs
- **AND** SHALL 继承BindableBase
- **AND** SHALL 包含FromDto和ToInputDto方法

#### Scenario: 创建Item模型
- **WHEN** 需要列表展示模型
- **THEN** SHALL 创建Models/Items/{Entity}Item.cs
- **AND** SHALL 使用init属性确保只读
- **AND** SHALL 包含FromDto静态方法

#### Scenario: 创建ViewState
- **WHEN** 需要管理视图状态
- **THEN** SHALL 创建{Entity}ViewState.cs
- **AND** SHALL 包含与业务无关的UI状态属性

---

### Requirement: CLI-008 Data层组件职责

Data层组件 SHALL 遵循清晰的职责划分。

**Repository职责**:
- API调用封装
- 响应缓存(可选)
- 错误转换为领域异常

**DataManager职责**:
- 聚合根状态管理
- 子实体协调
- 乐观并发控制(RowVersion)
- 脏数据追踪

**CommandHandler职责**:
- 单一命令执行
- 委托给父聚合DataManager
- 命令参数验证

#### Scenario: Repository实现API调用
- **WHEN** Repository需要获取数据
- **THEN** SHALL 调用对应的IApi接口
- **AND** SHALL 返回DTO而非实体
- **AND** SHALL 不包含业务逻辑

#### Scenario: DataManager管理聚合状态
- **WHEN** 需要管理聚合根及其子实体
- **THEN** SHALL 使用DataManager
- **AND** SHALL 追踪修改状态
- **AND** SHALL 处理RowVersion并发

#### Scenario: CommandHandler执行命令
- **WHEN** 从属实体需要执行创建/更新/删除
- **THEN** SHALL 使用CommandHandler
- **AND** SHALL 通过父聚合的DataManager协调
- **AND** SHALL 不直接调用API

# Client 端业务模块统一设计标准

> **版本**: 2.4
> **制定日期**: 2025-01-11
> **适用范围**: Desktop WPF 客户端所有业务模块
> **关联 Issue**: #1114, #1119, #1118, #1013, #1151, #1152, #1153

---

## 一、架构概览

### 1.1 分层架构（模块化架构 v2.0）

```
┌─────────────────────────────────────────┐
│           View (XAML)                   │
│     用户界面、数据绑定、样式             │
└───────────────┬─────────────────────────┘
                │ Binding
┌───────────────▼─────────────────────────┐
│         ViewModel                       │
│   UI逻辑、命令、属性、状态管理           │
│   异常处理（ViewModelBase）              │
└───────────────┬─────────────────────────┘
                │ 直接调用（无Service层）
┌───────────────▼─────────────────────────┐
│        Repository                       │
│   数据访问、HTTP调用、ServiceResult封装   │
└───────────────┬─────────────────────────┘
                │ HTTP
┌───────────────▼─────────────────────────┐
│         WebAPI (Server)                 │
│   业务逻辑、数据持久化                   │
└─────────────────────────────────────────┘
```

**架构变更说明（v2.1）**：
- ❌ **移除Service层**：Desktop端不应重复Server端业务逻辑
- ✅ **ViewModel直调Repository**：简化调用链，提升性能
- ✅ **Repository返回裸类型**（v2.1修订）：直接返回DTO或PagedResult，异常通过抛出处理
- ✅ **异常处理在UnifiedViewModelBase**：基类统一捕获Repository异常

### 1.2 模块组织原则（模块化架构）

- **模块 = 垂直切片**：每个模块包含 Models、ViewModels、Views、Repositories
- **职责独立**：每个模块拥有独立的数据访问层（Repositories）
- **水平分层**：技术基础设施（Foundation）、UI基础设施（Presentation）集中管理
- **接口统一**：使用 `Shared.Interfaces.Repositories`

---

## 二、目录结构标准（模块化架构 v2.0）

### 2.1 模块目录结构（强制）

```
LYBT.Desktop.{ModuleName}/
├── Models/                      ✅ UI专用模型
│   ├── {Entity}Item.cs         (列表项模型)
│   ├── {Entity}ViewState.cs    (视图状态)
│   └── {Wizard}Step.cs         (向导步骤枚举)
│
├── ViewModels/                  ✅ 视图模型
│   ├── {Entity}ManagementViewModel.cs  (列表管理)
│   ├── {Entity}DetailViewModel.cs      (详情查看)
│   ├── {Entity}CreateViewModel.cs      (创建)
│   ├── {Entity}EditViewModel.cs        (编辑)
│   └── {Action}DialogViewModel.cs      (对话框)
│
├── Views/                       ✅ XAML视图
│   ├── {Entity}ManagementView.xaml     (+ .xaml.cs)
│   ├── {Entity}DetailView.xaml         (+ .xaml.cs)
│   └── {Action}Dialog.xaml             (+ .xaml.cs)
│
├── Interfaces/                  🆕 v2.2 模块接口目录
│   └── I{Entity}Repository.cs  (Repository接口)
│
├── Repositories/                🆕 模块独立数据访问层
│   └── {Entity}Repository.cs   (Repository实现)
│
├── {ModuleName}Module.cs        ✅ Prism模块注册
└── README.md                    ✅ 模块说明文档
```

**v2.0 关键变更**：
- 🆕 **Repositories/** 目录：每个模块拥有独立的数据访问层
- ❌ **Services/** 目录：已废弃，不再使用Service层

**v2.2 架构调整**：
- 🆕 **Interfaces/** 目录：Repository接口独立目录，对齐Server端标准
- ✅ **Repositories/** 目录：仅包含实现类，不再混合接口

### 2.2 禁止的目录（已废弃）

- ❌ **Mappings/** - AutoMapper配置已废弃（Repository直接返回DTO）
- ❌ **Services/** - Service层已移除

### 2.3 Core 层目录结构

```
Desktop/Core/
├── Desktop.Foundation/          🆕 技术基础设施
│   ├── Caching/
│   ├── Configuration/
│   ├── Diagnostics/
│   ├── ErrorHandling/
│   ├── Http/
│   ├── Performance/
│   ├── Security/
│   ├── Session/
│   ├── Settings/
│   ├── HealthCheck/
│   ├── Modules/
│   ├── Handlers/
│   └── Extensions/
│
├── Desktop.Presentation/        🆕 UI基础设施
│   ├── Navigation/
│   ├── Notifications/
│   ├── Theming/
│   ├── UserExperience/
│   └── Print/
│
├── Desktop.Infrastructure/      ✅ 保留（通用接口与基类）
└── Desktop.Models/              ✅ 保留（共享模型）
```

**说明**：
- `Desktop.Services` 项目已删除
- 技术基础设施迁移至 `Desktop.Foundation`
- UI基础设施迁移至 `Desktop.Presentation`

---

## 三、ViewModel 设计标准

### 3.1 基类选择规则

| 场景 | 基类 | 示例 |
|------|------|------|
| 列表管理 | `UnifiedListViewModelBase<TDto>` | PatientManagementViewModel |
| 详情/单项 | `UnifiedViewModelBase` | PatientDetailViewModel |
| 对话框 | `UnifiedViewModelBase` | ConfirmDialogViewModel |

### 3.2 构造函数依赖注入（强制标准，v2.0）

```csharp
/// <summary>
/// {Entity}{ViewType}ViewModel - {简要描述}
/// </summary>
public XxxViewModel(
    // 1️⃣ Repository依赖（必需，非null）
    IXxxRepository xxxRepository,

    // 2️⃣ 基类必需依赖
    IEventAggregator eventAggregator,
    ILoggerFactory loggerFactory,
    IRegionManager regionManager,

    // 3️⃣ 可选依赖（末尾，使用 = null）
    ISessionManager? sessionManager = null,
    IUserNotificationService? userNotificationService = null)
    : base(eventAggregator, loggerFactory, regionManager,
           sessionManager, userNotificationService)
{
    _xxxRepository = xxxRepository ?? throw new ArgumentNullException(nameof(xxxRepository));
}
```

**依赖顺序规则（v2.0）**：
1. Repository依赖优先（如 IPatientRepository）
2. 基类必需依赖（EventAggregator, LoggerFactory, RegionManager）
3. 可选依赖最后（SessionManager, NotificationService）

**v2.1 关键变更**：
- ❌ 不再注入 `IXxxService`（已废弃Server Service依赖）
- ✅ 直接注入 `IXxxRepository`（模块内数据访问层）
- ❌ 不再注入 `IMapper`（Repository直接返回DTO，无需映射）
- ⚠️ **重要**：禁止使用 `LYBT.Shared.Interfaces.Services.*` 命名空间（会导致DI容器解析失败）

### 3.3 命令命名标准

| 命令类型 | 命名规则 | 示例 |
|---------|---------|------|
| CRUD | `{Action}Command` | `AddCommand`, `EditCommand`, `DeleteCommand`, `SaveCommand` |
| 导航 | `{Direction/Target}Command` | `BackCommand`, `NextCommand`, `GotoPatientCommand` |
| 刷新 | `RefreshCommand` / `LoadDataCommand` | `RefreshCommand` |
| 搜索 | `SearchCommand` / `ClearSearchCommand` | `SearchCommand` |
| 自定义 | `{Verb}{Noun}Command` | `ExportDataCommand`, `ImportPatientsCommand` |

### 3.4 属性命名标准

| 属性类型 | 命名规则 | 示例 |
|---------|---------|------|
| 数据集合 | `Items` | `Items` (列表项) |
| 当前选中 | `SelectedItem` / `CurrentItem` | `SelectedPatient`, `CurrentUser` |
| 状态标志 | `Is{State}` | `IsLoading`, `IsBusy`, `IsReadOnly` |
| 计数 | `{Noun}Count` / `Total{Noun}` | `ItemCount`, `TotalPages` |
| UI文本 | `{Context}Text` | `PageTitle`, `StatusText`, `ErrorMessage` |

### 3.5 ViewModel 示例模板（v2.0）

```csharp
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.{Module}.Interfaces;  // v2.2: Repository接口在独立Interfaces目录
using LYBT.Shared.Models.Contracts.{Module};
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.{Module}.ViewModels
{
    /// <summary>
    /// {Entity}管理视图模型 - 列表管理功能（v2.0 无Service层）
    /// </summary>
    public class {Entity}ManagementViewModel : UnifiedListViewModelBase<{Entity}Dto>
    {
        #region 私有字段

        private readonly I{Entity}Repository _{entity}Repository;

        #endregion

        #region 构造函数

        public {Entity}ManagementViewModel(
            I{Entity}Repository {entity}Repository,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager,
                   sessionManager, userNotificationService)
        {
            _{entity}Repository = {entity}Repository ?? throw new ArgumentNullException(nameof({entity}Repository));

            PageTitle = "{Entity}管理";
            InitializeCustomCommands();
        }

        #endregion

        #region 实现基类抽象方法

        protected override async Task<IEnumerable<{Entity}Dto>> GetItemsAsync(
            int page, int pageSize, string? searchText)
        {
            // v2.1: Repository返回裸类型，异常由UnifiedViewModelBase捕获
            var result = await _{entity}Repository.GetPagedAsync(page, pageSize, searchText);

            if (result != null && result.Items != null)
            {
                TotalCount = result.TotalCount;
                return result.Items;
            }

            return Enumerable.Empty<{Entity}Dto>();
        }

        #endregion

        #region 自定义命令

        private void InitializeCustomCommands()
        {
            // 添加模块特定命令
        }

        #endregion
    }
}
```

**v2.1 关键变更**：
- ❌ 移除 `using LYBT.Shared.Interfaces.Services`（会导致DI解析失败）
- ✅ 新增 `using LYBT.Desktop.{Module}.Repositories`（模块内Repository）
- ❌ 移除 `I{Entity}Service` 依赖（已废弃Server Service层）
- ✅ 新增 `I{Entity}Repository` 依赖（模块数据访问层）
- ✅ Repository返回裸类型（`PagedResult<T>`、`T`），异常通过抛出处理

### 3.6 ViewModel 组件化架构标准（v2.4 新增 - Issue #1153）

#### 3.6.1 组件化触发条件（复杂度阈值）

当 ViewModel 满足以下**任一条件**时，应考虑进行组件化重构：

| 触发条件 | 阈值 | 评估方式 |
|---------|------|---------|
| **代码行数** | ≥ 800 行 | 使用 `wc -l` 统计（含注释和空行） |
| **独立职责数量** | ≥ 4 个 | 识别独立的功能模块（如验证、计算、命令处理、数据管理） |
| **MVP 功能点数** | ≥ 50 个 | 统计 Issue 清单中的功能点数量 |
| **架构对齐需求** | - | 类似模块需要统一架构模式 |

**实际案例（Issue #1153）**：
- `FormulaDetailViewModel`: 672 行 → 触发组件化
- `PatientImportWizardViewModel`: 1079 行 + 6 个独立职责 → 触发组件化
- `PrescriptionDetailViewModel`: 已通过共享组件重构

#### 3.6.2 组件化架构模式

```
ViewModel（协调器，200-300行）
├── Calculator 组件（计算逻辑）
├── Validator 组件（验证逻辑）
├── CommandHandler 组件（命令操作）
└── DataManager 组件（数据管理）
```

**组件职责划分**：

| 组件类型 | 职责 | 典型行数 | 示例 |
|---------|------|---------|------|
| **Calculator** | 业务计算、统计分析、比率计算 | 150-200 | `FormulaCalculator`, `PrescriptionCalculator` |
| **Validator** | 数据验证、业务规则检查、错误收集 | 120-250 | `FormulaValidator`, `PrescriptionValidator` |
| **CommandHandler** | 保存、复制、删除等命令操作 | 150-200 | `FormulaCommandHandler` |
| **DataManager** | 数据加载、刷新、集合管理 | 100-360 | `FormulaDataManager` |
| **Executor** | 异步任务执行、进度报告 | 200-300 | `ImportExecutor` (BackgroundWorker封装) |
| **ProgressReporter** | 进度跟踪、统计汇总 | 100-150 | `ImportProgressReporter` |

#### 3.6.3 共享组件模式（推荐）

对于具有相似业务逻辑的模块（如 Prescription、Formula），优先使用共享组件基类：

**步骤 1：定义共享接口**
```csharp
// LYBT.Shared.Components/IHerbItem.cs
public interface IHerbItem
{
    Guid HerbId { get; }
    string HerbName { get; }
    decimal Dosage { get; }
    string Unit { get; }
    decimal Quantity { get; }
    decimal UnitPrice { get; }
}
```

**步骤 2：创建泛型基类**
```csharp
// LYBT.Shared.Components/HerbCalculatorBase.cs
public abstract class HerbCalculatorBase<TItem> where TItem : IHerbItem
{
    public decimal CalculateTotalDosage(IEnumerable<TItem> items)
    {
        return items?.Sum(i => i.Dosage) ?? 0m;
    }

    public decimal CalculateTotalPrice(IEnumerable<TItem> items)
    {
        return items?.Sum(i => i.UnitPrice * i.Quantity) ?? 0m;
    }

    // ... 更多共享计算逻辑
}
```

**步骤 3：模块特定实现**
```csharp
// LYBT.Desktop.Formula/ViewModels/Components/FormulaCalculator.cs
public class FormulaCalculator : HerbCalculatorBase<FormulaHerbItemViewModel>
{
    // 继承共享逻辑

    // 添加 Formula 特定计算
    public FormulaRatioAnalysis CalculateRatioDistribution(...)
    {
        // Formula 特有的配方比例分析
    }
}
```

**成果（Issue #1153 实际数据）**：
- Prescription 模块：删除 195 行重复代码
- Formula 模块：Calculator 180 行，Validator 150 行（共享基类提供 ~120 行）
- 代码复用率：约 60-70%

#### 3.6.4 组件目录结构

```
LYBT.Desktop.{Module}/
├── ViewModels/
│   ├── Components/                          🆕 v2.4 组件目录
│   │   ├── {Entity}Calculator.cs           (计算组件)
│   │   ├── {Entity}Validator.cs            (验证组件)
│   │   ├── {Entity}CommandHandler.cs       (命令处理组件)
│   │   └── {Entity}DataManager.cs          (数据管理组件)
│   │
│   ├── {Entity}DetailViewModel.cs          (主 ViewModel，协调器)
│   └── {Entity}ManagementViewModel.cs
```

**共享组件位置**：
```
LYBT.Shared.Components/                      🆕 v2.4 共享组件库
├── IHerbItem.cs                            (共享接口)
├── HerbCalculatorBase.cs                   (共享计算基类)
├── HerbValidatorBase.cs                    (共享验证基类)
└── ValidationResult.cs                     (共享验证结果)
```

#### 3.6.5 组件化 ViewModel 示例

```csharp
/// <summary>
/// 配方详情视图模型 - 组件化架构（v2.4）
/// Issue #1153: 从 672 行简化到 280 行
/// </summary>
public class FormulaDetailViewModel : UnifiedViewModelBase
{
    #region 服务依赖与组件

    private readonly IFormulaRepository _formulaRepository;
    private readonly FormulaDataManager _dataManager;
    private readonly FormulaCommandHandler _commandHandler;
    private readonly FormulaCalculator _calculator;
    private readonly FormulaValidator _validator;

    #endregion

    #region 构造函数

    public FormulaDetailViewModel(
        IFormulaRepository formulaRepository,
        IEventAggregator eventAggregator,
        ILoggerFactory loggerFactory,
        IRegionManager regionManager,
        ISessionManager? sessionManager = null,
        IUserNotificationService? userNotificationService = null)
        : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
    {
        _formulaRepository = formulaRepository ?? throw new ArgumentNullException(nameof(formulaRepository));

        // 初始化组件
        var logger = loggerFactory.CreateLogger<FormulaDetailViewModel>();
        _dataManager = new FormulaDataManager(formulaRepository, logger);
        _commandHandler = new FormulaCommandHandler(formulaRepository, logger);
        _calculator = new FormulaCalculator();
        _validator = new FormulaValidator();

        // 初始化命令
        InitializeCommands();
    }

    #endregion

    #region 显示属性（委托给组件）

    /// <summary>
    /// 药材数量 - 委托给 DataManager
    /// </summary>
    public int HerbCount => _dataManager.GetHerbItemCount(HerbItems);

    /// <summary>
    /// 总价格 - 委托给 DataManager
    /// </summary>
    public decimal TotalPrice => _dataManager.CalculateTotalPrice(HerbItems);

    #endregion

    #region 命令实现（委托给组件）

    private async Task SaveAsync()
    {
        if (Formula == null || !ValidateInputs())
            return;

        try
        {
            SetIsBusy(true, "正在保存配方...");

            // 委托给 CommandHandler
            var (success, updatedFormula, errorMessage) = await _commandHandler.SaveFormulaAsync(
                Formula, FormulaName, Effect, Usage, Remark, IsShared, HerbItems);

            if (success && updatedFormula != null)
            {
                Formula = updatedFormula;
                IsEditMode = false;
                await ShowSuccessMessageAsync("配方保存成功");
            }
            else
            {
                await ShowErrorMessageAsync(errorMessage ?? "保存配方失败");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "保存配方时发生异常");
            await ShowErrorMessageAsync("保存配方时发生系统错误，请稍后重试");
        }
        finally
        {
            SetIsBusy(false);
        }
    }

    private async Task LoadDataAsync()
    {
        // 委托给 DataManager
        var (success, formula, errorMessage) = await _dataManager.LoadFormulaAsync(FormulaId);
        
        if (success && formula != null)
        {
            Formula = formula;
        }
        else
        {
            await ShowErrorMessageAsync(errorMessage ?? "加载配方失败");
        }
    }

    #endregion
}
```

**重构效果**：
- **代码行数**: 672 行 → 280 行（减少 58%）
- **组件行数**: Calculator 180 + Validator 150 + CommandHandler 165 + DataManager 360 = 855 行
- **总行数**: 280 + 855 = 1135 行（但职责清晰，可测试性强）
- **独立职责**: 6 个模块 → 2 个（UI 协调 + 命令处理）

#### 3.6.6 组件设计原则

1. **单一职责原则（SRP）**
   - 每个组件只负责一类业务逻辑
   - Calculator 只做计算，不做验证
   - Validator 只做验证，不做数据操作

2. **依赖注入原则**
   - 组件通过构造函数接收依赖（Repository, Logger）
   - 不在组件内部创建依赖对象
   - 支持单元测试 Mock

3. **返回值约定**
   - 使用 Tuple 返回操作结果：`(bool success, T? result, string? errorMessage)`
   - 统一的错误处理模式
   - 避免抛出异常到 ViewModel

4. **无状态设计（推荐）**
   - 组件尽量设计为无状态（Stateless）
   - 状态由 ViewModel 管理
   - 组件方法接收参数，返回结果

5. **线程安全考虑**
   - 异步组件（如 Executor）需要处理线程同步
   - 使用事件向 ViewModel 报告进度
   - ViewModel 负责 UI 线程调度

#### 3.6.7 何时不应组件化

以下情况**不建议**进行组件化：

| 场景 | 原因 | 替代方案 |
|------|------|---------|
| ViewModel < 500 行 | 过度设计，增加复杂度 | 保持简单结构 |
| 职责 < 3 个 | 拆分收益低于成本 | 使用清晰的 region 分组 |
| 逻辑高度耦合 | 强行拆分导致频繁交互 | 重构内部方法，保持内聚 |
| 一次性功能 | 无复用价值 | 保持在 ViewModel 内 |
| 简单 CRUD | 基类已提供足够支持 | 使用 `UnifiedListViewModelBase<T>` |

#### 3.6.8 组件化最佳实践总结

**✅ 推荐做法**：
1. **优先考虑共享组件**：多个模块有相似逻辑时，创建泛型基类
2. **渐进式重构**：先提取最独立的模块（如 Calculator）
3. **保持接口简单**：组件方法参数不超过 5 个
4. **完整的日志记录**：每个组件操作都记录日志
5. **单元测试覆盖**：组件应 100% 可测试

**❌ 避免做法**：
1. **过度拆分**：不要为了拆分而拆分，保持合理粒度
2. **循环依赖**：组件之间不应相互调用
3. **状态泄漏**：组件不应持有 ViewModel 引用
4. **god 组件**：单个组件不应超过 400 行
5. **破坏封装**：不要将 Repository 暴露给外部

### 3.7 ViewModel 数据绑定模式标准（v2.5 新增 - Issue #1260）

#### 3.7.1 两种标准绑定模式

本项目根据ViewModel的**职责**采用两种不同的数据绑定模式，这是基于单一职责原则的合理差异，**不应强制统一**。

**模式A：DTO属性包装模式（DetailViewModel）**

**适用场景**：
- 只读详情查看
- 可切换编辑模式的详情页
- 不需要复杂验证的简单编辑

**核心特征**：
```csharp
public class UserDetailViewModel : UnifiedViewModelBase
{
    // 单一DTO属性
    private UserDto? _user;
    public UserDto? User
    {
        get => _user;
        set => SetProperty(ref _user, value);
    }

    // 可选：计算属性用于格式化显示
    public string StatusText => User?.IsActive == true ? "正常" : "已禁用";
    public int Age => User?.Age ?? 0;
}
```

**View绑定语法**：
```xml
<!-- DTO属性绑定 -->
<TextBlock Text="{Binding User.UserName}" />
<TextBlock Text="{Binding User.RealName}" />
<TextBox Text="{Binding User.PhoneNumber, Mode=TwoWay}" IsReadOnly="{Binding IsReadOnly}" />

<!-- 计算属性绑定 -->
<TextBlock Text="{Binding StatusText}" />
<TextBlock Text="{Binding Age}" />
```

**优点**：
- ✅ 代码量最少（约减少30-40%代码）
- ✅ 数据加载简单：`User = await _repository.GetByIdAsync(id);`
- ✅ 无需属性同步
- ✅ 适合只读或简单编辑场景

**局限**：
- ❌ DTOs在Shared.Models层，无法实现`INotifyDataErrorInfo`
- ❌ 不支持属性级验证（可通过DTO-level验证补充）
- ❌ 无法单独控制属性变更通知

---

**模式B：独立属性模式（Create/EditViewModel）**

**适用场景**：
- 创建新记录表单
- 编辑现有记录表单
- 需要复杂验证的场景
- 需要属性级控制的场景

**核心特征**：
```csharp
public class UserCreateViewModel : UnifiedViewModelBase
{
    // 每个字段独立属性
    private string _userName = "";
    public string UserName
    {
        get => _userName;
        set
        {
            SetProperty(ref _userName, value);
            ValidateProperty(value);  // 属性级验证
        }
    }

    private string _realName = "";
    public string RealName
    {
        get => _realName;
        set
        {
            SetProperty(ref _realName, value);
            ValidateProperty(value);
        }
    }

    private UserRole _selectedRole;
    public UserRole SelectedRole
    {
        get => _selectedRole;
        set => SetProperty(ref _selectedRole, value);
    }

    // 数据收集方法
    private UserCreateDto BuildCreateDto()
    {
        return new UserCreateDto
        {
            UserName = this.UserName,
            RealName = this.RealName,
            Role = this.SelectedRole,
            // ...
        };
    }
}
```

**View绑定语法**：
```xml
<!-- 直接绑定ViewModel属性 -->
<TextBox Text="{Binding UserName, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />
<TextBox Text="{Binding RealName, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />
<ComboBox SelectedValue="{Binding SelectedRole}" />
```

**优点**：
- ✅ 完整的WPF验证支持（INotifyDataErrorInfo）
- ✅ 属性级控制和变更通知
- ✅ 灵活的双向绑定
- ✅ 单元测试友好

**代价**：
- ⚠️ 代码量较大（需要每个属性的getter/setter）
- ⚠️ 需要Load和Build方法同步DTO↔属性

#### 3.7.2 模式选择决策树

```
                           ViewModel职责
                                |
                    ┌───────────┴───────────┐
                    |                       |
              需要验证？                只读/简单显示？
                    |                       |
                ┌───┴───┐                  YES
              YES      NO                   |
                |       |              DTO属性包装模式
        独立属性模式  根据复杂度           (DetailViewModel)
      (Create/Edit)     |
                    ┌───┴───┐
                  复杂    简单
                    |       |
            独立属性模式  DTO属性包装模式
```

#### 3.7.3 实际应用示例

**Users模块标准实现**（已验证，作为参考模板）：

| ViewModel | 模式 | 绑定语法示例 | 代码量 |
|-----------|------|------------|--------|
| `UserDetailViewModel` | DTO属性包装 | `{Binding User.UserName}` | ~280行 |
| `UserCreateViewModel` | 独立属性 | `{Binding UserName}` | ~350行 |
| `UserEditViewModel` | 独立属性 | `{Binding UserName}` | ~380行 |

**Patients模块验证**（与Users保持一致）：

| ViewModel | 模式 | 绑定语法 |
|-----------|------|---------|
| `PatientDetailViewModel` | DTO属性包装 + 计算属性 | `{Binding Patient.IdNumber}` + `{Binding PatientName}` |
| `PatientCreateViewModel` | 独立属性 | `{Binding Name}` |
| `PatientEditViewModel` | 独立属性 | `{Binding Name}` |

#### 3.7.4 常见错误与解决方案

**❌ 错误1：强制统一所有ViewModel使用相同模式**
```csharp
// ❌ 错误：Detail也用独立属性（过度设计）
public class UserDetailViewModel
{
    // 无谓的代码重复
    private string _userName = "";
    public string UserName { get => _userName; set => SetProperty(ref _userName, value); }
    // ... 每个字段都这样写
}
```

**✅ 正确做法：**
```csharp
// ✅ 正确：Detail用DTO包装（简洁高效）
public class UserDetailViewModel
{
    public UserDto? User { get; set; }
    public string StatusText => User?.IsActive == true ? "正常" : "已禁用";
}
```

---

**❌ 错误2：绑定路径不一致**
```xml
<!-- ❌ 错误：DetailView使用错误的绑定路径 -->
<TextBlock Text="{Binding UserName}" />  <!-- 应该是 User.UserName -->

<!-- ❌ 错误：CreateView使用错误的绑定路径 -->
<TextBox Text="{Binding User.UserName}" />  <!-- 应该是 UserName -->
```

**✅ 正确做法：**
```xml
<!-- ✅ DetailView：DTO属性包装模式 -->
<TextBlock Text="{Binding User.UserName}" />
<TextBlock Text="{Binding User.RealName}" />

<!-- ✅ CreateView：独立属性模式 -->
<TextBox Text="{Binding UserName, Mode=TwoWay}" />
<TextBox Text="{Binding RealName, Mode=TwoWay}" />
```

---

**❌ 错误3：DTOs实现INotifyPropertyChanged**
```csharp
// ❌ 错误：尝试让Shared.Models中的DTO实现WPF接口
namespace LYBT.Shared.Models.Contracts.Users
{
    public class UserDto : INotifyPropertyChanged  // ❌ 破坏跨平台兼容性
    {
        // ...
    }
}
```

**✅ 正确做法：**
- Shared.Models中的DTO保持为POCO
- 需要验证时使用独立属性模式
- 不需要验证时使用DTO属性包装模式

#### 3.7.5 设计原则总结

1. **单一职责原则（SRP）**
   - Detail = 展示 → DTO属性包装
   - Create/Edit = 编辑+验证 → 独立属性

2. **最小充分原则**
   - 不要过度设计
   - 够用即好，避免强制统一

3. **架构一致性**
   - 同一模块内的同类ViewModel保持一致
   - 跨模块遵循相同的模式选择原则
   - 参考已验证的Users和Patients模块

4. **约束意识**
   - DTOs在Shared.Models层不能实现WPF接口
   - 这是架构约束，不是设计缺陷
   - 选择模式时考虑这个约束

5. **文档先行**
   - 新模块开发前参考本标准
   - 模式选择有疑问时查阅Users模块实现
   - 保持项目内一致性

#### 3.7.6 检查清单

**DetailViewModel 检查清单**：
- [ ] 使用单一DTO属性（如 `User: UserDto?`）
- [ ] 计算属性仅用于格式化显示
- [ ] View绑定使用 `{Binding User.PropertyName}` 或 `{Binding ComputedProperty}`
- [ ] 可编辑字段使用 `Mode=TwoWay`，只读字段使用 `Mode=OneWay`

**Create/Edit ViewModel 检查清单**：
- [ ] 每个可编辑字段有独立属性
- [ ] 属性setter中调用验证方法
- [ ] 提供 `Build{Entity}Dto()` 方法收集数据
- [ ] 如需加载数据，提供 `LoadFrom(Dto)` 方法
- [ ] View绑定使用 `{Binding PropertyName}`

---

## 四、Repository 层设计标准（v3.0 - Project Standardization 3.0）

### 4.1 RepositoryBase统一架构（新增）

**核心组件**：
- **Client端**: `RepositoryBase<TDto, TCreateDto, TUpdateDto, TApi>` (LYBT.Desktop.Infrastructure.Repositories)
- **Server端**: `BaseRepository<TEntity>` (LYBT.Infrastructure.Repositories) - 已有

**设计目标**：
- ✅ **代码统一**: 所有Repository遵循相同的CRUD模式
- ✅ **减少重复**: 消除各Repository中的重复代码
- ✅ **易于维护**: 集中管理通用逻辑
- ✅ **类型安全**: 泛型约束确保编译时类型安全

### 4.2 Client端RepositoryBase（Task 1.2完成）

**位置**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Repositories/RepositoryBase.cs`

**核心功能**：
```csharp
public abstract class RepositoryBase<TDto, TCreateDto, TUpdateDto, TApi>
    where TApi : class
    where TDto : class
    where TCreateDto : class
    where TUpdateDto : class
{
    // 标准CRUD方法
    public virtual async Task<TDto?> GetByIdAsync(Guid id)
    public virtual async Task<PagedResult<TDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
    public virtual async Task<TDto> CreateAsync(TCreateDto dto)
    public virtual async Task<TDto> UpdateAsync(TUpdateDto dto)
    public virtual async Task<bool> DeleteAsync(Guid id)
    public virtual async Task<List<TDto>> SearchAsync(string keyword)
    
    // 抽象方法 - 子类实现
    protected abstract Task<Refit.ApiResponse<TDto>> CallApiGetByIdAsync(Guid id);
    protected abstract Task<Refit.ApiResponse<PagedResult<TDto>>> CallApiGetPagedAsync(int page, int pageSize, string? keyword);
    protected abstract Task<Refit.ApiResponse<TDto>> CallApiCreateAsync(TCreateDto dto);
    protected abstract Task<Refit.ApiResponse<TDto>> CallApiUpdateAsync(Guid id, TUpdateDto dto);
    protected abstract Task<Refit.ApiResponse<ApiResponse>> CallApiDeleteAsync(Guid id);
    protected abstract Guid? GetIdFromUpdateDto(TUpdateDto dto);
}
```

**已迁移模块**（Task 1.3完成）：
- ✅ ConsultationRepository → RepositoryBase<ConsultationDto, ConsultationCreateDto, ConsultationUpdateDto, IConsultationApi>
- ✅ PatientRepository → RepositoryBase<PatientDto, PatientCreateDto, PatientUpdateDto, IPatientApi>
- ✅ PrescriptionRepository → RepositoryBase<PrescriptionDto, PrescriptionCreateDto, PrescriptionUpdateDto, IPrescriptionApi>
- ✅ FormulaRepository → RepositoryBase<FormulaDto, FormulaCreateDto, FormulaUpdateDto, IFormulaApi>
- ✅ HerbRepository → RepositoryBase<HerbDto, HerbCreateDto, HerbUpdateDto, IHerbApi>
- ✅ MedicalCaseRepository → RepositoryBase<MedicalCaseDto, MedicalCaseCreateDto, MedicalCaseUpdateDto, IMedicalCaseApi>
- ✅ UserRepository → RepositoryBase<UserDto, UserCreateDto, UserUpdateDto, IUserApi>

### 4.3 Server端Specification模式支持（Task 1.4完成）

**Specification接口**:
```csharp
public interface ISpecification<T> where T : class
{
    Expression<Func<T, bool>> Criteria { get; }
    List<Expression<Func<T, object>>> Includes { get; }
    List<string> IncludeStrings { get; }
    List<(Expression<Func<T, object>> KeySelector, bool Ascending)> OrderByClauses { get; }
    Expression<Func<T, object>>? GroupBy { get; }
    (int Skip, int Take)? Pagination { get; }
    bool AsNoTracking { get; }
    bool UseCache { get; }
    int CacheExpirationSeconds { get; }
}
```

**BaseSpecification实现**:
```csharp
public abstract class BaseSpecification<T> : ISpecification<T> where T : class
{
    public Expression<Func<T, bool>> Criteria { get; protected set; }
    public List<Expression<Func<T, object>>> Includes { get; } = new();
    public List<string> IncludeStrings { get; } = new();
    public List<(Expression<Func<T, object>> KeySelector, bool Ascending)> OrderByClauses { get; } = new();
    
    // 构建器方法
    public BaseSpecification<T> OrderBy(Expression<Func<T, object>> keySelector) { /* 实现 */ }
    public BaseSpecification<T> WithInclude(Expression<Func<T, object>> include) { /* 实现 */ }
    public BaseSpecification<T> WithPagination(int pageNumber, int pageSize) { /* 实现 */ }
    public BaseSpecification<T> WithCache(int expirationSeconds = 300) { /* 实现 */ }
}
```

### 4.4 统一依赖注入配置（Task 1.5完成）

**Server端扩展方法**:
```csharp
// 位置: src/Server/Core/LYBT.Infrastructure/DependencyInjection/RepositoryServiceCollectionExtensions.cs
services.AddRepositories();  // 自动扫描注册
services.AddRepository<IUserRepository, UserRepository>();
services.AddRepositorySupportServices();
```

**Client端扩展方法**:
```csharp
// 位置: src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/DependencyInjection/RepositoryContainerRegistryExtensions.cs
containerRegistry.RegisterRepositories();  // 自动扫描注册
containerRegistry.RegisterRepository<IUserRepository, UserRepository>();
containerRegistry.RegisterClientRepositories();
```

**模块示例**（已更新）：
```csharp
// Server端 - UsersModule.cs
services.AddRepository<IUserRepository, UserRepository>();

// Client端 - UsersModule.cs  
containerRegistry.RegisterRepository<IUserRepository, UserRepository>();
```

### 4.5 Repository性能和监控增强（Task 1.4完成）

**新增支持功能**：
- ✅ **Specification查询**: 复杂查询逻辑封装
- ✅ **性能监控**: 查询执行时间跟踪和统计
- ✅ **批量操作**: 高性能批量插入/更新/删除
- ✅ **查询缓存**: 内存缓存支持
- ✅ **类型安全Include**: 编译时类型检查的Include操作
- ✅ **异步流式**: 大数据集的流式处理

**使用示例**:
```csharp
// Specification查询
var spec = new DirectSpecification<Patient>(p => p.IsActive && p.Name.Contains(keyword));
var patients = await patientRepository.FindAsync(spec);

// 缓存查询
var cachedPatients = await patientRepository.FindWithCacheAsync(predicate);

// 批量操作
await patientRepository.BulkInsertAsync(patients);
```

### 4.1 Repository 实现位置（v2.2修订）

- **接口位置**: `Desktop.{Module}/Interfaces/I{Entity}Repository.cs` （v2.2新增独立目录）
- **实现位置**: `Desktop.{Module}/Repositories/{Entity}Repository.cs`
- **命名**: `{Entity}Repository` (如 PatientRepository, UserRepository)
- **原则**: 每个模块拥有独立的Repository，接口与实现分离，对齐Server端标准

### 4.2 构造函数依赖（强制顺序）

```csharp
public PatientRepository(
    IApiClientManager apiClientManager,     // 1️⃣ Foundation层的统一API客户端管理器
    ILogger<PatientRepository> logger)      // 2️⃣ 日志
{
    _apiClient = apiClientManager ?? throw new ArgumentNullException(nameof(apiClientManager));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
}
```

**v2.1 关键变更**：
- ❌ 不再注入 `IMapper`（Repository直接返回DTO）
- ❌ 不再注入 `IExceptionHandler`（异常直接抛出，由ViewModel基类捕获）
- ✅ 注入 `IApiClientManager`（Foundation层统一HTTP客户端，替代直接注入HttpClient）

### 4.3 Repository 方法模板（v2.1修订：返回裸类型）

```csharp
/// <summary>
/// {方法功能描述}
/// </summary>
public async Task<{Entity}Dto> {Method}Async({Request}Dto dto)
{
    _logger.LogInformation($"{操作描述}: {dto}");

    // 1. 调用 Foundation 层的 ApiClient
    var result = await _apiClient.PostAsync<{Entity}Dto>("/api/{entity}/{action}", dto);

    // 2. 直接返回结果（异常由ApiClient抛出，UnifiedViewModelBase捕获）
    return result;
}
```

**v2.1 关键设计原则**：
- ✅ **Repository返回裸类型**：直接返回 `T` 或 `PagedResult<T>`，不封装 `ServiceResult`
- ✅ **异常向上抛出**：不捕获异常，由 UnifiedViewModelBase 统一处理
- ✅ **调用 Foundation 层 ApiClient**：统一的HTTP封装，包含重试、超时等逻辑
- ✅ **Repository直接返回DTO**：无需映射，Server API已返回DTO
- ❌ **Repository不处理业务验证**：业务逻辑在Server端

### 4.4 Repository 返回类型标准（v2.1）

| 场景 | 返回类型 | 说明 |
|------|---------|------|
| 查询单条 | `Task<{Entity}Dto>` | 返回单个实体（裸类型） |
| 查询列表 | `Task<PagedResult<{Entity}Dto>>` | 分页结果（裸类型） |
| 创建 | `Task<{Entity}Dto>` | 返回创建的实体（裸类型） |
| 更新 | `Task<{Entity}Dto>` | 返回更新的实体（裸类型） |
| 删除 | `Task` | 无返回数据（删除成功或抛异常） |

**v2.1 关键变更**：
- ✅ **Repository返回裸类型**：不再封装 `ServiceResult<T>`，直接返回 DTO
- ✅ **错误处理**：异常向上抛出，由 UnifiedViewModelBase 统一捕获
- ❌ **不再使用AutoMapper**：Repository直接从ApiClient获取DTO
- ❌ **不再返回Entity**：Desktop端不使用Entity类型
- ❌ **不再手动映射字段**：Server API已返回标准DTO

### 4.5 DTO 使用规范

**📚 权威参考**: 请参阅 [DTO 设计原则](../dto-design-principles.md) 获取完整的 DTO 设计规范。

**Desktop 端 DTO 使用要点（v2.0）**:

1. **DTO 来源**:
   - ✅ 使用 `Shared.Models.Contracts.*` 中的标准 DTO
   - ❌ 禁止在 Desktop 项目中重复定义 DTO

2. **场景选择**:
   ```csharp
   // ViewModel → Repository (创建场景)
   var createDto = new PatientCreateDto { Name = "张三", ... };
   var result = await _patientRepository.CreateAsync(createDto);

   // ViewModel → Repository (更新场景)
   var updateDto = new PatientUpdateDto { Name = "李四", ... };
   var result = await _patientRepository.UpdateAsync(id, updateDto);

   // Repository → ViewModel (展示场景)
   var patient = result.Data; // PatientDto
   ```

3. **Repository 层数据传输**:
   - Desktop Repository 通过 HTTP 调用 Server API
   - Repository 方法直接返回 DTO（从HTTP响应反序列化）
   - **无需DTO映射**：Server API已返回标准DTO格式

4. **常见错误**:
   - ❌ 在 Desktop 端使用 Entity 类型
   - ❌ 使用 `Guid.Empty` 作为默认值
   - ❌ 混用 CreateDto/UpdateDto/Dto 场景
   - ❌ 在Repository中使用AutoMapper（已废弃）

### 4.6 Repository 示例模板（v2.2修订）

```csharp
using LYBT.Desktop.Foundation.Api;
using LYBT.Desktop.{Module}.Interfaces;  // v2.2: 接口在独立Interfaces目录
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.{Module};
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.{Module}.Repositories
{
    /// <summary>
    /// {Entity}Repository - 数据访问层（v2.1 模块化架构，返回裸类型）
    /// </summary>
    public interface I{Entity}Repository
    {
        Task<PagedResult<{Entity}Dto>> GetPagedAsync(int pageIndex, int pageSize, string? keyword = null);
        Task<{Entity}Dto> GetByIdAsync(Guid id);
        Task<{Entity}Dto> CreateAsync({Entity}CreateDto dto);
        Task<{Entity}Dto> UpdateAsync({Entity}UpdateDto dto);  // ⚠️ dto.Id 在内部赋值
        Task DeleteAsync(Guid id);
    }

    /// <summary>
    /// {Entity}Repository 实现（v2.1 返回裸类型）
    /// </summary>
    public class {Entity}Repository : I{Entity}Repository
    {
        private readonly IApiClientManager _apiClient;
        private readonly ILogger<{Entity}Repository> _logger;
        private const string ApiBase = "/api/{entity}";

        public {Entity}Repository(
            IApiClientManager apiClientManager,
            ILogger<{Entity}Repository> logger)
        {
            _apiClient = apiClientManager ?? throw new ArgumentNullException(nameof(apiClientManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PagedResult<{Entity}Dto>> GetPagedAsync(
            int pageIndex, int pageSize, string? keyword = null)
        {
            _logger.LogInformation("查询{Entity}列表: pageIndex={PageIndex}, pageSize={PageSize}, keyword={Keyword}",
                pageIndex, pageSize, keyword);

            // ✅ 服务端分页：参数通过URL查询字符串传递给Server API
            var query = new PagedQueryBaseDto
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                Keyword = keyword
            };

            // ApiClient 统一处理HTTP请求，异常向上抛出
            return await _apiClient.GetPagedAsync<{Entity}Dto>(ApiBase, query);
        }

        public async Task<{Entity}Dto> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("查询{Entity}详情: id={Id}", id);

            // ApiClient 统一处理HTTP GET请求
            return await _apiClient.GetAsync<{Entity}Dto>($"{ApiBase}/{id}");
        }

        public async Task<{Entity}Dto> CreateAsync({Entity}CreateDto dto)
        {
            _logger.LogInformation("创建{Entity}: {@Dto}", dto);

            // ApiClient 统一处理HTTP POST请求
            return await _apiClient.PostAsync<{Entity}Dto>(ApiBase, dto);
        }

        public async Task<{Entity}Dto> UpdateAsync({Entity}UpdateDto dto)
        {
            _logger.LogInformation("更新{Entity}: {@Dto}", dto);

            // ⚠️ 注意：UpdateDto 需要包含 Id 属性
            // ApiClient 统一处理HTTP PUT请求
            return await _apiClient.PutAsync<{Entity}Dto>($"{ApiBase}/{dto.Id}", dto);
        }

        public async Task DeleteAsync(Guid id)
        {
            _logger.LogInformation("删除{Entity}: id={Id}", id);

            // ApiClient 统一处理HTTP DELETE请求（无返回值）
            await _apiClient.DeleteAsync($"{ApiBase}/{id}");
        }
    }
}
```

**v2.1 关键改进**：
- ✅ **服务端分页**：GetPagedAsync 通过 ApiClient 传递查询参数，由Server端分页
- ✅ **统一API客户端**：使用 Foundation 层的 `IApiClientManager`，统一HTTP调用
- ✅ **返回裸类型**：直接返回 DTO，异常向上抛出
- ✅ **简化错误处理**：不再使用 try-catch 和 ServiceResult，由 UnifiedViewModelBase 统一捕获异常
- ✅ **模块化架构**：接口与实现在同一模块，职责清晰
- ❌ **不再使用AutoMapper**：ApiClient 直接返回DTO

---

## 五、View 层设计标准

### 5.1 XAML 基础结构（强制模板）

```xml
<UserControl x:Class="LYBT.Desktop.{Module}.Views.{Entity}View"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             xmlns:prism="http://prismlibrary.com/"
             prism:ViewModelLocator.AutoWireViewModel="True"
             mc:Ignorable="d"
             d:DesignHeight="700" d:DesignWidth="1200">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />  <!-- 标题栏 -->
            <RowDefinition Height="*" />     <!-- 内容区 -->
        </Grid.RowDefinitions>

        <!-- 标题栏 -->
        <Border Grid.Row="0" Style="{StaticResource TitleBarStyle}" Padding="16">
            <Grid>
                <TextBlock Text="{Binding PageTitle}"
                           FontSize="20" FontWeight="Bold"
                           Foreground="White" />
            </Grid>
        </Border>

        <!-- 内容区 -->
        <ScrollViewer Grid.Row="1" VerticalScrollBarVisibility="Auto">
            <Grid Margin="16">
                <!-- 具体内容 -->
            </Grid>
        </ScrollViewer>

        <!-- 加载遮罩（统一模式） -->
        <Grid Grid.RowSpan="2"
              Visibility="{Binding IsLoading, Converter={StaticResource BooleanToVisibilityConverter}}"
              Background="#80000000">
            <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
                <ProgressBar Width="50" Height="50"
                             IsIndeterminate="True"
                             Margin="0,0,0,16" />
                <TextBlock Text="正在加载..."
                           Foreground="White"
                           HorizontalAlignment="Center" />
            </StackPanel>
        </Grid>
    </Grid>
</UserControl>
```

### 5.2 数据绑定标准

| 绑定类型 | 语法 | 示例 |
|---------|------|------|
| 命令绑定 | `Command="{Binding XxxCommand}"` | `Command="{Binding SaveCommand}"` |
| 双向绑定 | `Mode=TwoWay, UpdateSourceTrigger=PropertyChanged` | `Text="{Binding Name, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"` |
| 只读绑定 | `Mode=OneWay` | `Text="{Binding StatusText, Mode=OneWay}"` |
| 可见性 | `Converter={StaticResource XxxConverter}` | `Visibility="{Binding IsLoading, Converter={StaticResource BooleanToVisibilityConverter}}"` |

### 5.3 样式和资源标准

**资源引用规则**：
- ✅ **样式**: 使用 `{StaticResource XxxStyle}`（应用级样式）
- ✅ **主题资源**: 使用 `{DynamicResource XxxBrush}`（可切换主题）
- ✅ **Converter**: 定义在 `Desktop.Infrastructure/Converters/`
- ❌ **禁止内联样式**（除非确实特殊且有注释说明）

**常用 Converter**：
- `BooleanToVisibilityConverter` - bool → Visibility
- `InverseBooleanToVisibilityConverter` - !bool → Visibility
- `NullToVisibilityConverter` - null检查 → Visibility
- `EnumToStringConverter` - 枚举 → 显示文本

### 5.4 代码后置 (Code-behind) 标准

```csharp
using System.Windows.Controls;

namespace LYBT.Desktop.{Module}.Views
{
    /// <summary>
    /// {Entity}View.xaml 的交互逻辑
    /// </summary>
    public partial class {Entity}View : UserControl
    {
        public {Entity}View()
        {
            InitializeComponent();
            // 仅初始化，不包含任何业务逻辑
            // 所有逻辑必须在 ViewModel 中
        }
    }
}
```

**强制规则**：
- ✅ 代码后置仅包含 `InitializeComponent()`
- ❌ 禁止在代码后置中编写业务逻辑
- ❌ 禁止在代码后置中访问 ViewModel

---

## 六、命名约定

### 6.1 文件命名

| 文件类型 | 命名规则 | 示例 |
|---------|---------|------|
| ViewModel | `{Entity}{ViewType}ViewModel.cs` | `PatientManagementViewModel.cs` |
| View (XAML) | `{Entity}{ViewType}View.xaml` | `PatientDetailView.xaml` |
| Model | `{Entity}{Suffix}.cs` | `PatientItem.cs`, `PatientViewState.cs` |
| Service | `{Entity}Service.cs` | `PatientService.cs` |
| Repository | `{Entity}Repository.cs` | `PatientRepository.cs` |
| Interface | `I{Name}` | `IPatientService.cs` |

### 6.2 ViewType 后缀标准

| ViewType | 用途 | 示例 |
|----------|------|------|
| Management | 列表管理 | PatientManagementViewModel |
| Detail | 详情查看 | PatientDetailViewModel |
| Create | 创建表单 | PatientCreateViewModel |
| Edit | 编辑表单 | PatientEditViewModel |
| Dialog | 对话框 | ConfirmDialogViewModel |

---

## 七、质量检查清单

### 7.1 ViewModel 检查清单

- [ ] 继承正确的基类（`UnifiedViewModelBase` 或 `UnifiedListViewModelBase<TDto>`）
- [ ] 构造函数依赖顺序符合标准
- [ ] 所有必需依赖使用 `?? throw new ArgumentNullException`
- [ ] 可选依赖使用 `= null` 默认值
- [ ] 命令命名符合标准
- [ ] 属性命名符合标准
- [ ] 使用 `async`/`await` 处理异步操作
- [ ] 使用基类的 `ShowErrorMessageAsync` 等方法显示消息
- [ ] 重写 `OnNavigatedTo` 时调用 `base.OnNavigatedTo()`

### 7.2 Repository 检查清单（v2.2修订）

- [ ] ✅ **v2.2**: 接口定义在模块的 `Interfaces/I{Entity}Repository.cs`
- [ ] ✅ **v2.2**: 实现类在模块的 `Repositories/{Entity}Repository.cs`
- [ ] 构造函数依赖顺序符合标准（`IApiClientManager`, `ILogger`）
- [ ] ✅ **所有方法返回裸类型**（如 `Task<T>`, `Task<PagedResult<T>>`, `Task`）
- [ ] ✅ **GetPagedAsync使用服务端分页**（通过ApiClient传递PagedQueryBaseDto）
- [ ] ✅ **UpdateAsync方法签名**：`Task<{Entity}Dto> UpdateAsync({Entity}UpdateDto dto)`，dto包含Id
- [ ] 使用 `_logger` 记录关键操作（使用结构化日志）
- [ ] 调用 Foundation 层的 `IApiClientManager`（GetAsync, PostAsync, PutAsync, DeleteAsync）
- [ ] ❌ 不使用AutoMapper
- [ ] ❌ 不使用 try-catch 封装（异常向上抛出，由ViewModel基类捕获）

### 7.3 View 检查清单

- [ ] 使用 `prism:ViewModelLocator.AutoWireViewModel="True"`
- [ ] 标题栏 + 内容区 + 加载遮罩 三段式结构
- [ ] 命令绑定使用 `{Binding XxxCommand}`
- [ ] 数据绑定指定 `Mode` 和 `UpdateSourceTrigger`
- [ ] 使用 `{StaticResource}` 引用样式
- [ ] 使用 `{DynamicResource}` 引用主题资源
- [ ] 代码后置仅包含 `InitializeComponent()`

### 7.4 目录结构检查清单（v2.2修订）

- [ ] ✅ 有 `Models/`、`ViewModels/`、`Views/`
- [ ] ✅ **v2.2**: 有 `Interfaces/`（包含Repository接口）
- [ ] ✅ 有 `Repositories/`（包含Repository实现）
- [ ] ✅ 有 `{Module}Module.cs` 和 `README.md`
- [ ] ❌ 无 `Mappings/` 目录（已废弃）
- [ ] ❌ 无 `Services/` 目录（已废弃）

---

## 八、迁移指南（v1.0 → v2.0）

### 8.1 从Service层迁移到Repository层

**旧架构（v1.0）**：
```
ViewModel → Service → Repository → WebAPI
```

**新架构（v2.0）**：
```
ViewModel → Repository → WebAPI
```

**迁移步骤**：

#### Step 1：创建模块Repository目录
```bash
# 在模块内创建Repositories目录
mkdir src/Client/Desktop/Modules/LYBT.Desktop.Patients/Repositories
```

#### Step 2：迁移Repository接口和实现
```csharp
// 旧位置: Desktop.Services/Repositories/Interfaces/IPatientRepository.cs
// v2.0 位置: Desktop.Patients/Repositories/IPatientRepository.cs
// v2.2 位置: Desktop.Patients/Interfaces/IPatientRepository.cs （对齐Server端标准）

namespace LYBT.Desktop.Patients.Interfaces  // v2.2: 接口独立目录
{
    public interface IPatientRepository
    {
        // ✅ 返回ServiceResult（而非原始DTO）
        Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(int page, int pageSize, string? keyword);
        Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id);
        Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto);
        Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto dto);
        Task<ServiceResult> DeleteAsync(Guid id);
    }
}
```

#### Step 3：更新ViewModel依赖
```csharp
// ❌ 旧代码（v1.0）
using LYBT.Shared.Interfaces.Services;  // ❌ 会导致DI解析失败

public PatientManagementViewModel(
    IPatientService patientService,  // 删除Service依赖
    ...)
{
    _patientService = patientService;
}

protected override async Task<IEnumerable<PatientDto>> GetItemsAsync(...)
{
    var result = await _patientService.GetPagedAsync(...);
    if (result.IsSuccess && result.Data != null)  // ❌ 旧的ServiceResult模式
    {
        return result.Data.Items;
    }
}

// ✅ 新代码（v2.2）
using LYBT.Desktop.Patients.Interfaces;  // ✅ v2.2: 接口在独立Interfaces目录

public PatientManagementViewModel(
    IPatientRepository patientRepository,  // 直接注入Repository接口
    ...)
{
    _patientRepository = patientRepository;
}

protected override async Task<IEnumerable<PatientDto>> GetItemsAsync(...)
{
    var result = await _patientRepository.GetPagedAsync(...);
    if (result != null && result.Items != null)  // ✅ 直接检查裸类型
    {
        return result.Items;
    }
}
```

#### Step 4：修复P0性能问题（客户端分页→服务端分页）
```csharp
// ❌ 旧代码（PatientService.GetPagedAsync - 客户端分页）
public async Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(...)
{
    var allPatients = await _repository.GetAllAsync();  // ❌ 获取全部10,000条
    allPatients = allPatients.Where(...).ToList();      // 客户端过滤
    var items = allPatients.Skip(...).Take(...);        // 客户端分页
    // ...
}

// ✅ 新代码（Repository.GetPagedAsync - 服务端分页）
public async Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(
    int page, int pageSize, string? keyword)
{
    // ✅ 参数通过查询字符串传递给Server API
    var url = $"/api/patients?page={page}&pageSize={pageSize}";
    if (!string.IsNullOrEmpty(keyword))
        url += $"&keyword={Uri.EscapeDataString(keyword)}";

    var response = await _httpClient.GetAsync(url);
    // Server端分页，仅返回20条
}
```

#### Step 5：删除废弃代码
- 删除 `Desktop.Services/Business/{Entity}Service.cs`
- 删除 `Desktop.Services/Repositories/` 目录
- 删除 `Desktop.Services/Mapping/` 目录
- 最终删除整个 `Desktop.Services` 项目

### 8.2 迁移清单（按模块）

| 模块 | 旧Service位置 | 新Repository位置 | P0修复 |
|------|-------------|----------------|--------|
| Patients | Desktop.Services/Business/PatientService.cs | Desktop.Patients/Repositories/PatientRepository.cs | ✅ 修复GetPagedAsync客户端分页 |
| Users | Desktop.Services/Business/UserService.cs | Desktop.Users/Repositories/UserRepository.cs | ✅ 已正确（参考实现） |
| MedicalCase | Desktop.Services/Business/MedicalCaseService.cs | Desktop.MedicalCase/Repositories/MedicalCaseRepository.cs | - |
| Consultation | Desktop.Services/Business/ConsultationService.cs | Desktop.Consultation/Repositories/ConsultationRepository.cs | - |
| Prescriptions | Desktop.Services/Business/PrescriptionService.cs | Desktop.Prescriptions/Repositories/PrescriptionRepository.cs | - |
| Herbs | Desktop.Services/Business/HerbService.cs | Desktop.Herbs/Repositories/HerbRepository.cs | - |
| Formula | Desktop.Services/Business/FormulaService.cs | Desktop.Formula/Repositories/FormulaRepository.cs | - |
| Auth | Desktop.Services/Business/AuthService.cs | Desktop.Auth/Repositories/AuthRepository.cs | - |

### 8.3 常见问题与解决方案

**Q1: Repository如何处理异常？**
```csharp
// ✅ 使用ServiceResult封装异常
try
{
    var response = await _httpClient.GetAsync(...);
    // ...
    return ServiceResult<T>.Success(result);
}
catch (Exception ex)
{
    _logger.LogError(ex, "操作失败");
    return ServiceResult<T>.Failure($"操作失败: {ex.Message}");
}
```

**Q2: ViewModel如何处理Repository返回的裸类型？（v2.1修订）**
```csharp
// ✅ Repository 返回裸类型，异常由 UnifiedViewModelBase 自动捕获
var result = await _repository.GetPagedAsync(...);

if (result != null && result.Items != null)  // ✅ 直接检查null
{
    TotalCount = result.TotalCount;
    return result.Items;
}

// null 时返回空集合（UnifiedViewModelBase会自动记录警告）
return Enumerable.Empty<PatientDto>();
```

**Q3: 如何确保使用服务端分页？（v2.1修订）**
```csharp
// ✅ 使用 Foundation 层的 ApiClient，传递PagedQueryBaseDto
var query = new PagedQueryBaseDto
{
    PageIndex = pageIndex,
    PageSize = pageSize,
    Keyword = keyword
};
var result = await _apiClient.GetPagedAsync<PatientDto>("/api/patients", query);

// ❌ 不要调用GetAllAsync()再在客户端过滤
var allPatients = await _repository.GetAllAsync();  // 禁止！会导致性能问题
```

**Q4: UpdateAsync 方法为何不再传递 id 参数？（v2.1新增）**
```csharp
// ❌ 旧代码（v2.0）
Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto dto);

// 调用时：
var result = await _repository.UpdateAsync(patient.Id, updateDto);

// ✅ 新代码（v2.1）
Task<PatientDto> UpdateAsync(PatientUpdateDto dto);  // dto.Id 已包含

// 调用时：
updateDto.Id = patient.Id;  // ViewModel 中赋值
var updated = await _repository.UpdateAsync(updateDto);

// 原因：统一模式，避免参数冗余（UpdateDto 本身就应该包含 Id）
```

---

## 九、参考资料

- [DTO 设计原则](../dto-design-principles.md) - 本项目 DTO 设计规范
- [Server Module Design Standard](../server-module-design-standard.md) - Server 端模块设计标准
- [Prism 官方文档](https://prismlibrary.com/)
- [AutoMapper 官方文档](https://docs.automapper.org/)
- [MVVM 设计模式](https://learn.microsoft.com/zh-cn/dotnet/architecture/maui/mvvm)
- [WPF 数据绑定](https://learn.microsoft.com/zh-cn/dotnet/desktop/wpf/data/)

---

## 十、版本历史

| 版本 | 日期 | 修订内容 | 作者 |
|------|------|---------|------|
| 2.5 | 2025-10-13 | **ViewModel 数据绑定模式标准化** - Issue #1260 Users模块设计分析与标准化<br/>- ✅ **新增 3.7 节**：ViewModel数据绑定模式标准<br/>- ✅ **模式A（DTO属性包装）**：DetailViewModel使用单一DTO属性 + 计算属性<br/>- ✅ **模式B（独立属性）**：Create/EditViewModel使用独立属性支持验证<br/>- ✅ **设计原则**：基于单一职责，不强制统一，遵循最小充分原则<br/>- ✅ **参考实现**：Users和Patients模块作为标准模板<br/>- ✅ **决策树与检查清单**：提供模式选择指南和验证标准 | Claude Code |
| 2.2 | 2025-10-11 | **接口位置统一迁移** - Issue #1151 Desktop接口位置对齐Server端标准<br/>- ✅ **Interfaces/ 目录**：Repository接口独立目录（7个模块）<br/>- ✅ **Repositories/ 目录**：仅保留实现类，不再混合接口<br/>- ✅ **架构一致性**：Desktop与Server端接口位置统一<br/>- ✅ **命名空间调整**：`LYBT.Desktop.{Module}.Repositories` → `LYBT.Desktop.{Module}.Interfaces`<br/>- 📦 **影响模块**：Patients, Users, MedicalCase, Consultation, Prescriptions, Herbs, Formula | Claude Code |
| 2.1 | 2025-01-11 | **架构实现修订** - 基于 Issue #1119 Phase 1-4 实际迁移经验修订（Epic #1119）<br/>- ✅ **Repository 返回裸类型**（非 ServiceResult）<br/>- ✅ **UpdateAsync 方法签名调整**（dto 包含 Id，无需额外参数）<br/>- ✅ **IApiClientManager 替代 HttpClient**（Foundation 层统一API客户端）<br/>- ✅ **异常处理模式**：Repository 抛出异常，UnifiedViewModelBase 捕获<br/>- ✅ 更新所有代码示例、检查清单、迁移指南<br/>- ⚠️ 强调禁止使用 `LYBT.Shared.Interfaces.Services.*`（DI 解析失败） | Claude Code |
| 2.0 | 2025-01-09 | **重大架构变更** - 移除Service层，实现模块化架构 (Issue #1114)<br/>- ❌ 删除Desktop.Services项目<br/>- ✅ Repository下沉到各模块<br/>- ✅ 新增Desktop.Foundation/Presentation<br/>- ✅ 修复P0性能问题（服务端分页）<br/>- ❌ 废弃AutoMapper<br/>- 更新所有代码模板与检查清单 | Claude Code |
| 1.1 | 2025-01-09 | 添加 DTO 使用规范章节,引用 DTO 设计原则文档 (Issue #1094) | Claude Code |
| 1.0 | 2025-10-07 | 初始版本，制定统一设计标准 | Claude Code |

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

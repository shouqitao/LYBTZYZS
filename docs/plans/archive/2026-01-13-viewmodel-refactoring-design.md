# ViewModel层重构设计文档

**项目**: LYBTZYZS - 凌隐宝堂中医诊所管理系统
**日期**: 2026-01-13
**状态**: 已确认，待实施
**作者**: Claude Code (Brainstorming Session)

---

## 概述

本文档描述 Desktop 层 ViewModel 架构的全面重构方案，目标是从现有 7 个基类精简到 3 个，采用组合优于继承的设计原则，提升代码可维护性和可测试性。

### 重构目标

- 解决新人上手困难问题
- 降低维护成本
- 提升扩展性
- 清理技术债务

### 选定方案

**方案 B：三层精简继承** - 保留必要的继承层级，将 MasterDetail 能力从继承改为组合服务。

---

## 第一节：新基类体系结构

### 目标架构（从7个基类精简到3个）

```
ViewModelBase (所有ViewModel的根基类)
├── 核心能力：INotifyPropertyChanged、日志、忙状态、错误处理
├── 服务注入：IViewModelServices（聚合服务）
├── 继承：ObservableObject (CommunityToolkit.Mvvm)
│
├── PageViewModelBase : ViewModelBase (页面型ViewModel)
│   ├── 导航能力：INavigationAware、IConfirmNavigationRequest
│   ├── 生命周期：OnNavigatedTo/From、IsNavigationTarget
│   ├── 页面状态：PageTitle、IsLoaded
│   └── 适用场景：所有非对话框页面
│
└── DialogViewModelBase : ViewModelBase (对话框型ViewModel)
    ├── 对话框能力：IDialogAware
    ├── 参数传递：OnDialogOpened、RequestClose
    ├── 结果返回：DialogResult
    └── 适用场景：模态/非模态对话框
```

### 关键变化

1. **MasterDetailViewModelBase 降级为组合服务**，不再是基类
2. **ComposableViewModelBase、UnifiedViewModelBase、NavigableViewModelBase 合并**到 PageViewModelBase
3. **CoreViewModelBase** 重命名为 **ViewModelBase**（更简洁）

---

## 第二节：ViewModelBase 详细设计

### 类定义

```csharp
public abstract partial class ViewModelBase : ObservableObject
{
    // === 服务聚合 ===
    protected IViewModelServices Services { get; }

    // === 便捷属性（从Services解构） ===
    protected ILogger Logger => Services.Logger;
    protected IEventAggregator EventAggregator => Services.EventAggregator;
    protected ISessionManager? SessionManager => Services.SessionManager;

    // === 忙状态管理 ===
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    public bool IsNotBusy => !IsBusy;

    [ObservableProperty]
    private string? _busyMessage;

    // === 错误处理 ===
    [ObservableProperty]
    private string? _errorMessage;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    // === 构造函数 ===
    protected ViewModelBase(IViewModelServices services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    // === 辅助方法 ===
    protected async Task ExecuteWithBusyAsync(Func<Task> action, string? message = null);
    protected void ClearError();
    protected void SetError(string message);
}
```

### 设计要点

- **单一构造函数参数**：只需注入 `IViewModelServices`，简化DI配置
- **CommunityToolkit.Mvvm**：使用 `[ObservableProperty]` 减少样板代码
- **错误处理内置**：统一错误状态管理模式

---

## 第三节：PageViewModelBase 详细设计

### 类定义

```csharp
public abstract partial class PageViewModelBase : ViewModelBase,
    INavigationAware, IConfirmNavigationRequest
{
    // === 页面状态 ===
    [ObservableProperty]
    private string _pageTitle = string.Empty;

    [ObservableProperty]
    private bool _isLoaded;

    // === 导航服务便捷访问 ===
    protected IRegionManager RegionManager => Services.RegionManager;

    // === 构造函数 ===
    protected PageViewModelBase(IViewModelServices services) : base(services) { }

    // === INavigationAware 实现 ===
    public virtual void OnNavigatedTo(NavigationContext context)
    {
        Logger.LogDebug("导航到 {ViewModelType}", GetType().Name);
    }

    public virtual void OnNavigatedFrom(NavigationContext context)
    {
        Logger.LogDebug("离开 {ViewModelType}", GetType().Name);
    }

    public virtual bool IsNavigationTarget(NavigationContext context) => true;

    // === IConfirmNavigationRequest 实现 ===
    public virtual void ConfirmNavigationRequest(
        NavigationContext context, Action<bool> continuationCallback)
    {
        continuationCallback(true); // 默认允许离开
    }

    // === 生命周期钩子（子类重写） ===
    protected virtual Task OnFirstLoadAsync() => Task.CompletedTask;
    protected virtual Task OnRefreshAsync() => Task.CompletedTask;
}
```

### 设计要点

- **完整导航支持**：同时实现 `INavigationAware` 和 `IConfirmNavigationRequest`
- **生命周期分离**：`OnFirstLoadAsync`（首次加载）vs `OnRefreshAsync`（刷新）
- **默认行为合理**：`IsNavigationTarget` 默认复用、`ConfirmNavigationRequest` 默认允许

---

## 第四节：DialogViewModelBase 详细设计

### 类定义

```csharp
public abstract partial class DialogViewModelBase : ViewModelBase, IDialogAware
{
    // === 对话框标题 ===
    [ObservableProperty]
    private string _title = string.Empty;

    // === IDialogAware 实现 ===
    public event Action<IDialogResult>? RequestClose;

    public virtual bool CanCloseDialog() => true;

    public virtual void OnDialogClosed()
    {
        Logger.LogDebug("对话框关闭: {DialogType}", GetType().Name);
    }

    public virtual void OnDialogOpened(IDialogParameters parameters)
    {
        Logger.LogDebug("对话框打开: {DialogType}, 参数数量: {Count}",
            GetType().Name, parameters?.Count ?? 0);
    }

    // === 构造函数 ===
    protected DialogViewModelBase(IViewModelServices services) : base(services) { }

    // === 关闭辅助方法 ===
    protected void CloseDialog(ButtonResult result = ButtonResult.None)
    {
        RequestClose?.Invoke(new DialogResult(result));
    }

    protected void CloseDialog(ButtonResult result, IDialogParameters parameters)
    {
        RequestClose?.Invoke(new DialogResult(result, parameters));
    }

    // === 常用命令 ===
    [RelayCommand]
    protected virtual void Confirm() => CloseDialog(ButtonResult.OK);

    [RelayCommand]
    protected virtual void Cancel() => CloseDialog(ButtonResult.Cancel);
}
```

### 设计要点

- **内置关闭辅助**：`CloseDialog` 方法简化结果返回
- **预置常用命令**：`ConfirmCommand` 和 `CancelCommand` 开箱即用
- **参数传递支持**：`OnDialogOpened` 接收参数，`CloseDialog` 可返回参数
- **日志自动记录**：打开/关闭时自动记录调试日志

---

## 第五节：MasterDetail 组合服务设计

### 从继承到组合的转变

**旧模式（继承）：**
```csharp
public class PatientMasterDetailViewModel : MasterDetailViewModelBase<TList, TDetail>
```

**新模式（组合）：**
```csharp
public class PatientMasterDetailViewModel : PageViewModelBase
{
    private readonly IMasterDetailServices<PatientListDto, PatientDetailModel> _masterDetail;
}
```

### IMasterDetailServices 接口设计

```csharp
public interface IMasterDetailServices<TList, TDetail>
    where TList : class
    where TDetail : class, new()
{
    // === 列表管理 ===
    ObservableCollection<TList> Items { get; }
    TList? SelectedItem { get; set; }
    bool HasSelection { get; }

    // === 详情编辑 ===
    IDetailEditor<TDetail> DetailEditor { get; }
    TDetail? CurrentDetail { get; }
    bool IsEditMode { get; }
    bool IsNew { get; }

    // === 分页 ===
    IPaginationService Pagination { get; }

    // === 加载状态 ===
    ILoadingService Loading { get; }

    // === 错误处理 ===
    IErrorHandler ErrorHandler { get; }

    // === 对话框 ===
    IDialogHelper Dialog { get; }
}
```

### 优势

1. **灵活组合**：页面可选择是否使用 MasterDetail 能力
2. **独立测试**：MasterDetail 逻辑可独立单元测试
3. **职责分离**：基类只管导航，MasterDetail 逻辑由服务承担
4. **复用性强**：同一服务可在不同页面复用

---

## 第六节：现有ViewModel迁移映射

### 基类迁移对照表

| 原基类 | 新基类 | 迁移说明 |
|--------|--------|----------|
| CoreViewModelBase | ViewModelBase | 直接重命名，功能保留 |
| NavigableViewModelBase | PageViewModelBase | 合并导航能力 |
| UnifiedViewModelBase | PageViewModelBase | 合并，移除冗余 |
| ComposableViewModelBase | PageViewModelBase | 合并，移除冗余 |
| MasterDetailViewModelBase | PageViewModelBase + IMasterDetailServices | 继承改组合 |
| DialogViewModelBase | DialogViewModelBase | 保留，微调接口 |
| BindableBase (Prism) | ObservableObject (MVVM Toolkit) | 统一到 Toolkit |

### 24个ViewModel迁移计划

| ViewModel类型 | 数量 | 迁移目标 |
|---------------|------|----------|
| MasterDetail页面 | 5 | PageViewModelBase + IMasterDetailServices |
| 普通页面 | 12 | PageViewModelBase |
| 对话框 | 7 | DialogViewModelBase |

### 迁移顺序（低风险优先）

1. **Phase 1**：创建新基类（不影响现有代码）
2. **Phase 2**：迁移对话框ViewModel（独立性最强）
3. **Phase 3**：迁移普通页面ViewModel
4. **Phase 4**：迁移MasterDetail页面（复杂度最高）
5. **Phase 5**：删除旧基类，清理代码

---

## 第七节：IViewModelServices 接口优化

### 当前接口问题

现有 `IViewModelServices` 包含过多服务，部分服务并非所有ViewModel都需要。

### 优化后的接口设计

```csharp
// === 核心服务（所有ViewModel必需） ===
public interface IViewModelServices
{
    ILoggerFactory LoggerFactory { get; }
    ILogger Logger { get; }  // 便捷属性，基于调用者类型创建
    IEventAggregator EventAggregator { get; }
    ISessionManager? SessionManager { get; }
}

// === 页面服务（PageViewModelBase需要） ===
public interface IPageServices : IViewModelServices
{
    IRegionManager RegionManager { get; }
    INavigationCoordinator NavigationCoordinator { get; }
}

// === 对话框服务（DialogViewModelBase需要） ===
public interface IDialogServices : IViewModelServices
{
    IDialogService DialogService { get; }
}
```

### 实现类

```csharp
public class ViewModelServices : IPageServices, IDialogServices
{
    // 单一实现类，按需注入不同接口
    public ILoggerFactory LoggerFactory { get; }
    public ILogger Logger { get; }
    public IEventAggregator EventAggregator { get; }
    public ISessionManager? SessionManager { get; }
    public IRegionManager RegionManager { get; }
    public INavigationCoordinator NavigationCoordinator { get; }
    public IDialogService DialogService { get; }
}
```

### 优势

- **接口隔离**：不同基类依赖不同接口，职责更清晰
- **单一实现**：DI配置简单，一个类实现所有接口
- **按需注入**：ViewModel只看到需要的服务

---

## 第八节：统一错误处理与验证

### 错误处理策略

```csharp
public abstract partial class ViewModelBase
{
    // === 错误状态 ===
    private readonly Dictionary<string, string> _errors = new();

    [ObservableProperty]
    private string? _errorMessage;  // 主错误信息

    public bool HasError => _errors.Count > 0 || !string.IsNullOrEmpty(ErrorMessage);

    // === 字段级错误 ===
    protected void SetFieldError(string field, string message)
    {
        _errors[field] = message;
        OnPropertyChanged(nameof(HasError));
    }

    protected void ClearFieldError(string field)
    {
        _errors.Remove(field);
        OnPropertyChanged(nameof(HasError));
    }

    protected string? GetFieldError(string field)
        => _errors.TryGetValue(field, out var msg) ? msg : null;

    // === 全局错误 ===
    protected void SetError(string message) => ErrorMessage = message;
    protected void ClearError() => ErrorMessage = null;
    protected void ClearAllErrors()
    {
        _errors.Clear();
        ErrorMessage = null;
        OnPropertyChanged(nameof(HasError));
    }
}
```

### 验证集成（CommunityToolkit.Mvvm）

```csharp
public partial class PatientDetailModel : ObservableValidator
{
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "姓名不能为空")]
    [MaxLength(50, ErrorMessage = "姓名最多50个字符")]
    private string _name = string.Empty;

    // 手动触发验证
    public bool Validate() => !HasErrors;
}
```

### 设计要点

- **两级错误**：字段级（表单验证）+ 全局级（操作失败）
- **利用Toolkit**：`ObservableValidator` 提供 `INotifyDataErrorInfo` 支持
- **UI绑定友好**：`HasError` 可直接绑定到错误提示可见性

---

## 第九节：文件结构与命名规范

### 新目录结构

```
src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/
├── ViewModels/
│   ├── Base/
│   │   ├── ViewModelBase.cs              # 根基类
│   │   ├── PageViewModelBase.cs          # 页面基类
│   │   └── DialogViewModelBase.cs        # 对话框基类
│   │
│   └── Services/
│       ├── IViewModelServices.cs         # 核心服务接口
│       ├── IPageServices.cs              # 页面服务接口
│       ├── IDialogServices.cs            # 对话框服务接口
│       └── ViewModelServices.cs          # 统一实现
│
├── Services/
│   └── MasterDetail/
│       ├── IMasterDetailServices.cs      # MasterDetail服务接口
│       ├── MasterDetailServices.cs       # 实现
│       ├── IDetailEditor.cs              # 详情编辑器接口
│       ├── IPaginationService.cs         # 分页服务接口
│       └── ILoadingService.cs            # 加载状态接口
```

### 命名规范

| 类型 | 命名模式 | 示例 |
|------|----------|------|
| 页面ViewModel | `{Entity}{Action}ViewModel` | `PatientListViewModel` |
| MasterDetail | `{Entity}MasterDetailViewModel` | `HerbMasterDetailViewModel` |
| 对话框ViewModel | `{Entity}{Action}DialogViewModel` | `PatientEditDialogViewModel` |
| 服务接口 | `I{Feature}Service` | `IPaginationService` |
| 服务聚合 | `I{Scope}Services` (复数) | `IViewModelServices` |

### 待删除文件（迁移完成后）

```
# 旧基类（Phase 5 删除）
- CoreViewModelBase.cs
- NavigableViewModelBase.cs
- UnifiedViewModelBase.cs
- ComposableViewModelBase.cs
- MasterDetailViewModelBase.cs (原继承版本)
```

---

## 第十节：测试策略

### 单元测试结构

```
tests/Client/Desktop/
├── LYBT.Desktop.Infrastructure.Tests/
│   ├── ViewModels/
│   │   ├── ViewModelBaseTests.cs         # 基类测试
│   │   ├── PageViewModelBaseTests.cs     # 导航测试
│   │   └── DialogViewModelBaseTests.cs   # 对话框测试
│   │
│   └── Services/
│       ├── MasterDetailServicesTests.cs  # MasterDetail服务测试
│       ├── DetailEditorTests.cs          # 详情编辑器测试
│       └── PaginationServiceTests.cs     # 分页服务测试
```

### 测试重点

```csharp
// === ViewModelBase 测试用例 ===
[Fact] public void IsBusy_SetsCorrectly_UpdatesIsNotBusy()
[Fact] public void SetError_SetsErrorMessage_HasErrorReturnsTrue()
[Fact] public void ExecuteWithBusyAsync_SetsIsBusy_DuringExecution()

// === PageViewModelBase 测试用例 ===
[Fact] public void OnNavigatedTo_LogsNavigation()
[Fact] public void ConfirmNavigationRequest_DefaultAllowsNavigation()
[Fact] public void IsNavigationTarget_DefaultReturnsTrue()

// === MasterDetailServices 测试用例 ===
[Fact] public void SelectItem_UpdatesSelectedItem_RaisesPropertyChanged()
[Fact] public void DetailEditor_LoadDetail_SetsCurrentDetail()
[Fact] public void Pagination_ChangePage_TriggersCallback()
```

### Mock 策略

```csharp
// 使用 NSubstitute 创建服务 Mock
var services = Substitute.For<IViewModelServices>();
services.Logger.Returns(Substitute.For<ILogger>());
services.EventAggregator.Returns(Substitute.For<IEventAggregator>());

var viewModel = new TestViewModel(services);
```

### 覆盖率目标

| 层级 | 目标覆盖率 | 说明 |
|------|-----------|------|
| 基类 | 90%+ | 核心逻辑必须高覆盖 |
| 服务 | 85%+ | 业务逻辑重点测试 |
| 具体ViewModel | 70%+ | 关键路径测试 |

---

## 第十一节：总结与实施计划

### 架构对比

| 维度 | 当前架构 | 新架构 |
|------|----------|--------|
| 基类数量 | 7个 | 3个 |
| 继承层级 | 最深4层 | 最深2层 |
| MasterDetail | 继承 | 组合服务 |
| 服务注入 | 混合模式 | 统一聚合服务 |
| 测试难度 | 高（继承耦合） | 低（组合解耦） |

### 实施阶段（预估工作量）

| Phase | 内容 | 预估时间 |
|-------|------|----------|
| Phase 1 | 创建新基类和服务接口 | 4小时 |
| Phase 2 | 迁移7个对话框ViewModel | 2小时 |
| Phase 3 | 迁移12个普通页面ViewModel | 4小时 |
| Phase 4 | 迁移5个MasterDetail页面 | 4小时 |
| Phase 5 | 删除旧基类、清理代码 | 2小时 |
| Phase 6 | 编写单元测试 | 4小时 |
| **总计** | | **约20小时** |

### 风险与缓解

| 风险 | 缓解措施 |
|------|----------|
| 迁移遗漏导致运行时错误 | 每Phase编译验证 |
| 行为变化导致功能异常 | 保持接口兼容，逐步替换 |
| MasterDetail组合模式学习成本 | 提供迁移示例模板 |

### 成功标准

- 编译通过，0错误0警告
- 所有页面导航正常
- 所有对话框打开/关闭正常
- 单元测试覆盖率达标

---

## 附录：与现有OpenSpec的关系

本设计文档是独立的 ViewModel 重构方案，与现有 `unify-navigation-architecture` OpenSpec 互补：

- `unify-navigation-architecture`：聚焦导航架构统一（ViewNames常量、INavigationCoordinator）
- 本文档：聚焦 ViewModel 基类体系重构（从7个基类精简到3个）

建议执行顺序：
1. 先完成 `unify-navigation-architecture`（导航基础设施）
2. 再执行本文档的 ViewModel 重构（基于新导航架构）

---

**文档版本**: v1.0
**创建日期**: 2026-01-13
**最后更新**: 2026-01-13

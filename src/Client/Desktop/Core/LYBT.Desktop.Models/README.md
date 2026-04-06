# LYBT.Desktop.Models

> ViewModel基类与验证模型 | MVVM核心 | CommunityToolkit.Mvvm + Prism

## 项目定位

- **层级**: Client Core层
- **职责**: 提供MVVM模式的ViewModel基类体系 (CoreViewModelBase / DialogViewModelBase / NavigableViewModelBase)、可验证模型基类 (ValidatableModelBase)、RFC 7807错误响应模型

## 目录结构

```
LYBT.Desktop.Models/
├── Http/
│   └── ProblemDetails.cs                # RFC 7807 标准错误响应模型
└── ViewModels/Base/
    ├── CoreViewModelBase.cs             # 核心ViewModel基类 (ObservableObject)
    ├── DialogViewModelBase.cs           # 对话框ViewModel基类 (IDialogAware)
    ├── NavigableViewModelBase.cs        # 可导航ViewModel基类 (INavigationAware)
    ├── ValidatableModelBase.cs          # 可验证模型基类 (INotifyDataErrorInfo)
    └── ValidationAccessors.cs           # 验证错误索引器 (XAML绑定支持)
```

## 核心组件

| 组件 | 基类 | 说明 |
|------|------|------|
| CoreViewModelBase | ObservableObject (CommunityToolkit.Mvvm) | 状态管理/异步执行/日志/资源释放 |
| DialogViewModelBase | CoreViewModelBase + IDialogAware | 对话框生命周期/参数传递/关闭控制 |
| NavigableViewModelBase | CoreViewModelBase + INavigationAware | 区域导航/会话管理/未保存变更保护 |
| ValidatableModelBase | BindableBase (Prism) + INotifyDataErrorInfo | DataAnnotations验证/XAML错误绑定 |
| ValidationAccessors | -- | 验证错误/状态索引器，支持 `Errors["PropName"]` 绑定 |
| ProblemDetails | -- | RFC 7807 错误响应客户端模型 |

## ViewModel 继承体系

```
ObservableObject (CommunityToolkit.Mvvm)
  └── CoreViewModelBase (状态管理 + 异步执行 + 日志 + IDisposable)
        ├── DialogViewModelBase (IDialogAware + CancelCommand + ConfirmCommand)
        └── NavigableViewModelBase (INavigationAware + IConfirmNavigationRequest)
              └── MasterDetailViewModelBase (在 Infrastructure 层定义)

BindableBase (Prism)
  └── ValidatableModelBase (DataAnnotations + INotifyDataErrorInfo)
        └── 各模块 DetailModel (MedicalCase/Formula/Herb/Patient/User)
```

## 设计依据

- 两套独立继承体系: ViewModel基类基于CommunityToolkit.Mvvm源生成器 (`[ObservableProperty]`)，DetailModel基类基于Prism BindableBase提供验证支持。两者职责不同，不共享继承链
- CoreViewModelBase通过 `IViewModelServices` 聚合服务注入，避免构造函数参数膨胀
- ValidatableModelBase集成INotifyDataErrorInfo，使DataAnnotations验证与WPF绑定引擎无缝协作
- ValidationAccessors提供索引器访问器，支持XAML中 `{Binding Errors[PropertyName]}` 直接绑定验证错误

## 依赖关系

### 依赖
- LYBT.Desktop.Infrastructure (IViewModelServices等服务接口)
- LYBT.Desktop.Contracts (接口定义)
- LYBT.Shared.Models (DTO定义)
- LYBT.Shared.Components (药材业务组件)
- LYBT.Shared.Primitives (基础类型)
- LYBT.Shared.Utilities (工具类)
- CommunityToolkit.Mvvm (源生成器)
- Prism.Core / Prism.Wpf (MVVM框架)
- System.ComponentModel.Annotations (DataAnnotations)
- Microsoft.Extensions.Logging (日志)

### 被依赖
- 所有Desktop业务模块的ViewModel
- 所有Desktop工作站的ViewModel

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 完全重写: 按实际文件结构更新，修正已删除文件 (Exceptions/Mappers/Prescriptions)，更新继承体系为CommunityToolkit.Mvvm |
| 2025-12-04 | 按README规范重写文档 |
| 2025-10-29 | 初始版本 |

## 开发笔记

# LYBT.Desktop.Models 模块说明

## 代码文件结构

```
LYBT.Desktop.Models/
├── Http/
│   └── ProblemDetails.cs                  # RFC 7807 错误响应模型 [疑似死代码]
└── ViewModels/Base/
    ├── CoreViewModelBase.cs               # 核心ViewModel基类 (partial, 336行)
    ├── DialogViewModelBase.cs             # 对话框ViewModel基类 (partial, 249行)
    ├── NavigableViewModelBase.cs          # 可导航ViewModel基类 (partial, 450行)
    ├── ValidatableModelBase.cs            # 可验证模型基类 (166行)
    └── ValidationAccessors.cs            # 验证错误访问器 (39行)
```

### Http/ProblemDetails.cs

- **类型**: `ProblemDetails` (class)
- **职责**: RFC 7807 Problem Details for HTTP APIs 的客户端模型
- **属性**: Type, Title, Status, Detail, Instance, Extensions
- **状态**: 疑似死代码，无外部 using 引用。Infrastructure 层使用独立的 `ProblemDetailsResponse` 类

### ViewModels/Base/CoreViewModelBase.cs

- **类型**: `CoreViewModelBase` (abstract partial class)
- **基类**: `ObservableObject` (CommunityToolkit.Mvvm), `IDisposable`
- **OpenSpec**: enhance-viewmodel-architecture
- **职责**: 所有 ViewModel 的最底层基类，提供最小必要功能
- **构造参数**: `IViewModelServices services`
- **受保护属性**:
  - `Services` (IViewModelServices) - 服务聚合
  - `Logger` (ILogger) - 日志
  - `LoggerFactory` (ILoggerFactory) - 日志工厂
  - `EventAggregator` (IEventAggregator) - 事件总线
  - `Events` (EventSubscriptionManager) - 延迟初始化的事件订阅管理
- **可观察属性** (源生成器):
  - `IsBusy` / `IsNotBusy` - 忙碌状态
  - `StatusMessage` - 状态消息
  - `ErrorMessage` / `HasError` - 错误状态
- **核心方法**:
  - `SetBusy(bool, string?)` - 设置忙碌状态
  - `SetError(string)` / `ClearError()` - 错误管理
  - `ExecuteWithErrorHandlingAsync(Func<Task>, string, ...)` - 异步执行包装 (2个重载，含返回值版本)
  - `RunOnUIThread(Action)` / `RunOnUIThreadAsync(Func<Task>)` - UI 线程调度
  - `AddDisposable(IDisposable)` - 可释放对象管理
  - `OnDisposing()` - 子类清理钩子
- **继承者**: DialogViewModelBase, NavigableViewModelBase

### ViewModels/Base/DialogViewModelBase.cs

- **类型**: `DialogViewModelBase` (abstract partial class)
- **基类**: `CoreViewModelBase`, `IDialogAware` (Prism)
- **OpenSpec**: enhance-viewmodel-architecture
- **职责**: 所有对话框 ViewModel 的基类
- **构造参数**: `IViewModelServices services`
- **可观察属性**: `Title`, `IsLoading` / `IsNotLoading`
- **IDialogAware 实现**: CanCloseDialog(), OnDialogClosed(), OnDialogOpened()
- **关闭方法**:
  - `CloseDialog(ButtonResult)` - 无参关闭
  - `CloseDialog(IDialogParameters, ButtonResult)` - 带参关闭
  - `CloseDialogWithResult<T>(string, T, ButtonResult)` - 泛型结果关闭
- **参数提取**:
  - `GetDialogParameter<T>(IDialogParameters, string)` - 必需参数
  - `GetDialogParameter<T>(IDialogParameters, string, T)` - 可选参数
  - `TryGetDialogParameter<T>(...)` - 尝试获取
- **命令**: `CancelCommand` ([RelayCommand]), `ConfirmCommand` ([RelayCommand(CanExecute)])
- **钩子**: `OnDialogOpenedCore()`, `OnDialogClosedCore()`
- **继承者**: HistoryCopyDialogViewModel, FormulaImportDialogViewModel, SyncConflictDialogViewModel, InputDialogViewModel, MessageDialogViewModel, ConfirmationDialogViewModel

### ViewModels/Base/NavigableViewModelBase.cs

- **类型**: `NavigableViewModelBase` (abstract partial class)
- **基类**: `CoreViewModelBase`, `INavigationAware`, `IRegionMemberLifetime`, `IConfirmNavigationRequest`
- **OpenSpec**: enhance-viewmodel-architecture
- **职责**: 支持 Prism 区域导航的页面 ViewModel 基类
- **构造参数**: `IViewModelServices services`
- **受保护属性**:
  - `RegionManager` (IRegionManager) - 区域管理
  - `SessionManager` (ISessionManager) - 会话管理
  - `UserNotificationService` (IUserNotificationService) - 用户通知
  - `CommonDialogService` (ICommonDialogService) - 通用对话框
  - `RoleRegistry` (IRoleRegistry) - 角色注册表
- **可观察属性**: `PageTitle`, `IsLoading`/`IsNotLoading`, `IsInitialized`, `IsActive`, `HasUnsavedChanges`
- **导航方法**:
  - `NavigateTo(string, string, NavigationParameters?)` - 导航到视图
  - `NavigateBack(string)` - 导航回退
  - `NavigateToHomeCommand` ([RelayCommand]) - 返回主页
- **参数提取**: `GetNavigationParameter<T>()` (3个重载)
- **对话框方法**: `ShowSuccessMessageAsync()`, `ShowErrorMessageAsync()`, `ShowWarningMessageAsync()`, `ShowConfirmMessageAsync()`
- **钩子**: `OnNavigatedToCore()`, `OnNavigatedFromCore()`, `InitializeAsync()`, `CanNavigateAway()`
- **未保存变更**: `MarkAsChanged()` / `MarkAsSaved()`, 自动弹出确认对话框
- **继承者**: LoginViewModel, AdminHomeViewModel, ClinicalHomeViewModel, SystemSettingsViewModel, PatientSelectionViewModel, MedicalCaseWorkspaceViewModel, SyncViewModel

### ViewModels/Base/ValidatableModelBase.cs

- **类型**: `ValidatableModelBase` (abstract class)
- **基类**: `BindableBase` (Prism), `INotifyDataErrorInfo`
- **OpenSpec**: ui-validation-framework
- **职责**: 为 DetailModel 提供 DataAnnotations 验证支持
- **属性**:
  - `Errors` (ValidationErrorsAccessor) - 支持 XAML 索引器绑定 `Errors[PropertyName]`
  - `HasErrorsDictionary` (ValidationHasErrorsAccessor) - 支持 `HasErrorsDictionary[PropertyName]`
  - `HasErrors` (bool) - 是否有验证错误
- **方法**:
  - `SetPropertyAndValidate<T>(ref T, T, string?)` - 设置属性并自动验证
  - `ValidateProperty(string?)` - 验证指定属性 (DataAnnotations)
  - `ValidateAll()` - 验证所有带 ValidationAttribute 的属性
  - `AddValidationError(string, string)` - 添加验证错误
  - `ClearValidationErrors(string?)` - 清除验证错误
- **继承者**: MedicalCaseDetailModel, FormulaDetailModel, HerbDetailModel, PatientDetailModel, UserDetailModel

### ViewModels/Base/ValidationAccessors.cs

- **类型**: `ValidationErrorsAccessor` (class), `ValidationHasErrorsAccessor` (class)
- **OpenSpec**: ui-validation-framework
- **职责**: 验证错误索引器访问器，支持 XAML 绑定 `Errors["PropertyName"]` 和 `HasErrorsDictionary["PropertyName"]`
- **使用方**: ValidatableModelBase (内部依赖)，5个 EditControl 的 XAML 绑定

---

## 死代码与废弃标记

| 文件 | 状态 | 说明 |
|------|------|------|
| `Http/ProblemDetails.cs` | 疑似死代码 | 命名空间 `LYBT.Desktop.Models.Http` 无外部 using 引用。Infrastructure 层有独立的 `ProblemDetailsResponse` 替代 |

**README 中提到但已不存在的文件**:
- `Exceptions/ApiCallException.cs` - 已删除
- `Mappers/SimpleMapper.cs` - 已删除 (被 Mapperly 替代)
- `Prescriptions/PrescriptionTemplate.cs` - 已删除 (Prescriptions 模块已于 2026-01-05 移除)
- `ViewModels/Base/ViewModelBase.cs` - 已删除 (被 CoreViewModelBase 替代)
- `ViewModels/Base/UnifiedViewModelBase.cs` - 已删除
- `ViewModels/Base/UnifiedListViewModelBase.cs` - 已删除

README 内容与实际代码严重不一致，需要更新。

---

## 设计分析

### ViewModel 继承体系

```
ObservableObject (CommunityToolkit.Mvvm)
  └── CoreViewModelBase (状态管理 + 异步执行 + 日志 + 资源释放)
        ├── DialogViewModelBase (对话框生命周期 + IDialogAware)
        └── NavigableViewModelBase (区域导航 + INavigationAware)
              └── MasterDetailViewModelBase (在 Infrastructure 中定义)

BindableBase (Prism)
  └── ValidatableModelBase (DataAnnotations 验证 + INotifyDataErrorInfo)
        └── 各模块 DetailModel
```

### 两种基类体系的设计考量

- **CoreViewModelBase 体系**: 基于 CommunityToolkit.Mvvm 的 `ObservableObject`，使用源生成器 `[ObservableProperty]`，面向 ViewModel
- **ValidatableModelBase 体系**: 基于 Prism 的 `BindableBase`，面向可编辑的 Model (DetailModel)，提供验证支持

两者独立继承是有意设计: ViewModel 不需要 DataAnnotations 验证，DetailModel 不需要导航/对话框能力。

### 服务聚合模式

所有 ViewModel 基类通过 `IViewModelServices` 聚合服务注入，避免构造函数参数膨胀。

---

## 已知陷阱

1. **源生成器属性**: CoreViewModelBase, DialogViewModelBase, NavigableViewModelBase 均为 `partial class`，使用 CommunityToolkit.Mvvm 源生成器。修改时需注意 `[ObservableProperty]` 生成的属性名 (去掉前缀 `_` 并首字母大写)
2. **ConfirmCommand CanExecute 联动**: DialogViewModelBase 的 `ConfirmCommand` 在 `IsBusy` 和 `IsLoading` 变更时自动 `NotifyCanExecuteChanged()`，新增影响可执行状态的属性时需同步
3. **NavigableViewModelBase 首次初始化**: `InitializeAsync` 仅在首次导航时通过 `Dispatcher.InvokeAsync` 异步执行，非首次导航不会重复调用
4. **ValidatableModelBase 反射开销**: `ValidateAll()` 使用反射获取所有带 `ValidationAttribute` 的属性，频繁调用可能有性能影响
5. **ProblemDetails 命名冲突**: Desktop.Models 和 Infrastructure 各有一个 ProblemDetails 相关类型，注意不要混淆

---

最后更新: 2026-03-01

# enhance-viewmodel-architecture

## 概述

深化ViewModel架构，实现100%统一性：引入IViewModelServices服务聚合、统一所有可导航ViewModel继承结构、消除AccountSettingsViewModel特例。

**核心原则**:
1. **能统一就统一** - 消除所有特例
2. **能简单就不复杂** - KISS原则
3. **架构优秀为准** - 不允许特例存在

## 问题分析

### 问题1: AccountSettingsViewModel是唯一特例

当前状态:
- 6个ViewModel继承NavigableViewModelBase
- 1个ViewModel (AccountSettingsViewModel) 继承CoreViewModelBase + 手动INavigationAware

**违反原则**: 同一类型的可导航ViewModel存在两套实现，破坏架构一致性。

### 问题2: 构造函数参数过多

```csharp
// 当前: 7个参数，违反"不超过5个参数"最佳实践
protected NavigableViewModelBase(
    ILoggerFactory loggerFactory,           // 1
    IEventAggregator eventAggregator,       // 2
    IRegionManager regionManager,           // 3
    ISessionManager? sessionManager,        // 4
    IUserNotificationService? userNotificationService,  // 5
    ICommonDialogService? commonDialogService,          // 6
    IRoleRegistry? roleRegistry)            // 7
```

**问题**:
- 每个子类都需要传递多个基类参数
- 新增服务需要修改所有子类构造函数
- DI配置繁琐

### 问题3: MasterDetailViewModelBase继承链不一致

当前状态:
- MasterDetailViewModelBase直接继承ObservableObject (而非CoreViewModelBase)
- 使用组合模式IMasterDetailServices
- 重复定义Logger属性

**问题**: 与其他ViewModel基类继承模式不一致

## 目标架构

### 1. IViewModelServices服务聚合

```csharp
/// <summary>
/// ViewModel服务聚合接口 - 统一服务注入
/// </summary>
public interface IViewModelServices
{
    ILoggerFactory LoggerFactory { get; }
    IEventAggregator EventAggregator { get; }
    IRegionManager RegionManager { get; }
    ISessionManager SessionManager { get; }
    IUserNotificationService UserNotificationService { get; }
    ICommonDialogService CommonDialogService { get; }
    IRoleRegistry RoleRegistry { get; }
}
```

### 2. 统一继承结构

```
ObservableObject
|
+-- CoreViewModelBase
|   |  构造函数: (IViewModelServices services)
|   |  提供: Logger, IsBusy, ErrorMessage, EventAggregator, Services属性
|   |
|   +-- MainWindowViewModel [Shell容器, 不参与导航]
|   |
|   +-- DialogViewModelBase : IDialogAware
|   |   |  构造函数: (IViewModelServices services)
|   |   |
|   |   +-- ApiConnectionFailedDialogViewModel
|   |   +-- ConfirmationDialogViewModel
|   |   +-- EntityAuditLogDialogViewModel
|   |
|   +-- NavigableViewModelBase : INavigationAware, IConfirmNavigationRequest, IRegionMemberLifetime
|   |   |  构造函数: (IViewModelServices services)
|   |   |  提供: PageTitle, IsLoading, HasUnsavedChanges, 导航方法, 对话框方法
|   |   |
|   |   +-- AdminHomeViewModel
|   |   +-- ClinicalHomeViewModel
|   |   +-- PatientSelectionViewModel
|   |   +-- LoginViewModel
|   |   +-- SystemSettingsViewModel
|   |   +-- MedicalCaseWorkspaceViewModel
|   |   +-- AccountSettingsViewModel [统一后]
|   |
|   +-- MasterDetailViewModelBase<TList,TDetail> : INavigationAware, IRegionMemberLifetime
|       |  构造函数: (IViewModelServices services, IMasterDetailServices<T1,T2> masterDetailServices)
|       |
|       +-- FormulaMasterDetailViewModel
|       +-- HerbMasterDetailViewModel
|       +-- MedicalCaseMasterDetailViewModel
|       +-- PatientMasterDetailViewModel
|       +-- UserMasterDetailViewModel
|
+-- HerbItemViewModelBase [模块专用, 不变]
```

## 变更清单

### Phase 1: 创建IViewModelServices聚合

| 任务 | 文件 | 变更 |
|------|------|------|
| 1.1 | `Contracts/Services/IViewModelServices.cs` [NEW] | 创建服务聚合接口 |
| 1.2 | `Infrastructure/Services/ViewModelServices.cs` [NEW] | 实现服务聚合类 |
| 1.3 | `Shell/Extensions/ServiceCollectionExtensions.cs` | 注册IViewModelServices |

### Phase 2-4: 重构基类构造函数

| 任务 | 文件 | 变更 |
|------|------|------|
| 2.1 | `CoreViewModelBase.cs` | 构造函数改为(IViewModelServices) |
| 3.1 | `DialogViewModelBase.cs` | 构造函数改为(IViewModelServices) |
| 3.2 | Dialog子类 (3个) | 更新构造函数 |
| 4.1 | `NavigableViewModelBase.cs` | 构造函数改为(IViewModelServices) |

### Phase 5: 重构NavigableViewModel子类

| 任务 | 影响范围 | 变更 |
|------|----------|------|
| 5.1-5.6 | 6个NavigableViewModel子类 | 构造函数改为(IViewModelServices, ...) |

### Phase 6-7: 重构MasterDetailViewModelBase

| 任务 | 文件 | 变更 |
|------|------|------|
| 6.1 | `MasterDetailViewModelBase.cs` | 改为继承CoreViewModelBase，添加IViewModelServices |
| 7.1-7.5 | 5个MasterDetail子类 | 更新构造函数 |

### Phase 8: 迁移AccountSettingsViewModel (消除特例)

| 任务 | 文件 | 变更 |
|------|------|------|
| 8.1 | `AccountSettingsViewModel.cs` | 改为继承NavigableViewModelBase |
| 8.2 | 移除手动INavigationAware实现 | 使用基类OnNavigatedToCore/OnNavigatedFromCore钩子 |

### Phase 9: 重构MainWindowViewModel

| 任务 | 文件 | 变更 |
|------|------|------|
| 9.1 | `MainWindowViewModel.cs` | 构造函数改为(IViewModelServices, ...) |

### Phase 10: 最终验证

- 全量编译验证
- 功能测试
- 架构验证 (Grep确认无遗留模式)

## 不做的事情 (KISS)

以下优化推迟，当前不实施:

| 优化项 | 推迟原因 | 触发条件 |
|--------|----------|----------|
| IViewModel接口 | 无单元测试需求 | 编写ViewModel单元测试时 |
| INavigableViewModel接口 | 同上 | 同上 |
| IDialogViewModel接口 | 同上 | 同上 |
| IMasterDetailViewModel接口 | 同上 | 同上 |
| INavigationService抽象 | Prism耦合可接受 | 替换Prism框架时 |

## 预期收益

| 指标 | 改进前 | 改进后 | 收益 |
|------|--------|--------|------|
| 基类构造函数参数 | 2-7个 | 1个 | **统一** |
| 子类构造函数参数 | N+3到N+7 | N+1 | **-2到-6个** |
| 架构统一性 | 92% | 100% | **完全统一** |
| 特例数量 | 1个 | 0个 | **消除** |
| ContainerLocator使用 | 2处 | 0处 | **消除** |

## 风险评估

| 风险 | 级别 | 缓解措施 |
|------|------|----------|
| 大范围重构 | 中 | 分Phase执行，每Phase编译验证 |
| DI配置变更 | 低 | ViewModelServices单例注册 |
| MasterDetail继承变更 | 中 | 保留组合模式，仅改继承链 |
| AccountSettings功能变化 | 低 | OnNavigatedToCore钩子保持行为一致 |

## 执行顺序

1. **Phase 1**: 创建IViewModelServices (最小影响，可回滚)
2. **Phase 2**: 重构CoreViewModelBase (核心变更)
3. **Phase 3**: 重构DialogViewModelBase + 子类
4. **Phase 4**: 重构NavigableViewModelBase
5. **Phase 5**: 重构Navigable子类
6. **Phase 6**: 重构MasterDetailViewModelBase
7. **Phase 7**: 重构MasterDetail子类
8. **Phase 8**: 迁移AccountSettingsViewModel (消除特例)
9. **Phase 9**: 重构MainWindowViewModel
10. **Phase 10**: 最终验证

## 依赖关系

- **前置**: `refactor-viewmodel-base-classes` (已完成)
- **后续**: 单元测试编写可利用新接口(推迟)

---

**提案状态**: 待确认
**预估工时**: 6-8小时
**影响范围**: Desktop全部ViewModel (~23个文件)

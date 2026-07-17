# LYBT.Desktop.Infrastructure

> WPF Desktop 客户端基础设施层 — ViewModel 基类、角色系统、服务聚合、HTTP 管道、日志配置、XAML 行为

## 项目定位

| 维度 | 说明 |
|------|------|
| **层级** | Core（Desktop 分层架构第三层，被 Modules/Roles/Shell 引用） |
| **职责** | 为所有 Desktop 模块提供 ViewModel 基类体系、可复用服务（分页/搜索/选择/加载状态/会话/通知）、角色注册、HTTP 请求管道、Serilog 日志配置、XAML 附加行为、全局常量 |
| **状态** | Active |

## 目录结构

```
LYBT.Desktop.Infrastructure/
├── Behaviors/              # XAML 附加行为（DataGrid/PasswordBox/响应式布局）
├── Commands/               # 全局 CompositeCommand
├── Configuration/          # DI 配置扩展
├── Constants/              # ViewNames / RegionNames / SystemConstants / CommonOptions
├── DependencyInjection/    # ViewModelServicesExtensions
├── Events/                 # Prism 事件定义 + EventSubscriptionManager
├── Extensions/             # TaskExtensions (SafeFireAndForget)
├── Http/                   # DelegatingHandler 链 + ApiResponseHelper + ProblemDetails
├── Interfaces/             # 基础设施层自有接口（IClinicSettingsService 等）
├── Logging/                # DesktopSerilogConfiguration
├── Models/
│   ├── Options/            # DisplayOptions / PaginationOptions
│   └── State/              # LoadingState / PaginationState / SearchState
├── Performance/            # PerformanceMonitor
├── Repositories/           # RepositoryBase<T> 抽象 CRUD
├── Roles/
│   ├── Definitions/        # AdminRole / DoctorRole / ReceptionistRole / SuperAdminRole
│   ├── RoleDefinitionBase.cs
│   └── RoleRegistry.cs
├── Security/               # SensitiveInfoFilter (正则脱敏)
├── Services/               # 20+ 服务实现（见核心组件）
│   ├── Interfaces/         # 服务接口（IAsyncExecutor / IErrorHandler 等）
│   ├── Notifications/      # NotificationService
│   └── Toast/              # ToastService
├── ViewModels/
│   ├── Base/               # CoreVM / NavigableVM / DialogVM / ValidatableModel / HerbItemVM
│   ├── Composition/        # ChildViewModelBase (复合 VM 模式)
│   ├── Handlers/           # BaseStatusHandler<T> (Restore/Toggle 模板方法)
│   ├── MasterDetailViewModelBase.cs
│   └── UnfinishedCaseDialogViewModel.cs
├── Views/                  # UnfinishedCaseDialog.xaml
└── Windows/                # BaseDialogWindow.xaml
```

## 核心组件

### Roles/

**RoleDefinitionBase** — 角色定义抽象基类
- **设计依据**: 所有角色共享 AuthModule + UsersModule，各角色通过 `RequiredModules` 声明额外模块；`GetAllModules()` 合并去重
- **实现**: `IRoleDefinition` 接口（来自 Contracts 层）

| 成员 | 说明 |
|------|------|
| `Role` (abstract) | UserRole 枚举值 |
| `DisplayName` / `Description` | 角色显示名称 / 描述 |
| `HomeViewName` (abstract) | 该角色的默认主页视图名 |
| `RequiredModules` (abstract) | 角色专属模块列表 |
| `BaseModules` | 固定 `["AuthModule", "UsersModule"]` |
| `GetAllModules()` | `BaseModules.Concat(RequiredModules).Distinct()` |

**RoleRegistry** — 角色注册表（线程安全）
- **设计依据**: `ConcurrentDictionary<UserRole, IRoleDefinition>` 保证并发注册/查询安全；未注册角色 fallback 到 `ClinicalHome`

| 成员 | 说明 |
|------|------|
| `Register(IRoleDefinition)` | 注册角色定义，重复注册跳过并警告 |
| `GetDefinition(UserRole)` | 按角色查询定义，返回 `null` 表示未注册 |
| `GetHomeViewName(UserRole)` | 获取角色主页视图名，未注册时 fallback 到 `ClinicalHome` |
| `GetModulesForRole(UserRole)` | 获取角色可用模块列表 |
| `GetAllDefinitions()` | 返回所有已注册定义的只读集合 |

具体角色定义: `AdminRoleDefinition` / `DoctorRoleDefinition` / `ReceptionistRoleDefinition` / `SuperAdminRoleDefinition`

---

### Services/

**ViewModelServices** — 9 服务聚合（`IViewModelServices` 实现）
- **设计依据**: 所有 ViewModel 基类仅依赖单一 `IViewModelServices`，避免构造函数膨胀

| 聚合服务 | 类型 | 说明 |
|----------|------|------|
| `LoggerFactory` | `ILoggerFactory` | 日志工厂 |
| `EventAggregator` | `IEventAggregator` | Prism 事件聚合器 |
| `RegionManager` | `IRegionManager` | Prism 区域管理器 |
| `SessionManager` | `ISessionManager` | 会话管理 |
| `UserNotificationService` | `IUserNotificationService` | 用户通知 |
| `CommonDialogService` | `ICommonDialogService` | 对话框服务 |
| `ToastService` | `IToastService` | Toast 消息 |
| `RoleRegistry` | `IRoleRegistry` | 角色注册表 |
| `UiThreadDispatcher` | `IUiThreadDispatcher` | UI 线程调度 |

**WpfUiThreadDispatcher** — WPF Dispatcher 抽象
- **设计依据**: 先 `CheckAccess()` 判断当前线程，已在 UI 线程则直接执行，否则 `Invoke`/`InvokeAsync`

| 方法 | 说明 |
|------|------|
| `Invoke(Action)` | 同步 UI 线程执行 |
| `Invoke<T>(Func<T>)` | 同步 UI 线程执行（带返回值） |
| `InvokeAsync(Action)` | 异步 UI 线程执行 |
| `InvokeAsync<T>(Func<T>)` | 异步 UI 线程执行（带返回值） |
| `BeginInvoke(Action)` | 异步 fire-and-forget |
| `CheckAccess()` | 是否在 UI 线程 |

**ApiRouter** — API 连接路由（只读 Facade）
- **设计依据**: 包装 `IConnectionSettingsService`，暴露 `CurrentUrl` 和 `IsLocal` 供消费方查询

**ConnectionSettingsService** — 连接 URL 管理
- **设计依据**: URL 驱动模式切换；`localhost:5300` = Local（内嵌 LocalWebAPI），其他 = Remote；持久化到 `appsettings.json`

| 成员 | 说明 |
|------|------|
| `LocalUrlConstant` | 固定 `http://localhost:5300` |
| `CurrentUrl` | 根据 `PreferredMode` 返回本地或远程 URL |
| `IsLocal` | 当前是否本地连接 |
| `SetUrlAsync(string)` | 设置 URL 并自动判断 Local/Remote 模式 |
| `SaveRemoteUrlAsync(string)` | 保存远程 URL |
| `SavePreferredModeAsync(string)` | 保存首选模式（Local/Remote） |
| `UrlChanged` event | URL 变更通知 |

**SessionManager** — 会话管理器
- **设计依据**: 包装 `IAuthenticationService`；`CurrentUser` 使用 `lock` + 同步方法避免 WPF 死锁

| 成员 | 说明 |
|------|------|
| `CurrentUser` | 当前登录用户（缓存 + lock 保护） |
| `IsAuthenticated` | 是否已认证 |
| `SetSession(user, token)` | 设置会话 |
| `ClearSession()` | 清除会话，触发 `SessionChanged` + `SessionExpired` |
| `HasPermission(UserRole)` | 权限检查（枚举值比较） |

**CommonDialogService** — 通用对话框
- **设计依据**: 封装 Prism `IDialogService` + `MessageBox`，统一确认/成功/错误对话框接口

**ToastService** — 轻量级 Toast 消息
- **设计依据**: 使用 AdornerLayer 在窗口顶部显示非阻塞消息；失败时 fallback 到 MessageBox

| 方法 | 说明 |
|------|------|
| `ShowInfo/Success/Warning/Error` | 按类型显示 Toast |
| `Show(message, type, duration)` | 自定义持续时间（默认 3000ms，Error 4000ms） |

**PaginationService** — 分页服务（可观察）
- **设计依据**: `ObservableObject` + `[ObservableProperty]` 自动通知 UI；`PageSize` 变更时自动调整 `CurrentPage` 保持位置

| 成员 | 说明 |
|------|------|
| `CurrentPage` / `PageSize` / `TotalCount` | 分页状态 |
| `TotalPages` | 自动计算 `Math.Ceiling(TotalCount / PageSize)` |
| `GoToFirstPage/PreviousPage/NextPage/LastPage` | 导航方法 |
| `PageChanged` event | 翻页事件 |
| `Reset()` | 重置到第 1 页 |

**SearchService** — 搜索服务（300ms 防抖）
- **设计依据**: `CancellationTokenSource` 实现防抖；取消旧搜索 → 延迟 → 执行新搜索

| 成员 | 说明 |
|------|------|
| `SearchText` | 搜索文本 |
| `IsSearching` | 是否正在搜索 |
| `DebounceDelay` | 防抖延迟（默认 300ms） |
| `ExecuteSearchAsync(Func<string, Task>)` | 防抖搜索 |
| `ExecuteSearchImmediateAsync(...)` | 立即搜索（取消防抖） |
| `ClearSearch()` / `CancelSearch()` | 清除/取消 |

**SelectionService<T>** — 选择服务（单选/多选）
- **设计依据**: 支持单选和多选模式切换；`ToggleSelection` 在多选模式下切换选中状态

| 成员 | 说明 |
|------|------|
| `SelectedItem` | 当前选中项 |
| `SelectedItems` | 多选集合 |
| `IsMultiSelectMode` | 多选模式开关 |
| `Select(T)` / `SelectMultiple(IEnumerable<T>)` / `ToggleSelection(T)` | 选择操作 |
| `SelectionChanged` event | 选择变更事件 |

**LoadingStateManager** — 加载状态管理（嵌套计数器）
- **设计依据**: 内部 `_loadingCount` 计数器支持嵌套加载；只有当计数归零时 `IsLoading` 才变为 `false`

| 成员 | 说明 |
|------|------|
| `IsLoading` / `IsBusy` / `BusyMessage` | 可观察状态 |
| `BeginLoading(message)` | 增加计数 |
| `EndLoading()` | 减少计数，归零时清除状态 |
| `ExecuteWithLoadingAsync(...)` | 包装异步操作自动管理状态 |
| `Reset()` | 强制重置所有状态 |

**ErrorHandler** — 错误处理服务
- **设计依据**: 实现 `INotifyDataErrorInfo`，支持按属性名管理验证错误；`HandleException` 统一异常转错误消息

| 成员 | 说明 |
|------|------|
| `ErrorMessage` | 全局错误消息 |
| `HasErrors` | 是否有错误 |
| `HandleException(ex, context)` | 处理异常并设置错误消息 |
| `SetError/SetErrors/ClearError/ClearAllErrors` | 按属性管理错误 |
| `ValidateProperty/ValidateAll` | DataAnnotations 验证 |

**AsyncExecutor** — 异步执行器（重试 + 超时 + UI 线程）
- **设计依据**: 封装安全执行、递增延迟重试、超时控制、UI 线程调度

| 方法 | 说明 |
|------|------|
| `ExecuteSafelyAsync(...)` | 安全执行（吞异常） |
| `ExecuteWithRetryAsync(...)` | 重试执行（默认 3 次，递增延迟） |
| `ExecuteWithTimeoutAsync(...)` | 超时控制执行 |
| `ExecuteOnUIThread/ExecuteOnUIThreadAsync` | UI 线程执行 |

**ApplicationTickService** — 1 秒心跳服务
- **设计依据**: 单一 `DispatcherTimer`，每秒触发 `Tick` 事件，供 `UserActivityTracker` 等订阅

| 成员 | 说明 |
|------|------|
| `Start()` / `Stop()` | 启停定时器 |
| `TickCount` | 累计心跳数 |
| `Tick` event | 每秒触发 |

**UserActivityTracker** — 用户活动追踪（15 分钟超时）
- **设计依据**: `InputManager.PreProcessInput` 监听键盘/鼠标点击/滚轮（过滤鼠标移动）；通过 `ApplicationTickService` 定期检查不活跃状态

| 成员 | 说明 |
|------|------|
| `StartTracking()` / `StopTracking()` | 启停追踪 |
| `IsUserActive` | 是否活跃 |
| `TimeUntilInactive` | 距超时剩余时间 |
| `ResetActivity()` | 重置活动计时器 |
| `SessionExpired` event | 会话过期事件 |

**ActiveConsultationService** — 活跃医案追踪
- **设计依据**: 跟踪当前正在进行的中医诊断会话

**PrescriptionSettingsService** — 处方设置服务
- **设计依据**: 管理处方相关配置（最大/最小/总剂量策略、导入策略、保留策略）

---

### ViewModels/Base/

**CoreViewModelBase** — 核心 ViewModel 基类
- **设计依据**: `ObservableObject` + `IDisposable`；聚合 `IViewModelServices`；CommunityToolkit.Mvvm 源生成器优先

| 成员 | 说明 |
|------|------|
| `Services` | `IViewModelServices` 聚合服务 |
| `Logger` / `EventAggregator` | 从 Services 解构的常用服务 |
| `Events` | `EventSubscriptionManager`（延迟初始化，Dispose 自动清理） |
| `IsBusy` / `StatusMessage` / `ErrorMessage` | 可观察状态 |
| `SetBusy(bool, message?)` | 设置忙碌状态 |
| `SetError(message)` / `ClearError()` | 错误管理 |
| `ExecuteWithErrorHandlingAsync(...)` | 统一异常处理包装 |
| `RunOnUIThread(Action)` / `RunOnUIThreadAsync(Func<Task>)` | UI 线程执行 |
| `AddDisposable(IDisposable)` | 注册可释放资源 |
| `OnDisposing()` | 子类清理钩子 |

**NavigableViewModelBase** — 可导航 ViewModel 基类
- **设计依据**: 继承 `CoreViewModelBase`，实现 Prism `INavigationAware` + `IRegionMemberLifetime` + `IConfirmNavigationRequest` + `IEditable`；未保存变更守卫

| 成员 | 说明 |
|------|------|
| `RegionManager` / `SessionManager` / `ToastService` / `RoleRegistry` 等 | 从 Services 解构 |
| `PageTitle` / `IsLoading` / `IsInitialized` / `IsActive` / `IsEditing` | 可观察属性 |
| `HasUnsavedChanges` | 未保存变更标记 |
| `NavigateTo(region, view, params?)` | 区域导航 |
| `NavigateToHome` command | 返回角色对应主页 |
| `ConfirmNavigationRequest(...)` | 未保存变更守卫（弹确认框） |
| `BeginEdit/CancelEdit/EndEdit` | 编辑生命周期 |
| `ShowSuccessMessageAsync/ShowErrorMessageAsync/ShowWarningMessageAsync` | Toast 通知 |
| `ShowConfirmMessageAsync(...)` | 确认对话框 |
| `InitializeAsync(NavigationContext)` | 首次导航初始化钩子 |

**DialogViewModelBase** — 对话框 ViewModel 基类
- **设计依据**: 实现 Prism `IDialogAware`；标准 `Cancel` / `Confirm` 命令；`CanConfirm` 依赖 `!IsBusy && !IsLoading`

| 成员 | 说明 |
|------|------|
| `Title` / `IsLoading` | 可观察属性 |
| `Cancel` command | 取消（`ButtonResult.Cancel`） |
| `Confirm` command | 确认（`ButtonResult.OK`，可重写 `CanConfirm`） |
| `CloseDialog(...)` | 关闭对话框（支持返回参数） |
| `CloseDialogWithResult<T>(key, value)` | 关闭并返回数据 |
| `GetDialogParameter<T>(params, key)` | 获取必需参数（不存在抛异常） |
| `TryGetDialogParameter<T>(...)` | 尝试获取参数 |

**ValidatableModelBase** — 可验证模型基类
- **设计依据**: `BindableBase` + `INotifyDataErrorInfo`；DataAnnotations 验证；XAML 索引器绑定 `Errors[PropertyName]`

| 成员 | 说明 |
|------|------|
| `Errors` | `ValidationErrorsAccessor`（XAML 索引器） |
| `HasErrorsDictionary` | `ValidationHasErrorsAccessor`（XAML 索引器） |
| `SetPropertyAndValidate<T>(...)` | 设置属性并自动验证 |
| `ValidateProperty(propertyName)` | DataAnnotations 验证单个属性 |
| `ValidateAll()` | 验证所有带 `[ValidationAttribute]` 的属性 |

**HerbItemViewModelBase** — 药材项基类
- **设计依据**: 统一经验方和处方的药材编辑体验；拼音码智能匹配评分算法（100/90/80/70/50/40/30 分级）

| 成员 | 说明 |
|------|------|
| `HerbId` / `HerbName` / `Unit` / `Dosage` / `DecocteMethod` | 药材属性 |
| `AllHerbs` | 全量药材列表（父 VM 注入） |
| `FilteredHerbs` | 过滤后药材列表 |
| `SelectedHerb` | 选中药材（自动填充属性） |
| `UnitPrice` (abstract) | 单价（经验方返回 0，处方返回实际价格） |
| `FilterHerbs()` | 拼音码过滤（取 Top 5） |
| `OnHerbSelected(herb)` | 选中回调钩子 |
| `OnDosageChanged(dosage)` | 剂量变更回调钩子 |

**MasterDetailViewModelBase<TListItem, TDetail>** — Master-Detail 完整 CRUD
- **设计依据**: 继承 `NavigableViewModelBase`，组合 `IMasterDetailServices`；通过事件订阅同步子服务状态到基类属性；模板方法模式（子类实现抽象方法）

| 命令 | 说明 |
|------|------|
| `RefreshAsync` | 刷新列表（重置分页） |
| `SearchAsync` / `ClearSearchAsync` | 搜索/清除搜索 |
| `GoToFirstPage/PreviousPage/NextPage/LastPageAsync` | 分页导航 |
| `CreateNewAsync` | 新建 |
| `Edit` | 进入编辑模式 |
| `SaveAsync` | 保存 |
| `CancelAsync` | 取消（未保存变更确认） |
| `DeleteAsync` | 删除（支持单选/批量） |
| `BatchEnableAsync` / `BatchDisableAsync` | 批量启用/禁用 |
| `RestoreAsync` | 恢复软删除记录 |

| 抽象方法（子类必须实现） | 说明 |
|------|------|
| `LoadListAsync()` | 加载列表数据 |
| `LoadDetailAsync(TListItem)` | 加载详情数据 |
| `CreateNewDetail()` | 创建新详情实例 |
| `SaveDetailAsync(TDetail)` | 保存详情 |
| `DeleteItemAsync(TListItem)` | 删除项 |

**ChildViewModelBase** — 复合 VM 子 ViewModel
- **设计依据**: 持有 `IWorkspaceHost` 引用（父 VM 操作）；`InitializeAsync()` 由父 VM 导航生命周期后调用

**BaseStatusHandler<TListDto>** — 状态处理模板方法
- **设计依据**: 统一 Restore/Toggle 操作的确认 → 执行 → 通知 → 异常处理流程

| 抽象/虚方法 | 说明 |
|------|------|
| `EntityTypeName` | 实体显示名（如 "药材"） |
| `GetEntityId(entity)` / `GetEntityDisplayName(entity)` | 获取实体 ID / 显示名 |
| `ExecuteRestoreAsync(Guid)` | 执行恢复 |
| `ExecuteToggleStatusAsync(Guid)` | 执行状态切换（可选） |
| `RestoreAsync(entity)` | 恢复（统一实现：确认→执行→通知） |
| `ToggleStatusAsync(entity)` | 状态切换（默认实现：含确认对话框） |

---

### DependencyInjection/

**ViewModelServicesExtensions** — DI 注册扩展
- `AddViewModelServices()`: 注册 `IViewModelServices` 及其所有聚合服务
- `AddMasterDetailServices()`: 注册 Master-Detail 相关服务

### Constants/

**ViewNames** — 21 个视图名称常量（编译时类型安全）
- 主页: `AdminHome` / `ClinicalHome` / `ReceptionistHome` / `SysadminHome`
- 管理: `PatientManagement` / `MedicalCaseManagement` / `HerbManagement` / `FormulaManagement` / `UserManagement` / `ReportsHome`
- 工作台: `PatientSelection` / `MedicalCaseWorkspace` / `ClinicalWorkspace`
- 其他: `Login` / `SystemSettings` / `AccountSettings` / `RegistrationList` / `AuditLog` / `LogLevelControl` / `Deployment`

**RegionNames** — 15 个区域名称常量
- `ContentRegion` / `NavigationRegion` / `LoginRegion` / `ToolbarRegion` / `StatusBarRegion` / `SidebarRegion` / `ModuleRegion` / `PatientRegion` / `ConsultationRegion` / `PrescriptionRegion` / `HerbRegion` / `FormulaRegion` / `SettingsRegion` / `MainWindowRegion` / `DialogRegion`

**SystemConstants** — 系统配置常量
- `PasswordPolicy`: MinLength=8, MaxLength=128, 需大小写+数字+特殊字符
- `FilePaths`: Config/Logs/Temp/Backup/Export 目录
- `ErrorCodes`: AUTH_001~003 / VALID_001 / DATA_001 / DB_001 / NET_001 / SYS_001

### Logging/

**DesktopSerilogConfiguration** — Serilog 日志配置
- **设计依据**: `%LOCALAPPDATA%/LYBTZYZS/logs/` 路径；每日滚动；10MB 文件大小限制；30 天保留；CorrelationId（W3C TraceContext）；敏感数据脱敏

| 特性 | 值 |
|------|------|
| 日志路径 | `%LOCALAPPDATA%/LYBTZYZS/logs/lybt-desktop-{Date}.log` |
| 滚动策略 | 每日滚动 |
| 文件大小限制 | 10MB（超限自动滚动） |
| 保留天数 | 30 天 |
| CorrelationId | 基于 Activity API，W3C TraceContext |
| 脱敏 | `SensitiveDataDestructuringPolicy` |

### Http/

**BaseUrlDelegatingHandler** — URL 重写 Handler
- **设计依据**: 线程安全的 `DelegatingHandler`；每次请求根据 `IConnectionSettingsService.CurrentUrl` 重写 `RequestUri`；替代不安全的 `HttpClient.BaseAddress` 变更

**LoggingHttpHandler** — HTTP 日志 Handler
- **设计依据**: 注入 CorrelationId 到请求头，记录请求/响应日志

**ApiResponseHelper** — API 响应辅助工具

**ProblemDetailsParser** — RFC 7807 Problem Details 解析

### Events/

**EventSubscriptionManager** — 事件订阅自动管理
- **设计依据**: 跟踪所有 `SubscriptionToken`，`Dispose` 时自动清理，避免内存泄漏

| 方法 | 说明 |
|------|------|
| `Subscribe<TEvent, TPayload>(handler)` | 订阅事件（UI 线程） |
| `Subscribe<TEvent, TPayload>(handler, threadOption, filter?)` | 指定线程和过滤器 |
| `Publish<TEvent, TPayload>(payload)` | 发布事件 |
| `ClearSubscriptions()` | 清除所有订阅 |

**CaseEvents** — 医案相关事件定义

**PatientEvents** — 患者相关事件定义

### Repositories/

**RepositoryBase<T>** — 抽象 CRUD 仓储基类
- **设计依据**: 封装带日志的基础 CRUD 操作

### Security/

**SensitiveInfoFilter** — 敏感信息正则脱敏

### Behaviors/

| 类 | 说明 |
|------|------|
| `DataGridSelectionBehavior` | DataGrid 选择行为附加属性 |
| `PasswordBoxHelper` | PasswordBox 绑定辅助（Password 属性不可绑定的 workaround） |
| `ResponsiveLayoutBehavior` | 响应式布局行为 |

### Commands/

**ApplicationCommands** — 全局 `CompositeCommand`，支持跨模块命令注册

## 依赖关系

```
LYBT.Desktop.Contracts         ← 接口定义（IViewModelServices / IRoleDefinition 等）
LYBT.Desktop.Foundation        ← 安全基础（PasswordHelper / TokenStorage）
LYBT.Shared.Models             ← DTO / 枚举 / Contracts
LYBT.Shared.Logging            ← 共享日志组件（CorrelationId / Masking）
LYBT.Shared.Configuration     ← 配置选项（ApiClientOptions）
LYBT.Shared.Components        ← 共享 UI 组件（IHerbItemEditable）
LYBT.Desktop.Controls         ← 控件库（ToastControl）

↑ 上层引用
LYBT.Desktop.Infrastructure  ← 本项目
  ↑
  ├── LYBT.Desktop.Shell         ← 主窗口 / 启动
  ├── LYBT.Desktop.Modules.*     ← 业务模块
  └── LYBT.Desktop.Roles.*       ← 角色模块
```

## 设计决策

1. **服务聚合模式**: `IViewModelServices` 聚合 9 个服务，ViewModel 基类构造函数仅需一个参数，子类按需从 `Services` 属性解构
2. **CommunityToolkit.Mvvm 优先**: 使用 `[ObservableProperty]` / `[RelayCommand]` 源生成器替代 Prism `BindableBase` / `DelegateCommand`，减少样板代码
3. **EventSubscriptionManager 延迟初始化**: `CoreViewModelBase.Events` 属性惰性创建，仅在实际订阅事件时分配资源，Dispose 时自动清理所有 `SubscriptionToken`
4. **嵌套加载状态计数器**: `LoadingStateManager` 使用 `_loadingCount` 支持并发操作嵌套，避免早期内层操作结束导致 `IsLoading` 提前变 `false`
5. **URL 驱动连接切换**: `ConnectionSettingsService` 通过 URL 判断 Local/Remote 模式（`localhost:5300` = Local），`BaseUrlDelegatingHandler` 在请求时动态重写 URI，替代不安全的 `HttpClient.BaseAddress` 变更
6. **Toast 替代 MessageBox**: `NavigableViewModelBase` 的成功/错误/警告通知使用 `ToastService`（非阻塞），仅确认对话框保留 `CommonDialogService`
7. **角色注册表 fallback**: `RoleRegistry.GetHomeViewName()` 未注册角色 fallback 到 `ClinicalHome`，避免运行时崩溃
8. **拼音码评分算法**: `HerbItemViewModelBase` 7 级评分（名称完全匹配 100 > 拼音完全 90 > 名称前缀 80 > 拼音前缀 70 > 名称包含 50 > 拼音包含 40 > 拼音模糊 30），取 Top 5

## 已知陷阱

1. **SessionManager.CurrentUser 死锁**: 历史上使用 `_authService.GetCurrentUserAsync()` 导致 WPF UI 线程死锁，已改为同步方法 + `lock`
2. **HttpClient.BaseAddress 变更不安全**: 已用 `BaseUrlDelegatingHandler` 替代，不要在运行时修改 `HttpClient.BaseAddress`
3. **ToastService Dispatcher 依赖**: 构造时捕获 `Dispatcher.CurrentDispatcher`，必须在 UI 线程创建实例
4. **UserActivityTracker InputManager 线程**: `InputManager.Current.PreProcessInput` 必须在 STA/UI 线程订阅，通过 `Application.Current.Dispatcher.Invoke` 保证
5. **PaginationService PageSize 变更**: 变更时自动调整 `CurrentPage` 保持大致位置，但会触发 `PageChanged` 事件导致重新加载列表
6. **SearchService 防抖 CTS 泄漏**: `Dispose` 时必须取消并释放 `_debounceCts` 和 `_searchCts`，否则后台 `Task.Delay` 会泄漏
7. **ValidatableModelBase vs ErrorHandler**: 两者都实现 `INotifyDataErrorInfo`，前者用于 DetailModel 属性验证，后者用于服务层错误管理，不要混用

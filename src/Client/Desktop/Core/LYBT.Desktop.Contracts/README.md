# LYBT.Desktop.Contracts

> 纯接口层 — 零实现、零运行时依赖，仅包含契约、DTO、枚举和事件参数

## 项目定位

- **层级**: Core
- **职责**: 定义 Desktop 客户端所有服务、仓储、API 客户端的接口契约
- **状态**: Active
- **设计依据**: 接口隔离原则 (ISP) — 每个关注点（认证、导航、会话、缓存等）独立接口，依赖方仅引用所需契约

## 目录结构

```
LYBT.Desktop.Contracts/
├── ApiClient/          # 统一 API 客户端接口 (IApiClient + 8 个子接口)
├── Api/                # Refit 属性接口 (远程模式) + Local 模式接口
├── Roles/              # 角色定义接口 (IRoleDefinition, IRoleRegistry)
├── Services/           # 应用服务契约 (启动管道、连接、会话、导航、缓存等)
├── Security/           # 认证状态机接口
├── Repositories/       # 数据访问契约 (6 个实体仓储)
├── Performance/        # 性能监控接口
├── Initialization/     # 数据库初始化接口
└── CrossModule/        # 跨模块搜索提供者接口
```

## 核心组件

### ApiClient/ — 统一 API 客户端抽象

#### `IApiClient`
**设计依据**: Facade 模式，聚合所有领域子接口。两个实现：`RefitApiClient`（远程）和 `HttpClientApiClient`（本地）。`SwitchingApiClient` 代理透明切换，仓储层完全无感知。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Auth` | `IApiClientAuth` | 认证端点 |
| `Users` | `IApiClientUsers` | 用户管理 |
| `Patients` | `IApiClientPatients` | 患者管理 |
| `Herbs` | `IApiClientHerbs` | 药材管理 |
| `Formulas` | `IApiClientFormulas` | 验方管理 |
| `MedicalCases` | `IApiClientMedicalCases` | 医案管理 |
| `Registrations` | `IApiClientRegistrations` | 挂号管理 |
| `Reports` | `IApiClientReports` | 统计报表 |

#### `IApiClientAuth`
**设计依据**: 合并远程 `IAuthApi` 和本地 `ILocalAuthApi` 为统一契约，无 Refit 属性。

| 方法 | 返回类型 | 说明 |
|------|----------|------|
| `LoginAsync(LoginRequest)` | `Task<ApiResponse<LoginResponse>>` | 凭证登录 |
| `LoginWithAutoTokenAsync(AutoLoginRequest)` | `Task<ApiResponse<LoginResponse>>` | Token 自动登录 |
| `LogoutAsync(LogoutRequest)` | `Task<ApiResponse>` | 登出 |
| `RefreshTokenAsync(RefreshTokenRequest)` | `Task<ApiResponse<LoginResponse>>` | 刷新 Token |
| `ValidateTokenFromHeaderAsync()` | `Task<ApiResponse<object>>` | 从 Header 验证 Token |
| `HealthCheckAsync()` | `Task<ApiResponse<HealthCheckResponse>>` | 健康检查 |

#### `IApiClientUsers` (14 方法)
**设计依据**: 统一用户 CRUD + 密码管理。本地模式有额外方法（`RestoreAsync`, `BatchEnableAsync`, `GetCurrentUserAsync`）。

| 方法 | 说明 |
|------|------|
| `GetUsersAsync(page, pageSize, keyword)` | 分页查询 |
| `GetUserByIdAsync(id)` | 按 ID 查询 |
| `CreateUserAsync(request)` | 创建用户 |
| `UpdateUserAsync(id, request)` | 更新用户 |
| `DeleteUserAsync(id)` | 删除用户 |
| `ChangeProfileAsync(id, request)` | 修改个人资料 |
| `ChangePasswordAsync(id, request)` | 修改密码 |
| `ResetPasswordAsync(id, request)` | 重置密码 |
| `ToggleStatusAsync(id)` | 启用/禁用 |
| `BatchDeleteAsync(request)` | 批量删除 |

#### `IApiClientMedicalCases` (16+ 方法)
**设计依据**: 最大的 API 接口。MedicalCase 是 DDD 聚合根，生命周期复杂（Draft → Active → Suspended/Completed/Cancelled）。`SaveAsync` 是聚合保存（诊断 + 处方一次性提交）。

| 方法 | 说明 |
|------|------|
| `GetMedicalCasesAsync(page, pageSize, keyword, includeAllDoctors)` | 分页查询 |
| `QueryMedicalCasesAsync(queryType, patientId, ...)` | 统一查询模型 |
| `GetMedicalCaseByIdAsync(id)` | 按 ID 查询 |
| `CreateMedicalCaseAsync(request)` | 创建医案 |
| `SaveAsync(id, request)` | 聚合保存 |
| `CloseCaseAsync(id)` | 关闭医案 |
| `SuspendAsync(id, request)` | 暂存医案 |
| `CancelMedicalCaseAsync(id, request)` | 取消医案 |
| `UpdateStatusAsync(id, request)` | 更新状态 |
| `GetPermissionsAsync(id)` | 获取权限 |
| `RecordPrintAsync(id, request)` | 记录打印 |

### Roles/ — 角色驱动工作台系统

#### `IRoleDefinition`
**设计依据**: 每个角色实现此接口，声明模块集合和主页视图。`GetAllModules()` 合并基础模块 + 角色特定模块。

| 属性 | 类型 | 说明 |
|------|------|------|
| `Role` | `UserRole` | 角色枚举 |
| `DisplayName` | `string` | 显示名称 |
| `Description` | `string` | 角色描述 |
| `HomeViewName` | `string` | 主页视图名 |
| `RequiredModules` | `IReadOnlyList<string>` | 角色特定模块 |
| `BaseModules` | `IReadOnlyList<string>` | 基础模块（所有角色共享） |

#### `IRoleRegistry`
**设计依据**: 中央注册表，启动时根据用户角色确定加载哪些模块。

| 方法 | 说明 |
|------|------|
| `Register(IRoleDefinition)` | 注册角色定义 |
| `GetDefinition(UserRole)` | 按角色查找 |
| `GetAllDefinitions()` | 获取所有定义 |
| `IsRegistered(UserRole)` | 检查是否已注册 |
| `GetHomeViewName(UserRole)` | 获取主页视图名 |
| `GetModulesForRole(UserRole)` | 获取角色所需模块 |

### Services/ — 应用服务契约

#### `IStartupStep` / `IStartupPipeline`
**设计依据**: 启动步骤抽象。`ParallelGroup` 支持相邻步骤并行执行。必需步骤失败终止管道，可选步骤失败继续。

| 接口 | 关键成员 |
|------|----------|
| `IStartupStep` | `Name`, `Order`, `IsRequired`, `ParallelGroup`, `ExecuteAsync()` |
| `IStartupPipeline` | `State`, `Steps`, `RegisterStep()`, `ExecuteAsync()`, `Reset()`, `GetDiagnostics()` |

#### `IConnectionSettingsService`
**设计依据**: URL 驱动的连接模型。`localhost`/`127.0.0.1` → 本地模式，其他 → 远程模式。持久化设置跨会话。

| 属性/方法 | 说明 |
|-----------|------|
| `CurrentUrl` | 当前 API 基础 URL |
| `IsLocal` | 是否本地模式 |
| `LocalUrl` / `RemoteUrl` | 本地/远程 URL |
| `SetUrlAsync(url)` | 设置 URL |
| `SaveRemoteUrlAsync(url)` | 保存远程 URL |
| `UrlChanged` event | URL 变更事件 |

#### `ISessionManager`
**设计依据**: 内存会话状态。Token 管理委托给 `ITokenStorageService`。权限检查基于 `UserRole` 枚举。

| 成员 | 说明 |
|------|------|
| `CurrentUser` | 当前用户 |
| `IsAuthenticated` | 是否已认证 |
| `SetSession(user, token, refreshToken)` | 设置会话 |
| `ClearSession()` | 清除会话 |
| `HasPermission(UserRole)` | 角色权限检查 |
| `IsAdmin()` | 管理员检查 |

#### `INavigationCoordinator`
**设计依据**: 统一导航入口，整合 `NavigationManager`、`ViewNavigationService`、`RoleNavigationService`。支持面包屑导航和前进导航。

| 方法 | 说明 |
|------|------|
| `NavigateTo(viewName, parameters?)` | 导航到视图 |
| `NavigateToHome()` / `NavigateToHome(role)` | 导航到主页 |
| `NavigateBack()` / `NavigateForward()` | 前进/后退 |
| `NavigateToBreadcrumb(item)` | 面包屑跳转 |
| `ShowLoginDialog()` | 显示登录 |

#### `IViewModelServices`
**设计依据**: 聚合 9 个常用服务为一个可注入接口，将 ViewModel 构造函数参数从 9 个减少到 1 个。

| 属性 | 类型 |
|------|------|
| `LoggerFactory` | `ILoggerFactory` |
| `EventAggregator` | `IEventAggregator` |
| `RegionManager` | `IRegionManager` |
| `SessionManager` | `ISessionManager` |
| `UserNotificationService` | `IUserNotificationService` |
| `CommonDialogService` | `ICommonDialogService` |
| `ToastService` | `IToastService` |
| `RoleRegistry` | `IRoleRegistry` |
| `UiThreadDispatcher` | `IUiThreadDispatcher` |

#### `ICommonDialogService`
**设计依据**: 统一对话框抽象。`ShowTripleChoiceAsync`（是/否/取消）支持未保存更改确认。`ShowUnfinishedCaseDialogAsync` 是领域特定的 4 选项对话框。

| 方法 | 说明 |
|------|------|
| `ShowInfoAsync` / `ShowWarningAsync` / `ShowErrorAsync` | 消息对话框 |
| `ShowConfirmAsync` | 是/否确认 |
| `ShowTripleChoiceAsync` | 是/否/取消 |
| `ShowInputAsync` | 输入对话框 |
| `ShowOpenFileDialogAsync` / `ShowSaveFileDialogAsync` | 文件对话框 |
| `ShowUnfinishedCaseDialogAsync` | 未完成医案对话框 |

#### `IApplicationTickService`
**设计依据**: 单一 `DispatcherTimer`（1 秒间隔）广播 `Tick` 事件。所有周期任务（会话超时、健康检查）订阅 `Tick` 并自行决定频率。避免多个定时器。

### Security/ — 认证状态机

#### `IAuthenticationStateMachine`
**设计依据**: 表驱动状态机，11 个状态（Idle, Authenticating, ValidatingToken, LoadingProfile, LoadingModules, Navigating, Authenticated, Failed, LoggingOut, SessionExpired, RefreshingToken）。线程安全（lock）。事件在 lock 外发布避免死锁。

### Repositories/ — 数据访问契约

**设计依据**: 双实现仓储（HTTP 在 Foundation，LocalDB 在 LocalData）。所有方法接受 `CancellationToken`。

| 接口 | 关键方法 |
|------|----------|
| `IUserRepository` | CRUD + `GetDoctorsAsync`, `ChangePasswordAsync`, `ResetPasswordAsync` |
| `IPatientRepository` | CRUD + `GetByIdNumberAsync`（身份证查询）, `BatchImportAsync` |
| `IHerbRepository` | CRUD + `BatchImportAsync`, `ToggleStatusAsync` |
| `IFormulaRepository` | CRUD + `CloneFormulaAsync`, `BatchImportAsync` |
| `IMedicalCaseRepository` | CRUD + `SaveAsync`（聚合保存）, `QueryAsync`, `CloseCaseAsync`, `SuspendAsync` |
| `IRegistrationRepository` | `CreateAsync`, `GetWaitingQueueAsync`, `StartVisitAsync`, `CancelAsync` |

### CrossModule/ — 跨模块搜索提供者

#### `IHerbSearchProvider` / `IFormulaSearchProvider`
**设计依据**: 解耦 MedicalCase/Formula 模块与 Herbs/Formula 模块的编译时依赖。跨模块搜索通过接口注入。

## 依赖关系

- **依赖**: `LYBT.Shared.Models`（DTO、枚举）
- **被依赖**: Foundation、Infrastructure、Modules、Roles、Shell、LocalWebAPI — 所有 Desktop 项目

## 设计决策

| 决策 | 原因 | 日期 |
|------|------|------|
| IApiClient 聚合所有子接口 | 替代分散的 Refit 接口，统一 Remote/Local 模式 | 2025-12 |
| IRoleDefinition 驱动模块加载 | 不同角色加载不同模块集合，减少非必要初始化 | 2025-12 |
| IStartupStep 支持 ParallelGroup | 相邻步骤可并行执行，加速启动 | 2025-12 |
| IViewModelServices 聚合 9 服务 | 减少 ViewModel 构造函数参数爆炸 | 2026-01 |
| IUiThreadDispatcher 抽象 | 解耦 ViewModel 对 WPF Dispatcher 的直接依赖，提升可测试性 | 2025-12 |

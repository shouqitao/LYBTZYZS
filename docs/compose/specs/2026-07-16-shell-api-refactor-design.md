# Shell 层重构 + API 优化 — 设计规格

## [S1] 问题陈述

当前代码库存在以下系统性问题：

### Shell 层 (45 个源文件, ~5800 行)

- **MainWindowViewModel** (649 行) 是 God Class，混合了登录状态、侧边栏、18+ 委托命令、状态栏、导航、Token 生命周期、会话管理、密码/资料变更事件处理等 7+ 种职责
- 3 处死代码：`RegisterDataSourceLoggers` 空方法、`ClearInvalidTokenAsync` 未调用私有方法、`ApplicationState` 未使用枚举
- `EmbeddedLocalWebApiService.Dispose()` 使用 fire-and-forget 反模式
- `ThemeService` 用字符串替换修改 JSON 配置
- 两处重复的 HttpClient 配置（`HttpServiceRegistrationExtensions` 和 `UnifiedApiClientExtensions`）
- `App.xaml.cs` 中有 P/Invoke 声明，但 `NativeMethods.cs` 已存在
- 2 个占位符方法（`ExecuteShowHistory`、`ExecuteCycleRegions`）

### API 控制器 (Server 96 端点 + LocalWebAPI 100 端点)

- LocalWebAPI `AuthController` (268 行) 内联全部认证逻辑，绕过 MediatR
- LocalWebAPI `DiagnosticsController` 直接注入 `AppDbContext`，绕过服务层
- LocalWebAPI `HealthController` 直接注入 `AppDbContext`
- `MedicalCases GetById` 响应形状不一致（Local 返回匿名对象，Server 返回 DTO）
- `ReportsController` 在控制器内手动映射 DTO
- `DeployController` 使用 `Environment.Exit(0)` fire-and-forget
- `CancellationToken` 传播不完整

### Desktop 模块

- `MedicalCaseService` (697 行) 实现 3 个接口，是 God Class
- `HerbRepository` 有冗余的 Tuple 包装方法
- 服务方法命名不一致（`CreateAsync` vs `CreatePatientAsync` vs `SaveFormulaAsync`）
- `ReportsModule` 是存根实现
- `MedicalCaseMasterDetailViewModel` 被双重注册

## [S2] 解决方案概览

采用**全面并行重构**策略，按以下优先级分 6 批执行：

| 批次 | 范围 | 预估变更文件数 |
|------|------|---------------|
| 1 | Shell 层清理（死代码 + 反模式） | ~10 |
| 2 | MainWindowViewModel 拆分 | ~5 |
| 3 | LocalWebAPI Auth CQRS 改造 | ~12 |
| 4 | API 一致性修复 | ~15 |
| 5 | MedicalCaseService 拆分 | ~8 |
| 6 | Desktop 模块清理 | ~10 |

## [S3] 批次 1 — Shell 层清理

### 3.1 删除死代码

| 文件 | 代码 | 操作 |
|------|------|------|
| `Extensions/LoggingRegistrationExtensions.cs` | `RegisterDataSourceLoggers()` 空方法 | 删除方法及其调用 |
| `Services/Login/LoginCoordinator.cs:310-323` | `ClearInvalidTokenAsync()` 未调用私有方法 | 删除方法 |
| `Services/Lifecycle/ApplicationState.cs` | `ApplicationState` 枚举，未被引用 | 删除整个文件 |

### 3.2 修复 EmbeddedLocalWebApiService.Dispose()

当前代码使用 `_ = StopAsync()` fire-and-forget。改为同步等待：

```csharp
public void Dispose()
{
    try { StopAsync().GetAwaiter().GetResult(); }
    catch { /* Dispose 不应抛异常 */ }
}
```

### 3.3 修复 ThemeService JSON 字符串替换

当前用 `json.Replace(...)` 修改 appsettings.json。改为写入独立的 `theme-preference.json` 文件，启动时读取。

### 3.4 移除 App.xaml.cs 重复 P/Invoke

`App.xaml.cs` 中的 `GetConsoleWindow`、`SetConsoleOutputCP`、`SetConsoleCP` 移到 `NativeMethods.cs`，App 中调用 `NativeMethods.*`。

### 3.5 合并重复 HttpClient 配置

如果所有消费者都通过 `IApiClient`（SwitchingApiClient）访问，则 `HttpServiceRegistrationExtensions` 中的独立 HttpClient + 8 个 Refit 客户端是死代码，删除。

### 3.6 清理占位符方法

`MenuManager` 中 `ExecuteShowHistory()` 和 `ExecuteCycleRegions()` 只显示占位通知，删除这两个命令及其对应的快捷键绑定。

## [S4] 批次 2 — MainWindowViewModel 拆分

### 提取 LoginStateManager

- **职责**：登录状态属性（`IsLoggedIn`、`IsNotLoggedIn`、`CurrentUser`）、Token 生命周期事件处理、会话过期自动登出、密码变更事件处理
- **接口**：`ILoginStateManager`
- **事件**：`LoginStateChanged`、`LogoutRequested`

### 提取 ShellEventCoordinator

- **职责**：订阅 `TokenLifecycleStateChangedEvent`、`UserActivityTracker.SessionExpired`、`ProfileUpdatedEvent`、`PasswordChangedEvent`，协调各管理器响应

### MainWindowViewModel 瘦身后

- 保留：`IsSidebarExpanded`、`ToggleSidebarCommand`、委托命令属性
- 注入：`ILoginStateManager`、`INavigationManager`、`IMenuManager`、`IStatusBarManager`
- 命令属性改为表达式体成员减少行数

## [S5] 批次 3 — LocalWebAPI CQRS 改造

### 5A. AuthController 改造

创建本地专用 CQRS Handler（不复用 Server Handler，JWT 模型不同）：

```
src/Client/Desktop/LocalWebAPI/
├── Handlers/
│   ├── LocalLoginCommandHandler.cs
│   ├── LocalRefreshTokenCommandHandler.cs
│   ├── LocalAutoLoginCommandHandler.cs
│   └── LocalValidateTokenQueryHandler.cs
└── Commands/
    ├── LocalLoginCommand.cs
    ├── LocalRefreshTokenCommand.cs
    ├── LocalAutoLoginCommand.cs
    └── LocalValidateTokenQuery.cs
```

### 5B. DiagnosticsController 部分改造

**复用 Server Handler**：`GetLoggingStatusQuery`、`EnableDebugModeCommand`、`DisableDebugModeCommand`、`SetLoggingLevelCommand`

**保持直接调用**：`db-info`（改用 `IHealthCheckService`）、`version`（程序集反射）、`logs/recent`（改用 `ISystemLogRepository`）

### 5C. HealthController 改用 IHealthCheckService

注入 `IHealthCheckService` 替代直接 `AppDbContext`。需在 LocalWebAPI DI 中注册。

## [S6] 批次 4 — API 一致性修复

### 6A. MedicalCases GetById 响应形状统一

LocalWebAPI GetById 改为返回 `MedicalCaseDetailDto`，额外数据通过已有端点获取。

### 6B. ReportsController DTO 映射移至 Handler

将控制器内手动映射逻辑移至 `GetDailyIncomeQueryHandler` 等 Handler。

### 6C. DeployController 安全修复

`Environment.Exit(0)` 改为 `IHostApplicationLifetime.StopApplication()`。

### 6D. CancellationToken 传播补全

所有 Controller 端点方法添加 `CancellationToken ct` 参数并传递。

## [S7] 批次 5 — MedicalCaseService 拆分

| 新服务类 | 接口 | 职责 |
|----------|------|------|
| `MedicalCaseQueryService` | `IMedicalCaseQueryService` | GetPaged, GetById, Search, Query |
| `MedicalCaseCommandService` | `IMedicalCaseCommandService` | Save, Delete, BatchDelete, SetPrescriptionFlag, RecordPrint |
| `MedicalCaseLifecycleService` | `IMedicalCaseLifecycleService` | Close, Suspend, Cancel, UpdateStatus |

DI 注册改为直接注册三个独立服务类。

## [S8] 批次 6 — Desktop 模块清理

### 8A. HerbRepository 冗余方法

删除 `*WithResultAsync` Tuple 方法，更新调用方。

### 8B. 服务方法命名统一

Herbs/Users 改为含实体名（`CreateHerbAsync`），Formula 改 `SaveFormulaAsync` 为 `CreateFormulaAsync`/`UpdateFormulaAsync`。

### 8C. ReportsModule 标记存根

添加 `// STUB: minimal implementation` 注释。

### 8D. 修复双重注册

删除 `MedicalCaseModule.RegisterTypes()` 中重复的 ViewModel 注册。

### 8C. 清理死代码引用

清理模块注释中已删除类/模块引用，删除不必要的 `using Refit;`。

# Shell 层重构 + API 优化 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 全面重构 Shell 层、统一 API 控制器模式、拆分 God Class，提升代码质量和可维护性

**Architecture:** 分 6 批并行执行，每批独立可测试。Shell 层清理 → MainWindowViewModel 拆分 → LocalWebAPI CQRS → API 一致性 → MedicalCaseService 拆分 → 模块清理

**Tech Stack:** .NET 8, WPF/Prism, MediatR, CommunityToolkit.Mvvm, EF Core, ASP.NET Core

## Global Constraints

- 所有变更必须通过 `dotnet build LYBTZYZS.sln` 编译
- 遵循 AGENTS.md 中的编码规范（中文业务文档/注释，英文标识符/commit）
- 保持向后兼容 — 不改变公共 API 契约
- 每个 Task 完成后独立可测试
- 使用 `codegraph_explore` 和 `serena_*` 工具理解代码后再修改

---

## Task 1: Shell 层死代码清理 [S3]

**Covers:** S3.1, S3.2, S3.3, S3.4, S3.5, S3.6

**Files:**
- Delete: `src/Client/Desktop/Shell/Services/Lifecycle/ApplicationState.cs`
- Modify: `src/Client/Desktop/Shell/Extensions/LoggingRegistrationExtensions.cs`
- Modify: `src/Client/Desktop/Shell/Services/Login/LoginCoordinator.cs`
- Modify: `src/Client/Desktop/Shell/Services/EmbeddedLocalWebApiService.cs`
- Modify: `src/Client/Desktop/Shell/Services/ThemeService.cs`
- Modify: `src/Client/Desktop/Shell/App.xaml.cs`
- Modify: `src/Client/Desktop/Shell/Services/MenuManager.cs`

**步骤:**

- [ ] **Step 1: 删除 ApplicationState.cs**

删除 `src/Client/Desktop/Shell/Services/Lifecycle/ApplicationState.cs` 文件。用 `codegraph_explore` 确认无引用。

- [ ] **Step 2: 删除空方法 RegisterDataSourceLoggers**

在 `LoggingRegistrationExtensions.cs` 中找到 `RegisterDataSourceLoggers()` 空方法及其调用方，删除。

- [ ] **Step 3: 删除未调用的 ClearInvalidTokenAsync**

在 `LoginCoordinator.cs` 中找到 `ClearInvalidTokenAsync()` 私有方法（约 310-323 行），删除。

- [ ] **Step 4: 修复 EmbeddedLocalWebApiService.Dispose()**

将 `_ = StopAsync()` 改为：
```csharp
public void Dispose()
{
    try { StopAsync().GetAwaiter().GetResult(); }
    catch { /* Dispose 不应抛异常 */ }
}
```

- [ ] **Step 5: 修复 ThemeService JSON 字符串替换**

将 `SaveThemePreference` 中的 `json.Replace(...)` 改为写入独立的 `theme-preference.json` 文件（位于 `AppContext.BaseDirectory`），启动时读取。

- [ ] **Step 6: 移除 App.xaml.cs 重复 P/Invoke**

将 `App.xaml.cs` 中的 `GetConsoleWindow`、`SetConsoleOutputCP`、`SetConsoleCP` DllImport 声明移到 `NativeMethods.cs`，App 中改为调用 `NativeMethods.*`。

- [ ] **Step 7: 清理 MenuManager 占位符方法**

删除 `MenuManager` 中的 `ExecuteShowHistory()` 和 `ExecuteCycleRegions()` 方法及其对应的命令注册和快捷键绑定。

- [ ] **Step 8: 检查 HttpServiceRegistrationExtensions 是否可删除**

用 `codegraph_explore` 检查 `HttpServiceRegistrationExtensions` 中创建的独立 HttpClient + 8 个 Refit 客户端是否被任何代码直接使用（非通过 `IApiClient`）。如果是死代码则删除该文件中的 HttpClient 注册。

- [ ] **Step 9: 编译验证**

Run: `dotnet build LYBTZYZS.sln`
Expected: 编译成功，无错误

- [ ] **Step 10: Commit**

```bash
git add -A
git commit -m "refactor(shell): 清理死代码和反模式"
```

---

## Task 2: MainWindowViewModel 拆分 [S4]

**Covers:** S4

**Files:**
- Create: `src/Client/Desktop/Shell/Services/Login/ILoginStateManager.cs`
- Create: `src/Client/Desktop/Shell/Services/Login/LoginStateManager.cs`
- Create: `src/Client/Desktop/Shell/Services/ShellEventCoordinator.cs`
- Modify: `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs`
- Modify: `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs`

**步骤:**

- [ ] **Step 1: 创建 ILoginStateManager 接口**

```csharp
// src/Client/Desktop/Shell/Services/Login/ILoginStateManager.cs
namespace LYBT.Desktop.Shell.Services.Login;

public interface ILoginStateManager
{
    bool IsLoggedIn { get; }
    bool IsNotLoggedIn { get; }
    CurrentUserInfo? CurrentUser { get; }
    event EventHandler<bool>? LoginStateChanged;
    event EventHandler? LogoutRequested;
    Task HandleLoginAsync(LoginResult result);
    Task HandleLogoutAsync();
}
```

- [ ] **Step 2: 实现 LoginStateManager**

从 MainWindowViewModel 中提取登录状态管理逻辑（`IsLoggedIn`、`IsNotLoggedIn`、`CurrentUser`、Token 生命周期事件处理、会话过期自动登出、密码变更事件处理）到 `LoginStateManager`。

- [ ] **Step 3: 创建 ShellEventCoordinator**

从 MainWindowViewModel 中提取事件订阅代码（`TokenLifecycleStateChangedEvent`、`UserActivityTracker.SessionExpired`、`ProfileUpdatedEvent`、`PasswordChangedEvent`）到 `ShellEventCoordinator`。

- [ ] **Step 4: 瘦身 MainWindowViewModel**

移除已提取的逻辑，改为注入 `ILoginStateManager`。命令属性改为表达式体成员。

- [ ] **Step 5: 更新 DI 注册**

在 `ServiceCollectionExtensions.RegisterAllServices()` 中注册 `ILoginStateManager`、`ShellEventCoordinator`。

- [ ] **Step 6: 编译验证**

Run: `dotnet build LYBTZYZS.sln`
Expected: 编译成功

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "refactor(shell): 拆分 MainWindowViewModel，提取 LoginStateManager"
```

---

## Task 3: LocalWebAPI Auth CQRS 改造 [S5]

**Covers:** S5A, S5B, S5C

**Files:**
- Create: `src/Client/Desktop/LocalWebAPI/Commands/LocalLoginCommand.cs`
- Create: `src/Client/Desktop/LocalWebAPI/Commands/LocalRefreshTokenCommand.cs`
- Create: `src/Client/Desktop/LocalWebAPI/Commands/LocalAutoLoginCommand.cs`
- Create: `src/Client/Desktop/LocalWebAPI/Commands/LocalValidateTokenQuery.cs`
- Create: `src/Client/Desktop/LocalWebAPI/Handlers/LocalLoginCommandHandler.cs`
- Create: `src/Client/Desktop/LocalWebAPI/Handlers/LocalRefreshTokenCommandHandler.cs`
- Create: `src/Client/Desktop/LocalWebAPI/Handlers/LocalAutoLoginCommandHandler.cs`
- Create: `src/Client/Desktop/LocalWebAPI/Handlers/LocalValidateTokenQueryHandler.cs`
- Modify: `src/Client/Desktop/LocalWebAPI/Controllers/AuthController.cs`
- Modify: `src/Client/Desktop/LocalWebAPI/Controllers/DiagnosticsController.cs`
- Modify: `src/Client/Desktop/LocalWebAPI/Controllers/HealthController.cs`

**步骤:**

- [ ] **Step 1: 创建 LocalLoginCommand 和 Handler**

从 AuthController.Login 中提取认证逻辑到 `LocalLoginCommandHandler`。

- [ ] **Step 2: 创建 LocalRefreshTokenCommand 和 Handler**

从 AuthController.RefreshToken 中提取 token 刷新逻辑。

- [ ] **Step 3: 创建 LocalAutoLoginCommand 和 Handler**

从 AuthController.AutoLogin 中提取自动登录逻辑。

- [ ] **Step 4: 创建 LocalValidateTokenQuery 和 Handler**

从 AuthController.ValidateToken 中提取验证逻辑。

- [ ] **Step 5: 改造 AuthController 为 ISender 注入**

```csharp
public class AuthController : BaseApiController
{
    private readonly ISender _sender;
    public AuthController(ISender sender, ILogger<AuthController> logger) : base(logger)
        => _sender = sender;
    // 所有端点改为 _sender.Send(...)
}
```

- [ ] **Step 6: 改造 DiagnosticsController 日志管理端点**

注入 `ISender`，4 个日志管理端点复用 Server Handler（`GetLoggingStatusQuery`、`EnableDebugModeCommand`、`DisableDebugModeCommand`、`SetLoggingLevelCommand`）。其余端点保持直接调用但改用 `IHealthCheckService`。

- [ ] **Step 7: 改造 HealthController**

注入 `IHealthCheckService` 替代直接 `AppDbContext`。在 LocalWebAPI DI 中注册 `IHealthCheckService`。

- [ ] **Step 8: 注册新 Handler 到 DI**

在 AuthModule 或相关模块中注册新的 CQRS Handler。

- [ ] **Step 9: 编译验证**

Run: `dotnet build LYBTZYZS.sln`
Expected: 编译成功

- [ ] **Step 10: Commit**

```bash
git add -A
git commit -m "refactor(local-api): Auth 改造为 CQRS，Diagnostics/Health 改用服务层"
```

---

## Task 4: API 一致性修复 [S6]

**Covers:** S6A, S6B, S6C, S6D

**Files:**
- Modify: `src/Client/Desktop/LocalWebAPI/Controllers/MedicalCasesController.cs`
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/ReportsController.cs`
- Modify: `src/Client/Desktop/LocalWebAPI/Controllers/ReportsController.cs`
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/DeployController.cs`
- Modify: 多个 Controller 文件（CancellationToken 补全）

**步骤:**

- [ ] **Step 1: 统一 MedicalCases GetById 响应形状**

LocalWebAPI `MedicalCasesController.GetById` 改为返回 `MedicalCaseDetailDto`，移除匿名对象包装。

- [ ] **Step 2: ReportsController DTO 映射移至 Handler**

将 Server 和 Local 的 `ReportsController` 中手动 DTO 映射逻辑移至对应的 Query Handler。

- [ ] **Step 3: 修复 DeployController 安全问题**

将 `Environment.Exit(0)` 改为 `IHostApplicationLifetime.StopApplication()`。

- [ ] **Step 4: 补全 CancellationToken 传播**

为所有 Controller 端点方法添加 `CancellationToken ct` 参数，传递到 `ISender.Send()` 和直接服务调用。

- [ ] **Step 5: 编译验证**

Run: `dotnet build LYBTZYZS.sln`
Expected: 编译成功

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "refactor(api): 统一响应形状，修复安全问题，补全 CancellationToken"
```

---

## Task 5: MedicalCaseService 拆分 [S7]

**Covers:** S7

**Files:**
- Create: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseQueryService.cs`
- Create: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseCommandService.cs`
- Create: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseLifecycleService.cs`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseService.cs`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/MedicalCaseModule.cs`

**步骤:**

- [ ] **Step 1: 创建 MedicalCaseQueryService**

从 MedicalCaseService 中提取 `IMedicalCaseQueryService` 接口的实现。

- [ ] **Step 2: 创建 MedicalCaseCommandService**

从 MedicalCaseService 中提取 `IMedicalCaseCommandService` 接口的实现。

- [ ] **Step 3: 创建 MedicalCaseLifecycleService**

从 MedicalCaseService 中提取 `IMedicalCaseLifecycleService` 接口的实现。

- [ ] **Step 4: 更新 DI 注册**

在 `MedicalCaseModule.RegisterTypes()` 中改为直接注册三个独立服务类。

- [ ] **Step 5: 处理 IMedicalCaseService 聚合代理**

如果有消费者注入 `IMedicalCaseService`，创建一个轻量聚合代理类委托到三个独立服务。

- [ ] **Step 6: 编译验证**

Run: `dotnet build LYBTZYZS.sln`
Expected: 编译成功

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "refactor(medical-case): 拆分 MedicalCaseService 为三个独立服务"
```

---

## Task 6: Desktop 模块清理 [S8]

**Covers:** S8A, S8B, S8C, S8D, S8E

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Repositories/HerbRepository.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Repositories/IHerbRepository.cs`
- Modify: 多个 Service 接口和实现（命名统一）
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/MedicalCaseModule.cs`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Reports/ReportsModule.cs`

**步骤:**

- [ ] **Step 1: 删除 HerbRepository 冗余方法**

删除 `IHerbRepository` 和 `HerbRepository` 中的 `CreateWithResultAsync`、`UpdateWithResultAsync`、`DeleteWithResultAsync`、`GetByIdWithResultAsync` 方法。用 `codegraph_explore` 找到并更新所有调用方。

- [ ] **Step 2: 统一 Herbs 服务方法命名**

将 `IHerbService` 的 `CreateAsync`/`UpdateAsync`/`DeleteAsync` 改为 `CreateHerbAsync`/`UpdateHerbAsync`/`DeleteHerbAsync`，更新所有实现和调用方。

- [ ] **Step 3: 统一 Users 服务方法命名**

将 `IUserService` 的 `CreateAsync`/`UpdateAsync`/`DeleteAsync` 改为 `CreateUserAsync`/`UpdateUserAsync`/`DeleteUserAsync`。

- [ ] **Step 4: 统一 Formula 服务方法命名**

将 `IFormulaService` 的 `SaveFormulaAsync` 改为 `CreateFormulaAsync` + `UpdateFormulaAsync`。

- [ ] **Step 5: 修复 MedicalCaseModule 双重注册**

删除 `MedicalCaseModule.RegisterTypes()` 中重复的 `MedicalCaseMasterDetailViewModel` 注册。

- [ ] **Step 6: 标记 ReportsModule 存根**

在 `ReportsModule` 类上添加 `// STUB: minimal implementation` 注释。

- [ ] **Step 7: 清理不必要的 using Refit**

删除 `IHerbService.cs` 和 `IUserService.cs` 中不必要的 `using Refit;`。

- [ ] **Step 8: 编译验证**

Run: `dotnet build LYBTZYZS.sln`
Expected: 编译成功

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "refactor(modules): 清理冗余方法，统一命名，修复双重注册"
```

---

## 最终验证

- [ ] **编译验证**

Run: `dotnet build LYBTZYZS.sln`
Expected: 编译成功，无错误

- [ ] **测试验证**

Run: `dotnet test tests/LYBT.Tests.Server/`
Run: `dotnet test tests/LYBT.Tests.Desktop/`
Run: `dotnet test tests/LYBT.Tests.Architecture/`
Expected: 所有测试通过

- [ ] **最终 Commit**

```bash
git add -A
git commit -m "refactor: Shell 层重构 + API 优化完成"
```

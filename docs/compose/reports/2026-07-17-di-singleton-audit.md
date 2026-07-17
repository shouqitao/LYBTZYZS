# DI Singleton 依赖链审计报告

**日期:** 2026-07-17  
**范围:** `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs` 及关联注册方法  
**DI 容器:** DryIoc (via Prism)  
**结论:** ✅ 无危险 Singleton→Transient 问题，1 个低风险系统性隐患

---

## 审计方法

1. 列出 `ServiceCollectionExtensions.cs` 中所有 `RegisterSingleton` 调用
2. 追踪每个 Singleton 实现类的构造函数依赖
3. 检查每个依赖的注册生命周期（Singleton / Transient / Prism-managed）
4. 标记 Singleton 构造函数中注入 Transient 的情况

---

## 一、Singleton 注册清单（共 48 个）

### Foundation 层（14 个）

| 接口 | 实现 | 构造函数依赖 | 依赖生命周期 | 结论 |
|------|------|-------------|-------------|------|
| `IMemoryCache` | `new MemoryCache(...)` | 无 DI 依赖 | — | ✅ 安全 |
| `IDesktopCacheManager` | `DesktopCacheManager` | `IMemoryCache`, `IEventAggregator`, `ILogger` | Singleton, Prism, Singleton¹ | ✅ 安全 |
| `IAuthenticationService` | `AuthenticationService` | `IAuthApi`, `ITokenStorageService`, `ITokenValidator`, `ICredentialVault`, `ILogger`, `IEventAggregator` | 全部 Singleton/Prism | ✅ 安全 |
| `ITokenStorageService` | `TokenStorageService` | `ILogger` | Singleton¹ | ✅ 安全 |
| `ITokenManager` | `TokenManager` | `ILogger` | Singleton¹ | ✅ 安全 |
| `ICredentialVault` | `CredentialVault` | `ILogger` | Singleton¹ | ✅ 安全 |
| `IPhotoStorageService` | `DpapiPhotoStorageService` | `ILogger` | Singleton¹ | ✅ 安全 |
| `IAuthenticationStateMachine` | `AuthenticationStateMachine` | `ILogger`, `IEventAggregator` | Singleton¹, Prism | ✅ 安全 |
| `ILogoutService` | `LogoutService` | `ILogger`, `ITokenStorageService`, `IAuthApi`, `IAuthenticationStateMachine`, `IEventAggregator` | 全部 Singleton/Prism | ✅ 安全 |
| `ITokenValidator` | `LocalTokenValidator` | `IOptions<JwtOptions>`, `ILogger` | Singleton, Singleton¹ | ✅ 安全 |
| `IUsernameStorageService` | `UsernameStorageService` | `ILogger` | Singleton¹ | ✅ 安全 |
| `IApiHealthCheckService` | `ApiHealthCheckService` | `HttpClient`, `IOptions<ApiClientOptions>` | Singleton, Singleton | ✅ 安全 |
| `IApiService` | `ApiService` | `HttpClient`, `IMemoryCache`, `ILogger` | 全部 Singleton | ✅ 安全 |
| `IStartupOptimizationService` | `StartupOptimizationService` | `ILogger` | Singleton¹ | ✅ 安全 |
| `ITokenLifecycleService` | `TokenLifecycleService` | `IAuthApi`, `ITokenStorageService`, `IEventAggregator`, `ILogger` | 全部 Singleton/Prism | ✅ 安全 |

### Presentation 层（8 个）

| 接口 | 实现 | 构造函数依赖 | 依赖生命周期 | 结论 |
|------|------|-------------|-------------|------|
| `INotificationService` | `NotificationService` | `ILogger`, `IUiThreadDispatcher` | Singleton¹, Singleton | ✅ 安全 |
| `IDesktopExceptionHandler` | `DesktopExceptionHandler` | `ILogger` | Singleton¹ | ✅ 安全 |
| `MenuManager` | `MenuManager` | `INavigationCoordinator`, `ISessionManager`, `IRoleRegistry`, `ILogger`, `IUserNotificationService`, `IApplicationCommands`, `IThemeService` | 全部 Singleton | ✅ 安全 |
| `INavigationCoordinator` | `NavigationCoordinator` | `IRegionManager`, `ISessionManager`, `IRoleRegistry`, `INavigationHistoryService`, `IModuleLazyLoader`, `IRegionMonitor`, `ILogger`, `IUserNotificationService` | Prism, 全部 Singleton | ✅ 安全 |
| `NavigationManager` | `NavigationManager` | `INavigationCoordinator`, `ILogger`, `IRoleRegistry` | 全部 Singleton | ✅ 安全 |
| `StatusBarManager` | `StatusBarManager` | `IApiHealthMonitor`, `IConnectionSettingsService`, `IConnectionModeService`, `IUiThreadDispatcher`, `ILogger` | 全部 Singleton | ✅ 安全 |
| `INavigationHistoryService` | `NavigationHistoryService` | `ILogger` | Singleton² | ✅ 安全 |
| `IModuleLazyLoader` | `ModuleLazyLoader` | `IModuleLoadingService`, `ILogger` | Singleton, Singleton² | ✅ 安全 |

### Infrastructure 层（13 个）

| 接口 | 实现 | 构造函数依赖 | 依赖生命周期 | 结论 |
|------|------|-------------|-------------|------|
| `IRegionMonitor` | `RegionMonitor` | `IRegionManager`, `ILogger` | Prism, Singleton² | ✅ 安全 |
| `ShellDialogHelper` | `ShellDialogHelper` | `ICommonDialogService`, `IToastService`, `ILogger` | Singleton, Singleton, Singleton² | ✅ 安全 |
| `ISessionManager` | `SessionManager` | `IAuthenticationService` | Singleton | ✅ 安全 |
| `IActiveConsultationService` | `ActiveConsultationService` | `ILogger` | Singleton | ✅ 安全 |
| `IApplicationTickService` | `ApplicationTickService` | `ILogger` | Singleton | ✅ 安全 |
| `UserActivityTracker` | factory | `ILogger`, `IApplicationTickService`, `ClientSessionOptions` | Singleton, Singleton, Singleton | ✅ 安全 |
| `IUserNotificationService` | `UserNotificationService` | 无 DI 依赖（直接使用 MessageBox） | — | ✅ 安全 |
| `IPrescriptionSettingsService` | `PrescriptionSettingsService` | `IConfiguration` | Singleton | ✅ 安全 |
| `IClinicSettingsService` | `ClinicSettingsService` | `IOptions<ClinicSettingsOptions>`, `ILogger` | Singleton, Singleton² | ✅ 安全 |
| `ICommonDialogService` | `CommonDialogService` | `IDialogService` | Prism | ✅ 安全 |
| `IRoleRegistry` | `RoleRegistry` (factory) | `ILogger` | Singleton | ✅ 安全 |
| `IApplicationCommands` | `ApplicationCommands` | 无 DI 依赖 | — | ✅ 安全 |
| `IModuleLoadingService` | `ModuleLoadingService` | `IModuleManager`, `IModuleCatalog`, `ILogger` | Prism, Prism, Singleton | ✅ 安全 |

### Application 层（7 个）

| 接口 | 实现 | 构造函数依赖 | 依赖生命周期 | 结论 |
|------|------|-------------|-------------|------|
| `IApplicationStateService` | `ApplicationStateService` | `IApiHealthCheckService`, `IOptions<ApiClientOptions>`, `ILogger` | 全部 Singleton | ✅ 安全 |
| `ISessionLifecycleManager` | `SessionLifecycleManager` | `ILogger`, `ITokenLifecycleService`, `IUserActivityTracker`, `IEventAggregator` | 全部 Singleton/Prism | ✅ 安全 |
| `ILoginCoordinator` | `LoginCoordinator` | `ILogger`, `IAuthenticationService`, `ITokenStorageService`, `ISessionLifecycleManager`, `IApplicationBootstrapper`, `INavigationCoordinator`, `ISessionManager`, `IAuthenticationStateMachine`, `IConfiguration`, `ICredentialVault`, `IUsernameStorageService` | 全部 Singleton | ✅ 安全 |
| `ILoginStateManager` | `LoginStateManager` | `IUserActivityTracker`, `ITokenLifecycleService`, `ILoginCoordinator`, `IEventAggregator`, `ILogger`, `IToastService` | 全部 Singleton/Prism | ✅ 安全 |
| `ShellEventCoordinator` | `ShellEventCoordinator` | `ILoginStateManager`, `IEventAggregator`, `IUserActivityTracker`, `ILoginCoordinator`, `ITokenLifecycleService`, `INavigationCoordinator`, `MenuManager`, `NavigationManager`, `IModuleLazyLoader`, `IUiThreadDispatcher`, `ILogger` | 全部 Singleton/Prism | ✅ 安全 |
| `IStartupPipeline` | `StartupPipeline` | `ILogger`, `IPerformanceMonitor` | Singleton¹, Singleton | ✅ 安全 |
| `IApiHealthMonitor` | `ApiHealthMonitor` | `IApiHealthCheckService`, `ILogger` | Singleton, Singleton¹ | ✅ 安全 |
| `IConnectionSettingsService` | `ConnectionSettingsService` | `IOptions<ApiClientOptions>`, `ILogger` | Singleton, Singleton² | ✅ 安全 |
| `IApiRouter` | `ApiRouter` | `IConnectionSettingsService` | Singleton | ✅ 安全 |

### HTTP 层（注册于 `HttpServiceRegistrationExtensions`）

| 接口/类 | 生命周期 | 结论 |
|---------|---------|------|
| `AuthorizationMessageHandler` | Singleton | ✅ 安全 |
| `TokenRefreshHandler` | Singleton | ✅ 安全 |
| `LoggingHttpHandler` | Singleton | ✅ 安全 |
| `HttpClient` | Singleton | ✅ 安全 |
| `IAuthApi` / `IPatientApi` / `IUserApi` / `IHerbApi` / `IFormulaApi` / `IMedicalCaseApi` / `IRegistrationApi` / `IDiagnosticsApi` | Singleton (Refit) | ✅ 安全 |

### ViewModel 服务层（注册于 `ViewModelServicesExtensions`）

| 接口 | 生命周期 | 结论 |
|------|---------|------|
| `IViewModelServices` | Singleton | ✅ 安全 |
| `IUiThreadDispatcher` | Singleton | ✅ 安全 |
| `IDialogManager` | Singleton | ✅ 安全 |
| `IAsyncExecutor` | Singleton | ✅ 安全 |
| `IToastService` | Singleton | ✅ 安全 |
| `IPerformanceMonitor` | Singleton | ✅ 安全 |

### Repository 层（注册于 `DataSourceRegistrationExtensions`）

| 接口 | 生命周期 | 结论 |
|------|---------|------|
| `ICurrentUserProvider` | Singleton | ✅ 安全 |
| `IConnectionModeService` | Singleton | ✅ 安全 |
| `IEmbeddedLocalWebApiService` | Singleton | ✅ 安全 |

---

## 二、关键设计分析

### StartupPipeline 与 IStartupStep（Transient）

`IStartupPipeline` 是 Singleton，但 `IStartupStep` 注册为 Transient（5 个实现）。**这不是问题**，因为：

1. `StartupPipeline` 的构造函数只接受 `ILogger` 和 `IPerformanceMonitor`（均为 Singleton）
2. 启动步骤通过 `RegisterStep()` 方法在运行时添加，而非构造函数注入
3. `AppStartupOrchestrator.RegisterSteps()` 在启动时从容器解析 Transient 步骤并传入
4. Singleton 持有的是已创建的对象引用，不是 DI 注册本身

```csharp
// StartupPipeline 构造函数 — 无 IStartupStep 依赖
public StartupPipeline(
    ILogger<StartupPipeline> logger,
    IPerformanceMonitor performanceMonitor)

// 步骤在运行时添加
pipeline.RegisterStep(_container.Resolve<IStartupStep>("ErrorHandling"));
```

### Repository 注册为 Transient

所有 Repository（`IPatientRepository`, `IHerbRepository` 等）注册为 Transient。这是正确的，因为：
- Repository 是无状态的 HTTP 客户端包装
- Transient 确保每次获取新实例，避免陈旧引用
- 无 Singleton 通过构造函数注入 Repository

---

## 三、系统性隐患：ILogger\<T\> 开放泛型注册

### 问题描述

在 `LoggingRegistrationExtensions.RegisterLoggerFactory()` 中：

```csharp
// 开放泛型 — Transient
containerRegistry.Register(typeof(ILogger<>), typeof(Logger<>));

// 具体泛型 — Singleton（仅部分类型）
containerRegistry.RegisterSingleton<ILogger<T>>(resolver =>
    resolver.Resolve<ILoggerFactory>().CreateLogger<T>());
```

`ILogger<>` 开放泛型注册为 **Transient**，但部分 Singleton 服务通过具体注册获得 **Singleton** 的 `ILogger<T>`。

### 受影响的 Singleton 服务

以下 Singleton 服务的 `ILogger<T>` 依赖回退到开放泛型 Transient 注册（无显式 `RegisterSingleton<ILogger<T>>`）：

| Singleton 服务 | ILogger 类型 | 实际生命周期 |
|---------------|-------------|-------------|
| `DesktopCacheManager` | `ILogger<DesktopCacheManager>` | Transient（回退） |
| `NavigationManager` | `ILogger<NavigationManager>` | Transient（回退） |
| `StatusBarManager` | `ILogger<StatusBarManager>` | Transient（回退） |
| `NavigationHistoryService` | `ILogger<NavigationHistoryService>` | Transient（回退） |
| `ModuleLazyLoader` | `ILogger<ModuleLazyLoader>` | Transient（回退） |
| `RegionMonitor` | `ILogger<RegionMonitor>` | Transient（回退） |
| `ShellDialogHelper` | `ILogger<ShellDialogHelper>` | Transient（回退） |
| `LoginStateManager` | `ILogger<LoginStateManager>` | Transient（回退） |
| `ShellEventCoordinator` | `ILogger<ShellEventCoordinator>` | Transient（回退） |
| `ApiHealthMonitor` | `ILogger<ApiHealthMonitor>` | Transient（回退） |
| `ConnectionSettingsService` | `ILogger<ConnectionSettingsService>` | Transient（回退） |
| `ClinicSettingsService` | `ILogger<ClinicSettingsService>` | Transient（回退） |
| `StartupPipeline` | `ILogger<StartupPipeline>` | Transient（回退） |

### 风险评估

**实际风险：低**

- DryIoc 允许 Singleton→Transient（"captive dependency"），不会抛异常
- Singleton 构造时解析一次 Transient `Logger<T>`，之后一直持有该实例
- `Logger<T>` 是无状态的，捕获的实例不会"过期"或"失效"
- 唯一影响：这些 `Logger<T>` 实例在首次解析时创建，不会被 GC（因为 Singleton 持有引用），但内存开销极小

### 建议

为所有被 Singleton 使用的 `ILogger<T>` 添加显式 `RegisterSingleton` 注册，消除依赖链中的隐式 Transient 回退。这属于代码整洁性改进，非紧急修复。

---

## 四、Transients 注册清单（非问题，仅记录）

以下注册为 Transient，无 Singleton 依赖：

| 类别 | 注册 |
|------|------|
| Startup Steps | `IStartupStep` × 5（ErrorHandling, ModuleCoordinator, LocalWebApi, ApiHealthCheck, Warmup） |
| Repositories | `IPatientRepository`, `IHerbRepository`, `IFormulaRepository`, `IUserRepository`, `IMedicalCaseRepository`, `IRegistrationRepository` |
| ViewModel Services | `ILoadingStateManager`, `IPaginationService`, `ISearchService`, `IErrorHandler`, `ISelectionService<>`, `IDetailEditorService<>`, `IListViewServices<>`, `IMasterDetailServices<,>` |
| HTTP | `ITokenRefreshHandler`（指向 Singleton `TokenRefreshHandler` 的转发注册） |
| Logging | `ILogger<>` 开放泛型 |

---

## 五、结论

| 检查项 | 结果 |
|--------|------|
| Singleton→Transient（危险） | ✅ **无** — 无 Singleton 通过构造函数注入 Transient 服务 |
| Singleton→Singleton（安全） | ✅ 正常 — 所有 Singleton 依赖均为 Singleton |
| Singleton→Prism-managed（安全） | ✅ 正常 — `IEventAggregator`, `IRegionManager`, `IDialogService` 等由 Prism 管理 |
| Singleton→IOptions（安全） | ✅ 正常 — 所有 Options 通过 `RegisterInstance` 注册为 Singleton |
| ILogger 开放泛型回退 | ⚠️ **低风险** — 13 个 Singleton 的 ILogger 回退到 Transient 开放泛型 |

**总体评价：** DI Singleton 依赖链健康，无阻塞性问题。建议将 `ILogger<>` 开放泛型回退作为后续代码整洁性改进处理。

# Desktop 错误处理规范（Error Handling）

> 版本: v1.0 | 日期: 2026-09-13 | 依据: 代码实际（src/Client/Desktop）+ 事实清单口径

> **计数口径（全文统一）**：View **30** / Control **33** / Dialog **7** / Root 1；ViewModel **55**；XAML 合计 **82** = 视图 71 + 资源/模板 11。
> 本文只描述代码可证的错误处理链路；未实现或与既有文档不符之处单列「已知落差」，推断项标 `[待确认]`。

## 目录

1. [三层兜底模型](#1-三层兜底模型)
2. [第一层：全局异常处理器](#2-第一层全局异常处理器)
3. [第二层：对话框与 Toast](#3-第二层对话框与-toast)
4. [第三层：页面内呈现](#4-第三层页面内呈现)
5. [错误分类与恢复动作](#5-错误分类与恢复动作)
6. [认证与会话失效链路](#6-认证与会话失效链路)
7. [审计与关联 ID 口径](#7-审计与关联-id-口径)
8. [已知落差](#8-已知落差)
9. [事实来源](#9-事实来源)

---

## 1. 三层兜底模型

```
┌ 第 1 层：全局兜底（进程级，仅记录，不面向用户） ────────────────────────────┐
│ DesktopExceptionHandler：AppDomain.UnhandledException / TaskScheduler        │
│ .UnobservedTaskException  →  Serilog（LogCritical / LogError）+ SetObserved   │
└─────────────────────────────────────────────────────────────────────────────┘
                    ▲ 未被业务捕获的异常
┌ 第 2 层：服务与提示（面向用户，落点由调用方选择） ───────────────────────────┐
│ IToastService（3000ms / 错误 4000ms） │ ICommonDialogService（MessageBox）     │
│ IDialogManager（Prism MessageDialog / ConfirmationDialog）                    │
│ IUserNotificationService（MessageBox + ClientErrorMessageMapper 文案）        │
│ ShellDialogHelper（成功/错误→Toast；警告/确认→CommonDialogService）           │
└─────────────────────────────────────────────────────────────────────────────┘
                    ▲ 业务异常经 ClientErrorMessageMapper 转用户文案
┌ 第 3 层：页面内（就地反馈） ────────────────────────────────────────────────┐
│ EmptyState（7 处空态/加载失败占位） │ 字段级 INotifyDataErrorInfo + 红框        │
│ ValidationErrorBrush #B00020 + ValidatesOnNotifyDataErrors                    │
└─────────────────────────────────────────────────────────────────────────────┘
```

**文案唯一真相源**：`Core/LYBT.Desktop.Foundation/ExceptionHandling/ClientErrorMessageMapper.cs`。业务层统一调用 `GetSafeOperationFailureMessage("<动作>", ex)` 得到「`<动作>`失败，请稍后重试」，或 `GetUserFriendlyMessage(ex)` 走异常分派。

---

## 2. 第一层：全局异常处理器

`Core/LYBT.Desktop.Infrastructure/ExceptionHandling/DesktopExceptionHandler.cs`（单例，`IDesktopExceptionHandler`）。

| 事件 | 是否注册 | 处理动作 |
|------|---------|---------|
| `AppDomain.CurrentDomain.UnhandledException` | ✅ | `LogCritical`（含 `IsTerminating`），并投递到 `HandleExceptionAsync`（仅日志），**不弹窗、不主动退出** |
| `TaskScheduler.UnobservedTaskException` | ✅ | `LogError` + `e.SetObserved()`（阻止进程终止） |
| `Application.DispatcherUnhandledException` | ❌ **未注册**（全客户端 grep 无匹配） | —（UI 线程未捕获异常无应用级兜底） |

- 注册入口：启动管道步骤 `ErrorHandlingStartupStep`（`Order = 10`，`IsRequired = true`，唯一必需步骤）→ `RegisterGlobalExceptionHandlers()`，内部有 `_isRegistered` 幂等守卫。
- 日志级别分级：`OutOfMemoryException` / `UnauthorizedAccessException` → Error；`ArgumentNullException` / `ArgumentException` / `InvalidOperationException` → Warning；`HttpRequestException` / `TimeoutException` → Information。
- 应用级启动失败兜底：`AppStartupOrchestrator` 捕获管道异常后 `LogCritical` 并以 `MessageBox.Show(ClientErrorMessageMapper.GetSafeOperationFailureMessage("启动", ex), "启动失败")` 呈现。

> **口径**：全局层面向诊断，不作为用户提示通道；用户可见反馈必须由业务代码经第 2/3 层提供。

---

## 3. 第二层：对话框与 Toast

### 3.1 Toast（`IToastService` / `ToastService` / `ToastControl`）

| 项 | 值 |
|----|----|
| 契约方法 | `ShowInfo` / `ShowSuccess` / `ShowWarning` / `ShowError` / `Show(message, ToastType, int durationMilliseconds = 3000)` |
| 严重级别 | `ToastType { Info, Success, Warning, Error }`（无 Fatal/Critical） |
| 自动消失时长 | 默认 **3000ms**；`ShowError` 固定 **4000ms**；业务层可显式覆盖（如医案工作台成功 5000ms / 失败 4000ms） |
| 显示实现 | Dispatcher 上经 `AdornerLayer` 挂到 `Application.Current.MainWindow`；同时只保留一个 Toast（新 Toast 顶掉旧的） |
| 降级 | 无主窗口或显示失败 → `MessageBox` 兜底（图标 Info→Information、Success→None、Warning→Warning、Error→Error） |
| 图标 | Info `ℹ️` / Success `✅` / Warning `⚠️` / Error `❌`（`ToastControl` 内部映射） |
| 生命周期 | `RegisterSingleton<IToastService, ToastService>()` |

### 3.2 对话框类服务对照

| 服务 | 实现载体 | 适用 |
|------|---------|------|
| `ICommonDialogService` | `System.Windows.MessageBox`（Warning / Error / YesNo / YesNoCancel） | Shell 层警告与确认；`TripleChoiceResult { Yes, No, Cancel }` 支持三选一 |
| `IDialogManager` | Prism `IDialogService` → `MessageDialog`（type=success/error/warning）、`ConfirmationDialog` | 需要与设计系统一致的模态框 |
| `IUserNotificationService` | `MessageBox` + `ClientErrorMessageMapper` 文案 | 菜单命令、导航失败等系统提示 |
| `ShellDialogHelper` | 成功/错误 → Toast；警告 → `ShowWarningAsync`；确认 → `ShowConfirmAsync` | Shell 聚合；服务缺失时仅记日志并返回 `false` |

---

## 4. 第三层：页面内呈现

### 4.1 空态/失败占位（`EmptyState`，7 处）

| 页面元素 | 文案 |
|---------|------|
| `FormulaMasterDetailControl` | 「请选择验方」+「新增验方」 |
| `HerbMasterDetailControl` | 「请选择药材」+「新增药材」 |
| `MedicalCaseMasterDetailControl` | 「请选择医案」+「在左侧列表中选择一个医案查看详情」 |
| `PatientMasterDetailControl` | 「请选择患者」+「新增患者」 |
| `PatientSelectionControl` | 「请选择患者」+「新建患者」 |
| `RegistrationListView` | 「暂无等待中的挂号记录」+「新建挂号开始工作」 |
| `UserMasterDetailControl` | 「请选择用户」+「新增用户」 |

进度/加载失败的就地占位由 `LoadingOverlay` 承担（见 [desktop-ux-loading-states.md](./desktop-ux-loading-states.md)）；加载中纯文本占位仅 `SecurityAuditLogView`（`Text="加载中..."`，绑 `IsLoading`）。

### 4.2 字段级校验

| 机制 | 位置 |
|------|------|
| `ValidatableModelBase`（`BindableBase` + `INotifyDataErrorInfo` + DataAnnotations） | `Core/LYBT.Desktop.Infrastructure/ViewModels/Base/ValidatableModelBase.cs`；`ValidateProperty` / `ValidateAll` / `HasErrors` / `GetErrors` |
| `ErrorHandler`（服务层错误集合） | `Core/LYBT.Desktop.Infrastructure/Services/ErrorHandler.cs`；`SetError` / `ClearError` / `HandleException` |
| XAML 校验绑定 | `ValidatesOnNotifyDataErrors=True`（多与 `UpdateSourceTrigger=PropertyChanged` 同用，见 5 个 `*EditControl.xaml`） |
| 红框与错误文案 | 资源 `ValidationErrorBrush`（`#B00020`）、`ValidationErrorBackgroundBrush`（`#FCE8E6`）；错误文本用显式 `TextBlock` + `ValidationErrorMessageVisibleStyle` 呈现 |
| 手写校验 | `ConsultationItem`、`PrescriptionItemViewModel` 自行实现 `INotifyDataErrorInfo`（单一 `_validationMessage` 驱动） |

> 职责边界（模块约定）：`ValidatableModelBase` 管详情模型属性级校验，`ErrorHandler` 管服务层错误管理，二者不混用。
> **未使用** `Validation.ErrorTemplate`（无自定义模板/`AdornedElementPlaceholder`），也**未使用** Prism `ErrorsContainer<T>`。

---

## 5. 错误分类与恢复动作

文案取自 `ClientErrorMessageMapper`；「恢复动作」列以代码中实际存在的机制为准。

| 类别 | 触发/状态码 | 用户可见反馈 | 恢复动作 |
|------|------------|-------------|---------|
| 网络不可达 | `SocketException`；`ApiHealthStatus.Unhealthy` | 文案「网络连接失败，请检查网络设置」；底栏徽标转「API 未连接」（橙色/灰色） | `ApiHealthMonitor` 每 10s 自动复探；`TokenRefreshTypes.NetworkError` 标 `canRetry: true` |
| 请求超时 | `HttpStatusCode.RequestTimeout` / `TimeoutException` / `TaskCanceledException` | 408 →「请求超时，请稍后重试」；`TimeoutException` →「操作超时，请稍后重试」；`TaskCanceledException` →「操作被取消」 | 用户手动重试；`RetryHealthCheckAsync`（`StatusBar.ForceCheckAsync()`）可强制复检 |
| 参数错误 | `400 BadRequest` | 「请求参数无效，请检查输入」 | 修正表单字段（配合第 4.2 节校验） |
| 未认证 | `401 Unauthorized` | 「登录已过期，请重新登录」 | `TokenRefreshHandler` 提前 5 分钟静默刷新（`SemaphoreSlim` 防并发 + 最多 3 次指数退避）；失败则强制登出并回到登录页 |
| 无权限 | `403 Forbidden` | 「您没有权限执行此操作」；刷新链路判为 `UserDisabled` →「您的账户已被禁用，请联系管理员」 | 无就地恢复；需管理员调整权限/账户状态 |
| 资源不存在 | `404 NotFound` | 「请求的资源不存在」 | 刷新列表；返回上一级页面（`BackCommand` / `Esc`） |
| 冲突 | `409 Conflict` | 「数据已被其他用户修改，请刷新后重试」 | **仅文案**：客户端无版本号/`If-Match`/合并逻辑，恢复靠用户手动刷新（见落差 4） |
| 语义校验（服务端） | `422` | **无专属文案** → 落默认兜底「操作失败，请稍后重试」`[待确认]`（是否补充 422 文案由需求方决定） | 修正输入后重试 |
| 服务端异常 | `500` / `502` / `503` / `504` | 「服务器处理异常，请稍后重试」；BadGateway / ServiceUnavailable / GatewayTimeout 各有独立文案 | 稍后重试；Sysadmin 可在「部署管理 / 日志级别」中排查 |
| 其它 | 未识别的异常 | `GetSafeOperationFailureMessage(action, ex)` →「`<action>`失败，请稍后重试」 | 重试或联系管理员，日志含 `CorrelationId` |

> 本地模式（内嵌 LocalWebAPI）的统一抛错点为 `HttpApiClientBase.EnsureSuccessOrThrowAsync`：非 2xx 时读取响应体并抛 `HttpRequestException(message, null, statusCode)`；**不使用** `EnsureSuccessStatusCode`。远端模式同样经该基类或 `ApiResponse` 信封解包（`Success=false` 且 `Data=null` 时记空信封告警）。

---

## 6. 认证与会话失效链路

```
请求发出 ──AuthorizationMessageHandler(Bearer + X-Correlation-ID)──► TokenRefreshHandler
                                                                      │
                        距过期 ≤ 5min 且用户活跃 → 静默刷新（最多 3 次退避）
                                                                      │
                                              刷新失败分级（TokenRefreshTypes）
                                                                      │
                             TokenRefreshFailedEvent(RequiresReLogin) ─┤
                                                                      ▼
   TokenLifecycleService（NotAuthenticated→Active→Warning→Expired，30s 间隔）
                                                                      │
                                                    Expired（Warning 仅 Debug 日志）
                                                                      ▼
        LoginStateManager.HandleTokenExpiredAsync()：Toast「您的登录凭证已过期，请重新登录。」
                                    ↓                     ↓
                       TokenLifecycleService.Reset()   PerformLogoutAsync()
                                                                      │
                                                  LogoutRequested 事件 │
                                                                      ▼
                  ShellEventCoordinator → NavigationCoordinator.ShowLoginDialog()
                                                                      │
                          RegionManager.RequestNavigate(LoginRegion, ViewNames.Login)
```

| 失败分级 | 文案 | 是否需重登 |
|---------|------|-----------|
| `RefreshTokenExpired` | 登录已过期，请重新登录 | ✅ |
| `RefreshTokenRevoked` | 登录凭证已失效，请重新登录 | ✅ |
| `RefreshTokenInvalid` | 登录凭证无效，请重新登录 | ✅ |
| `UserDisabled` | 您的账户已被禁用，请联系管理员 | —（403 分支产出） |
| `NetworkError` | 网络连接失败，请检查网络后重试 | ❌（`canRetry: true`） |
| `ServerError` | 服务暂时不可用，请稍后重试 | ❌ |

会话不活跃过期：`HandleSessionExpiredAsync()` → Toast「您的会话因长时间未操作已过期，请重新登录。」→ `PerformLogoutAsync()`。配置口径：`InactivityTimeoutMinutes = 30`（生产 `appsettings.Production.json` 为 5）、`WarningBeforeTimeoutMinutes = 0`（**无到期前预警**）、`ActivityCheckIntervalSeconds = 30`。

> 登出失败重试：`LogoutService` 最多 3 次，延迟 1s / 5s / 15s；登出入口统一为 `IShellLogoutService.RequestLogoutAsync()`（含活跃医案守卫），侧栏与 `MainWindowViewModel.LogoutCommand` 同源。

---

## 7. 审计与关联 ID 口径

| 项 | 客户端行为 |
|----|-----------|
| 请求头 | `AuthorizationMessageHandler` 注入 `X-Correlation-ID`（取 `Activity.Current?.Id`，缺失时取新 GUID 前 12 位） |
| 日志关联 | `LoggingHttpHandler`（`Shared/LYBT.Shared.Logging`）在请求/响应/异常日志中写入 `CorrelationId={CorrelationId}`；错误响应体经 `SensitiveDataMasker` 脱敏后再记录 |
| Provider | `ICorrelationIdProvider` 单例，源自 `LoggingBootstrap.CorrelationIdProvider`（与 Serilog 同一实例） |
| 服务端 | `CorrelationIdMiddleware` 优先取 W3C `traceparent`，回退 `X-Correlation-ID`，回写响应头并 `LogContext.PushProperty("CorrelationId", …)`；日志库表 `RequestId` / `CorrelationId` 列均为 `NVarChar(36)` |
| 响应信封 | 成功/已知业务失败：`ApiResponse<T>`（`success`/`message`/`data`/`errors`/`timestamp`/`requestId`）；Server 异常路径（X-3）：RFC 7807 ProblemDetails（`title`/`detail`/`status` + `errorCode`/`correlationId`/`traceId`）。`ApiErrorEnvelope.TryExtract` 双格式兼容 |
| 界面展示 | **当前不在 UI 展示任何追踪码**：`ClientErrorMessageMapper.TraceIdProvider` 从未赋值、`GetShortTrackingCode()` 无调用方；XAML 中无 `RequestId` / `CorrelationId` 绑定 |
| 审计查询 | 医案审计 `GET /api/v1/medicalcases/{id}/audit-logs`（`AuditLogView`）；安全审计（仅 SuperAdmin）→ `SecurityAuditLogView` |

> 呈现口径建议（尚未落地）`[待确认]`：错误 Toast/对话框展示 `GetShortTrackingCode()` 的 8 位短码，便于用户报障时对账日志列。

---

## 8. 已知落差

| # | 项 | 既有文档/预期 | 代码实际 |
|---|----|-------------|---------|
| 1 | UI 线程异常 | 通常假设有 `DispatcherUnhandledException` 兜底 | **未注册**；仅 AppDomain + TaskScheduler 两个事件 |
| 2 | 全局处理行为 | 可能被描述为「弹窗提示用户」 | 仅记日志，无 UI 交互、无退出 |
| 3 | 对话框封装 | 文档中出现 `DialogService` / `DialogServiceExtensions` / `ShowUnfinishedCaseDialogAsync` | 代码中**均不存在**；实际为 4 套并行封装（见 §3.2） |
| 4 | 并发冲突恢复 | 「409 → 刷新并重试/覆盖」 | 客户端无 `RowVersion` / `ETag` / `If-Match` / `ConcurrencyException`；仅有 409 文案映射 |
| 5 | 422 | 期望有专属提示 | 无专属文案，落默认兜底 |
| 6 | 追踪码展示 | 期望错误提示带追踪码 | 客户端无展示；`TraceIdProvider` 为死代码 |
| 7 | 辅助类 | 文档提及 `ApiResponseHelper` / `ProblemDetailsParser` | 代码不存在 |
| 8 | 健康探测周期 | `desktop-ui-design-guide.md` §8.1 写「每 30s」 | 实为 **10s**（`ApiHealthMonitor.CheckInterval`） |

---

## 9. 事实来源

- 全局层：`Core/LYBT.Desktop.Infrastructure/ExceptionHandling/DesktopExceptionHandler.cs`、`Shell/Services/Startup/Steps/ErrorHandlingStartupStep.cs`、`Shell/Services/AppStartupOrchestrator.cs`、`Shell/App.xaml.cs`。
- 提示层：`Core/LYBT.Desktop.Contracts/Services/{IToastService,ICommonDialogService,IUserNotificationService}.cs`、`Core/LYBT.Desktop.Infrastructure/Services/{Toast/ToastService,CommonDialogService,DialogManager,UserNotificationService,ErrorHandler}.cs`、`Core/LYBT.Desktop.Controls/Controls/Toast/ToastControl.xaml(.cs)`、`Shell/Services/ShellDialogHelper.cs`。
- 页面层：`Core/LYBT.Desktop.Controls/Controls/EmptyState.xaml(.cs)`、`Core/LYBT.Desktop.Infrastructure/ViewModels/Base/ValidatableModelBase.cs`、5 个 `*EditControl.xaml`、`Shell/App.xaml`（`ValidationErrorBrush`）。
- 文案与 HTTP：`Core/LYBT.Desktop.Foundation/ExceptionHandling/ClientErrorMessageMapper.cs`、`Core/LYBT.Desktop.Foundation/Http/{HttpApiClientBase,TokenRefreshHandler,AuthorizationMessageHandler}.cs`、`Core/LYBT.Desktop.Foundation/Security/{TokenRefreshTypes,LogoutService}.cs`、`src/Shared/LYBT.Shared.Models/Contracts/Common/ApiResponse.cs`。
- 认证/会话：`Core/LYBT.Desktop.Foundation/Security/TokenLifecycleService.cs`、`Shell/Services/Login/{LoginStateManager,LoginCoordinator}.cs`、`Shell/Services/ShellEventCoordinator.cs`、`Core/LYBT.Desktop.Infrastructure/Navigation/NavigationCoordinator.cs`、`Shell/appsettings.json`。
- 交叉文档：[desktop-ux-loading-states.md](./desktop-ux-loading-states.md)、[desktop-ux-interaction-spec.md](./desktop-ux-interaction-spec.md)、[desktop-ui-detailed-design.md](./desktop-ui-detailed-design.md)。

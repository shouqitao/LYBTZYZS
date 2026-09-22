# Desktop 加载状态规范（Loading States）

> 版本: v1.0 | 日期: 2026-09-13 | 依据: 代码实际（src/Client/Desktop）+ 事实清单口径

> **计数口径（全文统一）**：View **30** / Control **33** / Dialog **7** / Root 1；ViewModel **55**；XAML 合计 **82** = 视图 71 + 资源/模板 11。
> 加载态一律以代码实现为准；未实现项（如骨架屏）在「已知落差」中明确标注。

## 目录

1. [加载状态谱系](#1-加载状态谱系)
2. [组件清单](#2-组件清单)
3. [场景矩阵](#3-场景矩阵)
4. [列表分页加载](#4-列表分页加载)
5. [防重入与 CanExecute](#5-防重入与-canexecute)
6. [超时与取消](#6-超时与取消)
7. [API 健康探测与状态徽标](#7-api-健康探测与状态徽标)
8. [启动管道的可见性策略](#8-启动管道的可见性策略)
9. [已知落差](#9-已知落差)
10. [事实来源](#10-事实来源)

---

## 1. 加载状态谱系

```
(A) 状态层 —— 谁在表达「忙」
    LoadingStateManager（嵌套计数器 _loadingCount：>0 → IsLoading=true；归零才复位）
        └─ ServiceEventBridge → 各 MasterDetail VM 的 IsLoading / IsBusy / BusyMessage
    ApiHealthMonitor（Shell 单例）—— 连接健康，与业务加载解耦

(B) 契约/基类层 —— 谁在约束行为
    ILoadingStateManager：IsLoading / IsBusy / BusyMessage / Reset()
    NavigableViewModelBase：IsLoading、IsBusy + 反转属性 IsNotLoading / IsNotBusy + On*ChangedCore 钩子
    DialogViewModelBase：CanConfirm() => !IsBusy && !IsLoading
    MasterDetailCommandGroup：CanSave/CanEdit/CanDelete/CanRestore 均含 !IsBusy

(C) 视图层 —— 用户看到什么
    LoadingOverlay（200ms 延迟遮罩 + 不确定进度条）← 列表/详情主通道
    ProgressBar（6 处：5 不确定 + 1 确定型）、文本占位（仅 SecurityAuditLogView）
    按钮禁用（IsEnabled="{Binding IsNotLoading / IsNotTesting}"）、Skeleton/骨架屏：不存在
```

---

## 2. 组件清单

### 2.1 `LoadingOverlay`（`Core/LYBT.Desktop.Controls/Controls/LoadingOverlay.xaml(.cs)`）

| 依赖属性 | 类型/默认 | 说明 |
|---------|----------|------|
| `IsLoading` | `bool` / `false` | 由 VM 绑定；置 `true` 后启动延迟计时器 |
| `IsOverlayVisible` | `bool` / `false` | **内部置位**：`IsLoading=true` 且持续 **200ms** 后才显示（`DisplayDelayMs = 200`），避免闪烁 |
| `LoadingText` | `string` / `"正在加载..."` | 遮罩文案（调用方常绑 `BusyMessage` / `StatusMessage`） |

- 视觉：`DarkOpacityBrush` 遮罩 + 200×4 不确定态 `ProgressBar` + 14px 文案。
- **不提供** `IsBusy` / `Message` 命名属性。

使用位置（11 处）：

| 位置 | 绑定 |
|------|------|
| 5 个主从控件（`Formula` / `Herb` / `MedicalCase` / `Patient` / `User`）各 2 处 | 主列表：`IsLoading` + `BusyMessage`；详情面板：`IsBusy` + 「正在处理...」 |
| `RegistrationListView` | `IsLoading="{Binding IsBusy}"`、`LoadingText="{Binding StatusMessage}"`、`Grid.RowSpan="4"` |

> **Shell 层（`MainWindow` / `AppShell` / `FooterControl`）无加载遮罩**——壳级不阻塞用户，加载态一律落在内容区。

### 2.2 `ProgressBar`（全仓 XAML 共 6 处）

| # | 位置 | 形态 | 确定性 |
|---|------|------|--------|
| 1 | `LoadingOverlay.xaml` | 200×4 `IsIndeterminate="True"` | 不确定 |
| 2 | `FirstRunSetupView.xaml` / `LoginView.xaml` / `ServerConfigView.xaml` | 180×4 `IsIndeterminate="True"`（登录页附静态文本「正在登录...」，Auth 两页附 `#20000000` 遮罩层） | 不确定 |
| 3 | `RegistrationCreateDialog.xaml` | 200×4 `IsIndeterminate="True"`，文本绑 `StatusMessage` | 不确定 |
| 4 | `DeploymentView.xaml` | `Value="{Binding UploadProgress}" Maximum="100"`，`Visibility=IsUploading` | **确定型（唯一）** |

> 部署上传进度只有 `0` → `100` 两次赋值（中间无 `IProgress` 增量上报）；患者导入模型中的 `PercentComplete` 字段未绑定到任何 XAML 进度条。

### 2.3 骨架屏

- `Skeleton` / Shimmer 占位：**代码中不存在**（全仓 grep 无匹配）。
- 现有替代：`LoadingOverlay` 遮罩、纯文本「加载中...」（`SecurityAuditLogView`）、按钮禁用；新增骨架屏需先补控件并登记到 `Control` 清单。

---

## 3. 场景矩阵

| 场景 | 加载表达 | 证据/组件 | 备注 |
|------|---------|----------|------|
| 首屏（登录页 / 首次运行向导 / 服务器配置） | 登录按钮 `LoginCommand` 禁用 + 180×4 不确定进度条 + 文本「正在登录...」；Auth 两页用 `#20000000` 遮罩层 | `LoginView.xaml`、`LoginViewModel`、`FirstRunSetupView.xaml`、`ServerConfigView.xaml` | `IsLoading=true` 后立即 `LoginCommand.NotifyCanExecuteChanged()` |
| 列表首次加载 / 刷新 | `LoadingOverlay` 覆盖列表区（主列表侧） | 5 个主从控件 | 文案取 `BusyMessage` |
| 列表分页 | 分页条按钮 + 加载遮罩；`TotalCount/TotalPages` 由 `PaginationService` 驱动 | `UnifiedPaginationBar` ×5 | 见 §4 |
| 详情加载 / 切换选中 | 详情侧 `LoadingOverlay`（`IsBusy` + 「正在处理...」） | 5 个主从控件 | 与主列表遮罩分离，互不覆盖 |
| 保存 / 校验 | 命令 `CanExecute` 含 `!IsBusy`；保存期间按钮不可用 | `MasterDetailCommandGroup`、`MedicalCaseCommandsViewModel` | 医案保存后 Toast 5000ms |
| 导入 / 导出 | **无遮罩**：确认对话框 → 执行 → 成功/失败对话框（`MasterDetailServices.Dialog.*`）；导入完成后 `RefreshAsync()` 走列表加载遮罩 | `PatientMasterDetailViewModel.ImportPatientsAsync/ExportPatientsAsync`（`OpenFileDialog` 选文件） | 无百分比进度回传 |
| 备份 / 恢复 | **确定型进度条** `ProgressBar`（绑定 `IsOperationRunning`/`ProgressPercent`/`OperationPhase`）+ `StatusMessage`/`ErrorMessage` 文本反馈；恢复前二次确认面板（红色警告 + 输入「确认恢复」+ 5s 倒计时，`ConfirmRestoreCommand` 谓词 `CanConfirmRestore` 门禁） | `BackupManagementViewModel`、`BackupManagementView`；进度经 `GET /api/v1/backup/status` 每 1s 轮询（服务端读 `sys.dm_exec_requests.percent_complete`）；引擎 `SqlServerBackupService` 以 `SemaphoreSlim(1,1)` 串行化 | 备份/恢复期间 `IsOperationRunning` 守卫命令重入，按钮由谓词/守卫禁用 |
| 部署上传 | **确定型进度条** `UploadProgress`（0→100） | `DeploymentView.xaml`、`DeploymentViewModel` | 唯一确定型进度 |
| 读卡 / 后台轮询 | 读卡：`Host.SetBusy(true, "正在读取身份证...")` + 重复请求忽略（`IsReading`）；轮询：无阻塞提示（静默刷新） | `CardReaderViewModel.ManualReadCardAsync`；挂号 30s `PeriodicTimer`、Sysadmin 面板 30s `Task.Delay` | 轮询失败不打断用户 |

**约定**：耗时 < 200ms 的操作不显示遮罩（由 `LoadingOverlay` 延迟机制自然抑制），避免闪烁；需要明确反馈的写操作优先用按钮禁用 + Toast 结果。

---

## 4. 列表分页加载

### 4.1 数据契约

| 项 | 事实 |
|----|------|
| 分页结果类型与构造 | `PagedResult<T>`（`src/Shared/LYBT.Shared.Models/Contracts/Common/PagedResult.cs`）——**不存在** `IPagedResult`；`EntityApiClientRepositoryBase` 填充 `Items` / `TotalCount` / `CurrentPage` / `PageSize` |
| 分页状态服务 | `IPaginationService`：`TotalCount`、`TotalPages = ceil(TotalCount / PageSize)`；重置时 `CurrentPage = 1; TotalCount = 0;` |
| 失败语义 | `ApiClientRepositoryBase` 失败路径返回 `TotalCount = FailureCount = 失败条数`、`IsSuccess = false`；而 VM 的 `LoadListAsync` 仅在 `Success && Data != null` 时回写 `Pagination.TotalCount` → 列表页失败时**通常保留上一次总数**（是否改为清空/显示失败数 `[待确认]`） |

### 4.2 `UnifiedPaginationBar`（`Core/LYBT.Desktop.Controls/Controls/UnifiedPaginationBar.xaml(.cs)`）

10 个依赖属性、**无 CLR 事件**（全部经 `ICommand` 回传）：

`CurrentPage`(1)、`TotalPages`(1)、`PageSize`(20，默认双向绑定)、`TotalCount`(0)、`PageSizes` 以及 `PreviousPageCommand` / `NextPageCommand` / `PageSizeChangedCommand` / `FirstPageCommand` / `LastPageCommand`。

使用位置（5 处）：`FormulaMasterDetailControl`、`HerbMasterDetailControl`、`MedicalCaseMasterDetailControl`、`PatientMasterDetailControl`、`UserMasterDetailControl`（绑定完全一致：5 个数据属性 + 5 个命令）；总数文案为「共 N 条记录」。

例外：`SecurityAuditLogView` 未使用统一控件，自建「上一页 / 下一页」按钮（`PreviousPageCommand` / `NextPageCommand`）。

---

## 5. 防重入与 CanExecute

### 5.1 忙碌属性分布

| 属性 | 使用范围 | 备注 |
|------|---------|------|
| `IsLoading` / `IsBusy` / `BusyMessage` | 全局（`LoadingStateManager` → VM） | `IsBusy` 语义为「用于 UI 禁用」 |
| `IsSaving` | `ConfigurationCenterViewModel`、`ServerConfigSectionViewModel` | 保存防重入（`if (IsSaving) return;`） |
| `IsUploading` / `IsRestarting` | `DeploymentViewModel` | 上传/重启互斥 |
| `IsRunning` | `CardReaderDiagnosticsViewModel` | 诊断运行中禁止改设置 |
| `IsReading` | `CardReaderViewModel` | 读卡中忽略重复请求 |
| `IsBackingUp` / `IsRestoring` | `BackupManagementViewModel` | 备份/恢复重入守卫（`if (IsBackingUp) return;`） |
| `IsExecuting` / `IsProcessing` | **不存在**（`_isProcessingTransition` 仅为编辑状态机的私有重入保护） | — |

### 5.2 统一约定

1. **命令可执行性 = `!IsBusy && !IsLoading` + 业务前置条件**；属性变更时显式 `NotifyCanExecuteChanged()`。
   - 例：`RegistrationListViewModel.OnIsBusyChangedCore` 中一次刷新 `CreateRegistrationCommand` / `StartVisitCommand` / `CancelRegistrationCommand`。
2. **`AsyncRelayCommand` 默认拒绝并发执行**（`allowConcurrentExecutions: false`），双击仅首次生效；登录命令额外把 `!IsLoading` 写进 `CanExecute`。
3. **进入执行即禁用**：`LoginViewModel` 设 `IsLoading = true` 后立即 `LoginCommand.NotifyCanExecuteChanged()`，所有返回路径（含异常）在 `finally` 复位。
4. **对话框确认门禁**：`DialogViewModelBase.CanConfirm()`；`IsLoading` / `IsBusy` 变化时刷新 `ConfirmCommand`。
5. **服务端侧串行化**：登录/登出 `SemaphoreSlim(1,1)` 且 `WaitAsync(30s)`；Token 刷新与备份各自持锁；健康探测用 `WaitAsync(0)` 立即跳过（不排队）。

---

## 6. 超时与取消

| 场景 | 超时值 | 位置 |
|------|-------|------|
| 远端主 `HttpClient` | **60s**（`appsettings.json` → `ApiClientOptions.TimeoutSeconds`） | `Shell/Extensions/UnifiedApiClientExtensions.cs` |
| 本地模式 `HttpClient` | **30s**（硬编码） | 同上 |
| Token 刷新专用 `HttpClient` | **30s** | `TokenRefreshHandler` |
| 连接模式探测 / API 健康检查单次探测 | **3s**（远程 / 本地各一）/ **5s**（`ApiHealthCheckService.CheckHealthAsync(timeout = 5000)`） | `ConnectionModeService`、`ApiHealthCheckService` |
| 健康探测周期 / 超时 / 断路 / 恢复 | **10s / 5s / 3 次 / 30s** | `ApiHealthMonitor` |
| 启动步骤「API 健康检查」 | **10s**（DI 注释写 5s，实际 10s，见落差 4） | `ApiHealthCheckStartupStep` |
| 页面导航 | **10s** | `NavigationCoordinator.NavigationTimeoutSeconds` |
| 登录/登出互斥等待 | **30s** | `LoginCoordinator` |
| 会话不活跃 | 30 分钟（生产 5 分钟），检查间隔 30s | `Shell/appsettings.json` |
| 读卡器连接 / 读取 | 5s / 10s | `Shell/appsettings.json` |
| 重试策略（客户端） | Token 刷新：最多 3 次指数退避（1s/2s/…，仅网络/服务端错误可重试）；登出：最多 3 次（1s / 5s / 15s） | `TokenRefreshHandler`、`LogoutService` |
| SQL 备份/恢复命令 | 无超时（`CommandTimeout=0`，大库备份/恢复不限时）；进度轮询 500ms | `SqlServerBackupService` |

取消机制：`CancellationTokenSource` 用于搜索防抖、自动读卡、登录后台初始化、挂号自动刷新、SignalR 轮询（15s）、Sysadmin 面板轮询（30s）、健康监控（仅 Dispose 时取消）。**启动管道未传 `CancellationToken`**（`pipeline.ExecuteAsync()` 使用默认 token）→ 启动过程无法取消。

> Polly 重试策略：仅存在于 `UnifiedApiClientExtensions.cs` 注释中，**未注册** `AddHttpClient` / `AddPolicyHandler`。

---

## 7. API 健康探测与状态徽标

### 7.1 `ApiHealthMonitor`（`Shell/Services/HealthCheck/ApiHealthMonitor.cs`，单例）

| 配置 | 值 |
|------|-----|
| `CheckInterval` | **10s**（定时器首次立即执行） |
| `CheckTimeout` | 5s（每次探测独立 `CancellationTokenSource`） |
| `CircuitBreakerThreshold` | 连续 3 次失败 → 断路器 `Open` |
| `CircuitBreakerRecoveryTime` | 30s 后转 `HalfOpen` 试探 |

| 枚举 | 取值 |
|------|------|
| `ApiMonitorHealthStatus`（三态，监控器）/ `ApiHealthStatus`（UI 用） | `Checking` / `Healthy` / `Unhealthy` |
| `ApiConnectionState`（六态）/ `CircuitState` | `Unknown` / `Checking` / `Connected` / `Degraded` / `Disconnected` / `Reconnecting`；`Closed` / `Open` / `HalfOpen` |

- 并发保护：`SemaphoreSlim.WaitAsync(0)`——已有探测在执行则直接跳过，不排队。
- 事件：`StatusChanged`（仅状态实际变化时触发）、`CheckCompleted`；`ForceCheckAsync()` 支持手动立即探测。

### 7.2 徽标绑定（两处，枚举经 `StatusBarManager` 二次映射）

```
ApiHealthMonitor.ApiMonitorHealthStatus
        │  StatusBarManager（UI 线程 InvokeAsync）
        ▼
ApiHealthStatus { Checking, Healthy, Unhealthy }
        ├─► FooterControl：PackIcon Wifi / WifiOff / WifiStrengthAlertOutline
        │   颜色 Green / Orange / Gray；文案「API 已连接 / API 未连接 / API 检测中…」
        └─► LoginView：圆点 DataTrigger（Healthy #2E7D32 / Checking #9E9E9E / Unhealthy #C62828）
                      + 文案（「WebAPI 已连接」/「WebAPI 连接失败: …」）
```

- 手动重试入口：`MainWindowViewModel.RetryHealthCheckCommand` → `StatusBar.ForceCheckAsync()`；徽标**不阻塞操作**——即使 `Unhealthy`，页面仍可进入，由具体请求的错误处理给出提示（见 [desktop-ux-error-handling.md](./desktop-ux-error-handling.md)）。

---

## 8. 启动管道的可见性策略

### 8.1 启动顺序（`Shell/App.xaml.cs`）

```
① 单实例 Mutex（已有实例 → 激活其窗口并 Shutdown）
② 控制台编码 UTF-8（仅当附加控制台）
③ Serilog 初始化 + 版本/commit/pid 首条日志
④ base.OnStartup(e)：注册类型 → 模块目录 → CreateShell()（MainWindow）
⑤ InitializeShell() 为空实现：不隐藏窗口
⑥ OnInitialized：MainWindow.Show() 立即显示，随后后台 RunStartupAsync()
```

> 配置（`appsettings.json`）在 DI 阶段注入；主题/资源来自 `App.xaml` 的静态合并字典；首运行向导与登录导航分别属于 `LoginViewModel.BackgroundInitAsync` 与 `MainWindow` 的 `LoginRegion`——**三者都不在启动管道内**。

### 8.2 管道 5 步（按 `Order` 升序执行）

| 顺序 | 步骤 | `Order` | `IsRequired` | 可见性策略 |
|------|------|--------|-------------|-----------|
| 1 | `ErrorHandlingStartupStep` | 10 | ✅ 唯一必需 | 无 UI；失败 → 管道 `Failed` → `MessageBox`「启动失败」 |
| 2 | `ModuleCoordinatorStartupStep` | 20 | ❌ | 无 UI；模块按需加载（`AuthenticationModule` = WhenAvailable，业务模块 = OnDemand） |
| 3 | `ApiHealthCheckStartupStep` | 40 | ❌ | **fire-and-forget**：立即返回成功，后台探测（不改徽标初值 `Checking`） |
| 4 | `LocalWebApiStartupStep` | 250 | ❌ | 无 UI；本地模式服务启动 |
| 5 | `DesktopUpdateStartupStep` | 400 | ❌ | 无 UI；更新检查在后台 |
| — | `AppStartupOrchestrator` 异常兜底 | — | — | `LogCritical` + `MessageBox`「启动失败」 |

- 可选步骤失败仅 `LogWarning` 并继续；各步骤 `ParallelGroup=null`，严格按 `Order` 顺序执行。用户可感知的启动反馈只有：主窗口立即出现 + 登录页 `BackgroundInitAsync`（100ms 延迟后依次：首运行向导 → 凭据恢复 → API 状态 → 连接模式）+ 底栏徽标三态。

---

## 9. 已知落差

| # | 项 | 既有文档口径 | 代码实际 |
|---|----|-------------|---------|
| 1 | 骨架屏 | 设计规范常要求 Skeleton | 不存在，统一用遮罩 + 不确定进度条/文本 |
| 2 | `MaterialDesignCircularProgressBar` | `DESKTOP_ARCHITECTURE_STANDARD.md` 示例使用 | 真实 XAML 中无引用；ProgressBar 共 6 处 |
| 3 | 健康探测周期 | `desktop-ui-design-guide.md` 写「每 30s」、另有文档写「Polly + 15s」 | `ApiHealthMonitor` 为 **10s**；无 Polly 注册 |
| 4 | 健康检查启动步骤超时 / 步骤集 | DI 注释写「5 秒超时」；`Shell/README.md`、`AGENTS.md` 提到 `WarmupStartupStep`（Order=50） | 实际默认 **10s**（注册未传参）；`WarmupStartupStep` 不存在，第 5 步为 `DesktopUpdateStartupStep`（Order=400） |
| 5 | 分页失败语义 | 期望「共 N 条」表示真实总数 | 仓储失败路径 `TotalCount = FailureCount = 失败条数`，但 VM 仅在成功分支回写 `Pagination.TotalCount`，界面通常保留旧总数 |
| 6 | 导入进度 | 期望百分比进度条 | 患者导入仅有 `PercentComplete` 模型字段，无进度条绑定 |
| 7 | 管道取消 | 期望可取消启动 | `ExecuteAsync()` 未传 `CancellationToken` |

---

## 10. 事实来源

- 控件与视觉：`Core/LYBT.Desktop.Controls/Controls/LoadingOverlay.xaml(.cs)`、`UnifiedPaginationBar.xaml(.cs)`、5 个 `*MasterDetailControl.xaml`、`Modules/LYBT.Desktop.Registrations/Views/RegistrationListView.xaml`、`Roles/LYBT.Desktop.Admin/Sysadmin/Views/DeploymentView.xaml`。
- 状态与基类：`Core/LYBT.Desktop.Infrastructure/Services/LoadingStateManager.cs`、`Services/Interfaces/ILoadingStateManager.cs`、`ViewModels/Base/NavigableViewModelBase.cs`、`ViewModels/Base/DialogViewModelBase.cs`、`ViewModels/Composition/{MasterDetailCommandGroup,ServiceEventBridge}.cs`、`ViewModels/MasterDetailViewModelBase.cs`。
- 分页：`src/Shared/LYBT.Shared.Models/Contracts/Common/PagedResult.cs`、`Core/LYBT.Desktop.Foundation/Repositories/EntityApiClientRepositoryBase.cs`、`Core/LYBT.Desktop.Infrastructure/Services/PaginationService.cs`。
- 健康与启动：`Shell/Services/HealthCheck/ApiHealthMonitor.cs`、`Core/LYBT.Desktop.Contracts/Services/IApiHealthMonitor.cs`、`Shell/Services/StatusBarManager.cs`、`Shell/Services/AppStartupOrchestrator.cs`、`Shell/Services/Startup/StartupPipeline.cs`、`Shell/Services/Startup/Steps/*.cs`、`Shell/App.xaml.cs`。
- 超时与取消：`Shell/Extensions/UnifiedApiClientExtensions.cs`、`Core/LYBT.Desktop.Foundation/{Http/TokenRefreshHandler,HealthCheck/ApiHealthCheckService,Services/ConnectionModeService,Security/LogoutService}.cs`、`Core/LYBT.Desktop.Infrastructure/Navigation/NavigationCoordinator.cs`、`Shell/Services/Login/LoginCoordinator.cs`、`Shell/appsettings.json`。
- 交叉文档：[desktop-ux-error-handling.md](./desktop-ux-error-handling.md)、[desktop-ux-interaction-spec.md](./desktop-ux-interaction-spec.md)、[desktop-ui-detailed-design.md](./desktop-ui-detailed-design.md)。

# LYBT.Desktop.Registrations

挂号管理模块 -- 管理患者挂号队列的完整生命周期（创建→等待→接诊/取消）。

## 项目定位

前台挂号是诊所日常运营的入口流程。本模块为 Receptionist 提供创建挂号能力，为 Doctor 提供个人队列视图和接诊入口，队列 30 秒轮询 + SignalR 实时推送（US-REG-008）。接诊操作创建 MedicalCase 并导航至 MedicalCaseWorkspace（Clinical 编辑模式）。PRD: registration.md US-REG-001~006。

## 目录结构

```
LYBT.Desktop.Registrations/
├── RegistrationModule.cs               # Prism IModule 入口（依赖 Authentication/Patients/Users）
├── ViewModels/
│   └── RegistrationListViewModel.cs    # 队列列表 VM（角色感知 + 自动刷新 + SignalR 订阅）
├── Views/
│   └── RegistrationListView.xaml(.cs)  # 队列导航视图
├── Dialogs/
│   ├── RegistrationCreateDialog.xaml(.cs)
│   └── RegistrationCreateDialogViewModel.cs
├── Events/
│   └── RegistrationRefreshedEvent.cs   # 待诊列表刷新通知（PubSubEvent）
├── Models/
│   ├── RegistrationDetailModel.cs      # Detail 模型（ValidatableModelBase）
│   └── Items/
│       └── RegistrationEditContext.cs  # 新建挂号编辑上下文（ValidatableModelBase）
├── Repositories/
│   └── RegistrationRepository.cs       # 仓储（EntityApiClientRepositoryBase + IRegistrationRepository）
└── Services/
    ├── RegistrationService.cs          # 挂号服务（IRegistrationService，CommandResult 包装）
    └── SignalRClient.cs                # ISignalRClient：SignalR 推送 + 15s 降级轮询
```

## 视图 / ViewModel 清单

**计数口径**：View = 页面/导航级 XAML（`*/Views/*.xaml`）；Control = 内嵌组件（`*/Controls/*.xaml`）；Dialog = `*/Dialogs/**/*.xaml`；ViewModel 按「每文件 1 个 VM 类型」计。

| 类别 | 数量 | 明细 |
|------|------|------|
| View | 1 | `RegistrationListView`（`RegisterForNavigation`） |
| Control | 0 | — |
| Dialog | 1 | `RegistrationCreateDialog`（`RegisterDialog`） |
| ViewModel | 2 | `RegistrationListViewModel`、`RegistrationCreateDialogViewModel` |

> 全桌面口径：View 30 / Control 33 / Dialog 7 / ViewModel 55（代码实际：`src/Client/Desktop`）。

## 核心组件

### RegistrationModule

**设计依据**: Prism IModule 标准入口，声明模块依赖和 DI 注册。

| 注册项 | 类型 | 说明 |
|--------|------|------|
| `IRegistrationService` → `RegistrationService` | Service | 挂号服务实现（`IRegistrationRepository` 由 Shell DI 注册） |
| `RegistrationEditContext` | Model | 新建挂号编辑上下文（瞬时） |
| `ISignalRClient` → `SignalRClient` | Service | 单例：SignalR 连接生命周期跨页面（US-REG-008） |
| `RegistrationListViewModel` | ViewModel | 队列列表 VM |
| `RegistrationListView` | Navigation | 导航视图 |
| `RegistrationCreateDialog` + VM | Dialog | 新建挂号弹窗（VM 经构造注入 `IPatientService`/`IUserService`） |

**模块依赖**: `AuthenticationModule`、`PatientsModule`、`UsersModule`（`[ModuleDependency]` 声明运行时加载顺序；C# 层不引用这两个模块，仅经 `LYBT.Desktop.Contracts` 接口交互）

### RegistrationListViewModel

**设计依据**: NavigableViewModelBase 子类，角色感知队列 + 自动刷新 + SignalR 推送 + 接诊导航。

| 属性/命令 | 说明 |
|-----------|------|
| `WaitingQueue` | 等待队列集合 |
| `SelectedRegistration` | 当前选中挂号 |
| `RefreshCommand` | 手动刷新队列 |
| `CreateRegistrationCommand` | 打开新建挂号弹窗（`IDialogService.ShowDialog("RegistrationCreateDialog")`） |
| `StartVisitCommand` | 接诊：创建 MedicalCase → 导航 Workspace |
| `CancelRegistrationCommand` | 取消挂号（仅 Waiting + Receptionist） |

- **角色感知**: Doctor 只看自己队列（传 `doctorId`），Receptionist/Admin/SuperAdmin 看全部；`IsReceptionist`/`IsDoctor` 由构造时 `SessionManager.CurrentUser.Role` 判定
- **自动刷新**: `PeriodicTimer` 30 秒间隔，`OnNavigatedToCore` 启动、`OnNavigatedFromCore` 停止并 `_signalRClient.StopAsync()`
- **实时推送（US-REG-008）**: Doctor 登录后 `ISignalRClient.StartAsync(doctorId)`；收到推送或降级轮询（15 秒）时发布 `RegistrationRefreshedEvent`，VM 订阅后刷新队列（订阅 token 在 `Dispose` 中退订）
- **接诊流程**: `StartVisitAsync()` → 获取 `MedicalCaseId` → `IPatientApi.GetPatientByIdAsync()` → 导航 `MedicalCaseWorkspace`（`WorkspaceMode.Clinical` + `EditState.Editing`）

### SignalRClient — 实时通知

**设计依据**: `ISignalRClient` 实现（`RegistrationModule` 内声明接口）；连接 `hubs/registration`，推送 `RegistrationRefreshedEvent`；断线时降级为 15 秒 `PollInterval` 轮询同样发布事件。

### RegistrationRefreshedEvent — 刷新通知

**设计依据**: `PubSubEvent` 空载荷事件；由 `SignalRClient` 发布，`RegistrationListViewModel` 订阅（当前唯一消费者）。

### RegistrationCreateDialogViewModel

**设计依据**: DialogViewModelBase 子类，通过 `LYBT.Desktop.Contracts.Services` 调用患者搜索和医生列表。

| 依赖服务 | 来源 | 用途 |
|----------|------|------|
| `IPatientService` | `LYBT.Desktop.Contracts.Services` | 患者搜索自动补全 |
| `IUserService` | `LYBT.Desktop.Contracts.Services` | 医生下拉列表 |

### RegistrationService

**设计依据**: IRegistrationService 实现，CommandResult 模式包装 Repository。

| 方法 | 返回类型 | 说明 |
|------|----------|------|
| `CreateAsync` | `CommandResult<RegistrationDetailDto>` | 创建挂号，Source=Receptionist |
| `GetByIdAsync` | `CommandResult<RegistrationDetailDto>` | 获取详情 |
| `GetPagedAsync` | `CommandResult<PagedResult<RegistrationListDto>>` | 分页查询 |
| `GetQueueAsync` | `CommandResult<List<RegistrationListDto>>` | 获取等待队列 |
| `StartVisitAsync` | `CommandResult<Guid>` | 接诊，返回 MedicalCaseId |
| `CancelAsync` | `CommandResult` | 取消，仅 Waiting 状态可取消 |

## 依赖关系

### 依赖（编译时 ProjectReference）

| 项目 | 用途 |
|------|------|
| LYBT.Desktop.Infrastructure | `NavigableViewModelBase`、`DialogViewModelBase`、`Constants.ViewNames`、`Extensions` |
| LYBT.Desktop.Contracts | `Services.IRegistrationService` / `IPatientService` / `IUserService` / `INavigationCoordinator`、`Repositories.IRegistrationRepository`、`ApiClient.IApiClientRegistrations`、`Enums`/`Models`（`WorkspaceMode`/`EditState`/`MedicalCaseNavigationParameters`） |
| LYBT.Shared.Models | `RegistrationListDto` / `RegistrationDetailDto` / `RegistrationInputDto`、`CommandResult`、`Enums` |
| LYBT.Desktop.Foundation | `Repositories.EntityApiClientRepositoryBase`、`Application`、`Security`、`ExceptionHandling`（传递引用） |

NuGet 直接引用：`Prism.Core` / `Prism.DryIoc` / `Prism.Wpf`、`Microsoft.AspNetCore.SignalR.Client`。

> 导航参数契约（`WorkspaceMode`/`EditState`/`MedicalCaseNavigationParameters`）已于 A-18 批次2 下沉 `LYBT.Desktop.Contracts`，本模块**不再引用 `LYBT.Desktop.MedicalCase` 项目**。

### 被依赖

| 消费方 | 接口/视图 | 说明 |
|--------|-----------|------|
| `Roles/LYBT.Desktop.Clinical` | `ViewNames.RegistrationList` → `RegistrationListView` | 前台 `ReceptionistHomeViewModel` 与其导航「挂号队列 / 新建挂号」（支持 `Action`、`PatientId`、`PatientName` 预填充参数） |
| `Roles/LYBT.Desktop.Clinical` `PendingQueueViewModel` | `Contracts.Services.IRegistrationService.GetQueueAsync` | 医案工作台待诊队列（按当前医生 ID 过滤） |
| `ClinicalModule` | `[ModuleDependency("RegistrationModule")]` | 角色台运行时加载顺序 |

## 设计决策

1. **跨模块依赖收敛**: Registration 通过 `LYBT.Desktop.Contracts.Services` 使用 `IPatientService`/`IUserService`（已下沉 Contracts），通过 `LYBT.Desktop.Contracts.Enums`/`Models` 使用导航参数契约（A-18 批次2 下沉）；`[ModuleDependency]` 仅保留运行时加载顺序语义，**无任何业务模块 ProjectReference**
2. **PeriodicTimer 替代 DispatcherTimer**: 更现代的异步刷新模式，支持 CancellationToken
3. **CommandResult 模式**: 所有 Service 方法返回 `CommandResult<T>`，调用方检查 `.Success` 后访问 `.Data`
4. **实时推送 + 降级轮询**: SignalR（`hubs/registration`）优先；断线期间 `SignalRClient` 以 15 秒轮询兜底，两条路径都发布同一个 `RegistrationRefreshedEvent`，VM 无需区分来源

## 已知陷阱

- **StartVisit 导航参数**: 必须同时传 `MedicalCaseId`、`CurrentPatient`、`WorkspaceMode`、`EditState` 四个参数，缺一不可
- **Cancel 守卫条件**: 仅 `Status=Waiting` + `Source=Receptionist` + 当前用户为 Receptionist 三个条件同时满足才可取消
- **Contracts 服务依赖**: `RegistrationCreateDialogViewModel` 依赖 `IPatientService` 和 `IUserService`（来自 `LYBT.Desktop.Contracts.Services`），若这些服务未注册会 DI 失败
- **自动刷新生命周期**: 必须在 `OnNavigatedFromCore` 停止定时器并 `_signalRClient.StopAsync()`，否则离开页面后仍在轮询/保持连接
- **SignalR 订阅退订**: `RegistrationRefreshedEvent` 的 `SubscriptionToken` 必须保存并在 `Dispose` 中退订，否则已释放的 VM 仍会被回调（P2-14-7）
- **队列刷新间隔常量**: 30 秒间隔提取为 `QueueRefreshIntervalSeconds` 常量，勿在别处硬编码

---

2026-09-13 docs 复盘：与代码对齐（View/VM 清单、目录树、依赖）

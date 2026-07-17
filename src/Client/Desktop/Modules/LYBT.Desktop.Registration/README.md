# LYBT.Desktop.Registration

挂号管理模块 -- 管理患者挂号队列的完整生命周期（创建→等待→接诊/取消）。

## 项目定位

前台挂号是诊所日常运营的入口流程。本模块为 Receptionist 提供创建挂号能力，为 Doctor 提供个人队列视图和接诊入口，队列 30 秒自动刷新。接诊操作创建 MedicalCase 并导航至 MedicalCaseWorkspace（Clinical 编辑模式）。PRD: registration.md US-REG-001~006。

## 目录结构

```
LYBT.Desktop.Registration/
├── RegistrationModule.cs          # Prism IModule 入口
├── ViewModels/
│   └── RegistrationListViewModel.cs
├── Views/
│   └── RegistrationListView.xaml(.cs)
├── Dialogs/
│   ├── RegistrationCreateDialog.xaml(.cs)
│   └── RegistrationCreateDialogViewModel.cs
├── Repositories/
│   └── RegistrationRepository.cs   # 双模式仓库（Remote/Local）
└── Services/
    └── RemoteRegistrationService.cs
```

## 核心组件

### RegistrationModule

**设计依据**: Prism IModule 标准入口，声明模块依赖和 DI 注册。

| 注册项 | 类型 | 说明 |
|--------|------|------|
| `IRegistrationService` → `RemoteRegistrationService` | Service | 挂号服务实现 |
| `RegistrationListViewModel` | ViewModel | 队列列表 VM |
| `RegistrationListView` | Navigation | 导航视图 |
| `RegistrationCreateDialog` + VM | Dialog | 新建挂号弹窗 |

**模块依赖**: `AuthenticationModule`, `PatientsModule`, `UsersModule`

### RegistrationListViewModel

**设计依据**: NavigableViewModelBase 子类，角色感知队列 + 自动刷新 + 接诊导航。

| 属性/命令 | 说明 |
|-----------|------|
| `WaitingQueue` | 等待队列集合 |
| `SelectedRegistration` | 当前选中挂号 |
| `RefreshCommand` | 手动刷新队列 |
| `CreateRegistrationCommand` | 打开新建挂号弹窗 |
| `StartVisitCommand` | 接诊：创建 MedicalCase → 导航 Workspace |
| `CancelRegistrationCommand` | 取消挂号（仅 Waiting + Receptionist） |

- **角色感知**: Doctor 只看自己队列（传 `doctorId`），Receptionist/Admin 看全部
- **自动刷新**: `PeriodicTimer` 30 秒间隔，`OnNavigatedTo` 启动、`OnNavigatedFrom` 停止
- **接诊流程**: `StartVisitAsync()` → 获取 `MedicalCaseId` → `IPatientApi.GetPatientByIdAsync()` → 导航 `MedicalCaseWorkspace`（`WorkspaceMode.Clinical` + `EditState.Editing`）

### RegistrationCreateDialogViewModel

**设计依据**: DialogViewModelBase 子类，跨模块调用患者搜索和医生列表。

| 依赖服务 | 来源模块 | 用途 |
|----------|----------|------|
| `IPatientService` | PatientsModule | 患者搜索自动补全 |
| `IUserService` | UsersModule | 医生下拉列表 |

### RemoteRegistrationService

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

```
RegistrationModule
├── LYBT.Desktop.Contracts    # IRegistrationService, IRegistrationRepository, INavigationCoordinator
├── LYBT.Desktop.Infrastructure  # NavigableViewModelBase, DialogViewModelBase, ViewNames
├── LYBT.Desktop.Patients     # IPatientService, IPatientApi (跨模块)
├── LYBT.Desktop.Users        # IUserService (跨模块)
└── LYBT.Shared.Models        # DTOs, CommandResult
```

## 设计决策

1. **跨模块依赖许可**: Registration 是工作流模块，可引用 Patients/Users 的服务接口（AGENTS.md 明确例外）
2. **PeriodicTimer 替代 DispatcherTimer**: 更现代的异步刷新模式，支持 CancellationToken
3. **CommandResult 模式**: 所有 Service 方法返回 `CommandResult<T>`，调用方检查 `.Success` 后访问 `.Data`
4. **双模式仓库**: `RegistrationRepository` 通过 `IApiRouter` 路由到 Remote/Local

## 已知陷阱

- **StartVisit 导航参数**: 必须同时传 `MedicalCaseId`、`CurrentPatient`、`WorkspaceMode`、`EditState` 四个参数，缺一不可
- **Cancel 守卫条件**: 仅 `Status=Waiting` + `Source=Receptionist` + 当前用户为 Receptionist 三个条件同时满足才可取消
- **跨模块 DTO**: `RegistrationCreateDialogViewModel` 依赖 `IPatientService` 和 `IUserService`，若这些模块未加载会 DI 失败
- **自动刷新生命周期**: 必须在 `OnNavigatedFrom` 停止定时器，否则离开页面后仍在轮询

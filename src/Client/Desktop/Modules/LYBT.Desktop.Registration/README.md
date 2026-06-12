# LYBT.Desktop.Registration

> 挂号排队模块 — 患者挂号、候诊队列、开始就诊

## 定位

| 属性 | 说明 |
|------|------|
| 层级 | Desktop 业务模块 |
| 职责 | 挂号创建、候诊队列管理、开始就诊 |
| 模块依赖 | Authentication, Patients, Users |
| 导航目标 | `RegistrationListView` |

---

## 目录结构

```
LYBT.Desktop.Registration/
├── RegistrationModule.cs              # Prism IModule 注册
├── Dialogs/
│   ├── RegistrationCreateDialog.xaml   # 新建挂号对话框
│   └── RegistrationCreateDialogViewModel.cs
├── Repositories/
│   └── RegistrationRepository.cs       # IApiClient (Refit)
├── Services/
│   └── RemoteRegistrationService.cs    # CommandResult 包装
├── ViewModels/
│   └── RegistrationListViewModel.cs    # 候诊队列主VM
└── Views/
    └── RegistrationListView.xaml       # 候诊队列视图
```

---

## 核心接口

| 接口 | 职责 |
|------|------|
| `IRegistrationService` | 业务操作（创建、队列、开始就诊、取消） |
| `IRegistrationRepository` | API通信（Refit IApiClient） |

---

## 关键功能

### 候诊队列 (RegistrationListViewModel)

| 功能 | 命令 | 说明 |
|------|------|------|
| 刷新队列 | `RefreshCommand` | 手动刷新 |
| 新建挂号 | `CreateRegistrationCommand` | 打开创建对话框 |
| 开始就诊 | `StartVisitCommand` | 医生专用，创建医案并跳转 |
| 取消挂号 | `CancelRegistrationCommand` | 前台专用，取消候诊 |
| 自动刷新 | 30秒定时器 | PeriodicTimer，NavigateFrom时停止 |

### 新建挂号 (RegistrationCreateDialogViewModel)

- 患者搜索：关键字搜索 → 下拉选择
- 医生选择：加载所有启用医生，下拉选择
- 来源标记：`RegistrationSource.Receptionist`

### 开始就诊流程

```
选择候诊记录 → StartVisitAsync → 获取 MedicalCaseId
    → 加载 PatientDetailDto → 导航到 MedicalCaseWorkspace (Clinical/Editing)
```

---

## 角色权限

| 角色 | 队列可见范围 | 可用命令 |
|------|-------------|---------|
| Receptionist | 全部 | 新建、刷新、取消 |
| Doctor | 仅自己 | 刷新、开始就诊 |
| Admin/SuperAdmin | 全部（只读） | 刷新 |

---

## 模块依赖

| 依赖 | 用途 |
|------|------|
| `LYBT.Desktop.Patients` | `IPatientService`, `IPatientApi` — 患者搜索 |
| `LYBT.Desktop.Users` | `IUserService` — 加载医生列表 |
| `LYBT.Desktop.Infrastructure` | `INavigationCoordinator`, `ViewNames` |
| `LYBT.Desktop.MedicalCase` | `WorkspaceMode`, `EditState` — 就诊导航参数 |

---

## 状态机

```
Waiting → (StartVisit) → InProgress → (医案关闭) → Completed
Waiting → (Cancel, 仅Receptionist源) → Cancelled
```

---

## 测试

- `HttpRegistrationRepositoryTests` — Repository 单元测试
- `RegistrationsControllerTests` — LocalWebAPI 集成测试
- `RegistrationStatusTransitionTests` — 状态转换规则

# LYBT.Desktop.Sysadmin

系统运维控制台模块 -- 为 sysadmin 用户提供独立的仪表盘、日志控制和部署管理功能。

## 项目定位

Sysadmin 模块是系统运维人员的专属工作空间，提供运行状态仪表盘（30 秒轮询）、日志级别动态调整（通过 DiagnosticsController API）和部署管理（上传 ZIP 更新包 + 重启服务）。独立于 Admin 角色，面向系统运维而非业务管理。

## 目录结构

```
LYBT.Desktop.Sysadmin/
├── SysadminModule.cs                 # Prism IModule 入口
├── Models/
│   └── DashboardStatus.cs            # 仪表盘状态模型
├── ViewModels/
│   ├── SysadminHomeViewModel.cs      # 运维控制台主页（轮询仪表盘）
│   ├── LogLevelControlViewModel.cs   # 日志级别控制
│   └── DeploymentViewModel.cs        # 部署管理
└── Views/
    ├── SysadminHomeView.xaml(.cs)
    ├── LogLevelControlView.xaml(.cs)
    └── DeploymentView.xaml(.cs)
```

## 核心组件

### SysadminModule

**设计依据**: Prism IModule 标准入口，无模块依赖，WhenAvailable 立即加载。

| 注册项 | 类型 | 说明 |
|--------|------|------|
| `SysadminHomeViewModel` | ViewModel | 运维主页 VM |
| `LogLevelControlViewModel` | ViewModel | 日志级别控制 VM |
| `DeploymentViewModel` | ViewModel | 部署管理 VM |
| `SysadminHomeView` | Navigation | 运维主页 |
| `LogLevelControlView` | Navigation | 日志级别控制页 |
| `DeploymentView` | Navigation | 部署管理页 |

**模块依赖**: 无

### SysadminHomeViewModel

**设计依据**: NavigableViewModelBase 子类，30 秒轮询仪表盘展示系统健康状态。

| 属性 | 说明 |
|------|------|
| `Dashboard` | `DashboardStatus` 聚合状态模型 |

| 轮询项 | 数据来源 | 说明 |
|--------|----------|------|
| 数据库状态 | `IAuthApi.HealthCheckAsync()` | 健康检查 API |
| 系统信息 | `SystemConstants.ApplicationVersion` + `IClinicSettingsService.ClinicName` | 本地常量 |

- **轮询机制**: `OnNavigatedTo` 启动、`OnNavigatedFrom` 停止，`CancellationTokenSource` 控制生命周期
- **首次加载**: 首次轮询设置 `Dashboard.IsLoading = true`，完成后设为 `false`

### DeploymentViewModel

**设计依据**: NavigableViewModelBase 子类，ZIP 更新包上传和服务重启。

| 属性 | 说明 |
|------|------|
| `SelectedFileName` | 选中的文件路径 |
| `IsUploading` | 上传中状态 |
| `IsRestarting` | 重启中状态 |
| `UploadProgress` | 上传进度（百分比） |
| `StatusMessage` | 状态消息 |

| 命令 | 说明 |
|------|------|
| `SelectFileCommand` | 打开文件选择对话框（ZIP 过滤） |
| `UploadCommand` | 上传更新包（`MultipartFormDataContent`） |
| `RestartCommand` | 重启服务 |
| `GoBackCommand` | 返回上一页 |

- **API 端点**: `POST /api/v1/deploy/upload`, `POST /api/v1/deploy/restart`
- **CanExecute 守卫**: 上传和重启互斥，上传中不可重启，重启中不可上传

### LogLevelControlViewModel

**设计依据**: NavigableViewModelBase 子类，通过 DiagnosticsController API 调整运行时日志级别。

| 属性 | 说明 |
|------|------|
| `CurrentLevel` | 当前日志级别 |
| `StatusMessage` | 操作状态消息 |

| 命令 | 说明 |
|------|------|
| `SetLevelCommand` | 设置指定日志级别 |
| `EnableDebugCommand` | 开启 Debug 模式（60 分钟自动关闭） |
| `DisableDebugCommand` | 关闭 Debug 模式 |

- **日志级别检测**: 从 `GetLoggingStatusAsync()` 返回的 JSON 中关键字匹配（Debug/Verbose/Information/Warning/Error）
- **Debug 限时**: `EnableDebugModeRequest` 指定 `DurationMinutes = 60`

### DashboardStatus / StatusCard

**设计依据**: CommunityToolkit.Mvvm ObservableObject，仪表盘 2x2 网格卡片模型。

| 类 | 属性 | 说明 |
|----|------|------|
| `StatusCard` | `Title`, `Value`, `Status`, `IsHealthy` | 单个状态卡片 |
| `DashboardStatus` | `DbStatus`, `SystemInfo`, `IsLoading` | 仪表盘聚合状态 |

## 依赖关系

```
SysadminModule
├── LYBT.Desktop.Contracts    # IAuthApi, IDeployApi, IDiagnosticsApi, IViewModelServices
├── LYBT.Desktop.Infrastructure  # NavigableViewModelBase, SystemConstants
├── LYBT.Desktop.Infrastructure.Interfaces  # IClinicSettingsService
└── LYBT.Desktop.Sysadmin.Models  # DashboardStatus, StatusCard
```

## 设计决策

1. **30 秒轮询仪表盘**: 使用 `Task.Delay` + `CancellationToken` 实现简单轮询，`OnNavigatedTo`/`OnNavigatedFrom` 控制生命周期
2. **MultipartFormDataContent 上传**: `DeploymentViewModel` 直接构造 `MultipartFormDataContent`，通过 `IDeployApi.UploadAsync()` 上传
3. **日志级别字符串匹配**: `LogLevelControlViewModel` 从 JSON 响应中用 `Contains` 匹配日志级别，而非反序列化为强类型
4. **StatusCard/ObservableObject**: 使用 CommunityToolkit.Mvvm 的 `[ObservableProperty]` 实现属性变更通知

## 已知陷阱

- **轮询内存泄漏**: 必须在 `OnNavigatedFrom` 中调用 `StopPolling()` 取消 `CancellationTokenSource`，否则轮询永不停止
- **HealthCheckAsync 无认证**: 健康检查端点 `GET /api/v1/health` 无需 Bearer Token，但 `IAuthApi` 接口名可能造成误导
- **日志级别检测脆弱**: 用 `Contains("Debug")` 匹配 JSON 字符串，若返回格式变化会误判
- **UploadAsync 无进度回调**: 当前实现没有真正的上传进度追踪，`UploadProgress` 仅在成功时设为 100
- **RestartAsync 无确认**: 重启命令没有二次确认弹窗，误操作会直接重启服务
- **DashboardStatus 无测试覆盖**: `StatusCard` 和 `DashboardStatus` 模型没有单元测试

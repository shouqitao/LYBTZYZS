# Platform 模块设计

> 日期: 2026-06-29
> US 数量: 43 (PRD)
> 复杂度: 高
> 状态: 草稿

## 模块概述

Platform 是系统基础平台，包含 Shell、配置、错误处理、日志、健康检查、读卡器等基础设施。

**职责边界**:
- 应用启动与生命周期管理
- 导航与模块加载
- 配置管理（连接设置、诊所设置）
- 错误处理与全局异常捕获
- 日志系统（Serilog）
- API 健康检查与熔断
- 身份证读卡器集成
- 主题切换（亮/暗）

**依赖关系**:
- 上游: 无（最底层模块）
- 下游: 所有业务模块（提供基础设施服务）

**关键 US 清单**:
PLATFORM-001 ~ PLATFORM-043（详见 PRD）

## 子系统概览

### 1. Shell（应用外壳）

**入口**: `App.xaml.cs`（PrismApplication）

**启动流程**:
```
App.OnStartup
  → 单实例互斥锁（Global\LYBTZYZS_Shell_Instance）
  → DesktopSerilogConfiguration.Initialize()
  → base.OnStartup
  → RegisterTypes（DI 注册）
  → ConfigureModuleCatalog（14 个模块）
  → OnInitialized
    → Show MainWindow（登录界面）
    → AppStartupOrchestrator.RunStartupAsync()（后台）
```

**启动管道**（6 步）:
| 步骤 | 顺序 | 必需 | 并行组 |
|------|------|------|--------|
| ErrorHandling | 10 | 是 | — |
| ModuleCoordinator | 20 | 否 | CoreInit |
| CoreServices | 30 | 是 | CoreInit |
| ApiHealthCheck | 40 | 否 | — |
| Warmup | 50 | 否 | — |
| LocalWebApi | 250 | 否 | — |

**模块加载策略**:
- WhenAvailable: Auth, Clinical, Admin, Receptionist, Sysadmin
- OnDemand: Users, Patients, Herbs, Formula, MedicalCase, Registration, CardReader, Reports

**导航**: `NavigationCoordinator` — Region-based 导航，支持历史记录（最多 20 条）、前进/后退、面包屑。

### 2. 配置管理

| 服务 | 职责 |
|------|------|
| ISettingsService | 通用键值配置（内存字典） |
| IConnectionSettingsService | 远程/本地 URL 管理 |
| IConnectionModeService | 连接模式检测（自动/远程/本地） |
| IApplicationStateService | API 健康状态、连接状态 |
| IApiRouter | 当前 URL 和模式 |

**连接模式检测**:
```
DetectBestModeAsync()
  → GET /api/v1/health（远程，3秒超时）
  → 任何响应 → 远程模式
  → 全部超时 → 本地模式
```

### 3. 错误处理

- `IDesktopExceptionHandler`: 全局异常捕获注册
- `ApiErrorHandler`: Refit/HTTP 异常映射为用户友好消息
- `ApplicationInitializationService.InitializeErrorHandling()`: 兜底 DispatcherUnhandledException

### 4. 日志（Serilog）

- 路径: `%LOCALAPPDATA%/LYBTZYZS/logs/lybt-desktop-.log`
- 滚动: 每日，保留 30 天，10MB 文件限制
- 最低级别: Information（Microsoft/System/Prism 为 Warning）
- 增强: CorrelationId（W3C Activity）、敏感数据脱敏

### 5. 健康检查

- `IApiHealthCheckService`: 单次 HTTP 健康检查
- `IApiHealthMonitor`: 定时监控（10秒间隔）+ 熔断器（3次失败/30秒恢复）
- `IHealthCheckCoordinator`: Tick-based 调度器

**熔断器状态**:
```
Closed（正常）→ 3次失败 → Open（熔断）→ 30秒 → HalfOpen → 成功 → Closed
```

### 6. 读卡器

**策略模式**:
```
ICardReader（接口）
├── HuaDaHD100CardReader（P/Invoke HDstdapi.dll）
└── MockCardReader（测试用）

ICardReaderFactory → 根据类型创建读卡器
ICardReaderService → 单例服务，支持自动读取
```

**去重链**（PRD-15）:
1. IdNumber 精确匹配
2. Name + BirthDate 模糊匹配
3. 多候选提示
4. 无匹配 → 快速创建

## 关键服务清单

| 服务 | 层级 | 职责 |
|------|------|------|
| ILoginCoordinator | Shell | 登录流程编排 |
| INavigationCoordinator | Shell | 导航管理 |
| ISessionManager | Infrastructure | 当前会话（用户/角色） |
| ITokenManager | Foundation | Token 内存存储 |
| ITokenRefreshHandler | Foundation | Token 自动刷新 |
| ITokenLifecycleService | Foundation | Token 状态监控 |
| IThemeService | Shell | 主题切换 |
| ISnackbarService | Shell | 消息提示 |
| IDialogHostService | Shell | 对话框管理 |
| MenuManager | Shell | 菜单/快捷键 |
| IApiHealthMonitor | Shell | API 健康监控 |
| IStartupPipeline | Shell | 启动管道 |

## 异常处理

| 异常类型 | 场景 | 处理 |
|----------|------|------|
| DispatcherUnhandledException | 未捕获 UI 异常 | 全局兜底，e.Handled=true |
| ApiException | API 调用失败 | 映射为用户友好消息 |
| SocketException | 网络不可用 | 提示检查网络连接 |
| TaskCanceledException | 请求超时 | 提示请求超时 |

## 业务规则

| 规则 | 描述 | 与 US 映射 |
|------|------|-----------|
| 单实例 | 互斥锁防止多开 | PLATFORM-001 |
| 两阶段启动 | 立即显示登录 + 后台初始化 | PLATFORM-002 |
| 按需加载 | 业务模块按角色按需加载 | PLATFORM-003 |
| 熔断保护 | 3次失败后停止检查30秒 | PLATFORM-010 |

## 模块交互

| 交互模块 | 方式 | 方向 |
|----------|------|------|
| 所有模块 | 提供基础设施服务 | Platform → 全局 |
| Auth | 登录流程 | Auth → Platform |
| Patients | 读卡器集成 | Patients → Platform |

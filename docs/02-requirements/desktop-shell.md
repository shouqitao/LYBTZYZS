# Desktop Shell 基础设施 需求规格

## 概述

Desktop Shell 是 WPF 客户端的宿主框架，基于 Prism 9.0 模块化架构。负责应用启动流水线、用户认证协调、会话生命周期管理、页面导航、菜单系统、通用对话框和启动诊断。Shell 层位于架构最顶层，协调 Roles 层和 Modules 层的加载与交互。

---

## 用户角色

| 角色 | 在本模块中的交互 |
|------|-----------------|
| 所有角色 | 使用 Shell 基础设施 (启动、登录、导航、菜单、对话框) |
| Admin | 进入管理员工作台 (Admin Role)，可见管理菜单 |
| Doctor | 进入临床工作台 (Clinical Role)，可见临床菜单 |
| Receptionist | 使用基础设施，可见患者管理 + 读卡器 + 未完成医案简要提示 |

> Shell 基础设施对所有角色透明提供服务，角色差异体现在菜单可见性和导航目标。

---

## 功能清单

### FR-SHELL-001: 应用启动流水线

- **描述**: StartupPipeline 按顺序执行注册的启动步骤，支持进度报告和诊断
- **业务规则**:
  1. 启动流水线由 IStartupPipeline 管理，步骤通过 RegisterStep 注册
  2. 步骤按注册顺序依次执行 (ExecuteAsync)
  3. 支持 IProgress<string> 进度报告和 CancellationToken 取消
  4. 每步完成触发 StepCompleted 事件，流水线状态变更触发 StateChanged 事件
  5. 启动失败时记录诊断信息 (GetDiagnostics)，提供用户友好错误提示
  6. 支持 Reset 重置流水线状态
- **远程模式**: 启动步骤包含 API 连通性检查
- **本地模式**: 跳过 API 相关步骤，初始化本地数据库
- **验收标准**:
  - [ ] 应用启动 -> 依次执行所有注册的启动步骤
  - [ ] 启动步骤失败 -> 记录诊断信息，显示错误对话框
  - [ ] 进度条显示当前执行步骤名称
  - [ ] 支持取消启动过程

> **[已修订 2026-02-21]** 登录协调依赖计数不匹配，PRD 对齐代码行为 (PRD 标注 11 个依赖含 3 个可选，以代码实际注入为准)
> 原因: 代码重构后依赖数量可能变化，PRD 硬编码数字易过时  |  参考: SHELL-12

### FR-SHELL-002: 用户登录协调

- **描述**: LoginCoordinator 协调完整的登录流程，包括远程/本地模式分发、会话启动和模块加载
- **业务规则**:
  1. LoginCoordinator 聚合 11 个依赖 (含 3 个可选: CredentialVault, UsernameStorage, LocalAuthService)
  2. 登录流程: 凭据验证 -> 会话启动 -> 模块加载 -> 角色首页导航
  3. 远程模式: 调用 IAuthenticationService API 验证
  4. 本地模式: 调用 ILocalAuthService 本地验证
  5. 登录成功后自动加载用户角色对应的 Prism 模块
  6. 登出流程: 结束会话 -> 清除导航历史 -> 返回登录页
  7. 支持 IAuthenticationStateMachine 状态机驱动
  8. 事件: LoginSucceeded, LogoutCompleted, StateChanged
- **远程模式**: JWT Token 认证，凭据可选 DPAPI 加密存储
- **本地模式**: 简化认证，无 Token 机制
- **验收标准**:
  - [ ] 正确凭据 -> 登录成功 -> 导航到角色首页
  - [ ] 错误凭据 -> 显示错误消息，保持登录页
  - [ ] 登出 -> 返回登录页，清除会话和导航历史
  - [ ] Admin 登录 -> 加载管理员模块，导航到管理工作台
  - [ ] Doctor 登录 -> 加载临床模块，导航到临床工作台

> **[已修订 2026-02-21]** 超时前警告已被 simplify-auth 移除，PRD 修订移除警告要求 (原规则5 WarningBeforeTimeoutMinutes)
> 原因: simplify-auth 重构移除了超时前警告机制，仅保留静默登出  |  参考: SHELL-04

### FR-SHELL-003: 会话生命周期管理

- **描述**: SessionLifecycleManager 管理用户从登录到登出的完整会话生命周期
- **业务规则**:
  1. 会话状态: 未认证 -> 已认证 -> 会话活跃 -> 会话过期/登出
  2. Token 生命周期监控: TokenRemainingTime 属性
  3. 用户活跃度追踪: IUserActivityTracker 监测键盘/鼠标活动
  4. 无活动超时: InactivityTimeoutMinutes (默认 15 分钟，可配置)
  5. 超时前警告: WarningBeforeTimeoutMinutes (默认 2 分钟)
  6. 自动刷新 Token: 用户活跃时自动调用 RefreshTokenAsync
  7. 会话过期事件: SessionExpired 触发自动登出
  8. 实现 IDisposable: 释放 Timer 和事件订阅
- **远程模式**: JWT Token 滑动刷新 + 不活跃超时登出
- **本地模式**: 简化会话状态，无 Token 刷新。不活跃超时同远程模式 (防信息泄露)
- **验收标准**:
  - [ ] 登录成功 -> CurrentState 变为已认证
  - [ ] 用户无活动超过 InactivityTimeoutMinutes -> 触发 SessionExpired
  - [ ] 用户活跃时 Token 即将过期 -> 自动刷新
  - [ ] 登出 -> 清除所有会话状态

### FR-SHELL-004: 页面导航系统

- **描述**: NavigationCoordinator 封装 Prism Region 导航机制，提供统一导航入口
- **业务规则**:
  1. 基础导航: NavigateTo(viewName, parameters) 导航到指定视图
  2. 角色首页: NavigateToHome() / NavigateToHome(role) 导航到角色工作台
  3. 返回导航: NavigateBack() 从历史栈回退
  4. 历史管理: NavigationHistory 只读列表, ClearHistory() 清空
  5. Region 管理: ShowLoginDialog / ClearLoginRegion / ClearContentRegion
  6. 导航参数: 通过 IDictionary<string, object> 传递
  7. 导航变更事件: NavigationChanged 通知视图切换
- **远程模式**: 与本地模式导航行为一致
- **本地模式**: 与远程模式导航行为一致
- **验收标准**:
  - [ ] NavigateTo("PatientListView") -> ContentRegion 显示患者列表
  - [ ] NavigateBack() -> 返回上一个视图
  - [ ] ClearHistory() -> 清空导航历史，CanNavigateBack=false
  - [ ] 导航参数传递到目标 ViewModel

### FR-SHELL-005: 菜单与快捷键系统

- **描述**: MenuManager 管理菜单命令和全局快捷键
- **业务规则**:
  1. 快速操作: QuickAddPatient (Ctrl+N), QuickStartMedicalCase (Ctrl+Shift+C)
  2. 全局命令: SaveAll (Ctrl+S), RefreshAll (F5), Print (Ctrl+P), Export
  3. 编辑命令: Undo (Ctrl+Z), Redo (Ctrl+Y)
  4. 系统命令: Help (F1), Settings (Ctrl+,), ToggleTheme
  5. 导航命令: NavigateToHome, NavigateToSystemSettings, EditProfile
  6. 主题管理: 支持浅色/深色主题切换 (ApplyLightTheme/ApplyDarkTheme)
  7. 菜单内容根据用户角色动态显示/隐藏 (通过 IApplicationCommands)
- **远程模式**: 全部菜单可用
- **本地模式**: 部分菜单不可用 (如需要服务端的操作)

> **[Sprint 4 已实现]** 角色菜单可见性: MenuManager 根据 CurrentUser.Role 和 ConnectionMode 控制菜单项可见性，不同角色看到不同的导航菜单和操作按钮 (T4-S6-01~02)
- **验收标准**:
  - [ ] Ctrl+N -> 快速添加患者
  - [ ] Ctrl+S -> 触发当前页面保存
  - [ ] F5 -> 触发当前页面刷新
  - [ ] 主题切换 -> 所有控件样式更新

### FR-SHELL-006: 启动诊断与性能监控

- **描述**: StartupDiagnostics 记录每个启动步骤的耗时和结果，生成诊断报告
- **业务规则**:
  1. BeginStartup/EndStartup: 标记启动过程的开始和结束
  2. BeginStep/EndStep: 记录单个步骤的名称、耗时、成功/失败
  3. RecordMarker: 记录关键时间点标记
  4. 慢步骤检测: 超过 3 秒的步骤标记为慢步骤 (SlowStepThresholdSeconds=3.0)
  5. GetReport: 生成 StartupReport (步骤列表 + 总耗时 + 慢步骤列表)
  6. 诊断结果自动记录到日志
- **远程模式**: 包含 API 连通性检查步骤
- **本地模式**: 包含本地数据库初始化步骤
- **验收标准**:
  - [ ] 启动完成后 GetReport 返回所有步骤的耗时
  - [ ] 步骤耗时>3秒 -> 标记为慢步骤
  - [ ] 步骤失败 -> 记录 errorMessage

> **[延期 2026-02-21]** 缺少最后登录时间/IP 信息显示
> 原因: 非 MVP 必要，账户设置核心功能 (修改密码/个人资料) 已实现  |  计划: Sprint 后续  |  参考: SHELL-08

### FR-SHELL-007: 账户设置

- **描述**: AccountSettingsControl/ViewModel 提供个人信息查看和修改入口
- **业务规则**:
  1. 查看当前登录用户信息
  2. 修改密码入口
  3. 个人资料编辑
  4. 通过 MenuManager.EditProfileCommand 进入
- **远程模式**: 修改通过 API 提交
- **本地模式**: 修改本地存储
- **验收标准**:
  - [ ] 点击账户设置 -> 显示 AccountSettingsControl
  - [ ] 修改密码 -> 弹出密码修改对话框
  - [ ] 保存个人资料 -> 调用 API 更新

---

## 完整菜单结构

### 主菜单层级

```
顶部菜单栏
├── 文件 (File)
│   ├── 新建患者          Ctrl+N       所有角色
│   ├── 新建医案          Ctrl+Shift+C Doctor
│   ├── ─────────────
│   ├── 打印              Ctrl+P       Doctor+
│   ├── ─────────────
│   └── 退出              Alt+F4       所有角色
│
├── 编辑 (Edit)
│   ├── 撤销              Ctrl+Z       所有角色
│   ├── 重做              Ctrl+Y       所有角色
│   ├── ─────────────
│   └── 保存              Ctrl+S       所有角色
│
├── 视图 (View)
│   ├── 刷新              F5           所有角色
│   ├── ─────────────
│   ├── 浅色主题                       所有角色
│   └── 深色主题                       所有角色
│
├── 导航 (Navigate)                    -- 侧边栏菜单
│   ├── 首页                           所有角色
│   ├── 患者管理                       所有角色
│   ├── 医案管理                       Doctor+
│   ├── 验方管理                       Doctor+
│   ├── ─────────────                  (Management 模式可见)
│   ├── 药材管理                       Admin+
│   ├── 用户管理                       Admin+
│   ├── 数据同步                       Admin+ (Doctor也可)
│   └── 系统设置                       SuperAdmin
│
├── 工具 (Tools)
│   ├── 数据同步                       Doctor+
│   └── 系统健康检查                   Admin+
│
└── 帮助 (Help)
    ├── 帮助文档          F1           所有角色
    └── 关于                           所有角色
```

### 菜单可见性矩阵

| 菜单项 | SuperAdmin | Admin | Doctor | Receptionist |
|--------|:---:|:---:|:---:|:---:|
| 新建患者 | O | O | O | O |
| 新建医案 | X | X | O | X |
| 打印 | O | O | O | X |
| 患者管理 | O | O | O | O |
| 医案管理 | O | O | O | X |
| 验方管理 | O | O | O | X |
| 药材管理 | O | O | X | X |
| 用户管理 | O | O | X | X |
| 数据同步 | O | O | O | X |
| 系统设置 | O | X | X | X |
| 系统健康 | O | O | X | X |

---

## Prism Region 定义

| Region 名称 | 位置 | 承载视图 |
|-------------|------|---------|
| LoginRegion | 全屏覆盖 | LoginView |
| ContentRegion | 主内容区 | PatientListView, PatientDetailView, MedicalCaseListView, MedicalCaseEditView, HerbListView, HerbDetailView, FormulaListView, FormulaDetailView, UserListView, UserDetailView, SyncView, SettingsView |
| MenuRegion | 左侧侧边栏 | NavigationMenuView |
| StatusBarRegion | 底部状态栏 | StatusBarView |
| DialogRegion | 模态遮罩 | 各种对话框 (确认/导入/冲突解决等) |

### 导航参数规范

| 导航场景 | 参数 | 类型 | 说明 |
|----------|------|------|------|
| 患者详情 | PatientId | Guid | 患者 ID |
| 患者医案 | PatientId | Guid | 从患者详情导航到医案列表，自动过滤 |
| 医案编辑 | MedicalCaseId | Guid | 编辑现有医案 |
| 新建医案 | PatientId | Guid | 为指定患者创建新医案 |
| 药材详情 | HerbId | Guid | 药材 ID |
| 验方详情 | FormulaId | Guid | 验方 ID |
| 用户详情 | UserId | Guid | 用户 ID |

### 导航历史

| 属性 | 规范 |
|------|------|
| 后退 | 支持，Alt+左箭头 快捷键 |
| 前进 | 不支持 |
| 历史深度 | 最多 20 条 |
| 清空时机 | 登出时清空 |

> **[Sprint 4 已实现]** 导航历史上限: NavigationCoordinator 限制导航历史记录最多 20 条，超出时自动移除最早的记录 (T4-S6-03)

---

## 状态栏信息

```
┌─────────────────────────────────────────────────────────────────┐
│ 当前用户: 张医生 (Doctor)  │  远程模式  │  v1.0.0              │
└─────────────────────────────────────────────────────────────────┘
```

| 位置 | 内容 | 更新时机 |
|------|------|---------|
| 左侧 | 当前用户名 + 角色 | 登录/登出时 |
| 中间 | 运行模式 (远程模式 / 本地模式) | 模式切换时 |
| 右侧 | 版本号 | 固定 |
| 可选 | 网络状态指示 (v2.0) | 网络变化时 |

---

## 启动画面 (Splash Screen)

### 布局

```
┌─────────────────────────────────┐
│                                 │
│           [应用 Logo]           │
│                                 │
│     凌隐宝堂中医诊所管理系统      │
│                                 │
│  ████████████░░░░░░░  60%      │
│  正在初始化数据库连接...          │
│                                 │
└─────────────────────────────────┘
```

### 行为规则

| 属性 | 规范 |
|------|------|
| 显示时机 | 应用启动到登录页出现之前 |
| 进度来源 | StartupPipeline 的步骤进度 |
| 步骤文字 | 显示当前执行步骤名称 (如 "正在初始化数据库连接...") |
| 完成行为 | 所有步骤完成后自动关闭 Splash Screen，显示登录页 |
| 失败行为 | 显示错误信息 + "重试"/"退出"按钮 |
| 最短显示 | 至少 1 秒 (避免闪烁) |

### 启动失败降级策略

| 失败场景 | 处理 |
|----------|------|
| API 不可达 | 提示"服务器连接失败"，提供"切换到本地模式"按钮 |
| SQLite 初始化失败 | 提示错误详情，提供"重试"/"退出"按钮 |
| 配置文件缺失 | 提示"配置文件错误"，提供错误详情 |

---

## 账户设置详细

### FR-SHELL-007 扩展

可配置项列表:

| 设置项 | 说明 | 关联 FR |
|--------|------|---------|
| 修改密码 | 弹出对话框: 旧密码 + 新密码 + 确认密码 | FR-USER-009 |
| 修改个人资料 | 显示名称 / 电话 / 邮箱 | FR-USER-010 |
| 查看登录信息 | 最后登录时间 / 登录 IP | 只读 |

界面布局: 模态对话框或侧边滑出面板

---

## 数据模型

### SessionState 状态机

```
Unauthenticated -> Authenticating -> Authenticated -> Active -> Expired -> Unauthenticated
                                                    -> LoggingOut -> Unauthenticated
```

### StartupPipelineState

| 值 | 说明 |
|----|------|
| NotStarted | 未开始 |
| Running | 执行中 |
| Completed | 完成 |
| Failed | 失败 |

> **[已修订 2026-02-21]** 状态枚举命名差异，PRD 对齐代码命名 (代码实际枚举值以实现为准)
> 原因: 确保 PRD 枚举命名与代码一致  |  参考: SHELL-11

### AuthState

| 值 | 说明 |
|----|------|
| NotAuthenticated | 未认证 |
| Authenticating | 认证中 |
| Authenticated | 已认证 |
| LoggingOut | 登出中 |

> **[已修订 2026-02-21]** StartupReport 返回类型差异，PRD 对齐代码类型 (代码实际返回类型和字段以实现为准)
> 原因: PRD 类型定义与代码实际实现不一致  |  参考: SHELL-13

> **[已修订 2026-02-21]** 启动诊断信息格式差异，PRD 对齐代码格式 (诊断信息的具体格式以代码实现为准)
> 原因: PRD 诊断信息格式规范与代码输出不一致  |  参考: SHELL-14

### StartupReport

| 字段 | 类型 | 说明 |
|------|------|------|
| Steps | List<StepRecord> | 步骤记录列表 |
| TotalDuration | TimeSpan | 总耗时 |
| SlowSteps | List<StepRecord> | 慢步骤 (>3秒) |
| StartTime | DateTime | 启动时间 |
| EndTime | DateTime | 结束时间 |

---

## 决策记录

| # | 决策 | 结论 | 日期 |
|---|------|------|------|
| 1 | Shell 基础设施是否需要 PRD | 是。Shell 包含7个独立功能需求，值得正式文档化 | 2026-02-11 |
| 2 | 本地模式 Shell 差异 | 大部分 Shell 功能不区分模式，仅认证和 Token 管理有差异 | 2026-02-11 |
| 3 | 慢步骤阈值 | 3.0 秒，平衡诊断精度和噪声 | 2026-02-11 |
| 4 | 状态栏信息 | 左: 用户名+角色, 中: 运行模式, 右: 版本号。简洁实用 | 2026-02-17 |
| 5 | 启动画面 | 进度报告型: Logo+进度条+当前步骤名称。失败时提供降级/重试选项 | 2026-02-17 |
| 6 | 导航历史 | 仅支持后退 (Alt+左箭头)，不支持前进。历史深度最多20条 | 2026-02-17 |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-11 | v1.0 | 初始版本，从代码实现逆向工程 |
| 2026-02-17 | v2.0 | Round 5 深化: 新增完整菜单层级+角色可见性矩阵、Prism Region定义+导航参数、状态栏信息、启动画面规格+失败降级、账户设置详细、导航历史规范 |
| 2026-02-17 | v2.1 | PRD审查修复: A2-Receptionist角色定义, A3-超时15min/警告2min, A4-本地模式同远程超时, E3-新建医案仅Doctor可见 |
| 2026-02-21 | v2.2 | PRD vs Code 偏差分析修订: 5 项修订, 1 项延期标注 |
| 2026-02-26 | v2.3 | **Sprint 4 已实现标记**: 角色菜单可见性 MenuManager (T4-S6-01~02)、导航历史上限 20 条 (T4-S6-03) |

# Desktop Shell 产品需求文档

---

## 1. Problem Statement

### 1.1 问题描述

中医诊所管理系统需要一个统一的桌面客户端宿主框架，将分散的功能模块 (患者管理、医案管理、药材管理等) 整合到一个连贯的用户体验中。缺乏统一的壳程序意味着模块加载无序、导航混乱、启动过程不可控，医生无法高效地在不同功能之间切换。

### 1.2 用户痛点

| 角色 | 痛点 | 影响 |
|------|------|------|
| 医生 | 接诊时需频繁在患者、医案、验方之间切换，缺乏统一导航 | 每次切换浪费 5-10 秒，打断诊疗思路 |
| 医生 | 应用启动慢或启动失败时没有明确反馈 | 不知道是否在加载、是否该等待，焦虑感增加 |
| 管理员 | 不同角色能看到的功能不同，需要权限控制 | 前台误操作管理功能导致数据错误 |
| 所有用户 | 应用外观单调，长时间使用视觉疲劳 | 降低工作效率和使用意愿 |

### 1.3 证据

- 模块化架构需求: 系统包含 7+ 功能模块，需要统一的加载和生命周期管理
- 角色差异化: 4 级角色 (SuperAdmin/Admin/Doctor/Receptionist) 对应不同的功能可见性
- 诊疗流程观察: 医生单次接诊平均需要在 3-4 个功能模块间切换

---

## 2. Target Users

| 角色 | 在本模块中的交互 |
|------|-----------------|
| SuperAdmin | 使用全部 Shell 基础设施，可见所有菜单 (含系统设置) |
| Admin | 使用 Shell 基础设施，可见管理菜单 (用户管理、药材管理等) |
| Doctor | 使用 Shell 基础设施，可见临床菜单 (医案管理、验方管理等) |
| Receptionist | 使用基础设施，可见患者管理 + 读卡器 + 未完成医案简要提示 |

> Shell 基础设施对所有角色透明提供服务，角色差异体现在菜单可见性和导航目标。

---

## 3. Strategic Context

### 3.1 业务目标

| 目标 | 对应 |
|------|------|
| 高效诊疗 | 统一导航 + 快捷键系统最小化模块切换摩擦，保持诊疗节奏 |
| 模块化扩展 | 基于 Prism 9.0 的模块化架构，新功能模块可独立开发和热加载 |
| 角色权限可视化 | 菜单和导航根据角色动态显示/隐藏，防止越权操作 |
| 可靠启动 | 启动流水线 + 诊断系统确保启动过程可观测、可降级、可恢复 |

### 3.2 Why Now

Shell 是所有功能模块的运行基座。没有稳定的 Shell 基础设施，任何业务模块都无法被用户访问。这是系统交付的前提条件，不是可选功能。

---

## 4. Solution Overview

Desktop Shell 采用 Prism 9.0 模块化架构，作为 WPF 客户端的宿主框架，负责应用全生命周期管理:

**核心能力:**
- **启动流水线**: StartupPipeline 按序执行启动步骤，支持进度报告、取消、诊断和失败降级
- **登录协调**: LoginCoordinator 协调远程/本地模式分发、会话启动和模块加载
- **会话管理**: SessionLifecycleManager 管理用户活跃度追踪、Token 滑动刷新、不活跃超时登出
- **页面导航**: NavigationCoordinator 封装 Prism Region 导航，提供统一导航入口 + 历史回退
- **菜单系统**: MenuManager 管理菜单命令、全局快捷键和角色权限可见性
- **主题切换**: 支持浅色/深色主题一键切换
- **启动诊断**: StartupDiagnostics 记录启动步骤耗时，检测慢步骤，生成诊断报告

**启动流程:**
```
应用启动 → Splash Screen (进度条) → StartupPipeline 执行步骤
  → [成功] 关闭 Splash → 显示登录页 → 认证 → 加载角色模块 → 导航到角色首页
  → [失败] Splash 显示错误 → 提供 "重试" / "切换本地模式" / "退出"
```

---

## 5. Success Metrics

| 指标 | 当前 | v1.0 目标 | 衡量方式 |
|------|------|----------|---------|
| 应用启动耗时 | N/A | < 5 秒 (正常)，慢步骤阈值 3 秒 | StartupDiagnostics Report |
| 启动成功率 | N/A | > 99% (含降级恢复) | 启动诊断日志 |
| 模块切换耗时 | N/A | < 1 秒 (导航到目标视图) | 用户操作观察 |
| 角色菜单正确率 | N/A | 100% (无越权菜单项) | 菜单可见性矩阵验证 |
| 快捷键覆盖率 | N/A | 核心操作 100% 有快捷键 | 快捷键矩阵 |

---

## 6. Epic Hypothesis

We believe that 实现基于 Prism 9.0 的统一壳程序 (启动流水线 + 登录协调 + 会话管理 + 导航系统 + 菜单系统 + 主题切换 + 启动诊断) for 诊所全部用户 (医生/管理员/前台) will achieve 高效的模块切换体验和可靠的应用启动过程。We'll know we're right when 应用启动耗时 < 5 秒、模块切换 < 1 秒、且角色菜单可见性 100% 正确。

---

## 7. User Stories

### 优先级汇总

| US 编号 | 名称 | Priority |
|---------|------|----------|
| US-SHELL-001 | 应用启动流水线 | Must |
| US-SHELL-002 | 用户登录协调 | Must |
| US-SHELL-003 | 会话生命周期管理 | Must |
| US-SHELL-004 | 页面导航系统 | Must |
| US-SHELL-005 | 菜单与快捷键系统 | Must |
| US-SHELL-006 | 启动诊断与性能监控 | Should |
| US-SHELL-007 | 账户设置 | Could |

---

### US-SHELL-001: 应用启动流水线

> As a 用户, I want to 应用启动时看到进度反馈并在失败时获得明确提示,
> so that 我知道应用正在加载，并能在失败时选择重试或切换模式。

**Acceptance Criteria:**
- [ ] 应用启动 -> Splash Screen 显示进度条 + 当前步骤名称
- [ ] Splash Screen 至少显示 1 秒 (避免闪烁)
- [ ] 启动步骤失败 -> 显示错误对话框 + "重试" / "退出" 按钮
- [ ] API 不可达 -> 提示 "服务器连接失败"，提供 "切换到本地模式" 按钮
- [ ] 支持取消启动过程

**Business Rules:**
1. 启动流水线由 IStartupPipeline 管理，步骤通过 RegisterStep 注册
2. 步骤按注册顺序依次执行 (ExecuteAsync)
3. 支持 IProgress\<string\> 进度报告和 CancellationToken 取消
4. 每步完成触发 StepCompleted 事件，流水线状态变更触发 StateChanged 事件
5. 启动失败时记录诊断信息 (GetDiagnostics)，提供用户友好错误提示
6. 支持 Reset 重置流水线状态

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | 启动步骤包含 API 连通性检查 |
| 本地 | 跳过 API 相关步骤，初始化本地数据库 |

**Splash Screen 布局:**
```
+----------------------------------+
|                                  |
|           [应用 Logo]            |
|                                  |
|    凌隐宝堂中医诊所管理系统       |
|                                  |
|  ================----  60%      |
|  正在初始化数据库连接...          |
|                                  |
+----------------------------------+
```

**启动失败降级策略:**
| 失败场景 | 处理 |
|----------|------|
| API 不可达 | 提示 "服务器连接失败"，提供 "切换到本地模式" 按钮 |
| SQLite 初始化失败 | 提示错误详情，提供 "重试" / "退出" 按钮 |
| 配置文件缺失 | 提示 "配置文件错误"，提供错误详情 |

### US-SHELL-002: 用户登录协调

> As a 用户, I want to 登录后自动加载我角色对应的功能模块并导航到工作台,
> so that 我可以直接开始工作而不需要手动配置。

**Acceptance Criteria:**
- [ ] 正确凭据 -> 登录成功 -> 导航到角色首页
- [ ] 错误凭据 -> 显示错误消息，保持登录页
- [ ] Admin 登录 -> 加载管理员模块，导航到管理工作台
- [ ] Doctor 登录 -> 加载临床模块，导航到临床工作台
- [ ] 登出 -> 返回登录页，清除会话和导航历史

**Business Rules:**
1. LoginCoordinator 聚合多个依赖 (含可选: CredentialVault, UsernameStorage, LocalAuthService)
2. 登录流程: 凭据验证 -> 会话启动 -> 模块加载 -> 角色首页导航
3. 远程模式: 调用 IAuthenticationService API 验证
4. 本地模式: 调用 ILocalAuthService 本地验证
5. 登录成功后自动加载用户角色对应的 Prism 模块
6. 登出流程: 结束会话 -> 清除导航历史 -> 返回登录页
7. 支持 IAuthenticationStateMachine 状态机驱动
8. 事件: LoginSucceeded, LogoutCompleted, StateChanged

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | JWT Token 认证，凭据可选 DPAPI 加密存储 |
| 本地 | 简化认证，无 Token 机制 |

### US-SHELL-003: 会话生命周期管理

> As a 诊所管理者, I want to 用户会话被自动管理 (活跃刷新、不活跃超时),
> so that 医生专注诊疗时不被打断，离开工位时数据自动受保护。

**Acceptance Criteria:**
- [ ] 登录成功 -> CurrentState 变为已认证
- [ ] 用户无活动超过 15 分钟 -> 触发 SessionExpired，静默登出
- [ ] 用户活跃时 Token 即将过期 -> 自动刷新，用户无感知
- [ ] 登出 -> 清除所有会话状态

**Business Rules:**
1. 会话状态: 未认证 -> 已认证 -> 会话活跃 -> 会话过期/登出
2. Token 生命周期监控: TokenRemainingTime 属性
3. 用户活跃度追踪: IUserActivityTracker 监测键盘/鼠标活动
4. 无活动超时: InactivityTimeoutMinutes (默认 15 分钟，可配置)
5. 自动刷新 Token: 用户活跃时自动调用 RefreshTokenAsync
6. 会话过期事件: SessionExpired 触发自动登出
7. 实现 IDisposable: 释放 Timer 和事件订阅

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | JWT Token 滑动刷新 + 不活跃超时登出 |
| 本地 | 简化会话状态，无 Token 刷新。不活跃超时同远程模式 (防信息泄露) |

### US-SHELL-004: 页面导航系统

> As a 医生, I want to 在功能模块之间快速切换并能回退到上一个页面,
> so that 我可以高效地在患者、医案、验方之间流转，不丢失操作上下文。

**Acceptance Criteria:**
- [ ] NavigateTo("PatientListView") -> ContentRegion 显示患者列表
- [ ] NavigateBack() -> 返回上一个视图
- [ ] ClearHistory() -> 清空导航历史，CanNavigateBack=false
- [ ] 导航参数正确传递到目标 ViewModel

**Business Rules:**
1. 基础导航: NavigateTo(viewName, parameters) 导航到指定视图
2. 角色首页: NavigateToHome() / NavigateToHome(role) 导航到角色工作台
3. 返回导航: NavigateBack() 从历史栈回退，快捷键 Alt+左箭头
4. 历史管理: NavigationHistory 只读列表, ClearHistory() 清空，最多 20 条
5. Region 管理: ShowLoginDialog / ClearLoginRegion / ClearContentRegion
6. 导航参数: 通过 IDictionary\<string, object\> 传递
7. 导航变更事件: NavigationChanged 通知视图切换
8. 不支持前进导航
9. 登出时清空导航历史

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | 与本地模式导航行为一致 |
| 本地 | 与远程模式导航行为一致 |

**Prism Region 定义:**

| Region 名称 | 位置 | 承载视图 |
|-------------|------|---------|
| LoginRegion | 全屏覆盖 | LoginView |
| ContentRegion | 主内容区 | PatientListView, PatientDetailView, MedicalCaseListView, MedicalCaseEditView, HerbListView, HerbDetailView, FormulaListView, FormulaDetailView, UserListView, UserDetailView, SyncView, SettingsView |
| MenuRegion | 左侧侧边栏 | NavigationMenuView |
| StatusBarRegion | 底部状态栏 | StatusBarView |
| DialogRegion | 模态遮罩 | 各种对话框 (确认/导入/冲突解决等) |

**导航参数规范:**

| 导航场景 | 参数 | 类型 | 说明 |
|----------|------|------|------|
| 患者详情 | PatientId | Guid | 患者 ID |
| 患者医案 | PatientId | Guid | 从患者详情导航到医案列表，自动过滤 |
| 医案编辑 | MedicalCaseId | Guid | 编辑现有医案 |
| 新建医案 | PatientId | Guid | 为指定患者创建新医案 |
| 药材详情 | HerbId | Guid | 药材 ID |
| 验方详情 | FormulaId | Guid | 验方 ID |
| 用户详情 | UserId | Guid | 用户 ID |

### US-SHELL-005: 菜单与快捷键系统

> As a 用户, I want to 通过菜单和快捷键快速执行常用操作,
> so that 我可以高效完成工作而不需要逐级点击。

**Acceptance Criteria:**
- [ ] Ctrl+N -> 快速添加患者
- [ ] Ctrl+S -> 触发当前页面保存
- [ ] F5 -> 触发当前页面刷新
- [ ] 主题切换 -> 所有控件样式更新
- [ ] 不同角色登录 -> 菜单项按角色可见性矩阵显示/隐藏

**Business Rules:**
1. 快速操作: QuickAddPatient (Ctrl+N), QuickStartMedicalCase (Ctrl+Shift+C)
2. 全局命令: SaveAll (Ctrl+S), RefreshAll (F5), Print (Ctrl+P), Export
3. 编辑命令: Undo (Ctrl+Z), Redo (Ctrl+Y)
4. 系统命令: Help (F1), Settings (Ctrl+,), ToggleTheme
5. 导航命令: NavigateToHome, NavigateToSystemSettings, EditProfile
6. 主题管理: 支持浅色/深色主题切换 (ApplyLightTheme/ApplyDarkTheme)
7. 菜单内容根据用户角色动态显示/隐藏 (通过 IApplicationCommands)

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | 全部菜单可用 |
| 本地 | 部分菜单不可用 (如需要服务端的操作) |

**完整菜单结构:**

```
顶部菜单栏
+-- 文件 (File)
|   +-- 新建患者          Ctrl+N       所有角色
|   +-- 新建医案          Ctrl+Shift+C Doctor
|   +-- ---------------
|   +-- 打印              Ctrl+P       Doctor+
|   +-- ---------------
|   +-- 退出              Alt+F4       所有角色
|
+-- 编辑 (Edit)
|   +-- 撤销              Ctrl+Z       所有角色
|   +-- 重做              Ctrl+Y       所有角色
|   +-- ---------------
|   +-- 保存              Ctrl+S       所有角色
|
+-- 视图 (View)
|   +-- 刷新              F5           所有角色
|   +-- ---------------
|   +-- 浅色主题                       所有角色
|   +-- 深色主题                       所有角色
|
+-- 导航 (Navigate)                    -- 侧边栏菜单
|   +-- 首页                           所有角色
|   +-- 患者管理                       所有角色
|   +-- 医案管理                       Doctor+
|   +-- 验方管理                       Doctor+
|   +-- ---------------               (Management 模式可见)
|   +-- 药材管理                       Admin+
|   +-- 用户管理                       Admin+
|   +-- 数据同步                       Admin+ (Doctor也可)
|   +-- 系统设置                       SuperAdmin
|
+-- 工具 (Tools)
|   +-- 数据同步                       Doctor+
|   +-- 系统健康检查                   Admin+
|
+-- 帮助 (Help)
    +-- 帮助文档          F1           所有角色
    +-- 关于                           所有角色
```

**菜单可见性矩阵:**

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

### US-SHELL-006: 启动诊断与性能监控

> As a 管理员, I want to 查看应用启动的详细诊断报告,
> so that 我可以识别启动瓶颈并向技术支持提供准确的故障信息。

**Acceptance Criteria:**
- [ ] 启动完成后 GetReport 返回所有步骤的耗时
- [ ] 步骤耗时 > 3 秒 -> 标记为慢步骤
- [ ] 步骤失败 -> 记录 errorMessage
- [ ] 诊断结果自动记录到日志

**Business Rules:**
1. BeginStartup/EndStartup: 标记启动过程的开始和结束
2. BeginStep/EndStep: 记录单个步骤的名称、耗时、成功/失败
3. RecordMarker: 记录关键时间点标记
4. 慢步骤检测: 超过 3 秒的步骤标记为慢步骤 (SlowStepThresholdSeconds=3.0)
5. GetReport: 生成 StartupReport (步骤列表 + 总耗时 + 慢步骤列表)
6. 诊断结果自动记录到日志

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | 包含 API 连通性检查步骤 |
| 本地 | 包含本地数据库初始化步骤 |

### US-SHELL-007: 账户设置

> As a 用户, I want to 查看和修改我的个人信息和密码,
> so that 我可以保持账户信息的准确性和安全性。

**Acceptance Criteria:**
- [ ] 点击账户设置 -> 显示 AccountSettingsControl
- [ ] 修改密码 -> 弹出密码修改对话框 (旧密码 + 新密码 + 确认密码)
- [ ] 保存个人资料 -> 调用 API 更新

**Business Rules:**
1. 查看当前登录用户信息
2. 修改密码入口 (关联 users.md FR-USER-009)
3. 个人资料编辑: 显示名称 / 电话 / 邮箱 (关联 users.md FR-USER-010)
4. 查看登录信息: 最后登录时间 / 登录 IP (只读)
5. 通过 MenuManager.EditProfileCommand 进入
6. 界面布局: 模态对话框或侧边滑出面板

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | 修改通过 API 提交 |
| 本地 | 修改本地存储 |

---

## 8. Out of Scope

| 排除项 | 原因 |
|--------|------|
| 多窗口/多标签页 | 增加架构复杂度，诊所单屏场景不需要，后续版本考虑 |
| 网络状态实时监控 | 状态栏网络指示器延期到后续版本 |
| 前进导航 | 增加导航复杂度，仅后退已满足需求 |
| 插件市场/动态模块下载 | 超出 v1.0 范围，模块编译时静态注册 |
| 最后登录时间/IP 信息显示 | 非当前优先级，Sprint 后续实现 |
| 触摸屏优化 | 诊所以鼠标键盘为主，触控非优先 |

---

## 9. Dependencies & Risks

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| Prism 9.0 模块加载失败 | 角色功能完全不可用 | 启动诊断 + 错误对话框 + 详细日志 |
| 启动步骤阻塞 | 应用长时间无响应 | 慢步骤检测 (3 秒阈值) + 取消支持 + 超时机制 |
| 远程 API 不可达 | 远程模式无法使用 | 启动失败降级: 提供 "切换到本地模式" 按钮 |
| 导航历史内存泄漏 | 长时间运行内存增长 | 导航历史限制 20 条，登出时清空 |
| 角色菜单权限配置错误 | 越权访问功能 | 菜单可见性矩阵 + 架构测试验证 |

**模块依赖:**

| 依赖模块 | 关系 | 说明 |
|----------|------|------|
| auth.md | 强依赖 | 登录协调依赖认证模块的 IAuthenticationService |
| users.md | 弱依赖 | 账户设置关联用户模块的修改密码/个人资料功能 |
| 各业务模块 | 加载关系 | Shell 按角色动态加载 Patients/MedicalCases/Herbs/Formulas/Users 模块 |

---

## 10. Open Questions

| ID | 问题 | 状态 |
|----|------|------|
| OQ-SHELL-01 | 超时前警告是否恢复? simplify-auth 已移除，是否永久移除? | 已确定: 永久移除，仅保留静默登出 |
| OQ-SHELL-02 | 启动诊断报告是否暴露给用户查看? 还是仅写入日志? | 延期。当前仅日志记录，后续版本考虑管理员诊断面板 |
| OQ-SHELL-03 | 本地模式下哪些菜单项应禁用? 需要完整的本地模式菜单可用性矩阵 | 待定。当前标注 "部分菜单不可用"，需逐项确认 |
| OQ-SHELL-04 | 状态栏是否需要显示网络状态指示? | 延期到后续版本 |

---

## Data Model

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

### AuthState

| 值 | 说明 |
|----|------|
| NotAuthenticated | 未认证 |
| Authenticating | 认证中 |
| Authenticated | 已认证 |
| LoggingOut | 登出中 |

### StartupReport

| 字段 | 类型 | 说明 |
|------|------|------|
| Steps | List\<StepRecord\> | 步骤记录列表 |
| TotalDuration | TimeSpan | 总耗时 |
| SlowSteps | List\<StepRecord\> | 慢步骤 (>3秒) |
| StartTime | DateTime | 启动时间 |
| EndTime | DateTime | 结束时间 |

### 状态栏信息

```
+-------------------------------------------------------------+
| 当前用户: 张医生 (Doctor)  |  远程模式  |  v1.0.0            |
+-------------------------------------------------------------+
```

| 位置 | 内容 | 更新时机 |
|------|------|---------|
| 左侧 | 当前用户名 + 角色 | 登录/登出时 |
| 中间 | 运行模式 (远程模式 / 本地模式) | 模式切换时 |
| 右侧 | 版本号 | 固定 |
| 可选 | 网络状态指示 (后续版本) | 网络变化时 |

---

## Error Codes

> Desktop Shell 为客户端宿主层，不定义独立错误码。启动失败和导航错误通过异常和日志处理，认证相关错误码见 [auth.md](auth.md)。

---

## Decision Log

| 编号 | 决策 | 结论 | 日期 |
|------|------|------|------|
| SHELL-D01 | Shell 基础设施是否需要 PRD | 是。Shell 包含 7 个独立功能需求，值得正式文档化 | 2026-02-11 |
| SHELL-D02 | 本地模式 Shell 差异 | 大部分 Shell 功能不区分模式，仅认证和 Token 管理有差异 | 2026-02-11 |
| SHELL-D03 | 慢步骤阈值 | 3.0 秒，平衡诊断精度和噪声 | 2026-02-11 |
| SHELL-D04 | 状态栏信息 | 左: 用户名+角色, 中: 运行模式, 右: 版本号。简洁实用 | 2026-02-17 |
| SHELL-D05 | 启动画面 | 进度报告型: Logo+进度条+当前步骤名称。失败时提供降级/重试选项 | 2026-02-17 |
| SHELL-D06 | 导航历史 | 仅支持后退 (Alt+左箭头)，不支持前进。历史深度最多 20 条 | 2026-02-17 |

### 修订历史

| 日期 | 修订项 | 原因 | 参考 |
|------|--------|------|------|
| 2026-02-21 | 登录协调依赖计数不匹配 | 代码重构后依赖数量可能变化，PRD 硬编码数字易过时，以代码实际注入为准 | SHELL-12 |
| 2026-02-21 | 超时前警告已被 simplify-auth 移除 | simplify-auth 重构移除了超时前警告机制，仅保留静默登出 | SHELL-04 |
| 2026-02-21 | 状态枚举命名差异 | 确保 PRD 枚举命名与代码一致 | SHELL-11 |
| 2026-02-21 | StartupReport 返回类型差异 | PRD 类型定义与代码实际实现对齐 | SHELL-13 |
| 2026-02-21 | 启动诊断信息格式差异 | PRD 诊断信息格式规范与代码输出对齐 | SHELL-14 |
| 2026-02-21 | 缺少最后登录时间/IP 信息显示 | 非当前优先级，延期到 Sprint 后续 | SHELL-08 |

---

## Change Log

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-11 | v1.0 | 初始版本，从代码实现逆向工程 |
| 2026-02-17 | v2.0 | Round 5 深化: 新增完整菜单层级+角色可见性矩阵、Prism Region定义+导航参数、状态栏信息、启动画面规格+失败降级、账户设置详细、导航历史规范 |
| 2026-02-17 | v2.1 | PRD审查修复: A2-Receptionist角色定义, A3-超时15min/警告2min, A4-本地模式同远程超时, E3-新建医案仅Doctor可见 |
| 2026-02-21 | v2.2 | PRD vs Code 偏差分析修订: 5 项修订, 1 项延期标注 |
| 2026-02-26 | v2.3 | Sprint 4 已实现标记: 角色菜单可见性 MenuManager (T4-S6-01~02)、导航历史上限 20 条 (T4-S6-03) |
| 2026-03-06 | v3.0 | PRD 全面重写: FR->US 格式迁移，新增 Problem Statement/Strategic Context/Success Metrics/Epic Hypothesis/Out of Scope/Dependencies & Risks/Open Questions 7 个章节 |

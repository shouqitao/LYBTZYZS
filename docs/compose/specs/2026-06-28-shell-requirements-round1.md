# Shell 需求规格（v1.0 冻结版）

> **状态**：✅ **已冻结** — Phase② 设计的正式输入
> **日期**：2026-06-28
> **来源**：整合 `docs/02-requirements/11-platform.md` + `12-nfr.md` + `docs/01-product/02-personas.md` + `DESIGN.md` v2 + 审查基线 `2026-06-28-shell-audit-baseline.md`
> **用途**：作为 Phase②「Shell 整层设计」的输入。每条需求将成为设计验收标准。
> **不在范围**：Server 立、各业务模块内部需求（仅取其对 Shell 的约束）。
> **冻结依据**：Round 1 缺口/冲突通过与用户逐条梳理，全部收敛为确定决策。

---

## [S1] 范围与角色

Shell 是 WPF Desktop 客户端宿主，承载 4 类用户的全生命周期交互。Shell 自身不实现业务逻辑，只提供：登录鉴权入口、角色路由、模块加载编排、共享 chrome（导航/状态栏/对话框/通知）、双模式切换、全局错误与日志兜底。

| 用户类型 | 身份性质 | PermissionLevel | 首页视图 | Shell 必须加载的模块 |
|---------|---------|----------------|---------|---------------------|
| **Sysadmin** | 独立用户（非角色，`IsSysAdmin=true`） | 跳过角色检查 | AdminHome | 5（全量） |
| **Admin** | 角色 | 10 | AdminHome | 5（Users/Patients/Herbs/Formula/MedicalCase） |
| **Doctor** | 角色 | 1 | ClinicalWorkspace | **6**（含 Registration） |
| **Receptionist** | 角色 | 0 | ReceptionistHome | **3**（Users/Patients/Registration） |

**单一真相源**：`RoleRegistry` ← 各 `RoleDefinition.cs` 的 `RequiredModules` + `HomeViewName`（见 `02-personas.md` 架构层差异汇总表）。

> ✅ **冲突 C1 已关闭**：`LoginCoordinator.LoadModulesForUserAsync` 硬编码绕过 `RoleRegistry`，且对非 Admin 角色只加 `PatientsModule`。**决策：A — 删 LoginCoordinator 旁路，统一走 RoleRegistry，消除双轨。** 这是历史遗留 bug，需重构。

---

## [S2] 功能需求（整合自 `11-platform.md`）

### [S2.1] FR-01 应用启动与单实例（源自 US-SHELL-001，优先级 Must）

**作为** 用户，**我想要** 应用单实例启动并看到启动闪屏与进度反馈，**以便** 避免多开导致的数据竞争，并在启动失败时获得明确提示。

**验收标准**：
1. 已有实例运行时拒绝第二次启动（`Mutex` 命名 `Global\LYBTZYZS_Shell_Instance`）
2. 启动时显示 Splash Screen（**Logo + 进度条 + 当前步骤名**），至少显示 1 秒避免闪烁
3. 启动步骤失败 → 错误对话框 + "重试"/"退出"
4. API 不可达 → 提示并提供"切换到本地模式"按钮
5. 两阶段 Serilog 引导：先 bootstrap logger 捕获早期错误，再切换最终 logger
6. Debug 模式运行上限 120 分钟

**双模式**：远程启动含 API 连通性检查；本地跳过 API 步骤、初始化本地数据库。

> ⚠️ **符合性**：验收标准 2（Splash 显示步骤名/进度）**未满足** — 审查 S5 发现 `AppStartupOrchestrator.RunStartupAsync` 调 `pipeline.ExecuteAsync()` 未传 `IProgress<string>`，step 内 `progress?.Report()` 全部 no-op。验收标准 1/4/5 已满足。

---

### [S2.2] FR-02 角色基础模块加载（源自 US-SHELL-003，优先级 Must）

**作为** 用户，**我想要** 登录后系统按我的角色自动加载对应功能模块，**以便** 直接进入工作台且无越权菜单。

**验收标准**：
1. Admin 登录 → 加载管理模块，导航到 AdminHome
2. Doctor 登录 → 加载临床模块，导航到 ClinicalWorkspace
3. Receptionist 登录 → 加载 Patients + CardReader 模块，导航到 ReceptionistHome
4. 登出 → 清除会话与导航历史，返回登录页

**业务规则**：
1. **`ApplicationBootstrapper.LoadModulesForRoleAsync` 按角色过滤 Prism 模块**（原文）
2. 菜单可见性矩阵：系统设置仅 SuperAdmin；药材/用户管理 Admin+；医案/验方 Doctor+；患者管理全部角色
3. 角色层级：Receptionist=0, Doctor=1, Admin=10, SuperAdmin=100

> ✅ **C1 已关闭**：见 S1。决策：删 LoginCoordinator 旁路，统一走 RoleRegistry。Doctor/Receptionist 登录后将正确加载角色模块。

---

### [S2.3] FR-03 账户设置（源自 US-SHELL-004，优先级 Could）

**作为** 用户，**我想要** 查看和修改个人信息与密码，**以便** 保持账户信息准确与安全。

**验收标准**：
1. 点击账户设置 → 显示 `AccountSettingsControl`
2. 修改密码 → 对话框（旧密码 + 新密码 + 确认密码）
3. 保存个人资料 → 调 API 更新（IDOR 防护：仅本人）

**业务规则**：个人资料编辑显示名称/电话/邮箱；修改密码需旧密码；登录信息只读；入口 `MenuManager.EditProfileCommand`。

> **符合性**：已实现。审查仅发现密码长度校验硬编码 `< 8` 未复用 Shared.Validators（小问题）。

---

### [S2.4] FR-04 菜单导航（源自 US-SHELL-005，优先级 Must）

**作为** 医生，**我想要** 在功能模块间快速切换并能回退，**以便** 高效流转而不丢失上下文。

**验收标准**：
1. `NavigateTo(viewName, params)` → ContentRegion 显示目标视图
2. `NavigateBack()` → 返回上一视图（**Alt+左箭头**）
3. 导航历史最多 20 条，登出时清空
4. 导航参数正确传递到目标 ViewModel
5. 不同角色登录 → 菜单项按可见性矩阵显示/隐藏

**业务规则**：基于 Prism Region 导航（`NavigationCoordinator` 封装）；主题切换浅色/深色一键切换。

> ✅ **冲突 C2 已关闭**：文档列的快捷键（Ctrl+S/F5/Ctrl+P）与代码实际绑定（F1/Ctrl+,/Ctrl+M 等）几乎不重叠。**决策：合并去重，参考业界常用设定，统一为一套。** 具体快捷键清单在 Phase② 设计中定义。

---

### [S2.5] FR-05 双模式连接切换（源自 US-SHELL-007，优先级 Must）

**作为** 医生，**我想要** 手动切换远程/本地工作模式，**以便** 外出看诊离线工作。

**验收标准**：
1. 切换到本地 → Repository 使用 LocalXxxRepository（LocalDB）
2. 切换到远程 → Repository 使用 Refit HTTP API
3. 本地有未完成医案（Active/Suspended）时切换到远程 → 阻断并提示（ERR-70506）
4. 切换失败 → 自动回退到切换前模式
5. 切换成功 → **状态栏显示模式标识**

**业务规则**：`IConnectionModeProvider.SwitchModeAsync`（5 步）驱动；本地→远程前置检查（无未结医案 + 网络连通 + Token 有效）；`SwitchingApiClient` URL 路由；异常返回 `ModeSwitchResult.Failed` 自动回退。

> **符合性**：已实现。验收标准 5 依赖状态栏（见 S3 缺口 G3）。

---

## [S3] 非功能需求（摘自 `12-nfr.md`）

| ID | 对 Shell 的约束 | 符合性 |
|----|---------------|--------|
| NFR-PERF-002 | 冷启动 < 5s（双击→登录页）；热启动 < 1s；页面切换 < 1s | ⚠️ 冷启动受双轨初始化拖累（S5），待测 |
| NFR-AVAIL-004 | `Mutex`(`Global\LYBTZYZS_Shell_Instance`) 单实例 | ✅ 已满足（`App.xaml.cs:36-68`） |
| NFR-COMP-004 | 双模式同 Service 层，URL 路由切换 | ✅ `SwitchingApiClient` 已满足 |
| NFR-MAINT-001 | 架构测试守护分层（Controller→Service→Repository 单向） | ✅（Shell 不在此约束范围） |
| NFR-SEC-001 | AccessToken 远程 30min / 本地 1 年；账户锁定可配 | ✅ Token 生命周期由 `SessionLifecycleManager` 管 |
| NFR-MAINT-003 | 全局异常统一捕获，生产屏蔽堆栈返回中文消息 + TraceId | ✅ G8 已决策：Toast 通知 |

---

## [S4] 视觉/布局约束（整合 `DESIGN.md` v2 + 审查）

### 已规格化（DESIGN.md v2 覆盖）
- 配色：Brown 主 / Amber 辅 / Surface L0(#FAF8F5)~L3 / 功能色 / 文字色，全部 WCAG AA+
- 字体：Microsoft YaHei UI（正文）/ Cascadia Code（等宽数据）/ PackIcon（图标，禁 emoji）
- 间距 Token：SpacingXS(4)~XXXL(40)
- 圆角/投影 Token；组件规范（InfoCard/MasterDetail/DetailToolbar/DataGrid/表单/筛选栏/PopupBox）
- MDIX 资源加载顺序（Surfaces.xaml 必须在 MDIX 之后）

### Shell 层布局规格（Round 2 决策）— ✅ 已冻结

| 缺口 ID | 问题 | 决策 | 说明 |
|--------|------|------|------|
| **G1** | 侧边栏形态 | **A 固定 Border** | 当前固定 Border + 宽度切换(60/140)，保持不变 |
| **G2** | 侧边栏行为 | **默认展开，不记忆状态** | 当前 `IsSidebarExpanded` 切换，无持久化 |
| **G3** | 状态栏必显项 | **全部显示** | 时间 + 用户 + 角色 + 连接模式 + 健康状态 |
| **G4** | 窗口 chrome | **仅最大化** | `WindowStyle=None` + 仅最大化，无窗口化/拖拽 |
| **G5** | 断网 UX | **状态栏变色 + 非模态横幅** | 断网时状态栏变黄/红，顶部横幅提示，不打断操作 |
| **G6** | 登录区行为 | **首次强制改密 + Sysadmin 本地自动登录** | personas 要求落地 |
| **G7** | 主题切换 | **双主题，默认 Light** | DESIGN.md 说仅 Light，但代码已有 Dark 切换；决策支持双主题 |
| **G8** | 错误 UX | **Toast 通知** | NFR 要求屏蔽堆栈+中文消息，Shell 用 Toast 非模态呈现 |
| **G9** | 对话框体系 | **全迁 MDIX DialogHost** | 统一风格，打印预览等需独立窗口的除外 |
| **G10** | 导航项配置 | **扁平列表** | 按角色可见性矩阵显示/隐藏，图标用 Icons.xaml 已有几何 |

---

## [S5] 符合性矩阵汇总（需求 vs 当前代码）

| 需求 | 状态 | 证据 |
|------|------|------|
| FR-01 启动单实例 | ✅ | `App.xaml.cs:42-68` Mutex |
| FR-01 Splash 进度 | 🔴 **违反** | 审查 S5：未传 IProgress，进度 no-op |
| FR-01 API 不可达提示+切本地 | ⚠️ 待核 | 启动步骤有 ApiHealthCheck 但 UX 待验 |
| FR-01 两阶段 Serilog | ✅ | `App.xaml.cs:50` |
| FR-02 角色模块加载（Admin） | 🔴 **bug** | C1 已关闭：删旁路统一 RoleRegistry |
| FR-02 角色模块加载（Doctor/Receptionist） | 🔴 **bug** | C1 已关闭：删旁路统一 RoleRegistry |
| FR-02 登出清会话 | ✅ | `SessionLifecycleManager` |
| FR-03 账户设置 | ✅ | AccountSettingsControl |
| FR-04 NavigateTo/Back | ✅ | NavigationCoordinator |
| FR-04 历史最大 20 | ⚠️ 待核 | NavigationCoordinator 实现 |
| FR-04 快捷键 | 🔴 **冲突** | C2 已关闭：合并去重，参考业界标准 |
| FR-05 双模式切换 | ✅ | SwitchingApiClient + ModeSwitchValidator |
| FR-05 状态栏模式标识 | ✅ | G3 已决策：状态栏全部显示 |
| NFR 冷启动 <5s | ⚠️ 待测 | 双轨初始化拖累 |

---

## [S6] Round 2 议题决策记录

所有议题已关闭。以下为最终决策：

| # | 议题 | 决策 | 说明 |
|---|------|------|------|
| 1 | C1 模块加载真相源 | **删 LoginCoordinator 旁路，统一 RoleRegistry** | 历史遗留 bug，需重构 |
| 2 | C2 快捷键冲突 | **合并去重，参考业界标准** | Phase② 设计中定义具体清单 |
| 3 | G1 侧边栏形态 | **固定 Border** | 保持当前实现 |
| 4 | G3 状态栏必显项 | **全部显示** | 时间+用户+角色+连接模式+健康 |
| 5 | G7 主题冲突 | **双主题，默认 Light** | 支持 Light/Dark 切换 |
| 6 | G4 窗口 chrome | **仅最大化** | WindowStyle=None + 仅最大化 |
| 7 | G5 断网 UX | **状态栏变色 + 非模态横幅** | 不打断操作 |
| 8 | G8 错误 UX | **Toast 通知** | 非阻塞，中文摘要 |
| 9 | G9 对话框体系 | **全迁 MDIX DialogHost** | 统一风格 |
| 10 | G6 登录区行为 | **首次强制改密 + Sysadmin 本地自动登录** | personas 要求落地 |
| 11 | G10 导航项配置 | **扁平列表** | 按角色可见性矩阵 |

---

## [S7] 本文档边界

本文档为 **v1.0 冻结版**，是 Phase②「Shell 整层设计」的正式输入。所有缺口/冲突已闭合，决策已记录。设计阶段将基于本文档的验收标准逐项设计实现方案。

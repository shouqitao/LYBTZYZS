# Shell (平台壳程序)

> 版本: v1.0 | 日期: 2026-06-28 | 状态: ✅ 已完成
> Split from 11-platform.md (2026-06-28)

## 模块概述

Shell 采用 Prism 9.0 模块化架构，作为 WPF 客户端宿主，负责应用全生命周期：单实例互斥锁（`Global\LYBTZYZS_Shell_Instance`）、启动闪屏、两阶段 Serilog 引导、按角色动态加载模块（`ApplicationBootstrapper.LoadModulesForRoleAsync`）、页面导航与菜单系统。

> 原 7 US（US-SHELL-001~007），合并去除 2 个冗余后 5 US + SHELL-010~019 补充。**v1.0 有效 = 13 US**（原 5 + 补充 8）；另有 US-SHELL-012 = v2.0（不计入 Platform 43），US-SHELL-015 = 撤销（并入 013）。完整列表：001, 003, 004, 005, 007, 010(v1.0), 011(v1.0), 012(v2.0), 013(v1.0, 含原 015), 014(v1.0), ~~015~~(撤销), 016(v1.0), 017(v1.0), 018(v1.0), 019(v1.0)。

**双模式总则**：`SwitchingApiClient` 将 localhost 请求路由到嵌入式 `LocalWebAPI`，否则走 Refit 远程；`LocalWebAPI` 复用全部服务端模块的 Service 层；`LocalDbBackupService` 仅本地模式运行。

---

### US-SHELL-001: 应用启动（单实例）

**角色**: 所有用户
**优先级**: Must
**状态**: ✅ 已实现

**作为** 用户，**我想要** 应用单实例启动并看到启动闪屏与进度反馈，**以便** 避免多开导致的数据竞争，并在启动失败时获得明确提示。

**验收标准**:
- [ ] 已有实例运行时拒绝第二次启动（`Mutex` 命名 `Global\LYBTZYZS_Shell_Instance`）
- [ ] 启动时显示 Splash Screen（Logo + 进度条 + 当前步骤名），至少显示 1 秒避免闪烁
- [ ] 启动步骤失败 → 错误对话框 + "重试"/"退出"
- [ ] API 不可达 → 提示并提供"切换到本地模式"按钮
- [ ] 两阶段 Serilog 引导：先 bootstrap logger 捕获早期错误，再切换最终 logger

**业务规则**:
1. 单实例互斥锁 `Global\LYBTZYZS_Shell_Instance`。
2. 启动闪屏与应用启动同属一个启动管线（原 US-SHELL-002 已并入）。
3. 两阶段 Serilog bootstrap 在 WebAPI 与 Desktop 均生效。
4. Debug 模式运行上限 120 分钟。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 启动步骤含 API 连通性检查 |
| 本地 | 跳过 API 步骤，初始化本地数据库 |

**实现参考**: `src/Client/Desktop/Shell/App.xaml.cs:41`、`Services/Bootstrap/ApplicationBootstrapper.cs:35`

---

### US-SHELL-003: 角色基础模块加载

**角色**: 所有用户
**优先级**: Must
**状态**: ⚠️ **v1.0 修复**（C1 决策：删 LoginCoordinator 旁路，统一走 RoleRegistry）

**作为** 用户，**我想要** 登录后系统按我的角色自动加载对应功能模块，**以便** 我直接进入工作台而不需手动配置，且无越权菜单。

**验收标准**:
- [ ] Admin 登录 → 加载管理模块，导航到管理工作台
- [ ] Doctor 登录 → 加载临床模块，导航到临床工作台
- [ ] Receptionist 登录 → 加载患者管理 + 读卡器模块
- [ ] 登出 → 清除会话与导航历史，返回登录页

**业务规则**:
1. `ApplicationBootstrapper.LoadModulesForRoleAsync` 按角色过滤 Prism 模块。
2. 菜单可见性矩阵：系统设置仅 SuperAdmin；药材/用户管理 Admin+；医案/验方 Doctor+；患者管理全部角色。
3. 角色层级：Receptionist=0, Doctor=1, Admin=10, SuperAdmin=100。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（模块加载逻辑与模式无关） |

**实现参考**: `src/Client/Desktop/Shell/Services/Bootstrap/ApplicationBootstrapper.cs:35`

---

### US-SHELL-004: 账户设置（个人资料+密码）

**角色**: 所有用户
**优先级**: Could
**状态**: ✅ 已实现

**作为** 用户，**我想要** 查看和修改我的个人信息与密码，**以便** 保持账户信息准确与安全。

**验收标准**:
- [ ] 点击账户设置 → 显示 `AccountSettingsControl`
- [ ] 修改密码 → 弹出对话框（旧密码 + 新密码 + 确认密码）
- [ ] 保存个人资料 → 调用 API 更新（IDOR 防护：仅本人）

**业务规则**:
1. 个人资料编辑：显示名称/电话/邮箱（关联 [03-users.md](03-users.md) US-USER-008）。
2. 修改密码需旧密码（关联 US-USER-009）。
3. 登录信息（最后登录时间/IP）只读。
4. 入口：`MenuManager.EditProfileCommand`。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 修改通过 API 提交 |
| 本地 | 修改提交到本地 Service 层 |

**实现参考**: `AccountSettingsControl`、`MenuManager.EditProfileCommand`

---

### US-SHELL-005: 菜单导航

**角色**: 所有用户
**优先级**: Must
**状态**: ✅ 已实现

**作为** 医生，**我想要** 在功能模块间快速切换并能回退到上一页，**以便** 高效地在患者/医案/验方间流转而不丢失上下文。

**验收标准**:
- [ ] `NavigateTo(viewName, params)` → ContentRegion 显示目标视图
- [ ] `NavigateBack()` → 返回上一视图（Alt+左箭头）
- [ ] 导航历史最多 20 条，登出时清空
- [ ] 导航参数正确传递到目标 ViewModel
- [ ] 不同角色登录 → 菜单项按可见性矩阵显示/隐藏

**业务规则**:
1. 基于 Prism Region 导航（`NavigationCoordinator` 封装）。
2. 全局快捷键：Ctrl+N 新建患者、Ctrl+S 保存、F5 刷新、Ctrl+P 打印。
3. 主题切换：浅色/深色一键切换。
4. 前进导航与面包屑已实现。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 全部菜单可用 |
| 本地 | 部分需服务端的菜单禁用 |

**实现参考**: `NavigationCoordinator`、`MenuManager`、Prism Region 定义

---

### US-SHELL-007: 双模式连接切换

**角色**: 医生
**优先级**: Must
**状态**: ✅ 已实现

**作为** 医生，**我想要** 手动切换远程/本地工作模式，**以便** 根据网络环境选择合适模式，外出看诊离线工作。

**验收标准**:
- [ ] 切换到本地 → Repository 使用 LocalXxxRepository（LocalDB）
- [ ] 切换到远程 → Repository 使用 Refit HTTP API
- [ ] 本地有未完成医案（Active/Suspended）时切换到远程 → 阻断并提示（ERR-70506）
- [ ] 切换失败 → 自动回退到切换前模式
- [ ] 切换成功 → 状态栏显示模式标识

**业务规则**:
1. 切换由 `IConnectionModeProvider.SwitchModeAsync`（5 步）驱动。
2. 本地→远程前置检查：无 Active/Suspended 医案 + 网络连通 + Token 有效（SYNC-D01）。
3. `SwitchingApiClient` 路由 localhost → 嵌入式 `LocalWebAPI`，否则 → 远程。
4. 异常捕获返回 `ModeSwitchResult.Failed`，自动回退。
5. **强制本地策略（S5 决策）**：v1.0 仅支持用户主动切换模式；运维强制某台机器走本地（如断网降级、离线巡诊）属 **v2.0**，需扩展 `SystemAdminOptions` 增加按机器/按用户锁定模式的策略，不在 v1.0 范围。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 不适用（切换操作本身） |
| 本地 | 不适用（切换操作本身） |

**实现参考**: `IConnectionModeProvider.SwitchModeAsync`、`SwitchingApiClient`、`ModeSwitchValidator`

---

### US-SHELL-010: Desktop 安装（Velopack 打包）

**角色**: sysadmin
**优先级**: Must
**状态**: 📋 已设计（🧲 v1.0 待实现）

**作为** sysadmin，**我想要** 一键安装 Desktop 应用，**以便** 不需懂 .NET/SQL Server 技术也能完成部署。

**验收标准**:
- [ ] 提供 `Setup.exe`（Velopack 打包，自包含 .NET 运行时）
- [ ] 安装到 `%LocalAppData%\LYBT`（免管理员权限）
- [ ] 安装后自动创建桌面快捷方式
- [ ] 静默安装支持：`Setup.exe --silent`（用于批量部署）

**业务规则**:
1. 使用 Velopack 打包，替代手动安装 .NET 8 Runtime。
2. 更新源（Update Feed）挂载在 WebAPI 服务器提供。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 安装后指向 WebAPI 地址 |
| 本地 | 安装后自动切换本地模式（内嵌 LocalWebAPI） |

**实现参考**: `velopack` NuGet + `VelopackApp.Build().Run()` 集成于 `App.xaml.cs`

---

### US-SHELL-011: 首次初始化向导（5 步强制）

**角色**: sysadmin
**优先级**: Must
**状态**: 📋 已设计（🧲 v1.0 待实现）

**作为** sysadmin，**我想要** 首次登录后走初始化向导，**以便** 一站式完成系统配置（改密/诊所/模式/admin），不需手动改文件。

**验收标准**:
- [ ] sysadmin 首次登录后强制进入向导，不可跳过
- [ ] Step 1: 强制修改默认密码（`ForceChangeOnFirstLogin=true`）
- [ ] Step 2: 填写诊所信息（名称/科室/地址/电话），驱动处方打印标题
- [ ] Step 3: 选择远程/本地模式（测试远程连通性或跳过用本地）
- [ ] Step 4: 创建首个 admin 账号（用户名 + 临时密码）
- [ ] Step 5: 完成提示"请以 admin 登录继续配置用户/药材" → 注销 sysadmin
- [ ] 向导未完成，sysadmin 无法进入主界面

**业务规则**:
1. `IdentitySeedData` 改为只种子 sysadmin（不种子 admin），admin 由 sysadmin 在向导中手动创建。
2. 默认密码随机生成并显示一次，首登强制改。
3. JWT 密钥首次启动生成随机密钥（替代硬编码）。
4. 向导 UI 参考业界最佳实践。

**实现参考**: 扩展现有 `FirstRunSetupViewModel`（Auth 模块），或新建 `InitializationWizard`

---

### US-SHELL-012: Desktop 自动更新（Velopack）

**角色**: 所有用户
**优先级**: Should
**状态**: 📋 已设计（v2.0 规划，不在 v1.0 范围）

**作为** 用户，**我想要** Desktop 自动检查更新并一键升级，**以便** 始终使用最新版本而不需手动操作。

**验收标准**:
- [ ] 启动时自动检查远程更新源（Velopack `CheckForUpdatesAsync`）
- [ ] 普通更新：提示用户"发现新版本 vX.X"，用户自愿下载安装
- [ ] 安全更新（标记为 critical）：强制倒计时升级（可被 sysadmin 延迟）
- [ ] 更新下载支持增量（delta），省带宽
- [ ] 更新安装后自动重启应用
- [ ] 更新过程中数据不丢失
- [ ] 展示 Release Notes（更新说明）

**业务规则**:
1. 更新源：WebAPI 服务器提供更新包。
2. sysadmin 可配置：是否允许跳过更新、安全更新强制窗口。
3. 更新采用 Velopack（替代已维护模式的 Squirrel.Windows）。

**实现参考**: `UpdateManager.CheckForUpdatesAsync()` / `DownloadUpdatesAsync()` / `ApplyUpdatesAndRestart()`

---

### US-SHELL-013: 数据库备份恢复（含备份状态展示 + 手动备份）

**角色**: sysadmin
**优先级**: Should
**状态**: ⚠️ 部分实现（备份有，恢复 UI 缺）（🧲 v1.0，原 US-SHELL-015 已并入）

**作为** sysadmin，**我想要** 从备份恢复 LocalDB 数据库，并查看备份状态/手动触发备份，**以便** 系统崩溃后能自助恢复数据、掌握数据保护情况。

**验收标准**:
- [ ] 显示本地备份文件列表（保留 7 天，含日期/大小）
- [ ] **备份状态展示**（原 015 并入）：上次备份时间、备份文件数量、总大小
- [ ] **手动备份按钮**（原 015 并入）：sysadmin 可手动触发备份（不限于登录时自动备份），备份进行中显示进度指示，失败时显示错误原因
- [ ] 选择备份文件后执行恢复（`RESTORE DATABASE`）
- [ ] 恢复前弹框确认"将覆盖当前数据库，是否继续"
- [ ] 恢复完成后提示重启应用

**业务规则**:
1. LocalDB 备份路径：`%AppData%/LYBTZYZS/Backup/`（已实现）。
2. 远程 SQL Server 备份依赖 SQL Server Agent（应用层不控制，提供运维手册）。
3. 恢复操作仅 sysadmin 可执行。

**实现参考**: 现有 `ILocalDbBackupService`（备份）+ 新增恢复 UI

---

### US-SHELL-014: 安全审计日志查看

**角色**: sysadmin
**优先级**: Should
**状态**: 🔴 决策补回，待开发（🧲 v1.0 待实现，D3 决策）

**作为** sysadmin，**我想要** 查看登录/登出/密码变更/权限变更等安全事件日志，**以便** 追溯安全事件、满足医疗合规要求。

**验收标准**:
- [ ] 提供安全事件列表（分页，按时间倒序）
- [ ] 事件类型：登录成功/失败、登出、密码修改、用户创建/删除/禁用
- [ ] 每条记录含：时间、用户、操作类型、IP 地址、结果（成功/失败）
- [ ] 支持按事件类型/用户/时间范围筛选
- [ ] 保留 365 天（`SecurityOptions.AuditRetentionDays`）
- [ ] 审计日志仅追加，不可修改/删除

**业务规则**:
1. `SecurityAuditService` + `SecurityAuditLog` 表（v2.0 迁移已删除，需恢复）。
2. 覆盖：认证事件（US-LOG-004）、权限变更、操作审计。
3. 仅 sysadmin 可查看全局审计日志。

**实现参考**: 恢复 `SecurityAuditLog` 实体 + `SecurityAuditService` + SysadminHomeView 面板

---

### ~~US-SHELL-015: 备份状态与手动备份~~（已撤销，并入 US-SHELL-013）

> **撤销决策（2026-06-28）**：本 US 的「备份状态展示 + 手动备份按钮」已并入 US-SHELL-013 数据库备份恢复的验收标准。独立 US 撤销，总览计数不单列。

---

### US-SHELL-016: 配置导出/导入

**角色**: sysadmin
**优先级**: Could
**状态**: ❌ 未实现（🧲 v1.0 待实现，3 台客户端分发场景）

**作为** sysadmin，**我想要** 导出和导入系统配置，**以便** 重装后快速恢复配置、多机部署时统一配置。

**验收标准**:
- [ ] "导出配置"按钮：将 `appsettings.json` + `clinic-settings.json` 打包为 JSON 文件下载
- [ ] "导入配置"按钮：选择 JSON 文件 → 覆盖当前配置 → 提示重启生效
- [ ] 导入前校验文件格式，格式错误拒绝

**业务规则**:
1. 配置文件路径：`appsettings.json` + `clinic-settings.json`。
2. 导入后需要重启 Desktop 才生效（部分配置不支持热更新）。
3. 导入时保留当前 `Jwt:SecretKey`（不覆盖安全密钥）。

**实现参考**: SysadminHomeView 新增导入/导出按钮

---

### US-SHELL-017: 生产环境安全门控（SystemAdminOptions）

**角色**: sysadmin / 运维
**优先级**: Must
**状态**: ✅ 服务端已实现（v1.0，`SystemAdminOptions` + `DefaultPasswordService`）

**作为** 运维人员，**我想要** 生产环境的 sysadmin 创建受安全门控保护，**以便** 防止默认密码在生产环境裸奔。

**验收标准**:
- [x] `AutoCreateOnStartup` 控制是否启动时自动创建 sysadmin（默认 `true`）
- [x] `AllowAutoCreateInProduction` 默认 `false`——生产环境不自动创建 sysadmin（安全默认值）
- [x] `InitialSetupToken` —— 生产环境创建 sysadmin 需要一次性设置令牌（环境变量提供，不入库）
- [x] `DefaultPasswordService.GetOrGeneratePassword()` —— 生产环境自动生成随机密码（替代硬编码默认密码）
- [x] `DefaultPasswordService.ValidateSetupToken()` —— 加密常量时间比较，防时序攻击
- [x] `ForceChangeOnFirstLogin` —— `DefaultPasswordOptions` 控制首次登录是否强制改密

**业务规则**:
1. **开发环境**：`AutoCreateOnStartup=true` + `ForceResetOnStartup` 可选（开发时强制重置密码）。
2. **生产环境**：`AllowAutoCreateInProduction=false` + 需配置 `InitialSetupToken` 环境变量才能创建 sysadmin。
3. 生产环境默认密码由 `DefaultPasswordService.GetOrGeneratePassword()` 随机生成，不再硬编码。
4. `SessionTimeoutMinutes` 控制会话超时（默认 240 分钟）。
5. `SystemAdminOptions` 配置节 `appsettings.json → SystemAdmin`。

**实现参考**: `SystemAdminOptions.cs`（`Shared.Configuration`）、`DefaultPasswordService.cs`（`Infrastructure/Configuration`）、`DatabaseInitializationService.cs:99-113`

---

### US-SHELL-018: sysadmin 配置中心（SysadminHomeView）

**角色**: sysadmin
**优先级**: Must
**状态**: ❌ 未实现（🧲 v1.0 待实现）

**作为** sysadmin，**我想要** 在统一的配置面板中管理所有基础配置，**以便** 不需手动改 JSON 文件就能完成系统调整。

**验收标准**:
- [ ] SysadminHomeView 展示配置中心面板，分组显示所有可配置项
- [ ] 诊所信息（Name/Address/Phone/Department/LicenseNumber/Email）可编辑保存
- [ ] 会话设置（InactivityTimeoutMinutes/WarningBeforeTimeoutMinutes/ActivityCheckIntervalSeconds）可编辑
- [ ] 连接设置（API BaseUrl + 测试连通按钮 + TimeoutSeconds）可编辑
- [ ] 安全策略（ForceChangeOnFirstLogin / NewUserPassword）可编辑
- [ ] 功能开关（OverwriteConflicts / DuplicateHerbMergeStrategy）可切换，热更新即时生效
- [ ] 读卡器参数（UsbPort/ConnectTimeout/ReadTimeout）可编辑（自动检测优先）
- [ ] 系统信息（版本/DB状态/连接状态）只读展示
- [ ] 配置保存后：诊所信息/会话/连接/安全策略/读卡器需重启生效；功能开关热更新即时生效

**配置项清单**（7 组 13 项，代码扫描确认无遗漏）:

| 分组 | 配置项 | Options 类 | 生效方式 |
|------|--------|-----------|---------|
| 诊所信息 | Name/Address/Phone/Department/LicenseNumber/Email | `ClinicSettingsOptions` | 重启 |
| 会话设置 | InactivityTimeoutMinutes(1-120)/WarningBeforeTimeoutMinutes(0-10)/ActivityCheckIntervalSeconds(10-120) | `ClientSessionOptions` | 重启 |
| 连接设置 | BaseUrl + TimeoutSeconds(5-300) | `ApiClientOptions` | 重启 |
| 安全策略 | ForceChangeOnFirstLogin / NewUserPassword | `DefaultPasswordOptions` | 重启 |
| 功能开关 | OverwriteConflicts / DuplicateHerbMergeStrategy | `FeatureToggleOptions` | **热更新** |
| 读卡器管理 | 厂家选择/诊断测试/连接状态/手动参数覆盖 | `ICardReader` + `ICardReaderDiagnostics` | **测试模式 UI**（详见 US-SHELL-019） |
| 系统信息 | 版本/DB状态/连接状态 | `DiagnosticsController` | 只读 |

**业务规则**:
1. 配置保存写入 `appsettings.json` + `clinic-settings.json`。
2. `FeatureToggleOptions` 通过 `ConfigurationOptionsMonitor` 支持热更新，无需重启。
3. 其他配置修改需重启 Desktop 生效（v1.0 限制）。
4. 密码相关配置（SecretKey/JWT）不可在 UI 中修改。
5. 服务器端配置：远程模式通过服务端 Configuration API 管理（业务参数可改/敏感只读，见 [ADR-0014](../03-architecture/decisions/0014-sysadmin-config-dual-mode.md)）；本地模式无独立服务端（LocalWebAPI 内嵌，配置归「本地配置」面板）。

### 双模式面板（ADR-0014）

SysadminHomeView 按连接模式区分面板布局——配置对象在双模式下本质不同（远程管「服务端 + 客户端」两层，本地管「本地全栈」一层）：

| 模式 | 面板布局 | 数据源 |
|------|---------|--------|
| **远程** | ① 客户端配置（本机 Desktop，上方 7 组配置） ② 服务端配置（调服务端 Configuration API） | ① 客户端 appsettings ② 服务端 `GET /configuration`（脱敏） |
| **本地** | ① 本地配置（全栈：LocalWebAPI + LocalDB + Desktop） ② 备份恢复（[US-SHELL-013](#us-shell-013-数据库备份恢复含备份状态展示--手动备份)） | 客户端 appsettings（含 `OfflineMode`/`LocalApiBaseUrl`/本地 Jwt 等） |

**服务端配置面板（仅远程）**：展示 GET 全部节（敏感字段掩码）；业务参数行可编辑（`PUT /configuration/{section}`）；敏感行只读标记 🔒；改后提示「重启生效」+「应用并重启」按钮（`POST /configuration/restart`）。该面板依赖 ConfigurationController 扩展（PUT 业务参数白名单/敏感黑名单 403/GET 脱敏/POST 延迟重启），详见 [ADR-0014](../03-architecture/decisions/0014-sysadmin-config-dual-mode.md) 与 [配置 API](../04-api-reference/10-configuration.md)。

**本地配置面板**：客户端 7 组 + LocalWebAPI 特有（`OfflineMode.LocalApiBaseUrl` 5300、本地 Jwt 等，可改）+ 备份恢复入口。本地模式无独立服务端，所有配置直接读写本机 appsettings。

---

### US-SHELL-019: 读卡器诊断测试工具（sysadmin）

**角色**: sysadmin
**优先级**: Should
**状态**: 📋 已设计（🧲 v1.0 待实现，前台建档入口需自检）

**作为** sysadmin，**我想要** 通过官方 demo 测试功能验证读卡器是否正常工作，**以便** 快速定位硬件问题而不需运行外部测试软件。

**验收标准**:
- [ ] sysadmin 配置中心提供读卡器诊断面板
- [ ] 厂家选择：下拉选择已适配厂家（华大 HD100 等），测试时临时切换
- [ ] 设备探测：发送探测指令，检测设备是否在线，显示连接状态
- [ ] 读卡测试：读取一张样卡，显示解析结果（姓名/身份证号/性别/出生日期/住址）
- [ ] 串口测试：验证 USB 通信链路（发送/接收握手数据包）
- [ ] 固件版本：读取设备固件版本号
- [ ] 手动参数：USB 端口/连接超时/读取超时可手动覆盖（仅自动检测失败时使用）
- [ ] 测试通过后，厂家选择持久化到 config，后续自动加载
- [ ] 医生端完全无感——自动检测走 `ICardReaderFactory.AutoDetectReaderAsync`，匹配到已适配厂家直接使用

**业务规则**:
1. 读卡器管理分**测试模式**（sysadmin）和**使用模式**（医生）两层。
2. 测试模式：sysadmin 在配置中心操作，选择厂家、运行诊断、验证设备。
3. 使用模式：医生端启动时 `ICardReaderFactory.AutoDetectReaderAsync()` 自动匹配厂家，匹配到则静默使用；未匹配到则降级 `MockCardReader`，不阻塞启动。
4. 诊断测试集成各厂家的官方 demo 功能（华大 HD100 提供 USB 探测/读卡/固件查询等标准指令）。
5. 厂家扩展：新增读卡器型号时，在 sysadmin UI 中测试兼容性，无需改代码即可验证。

**架构影响**:
- 新增 `ICardReaderDiagnostics` 接口（厂家诊断能力）
- `ICardReader` 扩展 `GetDeviceInfo()` 方法
- sysadmin 配置中心增加读卡器诊断 tab

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 读卡器为本地硬件，与模式无关 |
| 本地 | 同上 |

**实现参考**: 现有 `ICardReader`/`ICardReaderFactory` + 新增 `ICardReaderDiagnostics` 接口

---

## 依赖

| 依赖 | 说明 |
|------|------|
| [02-auth.md](02-auth.md) | Shell 登录协调、审计日志事件来源 |
| [03-users.md](03-users.md) | 账户设置关联修改密码/个人资料 |
| [07-medical-cases.md](07-medical-cases.md) | MedicalCaseAuditLog 归属医案模块 |
| [09-printing.md](09-printing.md) | ClinicSettings 驱动打印标题区 |

---

## 变更记录

| 版本 | 日期 | 变更 | 原因 |
|------|------|------|------|
| v1.0 | 2026-06-28 | Split from 11-platform.md into focused module | 文档结构优化 S4 批次 3 |

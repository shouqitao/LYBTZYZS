# 用户画像 (Personas)

> 版本: v3.1 | 日期: 2026-06-20 | 状态: 代码验证

本文件定义凌隐宝堂中医诊所管理系统的四个核心角色。所有功能映射均经过代码验证。

---

## 角色一：Sysadmin（系统运维 — 独立用户）

> **身份：独立用户（非角色）** | 授权：系统运维 | 使用频率低

### 定位

**sysadmin 不是一个角色，而是一个独立的系统用户。** 安装时自动创建，不可删除，独立于角色体系。负责系统全生命周期管理：部署→初始化→日常运维→安全→备份恢复→升级。

### 核心职责

| 阶段 | 职责 | 实现状态 |
|------|------|---------|
| **部署** | Desktop 安装（Velopack）、WebAPI 服务器部署 | 📋 已设计 |
| **初始化** | 5 步向导：改密→诊所信息→模式选择→创建首个 admin→交权 | 📋 已设计 |
| **日常运维** | 系统健康监控、配置管理、日志级别/调试模式、用户管理支持 | ✅ API 齐备（Health/Diagnostics/Configuration） |
| **数据维护** | 数据库备份/恢复、备份状态查看、日志清理 | ⚠️ 备份有、恢复 UI 待开发 |
| **安全管理** | 安全审计日志查看、sysadmin 不可删/禁/改保护 | ⚠️ 审计日志待补回 |
| **升级** | Desktop 自动更新（Velopack）、WebAPI 手动升级 | 📋 已设计 |

### 配置中心（SysadminHomeView 配置面板）

**设计原则**：sysadmin 所有基础配置都有 UI 界面，不靠手动改 JSON。

| 分组 | 配置项 | 值域 | Options 类 | 实现状态 |
|------|--------|------|-----------|---------|
| **诊所信息** | 名称/地址/电话/科室/许可证号/邮箱 | 文本 | `ClinicSettingsOptions` | ✅ 向导写入，UI 待做 |
| **会话设置** | 不活动超时 | 1-120 分钟 | `ClientSessionOptions` | ✅ API 齐备，UI 待做 |
| | 超时前警告 | 0-10 分钟 | `ClientSessionOptions` | ✅ API 齐备，UI 待做 |
| | 活动检查间隔 | 10-120 秒 | `ClientSessionOptions` | ✅ API 齐备，UI 待做 |
| **连接设置** | API 地址 | URL | `ApiClientOptions` | ✅ 已有测试连通按钮 |
| | 请求超时 | 5-300 秒 | `ApiClientOptions` | ✅ API 齐备，UI 待做 |
| **安全策略** | 首登强制改密 | 开/关 | `DefaultPasswordOptions` | ✅ 已修复为 `true` |
| | 新用户默认密码 | 文本 | `DefaultPasswordOptions` | ⚠️ 需加 UI |
| **功能开关** | OverwriteConflicts | 开/关 | `FeatureToggleOptions` | ✅ 支持热更新 |
| | DuplicateHerbMergeStrategy | 下拉 | `FeatureToggleOptions` | ✅ 支持热更新 |
| **读卡器管理** | 厂家选择/诊断测试/连接状态/手动参数覆盖 | 多项 | `ICardReader`+`ICardReaderDiagnostics` | 📋 设计中 |
| | ↳ 测试模式（sysadmin） | 厂家下拉+设备探测/读卡测试/串口测试/固件版本 | 诊断接口 | 📋 设计中 |
| | ↳ 使用模式（医生） | 无感——自动检测+自动读卡，全程无 UI | `ICardReaderFactory` | ✅ 已实现 |
| **系统信息(只读)** | 版本/DB状态/连接状态 | 只读 | `DiagnosticsController` | ✅ API 齐备，UI 待做 |

**不纳入 UI 的配置**（服务器端/安全敏感）：`JwtOptions`(SecretKey)、`SystemAdminOptions`(部署配置)、`DatabaseOptions`(连接串)、`SecurityOptions`(速率限制)、`SessionOptions`(服务端会话)、`LoggingOptions`(日志清理)、`MemoryCacheOptions`(缓存)、`SwaggerOptions`(API文档)。

### 约束

- **不可删除**：`ApplicationUser.IsSysAdmin=true` 标记，Users 检查 `IsSysAdmin` 后拒绝删/禁/改
- **不参与常规权限层级**：绕过 `PermissionLevel` 检查，可管理任何用户
- **不可混淆**：sysadmin 是独立用户（`IsSysAdmin=true` + SuperAdmin 角色双机制并存）；admin 是业务管理员角色用户
- **只种子 sysadmin**（v1.0 设计）：系统启动仅自动创建 sysadmin，首个 admin 由 sysadmin 在初始化向导中手动创建
  - ⚠️ 代码待改：`IdentitySeedData.cs:26-27` 仍同时种子 admin（Phase② TODO）
- **首登强制改密**：`ForceChangeOnFirstLogin=true`，sysadmin 首次登录后必须修改默认密码
  - ✅ 已修：appsettings.json 值已改为 `true`，`EmbeddedLocalWebApiService` 从 config 读

### 认证方式

| 模式 | 登录方式 | 密码来源 | 说明 |
|------|---------|---------|------|
| 远程 | 用户名密码 | `appsettings.json:DefaultPasswords.SysAdminPassword` | 首次登录强制改密（`ForceChangeOnFirstLogin=true`） |
| 本地 | 用户名密码 | **同一份 config** | 远程/本地用相同凭证：`sysadmin/{DefaultPasswords.SysAdminPassword}` |

### 会话超时策略

| 类型 | 值 | 说明 |
|------|:---:|------|
| 不活动超时 | **30 分钟** | 无操作 30 分钟自动退出（原 5 分钟太短，医生看诊交谈时不碰电脑会被踢） |
| 绝对超时 | **禁用**（v1.0） | 240 分钟绝对超时不启用，小诊所场景无此需求 |

### v1.0 新增能力（设计中）

- **Desktop 自动更新**：Velopack 打包 + 启动时检查 + 用户自愿升级（安全更新强制）
- **首次初始化向导**：sysadmin 首登走 5 步向导，不完成不能用系统
- **数据恢复 UI**：从 LocalDB 备份恢复数据库
- **安全审计日志查看**：登录/登出/密码变更/权限变更记录
- **配置导出/导入**：sysadmin 可备份/还原系统配置

### 当前实现状态

| 项 | 状态 | 代码位置 |
|----|------|---------|
| `ApplicationUser.IsSysAdmin` 布尔字段 | ✅ | `Users/ApplicationUser.cs` |
| `CanManageUser` 使用 `IsSysAdmin` 判断 | ✅ | `UsersController.cs` |
| JWT Claims 包含 `IsSysAdmin=true` | ✅ | Auth 模块 |
| IdentitySeedData 种子 sysadmin | ✅ | `IdentitySeedData.cs:26`（但目前同时种子 admin，v1.0 改为只种子 sysadmin） |
| **`SystemAdminOptions`** | ✅ | `Shared.Configuration/Options/Server/SystemAdminOptions.cs` — 含 `AutoCreateOnStartup`(true)、`AllowAutoCreateInProduction`(false)、`InitialSetupToken`(生产环境令牌)、`SessionTimeoutMinutes`(240) |
| **`DefaultPasswordService`** | ✅ 服务端已实现 | `Infrastructure/Configuration/Services/DefaultPasswordService.cs` — 含 `GetOrGeneratePassword()`（生产环境随机密码）、`ValidateSetupToken()`（加密令牌验证）、`ShouldForcePasswordChange()` |
| `ForceChangeOnFirstLogin` | ⚠️ `DefaultPasswordOptions` 类默认 `true`，但 `appsettings.json` 覆盖为 `false`（v1.0 已改为 `true`） | `DefaultPasswordOptions.cs:36` |
| 默认密码硬编码 | ⚠️ `appsettings.json:9-11` 三套密码明文（v1.0 改为随机生成或 config-only） | `DefaultPasswords` 配置节 |
| `EmbeddedLocalWebApiService` 密码硬编码 | ⚠️ 已修复：改为从 `IConfiguration` 读取（不再硬编码） | `EmbeddedLocalWebApiService.cs:50-53` |
| 首次初始化向导 | ❌ 现有 `FirstRunSetupViewModel` 仅做连接配置，需扩展为 5 步 | `Auth/ViewModels/FirstRunSetupViewModel.cs` |

### 默认用户

| 用户 | 用户名 | 默认密码 | 角色 | IsSysAdmin | 可删除 |
|------|--------|---------|------|-----------|--------|
| 业务管理员 | `admin` | `Admin@123456` | Admin | **false** | ✅ |
| 系统运维 | `sysadmin` | `SysAdmin@2026!` | SuperAdmin | **true** | ❌ |

> ⚠️ admin 和 sysadmin 是**两个独立用户**，不可混淆。sysadmin 是信任根——创建第一个 admin，可重置 admin 密码。admin 只能由 sysadmin 创建。

---

## 角色二：Admin（业务管理员）

> **PermissionLevel = 10** | 授权策略：`DoctorOrReceptionist` + `AdminOrSuperAdmin`

### 定位

负责中医诊所业务管理的管理员。主要工作是维护业务基础数据、管理用户账号、管理药材/验方、数据维护。日均使用 1-2h。

### 代码实现

| 配置项 | 值 | 代码位置 |
|--------|-----|---------|
| `UserRole` | `Admin(10)` | `AdminRoleDefinition.cs:28` |
| `HomeViewName` | `ViewNames.AdminHome` | `AdminRoleDefinition.cs:36` |
| `RequiredModules` | UsersModule, PatientsModule, HerbsModule, FormulaModule, MedicalCaseModule | `AdminRoleDefinition.cs:17-22` |
| `CanManageUser` 逻辑 | Admin 可管理 Doctor/Receptionist，不可管理 Admin/SuperAdmin | `UsersController.cs:547-560` |
| Desktop 页面 | AdminHomeView + 7 个管理页面（SystemSettings/Herb/Formula/Patient/MedicalCase/UserManagement） | `AdminModule.cs` 注册 |

### 核心职责与实现状态

| 职责 | 覆盖模块 | US 达标率 | 实现状态 | 关键问题 |
|------|---------|:---:|------|------|
| **用户管理** | Users (12 US) | 8/12 ✅ 3⚠️ 1🔴 | ⚠️ 部分 | 分页筛选 bug（内存过滤导致 TotalCount 错误）、Restore 未实现、CreatedAt 始终 MinValue |
| **药材管理** | Herbs (13 US) | 3/13 ✅ 3⚠️ **7🔴** | 🔴 严重 | 删除无引用检查（破坏处方完整性）、Excel 导入导出**完全缺失**、批量操作不完整、权限策略错误（`DoctorOrAdmin` 应为 `DoctorOrReceptionist`） |
| **验方管理** | Formulas (13 US) | 7/13 ✅ 3⚠️ 3🔴 | ⚠️ 部分 | GetDetail **无所有权检查**（Admin 能读他人非共享验方→安全缺陷）、Export/Import 端点缺失、Restore 缺失 |
| **医案管理** | MedicalCases (18 US) | 8/18 ✅ 6⚠️ 4🔴 | ⚠️ 部分 | 审计日志缺失、打印保护缺失、历史聚合缺失。**设计决策**：admin 医案操作 = 状态变更（CaseStatus → Completed），**不编辑 Consultation/Prescription 内容**。**v1.0 医生对医案负责，Admin 不审核医案**（医案查询见 MC-005/006，不可创建/编辑/审核）；医生不可用时管理关闭解除 BR-001；变更追溯由 D1 审计日志（v1.0 补回）保障。代码已符合此边界 |
| **挂号监控** | Registration (7 US) | 2/7 ✅ 2⚠️ 3🔴 | 🔴 严重 | **权限策略错误**（类级 `DoctorOrAdmin` 挡住 Receptionist 核心职能）、StartVisit 链路断裂、跨模块直接引用 |

**汇总**：63 个 US 中 28 个达标（44%），**21 个完全未实现**。

### 权限矩阵（Admin 相关操作）

| 操作 | Receptionist | Doctor | Admin | SuperAdmin |
|------|:---:|:---:|:---:|:---:|
| 用户 CRUD | ✗ | ✗ | ✓（仅 Doctor/Receptionist） | ✓ 全部 |
| 重置密码 | ✗ | ✗ | ✓ | ✓ |
| 患者管理 | ✓ | ✓ | ✓ | ✓ |
| 药材查询 | ✗ | ✓ | ✓ | ✓ |
| 药材写操作 | ✗ | ✓*（仅自己创建） | ✓（全部） | ✓ |
| 验方查询 | ✗ | ✓*（仅自己+共享） | ✓（全部） | ✓ |
| 验方写操作 | ✗ | ✓*（仅自己创建） | ✓（全部） | ✓ |
| 医案创建 | ✗ | ✓ | ✗ | ✗ |
| 医案查看 | ✗ | ✓*（仅自己的） | ✓（全部） | ✓ |
| 医案完成/关闭 | ✗ | ✓*（仅自己的） | ✓（全部） | ✓ |
| 挂号创建 | ✓（前台） | ✓（QuickVisit） | ✗ | ✗ |
| 打印 | ✗ | ✓ | ✗ | ✗ |

### 需要修复的权限问题

| # | 问题 | 严重度 | 代码位置 |
|---|------|:---:|---------|
| 1 | Herbs Controller 用 `DoctorOrAdmin` 而非 `DoctorOrReceptionist` → Receptionist 无法查药材 | 🔴 | `HerbsController.cs` |
| 2 | Registration Controller 类级 `DoctorOrAdmin` → Receptionist 无法挂号/取消 | 🔴 | `RegistrationsController.cs` |
| 3 | Formulas `GetDetail` 无所有权检查 → Admin 可读他人非共享验方 | 🟠 | `FormulasService` |
| 4 | Patients 单删路径无引用检查（BR-DEL-001）→ 可删除被医案引用的患者 | 🔴 | `PatientsService` |
| 5 | Herbs 删除无引用检查 → 被处方引用的药材可静默软删 | 🔴 | `HerbsService` |

---

## 角色三：Doctor（中医医生）

> **PermissionLevel = 1** | 授权策略：`DoctorOrReceptionist`

### 定位

中医内科主治医师，系统核心业务操作者——**唯一能创建医案的角色**，承担从诊断到开方到打印的完整临床链路。日均使用 6-8h，触及全部 6 个业务模块。

> **双模式工作流（R10 spec S2/S3）**：
> - **远程模式**：从待诊队列选患者→StartVisit→看诊；急诊可用 QuickVisit 直接接诊
> - **本地模式**：**直接看诊**——「来一个看一个」，选/建患者(Patient)→直接开医案(MedicalCase)→看诊→打印，**无挂号环节**、无队列、无 SignalR（本质等同远程 QuickVisit）

### 代码实现

| 配置项 | 值 | 代码位置 |
|--------|-----|---------|
| `UserRole` | `Doctor(1)` | `DoctorRoleDefinition.cs:28` |
| `HomeViewName` | `ViewNames.ClinicalWorkspace` | `DoctorRoleDefinition.cs:37` |
| `RequiredModules` | UsersModule, PatientsModule, HerbsModule, FormulaModule, MedicalCaseModule, **RegistrationModule** | `DoctorRoleDefinition.cs:16-24` |
| 专属 UI | ClinicalWorkspaceView（患者列表+看诊工作区一体化） | `Roles/LYBT.Desktop.Clinical/Views/` |
| 医案创建权限 | **唯一**能创建医案（`DoctorOrAdmin` 策略，Admin 仅管理关闭） | `MedicalCasesController` |

### 核心职责与实现状态

| 职责 | 覆盖模块 | US 达标率 | 实现状态 | 关键问题 |
|------|---------|:---:|------|------|
| **临床诊疗（核心）** | MedicalCases (18 US) | 8/18 ✅ 6⚠️ 4🔴 | ⚠️ 部分 | 复诊历史聚合缺失（MC-008/009）、打印回写缺失、BR-001 DB索引漏Suspended、MC-LOCK时区错误、跨模块非事务 |
| **患者管理** | Patients (13 US) | 4/13 ✅ 4⚠️ 5🔴 | ⚠️ 部分 | 读卡去重第一环落空、引用检查缺失、Restore 缺失、权限策略 `DoctorOrAdmin` 需改 |
| **药材查询/有限写** | Herbs (13 US) | 3/13 ✅ 3⚠️ 7🔴 | ⚠️ 部分 | 删除无引用检查（破坏处方完整性）、批量操作不完整、Excel 缺失 |
| **验方管理** | Formulas (13 US) | 7/13 ✅ 3⚠️ 3🔴 | ⚠️ 部分 | GetDetail 无所有权检查、Export/Import 端点缺失、Restore 缺失 |
| **偶尔挂号** | Registration (7 US) | 2/7 ✅ 2⚠️ 3🔴 | ⚠️ 部分 | QuickVisit 死代码（Service 方法无调用方）；StartVisit 不创建医案 |
| **处方打印** | Printing (4 US) | 1/4 ✅ 2⚠️ 1🔴 | ⚠️ 部分 | 回写缺失（PrintLog 表已删）、PDF 分页逻辑未镜像 XAML 多页、IsDraft 字段未克隆 |

### 权限边界（医生独有 vs 与其他角色的区别）

| 能力 | Doctor | Admin | Sysadmin |
|------|--------|-------|---------|
| 医案创建 | **唯一** | 管理关闭（仅状态） | 不参与 |
| 医案编辑 | **仅自己的** | 查看全部（不编辑内容） | 不参与 |
| 药材/验方写操作 | **仅自己创建** | 全部 | 不参与 |
| 打印处方 | **唯一** | 不能 | 不能 |
| 历史查看 | 期望看**自己所有医案历史** | 看所有人（但聚合也缺失） | 不参与 |
| 挂号 | QuickVisit（仅自己） | 不参与 | 不参与 |

### 医生视角的 P0 缺陷

| 缺陷 | 影响 | PRD 量化目标关联 |
|------|------|----------------|
| **MC-008/009 历史聚合缺失** | 复诊时看不到既往诊断/处方，需手动翻医案 | vision："25min→10min" 的数据基础缺失 |
| **BR-001 DB 索引漏 Suspended** | 并发下可为同一患者创建两个 Suspended 医案，违反核心铁律 | 数据一致性破坏 |
| **打印回写缺失** | 处方打印后无追溯记录 | 合规要求 |
| **MC-LOCK 时区用 UtcNow** | 北京时间 8am 前误判医案锁定 | 用户体验 bug |

### 设计决策（与 Sysadmin/Admin 协同）

- **医案归属**：Doctor 仅操作自己的医案（`UserId=自己`归属限制）
- **唯一创建者**：仅 Doctor 能创建医案（`DoctorOrAdmin` 策略限 MC-001）
- **打印控制**：仅 Doctor 能打印处方（`DoctorOnly` 策略）
- **验方归属**：Doctor 只能编辑自己创建的验方（`CreatedBy` 归属检查）
- **QuickVisit**：Doctor 在前台繁忙时可替代挂号（RegistrationModule 在 RequiredModules 中）

---

## 角色四：Receptionist（前台）

> **PermissionLevel = 0** | 授权策略：`DoctorOrReceptionist`

### 定位

前台接待人员，专注患者挂号和相关信息维护。日均使用 4-6h，是诊所每天第一个开机、最后关机的角色。

> **双模式适用性（R10 spec S2/S3）**：前台挂号职能**仅远程模式存在**。本地模式无前台——医生独立「来一个看一个」，直接 Patient→MedicalCase，Registration 模块不激活。故 Receptionist 角色的挂号/队列职责在本地模式下无适用场景。

### 代码实现

| 配置项 | 值 | 代码位置 |
|--------|-----|---------|
| `UserRole` | `Receptionist(0)` | `ReceptionistRoleDefinition.cs:27` |
| `HomeViewName` | `ViewNames.ReceptionistHome` | `ReceptionistRoleDefinition.cs:34` |
| `RequiredModules` | **仅 3 个**：UsersModule, PatientsModule, RegistrationModule | `ReceptionistRoleDefinition.cs:18-21` |
| 专属 UI | ReceptionistHomeView（挂号队列+快捷操作） | `Roles/LYBT.Desktop.Receptionist/Views/` |

### ⚠️ 致命问题：前台角色在当前代码中基本不可用

**三个核心模块全部被 `DoctorOrAdmin` 权限策略阻断**，前台无法执行任何本职工作：

| 模块 | PRD 要求策略 | 代码实际策略 | 后果 |
|------|------------|------------|------|
| Registration（挂号） | Receptionist 是主体 | **`DoctorOrAdmin`** | 🔴 无法创建/取消挂号 |
| Patients（患者管理） | `DoctorOrReceptionist`（PRD 已标 TODO） | **`DoctorOrAdmin`** | 🔴 无法管理患者 |
| Herbs（药材查询） | `DoctorOrReceptionist` | **`DoctorOrAdmin`** | 🔴 无法查药材 |

**根因**：Registration 和 Patients 的 Controller 类级属性用 `DoctorOrAdmin` 而非 `DoctorOrReceptionist`。Herbs 同理。这是系统最严重的权限架构问题——**前台角色在服务端设计层面被完全排除**。v1.0 首要修复项。

### 核心职责与实现状态

| 职责 | 覆盖模块 | US 达标率 | 实现状态 | 关键问题 |
|------|---------|:---:|------|------|
| **挂号管理** | Registration (7 US) | 2/7 ✅ 2⚠️ **3🔴** | 🔴 严重 | 权限阻断前台（001/006）、StartVisit 未创建医案+返回错 ID（005）、QuickVisit 死代码（002） |
| **患者管理** | Patients (13 US) | 4/13 ✅ 4⚠️ 5🔴 | 🔴 严重 | 权限阻断前台、Restore 缺失、引用检查缺失（单删/批量）、读卡去重数据层缺陷、BR-DEL-001 不一致 |
| **读卡登记** | CardReader (2 US) | 2/2 ✅ | ✅ 达标 | 读卡+患者去重查找已实现；但 Patients 的 IdNumber 搜索数据层丢弃导致去重第一环落空 |

### 权限矩阵（前台相关操作）

| 操作 | Receptionist | Doctor | Admin | SuperAdmin |
|------|:---:|:---:|:---:|:---:|
| 创建挂号 | ✅（应可，但**代码阻断**） | ✓ QuickVisit | ✗ | ✗ |
| 取消挂号 | ✅（应可，但**代码阻断**） | ✗ | ✗ | ✗ |
| 患者 CRUD | ✅（应可，但**代码阻断**） | ✓ | ✓ | ✓ |
| 药材查询 | ✅（应可，但**代码阻断**） | ✓ | ✓ | ✓ |
| 患者删除 | ✗ | ✓ | ✓ | ✓ |
| 患者启用/禁用 | ✗ | ✗ | ✓ | ✓ |

> 上表标注"代码阻断"的操作，PRD 设计上允许但代码权限策略未放行。

### 需要修复的权限问题（共 3 个，均 P0）

| # | 问题 | 代码位置 | 修复方案 |
|---|------|---------|---------|
| 1 | Registration 类级 `DoctorOrAdmin` | `RegistrationsController.cs` | 改为 `DoctorOrReceptionist`，QuickVisit/Cancel 按操作细分 |
| 2 | Patients 类级 `DoctorOrAdmin` | `PatientsController.cs:23` | 改为 `DoctorOrReceptionist`（PRD 注释已标 T5-P2-30 TODO） |
| 3 | Herbs Controller `DoctorOrAdmin` | `HerbsController.cs` | 改为 `DoctorOrReceptionist`（同 Admin 的权限问题） |

### 与 sysadmin 交叉点

- Sysadmin 创建首个 admin 后，admin 创建 Receptionist 账号
- Sysadmin 可重置 Receptionist 密码（通过用户管理 UI）
- Sysadmin 的读卡器诊断测试 → Receptionist 日常无感使用读卡
- Sysadmin 不参与挂号流程

---

## 四角色交叉对比（2026-06-28 审计 + 二轮设计验证）

### 功能闭环检查：首诊旅程

| 步骤 | 角色 | 状态 | 说明 |
|------|------|:---:|------|
| 部署系统 | Sysadmin | 📋 | 初始化向导待开发 |
| 创建 admin | Sysadmin | 📋 | 依赖向导 |
| admin 创建医生/前台 | Admin | ⚠️ | Users 分页 bug |
| 前台读卡 | Receptionist | ✅ | |
| **前台创建挂号** | Receptionist | 🔴 | **DoctorOrAdmin 阻断**（P0 修复） |
| **医生开始就诊** | Doctor | 🔴 | **StartVisit 不创建医案**（P0） |
| 医生创建医案 | Doctor | ⚠️ | 能建但策略允许 Admin |
| 诊断+开方 | Doctor | ⚠️ | 能做但非事务 |
| 打印 | Doctor | ⚠️ | 能打印但回写缺失 |

### 跨角色决策汇总

| # | 决策 | 状态 |
|:---:|------|:---:|
| 1 | **权限修复**：Registration/Patients/Herbs + LocalWebAPI Patients 的 `[Authorize]` 改为 `DoctorOrReceptionist` | 📋 Phase② |
| 2 | **审计日志 + 打印回写** | ✅ **v1.0 必做**（医疗合规） |
| 3 | **历史医案查询（MC-008/009）**：搜索 + 导出处方到当前 | ✅ **v1.0 Must** |
| 4 | **本地模式角色检查**：LocalWebAPI 统一 `DoctorOrReceptionist` | 📋 Phase② |
| 5 | **知情同意**：医案完成时增加"患者已知情同意"勾选（方案 A） | 📋 Phase② |
| 6 | **多医生协作**：BR-001 已覆盖 | ✅ 无需额外功能 |
| 7 | **数据迁移**：`MigrateAsync()` 自动处理非破坏性迁移 | ✅ 无需额外开发 |
| 8 | **权限模型统一**：RBAC + Permission 枚举（~35 项，Phase② 落地） | 📋 Phase② |
| 9 | **数据一致性**：v1.0 不需新增设计（切换主动行为 / 并发乐观锁 / 本地生命周期 v2.0） | ✅ 无需设计变更 |

### 无人区场景处理

| 场景 | 决策 | 说明 |
|------|------|------|
| 版本升级数据迁移 | 自动 + 手动 | 非破坏性→`MigrateAsync()`；破坏性→sysadmin 手动+文档 |
| 医疗纠纷追溯 | v1.0 审计功能全部补回 | SecurityAuditLog 恢复 + 关键操作审计 + Sysadmin 查看 UI |
| 处方打印审计 | v1.0 打印回写补回 | 恢复 PrintLog 字段/实体 |
| 患者知情同意 | 方案 A（勾选） | 医案完成时确认勾选 + 打印处方单为知情同意载体 |
| 多医生协作 | BR-001 已覆盖 | 无需额外功能 |

---

### 二轮设计验证（2026-06-28）

#### 角色交接闭环验证

8 个角色间交接点全部验证，**0 个完全闭环**：

| 交接 | 上游→下游 | 状态 | 关键缺口 |
|:---:|---------|:---:|---------|
| 1 | Sysadmin→Admin | 📋 | 向导未实现 |
| 2 | Admin→Doctor/Receptionist | ⚠️ | **药材 Excel 导入缺失**（阻断 Admin 初始化药材库→Doctor 无药可开方） |
| 3 | Receptionist→Doctor | 🔴 | 双重断裂：权限阻断 + StartVisit 不创建医案 |
| 4 | Doctor→医案系统 | ⚠️ | 历史聚合缺失 + 打印回写缺失 |
| 5 | Admin→医案状态维护 | ⚠️ | 审计日志缺失（v1.0 补回已定） |
| 6 | Doctor→患者 | ⚠️ | 打印回写缺失（v1.0 补回已定） |
| 7 | 任何→Sysadmin | ✅ | — |
| 8 | 系统→Sysadmin | ⚠️ | 审计日志缺失 |

#### 统一权限模型（2026-06-28 确定）

采用 **RBAC + Permission 枚举**方案（~35 项原子操作），替代当前分散在 `CanManageUser`/API 策略/Service 层的碎片化权限逻辑。每个角色在 `RoleDefinition` 中声明 `RequiredPermissions` 集合。Phase② 落地。

#### 数据一致性设计（2026-06-28 确定）

| 场景 | 结论 | 理由 |
|------|------|------|
| 模式切换告知 | v1.0 不需新增 | 切换是用户主动行为，状态栏已有模式标识 |
| 并发操作保护 | v1.0 不需设计变更 | 乐观并发（RowVersion）设计已正确；Phase② 扩展重试到所有写操作 |
| 本地数据生命周期 | v1.0 不处理 | v2.0 同步处理；本地数据随 Desktop 存在，卸载删除 |

---

## 架构层差异汇总

| 维度 | Sysadmin | Admin | Doctor | Receptionist |
|------|----------|-------|--------|-------------|
| **身份类型** | **独立用户** | 角色 | 角色 | 角色 |
| **模块数** | 5（全量） | 5 | **6** | **3** |
| **包含 RegistrationModule** | ❌ | ❌ | **✅** | **✅** |
| **包含 HerbsModule** | ✅ | ✅ | ✅ | ❌ |
| **包含 FormulaModule** | ✅ | ✅ | ✅ | ❌ |
| **包含 MedicalCaseModule** | ✅ | ✅ | ✅ | ❌ |
| **首页视图** | AdminHome | AdminHome | **ClinicalWorkspace** | ReceptionistHome |
| **授权策略** | 跳过角色检查 | DoctorOrReceptionist + AdminOrSuperAdmin | 仅 DoctorOrReceptionist | 仅 DoctorOrReceptionist |
| **CanManageUser** | 跳过 | 可管理 Doctor/Receptionist | 不可管理 | 不可管理 |
| **可删除** | ❌ | ✅ | ✅ | ✅ |
| **可禁用** | ❌ | ✅ | ✅ | ✅ |

---

## 变更日志

| 日期 | 变更 | 原因 |
|------|------|------|
| 2026-06-28 | Doctor/Receptionist 角色补双模式工作流注（本地模式直接看诊/无前台） | R10 spec S8 文档更新 |
| 2026-06-28 | 四角色全面重写 + 交叉对比 + 二轮设计验证 | 代码审计 + 设计决策 + 功能闭环 + 权限模型 + 数据一致性 |
| 2026-06-28 | 二轮验证：交接闭环(8点) + RBAC权限枚举 + 数据一致性策略 | 协作闭环验证 + 设计合理性 |
| 2026-06-28 | 一轮决策：权限修复/审计补回/知情同意方案A/历史查询v1.0 Must | 交叉对比发现 |
| 2026-06-20 | v3.2 Sysadmin 改为独立用户设计 | 行业标准：sysadmin 是用户而非角色 |
| 2026-06-20 | v3.1 增加代码实现列 | 用户要求结合代码验证角色定位 |
| 2026-06-20 | v3.0 重构角色定义 | 明确各角色定位 |
| 2026-06-15 | v2.0 重建 | Phase 1 简化后重建 |

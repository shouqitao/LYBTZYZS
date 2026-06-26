# LYBTZYZS — 凌隐宝堂中医诊所管理系统

**.NET 8** | WPF/Prism | ASP.NET Core | EF Core | SQL Server (Remote + LocalDB dual-mode)

---

## ⚠️ MANDATORY: 使用 MCP 工具和 Skills

**编码前必须读取此节。** 本项目配备了专业的代码探索和操作工具，必须优先使用。

### 为什么必须使用？

1. **CodeGraph** 理解代码关系比 Grep/Read 快 10 倍
2. **Serena** 重构比手动编辑安全 100 倍（LSP 保证不破坏引用）
3. **context7** 获取最新框架文档，避免猜测 API
4. **Sequential Thinking** 分布式思考，复杂问题分解和推理
5. **Skills** 提供标准化工作流，避免遗漏关键步骤

### 禁止行为

| ❌ 禁止 | ✅ 应该 |
|---------|---------|
| 直接用 Grep 搜索代码 | 用 `codegraph_explore` 理解流程 |
| 手动重命名变量 | 用 `serena_rename_symbol` |
| 猜测框架 API | 用 `context7_query-docs` 查文档 |
| 跳过 brainstorm 直接编码 | 先用 `compose:brainstorm` |
| 跳过 verify 声称完成 | 必须有 `dotnet build` 证据 |
| **设计方案前不读代码** | **先用 CodeGraph/Serena 阅读现有实现，再做设计** |

---

## MCP 工具决策树

收到任务后，**立即**按此决策树选择工具：

```
收到任务
  │
  ├── 第一步：必须激活 Serena（如果尚未激活）
  │     serena_activate_project(project="LYBTZYZS")
  │
  ├── 理解/探索代码？ → CodeGraph（首选，不用 Read/Grep）
  │     ├── codegraph_explore  "这段代码怎么工作的"
  │     ├── codegraph_node     "读这个文件/符号的源码"
  │     ├── codegraph_search   "这个符号在哪定义"
  │     └── codegraph_callers  "谁调用了这个方法"
  │
  ├── 重构/重命名/编辑符号？ → Serena
  │     ├── serena_find_symbol              LSP 精确定位
  │     ├── serena_rename_symbol            安全全代码库重命名
  │     ├── serena_find_referencing_symbols 查找所有引用
  │     ├── serena_get_diagnostics_for_file 实时编译诊断
  │     └── serena_replace_symbol_body      替换方法体
  │
  ├── 复杂问题需要分解推理？ → Sequential Thinking
  │     └── sequentialthinking_sequentialthinking  分步思考，支持分支和修正
  │
  ├── 查框架文档？ → context7
  │     ├── context7_resolve-library-id     "查 Prism/Refit/EF Core"
  │     └── context7_query-docs             获取最新 API 文档
  │
  └── 以上都不适用？ → Read/Write/Edit/Glob/Grep 兜底
```

### CodeGraph vs Serena 速查表

| 场景 | 用 CodeGraph | 用 Serena | 用 Sequential Thinking |
|------|-------------|----------|----------------------|
| 快速理解代码流程 | ✅ `codegraph_explore` |  |  |
| 读取文件源码 | ✅ `codegraph_node` | ✅ `serena_read_file` |  |
| 安全重命名 | | ✅ `serena_rename_symbol` |  |
| 查找引用 | ✅ `codegraph_callers` | ✅ `serena_find_referencing_symbols` |  |
| 编译诊断 | | ✅ `serena_get_diagnostics_for_file` |  |
| 替换方法体 | | ✅ `serena_replace_symbol_body` |  |
| 复杂问题分解 | | | ✅ `sequentialthinking` |
| 多步骤推理 | | | ✅ `sequentialthinking` |

**原则**: 探索用 CodeGraph（快、全），操作用 Serena（精、安全）。

**设计方案前必须先读代码** — 用 CodeGraph/Serena 阅读现有实现，了解已有功能和模式，避免重复造轮子。

### context7 使用示例

```
# 查 Prism 模块注册
context7_resolve-library-id → "Prism" → /prismlibrary/prism
context7_query-docs → "How to register modules in Prism WPF"

# 查 Refit 接口定义
context7_resolve-library-id → "Refit" → /reactiveui/refit
context7_query-docs → "How to define Refit interface with custom headers"
```

### Sequential Thinking 使用场景

```
# 复杂架构决策
"这个功能应该放在哪个模块？考虑依赖关系、测试难度、维护成本"

# 多步骤推理
"从登录流程追踪到权限验证，再到数据访问的完整链路"

# 问题分解
"这个 Bug 可能的原因有哪些？按可能性排序，逐个验证"

# 方案对比
"方案 A 和方案 B 的优劣分析，考虑扩展性、性能、开发成本"
```

---

## Skills 工作流决策树

**编码前必须触发对应 Skill。** 不同场景使用不同工作流：

```
收到请求
  │
  ├── 新功能/新设计？
  │     ├── "值得做吗"/方向不明确 → office-hours
  │     ├── 明确要做 → 先读代码 → compose:brainstorm → compose:plan → compose:execute
  │     └── 多模块大改 → 先读代码 → compose:brainstorm → compose:plan → plan-eng-review → compose:subagent
  │
  ├── Bug/异常/测试失败？ → compose:debug（四阶段：调查→分析→假设→实现）
  │
  ├── 声称"完成了"？ → compose:verify（必须有 dotnet build 通过证据）
  │
  ├── 代码审查？ → compose:review（dispatch 子代理审查 diff）
  │
  ├── 架构/代码理解？ → understand（生成知识图谱）/ understand-chat（问答）
  │
  ├── 安全审计？ → cso（OWASP + 依赖扫描 + STRIDE）
  │
  ├── 写 PRD/需求文档？ → create-prd
  │
  ├── UI/UX 设计？ → ui-ux-pro-max（WPF 注意：只适用颜色/字体/UX 原则）
  │
  ├── 写计划文档？ → compose:plan（代码计划） / writing-plans（通用计划）
  │
  └── 小修补（1-2 行）？ → 直接改 → dotnet build → 提交（不需要技能）
```

### 本项目推荐技能组合

| 场景 | 推荐流程 | 预计耗时 |
|------|---------|---------|
| **登录/认证 bug** | `compose:debug` → 查日志 → 定位 → 修 | 15-30 min |
| **新增 API 端点** | `compose:plan` → `compose:execute` → `compose:review` | 30-60 min |
| **新增 WPF 页面** | `compose:brainstorm` → `compose:plan` → `compose:subagent` → `compose:review` | 1-2 hours |
| **数据库 schema 变更** | `compose:plan` → EF Migration → `compose:verify` → `compose:review` | 30-60 min |
| **安全漏洞排查** | `cso` → 修复 → `compose:review` | 1-2 hours |
| **理解陌生模块** | `codegraph_explore` → `understand-chat` | 10-20 min |
| **重命名公共 API** | `serena_rename_symbol` → `dotnet build` → `compose:verify` | 10-15 min |
| **查框架用法** | `context7_query-docs` → 实现 | 5-10 min |
| **复杂架构决策** | `sequentialthinking` → 分析 → 方案对比 → 实施 | 15-30 min |

---

## Compose Workflow (开发工作流)

### 标准流程

```
brainstorm → plan → execute → review → report → merge
```

### 工程纪律（编码前 MUST 触发）

| 场景 | 技能 | 规则 |
|------|------|------|
| 新功能/修改行为 | `compose:brainstorm` | 探索意图后再编码 |
| 有规格的多步骤任务 | `compose:plan` | 碰代码前出计划 |
| Bug/测试失败/异常 | `compose:debug` | 找到根因再修，禁止跳过到修复 |
| 实现功能/修复 | `compose:tdd` | 先写测试再写实现 |
| 声称"完成/修好" | `compose:verify` | 必须有 `dotnet build` 通过的证据 |
| **执行 Plan** | `compose:execute` / `compose:subagent` | **必须经用户确认后才能执行** |

### 工作流分级

| 规模 | 必须流程 |
|------|---------|
| **小修补** (typo、1-2 行) | 改完 → `dotnet build` → 提交 |
| **新功能/明确重构** | `brainstorm` → `plan` → TDD 实现 → `verify` → `review` → 提交 |
| **跨模块大改/新架构** | `brainstorm` → `plan` → `plan-eng-review` → `subagent` 并行 → `verify` → `review` → `merge` |

### 核心原则

- **前期思考 > 后期 debug** — 20% 的方案评审决定 80% 的结果
- **按需裁剪** — 小修小补跳过完整流程，不搞流程内耗
- **不验证不声称完成** — "应该修好了"不算完成，必须有 build 输出作为证据
- **找不到根因不修 bug** — `compose:debug` 四阶段：调查 → 分析 → 假设 → 实现
- **Subagent 优先** — 有 subagent 支持时用 `compose:subagent` 替代 `compose:execute`
- **设计方案前先读代码** — 用 CodeGraph/Serena 阅读现有实现，避免重复造轮子

---

## Build & Test

```bash
dotnet build LYBTZYZS.sln

dotnet test tests/LYBT.Tests.Server/        # Integration (real SQL Server + Respawn)
dotnet test tests/LYBT.Tests.Desktop/       # Desktop (LocalDB, AuthControllerTests 8/8 pass)
dotnet test tests/LYBT.Tests.Architecture/  # Architecture guards
```

## Git & Remote

- **Remote**: Gitee (`https://gitee.com/shouqitao/LYBTZYZS.git`) — NOT GitHub
- **Branch**: `master`
- **Commit convention**: `feat(模块): 描述` / `fix(模块): 描述` / `docs:` / `refactor:` / `test:`
- **Commit message language**: English preferred (PowerShell encoding issues with Chinese)

## Database

- **EF Core Migration**: Latest is `AddIsSysAdmin` — run `dotnet ef database update` after pulling
- **Dual-mode**: Remote = SQL Server (`LYBTDB_Dev`) | Local = LocalDB (`LYBTDesktop`) — NOT SQLite
- **Migration command**: `dotnet ef migrations add <Name> --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI`
- **EnsureCreatedAsync bypasses migrations**: `DatabaseInitializationService` uses `EnsureCreatedAsync`, not `Migrate()`. Fresh DB works but `__EFMigrationsHistory` stays empty.
- **Reset DB**: `sqlcmd -S "localhost" -Q "DROP DATABASE IF EXISTS LYBTDB_Dev; CREATE DATABASE LYBTDB_Dev"` then restart WebAPI
- **Reset LocalDB**: `sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "DROP DATABASE IF EXISTS LYBTDesktop"` then restart Desktop app

## Architecture

- **3-Layer**: Controller → Service → Repository → DbContext
- **MVVM**: View (XAML) ← binding → ViewModel → Repository → API
- **DDD**: MedicalCase is the sole aggregate root (Consultation + Prescription are internal entities)
- **Dual-Mode**: Remote (SQL Server port 5000) + Local (LocalDB port 5100), URL-based switching via `BaseUrlDelegatingHandler`
- **Modular**: Server modules (`LYBT.Module.*`), Desktop modules (`LYBT.Desktop.*`), role workspaces (Admin/Clinical/Receptionist/Sysadmin)

### Sysadmin Architecture (2026-06-20)

- **sysadmin = standalone user, NOT a role**: `ApplicationUser.IsSysAdmin = true`
- **admin ≠ sysadmin**: Two independent users. sysadmin creates admin, can reset admin passwords.
- **Default users**: `sysadmin` / `SysAdmin@2026!` (IsSysAdmin=true) | `admin` / `Admin@123456` (IsSysAdmin=false)
- **Passwords from config**: `DefaultPasswordOptions` (`appsettings.json` → `DefaultPasswords` section)
- **sysadmin has dedicated UI**: `SysadminModule` — dark dashboard + admin user management + log level control
- **SuperAdminRoleDefinition**: Maps `UserRole.SuperAdmin` → `SysadminHomeView` + loads `SysadminModule`

## Terminology

| Term | Meaning | Not |
|------|---------|-----|
| Consultation | 中医诊断 (TCM diagnosis) | "问诊" or "就诊" |
| MedicalCase | 医案 (medical case) | "病历" |
| Formula | 验方/经验方 (empirical recipe) | "公式" |
| HerbRole | 药材角色（君臣佐使） | — |
| Sysadmin | 系统运维（独立用户） | "超级管理员角色" |

## CODE STYLE

- **语言**: 中文用于业务文档和注释；英文用于技术标识符和 commit message
- **命名**: `PascalCase`（公共成员）、`_camelCase`（私有字段）、`I PascalCase`（接口）
- **包版本**: 统一在 `Directory.Packages.props` 声明，`.csproj` 不带版本号
- **无注释**: 除非用户要求，不添加代码注释
- **无 Emoji**: 代码中不使用 Emoji
- **跨模块禁止**: Server/Desktop 模块间禁止直接引用
- **详细规范**: `.editorconfig`

## WHERE TO LOOK

| Task | Location |
|------|----------|
| WebAPI entry | `src/Server/Services/LYBT.WebAPI/Program.cs` |
| Desktop entry | `src/Client/Desktop/Shell/App.xaml.cs` |
| DbContext | `src/Server/Core/LYBT.Infrastructure/Data/AppDbContext.cs` |
| Entities | `src/Server/Core/LYBT.Entities/` |
| DTOs/Contracts | `src/Shared/LYBT.Shared.Models/Contracts/` |
| Server Controllers | `src/Server/Services/LYBT.WebAPI/Controllers/` |
| Server Modules | `src/Server/Modules/LYBT.Module.*/` |
| Desktop Modules | `src/Client/Desktop/Modules/LYBT.Desktop.*/` |
| Desktop Core | `src/Client/Desktop/Core/` (Contracts, Foundation, Infrastructure, Controls, Shared, LocalData, Printing, CardReader) |
| Desktop Roles | `src/Client/Desktop/Roles/` (Admin, Clinical, Receptionist, **Sysadmin**) |
| Shared Exception | `src/Shared/LYBT.Shared.ExceptionHandling/` |
| Sysadmin Console | `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/` |
| Seed Data | `src/Server/Modules/LYBT.Module.Users/Services/IdentitySeedData.cs` |
| Connection Config | `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Services/ConnectionSettingsService.cs` |
| HTTP Registration | `src/Client/Desktop/Shell/Extensions/HttpServiceRegistrationExtensions.cs` |
| Role Definitions | `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Roles/Definitions/` |
| Docs | `docs/{01-product,02-requirements,03-architecture,04-api-reference,05-development,06-operations}/` |

## Key Patterns

- **Two-phase Serilog bootstrap** (WebAPI + Desktop)
- **Role-based module loading** (Desktop loads modules by user role via `SuperAdminRoleDefinition.RequiredModules`)
- **CQRS for MedicalCase** (CommandHandler pattern, not traditional 3-layer)
- **Testing Trophy** (Integration-first, zero mock for Server tests)
- **Soft-delete + global query filter** on most entities
- **BaseUrlDelegatingHandler**: Thread-safe per-request URL rewriting based on connection mode
- **SwitchingApiClient** routes localhost:5100 → embedded LocalWebAPI, otherwise → Refit remote API
- **BaseApiController** provides `Success()`, `HandleResult()`, `GetOperator()`, `ValidatePagination()`
- **ApiResponse<T>** envelope used consistently — ALL controllers must wrap responses
- **RegistrationQueueNumber** auto-generated daily (max+1), **RegistrationFee** input by receptionist
- **HerbRole** (君臣佐使) on PrescriptionItem for auto-sorting in prescriptions

### Desktop WPF Patterns

- **Prism Region-based navigation** via `NavigationCoordinator` + `IRegionManager`
- **MasterDetailControlBase** pattern for entity CRUD (Patients/Herbs/Formulas/Users)
- **CommunityToolkit.Mvvm** `[ObservableProperty]` / `[RelayCommand]` — NOT Prism's `BindableBase` / `DelegateCommand`
- **Thin Wrapper views** — Role views embed business module controls as thin XAML wrappers
- **Riok.Mapperly** for compile-time mapping — `.csproj` must NOT include AutoMapper unless fallback needed
- **ViewModelLocator.AutoWireViewModel**: VM 必须在 Module.RegisterTypes 中 `containerRegistry.Register<T>()` 注册

### Desktop Core DAG

```
Contracts ← Foundation ← Infrastructure ← Controls
Shared (standalone) — referenced by Contracts + Controls
Navigation (new) — references Contracts + Foundation + Infrastructure
Modules reference Infrastructure + Controls + Navigation
Roles reference Infrastructure + Controls + Navigation + Modules
```

### Desktop 项目清单 (post-refactor 2026-06-21)

| 项目 | 职责 |
|------|------|
| `LYBT.Desktop.Infrastructure` | VM 基类 + WPF 服务 + 角色定义 + HTTP + 行为 |
| `LYBT.Desktop.Navigation` | **新**：NavigationCoordinator + 导航模型 |
| `LYBT.Desktop.Controls` | WPF 控件 + 转换器 + **主题样式**（Phase 1 从 Infrastructure 迁入） |
| `LYBT.Desktop.Contracts` | 纯接口（Phase 4 清理后无 DTO/Enum 泄漏） |
| `LYBT.Desktop.Foundation` | HTTP 客户端 + 安全/JWT + 缓存 |
| `LYBT.Desktop.Shared` | 纯 DTO/Enum（Phase 4 吸收了 Contracts 迁出的类型） |
| `LYBT.Desktop.LocalData` | SQL Server LocalDB |
| `LYBT.Desktop.Printing` | QuestPDF 打印 |
| `LYBT.Desktop.CardReader` | 读卡器 |

## Common Pitfalls (CRITICAL — read before coding)

### Identity & Auth

- **Scoped service resolution**: `RoleManager`/`UserManager` are SCOPED. NEVER resolve from root provider. Always `using var scope = app.Services.CreateScope()`.
- **IdentitySeedData password reset**: Only reset when `user.LastLoginAt == null`. Otherwise admin password changes get wiped on restart.
- **DatabaseInitializationService does NOT create users**: User creation is delegated to `IdentitySeedData` via `UserManager.CreateAsync`. DatabaseInitializationService only runs migrations + status resets.
- **PasswordHelper (BCrypt) vs Identity (PBKDF2)**: These are INCOMPATIBLE. `SignInManager.CheckPasswordSignInAsync` fails on BCrypt hashes. All password hashing must go through `UserManager`.
- **AddIdentity ordering**: `AddIdentity()` must run BEFORE JWT `AddAuthentication()` — otherwise cookie scheme overrides JWT.

### LocalWebAPI (Embedded Server)

- **Route prefix MUST be `api/v1/[controller]`**: Desktop Refit client uses `/api/v1/` prefix. All 11 LocalWebAPI controllers must use `[Route("api/v1/[controller]")]`.
- **Response format MUST be `ApiResponse<T>`**: Desktop Refit client deserializes into `ApiResponse<T>`. Bare `Ok(new { ... })` causes silent deserialization failure → UI stuck.
- **WebApplication.CreateBuilder (NOT CreateSlimBuilder)**: `CreateSlimBuilder` does NOT load `appsettings.json` in .NET 8. Use `CreateBuilder` for embedded LocalWebAPI.
- **DefaultPasswordOptions must be explicitly registered**: In `EmbeddedLocalWebApiService.StartAsync`, call `builder.Services.Configure<DefaultPasswordOptions>(...)` BEFORE `CreateApplication`.
- **AddApplicationPart required**: `builder.Services.AddControllers()` alone won't discover LocalWebAPI controllers from a WPF host. Must `.AddApplicationPart(typeof(HealthController).Assembly)`.
- **LocalWebApiProgram scope fix**: `InitializeDatabaseAsync` must use `scope.ServiceProvider` (NOT `app.Services`) for `IdentitySeedData`.

### Connection Mode Switching

- **IsLocalUrl matches port 5100 only**: `localhost:5000` is REMOTE (WebAPI running locally). Only `localhost:5100` is LOCAL (embedded LocalWebAPI).
- **SaveAndEnableAsync must re-probe**: Call `CheckRemoteAvailableAsync()` before `SetMode(Remote)` — stale `_isRemoteAvailable` blocks mode switch.
- **appsettings.json path**: Use `AppContext.BaseDirectory` (NOT `Directory.GetCurrentDirectory()`) for `ConnectionSettingsService._settingsFilePath`.

### Desktop WPF

- **FindAsync applies global query filters** (`IsDeleted`) — use `IgnoreQueryFilters()` for soft-deleted records
- **MedicalCase.HasPrescription** is computed — Mapper must set it explicitly
- **WPF Desktop tests require `net8.0-windows`** — cannot mix with Server tests
- **PrescriptionItem.HerbId is `Guid`** (non-nullable) — `.HasValue` will fail compile
- **Refit interface convention**: Do NOT use `using Refit;` in Contracts/Api files — CS0104 ambiguity with `LYBT.Shared.Models.Contracts.Common.ApiResponse<T>`. Use `[Refit.Get]` fully-qualified.
- **BoolToVis converter**: Use `{x:Static converters:Cvt.BoolToVis}`, NOT `{StaticResource BoolToVis}`
- **XAML ResourceDictionary pack URI**: Use `Source="/Assembly;component/Path.xaml"` (pack URI), NOT relative `../Path.xaml`

### API & Authorization

- Permission levels: `Receptionist=0, Doctor=1, Admin=10, SuperAdmin=100`
- 2 authorization policies: `DoctorOrReceptionist` + `AdminOrSuperAdmin`
- `CanManageUser`: `IsSysAdmin` bypasses all role checks; Admin can only manage Doctor/Receptionist
- sysadmin account is immutable: cannot be deleted, disabled, or modified via API
- `BaseApiController` requires `ILogger` in constructor

## Specs & Plans

- Active specs: `docs/compose/specs/` (design documents)
- Active plans: `docs/compose/plans/` (implementation plans)
- Reports: `docs/compose/reports/` (completion reports)

## Key Files Reference

| File | Purpose |
|------|---------|
| `docs/01-product/02-personas.md` | 用户画像 + 默认密码 + 角色权限矩阵 |
| `docs/02-requirements/03-users.md` | 用户管理模块设计 + 权限端点矩阵 |
| `src/Server/Modules/LYBT.Module.Users/Services/IdentitySeedData.cs` | 种子数据（sysadmin + admin 创建） |
| `src/Client/Desktop/Shell/Services/EmbeddedLocalWebApiService.cs` | 嵌入式 LocalWebAPI 启动 |
| `src/Client/Desktop/LocalWebAPI/LocalWebApiProgram.cs` | LocalWebAPI 配置 + 初始化 |
| `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Http/BaseUrlDelegatingHandler.cs` | 线程安全的 HTTP URL 路由 |

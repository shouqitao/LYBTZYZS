# LYBTZYZS — 凌隐宝堂中医诊所管理系统

**.NET 8** | WPF/Prism | ASP.NET Core | EF Core | SQL Server (Remote + LocalDB dual-mode)

---

## ⚠️ 收到任务后的第一步

**设计方案前必须先读代码。** 用 CodeGraph/Serena 阅读现有实现，避免重复造轮子。

| ❌ 禁止 | ✅ 应该 |
|---------|---------|
| 直接用 Grep/Read 搜索代码 | 用 `codegraph_explore` 理解流程 |
| 手动重命名变量 | 用 `serena_rename_symbol` |
| 猜测框架 API | 用 `context7_query-docs` 查文档 |
| 跳过 brainstorm 直接编码 | 先用 `compose:brainstorm` |
| 跳过 verify 声称完成 | 必须有 `dotnet build` 证据 |
| 设计方案前不读代码 | 先用 CodeGraph/Serena 阅读现有实现 |

---

## 工具选择（按场景）

| 场景 | 工具 | 说明 |
|------|------|------|
| 理解代码流程 | `codegraph_explore` | 不用 Read/Grep |
| 读文件/符号源码 | `codegraph_node` 或 `serena_read_file` | |
| 重命名/重构 | `serena_rename_symbol` | LSP 保证安全 |
| 查找引用 | `codegraph_callers` 或 `serena_find_referencing_symbols` | |
| 编译诊断 | `serena_get_diagnostics_for_file` | 不用等 build |
| 替换方法体 | `serena_replace_symbol_body` | |
| 复杂问题分解 | `sequentialthinking` | 分步思考 |
| 查框架文档 | `context7_resolve-library-id` → `context7_query-docs` | Prism/Refit/EF Core |
| 以上都不适用 | Read/Write/Edit/Glob/Grep | 兜底 |

**首次使用 Serena 前必须激活：** `serena_activate_project(project="LYBTZYZS")`

---

## 工程纪律

| 场景 | 规则 |
|------|------|
| 新功能 | `compose:brainstorm` 探索意图后再编码 |
| 多步骤任务 | `compose:plan` 碰代码前出计划 |
| Bug | `compose:debug` 找到根因再修，禁止跳过到修复 |
| 声称完成 | `compose:verify` 必须有 `dotnet build` 通过证据 |
| **执行 Plan** | **必须经用户确认后才能执行** |

### 工作流分级

- **小修补** (1-2 行): 改完 → `dotnet build` → 提交
- **新功能**: `brainstorm` → `plan` → 实现 → `verify` → `review`
- **跨模块大改**: `brainstorm` → `plan` → `plan-eng-review` → `subagent` → `verify` → `review` → `merge`

### Karpathy 准则

- **先思考再编码** — 不假设、不隐藏困惑、呈现权衡
- **简单优先** — 最小代码解决问题，不添加 speculative 功能
- **外科手术式修改** — 只改必须的部分，不顺手重构
- **目标驱动执行** — 定义可验证的成功标准，循环直到通过

---

## Build & Test

```bash
dotnet build LYBTZYZS.sln
dotnet test tests/LYBT.Tests.Server/        # Integration (real SQL Server + Respawn)
dotnet test tests/LYBT.Tests.Desktop/       # Desktop (LocalDB)
dotnet test tests/LYBT.Tests.Architecture/  # Architecture guards
```

## Git & Remote

- **Remote**: Gitee (`https://gitee.com/shouqitao/LYBTZYZS.git`) — NOT GitHub
- **Branch**: `master`
- **Commit**: `feat(模块): 描述` / `fix(模块): 描述` / `docs:` / `refactor:` / `test:`
- **Commit language**: English (PowerShell encoding issues with Chinese)

## Database

- **Migration**: Latest is `AddIsSysAdmin` — run `dotnet ef database update` after pulling
- **Dual-mode**: Remote = SQL Server (`LYBTDB_Dev`) | Local = LocalDB (`LYBTDesktop`) — NOT SQLite
- **Migration cmd**: `dotnet ef migrations add <Name> --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI`
- **EnsureCreatedAsync**: `DatabaseInitializationService` uses `EnsureCreatedAsync`, not `Migrate()`
- **Reset DB**: `sqlcmd -S "localhost" -Q "DROP DATABASE IF EXISTS LYBTDB_Dev; CREATE DATABASE LYBTDB_Dev"` then restart WebAPI
- **Reset LocalDB**: `sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "DROP DATABASE IF EXISTS LYBTDesktop"` then restart Desktop

## Architecture

- **3-Layer**: Controller → Service → Repository → DbContext
- **MVVM**: View (XAML) ← binding → ViewModel → Repository → API
- **DDD**: MedicalCase is the sole aggregate root
- **Dual-Mode**: Remote (port 5000) + Local (LocalDB port 5100), URL-based switching via `BaseUrlDelegatingHandler`
- **Modular**: Server (`LYBT.Module.*`), Desktop (`LYBT.Desktop.*`), Roles (Admin/Clinical/Receptionist/Sysadmin)

### Sysadmin (2026-06-20)

- **sysadmin = standalone user, NOT a role**: `ApplicationUser.IsSysAdmin = true`
- **Default users**: `sysadmin` / `SysAdmin@2026!` | `admin` / `Admin@123456`
- **Passwords from config**: `appsettings.json` → `DefaultPasswords` section

## Terminology

| Term | Meaning | Not |
|------|---------|-----|
| Consultation | 中医诊断 | "问诊" or "就诊" |
| MedicalCase | 医案 | "病历" |
| Formula | 验方/经验方 | "公式" |
| HerbRole | 药材角色（君臣佐使） | — |
| Sysadmin | 系统运维（独立用户） | "超级管理员角色" |

## CODE STYLE

- **语言**: 中文业务文档/注释；英文技术标识符/commit
- **命名**: `PascalCase`（公共）、`_camelCase`（私有）、`I PascalCase`（接口）
- **包版本**: 统一在 `Directory.Packages.props`
- **无注释/无 Emoji**: 除非用户要求
- **跨模块禁止**: Server/Desktop 模块间禁止直接引用

---

## Common Pitfalls (CRITICAL — read before coding)

### Identity & Auth

- `RoleManager`/`UserManager` are SCOPED — NEVER resolve from root provider
- `IdentitySeedData` password reset: Only when `user.LastLoginAt == null`
- BCrypt (PasswordHelper) vs PBKDF2 (Identity) are INCOMPATIBLE — all hashing via `UserManager`
- `AddIdentity()` must run BEFORE JWT `AddAuthentication()`

### LocalWebAPI (Embedded Server)

- Route prefix MUST be `api/v1/[controller]`
- Response format MUST be `ApiResponse<T>` — bare `Ok()` causes silent deserialization failure
- Use `WebApplication.CreateBuilder` (NOT `CreateSlimBuilder`)
- Must `.AddApplicationPart(typeof(HealthController).Assembly)`
- `InitializeDatabaseAsync` must use `scope.ServiceProvider` (NOT `app.Services`)

### Connection Mode Switching

- `localhost:5100` = LOCAL, `localhost:5000` = REMOTE
- `SaveAndEnableAsync` must re-probe: Call `CheckRemoteAvailableAsync()` before `SetMode(Remote)`
- appsettings.json path: Use `AppContext.BaseDirectory`

### Desktop WPF

- `FindAsync` applies global query filters (`IsDeleted`) — use `IgnoreQueryFilters()`
- `PrescriptionItem.HerbId` is `Guid` (non-nullable) — `.HasValue` fails compile
- Do NOT use `using Refit;` in Contracts/Api — CS0104 ambiguity. Use `[Refit.Get]`
- BoolToVis: `{x:Static converters:Cvt.BoolToVis}`, NOT `{StaticResource BoolToVis}`
- XAML pack URI: `Source="/Assembly;component/Path.xaml"`, NOT relative

### API & Authorization

- Permission: `Receptionist=0, Doctor=1, Admin=10, SuperAdmin=100`
- Policies: `DoctorOrReceptionist` + `AdminOrSuperAdmin`
- sysadmin account is immutable: cannot be deleted/disabled/modified
- `BaseApiController` requires `ILogger` in constructor

---

## WHERE TO LOOK

| Task | Location |
|------|----------|
| WebAPI entry | `src/Server/Services/LYBT.WebAPI/Program.cs` |
| Desktop entry | `src/Client/Desktop/Shell/App.xaml.cs` |
| DbContext | `src/Server/Core/LYBT.Infrastructure/Data/AppDbContext.cs` |
| Entities | `src/Server/Core/LYBT.Entities/` |
| DTOs | `src/Shared/LYBT.Shared.Models/Contracts/` |
| Server Controllers | `src/Server/Services/LYBT.WebAPI/Controllers/` |
| Desktop Modules | `src/Client/Desktop/Modules/LYBT.Desktop.*/` |
| Desktop Core | `src/Client/Desktop/Core/` |
| Desktop Roles | `src/Client/Desktop/Roles/` (Admin, Clinical, Receptionist, **Sysadmin**) |
| Seed Data | `src/Server/Modules/LYBT.Module.Users/Services/IdentitySeedData.cs` |
| Role Definitions | `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Roles/Definitions/` |
| Specs | `docs/compose/specs/` |
| Plans | `docs/compose/plans/` |

## Key Patterns

- **ApiResponse<T>** envelope — ALL controllers must wrap responses
- **CommunityToolkit.Mvvm** `[ObservableProperty]` / `[RelayCommand]` — NOT Prism's BindableBase/DelegateCommand
- **Riok.Mapperly** for compile-time mapping — NOT AutoMapper
- **ViewModelLocator.AutoWireViewModel**: VM 必须在 Module.RegisterTypes 中注册
- **Soft-delete + global query filter** on most entities
- **SwitchingApiClient** routes localhost:5100 → embedded LocalWebAPI, otherwise → Refit remote

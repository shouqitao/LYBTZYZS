# LYBTZYZS — 凌隐宝堂中医诊所管理系统

**.NET 8** | WPF/Prism | ASP.NET Core | EF Core | SQL Server (Remote + LocalDB dual-mode)

## Build & Test

```bash
dotnet build LYBTZYZS.sln

dotnet test tests/LYBT.Tests.Server/        # Integration (real SQL Server + Respawn)
dotnet test tests/LYBT.Tests.Desktop/       # Desktop (SQLite InMemory)
dotnet test tests/LYBT.Tests.Architecture/  # Architecture guards
```

## Git & Remote

- **Remote**: Gitee (`https://gitee.com/shouqitao/LYBTZYZS.git`) — NOT GitHub
- **Branch**: `master`
- **Commit convention**: `feat(模块): 描述` / `fix(模块): 描述` / `docs:` / `refactor:` / `test:`

## Database

- **EF Core Migration**: Latest is `SimplifyDataModel` — run `dotnet ef database update` after pulling
- **Dual-mode**: Remote = SQL Server | Local = SQL Server LocalDB (NOT SQLite — SQLite is test-only)
- **Migration command**: `dotnet ef migrations add <Name> --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI`

## Architecture

- **3-Layer**: Controller → Service → Repository → DbContext
- **MVVM**: View (XAML) ← binding → ViewModel → Repository → API
- **DDD**: MedicalCase is the sole aggregate root (Consultation + Prescription are internal entities)
- **Dual-Mode**: Remote (SQL Server) + Local (SQL Server LocalDB), URL-based switching
- **Modular**: Server modules (`LYBT.Module.*`), Desktop modules (`LYBT.Desktop.*`), role workspaces (Admin/Clinical/Receptionist)

## Terminology

| Term | Meaning | Not |
|------|---------|-----|
| Consultation | 中医诊断 (TCM diagnosis) | "问诊" or "就诊" |
| MedicalCase | 医案 (medical case) | "病历" |
| Formula | 验方/经验方 (empirical recipe) | "公式" |
| HerbRole | 药材角色（君臣佐使） | — |

## CODE STYLE

- **语言**: 中文用于业务文档和注释；英文用于技术标识符
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
| Desktop Core | `src/Client/Desktop/Core/` (Contracts, Foundation, Infrastructure, LocalData, Printing, CardReader) |
| Desktop Roles | `src/Client/Desktop/Roles/` (Admin, Clinical, Receptionist workspaces) |
| Shared Exception | `src/Shared/LYBT.Shared.ExceptionHandling/` |
| Reports Module | `src/Server/Modules/LYBT.Module.Reports/` + `src/Client/Desktop/Modules/LYBT.Desktop.Reports/` |
| HerbRole Enum | `src/Shared/LYBT.Shared.Models/Enums/HerbRole.cs` |
| Docs | `docs/{01-product,02-requirements,03-architecture,04-api-reference,05-development,06-operations}/` |

## Common Pitfalls

- `FindAsync` applies global query filters (`IsDeleted`) — use `IgnoreQueryFilters()` for soft-deleted records
- `MedicalCase.HasPrescription` is computed — Mapper must set it explicitly
- WPF Desktop tests require `net8.0-windows` — cannot mix with Server tests
- Permission levels: `Receptionist=0, Doctor=1, Admin=10, SuperAdmin=100`
- Receptionist excluded from `/patients` and `/registrations/queue` (DoctorOrAdmin policy)
- Admin can access diagnostics and reset passwords (AdminOnly policy)
- `BaseApiController` requires `ILogger` in constructor — all LocalWebAPI controllers must pass it
- `PrescriptionItem.HerbId` is `Guid` (non-nullable) — code referencing `.HasValue` will fail compile
- EF Migration `SimplifyDataModel` dropped 5 tables, 17 columns — run `dotnet ef database update` after pulling

## Key Patterns

- **Two-phase Serilog bootstrap** (WebAPI + Desktop)
- **Role-based module loading** (Desktop loads modules by user role)
- **CQRS for MedicalCase** (CommandHandler pattern, not traditional 3-layer)
- **Testing Trophy** (Integration-first, zero mock for Server tests)
- **Soft-delete + global query filter** on most entities
- **SwitchingApiClient** routes localhost → embedded LocalWebAPI, otherwise → Refit remote API
- **BaseApiController** provides `Success()`, `HandleResult()`, `GetOperator()`, `ValidatePagination()` — all controllers inherit it
- **ApiResponse<T>** envelope used consistently (Phase 2 API fixes)
- **RegistrationQueueNumber** auto-generated daily (max+1), **RegistrationFee** input by receptionist
- **HerbRole** (君臣佐使) on PrescriptionItem for auto-sorting in prescriptions

### Desktop WPF Patterns

- **Prism Region-based navigation** via `NavigationCoordinator` + `IRegionManager`
- **MasterDetailControlBase** pattern for entity CRUD (Patients/Herbs/Formulas/Users)
- **CommunityToolkit.Mvvm** `[ObservableProperty]` / `[RelayCommand]` — NOT Prism's `BindableBase` / `DelegateCommand`
- **Thin Wrapper views** — Role views (Admin/Clinical) embed business module controls as 15-19 line XAML wrappers
- **Composite ViewModel** — MedicalCaseWorkspaceViewModel composes child VMs (ConsultationEditor, PrescriptionEditor, Commands)
- **Riok.Mapperly** for compile-time mapping — `.csproj` must NOT include AutoMapper unless fallback needed

### Recent Refactoring (Phase 1-3, 2026-06-16)

Phase 1 removed: sync module (entire), audit logs, print tracking, RestoreAsync, CheckReference, PendingQueue, simplified permissions (4→2 policies), simplified auth (no refresh/auto-login tokens), simplified Patient entity (18→8 fields).

Phase 2 added: RegistrationFee + QueueNumber on Registration, Reports module (3 daily report endpoints), cleaned legacy endpoints.

Phase 3 updated: Registration UI (fee input + queue display), Reports dashboard (3-card UI), cleaned stale UI references (sync/audit).

**Do NOT reference removed features** — code will fail to compile. Tests for removed features have been deleted.

## Skill Routing (技能路由)

收到请求时，先检查技能是否匹配。匹配 → invoke 技能，不直接编码。

### 工程纪律（编码前 MUST 触发）

| 场景 | 技能 | 规则 |
|------|------|------|
| 新功能/修改行为 | `compose:brainstorm` | 探索意图后再编码 |
| 有规格的多步骤任务 | `compose:plan` | 碰代码前出计划 |
| Bug/测试失败/异常 | `compose:debug` | 找到根因再修，禁止跳过到修复 |
| 实现功能/修复 | `compose:tdd` | 先写测试再写实现 |
| 声称"完成/修好" | `compose:verify` | 必须有 `dotnet test` 通过的证据 |

### 方向与交付（匹配时触发）

| 场景 | 技能 |
|------|------|
| 新想法/"值得做吗" | `office-hours` |
| 架构/设计评审 | `plan-eng-review` |
| 策略/范围/"再大胆点" | `plan-ceo-review` |
| 全自动评审流水线 | `autoplan` |
| 安全审计/漏洞扫描 | `cso` |
| 代码审查/diff 检查 | `compose:review` |
| 合并/集成/PR | `compose:merge` |

### 工作流分级

| 规模 | 必须流程 |
|------|---------|
| **小修补** (typo、1-2 行) | 改完 → `dotnet test` → 提交 |
| **新功能/明确重构** | `brainstorm` → `plan` → TDD 实现 → `verify` → 提交 |
| **跨模块大改/新架构** | `brainstorm` → `plan` → `plan-eng-review` → `subagent` 并行 → `verify` → `review` → `merge` |

### 核心原则

- **前期思考 > 后期 debug** — 20% 的方案评审决定 80% 的结果
- **按需裁剪** — 小修小补跳过完整流程，不搞流程内耗
- **不验证不声称完成** — "应该修好了"不算完成，必须有测试输出作为证据
- **找不到根因不修 bug** — `compose:debug` 四阶段：调查 → 分析 → 假设 → 实现

## MCP Tools

- **CodeGraph**: Pre-indexed code knowledge graph with auto-sync. Use `codegraph_explore` to answer architecture questions, `codegraph_node` for symbol details, `codegraph_search` to find symbols, `codegraph_callers` for call sites. Auto-syncs on file changes — no manual re-indexing needed.
- **filesystem**: File system access for the project directory
- **context7**: Library documentation lookup (v3.2.1, stdio)

# LYBTZYZS - 凌隐宝堂中医诊所管理系统

**.NET 8** | WPF/Prism | ASP.NET Core | EF Core | SQL Server + SQLite dual-mode

---

## Build & Test

```bash
dotnet build LYBTZYZS.sln

# 交叉编译 (Ubuntu → Windows win-x64)
dotnet publish src/Server/Services/LYBT.WebAPI/LYBT.WebAPI.csproj -c Release -r win-x64 --self-contained false -p:EnableWindowsTargeting=true

# Test projects (~2021 tests total)
dotnet test tests/LYBT.Tests.Server/        # 1185 tests (real SQL Server + Respawn)
dotnet test tests/LYBT.Tests.Desktop/       # 760 tests (SQLite InMemory)
dotnet test tests/LYBT.Tests.Architecture/  # 76 tests (architecture guards)
```

## Architecture

- **3-Layer**: Controller → Service → Repository → DbContext
- **MVVM**: View (XAML) ← binding → ViewModel → Repository → API
- **DDD**: MedicalCase is the sole aggregate root (Consultation + Prescription are internal entities)
- **Dual-Mode**: Remote (SQL Server) + Local (SQLite), shared Service/Repository layer

## Terminology

| Term | Meaning | Not |
|------|---------|-----|
| Consultation | 中医诊断 (TCM diagnosis) | "问诊" or "就诊" |
| MedicalCase | 医案 (medical case) | "病历" |
| Formula | 验方/经验方 (empirical recipe) | "公式" |

## Dev Rules

1. **Architecture First** - Prioritize architectural integrity
2. **Root Cause Analysis** - No surface-level patches
3. **Test Coverage** - New features must include tests
4. **Documentation** - Update `docs/` for architectural decisions and API changes

## Common Pitfalls

- `FindAsync` applies global query filters (`IsDeleted`) when entity not in ChangeTracker — use `IgnoreQueryFilters()` for soft-deleted records
- WPF Desktop tests require `net8.0-windows` target framework — cannot mix with Server tests
- `MedicalCase.HasPrescription` is computed property depending on `PrescriptionId.HasValue` — Mapper must set it explicitly
- Edit tool requires distinct oldString and newString — never use identical or empty strings (see ERR-20260406-003)

## Module Docs

```
docs/
├── 01-product/          # Product docs
├── 02-requirements/     # PRD (9 modules, 92 requirements)
├── 03-architecture/     # Architecture, data model, security, ADR
├── 04-api-reference/    # 99 API endpoints
├── 05-development/      # Dev guide, coding standards, testing
└── 06-operations/       # Deployment, config, monitoring
```

Sub-directory `CLAUDE.md` files provide module-specific guidance throughout `src/`.

## MCP Tools Quick Reference

| Use Case | Tool |
|----------|------|
| Project architecture knowledge | Serena: `list_memories` → `read_memory` |
| NuGet / framework docs | Context7: `resolve-library-id` → `query-docs` |
| Microsoft official docs | `microsoft_docs_search` |
| Code semantic search | `get_code_context_exa` |
| Web search | `tavily_search` / `brave_web_search` |
| GitHub operations | `gh` CLI |
| Current time | `get_current_time(timezone="Asia/Shanghai")` |

## Self-Improvement & Learnings

**Skill**: `self-improvement` — Log learnings and errors for continuous improvement.

### Session Start (MANDATORY)

At session start, BEFORE executing any commands:

1. **Read `.learnings/ERRORS.md`** — Check for known error patterns
2. **Read `.learnings/LEARNINGS.md`** — Check for project-specific conventions
3. **Apply patterns** — Avoid repeating documented mistakes

### Platform-Specific Rules (CRITICAL)

This project runs on **Windows PowerShell**. NEVER use bash-specific syntax:
- ❌ `export VAR=value` — Use `git` directly (env vars injected by tool)
- ❌ `$(cat <<'EOF')` heredoc — Use simple string messages
- ❌ `ls -la` — Use `dir` or tool's native file listing
- ✅ `git add <files>` — Direct git commands work fine
- ✅ `git commit -m "simple message"` — Single-line messages only

### Usage

When you encounter:
- **Errors**: Command failures, API errors, test failures → Log to `.learnings/ERRORS.md`
- **Corrections**: User corrects your approach → Log to `.learnings/LEARNINGS.md`
- **Feature Requests**: User wants missing capability → Log to `.learnings/FEATURE_REQUESTS.md`
- **Best Practices**: Discovered better approach → Log to `.learnings/LEARNINGS.md`

### File Locations

```
.learnings/
├── LEARNINGS.md          # Corrections, insights, knowledge gaps
├── ERRORS.md             # Command failures, integration errors
└── FEATURE_REQUESTS.md   # User-requested capabilities
```

### Format

```markdown
## [LRN-YYYYMMDD-XXX] category

**Logged**: ISO-8601 timestamp
**Priority**: low | medium | high | critical
**Status**: pending | resolved | promoted

### Summary
One-line description

### Details
Full context

### Suggested Action
Specific fix or improvement

### Metadata
- Source: conversation | error | user_feedback
- Related Files: path/to/file.ext
- Tags: tag1, tag2
```

## WHERE TO LOOK (Quick Reference)

| Task | Location | Notes |
|------|----------|-------|
| WebAPI entry point | `src/Server/Services/LYBT.WebAPI/Program.cs` | Two-phase Serilog bootstrap |
| Desktop entry point | `src/Client/Desktop/Shell/App.xaml.cs` | Prism startup pipeline |
| DbContext | `src/Server/Core/LYBT.Infrastructure/Data/AppDbContext.cs` | EF Core 8 |
| Entities | `src/Server/Core/LYBT.Entities/` | Anemic except MedicalCaseModel |
| DTOs/Contracts | `src/Shared/LYBT.Shared.Models/Contracts/` | Shared between Client/Server |
| Server Controllers | `src/Server/Services/LYBT.WebAPI/Controllers/` | 13 controllers |
| Server Modules | `src/Server/Modules/LYBT.Module.*/` | 7 modules (Auth, Users, Patients, MedicalCase, Herbs, Formula, Sync) |
| Desktop Modules | `src/Client/Desktop/Modules/LYBT.Desktop.*/` | 8 modules |
| Desktop Roles | `src/Client/Desktop/Roles/LYBT.Desktop.{Admin,Clinical,Receptionist}/` | 3 role workspaces |
| Shared Exception Handling | `src/Shared/LYBT.Shared.ExceptionHandling/` | Desktop + Server handlers |
| Shared Logging | `src/Shared/LYBT.Shared.Logging/` | Serilog config |
| Server Tests | `tests/LYBT.Tests.Server/` | Real SQL Server + Respawn |
| Desktop Tests | `tests/LYBT.Tests.Desktop/` | SQLite InMemory |
| Architecture Tests | `tests/LYBT.Tests.Architecture/` | Guard tests |
| Build config | `Directory.Build.props` | LangVersion, Nullable, ImplicitUsings |
| Package versions | `Directory.Packages.props` | Central package management |
| Code style | `.editorconfig` | Naming, formatting, analyzer rules |

## ANTI-PATTERNS (THIS PROJECT)

- **_wpftmp.csproj files** — WPF designer artifacts cluttering repo (found in `src/Client/Desktop/Modules/`)
- **Service layer injecting DbContext** — Must use Repository interface (enforced by architecture test `P10_Services_Should_Not_Directly_Inject_AppDbContext`)
- **ContainerLocator in UI layer** — Known anti-pattern documented in Desktop README
- **Cross-module references** — Server modules MUST NOT reference each other; Desktop modules MUST NOT reference each other
- **Emoji in code** — Cleaned from codebase 2025-11-20

## UNIQUE STYLES

- **Two-phase Serilog bootstrap** — Bootstrap logger → final logger (both WebAPI and Desktop)
- **Role-based module loading** — Desktop loads modules dynamically based on user role
- **StartupPipeline pattern** — Desktop uses step-based startup (ErrorHandling → ModuleCoordinator → CoreServices → ApiHealthCheck → Warmup)
- **CQRS for MedicalCase** — Server MedicalCase module uses CommandHandler pattern, not traditional 3-layer
- **Testing Trophy** — Integration-first: real SQL Server, zero mock for Server tests

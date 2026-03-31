# LYBTZYZS - 凌隐宝堂中医诊所管理系统

**.NET 8** | WPF/Prism | ASP.NET Core | EF Core | SQL Server + SQLite dual-mode

---

## Build & Test

```bash
dotnet build LYBT.All.sln

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

- `FindAsync` applies global query filters (`IsDeleted`) when entity is not in ChangeTracker — use `IgnoreQueryFilters()` for soft-deleted records
- WPF Desktop tests require `net8.0-windows` target framework — cannot mix with Server tests
- `MedicalCase.HasPrescription` is a computed property depending on `PrescriptionId.HasValue` — Mapper must set it explicitly

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

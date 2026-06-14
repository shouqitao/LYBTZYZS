# LYBTZYZS — 凌隐宝堂中医诊所管理系统

**.NET 8** | WPF/Prism | ASP.NET Core | EF Core | SQL Server (Remote + LocalDB dual-mode)

## Build & Test

```bash
dotnet build LYBTZYZS.sln

dotnet test tests/LYBT.Tests.Server/        # Integration (real SQL Server + Respawn)
dotnet test tests/LYBT.Tests.Desktop/       # Desktop (SQLite InMemory)
dotnet test tests/LYBT.Tests.Architecture/  # Architecture guards
```

## Architecture

- **3-Layer**: Controller → Service → Repository → DbContext
- **MVVM**: View (XAML) ← binding → ViewModel → Repository → API
- **DDD**: MedicalCase is the sole aggregate root (Consultation + Prescription are internal entities)
- **Dual-Mode**: Remote (SQL Server) + Local (SQL Server LocalDB), URL-based switching

## Terminology

| Term | Meaning | Not |
|------|---------|-----|
| Consultation | 中医诊断 (TCM diagnosis) | "问诊" or "就诊" |
| MedicalCase | 医案 (medical case) | "病历" |
| Formula | 验方/经验方 (empirical recipe) | "公式" |

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
| Shared Exception | `src/Shared/LYBT.Shared.ExceptionHandling/` |
| Docs | `docs/{01-product,02-requirements,03-architecture,04-api-reference,05-development,06-operations}/` |

## Common Pitfalls

- `FindAsync` applies global query filters (`IsDeleted`) — use `IgnoreQueryFilters()` for soft-deleted records
- `MedicalCase.HasPrescription` is computed — Mapper must set it explicitly
- WPF Desktop tests require `net8.0-windows` — cannot mix with Server tests
- Permission levels: `Receptionist=0, Doctor=1, Admin=10, SuperAdmin=100`

## Key Patterns

- **Two-phase Serilog bootstrap** (WebAPI + Desktop)
- **Role-based module loading** (Desktop loads modules by user role)
- **CQRS for MedicalCase** (CommandHandler pattern, not traditional 3-layer)
- **Testing Trophy** (Integration-first, zero mock for Server tests)
- **Soft-delete + global query filter** on most entities

<!-- gitnexus:start -->
GitNexus indexed: **LYBTZYZS** (35341 symbols, 76290 relationships). Run `gitnexus_impact` before editing any symbol. Run `gitnexus_detect_changes()` before committing.
<!-- gitnexus:end -->

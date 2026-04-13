# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

凌隐宝堂中医诊所管理系统 (LYBTZYZS) -- Enterprise clinic management system for Traditional Chinese Medicine clinics.

**Tech Stack**: .NET 8 + WPF/Prism.DryIoc (Desktop) + ASP.NET Core WebAPI (Server) + EF Core 8 + SQL Server/SQLite (dual-mode)

**Architecture**: Server/Shared/Client three-tier, supporting both remote (SQL Server via HTTP API) and local (SQLite) modes.

## Build, Test, Run Commands

### Build
```bash
dotnet restore LYBTZYZS.sln
dotnet build LYBTZYZS.sln

# Frontend only (faster)
dotnet build LYBT.Desktop.sln

# Backend only
dotnet build LYBT.Backend.sln

# Clean rebuild
dotnet clean LYBTZYZS.sln && dotnet build LYBTZYZS.sln
```

### Test
```bash
# All tests (~1700+ across 3 projects, Testing Trophy architecture)
dotnet test LYBTZYZS.sln --filter "FullyQualifiedName~LYBT.Tests"

# Individual test projects
dotnet test tests/LYBT.Tests.Server/           # ~1185 tests, real SQL Server + Respawn, zero mock
dotnet test tests/LYBT.Tests.Desktop/          # ~493 tests, SQL Server LocalDB + real Repository
dotnet test tests/LYBT.Tests.Architecture/     # ~76 tests, architecture guard + AntiMockRules

# Run a single test
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~TestClassName.MethodName"

# Run tests in a specific module
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~MedicalCase"
```

### Run
```bash
# Start WebAPI server
dotnet run --project src/Server/Services/LYBT.WebAPI

# Start Desktop client -- set LYBT.Desktop.Shell as startup project in Visual Studio, press F5
```

### Scripts (run from project root)
```bash
# PowerShell
.\scripts\run-webapi.ps1          # Start WebAPI
.\scripts\stop-webapi.ps1         # Stop WebAPI
.\scripts\run-tests-local.ps1     # Run tests locally
.\scripts\cleanup.ps1             # Clean temp files

# Batch
scripts\build.bat                 # Interactive build manager
scripts\build-check.bat           # Build verification
scripts\quick-compile.bat         # Quick compile check
```

## Code Architecture

### Solution Structure (~40+ projects)

```
LYBTZYZS.sln
  src/
    Server/                          # ASP.NET Core Backend
      Core/
        LYBT.Entities/               # Domain entities (anemic model, except MedicalCaseModel DDD aggregate root)
        LYBT.Infrastructure/         # DbContext, BaseRepository, EF configurations
      Modules/                       # Business modules (Auth, Users, Patients, Herbs, Formula, MedicalCase, Sync)
      Services/
        LYBT.WebAPI/                 # API entry point

    Client/Desktop/                  # WPF Desktop Client (Prism.DryIoc MVVM)
      Core/
        LYBT.Desktop.Contracts/      # Interface definitions (Refit IApi, IRepository, IService)
        LYBT.Desktop.Foundation/     # Infrastructure (HTTP, security, config, ConnectionMode)
        LYBT.Desktop.Infrastructure/ # WPF services (Dialog, Navigation, controls, converters)
        LYBT.Desktop.Models/         # Client UI models
        LYBT.Desktop.Printing/       # QuestPDF print service
        LYBT.Desktop.LocalData/      # SQLite local mode DbContext + repositories
        LYBT.Desktop.CardReader/     # ID card reader hardware integration
        LYBT.Desktop.Utilities/      # Utility classes
      Modules/                       # Business modules (Auth, Patients, Herbs, Formula, MedicalCase, Users, Sync)
      Roles/
        LYBT.Desktop.Admin/          # Admin role workspace
        LYBT.Desktop.Clinical/       # Clinical role workspace
      Shell/
        LYBT.Desktop.Shell/          # PrismApplication entry point

    Shared/                          # Shared libraries
      LYBT.Shared.Models/            # DTOs, contracts
      LYBT.Shared.Components/        # Shared UI components
      LYBT.Shared.Validators/        # Shared FluentValidation rules
      LYBT.Shared.Configuration/     # Shared config models
      LYBT.Shared.Primitives/        # Base types and constants
      LYBT.Shared.ExceptionHandling/ # Unified exception types
      LYBT.Shared.Logging/           # Unified logging abstraction
      LYBT.Shared.Utilities/         # Utility classes

  tests/
    LYBT.Tests.Server/               # Server integration tests (real SQL Server, zero mock)
    LYBT.Tests.Desktop/              # Desktop tests (LocalDB + real Repository)
    LYBT.Tests.Architecture/         # Architecture guard tests
```

### Dependency Rules (ENFORCE)

**Server**: `WebAPI -> Modules -> Infrastructure -> Entities` (unidirectional)
- Modules MUST NOT reference each other; cross-module via `ICrossModuleService` interfaces
- All layers may reference `Shared.Models`

**Client**: `Shell -> Roles -> Modules -> Infrastructure -> Foundation -> Contracts` (unidirectional)
- Business modules MUST NOT reference each other
- Modules depend on `Desktop.Models` and `Desktop.Contracts`

**Cross-layer**: Client references `Shared.Models` for DTOs; Server and Client NEVER directly reference each other

### Key Architectural Decisions

1. **MedicalCase is the sole DDD aggregate root** (ADR-0001): Consultation and Prescription are internal entities; no independent repositories; all operations through `MedicalCaseDataManager`
2. **Dual-mode architecture** (ADR-0002): Remote (SQL Server via HTTP) vs Local (SQLite), sharing Service/Repository layer, differing only in DbContext provider
3. **Integration-first testing** (ADR-0003): Server tests use real SQL Server + Respawn, zero mock
4. **Anemic model except MedicalCaseModel**: MedicalCaseModel is rich with domain methods (`Complete()`, `SaveAsDraft()`, `SoftDelete()`, `UpdateConsultation()`); all other entities are anemic
5. **Interface segregation**: All services injected via interfaces (ISP principle); fine-grained cross-module service interfaces

### Desktop MVVM Patterns

- ViewModel base: `UnifiedViewModelBase` / `UnifiedListViewModelBase<T>`
- Data access: `I{Entity}Repository` for CRUD, `I{Entity}DataManager` for aggregates
- Object mapping: Riok.Mapperly (compile-time) + AutoMapper (runtime fallback)
- Module registration: Prism `IModule` interface in `{Domain}Module.cs`
- Navigation: Prism Region-based between modules

### Server API Patterns

- Three-layer: `Controller -> Service -> Repository -> DbContext`
- Simple modules: Traditional three-layer pattern
- MedicalCase module: CQRS pattern with CommandHandler
- Repository pattern: `BaseRepository<T>` (21 public methods), `IRepository<T>` interface
- All DTOs in `Shared.Models`; entities in `LYBT.Entities`

## Development Conventions

- **Language**: C# latest, nullable enabled, implicit usings enabled
- **Package management**: Central Package Management via `Directory.Packages.props`
- **Build config**: Shared via `Directory.Build.props` (TreatWarningsAsErrors in CI only)
- **Commit format**: `feat(module): description - Issue #N` / `fix(module): description - Issue #N`
- **No emojis in code** (cleaned from codebase in 2025-11-20 quality improvement)
- **XML doc warnings suppressed**: CS1591, CS1570, CS1572, CS1573, CS1587

## Important Directories

- `docs/03-architecture/` -- Architecture documentation and ADRs (8 decisions recorded)
- `docs/03-architecture/decisions/` -- Architecture Decision Records
- `docs/05-development/` -- Development guides and coding standards
- `docs/plans/` -- Active and archived design/plan documents
- `scripts/` -- All automation scripts (build, test, deploy, maintenance)
- `.serena/memories/` -- Project memory and analysis notes from prior sessions
- `openspec/` -- OpenSpec specifications (being deprecated)

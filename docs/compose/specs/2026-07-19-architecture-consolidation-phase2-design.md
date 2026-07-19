# Architecture Consolidation Phase 2 Design

## [S1] Problem

The LYBTZYZS solution has 42 .csproj projects. After Phase 1 consolidation (40 → 37 projects), several structural issues remain:

1. **Small projects in Desktop.Core**: `CardReader` (17 .cs) and `LocalData` (19 .cs) are too small to justify separate projects.
2. **Small projects in Desktop.Roles**: `Receptionist` (16 .cs) and `Sysadmin` (31 .cs) are too small to justify separate projects.
3. **Small module in Desktop.Modules**: `Reports` (12 .cs) is too small to justify a separate project.
4. **Server-side SharedKernel**: `SharedKernel` can be merged into `Infrastructure` since it contains only server-side types.
5. **Small projects in Shared**: `Primitives` (11 .cs), `Utilities` (12 .cs), and `Validators` (17 .cs) are too small to justify separate projects.

## [S2] Solution Overview

Ten consolidation operations, executed in dependency order:

| Phase | Operation | From → To | Files Touched |
|-------|-----------|-----------|---------------|
| 1a | Merge CardReader | Desktop.CardReader → Desktop.Infrastructure | ~5 using updates |
| 1b | Merge LocalData | Desktop.LocalData → Desktop.Infrastructure | ~10 using updates |
| 2a | Merge Receptionist | Desktop.Receptionist → Desktop.Clinical | ~5 using updates |
| 2b | Merge Sysadmin | Desktop.Sysadmin → Desktop.Admin | ~5 using updates |
| 3 | Merge Reports | Desktop.Reports → Desktop.MedicalCase | ~5 using updates |
| 4 | Merge SharedKernel | Server.SharedKernel → Server.Infrastructure | ~20 using updates |
| 5a | Merge Primitives | Shared.Primitives → Shared.Models | ~15 using updates |
| 5b | Merge Utilities | Shared.Utilities → Shared.Models | ~10 using updates |
| 5c | Merge Validators | Shared.Validators → Shared.Models | ~10 using updates |

**Net result**: 42 projects → 32 projects (-10). All small projects eliminated.

## [S3] Phase 1: Merge Desktop.Core Small Projects

### 1a. Merge CardReader into Infrastructure

**Source files to move (17 files):**
- `CardReader/Abstractions/*.cs`
- `CardReader/Adapters/*.cs`
- `CardReader/Integration/*.cs`
- `CardReader/Models/*.cs`
- `CardReader/Native/*.cs`
- `CardReader/Services/*.cs`
- `CardReader/CardReaderModule.cs`

**Target namespace**: `LYBT.Desktop.Infrastructure.CardReader`

**Update using statements**: ~5 files in Shell and other modules.

**Remove from solution:**
- Delete `src/Client/Desktop/Core/LYBT.Desktop.CardReader/` directory
- Remove `LYBT.Desktop.CardReader` from `LYBTZYZS.sln`

### 1b. Merge LocalData into Infrastructure

**Source files to move (19 files):**
- `LocalData/Context/*.cs`
- `LocalData/Helpers/*.cs`
- `LocalData/Initialization/*.cs`
- `LocalData/Mappers/*.cs`
- `LocalData/Services/*.cs`

**Target namespace**: `LYBT.Desktop.Infrastructure.LocalData`

**Update using statements**: ~10 files in Shell and other modules.

**Remove from solution:**
- Delete `src/Client/Desktop/Core/LYBT.Desktop.LocalData/` directory
- Remove `LYBT.Desktop.LocalData` from `LYBTZYZS.sln`

**Note**: LocalData references `LYBT.Entities` (Server project). After merging into Infrastructure, this reference must be added to Infrastructure.csproj.

## [S4] Phase 2: Merge Desktop.Roles Small Projects

### 2a. Merge Receptionist into Clinical

**Source files to move (16 files):**
- All files in `Roles/LYBT.Desktop.Receptionist/`

**Target namespace**: `LYBT.Desktop.Clinical.Receptionist`

**Update using statements**: ~5 files in Shell and other modules.

**Remove from solution:**
- Delete `src/Client/Desktop/Roles/LYBT.Desktop.Receptionist/` directory
- Remove `LYBT.Desktop.Receptionist` from `LYBTZYZS.sln`

### 2b. Merge Sysadmin into Admin

**Source files to move (31 files):**
- All files in `Roles/LYBT.Desktop.Sysadmin/`

**Target namespace**: `LYBT.Desktop.Admin.Sysadmin`

**Update using statements**: ~5 files in Shell and other modules.

**Remove from solution:**
- Delete `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/` directory
- Remove `LYBT.Desktop.Sysadmin` from `LYBTZYZS.sln`

## [S5] Phase 3: Merge Desktop.Modules Small Projects

### 3. Merge Reports into MedicalCase

**Source files to move (12 files):**
- All files in `Modules/LYBT.Desktop.Reports/`

**Target namespace**: `LYBT.Desktop.MedicalCase.Reports`

**Update using statements**: ~5 files in Shell and other modules.

**Remove from solution:**
- Delete `src/Client/Desktop/Modules/LYBT.Desktop.Reports/` directory
- Remove `LYBT.Desktop.Reports` from `LYBTZYZS.sln`

## [S6] Phase 4: Merge Server-side SharedKernel

### 4. Merge SharedKernel into Infrastructure

**Source files to move:**
- All files in `Server/Core/LYBT.SharedKernel/`

**Target namespace**: `LYBT.Infrastructure.SharedKernel`

**Update using statements**: ~20 files in Server modules.

**Remove from solution:**
- Delete `src/Server/Core/LYBT.SharedKernel/` directory
- Remove `LYBT.SharedKernel` from `LYBTZYZS.sln`

## [S7] Phase 5: Merge Shared Small Projects

### 5a. Merge Primitives into Models

**Source files to move (11 files):**
- All files in `Shared/LYBT.Shared.Primitives/`

**Target namespace**: `LYBT.Shared.Models.Primitives`

**Update using statements**: ~15 files across solution.

**Remove from solution:**
- Delete `src/Shared/LYBT.Shared.Primitives/` directory
- Remove `LYBT.Shared.Primitives` from `LYBTZYZS.sln`

### 5b. Merge Utilities into Models

**Source files to move (12 files):**
- All files in `Shared/LYBT.Shared.Utilities/`

**Target namespace**: `LYBT.Shared.Models.Utilities`

**Update using statements**: ~10 files across solution.

**Remove from solution:**
- Delete `src/Shared/LYBT.Shared.Utilities/` directory
- Remove `LYBT.Shared.Utilities` from `LYBTZYZS.sln`

### 5c. Merge Validators into Models

**Source files to move (17 files):**
- All files in `Shared/LYBT.Shared.Validators/`

**Target namespace**: `LYBT.Shared.Models.Validators`

**Update using statements**: ~10 files across solution.

**Remove from solution:**
- Delete `src/Shared/LYBT.Shared.Validators/` directory
- Remove `LYBT.Shared.Validators` from `LYBTZYZS.sln`

## [S8] Dependency Graph After Consolidation

```
Shared:
  Shared.Models (+ Primitives, + Utilities, + Validators)
  Shared.Configuration
  Shared.Logging
  Shared.ExceptionHandling

Server:
  Entities (pure domain)
  Infrastructure (+ SharedKernel, + domain events, + Entity base)
  Modules (Auth, Users, Patients, Herbs, Formula, MedicalCase, Registration, Reports)
  WebAPI (entry point)

Desktop:
  Contracts
  Foundation (HTTP, Security)
  Infrastructure (+ CardReader, + LocalData, + Navigation)
  Controls (WPF controls, themes)
  Printing
  Modules (Auth, Users, Patients, Herbs, Formula, MedicalCase, Registration)
  Roles (Admin (+ Sysadmin), Clinical (+ Receptionist))
  Shell (entry point)
```

**Key improvement**: Project count reduced from 42 to 32. All small projects eliminated. Architecture simplified.

## [S9] Verification

1. **Build**: `dotnet build LYBTZYZS.sln` — zero errors
2. **Tests**: `dotnet test tests/` — all pass
3. **Architecture tests**: `dotnet test tests/LYBT.Tests.Architecture/` — all pass
4. **Dependency check**: No circular dependencies introduced
5. **Namespace check**: All using statements updated

## [S10] Rollback Plan

If issues arise:
1. Revert git commit
2. Restore deleted projects from git history
3. Re-add projects to solution file

## [S11] Timeline

- **Phase 1**: 1-2 days (Desktop.Core consolidation)
- **Phase 2**: 1 day (Desktop.Roles consolidation)
- **Phase 3**: 0.5 day (Desktop.Modules consolidation)
- **Phase 4**: 1 day (Server.SharedKernel consolidation)
- **Phase 5**: 1 day (Shared consolidation)
- **Verification**: 1 day

**Total**: 5-6 days

## [S12] Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Circular dependencies | High | Test after each phase |
| Namespace conflicts | Medium | Use distinct namespaces |
| Build errors | Medium | Fix immediately after each merge |
| Test failures | Medium | Run tests after each phase |

## [S13] Success Criteria

1. Solution builds with zero errors
2. All tests pass
3. Project count reduced from 42 to 32
4. No circular dependencies
5. All namespaces properly organized
6. Architecture tests pass

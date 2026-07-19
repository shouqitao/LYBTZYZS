# Architecture Consolidation Design

## [S1] Problem

The LYBTZYZS solution has 40 .csproj projects. Five structural issues reduce clarity:

1. **Dependency violation**: `Shared.ExceptionHandling` and `Desktop.Contracts` reference `SharedKernel` (a Server project), breaking the Shared/Client → Server dependency rule.
2. **Three Result patterns** coexist: `SharedKernel.Result<T>`, `Shared.Models.Result` (nonexistent — only `ApiResponse<T>` exists), and `Desktop.Shared.CommandResult`. The `SharedKernel.Result<T>` is used by Desktop (19 files) and Shared.ExceptionHandling (4 files) — it should live in Shared.Models.
3. **Oversplit Desktop Core**: `Desktop.Shared` (6 .cs) and `Desktop.Navigation` (10 .cs) are too small to justify separate projects.
4. **Oversplit Shared**: `Shared.Components` (9 .cs) contains only herb-related interfaces and a validator base — it belongs in Shared.Models.
5. **SharedKernel bloat**: Contains both shared types (Result<T>) and server-only types (domain events, Entity base). After moving Result<T>, SharedKernel becomes purely server-side.

## [S2] Solution Overview

Five consolidation operations, executed in dependency order:

| Phase | Operation | From → To | Files Touched |
|-------|-----------|-----------|---------------|
| 1a | Move Result<T>/Result | SharedKernel → Shared.Models | ~23 using updates |
| 1b | Remove SharedKernel refs | ExceptionHandling + Desktop.Contracts csproj | 2 csproj edits |
| 2 | Merge Desktop.Shared | Desktop.Shared → Desktop.Contracts | ~33 using updates |
| 3 | Merge Desktop.Navigation | Desktop.Navigation → Desktop.Infrastructure | ~5 using updates |
| 4 | Merge Shared.Components | Shared.Components → Shared.Models | ~2 using updates |

**Net result**: 40 projects → 36 projects (-4). All dependency violations eliminated.

## [S3] Phase 1: Move Result<T> and Fix Dependencies

### 1a. Move Result<T>/Result from SharedKernel to Shared.Models

**Source**: `src/Server/Core/LYBT.SharedKernel/Common/Result.cs`
**Target**: `src/Shared/LYBT.Shared.Models/Contracts/Common/Result.cs`

- Move the `Result<T>` and `Result` classes
- Change namespace from `LYBT.SharedKernel.Common` to `LYBT.Shared.Models.Contracts.Common`
- Keep all methods, properties, and implicit operators identical
- Add `using LYBT.Shared.Primitives.ErrorCodes;` (already available since Shared.Models references Shared.Primitives)

### 1b. Remove SharedKernel project references

**File: `src/Shared/LYBT.Shared.ExceptionHandling/LYBT.Shared.ExceptionHandling.csproj`**
- Remove: `<ProjectReference Include="..\..\Server\Core\LYBT.SharedKernel\LYBT.SharedKernel.csproj" />`
- The ExceptionHandling project only used SharedKernel for `Result<T>`. After the move, it gets Result<T> from Shared.Models (already referenced).

**File: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/LYBT.Desktop.Contracts.csproj`**
- Remove: `<ProjectReference Include="..\..\..\..\Server\Core\LYBT.SharedKernel\LYBT.SharedKernel.csproj" />`
- Desktop.Contracts only used SharedKernel for `Result<T>`. After the move, it gets Result<T> from Shared.Models (already referenced).

### 1c. Update all `using LYBT.SharedKernel.Common` statements

**23 files total** (4 in Shared.ExceptionHandling + 19 in Desktop client):

Replace `using LYBT.Shared.Models.Contracts.Common;` with `using LYBT.Shared.Models.Contracts.Common;`

Files to update:
- `src/Shared/LYBT.Shared.ExceptionHandling/Handlers/Desktop/IDesktopExceptionHandler.cs`
- `src/Shared/LYBT.Shared.ExceptionHandling/Handlers/Desktop/DesktopExceptionHandler.cs`
- `src/Shared/LYBT.Shared.ExceptionHandling/Handlers/Server/SystemExceptionHandler.cs`
- `src/Shared/LYBT.Shared.ExceptionHandling/Handlers/Server/BusinessExceptionHandler.cs`
- `src/Client/Desktop/GlobalUsings.cs` (global using — single change covers many files)
- `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Repositories/IUserRepository.cs`
- `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Security/IAuthenticationService.cs`
- `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Security/AuthenticationService.cs`
- `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Security/ILogoutService.cs`
- `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/ApiErrorHandler.cs`
- `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/TokenRefreshHandler.cs`
- `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/HttpClientApiClient.cs`
- `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/ApiService.cs`
- `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/Clients/AuthApiClient.cs`
- `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/Clients/FormulaApiClient.cs`
- `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/Clients/HerbApiClient.cs`
- `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/Clients/MedicalCaseApiClient.cs`
- `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/Clients/PatientApiClient.cs`
- `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/Clients/RegistrationApiClient.cs`
- `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/Clients/ReportsApiClient.cs`
- `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/Clients/UserApiClient.cs`
- `src/Client/Desktop/LocalWebAPI/Repositories/HttpUserRepository.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Users/Repositories/UserRepository.cs`

**After Phase 1**: SharedKernel is only referenced by Server projects (legitimate).

## [S4] Phase 2: Merge Desktop.Shared into Desktop.Contracts

### Source files to move (6 files):

| Source | Target Namespace |
|--------|-----------------|
| `Desktop.Shared/Results/CommandResult.cs` | `LYBT.Desktop.Contracts.Results` |
| `Desktop.Shared/Models/AuthState.cs` | `LYBT.Desktop.Contracts.Models` |
| `Desktop.Shared/Models/PerformanceMetric.cs` | `LYBT.Desktop.Contracts.Models` |
| `Desktop.Shared/Models/PerformanceReport.cs` | `LYBT.Desktop.Contracts.Models` |
| `Desktop.Shared/Events/CacheEvents.cs` | `LYBT.Desktop.Contracts.Events` |
| `Desktop.Shared/Enums/UnfinishedCaseChoice.cs` | `LYBT.Desktop.Contracts.Enums` |
| `Desktop.Shared/UI/BreadcrumbItem.cs` | `LYBT.Desktop.Contracts.UI` |

### Update using statements (~33 files):

Replace `using LYBT.Desktop.Contracts.Results;` → `using LYBT.Desktop.Contracts.Results;`
Replace `using LYBT.Desktop.Contracts.Models;` → `using LYBT.Desktop.Contracts.Models;`
Replace `using LYBT.Desktop.Contracts.Events;` → `using LYBT.Desktop.Contracts.Events;`
Replace `using LYBT.Desktop.Contracts.Enums;` → `using LYBT.Desktop.Contracts.Enums;`
Replace `using LYBT.Desktop.Contracts.UI;` → `using LYBT.Desktop.Contracts.UI;`

### Remove from solution:
- Delete `src/Client/Desktop/Core/LYBT.Desktop.Shared/` directory
- Remove `LYBT.Desktop.Shared` from `LYBTZYZS.sln`
- Remove `LYBT.Desktop.Shared.csproj` reference from `LYBT.Desktop.Contracts.csproj`

### Note on CacheEvents.cs:
`CacheEvents.cs` uses `Prism.Events.PubSubEvent`. Desktop.Contracts already has a conditional Prism reference for `net8.0-windows` target, so this will work.

## [S5] Phase 3: Merge Desktop.Navigation into Desktop.Infrastructure

### Source files to move (10 files):

| Source | Target |
|--------|--------|
| `Navigation/NavigationCoordinator.cs` | `Infrastructure/Navigation/NavigationCoordinator.cs` |
| `Navigation/INavigationServices.cs` | `Infrastructure/Navigation/INavigationServices.cs` |
| `Navigation/NavigationServices.cs` | `Infrastructure/Navigation/NavigationServices.cs` |
| `Navigation/INavigationHistoryService.cs` | `Infrastructure/Navigation/INavigationHistoryService.cs` |
| `Navigation/NavigationHistoryService.cs` | `Infrastructure/Navigation/NavigationHistoryService.cs` |
| `Navigation/IRegionMonitor.cs` | `Infrastructure/Navigation/IRegionMonitor.cs` |
| `Navigation/RegionMonitor.cs` | `Infrastructure/Navigation/RegionMonitor.cs` |
| `Navigation/IModuleLazyLoader.cs` | `Infrastructure/Navigation/IModuleLazyLoader.cs` |
| `Navigation/ModuleLazyLoader.cs` | `Infrastructure/Navigation/ModuleLazyLoader.cs` |
| `Navigation/NavigationArgs/*.cs` | `Infrastructure/Navigation/NavigationArgs/*.cs` |

### Update using statements (~5 files in Shell):

Replace `using LYBT.Desktop.Infrastructure.Navigation;` → `using LYBT.Desktop.Infrastructure.Navigation;`
Replace `using LYBT.Desktop.Infrastructure.Navigation.NavigationArgs;` → `using LYBT.Desktop.Infrastructure.Navigation.NavigationArgs;`

### Remove from solution:
- Delete `src/Client/Desktop/Core/LYBT.Desktop.Navigation/` directory
- Remove `LYBT.Desktop.Navigation` from `LYBTZYZS.sln`

### Note:
Desktop.Navigation depends on Contracts + Foundation + Infrastructure. After merging into Infrastructure, the Navigation code lives in a project that already has all those dependencies. No circular dependency introduced.

## [S6] Phase 4: Merge Shared.Components into Shared.Models

### Source files to move (9 files):

| Source | Target |
|--------|--------|
| `Components/IHerbItem.cs` | `Models/Contracts/Herbs/IHerbItem.cs` |
| `Components/IHerbItemEditable.cs` | `Models/Contracts/Herbs/IHerbItemEditable.cs` |
| `Components/HerbValidatorBase.cs` | `Models/Contracts/Herbs/HerbValidatorBase.cs` |

(Plus any other files in the root)

### Update using statements (~2 files):

Replace `using LYBT.Shared.Models.Contracts.Herbs;` → `using LYBT.Shared.Models.Contracts.Herbs;`

Files:
- `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/Base/HerbItemViewModelBase.cs`
- `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/HerbItem/HerbItemControlViewModel.cs`

### Remove from solution:
- Delete `src/Shared/LYBT.Shared.Components/` directory
- Remove `LYBT.Shared.Components` from `LYBTZYZS.sln`
- Remove `LYBT.Shared.Components` reference from `LYBT.Desktop.Controls.csproj` and `LYBT.Desktop.Infrastructure.csproj`

### Note:
Shared.Components referenced Shared.Models for `HerbListDto`. After merging into Shared.Models, this becomes an internal reference (same project). No issue.

## [S7] Dependency Graph After Consolidation

```
Shared.Primitives (zero deps)
  └─→ Shared.Models (+ Result<T>, + IHerbItem, + HerbValidatorBase)
       ├─→ Shared.Configuration
       ├─→ Shared.Logging
       ├─→ Shared.ExceptionHandling (no more SharedKernel ref)
       ├─→ Shared.Validators
       └─→ Shared.Utilities

Server:
  SharedKernel (Server-only: Entity, IAggregateRoot, Domain Events)
  Entities (pure domain)
  Infrastructure (DbContext, Repository)
  Modules (Auth, Users, Patients, Herbs, Formula, MedicalCase, Registration, Reports)
  WebAPI (entry point)

Desktop:
  Contracts (+ CommandResult, AuthState, BreadcrumbItem, CacheEvents)
  Foundation (HTTP, Security)
  Infrastructure (+ NavigationCoordinator, RegionMonitor)
  Controls (WPF controls, themes)
  LocalData, Printing, CardReader
  Modules (Auth, Users, Patients, Herbs, Formula, MedicalCase, Registration, Reports)
  Roles (Admin, Clinical, Receptionist, Sysadmin)
  Shell (entry point)
```

**Key improvement**: Zero cross-tier dependency violations. SharedKernel is Server-only. Desktop no longer references any Server project.

## [S8] Verification

After each phase, run:
```bash
dotnet build LYBTZYZS.sln
dotnet test tests/LYBT.Tests.Architecture/  # Architecture guards
dotnet test tests/LYBT.Tests.Server/
dotnet test tests/LYBT.Tests.Desktop/
```

Architecture tests should catch any remaining dependency violations.

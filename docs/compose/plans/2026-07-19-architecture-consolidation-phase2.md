# Architecture Consolidation Phase 2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reduce LYBTZYZS solution from 42 to 32 projects by consolidating small projects while maintaining all functionality.

**Architecture:** Merge small projects into larger ones following dependency order: Desktop.Core → Desktop.Roles → Desktop.Modules → Server.SharedKernel → Shared projects.

**Tech Stack:** .NET 8, WPF/Prism, ASP.NET Core, EF Core, SQL Server

## Global Constraints

- All existing functionality must be preserved
- No circular dependencies introduced
- All tests must pass after each phase
- Build must succeed after each phase
- Namespaces must be properly organized
- Follow existing code patterns and conventions

---

## Phase 1: Desktop.Core Consolidation

### Task 1: Merge CardReader into Infrastructure

**Covers:** [S3]
<!-- spec section anchors this task implements; every task that produces
     spec-required behavior must list at least one. Omit only for pure
     scaffolding tasks (e.g. project setup) that map to no spec section. -->

**Files:**
- Move: `src/Client/Desktop/Core/LYBT.Desktop.CardReader/Abstractions/*.cs` → `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/CardReader/Abstractions/`
- Move: `src/Client/Desktop/Core/LYBT.Desktop.CardReader/Adapters/*.cs` → `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/CardReader/Adapters/`
- Move: `src/Client/Desktop/Core/LYBT.Desktop.CardReader/Integration/*.cs` → `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/CardReader/Integration/`
- Move: `src/Client/Desktop/Core/LYBT.Desktop.CardReader/Models/*.cs` → `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/CardReader/Models/`
- Move: `src/Client/Desktop/Core/LYBT.Desktop.CardReader/Native/*.cs` → `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/CardReader/Native/`
- Move: `src/Client/Desktop/Core/LYBT.Desktop.CardReader/Services/*.cs` → `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/CardReader/Services/`
- Move: `src/Client/Desktop/Core/LYBT.Desktop.CardReader/CardReaderModule.cs` → `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/CardReader/CardReaderModule.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/LYBT.Desktop.Infrastructure.csproj` (add CardReader references)
- Modify: `LYBTZYZS.sln` (remove CardReader project)
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.CardReader/` directory

**Interfaces:**
- Consumes: CardReader module files (17 .cs files)
- Produces: CardReader functionality available in Infrastructure project

- [ ] **Step 1: Create CardReader directory structure in Infrastructure**

```bash
mkdir -p src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/CardReader/Abstractions
mkdir -p src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/CardReader/Adapters
mkdir -p src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/CardReader/Integration
mkdir -p src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/CardReader/Models
mkdir -p src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/CardReader/Native
mkdir -p src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/CardReader/Services
```

- [ ] **Step 2: Move CardReader files to Infrastructure**

```bash
# Move all CardReader files to Infrastructure
Move-Item -Path "src/Client/Desktop/Core/LYBT.Desktop.CardReader/Abstractions/*" -Destination "src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/CardReader/Abstractions/"
Move-Item -Path "src/Client/Desktop/Core/LYBT.Desktop.CardReader/Adapters/*" -Destination "src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/CardReader/Adapters/"
Move-Item -Path "src/Client/Desktop/Core/LYBT.Desktop.CardReader/Integration/*" -Destination "src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/CardReader/Integration/"
Move-Item -Path "src/Client/Desktop/Core/LYBT.Desktop.CardReader/Models/*" -Destination "src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/CardReader/Models/"
Move-Item -Path "src/Client/Desktop/Core/LYBT.Desktop.CardReader/Native/*" -Destination "src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/CardReader/Native/"
Move-Item -Path "src/Client/Desktop/Core/LYBT.Desktop.CardReader/Services/*" -Destination "src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/CardReader/Services/"
Move-Item -Path "src/Client/Desktop/Core/LYBT.Desktop.CardReader/CardReaderModule.cs" -Destination "src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/CardReader/"
```

- [ ] **Step 3: Update namespaces in moved files**

For each moved .cs file, replace namespace:
```csharp
// Old namespace
namespace LYBT.Desktop.CardReader
// New namespace
namespace LYBT.Desktop.Infrastructure.CardReader
```

- [ ] **Step 4: Update Infrastructure.csproj references**

Add to `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/LYBT.Desktop.Infrastructure.csproj`:
```xml
<ItemGroup>
  <!-- CardReader dependencies -->
  <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
  <PackageReference Include="System.IO.Ports" Version="8.0.0" />
</ItemGroup>
```

- [ ] **Step 5: Update using statements in Shell and modules**

Search for `using LYBT.Desktop.CardReader` and replace with `using LYBT.Desktop.Infrastructure.CardReader`.

- [ ] **Step 6: Remove CardReader project from solution**

```bash
dotnet sln LYBTZYZS.sln remove src/Client/Desktop/Core/LYBT.Desktop.CardReader/LYBT.Desktop.CardReader.csproj
```

- [ ] **Step 7: Delete CardReader directory**

```bash
Remove-Item -Recurse -Force "src/Client/Desktop/Core/LYBT.Desktop.CardReader"
```

- [ ] **Step 8: Verify build**

```bash
dotnet build LYBTZYZS.sln
```

Expected: Build succeeds with zero errors.

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "refactor(Desktop): merge CardReader into Infrastructure project"
```

### Task 2: Merge LocalData into Infrastructure

**Covers:** [S3]

**Files:**
- Move: `src/Client/Desktop/Core/LYBT.Desktop.LocalData/Context/*.cs` → `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/LocalData/Context/`
- Move: `src/Client/Desktop/Core/LYBT.Desktop.LocalData/Helpers/*.cs` → `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/LocalData/Helpers/`
- Move: `src/Client/Desktop/Core/LYBT.Desktop.LocalData/Initialization/*.cs` → `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/LocalData/Initialization/`
- Move: `src/Client/Desktop/Core/LYBT.Desktop.LocalData/Mappers/*.cs` → `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/LocalData/Mappers/`
- Move: `src/Client/Desktop/Core/LYBT.Desktop.LocalData/Services/*.cs` → `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/LocalData/Services/`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/LYBT.Desktop.Infrastructure.csproj` (add LocalData references)
- Modify: `LYBTZYZS.sln` (remove LocalData project)
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.LocalData/` directory

**Interfaces:**
- Consumes: LocalData module files (19 .cs files)
- Produces: LocalData functionality available in Infrastructure project

- [ ] **Step 1: Create LocalData directory structure in Infrastructure**

```bash
mkdir -p src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/LocalData/Context
mkdir -p src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/LocalData/Helpers
mkdir -p src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/LocalData/Initialization
mkdir -p src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/LocalData/Mappers
mkdir -p src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/LocalData/Services
```

- [ ] **Step 2: Move LocalData files to Infrastructure**

```bash
Move-Item -Path "src/Client/Desktop/Core/LYBT.Desktop.LocalData/Context/*" -Destination "src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/LocalData/Context/"
Move-Item -Path "src/Client/Desktop/Core/LYBT.Desktop.LocalData/Helpers/*" -Destination "src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/LocalData/Helpers/"
Move-Item -Path "src/Client/Desktop/Core/LYBT.Desktop.LocalData/Initialization/*" -Destination "src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/LocalData/Initialization/"
Move-Item -Path "src/Client/Desktop/Core/LYBT.Desktop.LocalData/Mappers/*" -Destination "src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/LocalData/Mappers/"
Move-Item -Path "src/Client/Desktop/Core/LYBT.Desktop.LocalData/Services/*" -Destination "src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/LocalData/Services/"
```

- [ ] **Step 3: Update namespaces in moved files**

For each moved .cs file, replace namespace:
```csharp
// Old namespace
namespace LYBT.Desktop.LocalData
// New namespace
namespace LYBT.Desktop.Infrastructure.LocalData
```

- [ ] **Step 4: Update Infrastructure.csproj references**

Add to `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/LYBT.Desktop.Infrastructure.csproj`:
```xml
<ItemGroup>
  <!-- LocalData dependencies -->
  <ProjectReference Include="..\..\..\Server\Core\LYBT.Entities\LYBT.Entities.csproj" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.0" />
</ItemGroup>
```

- [ ] **Step 5: Update using statements in Shell and modules**

Search for `using LYBT.Desktop.LocalData` and replace with `using LYBT.Desktop.Infrastructure.LocalData`.

- [ ] **Step 6: Remove LocalData project from solution**

```bash
dotnet sln LYBTZYZS.sln remove src/Client/Desktop/Core/LYBT.Desktop.LocalData/LYBT.Desktop.LocalData.csproj
```

- [ ] **Step 7: Delete LocalData directory**

```bash
Remove-Item -Recurse -Force "src/Client/Desktop/Core/LYBT.Desktop.LocalData"
```

- [ ] **Step 8: Verify build**

```bash
dotnet build LYBTZYZS.sln
```

Expected: Build succeeds with zero errors.

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "refactor(Desktop): merge LocalData into Infrastructure project"
```

---

## Phase 2: Desktop.Roles Consolidation

### Task 3: Merge Receptionist into Clinical

**Covers:** [S4]

**Files:**
- Move: `src/Client/Desktop/Roles/LYBT.Desktop.Receptionist/*` → `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Receptionist/`
- Modify: `LYBTZYZS.sln` (remove Receptionist project)
- Delete: `src/Client/Desktop/Roles/LYBT.Desktop.Receptionist/` directory

**Interfaces:**
- Consumes: Receptionist role files (16 .cs files)
- Produces: Receptionist functionality available in Clinical project

- [ ] **Step 1: Create Receptionist directory in Clinical**

```bash
mkdir -p src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Receptionist
```

- [ ] **Step 2: Move Receptionist files to Clinical**

```bash
Move-Item -Path "src/Client/Desktop/Roles/LYBT.Desktop.Receptionist/*" -Destination "src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Receptionist/"
```

- [ ] **Step 3: Update namespaces in moved files**

For each moved .cs file, replace namespace:
```csharp
// Old namespace
namespace LYBT.Desktop.Receptionist
// New namespace
namespace LYBT.Desktop.Clinical.Receptionist
```

- [ ] **Step 4: Update using statements in Shell and modules**

Search for `using LYBT.Desktop.Receptionist` and replace with `using LYBT.Desktop.Clinical.Receptionist`.

- [ ] **Step 5: Remove Receptionist project from solution**

```bash
dotnet sln LYBTZYZS.sln remove src/Client/Desktop/Roles/LYBT.Desktop.Receptionist/LYBT.Desktop.Receptionist.csproj
```

- [ ] **Step 6: Delete Receptionist directory**

```bash
Remove-Item -Recurse -Force "src/Client/Desktop/Roles/LYBT.Desktop.Receptionist"
```

- [ ] **Step 7: Verify build**

```bash
dotnet build LYBTZYZS.sln
```

Expected: Build succeeds with zero errors.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "refactor(Desktop): merge Receptionist role into Clinical project"
```

### Task 4: Merge Sysadmin into Admin

**Covers:** [S4]

**Files:**
- Move: `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/*` → `src/Client/Desktop/Roles/LYBT.Desktop.Admin/Sysadmin/`
- Modify: `LYBTZYZS.sln` (remove Sysadmin project)
- Delete: `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/` directory

**Interfaces:**
- Consumes: Sysadmin role files (31 .cs files)
- Produces: Sysadmin functionality available in Admin project

- [ ] **Step 1: Create Sysadmin directory in Admin**

```bash
mkdir -p src/Client/Desktop/Roles/LYBT.Desktop.Admin/Sysadmin
```

- [ ] **Step 2: Move Sysadmin files to Admin**

```bash
Move-Item -Path "src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/*" -Destination "src/Client/Desktop/Roles/LYBT.Desktop.Admin/Sysadmin/"
```

- [ ] **Step 3: Update namespaces in moved files**

For each moved .cs file, replace namespace:
```csharp
// Old namespace
namespace LYBT.Desktop.Sysadmin
// New namespace
namespace LYBT.Desktop.Admin.Sysadmin
```

- [ ] **Step 4: Update using statements in Shell and modules**

Search for `using LYBT.Desktop.Sysadmin` and replace with `using LYBT.Desktop.Admin.Sysadmin`.

- [ ] **Step 5: Remove Sysadmin project from solution**

```bash
dotnet sln LYBTZYZS.sln remove src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/LYBT.Desktop.Sysadmin.csproj
```

- [ ] **Step 6: Delete Sysadmin directory**

```bash
Remove-Item -Recurse -Force "src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin"
```

- [ ] **Step 7: Verify build**

```bash
dotnet build LYBTZYZS.sln
```

Expected: Build succeeds with zero errors.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "refactor(Desktop): merge Sysadmin role into Admin project"
```

---

## Phase 3: Desktop.Modules Consolidation

### Task 5: Merge Reports into MedicalCase

**Covers:** [S5]

**Files:**
- Move: `src/Client/Desktop/Modules/LYBT.Desktop.Reports/*` → `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Reports/`
- Modify: `LYBTZYZS.sln` (remove Reports project)
- Delete: `src/Client/Desktop/Modules/LYBT.Desktop.Reports/` directory

**Interfaces:**
- Consumes: Reports module files (12 .cs files)
- Produces: Reports functionality available in MedicalCase project

- [ ] **Step 1: Create Reports directory in MedicalCase**

```bash
mkdir -p src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Reports
```

- [ ] **Step 2: Move Reports files to MedicalCase**

```bash
Move-Item -Path "src/Client/Desktop/Modules/LYBT.Desktop.Reports/*" -Destination "src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Reports/"
```

- [ ] **Step 3: Update namespaces in moved files**

For each moved .cs file, replace namespace:
```csharp
// Old namespace
namespace LYBT.Desktop.Reports
// New namespace
namespace LYBT.Desktop.MedicalCase.Reports
```

- [ ] **Step 4: Update using statements in Shell and modules**

Search for `using LYBT.Desktop.Reports` and replace with `using LYBT.Desktop.MedicalCase.Reports`.

- [ ] **Step 5: Remove Reports project from solution**

```bash
dotnet sln LYBTZYZS.sln remove src/Client/Desktop/Modules/LYBT.Desktop.Reports/LYBT.Desktop.Reports.csproj
```

- [ ] **Step 6: Delete Reports directory**

```bash
Remove-Item -Recurse -Force "src/Client/Desktop/Modules/LYBT.Desktop.Reports"
```

- [ ] **Step 7: Verify build**

```bash
dotnet build LYBTZYZS.sln
```

Expected: Build succeeds with zero errors.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "refactor(Desktop): merge Reports module into MedicalCase project"
```

---

## Phase 4: Server.SharedKernel Consolidation

### Task 6: Merge SharedKernel into Infrastructure

**Covers:** [S6]

**Files:**
- Move: `src/Server/Core/LYBT.SharedKernel/*` → `src/Server/Core/LYBT.Infrastructure/SharedKernel/`
- Modify: `LYBTZYZS.sln` (remove SharedKernel project)
- Delete: `src/Server/Core/LYBT.SharedKernel/` directory

**Interfaces:**
- Consumes: SharedKernel files (domain events, Entity base, etc.)
- Produces: SharedKernel functionality available in Infrastructure project

- [ ] **Step 1: Create SharedKernel directory in Infrastructure**

```bash
mkdir -p src/Server/Core/LYBT.Infrastructure/SharedKernel
```

- [ ] **Step 2: Move SharedKernel files to Infrastructure**

```bash
Move-Item -Path "src/Server/Core/LYBT.SharedKernel/*" -Destination "src/Server/Core/LYBT.Infrastructure/SharedKernel/"
```

- [ ] **Step 3: Update namespaces in moved files**

For each moved .cs file, replace namespace:
```csharp
// Old namespace
namespace LYBT.SharedKernel
// New namespace
namespace LYBT.Infrastructure.SharedKernel
```

- [ ] **Step 4: Update using statements in Server modules**

Search for `using LYBT.SharedKernel` and replace with `using LYBT.Infrastructure.SharedKernel`.

- [ ] **Step 5: Remove SharedKernel project from solution**

```bash
dotnet sln LYBTZYZS.sln remove src/Server/Core/LYBT.SharedKernel/LYBT.SharedKernel.csproj
```

- [ ] **Step 6: Delete SharedKernel directory**

```bash
Remove-Item -Recurse -Force "src/Server/Core/LYBT.SharedKernel"
```

- [ ] **Step 7: Verify build**

```bash
dotnet build LYBTZYZS.sln
```

Expected: Build succeeds with zero errors.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "refactor(Server): merge SharedKernel into Infrastructure project"
```

---

## Phase 5: Shared Projects Consolidation

### Task 7: Merge Primitives into Models

**Covers:** [S7]

**Files:**
- Move: `src/Shared/LYBT.Shared.Primitives/*` → `src/Shared/LYBT.Shared.Models/Primitives/`
- Modify: `LYBTZYZS.sln` (remove Primitives project)
- Delete: `src/Shared/LYBT.Shared.Primitives/` directory

**Interfaces:**
- Consumes: Primitives files (11 .cs files)
- Produces: Primitives functionality available in Models project

- [ ] **Step 1: Create Primitives directory in Models**

```bash
mkdir -p src/Shared/LYBT.Shared.Models/Primitives
```

- [ ] **Step 2: Move Primitives files to Models**

```bash
Move-Item -Path "src/Shared/LYBT.Shared.Primitives/*" -Destination "src/Shared/LYBT.Shared.Models/Primitives/"
```

- [ ] **Step 3: Update namespaces in moved files**

For each moved .cs file, replace namespace:
```csharp
// Old namespace
namespace LYBT.Shared.Primitives
// New namespace
namespace LYBT.Shared.Models.Primitives
```

- [ ] **Step 4: Update using statements across solution**

Search for `using LYBT.Shared.Primitives` and replace with `using LYBT.Shared.Models.Primitives`.

- [ ] **Step 5: Remove Primitives project from solution**

```bash
dotnet sln LYBTZYZS.sln remove src/Shared/LYBT.Shared.Primitives/LYBT.Shared.Primitives.csproj
```

- [ ] **Step 6: Delete Primitives directory**

```bash
Remove-Item -Recurse -Force "src/Shared/LYBT.Shared.Primitives"
```

- [ ] **Step 7: Verify build**

```bash
dotnet build LYBTZYZS.sln
```

Expected: Build succeeds with zero errors.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "refactor(Shared): merge Primitives into Models project"
```

### Task 8: Merge Utilities into Models

**Covers:** [S7]

**Files:**
- Move: `src/Shared/LYBT.Shared.Utilities/*` → `src/Shared/LYBT.Shared.Models/Utilities/`
- Modify: `LYBTZYZS.sln` (remove Utilities project)
- Delete: `src/Shared/LYBT.Shared.Utilities/` directory

**Interfaces:**
- Consumes: Utilities files (12 .cs files)
- Produces: Utilities functionality available in Models project

- [ ] **Step 1: Create Utilities directory in Models**

```bash
mkdir -p src/Shared/LYBT.Shared.Models/Utilities
```

- [ ] **Step 2: Move Utilities files to Models**

```bash
Move-Item -Path "src/Shared/LYBT.Shared.Utilities/*" -Destination "src/Shared/LYBT.Shared.Models/Utilities/"
```

- [ ] **Step 3: Update namespaces in moved files**

For each moved .cs file, replace namespace:
```csharp
// Old namespace
namespace LYBT.Shared.Utilities
// New namespace
namespace LYBT.Shared.Models.Utilities
```

- [ ] **Step 4: Update using statements across solution**

Search for `using LYBT.Shared.Utilities` and replace with `using LYBT.Shared.Models.Utilities`.

- [ ] **Step 5: Remove Utilities project from solution**

```bash
dotnet sln LYBTZYZS.sln remove src/Shared/LYBT.Shared.Utilities/LYBT.Shared.Utilities.csproj
```

- [ ] **Step 6: Delete Utilities directory**

```bash
Remove-Item -Recurse -Force "src/Shared/LYBT.Shared.Utilities"
```

- [ ] **Step 7: Verify build**

```bash
dotnet build LYBTZYZS.sln
```

Expected: Build succeeds with zero errors.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "refactor(Shared): merge Utilities into Models project"
```

### Task 9: Merge Validators into Models

**Covers:** [S7]

**Files:**
- Move: `src/Shared/LYBT.Shared.Validators/*` → `src/Shared/LYBT.Shared.Models/Validators/`
- Modify: `LYBTZYZS.sln` (remove Validators project)
- Delete: `src/Shared/LYBT.Shared.Validators/` directory

**Interfaces:**
- Consumes: Validators files (17 .cs files)
- Produces: Validators functionality available in Models project

- [ ] **Step 1: Create Validators directory in Models**

```bash
mkdir -p src/Shared/LYBT.Shared.Models/Validators
```

- [ ] **Step 2: Move Validators files to Models**

```bash
Move-Item -Path "src/Shared/LYBT.Shared.Validators/*" -Destination "src/Shared/LYBT.Shared.Models/Validators/"
```

- [ ] **Step 3: Update namespaces in moved files**

For each moved .cs file, replace namespace:
```csharp
// Old namespace
namespace LYBT.Shared.Validators
// New namespace
namespace LYBT.Shared.Models.Validators
```

- [ ] **Step 4: Update using statements across solution**

Search for `using LYBT.Shared.Validators` and replace with `using LYBT.Shared.Models.Validators`.

- [ ] **Step 5: Remove Validators project from solution**

```bash
dotnet sln LYBTZYZS.sln remove src/Shared/LYBT.Shared.Validators/LYBT.Shared.Validators.csproj
```

- [ ] **Step 6: Delete Validators directory**

```bash
Remove-Item -Recurse -Force "src/Shared/LYBT.Shared.Validators"
```

- [ ] **Step 7: Verify build**

```bash
dotnet build LYBTZYZS.sln
```

Expected: Build succeeds with zero errors.

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "refactor(Shared): merge Validators into Models project"
```

---

## Final Verification

### Task 10: Final Verification and Cleanup

**Covers:** [S9]

**Files:**
- Modify: `LYBTZYZS.sln` (verify all projects removed)
- Modify: `docs/AGENTS.md` (update project count)

**Interfaces:**
- Consumes: All previous tasks completed
- Produces: Clean solution with 32 projects

- [ ] **Step 1: Verify project count**

```bash
dotnet sln list | Measure-Object -Line
```

Expected: 32 projects (plus header line = 33 lines).

- [ ] **Step 2: Run full build**

```bash
dotnet build LYBTZYZS.sln
```

Expected: Build succeeds with zero errors.

- [ ] **Step 3: Run all tests**

```bash
dotnet test tests/
```

Expected: All tests pass.

- [ ] **Step 4: Run architecture tests**

```bash
dotnet test tests/LYBT.Tests.Architecture/
```

Expected: All architecture tests pass.

- [ ] **Step 5: Update documentation**

Update `docs/AGENTS.md` to reflect new project count (32 projects).

- [ ] **Step 6: Commit final verification**

```bash
git add -A
git commit -m "docs(Architecture): update project count to 32 after Phase 2 consolidation"
```

---

## Summary

**Total Tasks:** 10
**Estimated Time:** 5-6 days
**Project Count Reduction:** 42 → 32 projects (-10)

**Phase Breakdown:**
- Phase 1: 2 tasks (Desktop.Core consolidation)
- Phase 2: 2 tasks (Desktop.Roles consolidation)
- Phase 3: 1 task (Desktop.Modules consolidation)
- Phase 4: 1 task (Server.SharedKernel consolidation)
- Phase 5: 3 tasks (Shared consolidation)
- Final Verification: 1 task

**Key Success Factors:**
1. Follow dependency order (merge smaller into larger)
2. Update namespaces consistently
3. Verify build after each task
4. Run tests after each phase
5. Commit after each successful task

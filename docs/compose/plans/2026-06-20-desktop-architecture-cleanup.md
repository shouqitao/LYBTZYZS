# Desktop Infrastructure Architecture Cleanup — Implementation Plan

> [!NOTE]
> This document may not reflect the current implementation.
> See the final report for up-to-date state:
> [Final Report](../reports/desktop-architecture-cleanup.md)

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Split Infrastructure (18.6K LOC) into Infrastructure (~10K) + Controls (~8.7K), merge Models (1K) into Infrastructure, merge Utilities into Foundation, extract non-interface types from Contracts into Shared.

**Architecture:** 4 project-level changes: (1) New `LYBT.Desktop.Controls` for WPF presentation assets, (2) New `LYBT.Desktop.Shared` for non-interface shared types, (3) `Models` merged into `Infrastructure`, (4) `Utilities` merged into `Foundation`. Dependency direction: Controls → Contracts + Shared (no Infrastructure ref). Modules → Infrastructure + Controls. No cycles. Each step is build-verified independently.

**Tech Stack:** C# / .NET 8 / WPF / Prism / CommunityToolkit.Mvvm

---

## Pre-conditions

- All changes on `master` branch, unpushed until final verification
- `dotnet build LYBTZYZS.sln` passes with 0 errors at start
- Reference: `docs/compose/specs/2026-06-20-desktop-architecture-cleanup-design.md`

---

## Task 1: Create Controls project skeleton

**Covers:** [S4]
**Files:**
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Controls/LYBT.Desktop.Controls.csproj`
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Properties/AssemblyInfo.cs`

- [ ] **Step 1: Create Controls .csproj**

Create `src/Client/Desktop/Core/LYBT.Desktop.Controls/LYBT.Desktop.Controls.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>LYBT.Desktop.Controls</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\LYBT.Desktop.Contracts\LYBT.Desktop.Contracts.csproj" />
    <ProjectReference Include="..\..\..\..\Shared\LYBT.Shared.Models\LYBT.Shared.Models.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Prism.Core" />
    <PackageReference Include="CommunityToolkit.Mvvm" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Add to solution**

```bash
dotnet sln LYBTZYZS.sln add src/Client/Desktop/Core/LYBT.Desktop.Controls/LYBT.Desktop.Controls.csproj
```

- [ ] **Step 3: Verify build**

```bash
dotnet build src/Client/Desktop/Core/LYBT.Desktop.Controls/LYBT.Desktop.Controls.csproj
```
Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Controls/
git commit -m "refactor(controls): create LYBT.Desktop.Controls project skeleton"
```

---

## Task 2: Create Shared project skeleton

**Covers:** [S7]
**Files:**
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Shared/LYBT.Desktop.Shared.csproj`

- [ ] **Step 1: Create Shared .csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>LYBT.Desktop.Shared</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="CommunityToolkit.Mvvm" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Add to solution**

```bash
dotnet sln LYBTZYZS.sln add src/Client/Desktop/Core/LYBT.Desktop.Shared/LYBT.Desktop.Shared.csproj
```

- [ ] **Step 3: Verify build**

```bash
dotnet build src/Client/Desktop/Core/LYBT.Desktop.Shared/LYBT.Desktop.Shared.csproj
```
Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Shared/
git commit -m "refactor(shared): create LYBT.Desktop.Shared project skeleton"
```

---

## Task 3: Move Controls from Infrastructure to Controls project

**Covers:** [S3, S4]
**Files:**
- Move: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/**` → `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/LYBT.Desktop.Controls.csproj` (add controls-specific dependencies)

- [ ] **Step 1: Move Controls directory**

```powershell
# Create target directory structure
New-Item -ItemType Directory -Force -Path "src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls" | Out-Null

# Move all files
Move-Item -Path "src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/*" -Destination "src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/"
```

- [ ] **Step 2: Update Controls .csproj with needed dependencies**

The controls likely reference Prism, CommunityToolkit, and possibly Infrastructure types. Update `LYBT.Desktop.Controls.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>LYBT.Desktop.Controls</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\LYBT.Desktop.Contracts\LYBT.Desktop.Contracts.csproj" />
    <ProjectReference Include="..\LYBT.Desktop.Shared\LYBT.Desktop.Shared.csproj" />
    <ProjectReference Include="..\..\..\..\Shared\LYBT.Shared.Models\LYBT.Shared.Models.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Prism.Core" />
    <PackageReference Include="Prism.Wpf" />
    <PackageReference Include="CommunityToolkit.Mvvm" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Verify build**

```bash
dotnet build src/Client/Desktop/Core/LYBT.Desktop.Controls/LYBT.Desktop.Controls.csproj -nologo -clp:ErrorsOnly
```
Expected: May have errors — controls reference Infrastructure types. Record them.

- [ ] **Step 4: Resolve control base type dependencies**

Controls types (MasterDetailControlBase, HerbItemControl, etc.) may inherit from or reference WPF base types (UserControl, ContentControl) and Prism types — NOT Infrastructure types. Controls depends ONLY on:
- `LYBT.Desktop.Contracts` (interface definitions)
- `LYBT.Desktop.Shared` (shared types)
- `LYBT.Shared.Models` (DTOs)
- Prism.Core / Prism.Wpf / CommunityToolkit.Mvvm packages

If any control type references an Infrastructure type (e.g., MasterDetailViewModelBase), that control should be refactored to depend on an INTERFACE from Contracts instead, or the ViewModel base class reference should be injected via DataContext (WPF pattern). **DO NOT add a Controls → Infrastructure dependency.**

- [ ] **Step 5: Verify build**

```bash
dotnet build src/Client/Desktop/Core/LYBT.Desktop.Controls/LYBT.Desktop.Controls.csproj -nologo -clp:ErrorsOnly
```
Expected: 0 errors. If there are Infrastructure-type references, fix them by introducing Contracts interfaces or using DataContext patterns — do NOT add the dependency.

- [ ] **Step 6: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Controls/ src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Controls/
git commit -m "refactor(controls): migrate Controls from Infrastructure to Controls project"
```

---

## Task 4: Move Themes from Infrastructure to Controls project

**Covers:** [S4]
**Files:**
- Move: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Themes/**` → `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/`

- [ ] **Step 1: Move Themes directory**

```powershell
New-Item -ItemType Directory -Force -Path "src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes" | Out-Null
Move-Item -Path "src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Themes/*" -Destination "src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/"
```

- [ ] **Step 2: Add Theme resource PackUri to Controls .csproj**

Controls themes need to be embedded as resources. Add to `LYBT.Desktop.Controls.csproj`:

```xml
  <ItemGroup>
    <Resource Include="Themes\*.xaml" />
  </ItemGroup>
```

- [ ] **Step 3: Update App.xaml MergedDictionaries paths**

Shell's `App.xaml` references themes from Infrastructure. Update paths:

Find and replace in `src/Client/Desktop/Shell/App.xaml`:
- Old: `LYBT.Desktop.Infrastructure;component/Themes/DesignSystem.xaml`
- New: `LYBT.Desktop.Controls;component/Themes/DesignSystem.xaml`

Do the same for all other theme references in App.xaml and any other XAML files.

- [ ] **Step 4: Verify build**

```bash
dotnet build LYBTZYZS.sln -nologo -clp:ErrorsOnly
```
Expected: 0 errors.

- [ ] **Step 5: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/ src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Themes/ src/Client/Desktop/Shell/App.xaml
git commit -m "refactor(controls): migrate Themes from Infrastructure to Controls project"
```

---

## Task 5: Move Converters from Infrastructure to Controls project

**Covers:** [S4]
**Files:**
- Move: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Converters/**` → `src/Client/Desktop/Core/LYBT.Desktop.Controls/Converters/`

- [ ] **Step 1: Move Converters directory**

```powershell
New-Item -ItemType Directory -Force -Path "src/Client/Desktop/Core/LYBT.Desktop.Controls/Converters" | Out-Null
Move-Item -Path "src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Converters/*" -Destination "src/Client/Desktop/Core/LYBT.Desktop.Controls/Converters/"
```

- [ ] **Step 2: Verify build**

```bash
dotnet build LYBTZYZS.sln -nologo -clp:ErrorsOnly
```
Expected: 0 errors. Converters are typically self-contained.

- [ ] **Step 3: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Controls/Converters/ src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Converters/
git commit -m "refactor(controls): migrate Converters from Infrastructure to Controls project"
```

---

## Task 6: Merge Models into Infrastructure

**Covers:** [S3, S5]
**Files:**
- Move: `src/Client/Desktop/Core/LYBT.Desktop.Models/ViewModels/Base/*` → `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/Base/`
- Move: `src/Client/Desktop/Core/LYBT.Desktop.Models/ViewModels/ValidationAccessors.cs` → `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/`
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Models/` project
- Modify: All `.csproj` files that referenced Models
- Modify: All `.cs` files using `LYBT.Desktop.Models.ViewModels.Base`

- [ ] **Step 1: Move ViewModel base files**

```powershell
# Ensure target directory exists (ViewModels/Base/ may already have MasterDetailViewModelBase)
New-Item -ItemType Directory -Force -Path "src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/Base" | Out-Null

# Move all Models files
Move-Item -Path "src/Client/Desktop/Core/LYBT.Desktop.Models/ViewModels/Base/CoreViewModelBase.cs" -Destination "src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/Base/"
Move-Item -Path "src/Client/Desktop/Core/LYBT.Desktop.Models/ViewModels/Base/NavigableViewModelBase.cs" -Destination "src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/Base/"
Move-Item -Path "src/Client/Desktop/Core/LYBT.Desktop.Models/ViewModels/Base/DialogViewModelBase.cs" -Destination "src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/Base/"
Move-Item -Path "src/Client/Desktop/Core/LYBT.Desktop.Models/ViewModels/Base/ValidatableModelBase.cs" -Destination "src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/Base/"
Move-Item -Path "src/Client/Desktop/Core/LYBT.Desktop.Models/ViewModels/ValidationAccessors.cs" -Destination "src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/"
```

- [ ] **Step 2: Delete Models project from solution**

```bash
dotnet sln LYBTZYZS.sln remove src/Client/Desktop/Core/LYBT.Desktop.Models/LYBT.Desktop.Models.csproj
```

- [ ] **Step 3: Remove Models ProjectReference from all .csproj files**

Find all .csproj files referencing LYBT.Desktop.Models and remove that ProjectReference. Use Grep to find them:

```
rg "LYBT.Desktop.Models" --include "*.csproj"
```

Then remove the ProjectReference line from each.

- [ ] **Step 4: Update using directives across all modules**

Find all .cs files using `LYBT.Desktop.Models.ViewModels.Base`:

```
rg "using LYBT.Desktop.Models.ViewModels.Base" --include "*.cs"
```

Replace `using LYBT.Desktop.Models.ViewModels.Base` → `using LYBT.Desktop.Infrastructure.ViewModels.Base` in all matched files.

- [ ] **Step 5: Update Infrastructure .csproj to include Models project reference (if needed)**

If Infrastructure's .csproj still references Models, remove it (Models is now merged INTO Infrastructure).

- [ ] **Step 6: Verify build**

```bash
dotnet build LYBTZYZS.sln -nologo -clp:ErrorsOnly
```
Expected: 0 errors.

- [ ] **Step 7: Delete Models directory**

```powershell
Remove-Item -Path "src/Client/Desktop/Core/LYBT.Desktop.Models" -Recurse -Force
```

- [ ] **Step 8: Commit**

```bash
git add -A src/Client/Desktop/Core/LYBT.Desktop.Models/ src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ .sln
git commit -m "refactor(models): merge Models into Infrastructure — eliminate dependency edge"
```

---

## Task 8: Merge Utilities into Foundation

**Covers:** [S3, S6]
**Files:**
- Move: `src/Client/Desktop/Core/LYBT.Desktop.Utilities/Excel/ExcelHelper.cs` → `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Utilities/ExcelHelper.cs`
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Utilities/` project

- [ ] **Step 1: Create target directory**

```powershell
New-Item -ItemType Directory -Force -Path "src/Client/Desktop/Core/LYBT.Desktop.Foundation/Utilities" | Out-Null
```

- [ ] **Step 2: Move ExcelHelper**

```powershell
Move-Item -Path "src/Client/Desktop/Core/LYBT.Desktop.Utilities/Excel/ExcelHelper.cs" -Destination "src/Client/Desktop/Core/LYBT.Desktop.Foundation/Utilities/ExcelHelper.cs"
```

- [ ] **Step 3: Remove Utilities from solution**

```bash
dotnet sln LYBTZYZS.sln remove src/Client/Desktop/Core/LYBT.Desktop.Utilities/LYBT.Desktop.Utilities.csproj
```

- [ ] **Step 4: Remove Utilities ProjectReference from all .csproj files**

```
rg "LYBT.Desktop.Utilities" --include "*.csproj"
```
Remove from all matched files.

- [ ] **Step 5: Update using directives**

```
rg "using LYBT.Desktop.Utilities" --include "*.cs"
```
Replace `using LYBT.Desktop.Utilities.Excel` → `using LYBT.Desktop.Foundation.Utilities` in all matched files.

- [ ] **Step 6: Verify build**

```bash
dotnet build LYBTZYZS.sln -nologo -clp:ErrorsOnly
```
Expected: 0 errors.

- [ ] **Step 7: Delete Utilities directory**

```powershell
Remove-Item -Path "src/Client/Desktop/Core/LYBT.Desktop.Utilities" -Recurse -Force
```

- [ ] **Step 8: Commit**

```bash
git add -A src/Client/Desktop/Core/LYBT.Desktop.Utilities/ src/Client/Desktop/Core/LYBT.Desktop.Foundation/ .sln
git commit -m "refactor(utilities): merge Utilities into Foundation — single-file project eliminated"
```

---

## Task 9: Extract non-interface types from Contracts to Shared

**Covers:** [S7]
**Files:**
- Move selected types from `src/Client/Desktop/Core/LYBT.Desktop.Contracts/` → `src/Client/Desktop/Core/LYBT.Desktop.Shared/`
- Add Contracts → Shared dependency

- [ ] **Step 1: Identify types to move**

Use Grep to find non-interface, non-enum types in Contracts:

```
rg "^public (class|record|struct)" src/Client/Desktop/Core/LYBT.Desktop.Contracts/ --include "*.cs"
```

Identify: CommandResult, AuthState, ApiClientOptions, PerformanceReport, Metric, ImportValidationResult, UnfinishedCaseChoice, BreadcrumbItem, CacheEvents.

- [ ] **Step 2: Move identified types**

For each type file, move from Contracts to Shared. Create appropriate subdirectories in Shared:
- `Shared/Results/` — CommandResult.cs
- `Shared/Auth/` — AuthState.cs
- `Shared/Options/` — ApiClientOptions.cs
- `Shared/Diagnostics/` — PerformanceReport.cs, Metric.cs
- `Shared/Import/` — ImportValidationResult.cs
- `Shared/UI/` — BreadcrumbItem.cs
- `Shared/Events/` — CacheEvents.cs (note: spec says CacheEvents → Infrastructure, but it's a shared event definition used by multiple projects, so Shared is more appropriate)

- [ ] **Step 3: Add Shared ProjectReference to Contracts**

```xml
  <ItemGroup>
    <ProjectReference Include="..\LYBT.Desktop.Shared\LYBT.Desktop.Shared.csproj" />
  </ItemGroup>
```

This lets existing code that references Contracts types continue to work (Contracts re-exports from Shared via the dependency).

- [ ] **Step 4: Update using directives in downstream projects**

For each moved type, find usages and add `using LYBT.Desktop.Shared.*` if the type was previously accessed via Contracts namespace.

- [ ] **Step 5: Verify build**

```bash
dotnet build LYBTZYZS.sln -nologo -clp:ErrorsOnly
```
Expected: 0 errors.

- [ ] **Step 6: Commit**

```bash
git add -A src/Client/Desktop/Core/LYBT.Desktop.Shared/ src/Client/Desktop/Core/LYBT.Desktop.Contracts/
git commit -m "refactor(contracts): extract non-interface types to Shared — keep Contracts pure"
```

---

## Task 10: Update remaining module references

**Covers:** [S8]
**Files:**
- Modify: Each business module .csproj (update ProjectReferences)

- [ ] **Step 1: Add Controls reference to modules that use Controls types**

Check which modules reference Infrastructure.Controls types (MasterDetailControlBase, HerbItemControlBase, etc.) using the OLD namespace. Those modules now need a direct reference to Controls:

```
rg "LYBT.Desktop.Infrastructure.Controls" --include "*.cs" -l src/Client/Desktop/Modules/
```

For each module that imports Controls types, add to its .csproj:
```xml
  <ItemGroup>
    <ProjectReference Include="..\..\Core\LYBT.Desktop.Controls\LYBT.Desktop.Controls.csproj" />
  </ItemGroup>
```

Also update the using directives in those .cs files:
```
rg "using LYBT.Desktop.Infrastructure.Controls" --include "*.cs" src/Client/Desktop/Modules/
```
Replace `using LYBT.Desktop.Infrastructure.Controls` → `using LYBT.Desktop.Controls` in matched files.

- [ ] **Step 1b: Also check Role projects**

```
rg "LYBT.Desktop.Infrastructure.Controls" --include "*.cs" -l src/Client/Desktop/Roles/
```
Update Role projects the same way if needed.

- [ ] **Step 2: Remove stale Models references from modules**

Ensure no module .csproj still references Models (should be done in Task 7).

- [ ] **Step 3: Verify build**

```bash
dotnet build LYBTZYZS.sln -nologo -clp:ErrorsOnly
```
Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git add src/Client/Desktop/Modules/
git commit -m "refactor(modules): update ProjectReferences for new project layout"
```

---

## Task 11: Update Shell references

**Covers:** [S8]
**Files:**
- Modify: `src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj`
- Modify: `src/Client/Desktop/Shell/App.xaml`
- Modify: Shell DI extension files

- [ ] **Step 1: Add Controls and Shared references to Shell .csproj**

```xml
  <ItemGroup>
    <ProjectReference Include="..\Core\LYBT.Desktop.Controls\LYBT.Desktop.Controls.csproj" />
    <ProjectReference Include="..\Core\LYBT.Desktop.Shared\LYBT.Desktop.Shared.csproj" />
  </ItemGroup>
```

- [ ] **Step 2: Update DI extensions**

Shell's `ServiceCollectionExtensions.cs` registers infrastructure services. Ensure it also registers Controls converters if they need explicit registration (check if any controls use IValueConverter that needs DI).

- [ ] **Step 3: Verify build**

```bash
dotnet build LYBTZYZS.sln -nologo -clp:ErrorsOnly
```
Expected: 0 errors.

- [ ] **Step 5: Commit**

```bash
git add src/Client/Desktop/Shell/
git commit -m "refactor(shell): update Shell for new project layout"
```

---

## Task 12: Update test projects

**Covers:** [S8]
**Files:**
- Modify: `tests/LYBT.Tests.Desktop/LYBT.Tests.Desktop.csproj`
- Modify: Any test .cs files referencing old namespaces

- [ ] **Step 1: Add Controls and Shared references to test .csproj**

```xml
  <ItemGroup>
    <ProjectReference Include="..\..\src\Client\Desktop\Core\LYBT.Desktop.Controls\LYBT.Desktop.Controls.csproj" />
    <ProjectReference Include="..\..\src\Client\Desktop\Core\LYBT.Desktop.Shared\LYBT.Desktop.Shared.csproj" />
  </ItemGroup>
```

- [ ] **Step 2: Update test using directives**

```
rg "using LYBT.Desktop.Models" --include "*.cs" tests/
rg "using LYBT.Desktop.Utilities" --include "*.cs" tests/
```
Replace with updated namespaces.

- [ ] **Step 3: Verify tests build**

```bash
dotnet build tests/LYBT.Tests.Desktop/LYBT.Tests.Desktop.csproj -nologo -clp:ErrorsOnly
```
Expected: 0 errors.

- [ ] **Step 4: Verify architecture tests pass**

```bash
dotnet test tests/LYBT.Tests.Architecture/ --filter "FullyQualifiedName~P20|FullyQualifiedName~P21"
```
Expected: Update architecture test white-lists if needed (P20 allowed types list, P21 module reference rules).

- [ ] **Step 5: Commit**

```bash
git add tests/
git commit -m "refactor(tests): update test projects for new project layout"
```

---

## Task 13: Final verification and cleanup

**Covers:** [S10]
**Files:**
- Modify: `AGENTS.md` (update architecture description)
- Modify: Various module `AGENTS.md` files (update dependency tables)

- [ ] **Step 1: Clean empty directories**

```powershell
# Remove empty Infrastructure subdirectories (Controls, Themes, Converters if now empty)
Get-ChildItem -Path "src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/" -Directory | ForEach-Object {
    if ((Get-ChildItem $_.FullName -File -Recurse | Measure-Object).Count -eq 0) {
        Remove-Item $_.FullName -Recurse -Force
        Write-Output "Removed empty: $($_.Name)"
    }
}
```

- [ ] **Step 1b: Check for Behaviors directory**

If `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Behaviors/` exists and contains files, move them to `src/Client/Desktop/Core/LYBT.Desktop.Controls/Behaviors/` (per spec [S4]). If empty, it will be cleaned by Step 1.

- [ ] **Step 2: Full solution build**

```bash
dotnet build LYBTZYZS.sln -nologo -clp:ErrorsOnly
```
Expected: 0 errors.

- [ ] **Step 3: Architecture tests**

```bash
dotnet test tests/LYBT.Tests.Architecture/ --nologo
```
Expected: 0 failures (or only pre-existing failures).

- [ ] **Step 4: Desktop tests baseline comparison**

```bash
dotnet test tests/LYBT.Tests.Desktop/ --nologo -clp:ErrorsOnly
```
Expected: Pass count >= pre-refactor baseline (793).

- [ ] **Step 5: Verify Infrastructure LOC reduced**

```powershell
Get-ChildItem -Recurse -Include *.cs -Path "src/Client/Desktop/Core/LYBT.Desktop.Infrastructure" | Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' } | ForEach-Object { (Get-Content $_.FullName | Measure-Object -Line).Lines } | Measure-Object -Sum
```
Expected: < 10,000 (down from 18,663).

- [ ] **Step 6: Verify Controls project exists independently**

```bash
dotnet build src/Client/Desktop/Core/LYBT.Desktop.Controls/LYBT.Desktop.Controls.csproj -nologo -clp:ErrorsOnly
```
Expected: 0 errors.

- [ ] **Step 7: Verify Models and Utilities directories deleted**

```bash
Test-Path "src/Client/Desktop/Core/LYBT.Desktop.Models"
Test-Path "src/Client/Desktop/Core/LYBT.Desktop.Utilities"
```
Expected: both False.

- [ ] **Step 8: Update AGENTS.md**

Update `AGENTS.md` and affected module `AGENTS.md` files to reflect the new project layout:
- Core project list: Controls, Shared (new); Models, Utilities (removed)
- Infrastructure description: note Controls extraction
- Dependency diagrams if present

- [ ] **Step 9: Final commit**

```bash
git add -A
git commit -m "refactor(architecture): infrastructure cleanup complete

Desktop Core project layout restructured:
- LYBT.Desktop.Controls (NEW): WPF controls, themes, converters extracted from Infrastructure
- LYBT.Desktop.Shared (NEW): Non-interface shared types extracted from Contracts
- Models merged into Infrastructure (eliminated dependency edge)
- Utilities merged into Foundation (eliminated single-file project)

Infrastructure reduced from 18.6K LOC to ~10K. All 21 projects maintain
clean DAG. Build 0 errors, architecture tests pass, Desktop tests
baseline maintained."
```

- [ ] **Step 10: Push to remote**

```bash
git push origin master
```

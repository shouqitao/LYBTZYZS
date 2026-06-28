# Desktop 框架重构 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor Desktop layer for architecture clarity — split Infrastructure, unify navigation, fix VM inheritance, clean contracts.

**Architecture:** 4-phase progressive refactoring. Each phase compiles + tests independently. Approach A (3-project split): Infrastructure (slimmed) + Navigation (new) + Controls (absorbs Themes).

**Tech Stack:** .NET 8, WPF, Prism, CommunityToolkit.Mvvm, xUnit architecture tests

---

## File Structure (Target)

```
Core/
  LYBT.Desktop.Infrastructure/     ← 瘦身: VM基类 + WPF服务 + 角色定义 + HTTP + 行为
  LYBT.Desktop.Navigation/         ← 新项目: NavigationCoordinator + 导航模型
  LYBT.Desktop.Controls/           ← 吸收: Themes/ 从 Infrastructure 移入
  LYBT.Desktop.Contracts/          ← 清理: 纯接口，DTO 迁出
  LYBT.Desktop.Foundation/         ← 不变
  LYBT.Desktop.Shared/             ← 吸收: 从 Contracts 迁出的 DTO/Enum
  LYBT.Desktop.Printing/           ← 不变
  LYBT.Desktop.CardReader/         ← 不变
  LYBT.Desktop.LocalData/          ← 不变
```

---

## Phase 1: Infrastructure 拆分

### Task 1: 移动 Themes 到 Controls

**Covers:** [S4]
**Files:**
- Move: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Themes/` → `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/LYBT.Desktop.Controls.csproj` — add Theme XAML files
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/LYBT.Desktop.Infrastructure.csproj` — remove Theme XAML files
- Modify: All XAML files referencing `Source="/LYBT.Desktop.Infrastructure;component/Themes/..."` → `Source="/LYBT.Desktop.Controls;component/Themes/..."`
- Modify: `src/Client/Desktop/Shell/App.xaml` — update MergedDictionaries source paths

- [ ] **Step 1: Move Themes directory**

```bash
# Move all theme XAML files from Infrastructure to Controls
Move-Item -Path "src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Themes" -Destination "src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes" -Force
```

- [ ] **Step 2: Update Controls csproj — add Theme files as Page items**

In `LYBT.Desktop.Controls.csproj`, verify that XAML files in `Themes/` are included as `<Page>` items. If using wildcard `<Page Include="**\*.xaml">`, no change needed. Otherwise add:

```xml
<Page Include="Themes\*.xaml" />
```

- [ ] **Step 3: Update all XAML pack URI references**

Search and replace across ALL .xaml files in the solution:
- Old: `Source="/LYBT.Desktop.Infrastructure;component/Themes/`
- New: `Source="/LYBT.Desktop.Controls;component/Themes/`

```bash
# Find all references
rg "LYBT.Desktop.Infrastructure;component/Themes" src/Client/Desktop --include *.xaml -l

# Replace (use the tool, not sed, to preserve encoding)
```

- [ ] **Step 4: Update App.xaml MergedDictionaries**

In `src/Client/Desktop/Shell/App.xaml`, update all ResourceDictionary Source attributes from Infrastructure to Controls.

- [ ] **Step 5: Build**

```bash
dotnet build LYBTZYZS.sln
```
Expected: 0 errors. If errors about missing resources, grep for remaining old paths.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "refactor(phase1): move Themes from Infrastructure to Controls"
```

---

### Task 2: 创建 Navigation 项目

**Covers:** [S4, S5]
**Files:**
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Navigation/LYBT.Desktop.Navigation.csproj`
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Navigation/` — placeholder

- [ ] **Step 1: Create csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Prism.Wpf" />
    <PackageReference Include="CommunityToolkit.Mvvm" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\LYBT.Desktop.Contracts\LYBT.Desktop.Contracts.csproj" />
    <ProjectReference Include="..\LYBT.Desktop.Foundation\LYBT.Desktop.Foundation.csproj" />
    <ProjectReference Include="..\LYBT.Desktop.Infrastructure\LYBT.Desktop.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Add to solution**

```bash
dotnet sln LYBTZYZS.sln add src/Client/Desktop/Core/LYBT.Desktop.Navigation/LYBT.Desktop.Navigation.csproj
```

- [ ] **Step 3: Build**

```bash
dotnet build LYBTZYZS.sln
```
Expected: 0 errors (empty project compiles fine)

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "refactor(phase1): create LYBT.Desktop.Navigation project skeleton"
```

---

### Task 3: 迁移 NavigationCoordinator 到 Navigation 项目

**Covers:** [S4, S5]
**Files:**
- Move: `src/Client/Desktop/Shell/Services/NavigationCoordinator.cs` → `src/Client/Desktop/Core/LYBT.Desktop.Navigation/NavigationCoordinator.cs`
- Move: `src/Client/Desktop/Shell/Services/NavigationCoordinator.cs` interfaces → already in Contracts
- Modify: `src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj` — add Navigation reference, remove inline NavigationCoordinator
- Modify: `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs` — update namespace imports

- [ ] **Step 1: Move NavigationCoordinator.cs**

Move file from Shell/Services/ to Navigation project root. Update namespace to `LYBT.Desktop.Navigation`.

- [ ] **Step 2: Add Shell → Navigation project reference**

In `LYBT.Desktop.Shell.csproj`:
```xml
<ProjectReference Include="..\Core\LYBT.Desktop.Navigation\LYBT.Desktop.Navigation.csproj" />
```

- [ ] **Step 3: Update DI registration**

In `ServiceCollectionExtensions.cs`, change `using` from `LYBT.Desktop.Shell.Services` to `LYBT.Desktop.Navigation`.

- [ ] **Step 4: Update all consumers**

Search for `NavigationCoordinator` references across all files and fix namespace imports.

- [ ] **Step 5: Build + Commit**

```bash
dotnet build LYBTZYZS.sln
git add -A && git commit -m "refactor(phase1): move NavigationCoordinator to LYBT.Desktop.Navigation project"
```

---

## Phase 2: 统一导航系统

### Task 4: 删除 EnhancedNavigationService

**Covers:** [S5]
**Files:**
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Navigation/EnhancedNavigationService.cs` (571 LOC)
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Navigation/NavigationHistoryManager.cs` (131 LOC)
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Navigation/BreadcrumbManager.cs` (37 LOC)
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Navigation/NavigationModels.cs` (156 LOC)
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Services/IEnhancedNavigationService.cs`
- Modify: `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs` — remove IEnhancedNavigationService dependency
- Modify: Any ViewModel that injects IEnhancedNavigationService

- [ ] **Step 1: Find all IEnhancedNavigationService consumers**

```bash
rg "IEnhancedNavigationService|EnhancedNavigationService" src/Client/Desktop --include *.cs -l
```

- [ ] **Step 2: Delete EnhancedNavigationService files**

Delete the 4 files listed above plus the interface.

- [ ] **Step 3: Fix MainWindowViewModel — remove IEnhancedNavigationService**

Remove constructor parameter and all usages. Replace any navigation calls with `INavigationCoordinator` equivalents.

- [ ] **Step 4: Fix all other consumers**

Update any remaining references to use `INavigationCoordinator`.

- [ ] **Step 5: Build + Commit**

```bash
dotnet build LYBTZYZS.sln
git add -A && git commit -m "refactor(phase2): delete EnhancedNavigationService — unified to NavigationCoordinator"
```

---

### Task 5: 迁移导航相关 Infrastructure 文件到 Navigation 项目

**Covers:** [S5]
**Files:**
- Move: Remaining Navigation/ files from Infrastructure to Navigation project
- Modify: Infrastructure csproj — remove navigation files
- Modify: Navigation csproj — add navigation files

- [ ] **Step 1: Identify remaining navigation files**

```bash
ls src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Navigation/
```

Move any remaining files (e.g., `NavigationServiceRegistration.cs`, region adapters) to Navigation project.

- [ ] **Step 2: Move files + update namespaces**

Change namespace from `LYBT.Desktop.Infrastructure.Navigation` to `LYBT.Desktop.Navigation`.

- [ ] **Step 3: Update DI registration**

In `ServiceCollectionExtensions.cs` or `DependencyInjection/`, update the navigation service registration to use the new namespace.

- [ ] **Step 4: Build + Commit**

```bash
dotnet build LYBTZYZS.sln
git add -A && git commit -m "refactor(phase2): move all navigation files to LYBT.Desktop.Navigation"
```

---

## Phase 3: VM 继承体系修复

### Task 6: MasterDetailViewModelBase 继承 NavigableViewModelBase

**Covers:** [S6]
**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/MasterDetail/MasterDetailViewModelBase.cs` (590 LOC)
- Verify: All 5 MasterDetail subclasses compile (Patient/Herb/Formula/User/MedicalCase)

- [ ] **Step 1: Change base class**

In `MasterDetailViewModelBase.cs`:
- Change `: ObservableObject` to `: NavigableViewModelBase`
- Pass `IViewModelServices` to base constructor
- Remove duplicate Logger/EventAggregator/RegionManager/IsBusy/INavigationAware/IDisposable implementations
- Keep `IMasterDetailServices` as additional constructor parameter
- Remove the ARCH-REFACTOR comment at line 20

- [ ] **Step 2: Fix 5 subclass constructors**

Each MasterDetail subclass must pass `IViewModelServices` to the new base constructor. Files to check:
- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientMasterDetailViewModel.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/ViewModels/HerbMasterDetailViewModel.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/FormulaMasterDetailViewModel.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Users/ViewModels/UserMasterDetailViewModel.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseMasterDetailViewModel.cs`

- [ ] **Step 3: Build + fix compilation errors**

```bash
dotnet build LYBTZYZS.sln
```
Fix any duplicate member errors (properties overridden by both base and subclass).

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "refactor(phase3): MasterDetailViewModelBase inherits NavigableViewModelBase — eliminate ARCH-REFACTOR debt"
```

---

### Task 7: 收编散落 ObservableObject 继承者

**Covers:** [S6]
**Files:**
- Identify: All `: ObservableObject` that should be `: CoreViewModelBase` or services
- Modify: Service classes (PaginationService, SearchService, LoadingStateManager) → change base to plain service pattern

- [ ] **Step 1: Find all plain ObservableObject inheritors**

```bash
rg ": ObservableObject" src/Client/Desktop --include *.cs -l
```

- [ ] **Step 2: Classify each**

For each file, decide:
- **Service** (not a VM): remove ObservableObject, use plain class with events or callbacks
- **True VM**: change base to appropriate VM base class
- **Model**: change base to ValidatableModelBase

- [ ] **Step 3: Fix each file**

Apply the classification. Most likely changes:
- PaginationService, SearchService → plain services (no VM base needed)
- *EditorViewModel classes → ChildViewModelBase or NavigableViewModelBase
- DashboardStatus/StatusCard → keep as ObservableObject (legitimate model)

- [ ] **Step 4: Build + Commit**

```bash
dotnet build LYBTZYZS.sln
git add -A && git commit -m "refactor(phase3): classify scattered ObservableObject inheritors — VMs vs services vs models"
```

---

## Phase 4: Contracts 清理 + 死代码

### Task 8: 迁移 Contracts 中的 DTO/Enum 到正确位置

**Covers:** [S7]
**Files:**
- Move: 7 non-interface files from Contracts to Shared/Desktop.Shared

| File | From | To |
|------|------|-----|
| ApiClientOptions.cs | Contracts/ApiClient/ | Shared.Configuration/Options/Client/ |
| AuthState.cs | Contracts/Security/ | Desktop.Shared/Models/ |
| PerformanceMetric.cs | Contracts/Performance/ | Desktop.Shared/Models/ |
| PerformanceReport.cs | Contracts/Performance/ | Desktop.Shared/Models/ |
| ImportValidationResult.cs | Contracts/Models/ | Shared.Models/Contracts/Common/ |
| CacheEvents.cs | Contracts/Events/ | Desktop.Shared/Events/ |
| UnfinishedCaseChoice.cs | Contracts/Services/ | Desktop.Shared/Enums/ |

- [ ] **Step 1: Move each file**

Move files and update namespaces. Update all consumers' `using` statements.

- [ ] **Step 2: Build + fix using statements**

```bash
dotnet build LYBTZYZS.sln
```

- [ ] **Step 3: Commit**

```bash
git add -A && git commit -m "refactor(phase4): migrate 7 DTO/Enum from Contracts to correct layers"
```

---

### Task 9: 删除死代码

**Covers:** [S7]
**Files:**
- Delete: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/` — PatientViewState.cs (no consumers)
- Delete: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/` — FormulaCommandHandler.cs (not registered)
- Delete: `src/Client/Desktop/Modules/LYBT.Desktop.Users/` — dead IUserCommandHandler (not registered)
- Delete: 6 `_wpftmp.csproj` build artifacts

- [ ] **Step 1: Verify each file is truly dead**

```bash
# For each file, verify zero references
rg "PatientViewState" src/Client/Desktop --include *.cs -l
rg "FormulaCommandHandler" src/Client/Desktop --include *.cs -l
```

- [ ] **Step 2: Delete confirmed dead files**

- [ ] **Step 3: Build + verify no breakage**

```bash
dotnet build LYBTZYZS.sln
```

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "refactor(phase4): delete dead code — PatientViewState, FormulaCommandHandler, build artifacts"
```

---

### Task 10: 更新 AGENTS.md + 架构文档

**Covers:** [S4, S5, S6, S7]
**Files:**
- Modify: `AGENTS.md` — update Desktop Core DAG, project descriptions
- Modify: `src/Client/Desktop/Core/AGENTS.md` — add Navigation project
- Modify: `src/Client/Desktop/AGENTS.md` — update project list

- [ ] **Step 1: Update Desktop Core DAG**

```
Contracts ← Foundation ← Infrastructure ← Controls
Shared (standalone)
Navigation (new — references Contracts + Foundation + Infrastructure)
Modules reference Infrastructure + Controls + Navigation
Roles reference Infrastructure + Controls + Navigation + Modules
```

- [ ] **Step 2: Update project inventory tables**

- [ ] **Step 3: Commit + Push**

```bash
git add -A && git commit -m "docs: update AGENTS.md for 4-phase Desktop framework refactoring" && git push origin master
```

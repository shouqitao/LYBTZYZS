# Desktop Shell Extensions 整合 + API 层统一 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Consolidate redundant HTTP service registrations, mark legacy Refit interfaces as obsolete, and migrate all direct consumers to the unified `IApiClient` abstraction.

**Architecture:** The old `HttpServiceRegistrationExtensions` registers 8 individual Refit interfaces as singletons, while `RefitApiClient` already creates them lazily internally via `RestService.For<T>()`. This creates redundant registrations. We mark the old registration as `[Obsolete]`, migrate all ViewModels/Services that directly inject old Refit interfaces to use `IApiClient` sub-interfaces instead, then remove the old registration.

**Tech Stack:** .NET 8, WPF, Prism (DryIoc), CommunityToolkit.Mvvm, Refit, EF Core

## Global Constraints

- **Pure refactoring** — no functional changes, no API contract changes, no behavior changes
- **Incremental commits** — one commit per logical step
- **Build must pass** — `dotnet build LYBTZYZS.sln` after each step
- **Existing patterns** — follow CommunityToolkit `[RelayCommand]`, PascalCase public, `_camelCase` private
- **DI registration** — `RegisterSingleton` for services, `Transient` for ViewModels

---

### Task 1: Mark old Refit registration as Obsolete

**Covers:** [S2] Shell Extensions consolidation

**Files:**
- Modify: `src/Client/Desktop/Shell/Extensions/HttpServiceRegistrationExtensions.cs:20-26`

**Interfaces:**
- Consumes: none
- Produces: `[Obsolete]` attribute on `RegisterHttpServices` method

- [ ] **Step 1: Add [Obsolete] attribute to the old registration method**

```csharp
// File: src/Client/Desktop/Shell/Extensions/HttpServiceRegistrationExtensions.cs
// Change the method signature to:

/// <summary>注册HTTP相关服务</summary>
/// <remarks>已废弃: 请使用 IApiClient (SwitchingApiClient) 替代。此方法将在后续版本移除。</remarks>
[Obsolete("Use IApiClient (SwitchingApiClient) instead. This method will be removed in a future version.")]
public static void RegisterHttpServices(this IContainerRegistry containerRegistry, IConfiguration config)
```

- [ ] **Step 2: Build to verify no errors**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeds (warnings about obsolete usage are expected)

- [ ] **Step 3: Commit**

```bash
git add src/Client/Desktop/Shell/Extensions/HttpServiceRegistrationExtensions.cs
git commit -m "refactor(shell): mark legacy HttpServiceRegistrationExtensions as Obsolete"
```

---

### Task 2: Migrate ViewModel consumers from old Refit interfaces to IApiClient

**Covers:** [S2] API layer unification

**Files:**
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/PatientSelectionViewModel.cs`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/AuditLogViewModel.cs`
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/ViewModels/SysadminHomeViewModel.cs`
- Modify: `src/Client/Desktop/Shell/ViewModels/AccountSettingsViewModel.cs`
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/ViewModels/LogLevelControlViewModel.cs`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Registration/ViewModels/RegistrationListViewModel.cs`

**Interfaces:**
- Consumes: `IApiClient` (from `LYBT.Desktop.Contracts.ApiClient`)
- Produces: ViewModels that no longer depend on old Refit interfaces

**Migration pattern for each ViewModel:**

Replace `IPatientApi` → `_api.Patients`, `IMedicalCaseApi` → `_api.MedicalCases`, `IAuthApi` → `_api.Auth`, `IUserApi` → `_api.Users`, `IDiagnosticsApi` → `_api.Diagnostics`

- [ ] **Step 1: Migrate PatientSelectionViewModel**

```csharp
// File: src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/PatientSelectionViewModel.cs

// Before:
private readonly IPatientApi _patientApi;
private readonly IMedicalCaseApi _medicalCaseApi;

// After:
private readonly IApiClientPatients _patientApi;
private readonly IApiClientMedicalCases _medicalCaseApi;

// Constructor injection change:
// Before:
public PatientSelectionViewModel(..., IPatientApi patientApi, IMedicalCaseApi medicalCaseApi, ...)
// After:
public PatientSelectionViewModel(..., IApiClientPatients patientApi, IApiClientMedicalCases medicalCaseApi, ...)
```

Note: Check the actual method signatures on `IApiClientPatients` and `IApiClientMedicalCases` match what `PatientSelectionViewModel` calls. If method names differ, use `_api.Patients.XxxAsync()` pattern instead.

- [ ] **Step 2: Build and verify**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeds

- [ ] **Step 3: Migrate AuditLogViewModel**

```csharp
// File: src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/AuditLogViewModel.cs

// Before:
private readonly IMedicalCaseApi _medicalCaseApi;

// After:
private readonly IApiClientMedicalCases _medicalCaseApi;

// Constructor: replace IMedicalCaseApi → IApiClientMedicalCases
```

- [ ] **Step 4: Build and verify**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeds

- [ ] **Step 5: Migrate SysadminHomeViewModel**

```csharp
// File: src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/ViewModels/SysadminHomeViewModel.cs

// Before:
private readonly IAuthApi _authApi;

// After:
private readonly IApiClientAuth _authApi;

// Constructor: replace IAuthApi → IApiClientAuth
```

- [ ] **Step 6: Build and verify**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeds

- [ ] **Step 7: Migrate AccountSettingsViewModel**

```csharp
// File: src/Client/Desktop/Shell/ViewModels/AccountSettingsViewModel.cs

// Before:
private readonly IUserApi _userApi;

// After:
private readonly IApiClientUsers _userApi;

// Constructor: replace IUserApi → IApiClientUsers
```

- [ ] **Step 8: Build and verify**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeds

- [ ] **Step 9: Migrate LogLevelControlViewModel**

```csharp
// File: src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/ViewModels/LogLevelControlViewModel.cs

// Before:
private readonly IDiagnosticsApi _diagnosticsApi;

// After:
private readonly IApiClientReports _diagnosticsApi; // or appropriate sub-interface

// Check IApiClient for diagnostics sub-interface; if none exists, check IApiClientReports or create migration path
```

Note: `IDiagnosticsApi` may not have a direct equivalent in `IApiClient`. Check `IApiClient` definition. If no sub-interface exists, keep `IDiagnosticsApi` for now and note it as a follow-up.

- [ ] **Step 10: Build and verify**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeds

- [ ] **Step 11: Migrate RegistrationListViewModel**

```csharp
// File: src/Client/Desktop/Modules/LYBT.Desktop.Registration/ViewModels/RegistrationListViewModel.cs

// Before:
private readonly IPatientApi _patientApi;

// After:
private readonly IApiClientPatients _patientApi;

// Constructor: replace IPatientApi → IApiClientPatients
```

- [ ] **Step 12: Build and verify full solution**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeds

- [ ] **Step 13: Commit**

```bash
git add src/Client/Desktop/Roles/ src/Client/Desktop/Modules/ src/Client/Desktop/Shell/ViewModels/
git commit -m "refactor(desktop): migrate ViewModels from legacy Refit interfaces to IApiClient sub-interfaces"
```

---

### Task 3: Migrate Foundation/Security services from IAuthApi to IApiClientAuth

**Covers:** [S2] API layer unification

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Security/AuthenticationService.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Security/LogoutService.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Security/TokenLifecycleService.cs`

**Interfaces:**
- Consumes: `IApiClientAuth` (from `LYBT.Desktop.Contracts.ApiClient`)
- Produces: Foundation services that no longer depend on `IAuthApi`

- [ ] **Step 1: Migrate AuthenticationService**

```csharp
// File: src/Client/Desktop/Core/LYBT.Desktop.Foundation/Security/AuthenticationService.cs

// Before:
private readonly IAuthApi _authApi;

// After:
private readonly IApiClientAuth _authApi;

// Constructor: replace IAuthApi → IApiClientAuth
```

Note: `IApiClientAuth` methods may return different types (e.g., `ApiResponse<T>` vs raw). Check each call site. The adapter `AuthApiClient` already handles the conversion, so the method signatures should match. If there are signature differences, adjust the call sites accordingly.

- [ ] **Step 2: Build and verify**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeds

- [ ] **Step 3: Migrate LogoutService**

```csharp
// File: src/Client/Desktop/Core/LYBT.Desktop.Foundation/Security/LogoutService.cs

// Before:
private readonly IAuthApi _authApi;

// After:
private readonly IApiClientAuth _authApi;

// Constructor: replace IAuthApi → IApiClientAuth
```

- [ ] **Step 4: Build and verify**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeds

- [ ] **Step 5: Migrate TokenLifecycleService**

```csharp
// File: src/Client/Desktop/Core/LYBT.Desktop.Foundation/Security/TokenLifecycleService.cs

// Before:
private readonly IAuthApi _authApi;

// After:
private readonly IApiClientAuth _authApi;

// Constructor: replace IAuthApi → IApiClientAuth
```

- [ ] **Step 6: Build and verify full solution**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeds

- [ ] **Step 7: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Foundation/Security/
git commit -m "refactor(desktop): migrate Foundation security services from IAuthApi to IApiClientAuth"
```

---

### Task 4: Remove old Refit registration and clean up

**Covers:** [S2] API layer unification, [S4] Dead code cleanup

**Files:**
- Modify: `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs:54`
- Delete or gut: `src/Client/Desktop/Shell/Extensions/HttpServiceRegistrationExtensions.cs`

**Interfaces:**
- Consumes: all ViewModels/Services now migrated to `IApiClient`
- Produces: clean registration with single API client path

- [ ] **Step 1: Remove the RegisterHttpServices call from ServiceCollectionExtensions**

```csharp
// File: src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs

// Before (line 54):
containerRegistry.RegisterHttpServices(configuration);
containerRegistry.AddUnifiedApiClient(configuration);

// After:
// containerRegistry.RegisterHttpServices(configuration); // REMOVED: legacy Refit interfaces no longer needed
containerRegistry.AddUnifiedApiClient(configuration);
```

- [ ] **Step 2: Build and verify — check for DI resolution errors**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeds. If any errors about unresolved `IAuthApi`, `IPatientApi`, etc., those are ViewModels/Services we missed — go back and migrate them.

- [ ] **Step 3: Gut HttpServiceRegistrationExtensions — keep file as empty shell**

```csharp
// File: src/Client/Desktop/Shell/Extensions/HttpServiceRegistrationExtensions.cs

// Replace entire file content with:
namespace LYBT.Desktop.Shell.Extensions
{
    /// <summary>
    /// 已废弃: HTTP服务注册已整合到 ServiceCollectionExtensions + UnifiedApiClientExtensions。
    /// 此文件保留用于向后兼容，将在后续版本删除。
    /// </summary>
    [System.Obsolete("This class is obsolete. Use ServiceCollectionExtensions + UnifiedApiClientExtensions instead.")]
    public static class HttpServiceRegistrationExtensions
    {
    }
}
```

- [ ] **Step 4: Build and verify**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeds

- [ ] **Step 5: Run Desktop tests**

Run: `dotnet test tests/LYBT.Tests.Desktop/`
Expected: All tests pass

- [ ] **Step 6: Commit**

```bash
git add src/Client/Desktop/Shell/Extensions/
git commit -m "refactor(shell): remove legacy Refit registration, consolidate to unified IApiClient"
```

---

### Task 5: Clean up dead code comments across modules

**Covers:** [S4] Dead code cleanup

**Files:**
- Scan: all `*.cs` files under `src/Client/Desktop/Modules/` and `src/Client/Desktop/Roles/`

**Interfaces:**
- Consumes: none
- Produces: cleaner codebase with removed "已删除"/"已移除" comment blocks

- [ ] **Step 1: Search for dead code comment blocks**

Run: `rg "已删除|已移除|已废弃|DEPRECATED|TODO.*删除" --include="*.cs" -l src/Client/Desktop/`

- [ ] **Step 2: For each file found, read the context and remove stale comment blocks**

Focus on multi-line comment blocks that describe deleted features. Keep single-line comments that explain current behavior.

- [ ] **Step 3: Build and verify**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeds

- [ ] **Step 4: Commit**

```bash
git add src/Client/Desktop/
git commit -m "refactor(desktop): clean up dead code comments across modules"
```

---

### Task 6: Run full verification

**Covers:** [S3] Verification

**Files:** none (verification only)

- [ ] **Step 1: Full build**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeds with zero errors

- [ ] **Step 2: Desktop unit tests**

Run: `dotnet test tests/LYBT.Tests.Desktop/`
Expected: All tests pass

- [ ] **Step 3: Integration tests (if SQL Server available)**

Run: `dotnet test tests/LYBT.Tests.Server/`
Expected: All tests pass

- [ ] **Step 4: Architecture tests**

Run: `dotnet test tests/LYBT.Tests.Architecture/`
Expected: All tests pass (no new architecture violations)

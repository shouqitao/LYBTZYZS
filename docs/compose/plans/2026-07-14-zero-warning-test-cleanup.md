# Zero Warning & Test Cleanup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Achieve 0 build warnings, clean up redundant C# tests (Newman tests are complete), and sync to server.

**Architecture:** Fix all 55 compiler warnings across Desktop/LocalData, LocalWebAPI, WebAPI, Desktop.Shell, and test projects. Remove C# integration tests that duplicate Newman API test coverage. Keep only unit tests, architecture tests, and essential integration tests.

**Tech Stack:** .NET 8, C#, Riok.Mapperly, xUnit, FluentAssertions, Newman/Postman

## Global Constraints

- Build must produce 0 errors AND 0 warnings
- Newman API tests (86 endpoints) are the primary integration test layer
- C# integration tests that duplicate Newman coverage should be removed
- Unit tests (validators, mappers, entities, infrastructure) must be preserved
- Architecture tests must be preserved
- All changes must pass `dotnet build LYBTZYZS.sln` with 0 warnings

---

## Task 1: Fix Mapperly Warnings in Desktop.LocalData

**Covers:** Warning cleanup - RMG012/RMG020 in LocalMedicalCaseMapper.cs and LocalUserMapper.cs

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.LocalData/Mappers/LocalMedicalCaseMapper.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.LocalData/Mappers/LocalUserMapper.cs`

**Interfaces:**
- Consumes: Mapperly source generator
- Produces: Clean compilation without RMG012/RMG020 warnings

- [ ] **Step 1: Read current mapper files**

Read both mapper files to understand current mapping definitions.

- [ ] **Step 2: Fix LocalMedicalCaseMapper.cs**

Add `[Ignore]` attributes for properties that don't exist on source DTO:
- IsPrinted, PrintCount, LastPrintedAt, PrintVersion, PrintLogs

```csharp
// Add to MedicalCaseInputDto -> MedicalCase mapping:
[Ignore(nameof(MedicalCase.IsPrinted))]
[Ignore(nameof(MedicalCase.PrintCount))]
[Ignore(nameof(MedicalCase.LastPrintedAt))]
[Ignore(nameof(MedicalCase.PrintVersion))]
[Ignore(nameof(MedicalCase.PrintLogs))]
```

- [ ] **Step 3: Fix LocalUserMapper.cs**

For ApplicationUser -> UserDetailDto mapping, add `[Ignore]` for:
- IsSysAdmin, NormalizedUserName, NormalizedEmail, EmailConfirmed, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled

For UserInputDto -> ApplicationUser mapping, add `[Ignore]` for:
- Id, IsSysAdmin, NormalizedUserName, NormalizedEmail, EmailConfirmed, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, FailedLoginCount

- [ ] **Step 4: Verify build**

Run: `dotnet build src/Client/Desktop/Core/LYBT.Desktop.LocalData/LYBT.Desktop.LocalData.csproj`
Expected: 0 warnings from this project

- [ ] **Step 5: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.LocalData/Mappers/
git commit -m "fix: suppress Mapperly RMG012/RMG020 warnings in Desktop.LocalData mappers"
```

---

## Task 2: Fix Nullable Reference Warnings

**Covers:** Warning cleanup - CS8604, CS8601 in LocalAuthService.cs, LocalApiMapper.cs, FormulasController.cs

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.LocalData/Services/LocalAuthService.cs`
- Modify: `src/Client/Desktop/LocalWebAPI/Mappers/LocalApiMapper.cs`
- Modify: `src/Client/Desktop/LocalWebAPI/Controllers/FormulasController.cs`

**Interfaces:**
- Consumes: Nullable reference types
- Produces: Clean compilation without CS8604/CS8601 warnings

- [ ] **Step 1: Fix LocalAuthService.cs (lines 73, 127)**

Add null-conditional operator or null check before calling PasswordHelper.VerifyPassword:

```csharp
// Line 73 - add null check:
var hashedPassword = user?.PasswordHash;
if (string.IsNullOrEmpty(hashedPassword))
    return AuthResult.Fail("Invalid credentials");

var result = PasswordHelper.VerifyPassword(input.Password, hashedPassword, ...);
```

Same pattern for line 127.

- [ ] **Step 2: Fix LocalApiMapper.cs (lines 30, 36)**

Add null-forgiving operator or null check for nullable assignments:

```csharp
// Use null-forgiving operator where assignment is safe:
SomeProperty = someValue!,
// Or add null check:
SomeProperty = someValue ?? string.Empty,
```

- [ ] **Step 3: Fix FormulasController.cs (lines 148, 150)**

Add null-forgiving operator or null check:

```csharp
// Use null-forgiving operator:
var result = someValue!;
// Or add null check:
var result = someValue ?? defaultValue;
```

- [ ] **Step 4: Verify build**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 CS8604/CS8601 warnings

- [ ] **Step 5: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.LocalData/Services/LocalAuthService.cs
git add src/Client/Desktop/LocalWebAPI/Mappers/LocalApiMapper.cs
git add src/Client/Desktop/LocalWebAPI/Controllers/FormulasController.cs
git commit -m "fix: resolve nullable reference warnings CS8604/CS8601"
```

---

## Task 3: Fix Async & Unused Field Warnings

**Covers:** Warning cleanup - CS1998, CS0169 in MedicalCaseStartCoordinator.cs, PendingQueueViewModel.cs, App.xaml.cs

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/Components/MedicalCaseStartCoordinator.cs`
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/Workspace/PendingQueueViewModel.cs`
- Modify: `src/Client/Desktop/Shell/App.xaml.cs`

**Interfaces:**
- Consumes: async/await pattern
- Produces: Clean compilation without CS1998/CS0169 warnings

- [ ] **Step 1: Fix MedicalCaseStartCoordinator.cs (lines 118, 141)**

Either add await or remove async keyword:

```csharp
// Option A: If method should be async, add await:
public async Task SomeMethod()
{
    await Task.CompletedTask; // or actual async work
}

// Option B: If method doesn't need async, remove async keyword:
public Task SomeMethod()
{
    return Task.CompletedTask;
}
```

- [ ] **Step 2: Fix PendingQueueViewModel.cs (line 68)**

Same pattern as above - either add await or remove async.

- [ ] **Step 3: Fix App.xaml.cs (line 34)**

Remove unused field `_orchestrator`:

```csharp
// Delete this line:
// private SomeType _orchestrator;
```

- [ ] **Step 4: Verify build**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 CS1998/CS0169 warnings

- [ ] **Step 5: Commit**

```bash
git add src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/Components/MedicalCaseStartCoordinator.cs
git add src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/Workspace/PendingQueueViewModel.cs
git add src/Client/Desktop/Shell/App.xaml.cs
git commit -m "fix: resolve async and unused field warnings CS1998/CS0169"
```

---

## Task 4: Fix XML Comment & xUnit Warnings

**Covers:** Warning cleanup - CS1571 in WebAPI Program.cs, xUnit1013 in test files

**Files:**
- Modify: `src/Server/Services/LYBT.WebAPI/Program.cs`
- Modify: `tests/LYBT.Tests.Desktop/Integration/Modules/PatientTests.cs`
- Modify: `tests/LYBT.Tests.Desktop/Integration/Modules/HerbTests.cs`
- Modify: `tests/LYBT.Tests.Desktop/Integration/Modules/UserTests.cs`
- Modify: `tests/LYBT.Tests.Desktop/Integration/Modules/FormulaTests.cs`

**Interfaces:**
- Consumes: XML documentation, xUnit test attributes
- Produces: Clean compilation without CS1571/xUnit1013 warnings

- [ ] **Step 1: Fix WebAPI Program.cs (line 259)**

Remove duplicate `<param>` tag in XML comment:

```xml
<!-- Remove the duplicate param tag for "configuration" -->
```

- [ ] **Step 2: Fix PatientTests.cs (lines 162, 304)**

Either add `[Fact]` attribute or change method visibility to private:

```csharp
// Option A: Add [Fact] if test should run:
[Fact]
public void DeleteAndRestore_Patient_CompletesSuccessfully()

// Option B: Make private if test is not needed:
private void DeleteAndRestore_Patient_CompletesSuccessfully()
```

- [ ] **Step 3: Fix HerbTests.cs (lines 140, 280)**

Same pattern as PatientTests.cs.

- [ ] **Step 4: Fix UserTests.cs (lines 159, 321)**

Same pattern as PatientTests.cs.

- [ ] **Step 5: Fix FormulaTests.cs (line 190)**

Same pattern as PatientTests.cs.

- [ ] **Step 6: Verify build**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 warnings total

- [ ] **Step 7: Commit**

```bash
git add src/Server/Services/LYBT.WebAPI/Program.cs
git add tests/LYBT.Tests.Desktop/Integration/Modules/
git commit -m "fix: resolve XML comment and xUnit test warnings"
```

---

## Task 5: Verify 0 Warnings Build

**Covers:** Full build verification

**Files:**
- None (verification only)

**Interfaces:**
- Consumes: All previous tasks completed
- Produces: Confirmed 0 warnings build

- [ ] **Step 1: Clean build**

Run: `dotnet clean LYBTZYZS.sln && dotnet build LYBTZYZS.sln`
Expected: 0 errors, 0 warnings

- [ ] **Step 2: Run all tests**

Run: `dotnet test LYBTZYZS.sln`
Expected: All tests pass

- [ ] **Step 3: Commit final state**

```bash
git add -A
git commit -m "chore: achieve 0 warnings build across entire solution"
```

---

## Task 6: Clean Up C# Integration Tests

**Covers:** Test cleanup - Remove redundant integration tests covered by Newman

**Files:**
- Delete: `tests/LYBT.Tests.Server/Integration/` (most files)
- Keep: `tests/LYBT.Tests.Server/Unit/`, `tests/LYBT.Tests.Server/_Infrastructure/`
- Keep: `tests/LYBT.Tests.Server/RateLimiting/`
- Delete: `tests/LYBT.Tests.Server/UserJourneys/` (all files)

**Interfaces:**
- Consumes: Newman test collection (86 endpoints)
- Produces: Leaner test suite with only necessary tests

- [ ] **Step 1: Identify tests to keep**

Keep these test categories:
- Unit tests: validators, mappers, entities, infrastructure, utilities
- Architecture tests: all
- Rate limiting tests: all
- Integration test infrastructure: keep base classes for potential future use

Remove:
- All `Integration/` folder tests (covered by Newman)
- All `UserJourneys/` folder tests (covered by Newman)

- [ ] **Step 2: Delete redundant integration tests**

```bash
# Remove integration test folders
rm -rf tests/LYBT.Tests.Server/Integration/Auth/
rm -rf tests/LYBT.Tests.Server/Integration/Configuration/
rm -rf tests/LYBT.Tests.Server/Integration/Diagnostics/
rm -rf tests/LYBT.Tests.Server/Integration/ErrorHandling/
rm -rf tests/LYBT.Tests.Server/Integration/Formulas/
rm -rf tests/LYBT.Tests.Server/Integration/Health/
rm -rf tests/LYBT.Tests.Server/Integration/Herbs/
rm -rf tests/LYBT.Tests.Server/Integration/Logging/
rm -rf tests/LYBT.Tests.Server/Integration/MedicalCases/
rm -rf tests/LYBT.Tests.Server/Integration/Patients/
rm -rf tests/LYBT.Tests.Server/Integration/Registrations/
rm -rf tests/LYBT.Tests.Server/Integration/Reports/
rm -rf tests/LYBT.Tests.Server/Integration/Security/
rm -rf tests/LYBT.Tests.Server/Integration/Users/
```

- [ ] **Step 3: Delete UserJourney tests**

```bash
rm -rf tests/LYBT.Tests.Server/UserJourneys/
```

- [ ] **Step 4: Verify build and tests**

Run: `dotnet build LYBTZYZS.sln && dotnet test LYBTZYZS.sln`
Expected: 0 errors, 0 warnings, all remaining tests pass

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "test: remove redundant integration tests covered by Newman API tests"
```

---

## Task 7: Final Verification & Server Sync

**Covers:** Deployment preparation

**Files:**
- None (verification and deployment)

**Interfaces:**
- Consumes: All previous tasks completed
- Produces: Deployed server with 0 warnings

- [ ] **Step 1: Final build verification**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors, 0 warnings

- [ ] **Step 2: Run all tests one more time**

Run: `dotnet test LYBTZYZS.sln`
Expected: All tests pass

- [ ] **Step 3: Deploy to server**

Run: `./deploy.ps1` or `./sync-to-server.ps1`
Expected: Successful deployment

- [ ] **Step 4: Verify server health**

Check: `curl http://localhost:5000/api/v1/health`
Expected: Healthy response

- [ ] **Step 5: Commit deployment**

```bash
git add -A
git commit -m "deploy: sync 0-warning build to server"
```

---

## Self-Review Checklist

- [ ] All 55 warnings identified and fixed
- [ ] Newman tests (86 endpoints) provide full API coverage
- [ ] C# integration tests removed (redundant with Newman)
- [ ] Unit tests preserved (validators, mappers, entities, infrastructure)
- [ ] Architecture tests preserved
- [ ] Rate limiting tests preserved
- [ ] `dotnet build LYBTZYZS.sln` produces 0 errors, 0 warnings
- [ ] `dotnet test LYBTZYZS.sln` passes all remaining tests
- [ ] Server deployment successful

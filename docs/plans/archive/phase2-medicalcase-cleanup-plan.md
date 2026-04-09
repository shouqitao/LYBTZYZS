# Phase 2: MedicalCase Module Cleanup - Execution Plan

## Context

MedicalCaseController was split into 4 new controllers (Epic #1612). The old controller is marked `[Obsolete]` + `[NonController]` but still exists with embedded DTOs. The new controllers reference those old DTOs and lack CancellationToken support.

## Current State Audit

### Old Controller (Dead Code)

| File | Lines | Status |
|------|-------|--------|
| `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs` | 840 | `[Obsolete]` + `[NonController]`, 20+ methods still present |

**Embedded DTOs (lines 825-839):**
- `UpdateStatusRequest` (line 825): `{ MedicalCaseStatus Status }`
- `CancelMedicalCaseRequest` (line 835): `{ string? Reason }`

### New Controllers

| Controller | Methods | CancellationToken | Old DTO Usage |
|------------|---------|-------------------|---------------|
| MedicalCasesController | 12 | No | None |
| MedicalCaseWorkflowController | 4 | No | `UpdateStatusRequest`, `CancelMedicalCaseRequest` |
| MedicalCasePrintController | 2 | No | None |
| MedicalCaseAuditController | 2 | No | None |

### Files Referencing MedicalCaseController (Main Repo)

| File | Line(s) | Reference Type |
|------|---------|----------------|
| `tests/.../AggregateRootArchTests.cs` | 51-70 | Code: asserts controller exists |
| `tests/.../ArchTests.cs` | 55 | Code: excludedControllers list |
| `tests/.../MedicalCaseWorkflowControllerTests.cs` | 398-411 | Code: local DTO copies |
| `tests/.../MedicalCaseFlowTests.cs` | 14 | Comment only |
| `docs/04-api-reference/medical-cases.md` | 3 | Doc: controller name |
| `src/Server/CLAUDE.md` | 37 | Doc: endpoint table |
| `src/Server/Services/LYBT.WebAPI/CLAUDE.md` | 448-451 | Doc: internal DTOs |
| `src/Shared/.../MedicalCaseStatusInputDto.cs` | - | Existing replacement DTO |
| `src/Shared/.../MedicalCaseInputDto.cs` | 97-104 | Existing `CancelMedicalCaseRequestDto` |

### DTO Migration Map

| Old DTO (in controller) | Replacement (Shared.Models) | Field Delta |
|-------------------------|----------------------------|-------------|
| `UpdateStatusRequest` | `MedicalCaseStatusInputDto` | New has extra `StatusChangeReason` field |
| `CancelMedicalCaseRequest` | `CancelMedicalCaseRequestDto` | Identical |

---

## Task 2.1: Redesign Architecture Test AR001

**Goal:** Update `AggregateRootArchTests.AR001` to validate new controllers instead of the old monolithic one.

**Pre-condition:** Architecture tests must pass before starting.

### Sub-tasks

| # | Action | Files | TDD Phase |
|---|--------|-------|-----------|
| 1.1 | Write failing test: assert `MedicalCasesController` exists with write methods | `AggregateRootArchTests.cs` | Red |
| 1.2 | Write failing test: assert `MedicalCaseWorkflowController` exists with write methods | `AggregateRootArchTests.cs` | Red |
| 1.3 | Write failing test: assert `MedicalCaseController` does NOT exist | `AggregateRootArchTests.cs` | Red |
| 1.4 | Implement: replace old AR001 body (lines 32-71) with new logic | `AggregateRootArchTests.cs` | Green |
| 1.5 | Remove "MedicalCaseController" from `ArchTests.cs` excludedControllers (line 55) | `ArchTests.cs` | Green |
| 1.6 | Run `dotnet test tests/LYBT.Tests.Architecture/` -- all pass | - | Verify |

**Files modified:**
- `tests/LYBT.Tests.Architecture/AggregateRootArchTests.cs`
- `tests/LYBT.Tests.Architecture/ArchTests.cs`

**AR001 new design:**
```csharp
[Fact]
public void AR001_MedicalCase_Should_Be_Aggregate_Root()
{
    // 1. Verify ConsultationController and PrescriptionsController are deleted
    var consultationController = Types.InAssemblies(ServerAssemblies)
        .That().HaveName("ConsultationController").GetTypes().FirstOrDefault();
    Assert.Null(consultationController);

    var prescriptionsController = Types.InAssemblies(ServerAssemblies)
        .That().HaveName("PrescriptionsController").GetTypes().FirstOrDefault();
    Assert.Null(prescriptionsController);

    // 2. Verify old monolithic MedicalCaseController is removed
    var oldController = Types.InAssemblies(ServerAssemblies)
        .That().HaveName("MedicalCaseController").GetTypes().FirstOrDefault();
    Assert.Null(oldController); // Changed from Assert.NotNull

    // 3. Verify new split controllers exist with write methods
    var writeControllers = new[] { "MedicalCasesController", "MedicalCaseWorkflowController" };
    foreach (var name in writeControllers)
    {
        var controller = Types.InAssemblies(ServerAssemblies)
            .That().HaveName(name).GetTypes().FirstOrDefault();
        Assert.NotNull(controller);
    }
}
```

---

## Task 2.2: Migrate DTOs to Shared.Models

**Goal:** Move embedded DTOs out of old controller, adopt existing Shared DTOs.

### Sub-tasks

| # | Action | Files | TDD Phase |
|---|--------|-------|-----------|
| 2.1 | Write test: `MedicalCaseWorkflowController` uses `MedicalCaseStatusInputDto` | `MedicalCaseWorkflowControllerTests.cs` | Red |
| 2.2 | Write test: `MedicalCaseWorkflowController` uses `CancelMedicalCaseRequestDto` | `MedicalCaseWorkflowControllerTests.cs` | Red |
| 2.3 | Update `MedicalCaseWorkflowController.UpdateStatus` param type: `UpdateStatusRequest` -> `MedicalCaseStatusInputDto` | `MedicalCaseWorkflowController.cs` | Green |
| 2.4 | Update `MedicalCaseWorkflowController.CancelMedicalCase` param type: `CancelMedicalCaseRequest` -> `CancelMedicalCaseRequestDto` | `MedicalCaseWorkflowController.cs` | Green |
| 2.5 | Remove local DTO class copies from test file (lines 395-411) | `MedicalCaseWorkflowControllerTests.cs` | Green |
| 2.6 | Add `using LYBT.Shared.Models.Contracts.MedicalCase;` to test file if missing | `MedicalCaseWorkflowControllerTests.cs` | Green |
| 2.7 | Update test assertions: `request.Status` -> `dto.Status` (field name same, type changes) | `MedicalCaseWorkflowControllerTests.cs` | Green |
| 2.8 | Run `dotnet test tests/LYBT.Tests.Server.Unit/ --filter MedicalCaseWorkflow` -- all pass | - | Verify |
| 2.9 | Run `dotnet build src/Server/Services/LYBT.WebAPI/` -- success | - | Verify |

**Files modified:**
- `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseWorkflowController.cs`
- `tests/LYBT.Tests.Server.Unit/Controllers/MedicalCaseWorkflowControllerTests.cs`

**Key change in MedicalCaseWorkflowController.cs:**
```csharp
// Line 49: Change param type
[FromBody] MedicalCaseStatusInputDto request)  // was: UpdateStatusRequest

// Line 140: Change param type
[FromBody] CancelMedicalCaseRequestDto? request = null)  // was: CancelMedicalCaseRequest
```

---

## Task 2.3: Delete Old MedicalCaseController

**Goal:** Remove the 840-line dead code file.

**Pre-condition:** Tasks 2.1 and 2.2 complete. All tests pass.

### Sub-tasks

| # | Action | Files | TDD Phase |
|---|--------|-------|-----------|
| 3.1 | Verify no runtime code references `MedicalCaseController` class name (grep result: only comments and worktrees) | - | Verify |
| 3.2 | Delete `MedicalCaseController.cs` | `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs` | - |
| 3.3 | Run `dotnet build LYBTZYZS.sln` -- success | - | Verify |
| 3.4 | Run `dotnet test tests/LYBT.Tests.Architecture/` -- AR001 passes | - | Verify |
| 3.5 | Run `dotnet test tests/LYBT.Tests.Server.Unit/` -- all pass | - | Verify |
| 3.6 | Run `dotnet test tests/LYBT.Tests.Integration/` -- all pass | - | Verify |

**Files deleted:**
- `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs`

---

## Task 2.4: Add CancellationToken to New Controllers

**Goal:** Match project convention (HerbsController pattern: `CancellationToken cancellationToken = default`).

### Convention (from HerbsController.cs:40-45)

```csharp
public async Task<IActionResult> GetList(
    CancellationToken cancellationToken = default,
    [FromQuery] int page = 1, ...)
```

CancellationToken is the **first parameter** with default value.

### Sub-tasks per controller

**2.4a: MedicalCasesController (12 methods)**

| # | Method | HTTP | Params to add CT |
|---|--------|------|------------------|
| 1 | CreateMedicalCase | POST | Body DTO + CT |
| 2 | SetPrescriptionFlag | PUT | id, Body DTO + CT |
| 3 | Save | PUT | id, Body DTO + CT |
| 4 | DeleteMedicalCase | DELETE | id + CT |
| 5 | BatchDelete | POST | Body DTO + CT |
| 6 | GetBatchDetails | POST | Body DTO + CT |
| 7 | GetById | GET | id + CT |
| 8 | GetList | GET | Query params + CT |
| 9 | GetMedicalCases | GET | Query params + CT |
| 10 | SearchMedicalCases | GET | Query params + CT |
| 11 | GetConsultationList | GET | id + CT |
| 12 | GetPrescriptionList | GET | id + CT |

**2.4b: MedicalCaseWorkflowController (4 methods)**

| # | Method | HTTP | Params to add CT |
|---|--------|------|------------------|
| 1 | UpdateStatus | PUT | id, Body DTO + CT |
| 2 | CloseMedicalCase | PUT | id + CT |
| 3 | Suspend | PUT | id, Body DTO? + CT |
| 4 | CancelMedicalCase | PUT | id, Body DTO? + CT |

**2.4c: MedicalCasePrintController (2 methods)**

| # | Method | HTTP | Params to add CT |
|---|--------|------|------------------|
| 1 | RecordPrintCompleted | PUT | id, Body DTO + CT |
| 2 | AddPrintLog | POST | id, Body DTO + CT |

**2.4d: MedicalCaseAuditController (2 methods)**

| # | Method | HTTP | Params to add CT |
|---|--------|------|------------------|
| 1 | GetPermissions | GET | id + CT |
| 2 | GetAuditLogs | GET | id, Query params + CT |

### TDD Cycle for each controller

| # | Action | Files |
|---|--------|-------|
| 1 | Add `CancellationToken cancellationToken = default` as last parameter to all async action methods | Controller |
| 2 | Pass `cancellationToken` to all `_facade.*Async(...)` calls | Controller |
| 3 | Update unit tests: verify token is passed through (or use `CancellationToken.None`) | Tests |
| 4 | Run `dotnet build` + `dotnet test` | - |

**Files modified:**
- `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCasesController.cs`
- `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseWorkflowController.cs`
- `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCasePrintController.cs`
- `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseAuditController.cs`

---

## Task 2.5: Update Documentation

**Goal:** Reflect new controller structure in all docs.

### Sub-tasks

| # | File | Change |
|---|------|--------|
| 5.1 | `docs/04-api-reference/medical-cases.md` line 3 | `Controller: MedicalCaseController` -> `Controllers: MedicalCasesController, MedicalCaseWorkflowController, MedicalCasePrintController, MedicalCaseAuditController` |
| 5.2 | `docs/04-api-reference/medical-cases.md` line 9 | Remove `MedicalCaseAuthorizationHandler` reference (Authorization/ deleted) |
| 5.3 | `src/Server/CLAUDE.md` line 37 | Update controller table: split MedicalCaseController row into 4 rows |
| 5.4 | `src/Server/Services/LYBT.WebAPI/CLAUDE.md` Controllers section | Update MedicalCaseController entry: change to note it's deleted, add 4 new controller entries |
| 5.5 | `src/Server/Services/LYBT.WebAPI/CLAUDE.md` lines 448-451 | Remove/update internal DTOs table (DTOs moved to Shared) |

**Files modified:**
- `docs/04-api-reference/medical-cases.md`
- `src/Server/CLAUDE.md`
- `src/Server/Services/LYBT.WebAPI/CLAUDE.md`

---

## Task 2.6: Full Verification Gate

**Goal:** Ensure everything works end-to-end.

### Verification Steps

| # | Command | Expected |
|---|---------|----------|
| 6.1 | `dotnet build LYBTZYZS.sln` | 0 errors, 0 warnings |
| 6.2 | `dotnet test tests/LYBT.Tests.Architecture/` | All pass (AR001 validates new controllers) |
| 6.3 | `dotnet test tests/LYBT.Tests.Server.Unit/` | All pass |
| 6.4 | `dotnet test tests/LYBT.Tests.Integration/` | All pass |
| 6.5 | Manual: run WebAPI, hit all 20 MedicalCase endpoints | 200/204 responses |

---

## Atomic Commit Strategy

| Commit | Task(s) | Message |
|--------|---------|---------|
| 1 | 2.1 | `test(arch): update AR001 to validate new split controllers - Issue #1612` |
| 2 | 2.2 | `refactor(api): migrate MedicalCaseWorkflowController to Shared DTOs - Issue #1612` |
| 3 | 2.3 | `refactor(api): delete deprecated MedicalCaseController - Issue #1612` |
| 4 | 2.4 | `feat(api): add CancellationToken to MedicalCase controllers - Issue #1612` |
| 5 | 2.5 | `docs: update API references for new MedicalCase controller split - Issue #1612` |

Each commit is independently buildable and testable.

---

## Risk Assessment

| Risk | Mitigation |
|------|------------|
| AR001 test fails after old controller deletion | Task 2.1 must complete BEFORE Task 2.3 |
| DTO field mismatch (StatusChangeReason) | MedicalCaseStatusInputDto has superset of fields; workflow controller only reads `Status` |
| Integration tests reference old controller | MedicalCaseFlowTests.cs line 14 is a comment only; no code dependency |
| CancellationToken breaks facade method signatures | Facade methods already accept CT? Need to verify; if not, pass `default` |
| Worktree branches have old references | Worktrees are independent; main repo changes don't affect them |

---

## File Change Summary

| File | Action | Task |
|------|--------|------|
| `tests/.../AggregateRootArchTests.cs` | Edit | 2.1 |
| `tests/.../ArchTests.cs` | Edit | 2.1 |
| `tests/.../MedicalCaseWorkflowControllerTests.cs` | Edit | 2.2 |
| `src/.../MedicalCaseWorkflowController.cs` | Edit | 2.2, 2.4 |
| `src/.../MedicalCaseController.cs` | Delete | 2.3 |
| `src/.../MedicalCasesController.cs` | Edit | 2.4 |
| `src/.../MedicalCasePrintController.cs` | Edit | 2.4 |
| `src/.../MedicalCaseAuditController.cs` | Edit | 2.4 |
| `docs/04-api-reference/medical-cases.md` | Edit | 2.5 |
| `src/Server/CLAUDE.md` | Edit | 2.5 |
| `src/Server/Services/LYBT.WebAPI/CLAUDE.md` | Edit | 2.5 |

**Total:** 10 files edited, 1 file deleted.

# MedicalCase MediatR Simplification Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove trivial MedicalCase MediatR indirection by injecting existing `IMedicalCaseQueryService`, `IMedicalCaseCommandService`, and `IMedicalCaseStateService` directly into controllers for the operations that currently add no business logic beyond a service/repo call.

**Architecture:** Keep complex MediatR handlers (`SaveMedicalCaseCommand`, `BatchDeleteMedicalCasesCommand`, `QueryMedicalCasesCommand`, `RecordPrintCommand`, `GetMedicalCaseAuditLogsQuery`, `GetMedicalCasePermissionsQuery`, `GetPatientConsultationsQuery`, `GetPatientPrescriptionsQuery`) unchanged. Replace only the controller endpoints whose current handler is a pure pass-through to an existing service method, then delete the now-unused handler files and their command/query definition files.

**Tech Stack:** .NET 8 / ASP.NET Core / EF Core / MediatR. Verify with `dotnet build LYBTZYZS.sln`, `dotnet test tests/LYBT.Tests.Architecture/`, and targeted `MedicalCasesControllerTests` via `dotnet test tests/LYBT.Tests.Desktop/`.

## Global Constraints

- Do not change shared DTOs/Contracts.
- Do not change external API routes.
- Keep `Service` and `Repository` implementations unchanged.
- Keep complex handlers in place; remove only trivial handlers that are fully replaced by direct service calls.
- Preserve existing controller response shaping (`Success(...)` / `BusinessFail(...)` / `NotFound(...)` / `SuccessPaged(...)`).
- Commit message: `refactor(medicalcase): remove trivial MediatR handlers, inject services directly`.

---

### Task 1: Classify removable MedicalCase MediatR artifacts

**Covers:** Phase 1 analysis

**Files:**
- Inspect: `src/Server/Modules/LYBT.Module.MedicalCase/Application/**/*.cs`
- Inspect: `src/Server/Modules/LYBT.Module.MedicalCase/Controllers/BaseMedicalCasesController.cs`
- Inspect: `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCasesController.cs`
- Inspect: `src/Client/Desktop/LocalWebAPI/Controllers/MedicalCasesController.cs`

**Interfaces:**
- Consumes: existing controller action signatures and handler implementations
- Produces: exact deletion/change lists for Tasks 2-5

- [ ] **Step 1: Confirm direct-service-supported operations**

Confirm the following operations already have direct service methods:
- `IMedicalCaseQueryService.GetListDtoAsync(...)` for list
- `IMedicalCaseQueryService.GetByIdAsync(...)` for detail fetch + mapper
- `IMedicalCaseQueryService.GetConsultationListAsync(...)` for consultations
- `IMedicalCaseQueryService.GetPrescriptionListAsync(...)` for prescriptions
- `IMedicalCaseQueryService.GetBatchAsync(...)` for batch detail fetch
- `IMedicalCaseCommandService.SetPrescriptionFlagAsync(...)` for prescription flag
- `IMedicalCaseCommandService.DeleteAsync(...)` for delete
- `IMedicalCaseStateService.CompleteAsync(...)` for complete/close
- `IMedicalCaseStateService.SuspendAsync(...)` for suspend
- `IMedicalCaseStateService.UpdateStatusAsync(...)` for update status
- `IMedicalCaseStateService.CancelAsync(...)` for cancel

Expected: all present in `IMedicalCaseQueryService`, `IMedicalCaseCommandService`, `IMedicalCaseStateService`.

- [ ] **Step 2: Confirm trivial removable handler set**

Confirm these handler+definition files are pure delegation and can be removed once controllers call services directly:
- `Application/Commands/CreateMedicalCaseCommandHandler.cs`
- `Application/Commands/CreateMedicalCaseCommand.cs`
- `Application/Commands/CompleteMedicalCaseCommandHandler.cs`
- `Application/Commands/CompleteMedicalCaseCommand.cs`
- `Application/Commands/SuspendMedicalCaseCommandHandler.cs`
- `Application/Commands/SuspendMedicalCaseCommand.cs`
- `Application/Commands/UpdateMedicalCaseStatusCommandHandler.cs`
- `Application/Commands/UpdateMedicalCaseStatusCommand.cs`
- `Application/Commands/SetPrescriptionFlagCommandHandler.cs`
- `Application/Commands/SetPrescriptionFlagCommand.cs`
- `Application/Commands/DeleteMedicalCaseCommandHandler.cs`
- `Application/Commands/DeleteMedicalCaseCommand.cs`
- `Application/Queries/GetMedicalCaseQueryHandler.cs`
- `Application/Queries/GetMedicalCaseQuery.cs`
- `Application/Queries/GetMedicalCasesQueryHandler.cs`
- `Application/Queries/GetMedicalCasesQuery.cs`
- `Application/Queries/GetMedicalCasesBatchQueryHandler.cs`
- `Application/Queries/GetMedicalCasesBatchQuery.cs`
- `Application/Queries/GetMedicalCaseConsultationsQueryHandler.cs`
- `Application/Queries/GetMedicalCaseConsultationsQuery.cs`
- `Application/Queries/GetMedicalCasePrescriptionsQueryHandler.cs`
- `Application/Queries/GetMedicalCasePrescriptionsQuery.cs`

Expected: no other production code references these types except the targeted controllers/handlers.

- [ ] **Step 3: Confirm exceptions that stay on MediatR**

Confirm the following stay unchanged:
- `SaveMedicalCaseCommand` + `SaveMedicalCaseCommandHandler`
- `BatchDeleteMedicalCasesCommand` + `BatchDeleteMedicalCasesCommandHandler`
- `QueryMedicalCasesCommand` + `QueryMedicalCasesCommandHandler`
- `RecordPrintCommand` + `RecordPrintCommandHandler`
- `GetMedicalCaseAuditLogsQuery` + handler
- `GetMedicalCasePermissionsQuery` + handler
- `GetPatientConsultationsQuery` + handler
- `GetPatientPrescriptionsQuery` + handler
- `SearchMedicalCasesQuery` + handler

Expected: these retain complex logic or have no ready direct-service replacement.

---

### Task 2: Inject direct services into controllers

**Covers:** Phase 2 controller refactor

**Files:**
- Modify: `src/Server/Modules/LYBT.Module.MedicalCase/Controllers/BaseMedicalCasesController.cs`
- Modify: `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCasesController.cs`
- Modify: `src/Client/Desktop/LocalWebAPI/Controllers/MedicalCasesController.cs`

**Interfaces:**
- Consumes: service interfaces above
- Produces: controller endpoints now return the same `IActionResult` envelope without MediatR for the selected operations

- [ ] **Step 1: Add service dependencies to base MedicalCase controller**

Update `BaseMedicalCasesController` constructor to inject:
- `IMedicalCaseQueryService medicalCaseQueryService`
- `IMedicalCaseCommandService medicalCaseCommandService`
- `IMedicalCaseStateService medicalCaseStateService`

Store as `protected readonly` fields and keep `ISender`/`ILogger` for operations that still use MediatR.

- [ ] **Step 2: Convert direct-supported query actions in base controller**

Replace MediatR usage with direct service calls for:
- `GetConsultations`
- `GetPrescriptions`
- `GetBatchDetails`
- list/default `GetListDtoAsync` path if applicable in base

Map returned entities/Dtos to existing controller success wrappers. Keep route, parameters, and response envelope unchanged.

- [ ] **Step 3: Convert direct-supported command/state actions in base controller**

Replace MediatR usage for:
- `SetPrescriptionFlag(...)` -> `medicalCaseCommandService.SetPrescriptionFlagAsync(...)`
- complete/close/suspend/update-status/cancel where appropriate via `medicalCaseStateService` methods

Preserve existing success/failure message strings and response types from the current controller actions.

- [ ] **Step 4: Align remote controller overrides**

Update `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCasesController.cs` to use the same direct-service path for overridden actions where the base refactor applies. Keep remote-specific behavior like logging/rate limiting/response shaping intact, only replacing the `Sender.Send(...)` call with the matching service call.

- [ ] **Step 5: Align local controller overrides**

Update `src/Client/Desktop/LocalWebAPI/Controllers/MedicalCasesController.cs` likewise. Keep its existing action names and local behavior, but replace `Sender.Send(...)` for the same set of removable operations.

---

### Task 3: Delete trivial handler and definition files

**Covers:** Phase 2 cleanup

**Files:**
- Delete: all handler+definition files listed in Task 1 Step 2
- Modify: controller files only if additional unused `using` statements remain

**Interfaces:**
- Consumes: confirmation from Task 1 that no other production references exist
- Produces: smaller `Application` folder with only complex handlers

- [ ] **Step 1: Remove trivial command/query definitions and handlers**

Delete the 22 files identified in Task 1 Step 2.

- [ ] **Step 2: Remove now-unused usings**

In the three controller files, remove imports that only existed for the deleted command/query/handler types.

- [ ] **Step 3: Build the solution**

Run:
```bash
dotnet build LYBTZYZS.sln
```
Expected: 0 errors.

---

### Task 4: Verify routes, architecture, and MedicalCase tests

**Covers:** Phase 3 verification

**Files:**
- Verify: `tests/LYBT.Tests.Architecture/`
- Verify: `tests/LYBT.Tests.Desktop/Integration/LocalWebAPI/MedicalCasesControllerTests.cs`

**Interfaces:**
- Consumes: compiled solution and test suite
- Produces: evidence that refactor is safe

- [ ] **Step 1: Run architecture tests**

```bash
dotnet test tests/LYBT.Tests.Architecture/
```
Expected: all pass.

- [ ] **Step 2: run MedicalCase desktop integration tests**

```bash
dotnet test tests/LYBT.Tests.Desktop/ --filter MedicalCasesControllerTests
```
Expected: selected MedicalCase controller tests pass, confirming route/behavior stability.

- [ ] **Step 3: Commit**

```bash
git add src/Server/Modules/LYBT.Module.MedicalCase/Application src/Server/Modules/LYBT.Module.MedicalCase/Controllers src/Server/Services/LYBT.WebAPI/Controllers src/Client/Desktop/LocalWebAPI/Controllers
git commit -m "refactor(medicalcase): remove trivial MediatR handlers, inject services directly"
```

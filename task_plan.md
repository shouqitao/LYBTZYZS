# Task Plan: LYBTZYZS Frontend Completion

> **Version**: v2.1
> **Updated**: 2026-04-06
> **Status**: In Progress
> **Scope**: Desktop WPF Client (`src/Client/Desktop/`)

---

## Goal
Complete remaining frontend tasks to achieve 100% PRD coverage for all 76 user stories across 6 modules.

## Current Phase
Phase 7

## Phases

### Phase 1: IUserService + RemoteUserService (T1-1) ✅
- [x] Create IUserService interface
- [x] Create RemoteUserService implementation
- **Status:** complete

### Phase 2: IHerbService + RemoteHerbService (T1-2) ✅
- [x] Create IHerbService interface
- [x] Create RemoteHerbService implementation
- **Status:** complete

### Phase 3: IRegistrationService + RemoteRegistrationService (T1-3) ✅
- [x] Create IRegistrationService interface
- [x] Create RemoteRegistrationService implementation
- **Status:** complete

### Phase 4: IFormulaService Enhancement (T1-4) ✅
- [x] Add BatchImportAsync, ExportFormulasAsync, ExportTemplateAsync methods
- [x] Update IFormulaRepository + FormulaRepository + LocalFormulaRepository
- **Status:** complete

### Phase 5: IPatientService Enhancement (T1-5) ✅
- [x] Add BatchImportAsync, ExportTemplateAsync, ExportPatientsAsync methods
- [x] Update PatientService implementation
- **Status:** complete

### Phase 6: Registration Module (US-REG-001~004) ✅
- [x] T2-1: RegistrationCreateDialog (US-REG-001)
- [x] T2-2: Doctor Quick Visit navigation fix (US-REG-002)
- [x] T2-3: Cancel Registration (US-REG-004)
- [x] T2-4: Registration Tests (32 PureLogic tests)
- **Status:** complete

### Phase 7: Registration Status Sync (US-REG-005, 006, 007) ✅
- [x] T7-1: Auto-refresh timer (30-second periodic refresh)
- [x] T7-2: Queue refresh on navigation + timer
- [x] T7-3: Registration history query API (date/patient/doctor filters)
- **Status:** complete

### Phase 8: Verification Tasks — BLOCKED (Could priority, server APIs needed)
- [~] T8-1: US-PAT-011/012 — Patient reference check (Could, server API needed)
- [~] T8-2: US-FORM-010 — Formula pending validation (No explicit US)
- [~] T8-3: US-HERB-013 — Herb reference check (Could, server API needed)
- [~] T8-4: US-MC-012 — MedicalCase audit log (API exists, UI deferred)
- **Status:** blocked — all "Could" priority, skip for now

### Phase 9: Testing & Verification ✅
- [x] Run desktop tests (627 PureLogic tests pass)
- [x] Build verification (0 errors, 0 warnings)
- [~] E2E tests require server running (not a code issue)
- **Status:** complete

## Decisions Made
| Decision | Rationale |
|----------|-----------|
| Follow existing Service patterns | Maintain consistency with IPatientService, IMedicalCaseService |
| Add CancellationToken to all methods | Matches T6-2 requirement for Service interfaces |
| Use RemoteXxxService naming | Consistent with existing RemotePatientService |
| Status sync via server-side | Desktop client only refreshes queue on event receipt |

## Errors Encountered
| Error | Attempt | Resolution |
|-------|---------|------------|
| RMG020 unmapped members | 1 | Added MapperIgnoreSource attributes |
| CA1001 disposable field | 1 | Implemented IDisposable in DatabaseInitializer |

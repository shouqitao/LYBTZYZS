# Findings: Test Cleanup + US Test Fixes

## Phase 2 Coverage Audit (2026-03-10)

### Must Have US Coverage Summary

| Module | PRD Must Have | Tests Covered | Coverage | Gap |
|--------|-------------|--------------|---------|-----|
| Auth | 8 | 8* | 100%* | US-AUTH-012 (Desktop UI, not server-testable) |
| Users | 5 | 5 | 100% | - |
| Patients | 4 | 4 | 100% | - |
| Herbs | 5 | 5 | 100% | - |
| Formulas | 6 | 6 | 100% | - |
| MedicalCases | 9 | 9 | 100% | - |
| Registration | 6 | 6 | 100% | - |
| Config | 2 | 2 | 100% | - |
| Sync | 1 | 1 | 100% | - |
| **Total** | **46** | **45+1 extra** | **97.8%** | 1 Desktop-only |

*Auth tests include US-AUTH-007 (Token validation, not in Must Have PRD but security-critical)

### US with Thin Coverage (1 test only)

| US ID | Module | Current Tests | Missing Coverage |
|-------|--------|--------------|-----------------|
| US-AUTH-005 | Auth | 1 (happy path) | Logout with invalid token, double logout |
| US-AUTH-009 | Auth | 1 (role info) | Missing: doctor/nurse role validation |
| US-USER-003 | Users | 1 (update fields) | Missing: partial update, invalid data, unauthorized |
| US-MC-002 | MedicalCases | 1 (save diagnosis) | Missing: empty diagnosis, invalid fields |
| US-MC-003 | MedicalCases | 1 (add prescription) | Missing: empty items, invalid herb refs |
| US-MC-004 | MedicalCases | 1 (complete case) | Missing: complete without consultation, double complete |
| US-MC-005 | MedicalCases | 1 (cancel with reason) | Missing: cancel without reason (BR-006), cancel completed |
| US-MC-007 | MedicalCases | 1 (print flag) | Missing: print incomplete case (BR-003) |
| US-FORM-005 | Formulas | 1 (shared formula) | Missing: unshare, visibility check by other user |
| US-REG-002 | Registration | 1 (get queue) | Missing: empty queue, filtered queue |
| US-REG-003 | Registration | 1 (start visit) | Missing: start already-started, start cancelled |
| US-REG-005 | Registration | 1 (list) | Missing: pagination, date filter |

**Priority**: MedicalCase tests (BR-001/003/004/006 business rules) > Registration > Auth > Others

## API Status Code Mapping (discovered during test fixes)

| Endpoint | HTTP Method | Success Status | Notes |
|----------|-------------|---------------|-------|
| POST /api/v1/patients | POST | 201 Created | Standard REST |
| POST /api/v1/herbs | POST | 200 OK | Non-standard (returns Ok, not Created) |
| POST /api/v1/medicalcases | POST | 200 OK | Non-standard (returns Ok, not Created) |
| PUT /api/v1/medicalcases/{id}/cancel | PUT | 204 NoContent | Soft delete, no body |
| DELETE /api/v1/medicalcases/{id} | DELETE | 204 NoContent | Soft delete, no body |
| PUT /api/v1/registrations/{id}/cancel | PUT | 422 UnprocessableEntity | BusinessFail for not-found (not 404) |

**Inconsistency**: POST endpoint status codes not unified -- Patients returns 201, others return 200.

## MedicalCaseInputDto Validation Requirements

- `PatientId` and `UserId` always required (MedicalCaseInputDtoValidator)
- Nested `PrescriptionInputDto.MedicalCaseId` required on create (PrescriptionInputDtoValidator)
- Test BuildUpdate/BuildPrescription must include these fields

## UserJourneys Analysis (from Phase 0)

### Value Assessment
| Level | Tests | Key Coverage |
|-------|-------|-------------|
| VERY HIGH | CrossNarrativeValidation (8), FirstVisitJourney (5), BootstrapJourney (1) | AD probes, BR-001/003, RBAC matrix |
| HIGH | AuthJourney, DoctorClinical, HerbFormulaManagement, ReturnVisit | E2E business flows |
| MODERATE | AdminSetup, BatchOperations, MedicalCaseEdit, PatientManagement | Simpler flows |

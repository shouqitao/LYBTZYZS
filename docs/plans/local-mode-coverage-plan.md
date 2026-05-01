# Local Mode Feature Coverage Improvement Plan

## Executive Summary

**Scope**: Increase LocalWebAPI feature coverage from ~31% to ~85% across 6 modules (MedicalCase, Patients, Herbs, Formulas, Users, Auth) by adding ~50 missing endpoints and replacing ~40+ Http*Repository stubs with real HTTP calls.

**Approach**: 5-phase rollout prioritized by user impact (workflow-critical → batch ops → security → tail features). Each phase pairs server-side controller endpoints with client-side Http*Repository implementations.

**Constraints honored**:
- TBD-01 exclusions: no token refresh, no audit log queries, no user sync, no auto-login
- LocalWebAPI architecture: direct `LocalWebApiDbContext`, no service layer, no DTOs
- Soft-delete via `IsDeleted = true`
- Excel import/export handled on client side — server provides JSON API only
- Reference checks use inline DbContext queries
- Permissions return permissive results for offline single-user scenario

## Design Decisions (User-Confirmed)

1. **引用检查**: 内联查询检查 — Use inline DbContext queries for reference checks before delete
2. **Excel 导入导出**: Server provides JSON API, client handles Excel conversion. Both Remote and Local modes follow this approach.
3. **权限**: 返回宽松权限 — Return permissive permissions (CanEdit=true, CanClose=true, etc.) for offline single-user scenario

---

## Phase 1 (P0 — Critical): MedicalCase Workflow

### Tasks

| Task | Description | Depends On |
|------|-------------|------------|
| T1.1 | MedicalCase lifecycle endpoints (Close/Suspend/Cancel) | None |
| T1.2 | MedicalCase query/search endpoints (Search/Query/BatchDetails/Permissions/GetByStatus) | None |
| T1.3 | MedicalCase status/print/batch-delete endpoints | T1.1 |
| T1.4 | HttpMedicalCaseRepository — implement 12 stubs | T1.1, T1.2, T1.3 |
| T1.5 | MedicalCase E2E integration tests | T1.4 |

### Endpoints to Add (8 new on MedicalCasesController)

```
POST   /api/medicalcases/{id}/close           → CloseCase
POST   /api/medicalcases/{id}/suspend          → Suspend
POST   /api/medicalcases/{id}/cancel           → Cancel (soft-delete + registration rollback)
GET    /api/medicalcases/search                → Search (patientName, diagnosisKeyword, date range, pagination)
POST   /api/medicalcases/query                 → Query (MedicalCaseQueryDto)
POST   /api/medicalcases/batch-details         → GetBatchDetails (List<Guid> ids)
GET    /api/medicalcases/{id}/permissions       → GetPermissions (permissive for offline)
PUT    /api/medicalcases/{id}/prescription-flag → SetPrescriptionFlag
PUT    /api/medicalcases/{id}/status            → UpdateStatus
POST   /api/medicalcases/{id}/print-completed   → RecordPrintCompleted
POST   /api/medicalcases/save                   → SaveAsync (upsert)
DELETE /api/medicalcases/batch                  → BatchDelete
```

### HttpMedicalCaseRepository Methods to Implement (12)

```
SearchAsync, QueryAsync, CloseCaseAsync, GetPermissionsAsync, SaveAsync,
GetBatchDetailsAsync, SetPrescriptionFlagAsync, UpdateStatusAsync,
CancelMedicalCaseAsync, SuspendAsync, RecordPrintCompletedAsync, BatchDeleteAsync
```

---

## Phase 2 (P1 — High): Patients / Herbs / Formulas Batch Operations

### Tasks

| Task | Description | Depends On |
|------|-------------|------------|
| T2.1 | Patients controller (BatchDelete/Restore + JSON export/import API) | None |
| T2.2 | Herbs controller (BatchDelete/BatchEnable/BatchDisable/Toggle/Restore + JSON export/import API) | None |
| T2.3 | Formulas controller (BatchDelete/Restore + JSON export/import API) | None |
| T2.4 | HttpPatientRepository implementation | T2.1 |
| T2.5 | HttpHerbRepository implementation | T2.2 |
| T2.6 | HttpFormulaRepository implementation | T2.3 |
| T2.7 | Phase 2 integration tests | T2.4, T2.5, T2.6 |

**Note**: Excel conversion happens client-side. Server endpoints return/accept JSON data only.

---

## Phase 3 (P2 — Medium): Auth Security

### Tasks

| Task | Description | Depends On |
|------|-------------|------------|
| T3.1 | UsersController.ChangePassword + Lock/Unlock + Roles | None |
| T3.2 | HttpAuthRepository / HttpUserRepository implementation | T3.1 |
| T3.3 | Phase 3 integration tests | T3.2 |

**TBD-01 excluded**: RefreshToken, RevokeToken, AutoLogin → return 501

---

## Phase 4 (P3 — Low): Diagnostics / Configuration / Tail Coverage

### Tasks

| Task | Description | Depends On |
|------|-------------|------------|
| T4.1 | DiagnosticsController (local) | None |
| T4.2 | ConfigurationController (local) | None |
| T4.3 | HealthController extensions | None |
| T4.4 | Advanced query endpoints | T1.2 |
| T4.5 | Phase 4 integration tests | T4.1-T4.4 |

---

## Phase 5 (Wrap-up): Documentation & Architecture Guards

| Task | Description | Depends On |
|------|-------------|------------|
| T5.1 | Update dual-mode.md coverage table | All phases |
| T5.2 | Architecture test for LocalWebAPI pattern compliance | All phases |

---

## Success Criteria

1. **Coverage**: ≥85% endpoint parity (≥80/102 endpoints)
2. **Tests**: ≥40 new integration tests, all green
3. **Zero stubs**: No `LogWarning("not supported")` except TBD-01 excluded methods
4. **Architecture**: All new architecture tests pass
5. **Docs**: `docs/03-architecture/dual-mode.md` coverage table updated

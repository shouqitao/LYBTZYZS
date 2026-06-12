# Phase 3: Should Have US Tests - Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add Should Have priority US tests to achieve comprehensive API coverage beyond core Must Have flows.

**Architecture:** Same pattern as Phase 2 -- each module gets a `US_{Module}_ShouldHaveTests.cs` file using domain-specific Collection + generic base class. Tests use TestDataBuilders + BusinessAssertions.

**Tech Stack:** .NET 8, xUnit 2.x, Respawn, FluentAssertions, SQL Server LocalDB, WebApplicationFactory

---

## Scope

54 Should Have US total, 47 server-testable, 7 Desktop-only (skipped).

## Priority Batches (by business value)

| Batch | Modules | US Count | Tests (est.) | Why First |
|-------|---------|----------|-------------|-----------|
| 1 | Users | 5 | ~12 | All endpoints exist, quick wins |
| 2 | Herbs + Formulas | 8 | ~18 | Import/export/batch, data integrity |
| 3 | Patients + Registration | 3 | ~7 | Completes CRUD coverage |
| 4 | Error Handling | 6 | ~12 | Infrastructure quality |
| 5 | MedicalCase | 8 | ~20 | Most complex, highest value |
| 6 | Sync | 7 | ~14 | Dual-mode critical path |
| 7 | Auth + Config + Logging | 10 | ~15 | Security + infrastructure |

---

## Batch 1: Users Should Have (5 US)

### Task 1.1: Create US_User_ShouldHaveTests.cs

**Files:**
- Create: `tests/LYBT.Tests.Server/Features/Users/US_User_ShouldHaveTests.cs`

**US Coverage:**

| US ID | Description | API Endpoint | Tests |
|-------|-------------|-------------|-------|
| US-USER-008 | Admin reset password | POST /api/v1/users/{id}/reset-password | 2 (success + unauthorized) |
| US-USER-009 | User change password | PUT /api/v1/users/{id}/change-password | 3 (success + wrong old pwd + weak pwd) |
| US-USER-010 | Edit personal profile | PUT /api/v1/users/{id}/profile | 2 (success + unauthorized) |
| US-USER-011 | Enable/disable user | POST /api/v1/users/{id}/toggle-status | 2 (disable + re-enable) |
| US-USER-012 | Get current user | GET /api/v1/users/current | 2 (success + anonymous) |

**Run:** `dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~US_User_ShouldHaveTests"`

---

## Batch 2: Herbs + Formulas Should Have (8 US)

### Task 2.1: Create US_Herb_ShouldHaveTests.cs

**US Coverage:**

| US ID | Description | API Endpoint | Tests |
|-------|-------------|-------------|-------|
| US-HERB-006 | Enable/disable herb | POST /api/v1/herbs/{id}/toggle-status | 2 |
| US-HERB-008 | Batch delete | POST /api/v1/herbs/batch-delete | 2 (success + ref check) |
| US-HERB-009 | Import herbs | POST /api/v1/herbs/import or batch-import | 2 (success + invalid) |
| US-HERB-011 | Export herbs | GET /api/v1/herbs/export | 2 (success + empty) |

### Task 2.2: Create US_Formula_ShouldHaveTests.cs

| US ID | Description | API Endpoint | Tests |
|-------|-------------|-------------|-------|
| US-FORM-008 | Share formula | POST /api/v1/formulas/{id}/toggle-status (shared) | 2 |
| US-FORM-009 | Lazy binding validate | POST /api/v1/formulas/{id}/herbs/{itemId}/validate | 2 |
| US-FORM-010 | Pending verification | GET /api/v1/formulas/pending-validation | 2 |
| US-FORM-012 | Export formulas | GET /api/v1/formulas/export | 2 |

---

## Batch 3: Patients + Registration Should Have (3 US)

### Task 3.1: Add to US_Patient_MustHaveTests or create ShouldHave

| US ID | Description | API Endpoint | Tests |
|-------|-------------|-------------|-------|
| US-PAT-005 | Delete patient | DELETE /api/v1/patients/{id} | 2 (success + ref check) |
| US-PAT-013 | Status management | POST /api/v1/patients/{id}/toggle-status | 2 |

### Task 3.2: Add US_Registration_ShouldHaveTests.cs

| US ID | Description | API Endpoint | Tests |
|-------|-------------|-------------|-------|
| US-REG-007 | Registration history | GET /api/v1/registrations/query | 2 (with date filter + pagination) |

---

## Batch 4: Error Handling Should Have (5 US, skip ERR-003 Desktop)

### Task 4.1: Create US_ErrorHandling_ShouldHaveTests.cs

| US ID | Description | Test Approach | Tests |
|-------|-------------|--------------|-------|
| US-ERR-001 | Global exception | Trigger 500 via invalid payload | 2 |
| US-ERR-002 | ProblemDetails format | Verify error response structure | 2 |
| US-ERR-004 | Exception type system | Verify 400/422/404 types | 3 |
| US-ERR-005 | Severity classification | Verify error codes in responses | 2 |
| US-ERR-006 | Error message mapping | Verify localized messages | 2 |

---

## Batch 5: MedicalCase Should Have (8 US)

### Task 5.1: Create US_MedicalCase_ShouldHaveTests.cs

| US ID | Description | API Endpoint | Tests |
|-------|-------------|-------------|-------|
| US-MC-008 | Cancel medical case | PUT /api/v1/medicalcases/{id}/cancel | 2 (overlap with Must Have, focus edge cases) |
| US-MC-010 | Cross-case search | GET /api/v1/medicalcases/search | 3 (by diagnosis, by date, by patient) |
| US-MC-011 | Edit mode state machine | PUT /api/v1/medicalcases/{id}/status | 2 (state transitions) |
| US-MC-014 | Locking rules | GET /api/v1/medicalcases/{id}/permissions | 2 |
| US-MC-015 | Print trigger | POST /api/v1/medicalcases/{id}/print-logs | 2 |
| US-MC-016 | Formula import to Rx | via update with formula reference | 2 |
| US-MC-017 | Waiting queue | GET /api/v1/registrations/queue | 1 (already partially tested) |
| US-MC-018 | Copy historical Rx | GET /api/v1/medicalcases/{id}/prescriptions + POST | 2 |

---

## Batch 6: Sync Should Have (7 US)

### Task 6.1: Create US_Sync_ShouldHaveTests.cs

| US ID | Description | API Endpoint | Tests |
|-------|-------------|-------------|-------|
| US-SYNC-001 | Get syncable entities | GET /api/v1/sync/entity-types | 2 |
| US-SYNC-002 | Get sync metadata | GET /api/v1/sync/metadata | 2 |
| US-SYNC-003 | Data comparison | POST /api/v1/sync/compare | 2 |
| US-SYNC-004 | Upload changes | POST /api/v1/sync/upload | 2 |
| US-SYNC-005 | Download changes | POST /api/v1/sync/download | 2 |
| US-SYNC-006 | Sync deletion | POST /api/v1/sync/delete | 2 |
| US-SYNC-007 | Full sync workflow | Combined | 1 |

---

## Batch 7: Auth + Config + Logging Should Have (10 US)

### Task 7.1: Create US_Auth_ShouldHaveTests.cs

| US ID | Description | Test Approach | Tests |
|-------|-------------|--------------|-------|
| US-AUTH-004 | Replay detection | Reuse token after refresh | 2 |
| US-AUTH-006 | Inactivity timeout | Token expiry behavior | 1 |
| US-AUTH-011 | Refresh failure escalation | Multiple refresh failures | 2 |
| US-AUTH-013 | Auth event system | Audit endpoint for auth events | 1 |

### Task 7.2: Create US_Config_ShouldHaveTests.cs

| US ID | Description | Test Approach | Tests |
|-------|-------------|--------------|-------|
| US-CFG-003 | Environment config | Diagnostics endpoint env info | 1 |
| US-CFG-004 | Startup validation | Health check details | 1 |

### Task 7.3: Create US_Logging_ShouldHaveTests.cs

| US ID | Description | API Endpoint | Tests |
|-------|-------------|-------------|-------|
| US-LOG-001 | Structured logging | GET /diagnostics/logging/status | 1 |
| US-LOG-002 | Audit logging | Via MC audit-logs endpoint | 1 |
| US-LOG-007 | API request logging | Diagnostics endpoint | 1 |

---

## Execution Strategy

Each batch: Create file -> Write tests -> Build -> Run -> Fix -> Next batch.
Total estimated: ~98 tests across 10 new files.

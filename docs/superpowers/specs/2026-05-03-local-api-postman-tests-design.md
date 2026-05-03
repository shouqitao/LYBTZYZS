# LocalWebAPI Postman Test Collection Design

**Date:** 2026-05-03
**Status:** Approved
**Scope:** 10 controllers, 97 endpoints, ~310 tests

## Overview

Design a comprehensive Newman/Postman test collection for the LocalWebAPI (`http://127.0.0.1:5290`), covering all 97 endpoints across 10 controllers with full test depth (happy path + negative + boundary + business rules).

## Architecture

### Output Files

```
tests/postman/
├── local-api-tests.postman_collection.json   # Main collection
├── local-api.environment.json                # Environment variables
└── run-tests.ps1                             # Newman runner script
```

### Environment Variables

| Variable | Purpose | Example |
|----------|---------|---------|
| `base_url` | LocalWebAPI address | `http://127.0.0.1:5290` |
| `auth_token` | JWT Token (set by login) | `eyJhbG...` |
| `admin_user_id` | Admin user ID from seed | GUID |
| `test_patient_id` | Test patient (set by setup) | GUID |
| `test_patient_id_2` | For batch operations | GUID |
| `test_patient_id_3` | For delete/restore | GUID |
| `test_herb_id` | Test herb | GUID |
| `test_herb_id_2` | For batch operations | GUID |
| `test_herb_id_3` | For delete/restore | GUID |
| `test_formula_id` | Test formula | GUID |
| `test_formula_id_2` | For clone/batch | GUID |
| `test_medical_case_id` | Test medical case | GUID |
| `test_medical_case_id_2` | For batch delete | GUID |
| `test_registration_id` | Test registration (Waiting) | GUID |
| `test_registration_id_2` | For cancel | GUID |

### Folder Structure (Execution Order)

```
00-Setup/          # Login + create seed test data
01-Health/         # 3 endpoints → 6 tests
02-Auth/           # 5 endpoints → 12 tests
03-Users/          # 14 endpoints → 28 tests
04-Patients/       # 12 endpoints → 25 tests
05-Herbs/          # 14 endpoints → 28 tests
06-Formulas/       # 15 endpoints → 30 tests
07-MedicalCases/   # 20 endpoints → 45 tests
08-Registrations/  # 9 endpoints → 20 tests
09-Diagnostics/    # 3 endpoints → 6 tests
10-Configuration/  # 2 endpoints → 5 tests
```

## Initialization (00-Setup)

1. `POST /api/auth/auto-login` (admin) → save `auth_token`, `admin_user_id`
2. `POST /api/patients` × 3 → save `test_patient_id`, `_2`, `_3`
3. `POST /api/herbs` × 3 → save `test_herb_id`, `_2`, `_3`
4. `POST /api/formulas` × 2 → save `test_formula_id`, `_2`
5. `POST /api/registrations` × 2 → save `test_registration_id`, `_2`
6. `POST /api/medicalcases` × 2 → save `test_medical_case_id`, `_2`

Test data uses `_test_` prefix names for identification. No cleanup needed (LocalWebAPI re-seeds on restart).

## Test Scenarios Per Module

### 01-Health (6 tests)
- GET /health: 200 + status field
- GET /health/ping: 200 + "ok"
- GET /health/details: 200 + database.connected

### 02-Auth (12 tests)
- POST /auth/auto-login: 200 + Token | 401 invalid user
- POST /auth/login: 200 + Token | 401 wrong password
- POST /auth/refresh: 200 new Token | 401 no token
- GET /auth/validate: 200 IsValid=true | 401 no token
- POST /auth/logout: 200 Success

### 03-Users (28 tests)
- GET /users: 200 list
- GET /users/{id}: 200 detail | 404 not found
- POST /users: 201 create | 400 duplicate username
- PUT /users/{id}: 204 update | 404 not found
- DELETE /users/{id}: 204 soft delete | 404 not found | verify IsDeleted
- PUT /change-password: 204 | 400 wrong old password
- POST /toggle-status: 200 | 404 | verify double-toggle returns to original
- POST /restore: 200 | 404
- POST /batch-delete: 200 | 400 empty list
- POST /batch-enable: 200
- POST /batch-disable: 200
- GET /users/current: 200 current user
- POST /reset-password: 200 + temp password | 404
- PUT /profile: 200 | 404

### 04-Patients (25 tests)
- GET /patients: 200 paged (verify pagination params)
- GET /patients/{id}: 200 | 404
- POST /patients: 201 | 400 invalid
- PUT /patients/{id}: 200 | 404
- DELETE /patients/{id}: 204 | 404
- GET /by-id-number/{idNumber}: 200 | 404
- POST /batch-delete: 200 | 400 empty
- POST /{id}/restore: 200 | 404
- POST /{id}/toggle-status: 200 | 404
- GET /export: 200 non-empty list
- GET /import-template: 200
- POST /import: 200 SuccessCount > 0 | 400 empty list

### 05-Herbs (28 tests)
- Same CRUD pattern as Patients
- GET /categories: 200 list of strings
- Batch operations: delete, enable, disable
- Import/export: template, export, batch-import

### 06-Formulas (30 tests)
- Same CRUD pattern as Herbs
- POST /{id}/clone: 201 new ID, name contains "副本"
- GET /categories: 200 list

### 07-MedicalCases (45 tests)
- CRUD: GET list, GET detail, POST create, DELETE
- GET /search: 200 with date range filter
- GET /query: 200 with QueryType variations
- POST /batch-details: 200 | 400 > 50 IDs
- GET /{id}/permissions: Draft=can edit, Completed=read-only
- GET /by-status/{status}: 200 filtered
- PUT /{id}/close: status → Completed
- PUT /{id}/suspend: status → Suspended
- PUT /{id}/cancel: status → Cancelled, registration → Waiting
- PUT /{id}/prescription-flag: flag toggled
- PUT /{id}/status: status changed
- PUT /{id}/print-completed: PrintCount +1
- PUT /{id} (Save): 200
- POST /batch-delete: 200 | 400 empty
- GET /pending: 200 list
- GET /{id}/audit-logs: 200 (empty in local mode)
- POST /{id}/print-logs: 200 (no-op in local mode)

### 08-Registrations (20 tests)
- CRUD: GET list (date filter), GET detail, POST create, PUT update, DELETE
- GET /queue: only Waiting status, ordered by CreatedAt
- PUT /{id}/start-visit: Waiting → InProgress
- PUT /{id}/cancel: → Cancelled
- POST /quick-visit: creates both Registration + MedicalCase

### 09-Diagnostics (6 tests)
- GET /db-info: 200 + provider
- GET /version: 200 + assemblyVersion
- GET /logs/recent: 200 + count param

### 10-Configuration (5 tests)
- PUT /configuration/{key}: 200 set value
- GET /configuration/{key}: 200 read value | 404 not found

## Assertion Strategy

| Type | Assertion |
|------|-----------|
| Status code | `pm.response.to.have.status(200/201/204)` |
| Response time | `pm.expect(pm.response.responseTime).to.be.below(5000)` |
| JSON structure | `pm.expect(jsonData).to.have.property('token')` |
| Business value | `pm.expect(jsonData.successCount).to.be.above(0)` |
| Environment save | `pm.environment.set('test_id', jsonData.id)` |

## Newman Execution

```powershell
# run-tests.ps1
newman run local-api-tests.postman_collection.json `
  -e local-api.environment.json `
  --reporters cli,htmlextra `
  --reporter-htmlextra-export results/report.html `
  --timeout-request 10000 `
  --delay-request 100
```

## Success Criteria

1. All 97 endpoints covered (100%)
2. ~310 tests pass
3. Newman exit code 0
4. HTML report generated
5. No test depends on external state (self-contained via setup)

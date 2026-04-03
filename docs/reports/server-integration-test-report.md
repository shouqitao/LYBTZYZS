# LYBT Server Integration Test Report

**Date**: 2026-04-03
**Branch**: `master` (commit `b8e44ad10`)
**Test Project**: `tests/LYBT.Tests.Server`
**Framework**: xUnit + FluentAssertions + WebApplicationFactory (real SQL Server + Respawn)

---

## Executive Summary

| Metric | Value |
|--------|-------|
| Total tests executed | 496 |
| Passed | 489 (98.6%) |
| Failed (pre-existing) | 5 (1.0%) |
| Skipped (pre-existing) | 2 (0.4%) |
| New tests added (this session) | 24 (all passing) |
| PRD requirements with test coverage | 92 / 125 (73.6%) |
| Uncovered requirements | 33 (26.4%) |

---

## Bug Fix: SystemLogs Migration Conflict

**Problem**: 315 of 496 tests failed with `SqlException: 数据库中已存在名为 'SystemLogs' 的对象`.

**Root cause**: `Program.cs` unconditionally called `AddMSSqlServerSinkWithColumnOptions()` with `AutoCreateSqlTable = true`, creating the `SystemLogs` table via Serilog before EF migrations ran. The `InitialCreateV2` migration then attempted `CREATE TABLE SystemLogs` — conflict.

**Fix**: Wrapped `AddMSSqlServerSinkWithColumnOptions()` in `if (!context.HostingEnvironment.IsEnvironment("Test"))` guard in `Program.cs` (line 96).

**Result**: All 315 previously failing tests now pass. 489/496 pass rate achieved.

---

## New Test Files Added

| File | Tests | Module | Priority |
|------|-------|--------|----------|
| `Features/US_Health_MustHaveTests.cs` | 6 | Health Check | MustHave |
| `Features/US_Diagnostics_MustHaveTests.cs` | 10 | Diagnostics | MustHave |
| `Features/US_Sync_MustHaveTests.cs` | +8 new | Sync | MustHave |
| **Total** | **24** | | |

All 24 new tests pass.

---

## Test File Inventory

### MustHave Tests (145 tests in 11 files)

| File | Tests | Lines |
|------|-------|-------|
| `US_Auth_MustHaveTests.cs` | 20 | 301 |
| `US_Config_MustHaveTests.cs` | 7 | 103 |
| `US_Diagnostics_MustHaveTests.cs` | 10 | 160 |
| `US_Formula_MustHaveTests.cs` | 11 | 286 |
| `US_Health_MustHaveTests.cs` | 6 | 96 |
| `US_Herb_MustHaveTests.cs` | 13 | 248 |
| `US_MedicalCase_MustHaveTests.cs` | 24 | 580 |
| `US_Patient_MustHaveTests.cs` | 12 | 240 |
| `US_Registration_MustHaveTests.cs` | 20 | 503 |
| `US_Sync_MustHaveTests.cs` | 11 | 198 |
| `US_User_MustHaveTests.cs` | 11 | 208 |
| **Subtotal** | **145** | **2,923** |

### ShouldHave Tests (82 tests in 11 files, under `_Deferred/`)

| File | Tests | Lines |
|------|-------|-------|
| `US_Auth_ShouldHaveTests.cs` | 6 | 124 |
| `US_Config_ShouldHaveTests.cs` | 3 | 68 |
| `US_ErrorHandling_ShouldHaveTests.cs` | 11 | 206 |
| `US_Formula_ShouldHaveTests.cs` | 7 | 139 |
| `US_Herb_ShouldHaveTests.cs` | 8 | 153 |
| `US_Logging_ShouldHaveTests.cs` | 4 | 95 |
| `US_MedicalCase_ShouldHaveTests.cs` | 14 | 308 |
| `US_Patient_ShouldHaveTests.cs` | 4 | 110 |
| `US_Registration_ShouldHaveTests.cs` | 4 | 120 |
| `US_Sync_ShouldHaveTests.cs` | 12 | 241 |
| `US_User_ShouldHaveTests.cs` | 9 | 171 |
| **Subtotal** | **82** | **1,735** |

### Features Total: 227 tests in 22 files

> **Note**: `dotnet test` reports 496 total. The remaining ~269 tests reside in infrastructure/helper classes outside `Features/` (e.g., `ServerFixture` integration, repository tests, service tests).

---

## PRD Requirements Traceability Matrix

### By Module

| PRD Module | Requirements | Covered | Coverage | Missing |
|------------|-------------|---------|----------|---------|
| Auth (`auth.md`) | 13 | 12 | 92.3% | AUTH-012 |
| Configuration (`configuration.md`) | 4 | 4 | 100.0% | — |
| Error Handling (`error-handling.md`) | 8 | 5 | 62.5% | ERR-003, ERR-007, ERR-008 |
| Formula (`formulas.md`) | 13 | 10 | 76.9% | FORM-007, FORM-011, FORM-013 |
| Health/Diagnostics (`health-diagnostics.md`) | 9 | 2* | 22.2%* | 7 of SYS-001 to SYS-009* |
| Herbs (`herbs.md`) | 13 | 9 | 69.2% | HERB-007, HERB-010, HERB-012, HERB-013 |
| Logging (`logging.md`) | 7 | 3 | 42.9% | LOG-003, LOG-004, LOG-005, LOG-006 |
| MedicalCase (`medical-cases.md`) | 18 | 16 | 88.9% | MC-012, MC-016 |
| Patients (`patients.md`) | 13 | 6 | 46.2% | PAT-006–PAT-012 |
| Registration (`registration.md`) | 7 | 7 | 100.0% | — |
| Sync (`sync.md`) | 8 | 8** | 100.0%** | — |
| Users (`users.md`) | 12 | 10 | 83.3% | USER-006, USER-007 |
| **Total** | **125** | **92** | **73.6%** | **33** |

\* Health/Diagnostics tests use custom IDs (US-HEALTH-001, US-DIAG-001) rather than PRD's US-SYS-XXX format. 16 test methods exist but only 2 unique PRD-level IDs are mapped; 7 SYS requirements have no mapped test. Actual SYS coverage needs alignment.

\*\* Sync tests cover all 8 PRD requirements (SYNC-001 to SYNC-007 via ShouldHave, SYNC-008 via MustHave). 2 additional MustHave tests (SYNC-009, SYNC-010) go beyond PRD scope and are not counted in PRD coverage.

### Covered Requirements Detail

#### Auth (12/13)

| Requirement | Test Class | Status |
|-------------|-----------|--------|
| US-AUTH-001 | MustHave | ✅ Pass |
| US-AUTH-002 | MustHave | ✅ Pass |
| US-AUTH-003 | MustHave | ✅ Pass |
| US-AUTH-004 | ShouldHave | ✅ Pass |
| US-AUTH-005 | MustHave | ✅ Pass |
| US-AUTH-006 | ShouldHave | ✅ Pass |
| US-AUTH-007 | MustHave | ✅ Pass |
| US-AUTH-008 | MustHave | ✅ Pass |
| US-AUTH-009 | MustHave | ✅ Pass |
| US-AUTH-010 | MustHave | ✅ Pass |
| US-AUTH-011 | ShouldHave | ✅ Pass |
| US-AUTH-013 | ShouldHave | ✅ Pass |
| **US-AUTH-012** | — | ❌ No test |

#### Configuration (4/4) ✅

| Requirement | Test Class | Status |
|-------------|-----------|--------|
| US-CFG-001 | MustHave | ⚠️ Fail (pre-existing) |
| US-CFG-002 | MustHave | ✅ Pass |
| US-CFG-003 | ShouldHave | ✅ Pass |
| US-CFG-004 | ShouldHave | ✅ Pass |

#### Error Handling (5/8)

| Requirement | Test Class | Status |
|-------------|-----------|--------|
| US-ERR-001 | ShouldHave | ✅ Pass |
| US-ERR-002 | ShouldHave | ✅ Pass |
| US-ERR-004 | ShouldHave | ✅ Pass |
| US-ERR-005 | ShouldHave | ✅ Pass |
| US-ERR-006 | ShouldHave | ✅ Pass |
| **US-ERR-003** | — | ❌ No test |
| **US-ERR-007** | — | ❌ No test |
| **US-ERR-008** | — | ❌ No test |

#### Formula (10/13)

| Requirement | Test Class | Status |
|-------------|-----------|--------|
| US-FORM-001 to US-FORM-006 | MustHave | ✅ Pass |
| US-FORM-008 | ShouldHave | ✅ Pass |
| US-FORM-009 | ShouldHave | ✅ Pass |
| US-FORM-010 | ShouldHave | ✅ Pass |
| US-FORM-012 | ShouldHave | ⚠️ Fail (pre-existing) |
| **US-FORM-007** | — | ❌ No test |
| **US-FORM-011** | — | ❌ No test |
| **US-FORM-013** | — | ❌ No test |

#### Herbs (9/13)

| Requirement | Test Class | Status |
|-------------|-----------|--------|
| US-HERB-001 to US-HERB-005 | MustHave | ✅ Pass |
| US-HERB-006 | ShouldHave | ✅ Pass |
| US-HERB-008 | ShouldHave | ✅ Pass |
| US-HERB-009 | ShouldHave | ✅ Pass |
| US-HERB-011 | ShouldHave | ⚠️ Fail (pre-existing) |
| **US-HERB-007** | — | ❌ No test |
| **US-HERB-010** | — | ❌ No test |
| **US-HERB-012** | — | ❌ No test |
| **US-HERB-013** | — | ❌ No test |

#### MedicalCase (16/18)

| Requirement | Test Class | Status |
|-------------|-----------|--------|
| US-MC-001 | MustHave | ⚠️ Fail (pre-existing) |
| US-MC-002 to US-MC-007 | MustHave | ✅ Pass |
| US-MC-008 | ShouldHave | ✅ Pass |
| US-MC-009 | MustHave | ✅ Pass |
| US-MC-010 | ShouldHave | ✅ Pass |
| US-MC-011 | ShouldHave | ✅ Pass |
| US-MC-013 | MustHave | ✅ Pass |
| US-MC-014, US-MC-015 | ShouldHave | ✅ Pass |
| US-MC-017, US-MC-018 | ShouldHave | ✅ Pass |
| **US-MC-012** | — | ❌ No test |
| **US-MC-016** | — | ❌ No test |

#### Patients (6/13)

| Requirement | Test Class | Status |
|-------------|-----------|--------|
| US-PAT-001 to US-PAT-004 | MustHave | ✅ Pass |
| US-PAT-005 | ShouldHave | ✅ Pass |
| US-PAT-013 | ShouldHave | ✅ Pass |
| **US-PAT-006 to US-PAT-012** | — | ❌ No tests (7 requirements) |

#### Registration (7/7) ✅

| Requirement | Test Class | Status |
|-------------|-----------|--------|
| US-REG-001 to US-REG-006 | MustHave | ✅ Pass |
| US-REG-007 | ShouldHave | ✅ Pass |

#### Sync (8/8 + 2 extra) ✅

| Requirement | Test Class | Status |
|-------------|-----------|--------|
| US-SYNC-001 to US-SYNC-007 | ShouldHave | ✅ Pass |
| US-SYNC-008 | MustHave | ✅ Pass |
| US-SYNC-009 (extra) | MustHave | ✅ Pass |

#### Users (10/12)

| Requirement | Test Class | Status |
|-------------|-----------|--------|
| US-USER-001 to US-USER-005 | MustHave | ✅ Pass |
| US-USER-008 to US-USER-012 | ShouldHave | ✅ Pass |
| **US-USER-006** | — | ❌ No test |
| **US-USER-007** | — | ❌ No test |

#### Logging (3/7)

| Requirement | Test Class | Status |
|-------------|-----------|--------|
| US-LOG-001 | ShouldHave | ✅ Pass |
| US-LOG-002 | ShouldHave | ✅ Pass |
| US-LOG-007 | ShouldHave | ✅ Pass |
| **US-LOG-003 to US-LOG-006** | — | ❌ No tests (4 requirements) |

#### Health/Diagnostics (2 custom IDs, 9 PRD SYS requirements)

| Test ID | Test Class | PRD Mapping | Status |
|---------|-----------|-------------|--------|
| US-HEALTH-001 | MustHave | Partially maps to SYS requirements | ✅ Pass (6 tests) |
| US-DIAG-001 | MustHave | Partially maps to SYS requirements | ✅ Pass (10 tests) |

> **Gap**: 16 Health/Diagnostics test methods exist but use non-standard IDs. PRD alignment needed.

---

## Pre-existing Failures Analysis

### 1. US-FORM-012: Export — `BadRequest` instead of `OK`

```
Expected: HttpStatusCode.OK (200)
Actual:   HttpStatusCode.BadRequest (400)
File:     _Deferred/US_Formula_ShouldHaveTests.cs:159
```

**Classification**: Test/API bug — Export endpoint likely requires specific query parameters (format, columns) that the test does not supply. The 400 response suggests model validation failure rather than authorization.

**Suggested fix**: Inspect the Export endpoint's required parameters and update the test request.

---

### 2. US-HERB-011: Export — `BadRequest` instead of `OK`

```
Expected: HttpStatusCode.OK (200)
Actual:   HttpStatusCode.BadRequest (400)
File:     _Deferred/US_Herb_ShouldHaveTests.cs:178
```

**Classification**: Same pattern as FORM-012. Export endpoint requires parameters the test omits.

**Suggested fix**: Same as FORM-012 — inspect Export action parameters.

---

### 3. US-MC-001: Admin Cannot Create — `OK` instead of `Forbidden`

```
Expected: HttpStatusCode.Forbidden (403)
Actual:   HttpStatusCode.OK (200)
File:     US_MedicalCase_MustHaveTests.cs:138
```

**Classification**: Authorization gap — Admin role is not restricted from creating medical cases. Either:
- The authorization policy is not implemented in the controller/service
- The test assumption is wrong (admin SHOULD be able to create cases)

**Suggested fix**: Verify business rules. If admin should be restricted, add `[Authorize(Roles = "...")]` policy. If not, update test expectation.

---

### 4. US-CFG-001: Health Check — Missing `status` property

```
Expected: json.RootElement.TryGetProperty("status", ...) == true
Actual:   false
File:     US_Config_MustHaveTests.cs:35
```

**Classification**: Response format mismatch — Health endpoint returns a different JSON shape than expected. The ASP.NET Core health check middleware may use `status` at a different level or wrapped in an envelope.

**Suggested fix**: Call the health endpoint manually, inspect actual response JSON, update test assertions.

---

### 5. US-CFG-001: Health Details — Missing `database` property

```
Expected: json.RootElement.TryGetProperty("database", ...) == true
Actual:   false
File:     US_Config_MustHaveTests.cs:66
```

**Classification**: Same root cause as #4 — health check response format doesn't match test expectations.

**Suggested fix**: Align with actual ASP.NET Core health check response format.

---

## Pre-existing Skipped Tests

| Test | Reason |
|------|--------|
| `CreateCase_WithRegistrationId_LinksRegistration` | Requires registration linkage setup not yet implemented in test fixture |
| `I2_CompletedCase_NextDay_IsLocked` | Time-dependent test requiring `TimeProvider` mocking |

---

## Coverage Gap Analysis

### High-Priority Gaps (modules with <60% coverage)

| Module | Coverage | Missing Requirements | Impact |
|--------|----------|---------------------|--------|
| Health/Diagnostics | 22.2% | SYS-002 to SYS-009 | System monitoring critical |
| Logging | 42.9% | LOG-003 to LOG-006 | Audit trail important |
| Patients | 46.2% | PAT-006 to PAT-012 (7 reqs) | Core clinical module |
| Error Handling | 62.5% | ERR-003, ERR-007, ERR-008 | API robustness |

### Recommendations

1. **Patient tests (P0)**: 7 uncovered requirements is the largest single gap. PAT-006 to PAT-012 likely cover search, pagination, update, and delete scenarios.

2. **Health/Diagnostics ID alignment (P1)**: 16 test methods exist but use custom IDs (HEALTH-001, DIAG-001) instead of PRD's US-SYS-XXX. Remap test names to PRD IDs for traceability.

3. **Logging tests (P2)**: LOG-003 to LOG-006 cover structured logging, correlation IDs, and log levels — important for production debugging.

4. **Fix pre-existing failures (P2)**: 5 failures indicate either test bugs or unimplemented features. Export (FORM-012, HERB-011) is likely a test fix. MC-001 and CFG-001 require investigation.

5. **Enable skipped tests (P3)**: `TimeProvider` injection for `I2_CompletedCase_NextDay_IsLocked` and registration linkage for `CreateCase_WithRegistrationId_LinksRegistration`.

---

## Infrastructure

- **Test runner**: `dotnet test tests/LYBT.Tests.Server/`
- **Database**: Real SQL Server with Respawn cleanup between tests
- **Auth**: JWT token generation via `ServerFixture.GetAuthenticatedClientAsync()`
- **Test fixture**: `ServerFixture` (WebApplicationFactory) — manages database lifecycle, migration, and HTTP client configuration
- **Parallelization**: xUnit default (no explicit control)
- **Average test duration**: ~1 min 56 sec for 496 tests

---

## Appendix: PRD Modules Without Server Tests

The following PRD modules are desktop-only or infrastructure concerns (no server-side API to test):

- `desktop-shell.md` — WPF shell, navigation
- `card-reader.md` — Hardware integration
- `printing.md` — Report printing
- `ui-patterns.md` — Frontend patterns
- `nfr.md` — Non-functional requirements (performance, security)
- `roadmap.md` — Release planning

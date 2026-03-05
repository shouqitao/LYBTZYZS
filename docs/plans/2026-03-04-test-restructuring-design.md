# Test Restructuring Design: User Journey Driven Testing

## Date: 2026-03-04
## Status: Approved
## Author: Brainstorm session

---

## Problem Statement

Current test suite (2021 tests) gives false confidence:
- Desktop tests have 41 tests that never execute due to vstest session timeout, yet report "all pass"
- ~550 tests are low-value (mock-heavy ViewModel tests, trivial property getter/setter tests)
- Tests verify "code works as coded" but NOT "software works as user expects"
- Real example: All tests green, but basic login doesn't work in running application

## Goal

Tests must reflect the real state of the software. If tests are green, the software must actually work.

## Design Principle

> Test the software the way users use it. If users can't do it, tests should fail.

---

## New Test Architecture

### Hierarchy (Priority Order)

```
1. UserJourneys    — "Can the software be used?"      (highest priority)
2. Features        — "Does each feature work correctly?" (per-feature boundary tests)
3. Infrastructure  — "Does the framework behave?"      (config, logging, exceptions)
4. Architecture    — "Is the code structure sound?"     (layer rules, anti-patterns)
```

If UserJourneys fail, everything else is irrelevant.

### Directory Structure

```
tests/
├── LYBT.Tests.Server/
│   ├── UserJourneys/                    ★ NEW
│   │   ├── AuthJourneyTests.cs          (~8 steps)
│   │   ├── AdminSetupJourneyTests.cs    (~10 steps)
│   │   ├── DoctorClinicalJourneyTests.cs (~12 steps)
│   │   ├── MedicalCaseEditJourneyTests.cs (~6 steps)
│   │   ├── PatientManagementJourneyTests.cs (~6 steps)
│   │   └── BatchOperationsJourneyTests.cs (~5 steps)
│   │
│   ├── Features/                        ★ REORGANIZED (from PureLogic/ + scattered)
│   │   ├── Auth/
│   │   │   ├── LoginTests.cs            (boundary: wrong password, lockout, rate limit)
│   │   │   ├── TokenTests.cs            (boundary: expiry, refresh, revocation)
│   │   │   └── PermissionTests.cs       (boundary: role-based access)
│   │   ├── Patients/
│   │   │   ├── CrudTests.cs             (full CRUD via HTTP)
│   │   │   ├── ValidationTests.cs       (required fields, duplicates)
│   │   │   └── ImportExportTests.cs     (Excel import/export)
│   │   ├── Herbs/
│   │   │   ├── CrudTests.cs
│   │   │   ├── ValidationTests.cs
│   │   │   └── BatchTests.cs
│   │   ├── Formulas/
│   │   │   ├── CrudTests.cs
│   │   │   ├── ValidationTests.cs
│   │   │   └── OwnershipTests.cs
│   │   ├── MedicalCases/
│   │   │   ├── LifecycleTests.cs        (create -> diagnose -> prescribe -> complete -> lock)
│   │   │   ├── PrescriptionTests.cs     (formula import, history copy, manual)
│   │   │   ├── PermissionTests.cs       (cross-doctor, admin audit)
│   │   │   └── ValidationTests.cs       (completion conditions, edit reasons)
│   │   ├── Users/
│   │   │   ├── CrudTests.cs
│   │   │   └── ValidationTests.cs
│   │   └── Sync/
│   │       ├── ProtocolTests.cs
│   │       └── ConflictTests.cs
│   │
│   ├── Infrastructure/                  KEEP (trimmed)
│   │   ├── ExceptionHandling/
│   │   ├── Logging/
│   │   └── Configuration/
│   │
│   └── _Infrastructure/                 KEEP (ServerFixture + Respawn)
│
├── LYBT.Tests.Desktop/                  MAJOR TRIM
│   ├── EndToEnd/                        KEEP (real SQLite E2E)
│   ├── LocalData/                       KEEP (real SQLite repository tests)
│   ├── PureLogic/                       TRIM (keep only real business logic, delete mock-heavy)
│   └── _Infrastructure/                 KEEP (DesktopFixture)
│
└── LYBT.Tests.Architecture/             KEEP (structural guardrails)
```

---

## UserJourney Test Design

### Technical Implementation

- Uses existing `ServerFixture` (real SQL Server + Respawn + real login)
- Inherits `IntegrationTestBase` (same as current integration tests)
- New dependency: `Xunit.Extensions.Ordering` (NuGet) for step ordering
- Each Journey = one test class with ordered [Fact] methods
- Steps share state via static fields within the class
- Database is Respawn-reset before each Journey (not each step)

### Journey Definitions

#### AuthJourney (~8 steps)
1. Login with valid credentials -> 200 + JWT
2. Validate token -> 200
3. Access protected endpoint -> 200
4. Refresh token -> new JWT
5. Use old token -> 401
6. Auto-login with token -> 200
7. Logout -> revoke refresh token
8. Use revoked refresh -> 401

#### AdminSetupJourney (~10 steps)
1. Admin login -> 200
2. Create doctor account -> 201
3. Doctor login with new account -> 200
4. Create herbs (batch) -> 201
5. Verify herbs queryable -> 200 + data
6. Create formula with herbs -> 201
7. Verify formula-herb relationships -> 200 + items
8. Create patient -> 201
9. Verify patient queryable -> 200
10. Admin views all users -> 200 + includes doctor

#### DoctorClinicalJourney (~12 steps)
1. Doctor login -> 200
2. Query patients -> 200 + list
3. Create medical case for patient -> 201
4. Verify case status = Active
5. Save diagnosis (TcmDiagnosis) -> 200
6. Set NeedsPrescription = true -> 200
7. Import formula into prescription -> 200
8. Verify prescription items created
9. Complete medical case -> 200 (validates BR-003)
10. Verify case status = Completed
11. Try edit completed case -> requires EditReason
12. Verify audit log recorded

#### MedicalCaseEditJourney (~6 steps)
1. Setup: create completed case with prescription
2. Edit completed case (same day, no reason) -> 200
3. Print prescription -> PrintCount=1, IsPrinted=true
4. Edit after print -> requires EditReason, resets IsPrinted
5. Close case -> locked
6. Admin can still view audit trail

#### PatientManagementJourney (~6 steps)
1. Create patient -> 201
2. Update patient -> 200
3. Toggle status (disable) -> 200
4. Check reference (no medical cases) -> can delete
5. Create medical case -> reference exists
6. Check reference -> cannot delete (has cases)

#### BatchOperationsJourney (~5 steps)
1. Batch import herbs (JSON) -> 200
2. Batch import formulas (JSON) -> 200
3. Batch check herb references -> some in use
4. Batch delete unused herbs -> 200
5. Batch delete in-use herbs -> rejected

---

## Deletion Plan

### Desktop Tests to Delete (~350 tests)

All mock-heavy ViewModel and Service tests:
- ViewModels/Patients/PatientServiceTests.cs (18)
- ViewModels/MedicalCase/MedicalCaseFormViewModel_SimpleTests.cs (14)
- ViewModels/Shell/LoginCoordinatorTests.cs (16)
- ViewModels/Auth/LoginViewModelTests.cs (27)
- ViewModels/Users/UserServiceTests.cs (22)
- ViewModels/Herbs/HerbItemViewModelBaseTests.cs (18)
- ViewModels/Herbs/HerbFormulaItemViewModelTests.cs (15)
- ViewModels/Formula/FormulaHerbItemViewModelTests.cs (15)
- ViewModels/Formula/FormulaEditRegressionTests.cs (9)
- ViewModels/MedicalCase/PrescriptionEditFlowTests.cs (7)
- ViewModels/MedicalCase/PrescriptionHerbItemPriceTests.cs (16)
- ViewModels/Shell/Session/SessionLifecycleManagerTests.cs (18)
- PureLogic/Foundation/Security/AuthenticationServiceTests.cs (18)
- PureLogic/Foundation/Security/TokenManagerTests.cs (12)
- PureLogic/Infrastructure/Models/State/*.cs (~18)
- PureLogic/Infrastructure/Services/SearchServiceTests.cs (11)
- Other mock-heavy files following same pattern

### Server Tests to Delete (~200 tests)

Trivial entity property getter/setter tests:
- PureLogic/Entities/*/property-only test methods
- Tests where: `entity.X = value; assert entity.X == value`

Criterion: If the code under test is deliberately broken, would this test turn red? If not, delete.

---

## Features Reorganization

Existing Server integration tests are NOT rewritten. They are moved into Features/ directory:
- `Auth/AuthIntegrationTests.cs` -> `Features/Auth/LoginTests.cs`
- `Patients/PatientIntegrationTests.cs` -> `Features/Patients/CrudTests.cs`
- `PureLogic/Validators/AuthValidatorTests.cs` -> `Features/Auth/ValidationTests.cs`
- etc.

Logic unchanged. Only directory structure and namespaces change.

---

## Execution Plan

| Phase | Content | Depends On |
|-------|---------|------------|
| P1: UserJourney | New 6 Journey test classes (~47 tests) | Independent |
| P2: Delete low-value | Remove ~550 tests | P1 (safety net) |
| P3: Features reorg | Move Server tests to Features/ | P2 (stable base) |
| P4: Fix + verify | Desktop timeout, full run, docs | P3 |

---

## Out of Scope (YAGNI)

- BDD/Gherkin (Reqnroll) -- team size doesn't justify
- Requirements Traceability Matrix -- Journey structure provides implicit tracing
- Code coverage gates -- coverage != quality, Journey pass is the real standard
- Desktop-Server joint tests -- requires full WPF Runtime, cost too high
- Stryker mutation testing -- optional future enhancement after restructuring

---

## Success Criteria

- [ ] All 6 UserJourneys pass (software actually works)
- [ ] Zero timeout-aborted tests (100% of registered tests execute)
- [ ] Server tests: zero NSubstitute (AntiMockRuleTests enforced)
- [ ] Test count reduced from ~2021 to ~1400 with higher signal density
- [ ] Every test failure maps to a real user-visible problem

---

## References

- Kent C. Dodds: Testing Trophy ("Write tests. Not too many. Mostly integration.")
- Jimmy Bogard: Vertical Slice Testing, Respawn
- Xunit.Extensions.Ordering for sequential test execution
- Strangler Pattern for incremental test suite migration

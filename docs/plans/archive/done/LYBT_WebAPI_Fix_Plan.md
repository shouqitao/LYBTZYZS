# LYBT WebAPI Comprehensive Fix Plan

## Executive Summary

This plan addresses three critical objectives for the LYBT WebAPI project:
1. **100% Functional Completeness** - Verify all 102 API endpoints and edge cases
2. **Architecture Compliance** - Fix 8 architectural violations (direct DbContext dependencies)
3. **100% Test Pass Rate** - Fix Newman test failures (currently 60.1%)

**Estimated Duration:** 8-12 days  
**Team Configuration:** 2-3 developers  
**Testing Strategy:** TDD - Write tests first, then implement

---

## Phase 1: Foundation - Architecture Violation Fixes

### Task 1.1: Create HealthCheckService for HealthController
**Priority:** High  
**Category:** Architecture  
**Skill:** dotnet-testing, dotnet-testing-strategy  
**Estimated Time:** 4 hours

**Problem:** `HealthController` directly depends on `AppDbContext` (violates 3-layer architecture)

**Solution:**
- Create `IHealthCheckService` interface
- Create `HealthCheckService` implementation
- Refactor `HealthController` to use the service

**Files to Modify:**
```
NEW: src/Server/Modules/LYBT.Module.Common/Interfaces/IHealthCheckService.cs
NEW: src/Server/Modules/LYBT.Module.Common/Services/HealthCheckService.cs
MODIFY: src/Server/Services/LYBT.WebAPI/Controllers/HealthController.cs
MODIFY: src/Server/Modules/LYBT.Module.Common/ModuleExtensions.cs (register service)
```

**Code Changes:**
```csharp
// IHealthCheckService.cs
public interface IHealthCheckService
{
    Task<HealthCheckResult> CheckDatabaseAsync();
    Task<HealthStatus> GetOverallStatusAsync();
}

// HealthCheckService.cs
public class HealthCheckService : IHealthCheckService
{
    private readonly AppDbContext _dbContext;  // Now in service layer, acceptable
    
    public async Task<HealthCheckResult> CheckDatabaseAsync()
    {
        // Move CheckDatabase() logic from controller
    }
}

// HealthController.cs - After refactoring
public class HealthController : BaseApiController
{
    private readonly IHealthCheckService _healthCheckService;
    
    public HealthController(IHealthCheckService healthCheckService, ILogger<HealthController> logger)
        : base(logger)
    {
        _healthCheckService = healthCheckService;
    }
}
```

**Verification Criteria:**
- [ ] `HealthController` no longer has `_dbContext` field
- [ ] All health check endpoints return same response format
- [ ] Integration tests pass: `GET /api/v1/health`, `GET /api/v1/health/details`
- [ ] Newman test for health endpoints passes

**Atomic Commit:** `git commit -m "fix(architecture): refactor HealthController to use IHealthCheckService - ARCH-001"`

---

### Task 1.2: Create CrossModule Query Service for PatientService
**Priority:** High  
**Category:** Architecture  
**Skill:** dotnet-testing  
**Estimated Time:** 6 hours

**Problem:** `PatientService` directly queries `AppDbContext.MedicalCases` for reference checks (lines 436, 523, 582, 586)

**Solution:**
- Create `IMedicalCaseQueryService` for cross-module queries
- Move direct DbContext queries to the service
- Inject service into PatientService

**Files to Modify:**
```
NEW: src/Server/Modules/LYBT.Module.MedicalCase/Interfaces/IMedicalCaseQueryService.cs
NEW: src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseQueryService.cs
MODIFY: src/Server/Modules/LYBT.Module.Patients/Services/PatientService.cs
MODIFY: src/Server/Modules/LYBT.Module.MedicalCase/ModuleExtensions.cs
```

**Code Changes:**
```csharp
// IMedicalCaseQueryService.cs
public interface IMedicalCaseQueryService
{
    Task<int> CountActiveMedicalCasesAsync(Guid patientId);
    Task<int> CountUnfinishedMedicalCasesAsync(Guid patientId);
    Task<List<MedicalCaseReferenceDto>> GetRecentMedicalCasesAsync(Guid patientId, int count);
}

// PatientService.cs - After refactoring
public class PatientService : BaseService<Patient>
{
    private readonly IMedicalCaseQueryService _medicalCaseQueryService;
    // Remove: private readonly AppDbContext _dbContext;
}
```

**Verification Criteria:**
- [ ] `PatientService` no longer directly references `AppDbContext.MedicalCases`
- [ ] All reference check endpoints work correctly
- [ ] Unit tests for reference checking pass
- [ ] Newman tests for patient delete with references pass

**Atomic Commit:** `git commit -m "fix(architecture): extract MedicalCase queries to IMedicalCaseQueryService - ARCH-002"`

---

### Task 1.3: Create CrossModule Query Service for HerbService
**Priority:** High  
**Category:** Architecture  
**Skill:** dotnet-testing  
**Estimated Time:** 4 hours

**Problem:** `HerbService` directly queries `AppDbContext` for prescription references

**Solution:**
- Create `IHerbCrossModuleService` interface
- Implement service for herb reference queries
- Refactor HerbService to use the service

**Files to Modify:**
```
NEW: src/Server/Modules/LYBT.Module.Herbs/Interfaces/IHerbCrossModuleService.cs
NEW: src/Server/Modules/LYBT.Module.Herbs/Services/HerbCrossModuleService.cs
MODIFY: src/Server/Modules/LYBT.Module.Herbs/Services/HerbService.cs
MODIFY: src/Server/Modules/LYBT.Module.Herbs/ModuleExtensions.cs
```

**Verification Criteria:**
- [ ] HerbService no longer has direct DbContext queries for prescriptions
- [ ] Herb reference check functionality preserved
- [ ] Newman tests for herb endpoints pass

**Atomic Commit:** `git commit -m "fix(architecture): extract herb prescription queries to IHerbCrossModuleService - ARCH-003"`

---

### Task 1.4: Create Token Repository for Auth Services
**Priority:** Medium  
**Category:** Architecture  
**Skill:** dotnet-testing  
**Estimated Time:** 8 hours

**Problem:** `AuthService`, `SecurityAuditService`, and `TokenRevocationService` directly use `AppDbContext` for token operations

**Solution:**
- Create `ITokenRepository` interface
- Implement `TokenRepository` for all token-related DB operations
- Refactor Auth services to use repository

**Files to Modify:**
```
NEW: src/Server/Modules/LYBT.Module.Auth/Interfaces/ITokenRepository.cs
NEW: src/Server/Modules/LYBT.Module.Auth/Repositories/TokenRepository.cs
MODIFY: src/Server/Modules/LYBT.Module.Auth/Services/AuthService.cs
MODIFY: src/Server/Modules/LYBT.Module.Auth/Services/SecurityAuditService.cs
MODIFY: src/Server/Modules/LYBT.Module.Auth/Services/TokenRevocationService.cs
MODIFY: src/Server/Modules/LYBT.Module.Auth/ModuleExtensions.cs
```

**Verification Criteria:**
- [ ] Auth services no longer depend directly on AppDbContext
- [ ] Token operations work correctly (login, refresh, logout)
- [ ] Security audit logging functions properly
- [ ] Newman auth tests pass (login, refresh, logout, validate)

**Atomic Commit:** `git commit -m "fix(architecture): create TokenRepository for auth services - ARCH-004"`

---

### Task 1.5: Refactor MedicalCaseAuditService
**Priority:** Medium  
**Category:** Architecture  
**Skill:** dotnet-testing  
**Estimated Time:** 3 hours

**Problem:** `MedicalCaseAuditService` directly uses `AppDbContext` for audit log operations

**Solution:**
- Create `IMedicalCaseAuditRepository` 
- Move audit log persistence to repository
- Refactor service to use repository

**Files to Modify:**
```
NEW: src/Server/Modules/LYBT.Module.MedicalCase/Interfaces/IMedicalCaseAuditRepository.cs
NEW: src/Server/Modules/LYBT.Module.MedicalCase/Repositories/MedicalCaseAuditRepository.cs
MODIFY: src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseAuditService.cs
MODIFY: src/Server/Modules/LYBT.Module.MedicalCase/ModuleExtensions.cs
```

**Verification Criteria:**
- [ ] Audit service uses repository pattern
- [ ] Audit logs are written correctly
- [ ] Newman audit endpoint tests pass

**Atomic Commit:** `git commit -m "fix(architecture): add MedicalCaseAuditRepository - ARCH-005"`

---

### Task 1.6: Review SyncService Implementation
**Priority:** Low  
**Category:** Architecture  
**Skill:** dotnet-testing  
**Estimated Time:** 2 hours

**Note:** Exploration showed `SyncController` uses `ISyncService` correctly. This task verifies no hidden violations exist.

**Files to Review:**
```
src/Server/Modules/LYBT.Module.Sync/Services/SyncService.cs
```

**Verification Criteria:**
- [ ] SyncService only uses repositories, not direct DbContext
- [ ] Or document acceptable DbContext usage pattern for bulk operations

**Atomic Commit:** (Document-only or no commit if no changes needed)

---

## Phase 2: Functional Completeness Audit

### Task 2.1: Controller Endpoint Gap Analysis
**Priority:** High  
**Category:** Functional  
**Skill:** dotnet-testing-strategy  
**Estimated Time:** 6 hours  
**Dependencies:** Phase 1 complete

**Objective:** Compare implemented endpoints against Postman collection (102 endpoints)

**Methodology:**
1. Parse Postman collection to extract all endpoint paths
2. Extract all controller routes using reflection or regex
3. Create mapping table: Postman endpoint ↔ Controller action
4. Identify missing implementations

**Expected Gaps to Check:**
- Batch operations consistency across modules
- Import/Export template endpoints
- Restore endpoints for soft-deleted entities
- Toggle status endpoints
- Reference check endpoints

**Files to Analyze:**
```
docs/06-operations/LYBTZYZS_API_Collection.json
src/Server/Services/LYBT.WebAPI/Controllers/*.cs
```

**Deliverable:** 
- Gap analysis spreadsheet
- List of missing endpoints to implement

**Verification Criteria:**
- [ ] All 102 Postman endpoints mapped to controller actions
- [ ] Missing endpoints identified with priority
- [ ] Batch operation parity verified (all modules should have same batch operations)

---

### Task 2.2: Implement Missing Batch Operations
**Priority:** High  
**Category:** Functional  
**Skill:** dotnet-testing  
**Estimated Time:** 8 hours  
**Dependencies:** Task 2.1

**Expected Missing Operations (verify during Task 2.1):**
- Batch Enable/Disable for Patients
- Batch Restore for all modules
- Batch Import JSON for Patients

**Files to Modify (per module):**
```
src/Server/Services/LYBT.WebAPI/Controllers/{Module}Controller.cs
src/Server/Modules/LYBT.Module.{Module}/Services/{Module}Service.cs
src/Server/Modules/LYBT.Module.{Module}/Interfaces/I{Module}Service.cs
```

**Verification Criteria:**
- [ ] All batch operations return `BatchOperationResultDto`
- [ ] Item-level error isolation (one failure doesn't fail all)
- [ ] Newman batch operation tests pass

**Atomic Commit:** `git commit -m "feat(batch): implement missing batch operations - FUNC-001"`

---

### Task 2.3: Import/Export Format Standardization
**Priority:** Medium  
**Category:** Functional  
**Skill:** dotnet-testing  
**Estimated Time:** 6 hours

**Problem:** Import/export endpoints may have inconsistent formats

**Files to Review:**
```
src/Server/Modules/LYBT.Module.Patients/Services/PatientImportExportService.cs
src/Server/Modules/LYBT.Module.Herbs/Services/HerbImportExportService.cs
src/Server/Modules/LYBT.Module.Formula/Services/FormulaImportExportService.cs
```

**Standardization Requirements:**
- All imports accept same Excel format (headers, data types)
- All exports use consistent date formats
- All template endpoints return valid Excel files
- Error messages follow same format

**Verification Criteria:**
- [ ] Import template files can be opened in Excel
- [ ] Import data validation errors return consistent format
- [ ] Exported files contain all expected columns
- [ ] Newman import/export tests pass

---

### Task 2.4: Edge Case Business Logic Review
**Priority:** Medium  
**Category:** Functional  
**Skill:** dotnet-testing  
**Estimated Time:** 8 hours

**Edge Cases to Verify:**

1. **Same-day Edit Rule**
   - Non-admin can only edit entities created today
   - Admin can edit any entity
   - Test in: Patients, MedicalCases, Herbs, Formulas

2. **Soft Delete Recovery**
   - Restore endpoint finds deleted entities using `IgnoreQueryFilters()`
   - Restored entity preserves all data
   - Test in all modules with soft delete

3. **Reference Check Before Delete**
   - Cannot delete Patient with MedicalCases
   - Cannot delete Herb used in Prescriptions
   - Cannot delete Formula used in MedicalCases

4. **Concurrent Edit Handling**
   - RowVersion check prevents lost updates
   - Proper error message returned

**Files to Test:**
```
src/Server/Modules/*/Services/*Service.cs
```

**Verification Criteria:**
- [ ] All edge cases have unit tests
- [ ] Edge case tests pass
- [ ] Newman tests cover edge cases

---

## Phase 3: Response Format Standardization

### Task 3.1: Standardize ApiResponse Format
**Priority:** High  
**Category:** Testing  
**Skill:** dotnet-testing  
**Estimated Time:** 6 hours  
**Dependencies:** Phase 2

**Standard Response Format:**
```json
{
  "success": true,
  "message": "操作成功",
  "data": { ... },
  "errors": null,
  "timestamp": "2026-03-29T10:00:00Z",
  "requestId": "abc-123"
}
```

**Files to Review:**
```
src/Server/Services/LYBT.WebAPI/Controllers/*.cs
```

**Common Issues to Fix:**
- Missing `success` field
- `errors` field format inconsistency (array vs object)
- Missing `timestamp` or `requestId`
- Wrong HTTP status codes

**Verification Criteria:**
- [ ] All endpoints return consistent ApiResponse structure
- [ ] Success responses have `success: true`
- [ ] Error responses have `success: false` and populated `errors`
- [ ] Newman response format assertions pass

**Atomic Commit:** `git commit -m "fix(api): standardize ApiResponse format across all endpoints - TEST-001"`

---

### Task 3.2: Fix Validation Error Responses
**Priority:** High  
**Category:** Testing  
**Skill:** dotnet-testing  
**Estimated Time:** 4 hours

**Problem:** Validation errors may return different formats

**Expected Format:**
```json
{
  "success": false,
  "message": "验证失败",
  "data": null,
  "errors": [
    "患者姓名不能为空",
    "手机号格式不正确"
  ]
}
```

**Files to Review:**
```
src/Server/Core/LYBT.Infrastructure/Web/BaseApiController.cs
src/Server/Core/LYBT.Infrastructure/Filters/ValidationFilter.cs
```

**Verification Criteria:**
- [ ] FluentValidation errors return string array
- [ ] Manual validation returns consistent format
- [ ] ModelState errors are properly formatted
- [ ] Newman validation error tests pass

---

### Task 3.3: Fix PagedResult Structure
**Priority:** Medium  
**Category:** Testing  
**Skill:** dotnet-testing  
**Estimated Time:** 3 hours

**Expected PagedResult Format:**
```json
{
  "items": [...],
  "totalCount": 100,
  "currentPage": 1,
  "pageSize": 20,
  "totalPages": 5,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

**Files to Review:**
```
src/Server/Shared/LYBT.Shared/Models/Common/PagedResult.cs
```

**Verification Criteria:**
- [ ] All paginated endpoints return complete PagedResult
- [ ] Calculated fields (totalPages, hasNextPage) are correct
- [ ] Newman pagination tests pass

---

## Phase 4: Test Collection Fixes

### Task 4.1: Fix Variable Propagation in Postman Collection
**Priority:** High  
**Category:** Testing  
**Estimated Time:** 4 hours  
**Dependencies:** Phase 3

**Problem:** Variables like `testPatientId`, `testMedicalCaseId` not properly set/extracted

**Common Issues:**
- Missing test scripts to extract IDs from responses
- Variable names mismatch between set and get
- Environment vs Collection variable confusion

**Files to Modify:**
```
docs/06-operations/LYBTZYZS_API_Collection.json
```

**Fix Pattern:**
```javascript
// In test script of CREATE endpoints
pm.test("Store created entity ID", function () {
    const jsonData = pm.response.json();
    if (jsonData.success && jsonData.data) {
        pm.environment.set("testPatientId", jsonData.data.id);
    }
});

// In prerequest script of DELETE/GET endpoints
if (!pm.environment.get("testPatientId")) {
    pm.expect.fail("testPatientId not set. Run Setup first.");
}
```

**Verification Criteria:**
- [ ] All create endpoints extract and store IDs
- [ ] All dependent endpoints check for variable existence
- [ ] Newman run completes without variable errors

**Atomic Commit:** `git commit -m "fix(tests): fix variable propagation in Postman collection - TEST-002"`

---

### Task 4.2: Fix Assertion Mismatches
**Priority:** High  
**Category:** Testing  
**Estimated Time:** 6 hours

**Problem:** Tests expect specific status codes or response structures that don't match implementation

**Example Issues:**
- Test expects 201 but API returns 200
- Test expects field name `id` but API returns `Id`
- Test expects array but gets single object

**Methodology:**
1. Run Newman collection and capture all failures
2. Categorize failures by type
3. Fix either implementation or test to match

**Verification Criteria:**
- [ ] All status code assertions pass
- [ ] All response structure assertions pass
- [ ] All field name assertions pass

---

### Task 4.3: Fix Test Data Dependencies
**Priority:** Medium  
**Category:** Testing  
**Estimated Time:** 4 hours

**Problem:** Tests depend on specific test data that may not exist

**Solution:**
- Ensure Setup folder runs first
- Add guards to check prerequisites
- Make tests independent where possible

**Verification Criteria:**
- [ ] Tests can run independently
- [ ] Setup flow creates all required test data
- [ ] Cleanup flow removes test data

---

## Phase 5: Final Verification

### Task 5.1: Full Newman Test Run
**Priority:** Critical  
**Category:** Testing  
**Skill:** dotnet-testing  
**Estimated Time:** 2 hours  
**Dependencies:** All previous phases

**Command:**
```bash
newman run docs/06-operations/LYBTZYZS_API_Collection.json \
  --environment docs/06-operations/LYBTZYZS_Environment.json \
  --reporters cli,html,json \
  --reporter-json-export newman-results.json
```

**Success Criteria:**
- 100% pass rate (102/102 endpoints)
- No assertion failures
- Average response time < 500ms

**Verification Criteria:**
- [ ] Newman report shows 100% pass
- [ ] All critical paths tested
- [ ] No errors in console

---

### Task 5.2: Architecture Compliance Check
**Priority:** Critical  
**Category:** Architecture  
**Skill:** dotnet-testing  
**Estimated Time:** 2 hours

**Verification Method:**
```bash
# Search for direct DbContext usage in services/grep "private readonly AppDbContext" src/Server/Modules/*/Services/*.cs

# Should return empty after fixes
```

**Architecture Rules:**
- Controllers depend only on Services
- Services depend on Repositories (not DbContext)
- Repositories are the only layer accessing DbContext
- Cross-module queries use CrossModuleServices

**Verification Criteria:**
- [ ] No direct DbContext usage in Controllers
- [ ] No direct DbContext usage in Services (except via Repository)
- [ ] All cross-module queries use dedicated services
- [ ] Architecture tests pass

---

## Parallel Execution Graph

```
Phase 1: Architecture Fixes (Independent Tasks)
├── Task 1.1: HealthCheckService [4h]
├── Task 1.2: MedicalCaseQueryService [6h]
├── Task 1.3: HerbCrossModuleService [4h]
├── Task 1.4: TokenRepository [8h]
├── Task 1.5: AuditRepository [3h]
└── Task 1.6: SyncService Review [2h]

Phase 2: Functional Audit (After Phase 1)
├── Task 2.1: Endpoint Gap Analysis [6h]
├── Task 2.2: Batch Operations [8h]
├── Task 2.3: Import/Export [6h]
└── Task 2.4: Edge Cases [8h]

Phase 3: Response Format (After Phase 2)
├── Task 3.1: ApiResponse Standardization [6h]
├── Task 3.2: Validation Errors [4h]
└── Task 3.3: PagedResult [3h]

Phase 4: Test Fixes (After Phase 3)
├── Task 4.1: Variable Propagation [4h]
├── Task 4.2: Assertion Fixes [6h]
└── Task 4.3: Test Data [4h]

Phase 5: Verification (After all)
├── Task 5.1: Newman Run [2h]
└── Task 5.2: Architecture Check [2h]
```

**Parallel Execution Groups:**

**Wave 1** (Days 1-2, ~27 hours):
- Tasks 1.1, 1.2, 1.3, 1.4, 1.5, 1.6 (can all run in parallel with 2-3 developers)

**Wave 2** (Days 3-4, ~28 hours):
- Tasks 2.1, 2.2, 2.3, 2.4 (sequential dependencies within phase)

**Wave 3** (Day 5, ~13 hours):
- Tasks 3.1, 3.2, 3.3 (can run in parallel)

**Wave 4** (Day 6, ~14 hours):
- Tasks 4.1, 4.2, 4.3 (sequential)

**Wave 5** (Day 7, ~4 hours):
- Tasks 5.1, 5.2 (final verification)

---

## Test-Driven Development Approach

### For Each Task:

1. **Write/Update Test First**
   ```bash
   # Create or modify test
   dotnet test tests/LYBT.Tests.Server --filter "FullyQualifiedName~HealthController"
   # Expect: FAIL
   ```

2. **Run Test (Should Fail)**
   ```bash
   dotnet test tests/LYBT.Tests.Server
   ```

3. **Implement Fix**
   - Write minimal code to make test pass
   - Follow existing code patterns

4. **Run Test (Should Pass)**
   ```bash
   dotnet test tests/LYBT.Tests.Server
   # Expect: PASS
   ```

5. **Refactor (if needed)**
   - Clean up code
   - Ensure no regressions

6. **Commit**
   ```bash
   git add .
   git commit -m "type(scope): description - TICKET-001"
   ```

### Test Categories:

**Unit Tests:**
- Service logic
- Repository queries
- Cross-module service calls

**Integration Tests:**
- Controller endpoints
- Database operations
- API response formats

**Newman Tests:**
- Full API collection
- End-to-end flows
- Performance validation

---

## Atomic Commit Strategy

### Commit Message Format:
```
<type>(<scope>): <description> - <ticket-id>

<body>
- What changed
- Why it changed
- Any breaking changes

<footer>
Refs: ARCH-XXX or FUNC-XXX or TEST-XXX
```

### Examples:
```bash
# Architecture fix
git commit -m "fix(architecture): refactor HealthController to use IHealthCheckService - ARCH-001

- Created IHealthCheckService interface
- Implemented HealthCheckService with database check logic
- Refactored HealthController to inject service instead of DbContext
- Registered service in DI container

BREAKING CHANGE: None
Refs: ARCH-001"

# Feature implementation
git commit -m "feat(batch): implement batch enable/disable for patients - FUNC-002

- Added BatchEnableAsync and BatchDisableAsync methods
- Added controller endpoints POST /patients/batch-enable and batch-disable
- Returns BatchOperationResultDto with per-item status

Refs: FUNC-002"

# Test fix
git commit -m "fix(tests): update variable extraction in Postman collection - TEST-002

- Fixed testPatientId extraction from Create Patient response
- Added validation to ensure variable exists before dependent tests
- Updated error messages for clarity

Refs: TEST-002"
```

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Breaking changes to API contract | Medium | High | Maintain backward compatibility, use versioning |
| Test data dependencies | High | Medium | Create robust setup/cleanup flows |
| Performance regression | Low | Medium | Benchmark before/after changes |
| Cross-module coupling | Medium | High | Use interfaces, proper DI |
| Missing edge cases | Medium | High | Comprehensive test coverage |

---

## Success Metrics

### Objective 1: 100% Functional Completeness
- **Metric:** All 102 Postman endpoints implemented
- **Target:** 102/102 endpoints return valid responses
- **Measurement:** Newman collection run

### Objective 2: Architecture Compliance
- **Metric:** Zero direct DbContext dependencies outside Repositories
- **Target:** `grep -r "private readonly AppDbContext" src/Server/Modules/*/Services/` returns empty
- **Measurement:** Static code analysis

### Objective 3: 100% Test Pass Rate
- **Metric:** Newman test pass rate
- **Target:** 100% (102/102 tests passing)
- **Measurement:** Newman report

---

## Appendix A: Current Architectural Violations Summary

| # | Location | Violation | Severity | Fix Task |
|---|----------|-----------|----------|----------|
| 1 | HealthController | Direct AppDbContext | High | 1.1 |
| 2 | PatientService | Direct AppDbContext.MedicalCases queries | High | 1.2 |
| 3 | HerbService | Direct AppDbContext queries | High | 1.3 |
| 4 | AuthService | Direct AppDbContext for tokens | Medium | 1.4 |
| 5 | SecurityAuditService | Direct AppDbContext | Medium | 1.4 |
| 6 | TokenRevocationService | Direct AppDbContext | Medium | 1.4 |
| 7 | MedicalCaseAuditService | Direct AppDbContext | Medium | 1.5 |
| 8 | SyncService | Direct AppDbContext (if any) | Low | 1.6 |

---

## Appendix B: Module Endpoint Inventory

### Implemented Controllers (13):
1. AuthController - 5 endpoints
2. UsersController - 16 endpoints
3. PatientsController - 13 endpoints
4. MedicalCasesController - 20+ endpoints
5. MedicalCaseWorkflowController - 6 endpoints
6. MedicalCasePrintController - 3 endpoints
7. MedicalCaseAuditController - 4 endpoints
8. HerbsController - 15 endpoints
9. FormulasController - 13 endpoints
10. RegistrationsController - 6 endpoints
11. SyncController - 5 endpoints
12. HealthController - 3 endpoints
13. DiagnosticsController - 3 endpoints

**Total:** ~112 endpoints (Postman shows 102, some may be documentation-only or consolidated)

---

## Appendix C: Test Collection Structure

### Postman Collection Folders:
1. **0. Auth** (5): Login, Auto-Login, Logout, Refresh, Validate
2. **1. Setup** (6): Create test data (User, Patient, MedicalCase, etc.)
3. **2. Users** (8): CRUD, batch, profile operations
4. **3. Patients** (13): CRUD, import/export, batch, reference checks
5. **4. Medical Cases** (20+): CRUD, workflow, batch, search
6. **5. Herbs** (15): CRUD, import/export, batch, reference checks
7. **6. Sync** (5): Metadata, compare, upload, download, delete
8. **7. Registrations** (6): Queue management
9. **8. Formulas** (13): CRUD, validation, import/export, batch
10. **9. Cleanup** (4): Delete test data

---

## Appendix D: Verification Checklist

### Pre-Deployment Checklist:
- [ ] All Phase 1 tasks complete (Architecture)
- [ ] All Phase 2 tasks complete (Functional)
- [ ] All Phase 3 tasks complete (Response Format)
- [ ] All Phase 4 tasks complete (Test Fixes)
- [ ] All Phase 5 verification passed
- [ ] Newman: 100% pass rate
- [ ] No direct DbContext in Controllers
- [ ] No direct DbContext in Services
- [ ] All integration tests pass
- [ ] Documentation updated
- [ ] CHANGELOG.md updated

---

## Appendix E: Rollback Plan

If critical issues are found during deployment:

1. **Immediate Rollback:**
   ```bash
   git revert HEAD~N..HEAD  # N = number of commits to rollback
   dotnet build
   dotnet test
   ```

2. **Database Migrations:**
   - No schema changes expected in this plan
   - If migrations exist, rollback using:
   ```bash
   dotnet ef database update <previous-migration>
   ```

3. **Verification After Rollback:**
   - Newman tests should pass (return to baseline)
   - Smoke tests on critical paths
   - Monitor error logs

---

*Plan Version: 1.0*  
*Created: 2026-03-29*  
*Estimated Duration: 8-12 days*  
*Reviewers: Architecture Team, QA Team*

# WebAPI Newman Test Fix Plan - LYBTZYZS

## Executive Summary

**Objective:** Achieve 100% pass rate (0 failures) for WebAPI Newman tests
**Current State:** 125 test failures remaining after Phase 1 fixes
**Target State:** 0 failures across all 102 API endpoints
**Collection:** `docs/06-operations/LYBTZYZS_API_Collection.json` (7013 lines)

## Context & Current State Analysis

### Phase 1 Fixes Completed
- ApiResponse format standardization
- Sync property casing fixes (EntityType, LocalEntities, etc.)
- Diagnostics SetLogLevel endpoint
- Formulas batch IDs

### Remaining Failure Categories (125 failures)

| Module | Failures | Root Cause |
|--------|----------|------------|
| Registration | 16 | Missing PatientName/DoctorName/Source fields in some requests |
| Sync | 4 | Invalid entity IDs (using "guid1" placeholders), missing LastModifiedAt |
| Formulas Batch | 6 | Using hardcoded IDs instead of {{testFormulaId}} |
| Data Dependencies | ~99 | Tests depend on prior test data that doesn't exist |

### DTO Structure Requirements

**RegistrationInputDto:**
```csharp
public class RegistrationInputDto
{
    [Required] public Guid PatientId { get; set; }
    [Required] public string PatientName { get; set; }
    [Required] public Guid DoctorId { get; set; }
    [Required] public string DoctorName { get; set; }
    public RegistrationSource Source { get; set; }
    public string? Remark { get; set; }
}
```

**LocalEntityMetadata (for Sync Compare):**
```csharp
public class LocalEntityMetadata
{
    public Guid EntityId { get; set; }
    public string Checksum { get; set; }
    public DateTime? LastModifiedAt { get; set; }  // REQUIRED
}
```

## Task Dependency Graph

| Task ID | Task Name | Depends On | Reason |
|---------|-----------|------------|--------|
| 1 | Verify Setup Folder Structure | None | Foundation - ensure setup requests exist and work |
| 2 | Fix Registration Create Request | 1 | Requires {{testPatientId}}, {{testDoctorId}}, {{testDoctorName}} from setup |
| 3 | Fix Sync Compare Request | 1 | Requires {{testHerbId}}, {{testPatientId}}, {{testFormulaId}} from setup |
| 4 | Fix Sync Download/Delete Requests | 3 | Depends on valid entity IDs from Sync fixes |
| 5 | Fix Formulas Batch Operations | 1 | Requires {{testFormulaId}} from setup |
| 6 | Fix Herbs Batch Operations | 1 | Requires {{testHerbId}} from setup |
| 7 | Fix Patients Batch Operations | 1 | Requires {{testPatientId}} from setup |
| 8 | Update Batch ID Placeholders | 5,6,7 | Mass update all "guid1", "guid2" placeholders to use variables |
| 9 | Add Missing Prerequisite Scripts | 2,3,4,5,6,7 | Add dependency checks to all affected requests |
| 10 | Run Full Newman Test Suite | 8,9 | Final validation of all fixes |

## Parallel Execution Graph

### Wave 1: Analysis & Verification (No Dependencies)
- **Task 1:** Verify Setup Folder Structure
- **Task 8:** Identify all "guid1", "guid2" placeholder locations

### Wave 2: Core Fixes (After Wave 1)
- **Task 2:** Fix Registration Create Request
- **Task 3:** Fix Sync Compare Request with LastModifiedAt
- **Task 5:** Fix Formulas Batch Operations
- **Task 6:** Fix Herbs Batch Operations
- **Task 7:** Fix Patients Batch Operations

### Wave 3: Dependent Fixes (After Wave 2)
- **Task 4:** Fix Sync Download/Delete Requests
- **Task 9:** Add Missing Prerequisite Scripts to all affected requests

### Wave 4: Final Validation (After Wave 3)
- **Task 10:** Run Full Newman Test Suite

**Critical Path:** 1 → 2/3/5/6/7 → 4/9 → 10  
**Estimated Parallel Speedup:** 60% (Wave 2 tasks can run in parallel)

## Detailed Task Specifications

### Task 1: Verify Setup Folder Structure
**Category:** `unspecified-low`  
**Skills:** []  
**Acceptance Criteria:**
- [ ] "0. Setup" folder exists as first item in collection
- [ ] Contains 4 requests: Create Test Patient, Get Doctor Info, Create Test Formula, Create Test Herb
- [ ] Each request properly stores IDs in collectionVariables
- [ ] Prerequisite scripts check for authToken

**JSON Structure to Verify:**
```json
{
  "name": "0. Setup",
  "item": [
    {
      "name": "1. Create Test Patient",
      "event": [{
        "listen": "test",
        "script": {
          "exec": ["pm.collectionVariables.set('testPatientId', jsonData.data.id);"]
        }
      }]
    },
    {
      "name": "2. Get Doctor Info",
      "event": [{
        "listen": "test",
        "script": {
          "exec": [
            "pm.collectionVariables.set('testDoctorId', doctor.id);",
            "pm.collectionVariables.set('testDoctorName', doctor.userName);"
          ]
        }
      }]
    },
    {
      "name": "3. Create Test Formula",
      "event": [{
        "listen": "test",
        "script": {
          "exec": ["pm.collectionVariables.set('testFormulaId', jsonData.data.id);"]
        }
      }]
    },
    {
      "name": "4. Create Test Herb",
      "event": [{
        "listen": "test",
        "script": {
          "exec": ["pm.collectionVariables.set('testHerbId', jsonData.data.id);"]
        }
      }]
    }
  ]
}
```

---

### Task 2: Fix Registration Create Request
**Category:** `quick`  
**Skills:** []  
**Depends On:** Task 1  
**Blocks:** Task 9  
**File:** `docs/06-operations/LYBTZYZS_API_Collection.json`  
**Location:** "11. Registrations" → "Create Registration"

**Current Body (Line ~6218):**
```json
{
  "patientId": "{{testPatientId}}",
  "patientName": "测试患者张三",
  "doctorId": "{{testDoctorId}}",
  "doctorName": "{{testDoctorName}}",
  "source": 0,
  "remark": "测试挂号"
}
```

**Fix Required:** ✅ Already correct! Verify prerequisite script.

**Prerequisite Script to Add:**
```javascript
// Check dependencies
if (!pm.collectionVariables.get('authToken')) {
    pm.expect.fail('No authToken found. Please run Login first.');
}
if (!pm.collectionVariables.get('testPatientId')) {
    pm.expect.fail('No testPatientId found. Please run Setup > Create Test Patient first.');
}
if (!pm.collectionVariables.get('testDoctorId')) {
    pm.expect.fail('No testDoctorId found. Please run Setup > Get Doctor Info first.');
}
if (!pm.collectionVariables.get('testDoctorName')) {
    pm.expect.fail('No testDoctorName found. Please run Setup > Get Doctor Info first.');
}
```

---

### Task 3: Fix Sync Compare Request
**Category:** `quick`  
**Skills:** []  
**Depends On:** Task 1  
**Blocks:** Task 4, Task 9  
**File:** `docs/06-operations/LYBTZYZS_API_Collection.json`  
**Location:** "10. Sync" → "Compare" (Line ~5922)

**Current Body (Line ~5951):**
```json
{
  "EntityType": "Herb",
  "LocalEntities": [
    {
      "EntityId": "guid1",
      "Checksum": "abc123"
    }
  ]
}
```

**Fixed Body:**
```json
{
  "EntityType": "Herb",
  "LocalEntities": [
    {
      "EntityId": "{{testHerbId}}",
      "Checksum": "d41d8cd98f00b204e9800998ecf8427e",
      "LastModifiedAt": "2026-01-01T00:00:00Z"
    },
    {
      "EntityId": "{{testPatientId}}",
      "Checksum": "d41d8cd98f00b204e9800998ecf8427e",
      "LastModifiedAt": "2026-01-01T00:00:00Z"
    },
    {
      "EntityId": "{{testFormulaId}}",
      "Checksum": "d41d8cd98f00b204e9800998ecf8427e",
      "LastModifiedAt": "2026-01-01T00:00:00Z"
    }
  ]
}
```

---

### Task 4: Fix Sync Download/Delete Requests
**Category:** `quick`  
**Skills:** []  
**Depends On:** Task 3  
**Blocks:** Task 9  
**File:** `docs/06-operations/LYBTZYZS_API_Collection.json`  
**Locations:** "10. Sync" → "Download", "Delete"

**Sync Download Current (Line ~6082):**
```json
{
  "EntityType": "Herb",
  "EntityIds": ["guid1"]
}
```

**Fixed:**
```json
{
  "EntityType": "Herb",
  "EntityIds": ["{{testHerbId}}", "{{testPatientId}}", "{{testFormulaId}}"]
}
```

**Sync Delete Current (Line ~6148):**
```json
{
  "EntityType": "Herb",
  "EntityIds": ["guid1"]
}
```

**Fixed:**
```json
{
  "EntityType": "Herb",
  "EntityIds": ["{{testHerbId}}"]
}
```

---

### Task 5: Fix Formulas Batch Operations
**Category:** `quick`  
**Skills:** []  
**Depends On:** Task 1  
**Blocks:** Task 8, Task 9  
**File:** `docs/06-operations/LYBTZYZS_API_Collection.json`  
**Locations:** "9. Formulas" → Batch operations

**Affected Requests:**
1. Batch Import (Line ~5179) - Uses hardcoded "Formula1", needs update
2. Batch Delete (Line ~5613) - ✅ Already uses {{testFormulaId}}
3. Batch Enable (Line ~5682) - ✅ Already uses {{testFormulaId}}
4. Batch Disable (Line ~5751) - ✅ Already uses {{testFormulaId}}

**Batch Import Fix (Line ~5181):**
```json
{
  "formulas": [
    {
      "name": "TestBatchFormula-{{$timestamp}}",
      "herbs": [
        {
          "herbName": "测试药材",
          "dosage": 5,
          "unit": "克"
        }
      ]
    }
  ]
}
```

---

### Task 6: Fix Herbs Batch Operations
**Category:** `quick`  
**Skills:** []  
**Depends On:** Task 1  
**Blocks:** Task 8, Task 9  
**File:** `docs/06-operations/LYBTZYZS_API_Collection.json`  
**Locations:** "8. Herbs" → Batch operations

**Affected Requests:**
- Batch Enable (Line ~4631)
- Batch Disable (Line ~4697)
- Batch Delete (Line ~4763)

**Current Body:**
```json
{
  "ids": ["guid1"]
}
```

**Fixed:**
```json
{
  "ids": ["{{testHerbId}}"]
}
```

---

### Task 7: Fix Patients Batch Operations
**Category:** `quick`  
**Skills:** []  
**Depends On:** Task 1  
**Blocks:** Task 8, Task 9  
**File:** `docs/06-operations/LYBTZYZS_API_Collection.json`  
**Locations:** "3. Patients" → Batch operations

**Affected Requests:**
- Batch Delete (Line ~2127)
- Batch Check Reference (Line ~2252)

**Batch Delete Current (Line ~2129):**
```json
{
  "ids": ["guid1"]
}
```

**Fixed:**
```json
{
  "ids": ["{{testPatientId}}"]
}
```

**Batch Check Reference Current (Line ~2254):**
```json
{
  "patientIds": ["guid1"]
}
```

**Fixed:**
```json
{
  "patientIds": ["{{testPatientId}}"]
}
```

---

### Task 8: Update Batch ID Placeholders
**Category:** `quick`  
**Skills:** []  
**Depends On:** Task 5, Task 6, Task 7  
**Blocks:** Task 10  

**Pattern Replacement Map:**

| File | Pattern | Replacement | Location |
|------|---------|-------------|----------|
| LYBTZYZS_API_Collection.json | `"guid1"` | `"{{testHerbId}}"` | Herbs batch operations |
| LYBTZYZS_API_Collection.json | `"guid1"` | `"{{testPatientId}}"` | Patients batch operations |
| LYBTZYZS_API_Collection.json | `"guid1"` | `"{{testFormulaId}}"` | Formulas batch operations (where applicable) |
| LYBTZYZS_API_Collection.json | `["guid1", "guid2"]` | `["{{testHerbId}}"]` | Herbs batch-delete |

---

### Task 9: Add Missing Prerequisite Scripts
**Category:** `quick`  
**Skills:** []  
**Depends On:** Task 2, Task 3, Task 4, Task 5, Task 6, Task 7  
**Blocks:** Task 10  

**Script Template:**
```javascript
// Check dependencies
if (!pm.collectionVariables.get('authToken')) {
    pm.expect.fail('No authToken found. Please run Login first.');
}
if (!pm.collectionVariables.get('testVariableName')) {
    pm.expect.fail('No testVariableName found. Please run Setup > Specific Setup Step first.');
}
```

**Requests Needing Scripts:**
1. Registration → Create Registration (testPatientId, testDoctorId, testDoctorName)
2. Sync → Compare (testHerbId, testPatientId, testFormulaId)
3. Sync → Download (same as above)
4. Sync → Delete (testHerbId)
5. Herbs → Batch Enable (testHerbId)
6. Herbs → Batch Disable (testHerbId)
7. Herbs → Batch Delete (testHerbId)
8. Patients → Batch Delete (testPatientId)
9. Patients → Batch Check Reference (testPatientId)
10. Formulas → Batch Import (authToken only)
11. Formulas → Batch Delete (testFormulaId)
12. Formulas → Batch Enable (testFormulaId)
13. Formulas → Batch Disable (testFormulaId)

---

### Task 10: Run Full Newman Test Suite
**Category:** `unspecified-low`  
**Skills:** []  
**Depends On:** Task 8, Task 9  

**QA Steps:**
```bash
# Start the WebAPI server first
dotnet run --project src/Server/Services/LYBT.WebAPI

# In another terminal, run Newman tests
newman run docs/06-operations/LYBTZYZS_API_Collection.json \
  --environment docs/06-operations/newman-environment.json \
  --reporters cli,json \
  --reporter-json-export docs/06-operations/newman-report.json \
  --delay-request 100
```

**Acceptance Criteria:**
- [ ] All 102 requests execute
- [ ] 0 failures reported
- [ ] 100% assertions pass
- [ ] Report saved to `docs/06-operations/newman-report.json`

---

## JSON Fix Code Reference

### Setup Request: Create Test Patient
```json
{
  "name": "1. Create Test Patient",
  "request": {
    "method": "POST",
    "header": [
      {"key": "Content-Type", "value": "application/json"},
      {"key": "Authorization", "value": "Bearer {{authToken}}"}
    ],
    "url": "{{baseUrl}}/api/v1/patients",
    "body": {
      "mode": "raw",
      "raw": "{\n  \"name\": \"测试患者Newman\",\n  \"gender\": 1,\n  \"birthDate\": \"1990-01-01T00:00:00Z\",\n  \"phoneNumber\": \"13800138000\",\n  \"address\": \"测试地址\"\n}"
    }
  },
  "event": [
    {
      "listen": "prerequest",
      "script": {
        "exec": ["if (!pm.collectionVariables.get('authToken')) { pm.expect.fail('No authToken'); }"]
      }
    },
    {
      "listen": "test",
      "script": {
        "exec": [
          "pm.test('Status 201', function () { pm.response.to.have.status(201); });",
          "const jsonData = pm.response.json();",
          "if (jsonData.success && jsonData.data && jsonData.data.id) {",
          "    pm.collectionVariables.set('testPatientId', jsonData.data.id);",
          "    console.log('Created patient:', jsonData.data.id);",
          "}"
        ]
      }
    }
  ]
}
```

### Registration Create (Verified)
```json
{
  "name": "Create Registration",
  "request": {
    "method": "POST",
    "header": [
      {"key": "Content-Type", "value": "application/json"},
      {"key": "Authorization", "value": "Bearer {{authToken}}"}
    ],
    "url": "{{baseUrl}}/api/v1/registrations",
    "body": {
      "mode": "raw",
      "raw": "{\n  \"patientId\": \"{{testPatientId}}\",\n  \"patientName\": \"测试患者Newman\",\n  \"doctorId\": \"{{testDoctorId}}\",\n  \"doctorName\": \"{{testDoctorName}}\",\n  \"source\": 0,\n  \"remark\": \"Newman测试挂号\"\n}"
    }
  }
}
```

### Sync Compare (Fixed)
```json
{
  "name": "Compare",
  "request": {
    "method": "POST",
    "header": [
      {"key": "Content-Type", "value": "application/json"},
      {"key": "Authorization", "value": "Bearer {{authToken}}"}
    ],
    "url": "{{baseUrl}}/api/v1/sync/compare",
    "body": {
      "mode": "raw",
      "raw": "{\n  \"EntityType\": \"Herb\",\n  \"LocalEntities\": [\n    {\n      \"EntityId\": \"{{testHerbId}}\",\n      \"Checksum\": \"d41d8cd98f00b204e9800998ecf8427e\",\n      \"LastModifiedAt\": \"2026-01-01T00:00:00Z\"\n    }\n  ]\n}"
    }
  }
}
```

---

## Commit Strategy

### Commit 1: Setup Verification & Fixes
```
fix(newman): verify and update Setup folder structure

- Ensure Setup folder exists as first collection item
- Verify all 4 setup requests properly store IDs
- Add comprehensive prerequest scripts to setup requests

Refs: Newman Test Fix Phase 2
```

### Commit 2: Registration Module Fixes
```
fix(newman): add prerequisite scripts to Registration requests

- Add dependency checks for testPatientId, testDoctorId, testDoctorName
- Ensure Create Registration waits for setup completion
- Update Get/Update/Delete Registration to verify testRegistrationId

Refs: Newman Test Fix Phase 2
```

### Commit 3: Sync Module Fixes
```
fix(newman): fix Sync module entity IDs and structure

- Replace "guid1" placeholders with {{testHerbId}}, {{testPatientId}}, {{testFormulaId}}
- Add LastModifiedAt field to Sync Compare LocalEntities
- Fix Sync Download and Delete to use valid entity IDs
- Add prerequisite scripts to all Sync requests

Refs: Newman Test Fix Phase 2
```

### Commit 4: Batch Operations Fixes
```
fix(newman): update batch operations to use test entity IDs

- Herbs: Batch Enable/Disable/Delete use {{testHerbId}}
- Patients: Batch Delete/Check Reference use {{testPatientId}}
- Formulas: Batch Import uses unique names, Batch ops use {{testFormulaId}}
- Add prerequisite scripts to all batch operations

Refs: Newman Test Fix Phase 2
```

### Commit 5: Final Verification
```
test(newman): verify 100% test pass rate

- Run complete Newman test suite
- Confirm 0 failures across all 102 endpoints
- Update newman-report.json with latest results

Refs: Newman Test Fix Phase 2 Complete
```

---

## Success Criteria Checklist

- [ ] Setup folder exists and runs first
- [ ] All setup requests store IDs in collectionVariables
- [ ] Registration Create uses {{testPatientId}}, {{testDoctorId}}, {{testDoctorName}}, Source=0
- [ ] Sync Compare uses valid entity IDs with LastModifiedAt
- [ ] Sync Download/Delete use valid entity IDs
- [ ] All batch operations use {{testHerbId}}, {{testPatientId}}, {{testFormulaId}} instead of "guid1"
- [ ] All affected requests have prerequisite scripts
- [ ] Newman test run shows 0 failures
- [ ] All 102 requests pass assertions

---

## Risk Mitigation

| Risk | Impact | Mitigation |
|------|--------|------------|
| Setup requests fail | High | Verify API endpoints work independently first |
| Variable name mismatches | Medium | Use consistent naming: testPatientId, testDoctorId, etc. |
| Race conditions in parallel | Low | Newman runs sequentially within a collection |
| Test data conflicts | Low | Use unique names with timestamps where needed |
| Missing test entity | High | Add fallback logic in prerequisite scripts |

---

## TODO List (ADD THESE)

### Wave 1 (Start Immediately - No Dependencies)

- [ ] **1. Verify Setup Folder Structure**
  - What: Verify "0. Setup" folder exists with 4 requests; check they store IDs correctly
  - Depends: None
  - Blocks: 2, 3, 5, 6, 7
  - Category: `unspecified-low`
  - Skills: []
  - QA: `cat docs/06-operations/LYBTZYZS_API_Collection.json | jq '.item[0].name'` returns "0. Setup"

- [ ] **2. Identify All "guid1" Placeholders**
  - What: Search collection for all "guid1" and "guid2" placeholders; document locations
  - Depends: None
  - Blocks: 8
  - Category: `quick`
  - Skills: []
  - QA: List of all lines containing "guid1" or "guid2" in collection file

### Wave 2 (After Wave 1 Completes - Can Run in Parallel)

- [ ] **3. Fix Registration Create Prerequisite Script**
  - What: Add prerequest script to "Create Registration" to check testPatientId, testDoctorId, testDoctorName
  - Depends: 1
  - Blocks: 9
  - Category: `quick`
  - Skills: []
  - QA: Registration Create has prerequest script with 4 dependency checks

- [ ] **4. Fix Sync Compare Request Body**
  - What: Replace "guid1" with {{testHerbId}}, add LastModifiedAt field to LocalEntities
  - Depends: 1
  - Blocks: 5, 9
  - Category: `quick`
  - Skills: []
  - QA: Sync Compare body uses {{testHerbId}} and includes LastModifiedAt: "2026-01-01T00:00:00Z"

- [ ] **5. Fix Sync Download Request Body**
  - What: Replace "guid1" in EntityIds array with {{testHerbId}}, {{testPatientId}}, {{testFormulaId}}
  - Depends: 4
  - Blocks: 9
  - Category: `quick`
  - Skills: []
  - QA: Sync Download body uses valid entity IDs from variables

- [ ] **6. Fix Sync Delete Request Body**
  - What: Replace "guid1" in EntityIds array with {{testHerbId}}
  - Depends: 4
  - Blocks: 9
  - Category: `quick`
  - Skills: []
  - QA: Sync Delete body uses {{testHerbId}}

- [ ] **7. Fix Herbs Batch Operations**
  - What: Update Batch Enable/Disable/Delete to use {{testHerbId}} instead of "guid1"
  - Depends: 1
  - Blocks: 8, 9
  - Category: `quick`
  - Skills: []
  - QA: Herbs batch operations use {{testHerbId}}

- [ ] **8. Fix Patients Batch Operations**
  - What: Update Batch Delete and Batch Check Reference to use {{testPatientId}}
  - Depends: 1
  - Blocks: 8, 9
  - Category: `quick`
  - Skills: []
  - QA: Patients batch operations use {{testPatientId}}

### Wave 3 (After Wave 2 Completes)

- [ ] **9. Add Prerequisite Scripts to All Affected Requests**
  - What: Add prerequest scripts to check for required variables in all fixed requests
  - Depends: 3, 5, 6, 7, 8
  - Blocks: 10
  - Category: `quick`
  - Skills: []
  - QA: All 13 affected requests have prerequest scripts with dependency checks

### Wave 4 (Final Verification)

- [ ] **10. Run Full Newman Test Suite**
  - What: Execute complete Newman test run and verify 0 failures
  - Depends: 9
  - Blocks: None
  - Category: `unspecified-low`
  - Skills: []
  - QA: `newman run` shows "0 failures" and "102 requests"

---

## Execution Instructions

1. **Wave 1**: Run Task 1 and Task 2 in parallel
   ```
   task(category="unspecified-low", load_skills=[], run_in_background=false, prompt="Task 1: Verify Setup Folder Structure...")
   task(category="quick", load_skills=[], run_in_background=false, prompt="Task 2: Identify All guid1 Placeholders...")
   ```

2. **Wave 2**: After Wave 1, run Tasks 3-8 in parallel
   ```
   task(category="quick", load_skills=[], run_in_background=false, prompt="Task 3: Fix Registration Create...")
   task(category="quick", load_skills=[], run_in_background=false, prompt="Task 4: Fix Sync Compare...")
   task(category="quick", load_skills=[], run_in_background=false, prompt="Task 5: Fix Sync Download...")
   task(category="quick", load_skills=[], run_in_background=false, prompt="Task 6: Fix Sync Delete...")
   task(category="quick", load_skills=[], run_in_background=false, prompt="Task 7: Fix Herbs Batch...")
   task(category="quick", load_skills=[], run_in_background=false, prompt="Task 8: Fix Patients Batch...")
   ```

3. **Wave 3**: After Wave 2, run Task 9
   ```
   task(category="quick", load_skills=[], run_in_background=false, prompt="Task 9: Add Prerequisite Scripts...")
   ```

4. **Wave 4**: After Wave 3, run Task 10
   ```
   task(category="unspecified-low", load_skills=[], run_in_background=false, prompt="Task 10: Run Full Newman Test Suite...")
   ```

5. **Commit**: After all waves complete, commit changes following the commit strategy

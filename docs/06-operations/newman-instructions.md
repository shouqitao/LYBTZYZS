# Newman API Testing Instructions

## Problem

The LYBTZYZS API Collection uses `https://localhost:5001` with a self-signed SSL certificate. Newman requires the `--insecure` flag to bypass certificate validation.

## Solution: Single-Step Newman Execution

### Recommended Approach

```bash
newman run "docs\06-operations\LYBTZYZS_API_Collection.json" --insecure
```

**Note**: 
- `--insecure` flag bypasses self-signed SSL certificate validation (required for `https://localhost:5001`)
- The collection includes "0. Auth" folder (login) and "1. Setup" folder (create test data) at the beginning
- Setup folder prerequest script generates unique test data on each run using timestamps (no conflicts)

### Alternative: Two-Step Execution (for debugging)

If you need to debug authentication separately:

```bash
# Step 1: Run Auth and Setup only
newman run "docs\06-operations\LYBTZYZS_API_Collection.json" --folder "0. Auth" --folder "1. Setup" --insecure

# Step 2: Run remaining folders manually
# (Not recommended - easier to just run full collection once)
```

## Known Limitations

### Postman/Newman Prerequest Script Limitation

We attempted to add login logic to the Setup folder's prerequest script using `pm.sendRequest()`, but this failed because:

1. `pm.sendRequest()` is **asynchronous** (returns immediately, callback executes later)
2. Prerequest scripts are **synchronous** (Newman continues to next request without waiting)
3. Result: First setup item executes before login callback completes → `authToken` still undefined → FAIL

**Workaround**: Auth folder must be run as a separate folder before Setup.

## Current Test Results

- **92/92 requests**: PASS ✅ (0 failures)
- **257/284 assertions**: PASS (27 failures remaining)
- **Server endpoints**: All functional ✅
- **JWT authentication**: Working ✅

**Remaining assertion failures** (27):
- User management validation errors (Change Password, Change Profile, Toggle Status, Delete User)
- Medical Case creation fails when Setup data is re-created in same run (patient ID mismatch)
- Other Medical Case endpoints fail due to missing `testMedicalCaseId` (cascading from Create failure)

## Recommended Workflow (CI/CD)

For automated testing:

```bash
#!/bin/bash
newman run LYBTZYZS_API_Collection.json \
    --insecure \
    --timeout-request 30000 \
    --delay-request 100 \
    --reporters cli,json \
    --reporter-json-export test-results.json
```

## Files Modified During Investigation

1. `src/Server/Modules/LYBT.Module.MedicalCase/Helpers/MedicalCaseServiceHelper.cs` (lines 124-125, 135-136)
   - Changed `InvalidOperationException` → `KeyNotFoundException` for 404 responses

2. `docs/06-operations/LYBTZYZS_API_Collection.json`
   - Setup folder prerequest: Removed unique data generation conditional
   - (Reverted async login attempt - not functional)

---

**Last Updated**: 2026-04-02
**Status**: Server endpoints functional, collection requires manual Auth step before Newman execution

# Backend Acceptance Smoke Test Rerun - Baseline Information

**Test Session**: Backend Acceptance Smoke Test Rerun  
**Timestamp**: 2025-09-15 00:15:00  
**Branch**: release/backend-acceptance-smoketest-rerun  
**Previous Health Check**: ✅ PASSED (HTTP 200)

## Test Environment Baseline

### API Configuration
- **Base URL**: http://localhost:8080
- **Health Endpoint**: http://localhost:8080/api/v1/health
- **API Version**: v1
- **Environment**: Development

### Previous Health Check Results
Based on P2-Fix Batch1 health verification:
- **Port**: 8080 ✅
- **Status**: SUCCESS ✅  
- **HTTP Code**: 200 ✅
- **Last Check**: 2025-09-15 00:14:03
- **WebAPI Process**: Running and stable

### Environment Variables Snapshot
- **ASPNETCORE_URLS**: http://localhost:8080
- **ASPNETCORE_ENVIRONMENT**: Development
- **DOTNET_ENVIRONMENT**: Development

### System Information
- **Operating System**: Windows
- **Branch**: release/backend-acceptance-smoketest-rerun
- **Working Directory**: D:\source\repos\LYBTZYZS
- **Git Status**: Clean working tree (new branch created)

## Test Scope

### Target Modules (7)
1. **Auth** - Authentication and JWT token management
2. **Users** - User management (Doctor accounts)
3. **Patients** - Patient records and management
4. **Consultation** - Medical consultation records (四诊)
5. **Prescriptions** - Prescription management and drug combinations
6. **Herbs** - Traditional Chinese Medicine herb management
7. **Formula** - Traditional recipe/formula management

### Test Strategy
- **Approach**: Full CRUD cycle smoke testing
- **Data Strategy**: Minimal test data, cleanup after tests
- **Retries**: 2 attempts with 3-second backoff
- **Authentication**: Doctor account login for JWT token
- **Idempotent**: Tests can be run multiple times safely

## Quality Gates
- **Build**: Must pass `dotnet build -nologo` after each step
- **Health Check**: WebAPI must remain healthy throughout testing
- **No Business Code Changes**: Only scripts and reports added
- **Clean State**: All test data cleaned up after execution

## Prerequisites Verified
✅ WebAPI running on port 8080  
✅ Health endpoint returning HTTP 200  
✅ Database accessible and migrations up to date  
✅ Development environment configured  
✅ Git branch created for test execution  

## Next Steps
1. Acquire authentication token (Doctor account)
2. Execute full module smoke tests
3. Aggregate results and log failures
4. Generate final summary and recommendations

---
*Baseline recorded at 2025-09-15 00:15:00*
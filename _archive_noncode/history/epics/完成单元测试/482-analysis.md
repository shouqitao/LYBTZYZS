---
issue: 482
analyzed: 2025-09-03T13:55:00Z
updated: 2025-09-03T16:00:04Z
type: epic
---

# Epic Analysis: 完成单元测试覆盖率提升到60%

## Parallel Work Streams

### Stream A: Test Infrastructure Setup
**Agent Type**: general-purpose
**Priority**: High (prerequisite)
**Files**:
- tests/Backend/TestBase/*.cs
- tests/Backend/TestUtilities/*.cs
- tests/TestDataFactory/*.cs

**Scope**:
- Create base test classes
- Set up test data factories using Bogus
- Configure InMemory database contexts
- Create mock service helpers
- Set up AutoMapper test configuration

**Dependencies**: None (can start immediately)

### Stream B: Auth & Users Module Testing
**Agent Type**: test-runner
**Priority**: High
**Files**:
- tests/Backend/LYBT.Module.Auth.Tests/*.cs
- tests/Backend/LYBT.Module.Users.Tests/*.cs

**Scope**:
- Auth module Service layer tests
- Users module Service layer tests
- JWT token validation tests
- User CRUD operation tests
- Permission and role tests

**Dependencies**: Stream A (test infrastructure)

### Stream C: Patient & MedicalCase Module Testing
**Agent Type**: test-runner
**Priority**: High
**Files**:
- tests/Backend/LYBT.Module.Patients.Tests/*.cs
- tests/Backend/LYBT.Module.MedicalCase.Tests/*.cs

**Scope**:
- Patient Service layer tests
- MedicalCase Service layer tests
- Patient registration workflow tests
- Medical case lifecycle tests

**Dependencies**: Stream A (test infrastructure)

### Stream D: Clinical Modules Testing (Consultation, Prescriptions)
**Agent Type**: test-runner
**Priority**: High
**Files**:
- tests/Backend/LYBT.Module.Consultation.Tests/*.cs
- tests/Backend/LYBT.Module.Prescriptions.Tests/*.cs

**Scope**:
- Consultation Service tests (TCM four diagnostics)
- Prescription Service tests
- Prescription generation logic tests
- Herb compatibility tests

**Dependencies**: Stream A (test infrastructure)

### Stream E: Herbs & Formula Module Testing
**Agent Type**: test-runner
**Priority**: Medium
**Files**:
- tests/Backend/LYBT.Module.Herbs.Tests/*.cs
- tests/Backend/LYBT.Module.Formula.Tests/*.cs

**Scope**:
- Herbs Service layer tests
- Formula Service layer tests
- Herb database query tests
- Formula template tests

**Dependencies**: Stream A (test infrastructure)

### Stream F: Repository Layer Testing
**Agent Type**: test-runner
**Priority**: Medium
**Files**:
- tests/Backend/LYBT.Infrastructure.Tests/Repositories/*.cs

**Scope**:
- Complete UserRepository tests
- Complete PatientRepository tests
- Complete HerbRepository tests
- Add other repository tests

**Dependencies**: Stream A (test infrastructure)

## Execution Plan

### Phase 1 (Immediate):
- Start Stream A (Infrastructure) - MUST complete first

### Phase 2 (After Infrastructure):
- Launch Stream B, C, D, E in parallel
- These can work independently on different modules

### Phase 3 (After Module Tests):
- Launch Stream F for repository testing
- Can run alongside remaining module tests

## Coordination Rules

1. **File Ownership**: Each stream owns its test files exclusively
2. **Shared Resources**: Test infrastructure (Stream A) must be read-only after completion
3. **Commit Pattern**: "Test #482: [Stream X] {specific change}"
4. **Progress Tracking**: Update stream-specific progress files
5. **Blocking Issues**: Report in progress file and wait for resolution

## Success Metrics

- Test coverage increases from 2.76% to 60%+
- All tests pass consistently (>99% success rate)
- Test execution time < 5 minutes
- No flaky tests

## Risk Mitigation

1. **Complex Business Logic**: Use integration tests where unit tests are insufficient
2. **Test Data Complexity**: Create comprehensive test data factories upfront
3. **Performance**: Use InMemory database and parallel test execution
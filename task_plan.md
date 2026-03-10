# Task Plan: Test Cleanup + PRD-Driven Refactoring

## Goal

清理旧测试、迁移 UserJourneys 到 DomainCollection 体系，为 Phase 2 (US Tests) 铺平道路。

## Decisions

| Decision | Rationale |
|----------|-----------|
| 删除旧 Features/ 集成测试 | 将被 Phase 2 US 测试完全替代，旧测试设计有超时问题 |
| 保留 UserJourneys 并迁移 | 跨角色 E2E 流程不可替代 (RBAC, BR-001/003, AD-01/04/09) |
| 迁移到 DomainCollection | 从 [Collection("Server")] 迁移到 Auth/Clinical/HerbFormula 等，支持并行 |
| 删除 "Server" Collection | 迁移完成后无测试使用，清除死代码 |
| 保留 PureLogic/RateLimiting | 稳定、快速、无超时风险 |

## Phases

### Phase 0: Test Cleanup -- complete

#### 0.1: Delete old Features/ integration tests -- complete
删除 19 个旧集成测试文件 (保留 US_* 新测试):
- Features/Auth/AuthIntegrationTests.cs
- Features/Auth/AuthSmokeTests.cs
- Features/Auth/AuthTokenAdvancedIntegrationTests.cs
- Features/Formulas/FormulaIntegrationTests.cs
- Features/Formulas/FormulaServiceIntegrationTests.cs
- Features/Herbs/HerbIntegrationTests.cs
- Features/Infrastructure/ApiResponseContractTests.cs
- Features/Infrastructure/CorrelationIdMiddlewareIntegrationTests.cs
- Features/Infrastructure/DiagnosticsControllerIntegrationTests.cs
- Features/Infrastructure/HealthCheckIntegrationTests.cs
- Features/Infrastructure/PerformanceDataSeeder.cs
- Features/Infrastructure/PerformanceTests.cs
- Features/MedicalCases/MedicalCaseIntegrationTests.cs
- Features/MedicalCases/MedicalCasePermissionAndFilterTests.cs
- Features/MedicalCases/PrescriptionAggregateTests.cs
- Features/Patients/PatientIntegrationTests.cs
- Features/Registration/RegistrationIntegrationTests.cs
- Features/Sync/SyncIntegrationTests.cs
- Features/Users/UserIntegrationTests.cs

#### 0.2: Migrate UserJourneys to DomainCollections -- complete
每个 Journey 类: 改 [Collection] + 改基类为泛型 JourneyTestBase<TFixture>

| File | Old Collection | New Collection | New Base |
|------|---------------|----------------|----------|
| AuthJourneyTests | Server | Auth | JourneyTestBase<AuthFixture> |
| AdminSetupJourneyTests | Server | Users | JourneyTestBase<UserFixture> |
| BootstrapJourneyTests | Server | Users | JourneyTestBase<UserFixture> |
| FirstVisitJourneyTests | Server | Clinical | JourneyTestBase<ClinicalFixture> |
| ReturnVisitJourneyTests | Server | Clinical | JourneyTestBase<ClinicalFixture> |
| DoctorClinicalJourneyTests | Server | Clinical | JourneyTestBase<ClinicalFixture> |
| MedicalCaseEditJourneyTests | Server | Clinical | JourneyTestBase<ClinicalFixture> |
| PatientManagementJourneyTests | Server | Clinical | JourneyTestBase<ClinicalFixture> |
| HerbFormulaManagementJourneyTests | Server | HerbFormula | JourneyTestBase<HerbFormulaFixture> |
| BatchOperationsJourneyTests | Server | HerbFormula | JourneyTestBase<HerbFormulaFixture> |
| CrossNarrativeValidationTests | Server | Clinical | JourneyTestBase<ClinicalFixture> |

#### 0.3: Cleanup dead infrastructure -- complete
- 删除 ServerTestCollection.cs ([Collection("Server")] 定义)
- 移除 IntegrationTestBase 非泛型向后兼容类 (如无引用)
- 移除 JourneyTestBase 非泛型向后兼容类 (如无引用)

#### 0.4: Verify compile + test -- complete
- dotnet build
- dotnet test (PureLogic + US_* + UserJourneys + RateLimiting 全部 PASS)

### Phase 1: Infrastructure Foundation -- complete (previous session)

### Phase 2: Must Have US Tests (46 US) -- complete

#### 2.1: Fix existing US_* test failures -- complete
14 test failures fixed (assertion mismatches + missing DTO fields).
Result: 100 US_* tests PASS, 871 total server tests PASS

#### 2.2: Coverage audit + depth enhancement -- complete
- PRD audit: 46 Must Have US (not 51), 45 server-testable
- 12 US with thin coverage (1 test) identified and enhanced
- Added 14 boundary/negative tests (8 MC + 4 REG + 2 AUTH)
- Discovery: double-complete is idempotent (200 OK)
Result: 114 US_* tests, 885 total server tests PASS

### Phase 3: Should Have US Tests (47 server-testable) -- complete

Plan: `docs/plans/2026-03-10-phase3-should-have-us-tests.md`

| Batch | Modules | US | Status |
|-------|---------|-----|--------|
| 1 | Users | 5 | complete (10 tests) |
| 2 | Herbs + Formulas | 8 | complete (14 tests) |
| 3 | Patients + Registration | 3 | complete (8 tests) |
| 4 | Error Handling | 5 | complete (11 tests) |
| 5 | MedicalCase | 8 | complete (16 tests) |
| 6 | Sync | 7 | complete (13 tests) |
| 7 | Auth + Config + Logging | 10 | complete (10 tests) |

## Errors Encountered

| Error | Attempt | Resolution |
|-------|---------|------------|
| (from previous session) PK_Users duplicate key | 1 | Respawn.ResetAsync before SeedBaseDataAsync |

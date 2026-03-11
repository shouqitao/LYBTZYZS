# Task Plan: 测试驱动开发 (TDD)

## Goal

通过 TDD 确保权限矩阵缺陷修复 (v1.3) 和 Journey Test 重构按设计完成，实现 80%+ 测试覆盖率。

---

## Decisions

| Decision | Rationale |
|----------|-----------|
| TDD 原则 | RED→GREEN→REFACTOR 循环 |
| Journey Test 优先 | Layer A 端到端流程优先验证 |
| 删除冗余测试 | DoctorClinicalJourneyTests 与 FirstVisit 重叠 |
| Layer B 暂缓 | Features/ 下 111 测试移到 _Deferred/ |
| 测试命名规范 | 包含 US 编号 (如 `US_AUTH_001_...`) |

---

## Phases

### Phase 1: 权限矩阵缺陷修复验证 (v1.3)

**Status**: complete

| Task | Description | Test File | Status |
|------|-------------|-----------|--------|
| 1.1 | D-4 Receptionist 医案可见性 | ReceptionistJourneyTests.cs | [x] |
| 1.2 | D-5 归属字段一致性 | MedicalCaseModelTests.cs | [x] |
| 1.3 | I-2 当天可编辑边界 (04:00) | MedicalCaseEditJourneyTests.cs | [x] |
| 1.4 | G-9 Registration 回退流程 | RegistrationJourneyTests.cs | [x] |
| 1.5 | G-11 医生禁用后挂号处理 | DoctorDisableJourneyTests.cs | [x] |

---

### Phase 2: Journey Test 重构 (Chapter 0-8)

**Status**: in_progress

| Task | Chapter | File | Status |
|------|---------|------|--------|
| 2.1 | Chapter 0 - Auth & Security | AuthJourneyTests.cs (补全负面) | [x] |
| 2.2 | Chapter 1 - System Bootstrap | BootstrapJourneyTests.cs | [ ] |
| 2.3 | Chapter 2 - Admin Setup | AdminSetupJourneyTests.cs | [ ] |
| 2.4 | Chapter 3 - Master Data | HerbFormulaManagementJourneyTests.cs | [ ] |
| 2.5 | Chapter 4 - Patient Management | PatientManagementJourneyTests.cs | [x] |
| 2.6 | Chapter 5 - First Visit | FirstVisitJourneyTests.cs | [ ] |
| 2.7 | Chapter 6 - Return Visit | ReturnVisitJourneyTests.cs | [x] |
| 2.8 | Chapter 7 - Medical Case Edit | MedicalCaseEditJourneyTests.cs | [x] |
| 2.9 | Chapter 8 - Cross-Narrative Guard | CrossNarrativeValidationTests.cs | [ ] |
| 2.10 | 删除冗余 | DoctorClinicalJourneyTests.cs (删除) | [ ] |

---

### Phase 3: PRD 驱动测试对齐 (45 Must Have US)

**Status**: pending

| Module | Must Have US | Test File | Status |
|--------|-------------|-----------|--------|
| AUTH | 12 | US_Auth_MustHaveTests.cs | [ ] |
| USER | 8 | US_User_MustHaveTests.cs | [ ] |
| HERB | 5 | US_Herb_MustHaveTests.cs | [ ] |
| FORM | 6 | US_Formula_MustHaveTests.cs | [ ] |
| PAT | 4 | US_Patient_MustHaveTests.cs | [ ] |
| MC | 7 | US_MedicalCase_MustHaveTests.cs | [ ] |
| REG | 6 | US_Registration_MustHaveTests.cs | [ ] |
| SYNC | 3 | US_Sync_MustHaveTests.cs | [ ] |

---

### Phase 4: 验证与完成

**Status**: pending

| Task | Description | Acceptance Criteria | Status |
|------|-------------|---------------------|--------|
| 4.1 | 全量测试运行 | Server >95%, Desktop >95%, Architecture 100% | [ ] |
| 4.2 | 测试覆盖率验证 | 80%+ (Must Have US 100%) | [ ] |
| 4.3 | 文档同步 | role-permission-matrix.md, test-coverage-baseline.md | [ ] |

---

## Errors Encountered

| Error | Phase | Resolution | Status |
|-------|-------|------------|--------|
| - | - | - | - |

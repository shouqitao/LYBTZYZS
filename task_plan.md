# Task Plan: LYBTZYZS Frontend Service Layer Completion
<!-- 
  WHAT: This is your roadmap for implementing missing Service layers in LYBTZYZS project.
  WHY: After gap analysis, we need to systematically implement the missing interfaces and implementations.
  WHEN: Create this FIRST, before starting any work. Update after each phase completes.
-->

## Goal
<!-- 
  WHAT: Implement all missing Service interfaces and remote implementations to achieve proper 3-layer architecture and full API coverage for LYBTZYZS frontend.
  WHY: This enables proper separation of concerns, unit testing, and business logic encapsulation.
-->
Implement missing Service interfaces (IUserService, IHerbService, IRegistrationService) and enhance existing ones (IFormulaService, IPatientService) to achieve full API coverage and proper 3-layer architecture in LYBTZYZS frontend.

## Current Phase
<!-- 
  WHAT: Which phase you're currently working on (e.g., "Phase 1", "Phase 3").
  WHY: Quick reference for where you are in the task. Update this as you progress.
-->
Phase 2

## Phases
<!-- 
  WHAT: Break your task into logical phases. Each phase should be completable.
  WHY: Breaking work into phases prevents overwhelm and makes progress visible.
-->

### Phase 1: Create IUserService Interface + RemoteUserService Implementation (T1-1)
<!-- 
  WHAT: Create IUserService interface with all 14 API methods and RemoteUserService implementation.
  WHY: User module currently has 0 Service coverage, ViewModel calls API directly.
-->
- [x] Analyze existing IUserService.cs (has 10 methods, missing 4)
- [x] Add missing methods to IUserService interface
- [x] Create RemoteUserService.cs implementation
- [x] Update User module to use Service instead of direct API
- **Status:** complete

### Phase 2: Create IHerbService Interface + RemoteHerbService Implementation (T1-2)
<!-- 
  WHAT: Create IHerbService interface with all 13 API methods and RemoteHerbService implementation.
  WHY: Herb module has 0 Service coverage, ViewModel uses Repository directly.
-->
- [ ] Create IHerbService.cs interface with all methods
- [ ] Create RemoteHerbService.cs implementation
- [ ] Update HerbMasterDetailViewModel to use Service
- [ ] Ensure all CRUD + batch operations covered
- **Status:** pending

### Phase 3: Create IRegistrationService Interface + RemoteRegistrationService Implementation (T1-3)
<!-- 
  WHAT: Create IRegistrationService interface with all 6 API methods and RemoteRegistrationService implementation.
  WHY: Registration module has 0 Service coverage, ViewModel uses Repository directly.
-->
- [ ] Create IRegistrationService.cs interface
- [ ] Create RemoteRegistrationService.cs implementation
- [ ] Update RegistrationListViewModel to use Service
- [ ] Add QuickVisitAsync method for US-REG-002
- **Status:** pending

### Phase 4: Enhance IFormulaService (T1-4)
<!-- 
  WHAT: Add missing API methods to IFormulaService (currently has 4, needs 14 total).
  WHY: Formula service covers only basic CRUD, missing batch/import/export operations.
-->
- [ ] Analyze current IFormulaService.cs
- [ ] Add missing methods (CloneFormula, ToggleStatus, etc.)
- [ ] Update RemoteFormulaService implementation
- [ ] Ensure FormulaMasterDetailViewModel uses enhanced Service
- **Status:** pending

### Phase 5: Enhance IPatientService (T1-5)
<!-- 
  WHAT: Add missing API methods to IPatientService (currently has 8, needs 10 total).
  WHY: Missing BatchImport and Export operations.
-->
- [ ] Review current IPatientService.cs
- [ ] Add BatchImportAsync and Export operations
- [ ] Update RemotePatientService implementation
- [ ] Verify PatientMasterDetailViewModel compatibility
- **Status:** pending

### Phase 6: Testing & Verification
<!-- 
  WHAT: Run tests to verify all Service implementations work correctly.
  WHY: Ensure no regressions and all API methods are properly encapsulated.
-->
- [ ] Run desktop tests (dotnet test tests/LYBTZYZS.Tests.Desktop/)
- [ ] Verify lint and typecheck pass
- [ ] Test ViewModel-Service integration
- [ ] Update AGENTS.md if needed
- **Status:** pending

## Key Questions
<!-- 
  WHAT: Important questions from frontend-completion.md D1-D6 to answer during implementation.
-->
1. How to implement QuickVisitAsync for registration? (D1: 挂号模块实现策略)
2. Should Service methods include CancellationToken? (D2: Service层设计原则)
3. How to handle authentication in Service layer? (D3: Service层设计原则)
4. Priority order for missing methods? (D4: 优先级决策)
5. How to implement GetPendingValidationAsync for formulas? (D5: 验方待审核列表)
6. Integration with MedicalCase Pending queue? (D6: 挂号队列 vs 待接诊队列)

## Decisions Made
<!-- 
  WHAT: Technical and design decisions made during planning.
-->
| Decision | Rationale |
|----------|-----------|
| Follow existing Service patterns | Maintain consistency with IPatientService, IMedicalCaseService implementations |
| Add CancellationToken to all methods | Matches T6-2 requirement for Service interfaces |
| Use RemoteXxxService naming | Consistent with existing RemotePatientService, RemoteFormulaService |
| Implement all API methods | Achieve full coverage as per gap analysis |

## Errors Encountered
<!-- 
  WHAT: Every error encountered, attempt number, and resolution.
-->
| Error | Attempt | Resolution |
|-------|---------|------------|
|       | 1       |            |

## Notes
<!-- 
  REMINDERS:
  - Update phase status as you progress: pending → in_progress → complete
  - Re-read this plan before major decisions (attention manipulation)
  - Log ALL errors - they help avoid repetition
  - Never repeat a failed action - mutate your approach instead
-->
- Update phase status as you progress: pending → in_progress → complete
- Re-read this plan before major decisions (attention manipulation)
- Log ALL errors - they help avoid repetition
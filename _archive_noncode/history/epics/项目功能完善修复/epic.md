---
name: 项目功能完善修复
status: backlog
created: 2025-09-05T03:23:02Z
progress: 0%
prd: .claude/prds/项目功能完善修复PRD.md
github: https://github.com/shouqitao/LYBTZYZS/issues/511
---

# Epic: 项目功能完善修复

## Overview

This epic addresses critical system usability issues identified through Phase 3 business process validation. The implementation focuses on removing artificial frontend limitations that prevent core medical functions from working, despite having complete backend API support (95% complete). The goal is to transform the system from 25% usability to 90% usability through targeted frontend fixes and UI optimizations.

## Architecture Decisions

### Core Strategy: Minimal Code Changes, Maximum Value
- **Remove artificial limitations**: Replace hardcoded failure responses with proper API calls
- **Leverage existing backend**: Utilize the 95% complete enterprise-grade APIs already implemented
- **Progressive enhancement**: Focus on P0 critical fixes first, then UI optimizations
- **Risk mitigation**: Incremental rollout with comprehensive testing at each stage

### Technical Decisions
- **API Integration Pattern**: Use existing `_apiManager` and `ApiExceptionHandler` patterns
- **Error Handling**: Replace "简单诊所版本暂不支持" with proper error handling
- **UI Framework**: Continue with existing WPF + Prism architecture
- **Data Binding**: Leverage existing MVVM patterns and AutoMapper configurations

## Technical Approach

### Frontend Components

#### P0: Critical Function Restoration
**MedicalCase Module Recovery**:
- Remove 7 hardcoded failure methods (CreateAsync, UpdateAsync, SuspendAsync, etc.)
- Restore proper API calls using existing `_apiManager.MedicalCaseApi`
- Implement proper exception handling with `ApiExceptionHandler`

**Consultation Module Recovery**:
- Restore `StartAsync` for beginning consultations
- Enable `SaveFourDiagnosisAsync` for TCM four-diagnosis data
- Reactivate `GetFourDiagnosisByMedicalCaseIdAsync` for data retrieval

#### P1: UI Experience Enhancement
**Excel Import Wizard** (`PatientImportWizardViewModel.cs`):
- Implement 4-step wizard UI content switching logic
- Add step-specific content views and progress indicators
- Enhance user guidance and error feedback

**Dialog Optimizations**:
- Formula editing dialog functionality (5 TODOs)
- Prescription editor improvements (4 TODOs) 
- Medical case management UI (3 TODOs)

### Backend Services

**Minimal Backend Changes Required**:
- Backend APIs are 95% complete and functional
- No new endpoints needed for P0 critical fixes
- Possible minor API enhancements for UI optimization features
- Maintain existing authentication and authorization patterns

### Infrastructure

**Deployment Considerations**:
- Zero-downtime deployment approach
- Feature flags for gradual rollout
- Rollback capability for each restoration phase
- Monitoring of restored function usage and error rates

## Implementation Strategy

### Phase 1: Critical System Restoration (2-3 weeks)
**Goal**: System usability 25% → 70%
- Week 1-2: P0 frontend limitation removal
- Week 3: Testing and validation of core medical workflows

### Phase 2: User Experience Enhancement (2-3 weeks)  
**Goal**: System usability 70% → 90%
- Week 4: Excel import wizard completion
- Week 5-6: Dialog and UI optimizations

### Phase 3: Polish and Quality Assurance (1-2 weeks)
**Goal**: Production readiness
- Week 7: Comprehensive testing and bug fixes
- Week 8: User acceptance testing and deployment preparation

### Risk Mitigation
- **Incremental approach**: One module at a time to minimize impact
- **Comprehensive testing**: Unit, integration, and user acceptance testing
- **User communication**: Clear communication about restored functionality
- **Rollback plans**: Ability to revert changes if issues arise

## Task Breakdown

This epic has been decomposed into 9 coordinated tasks organized by implementation phases:

### Phase 1: P0 Critical Function Restoration
- **#512**: [MedicalCase模块功能恢复](512.md) - Remove hardcoded limitations, restore 7 core functions (L: 16-20h)
- **#513**: [Consultation模块功能恢复](513.md) - Enable TCM four-diagnosis workflow (L: 18-22h) 
- **#514**: [其他模块审查修复](514.md) - Comprehensive audit of Formula, Users, Prescriptions modules (M: 10-14h)

### Phase 2: P1 UI Enhancement (Can run in parallel after Phase 1)
- **#515**: [Excel导入向导完善](515.md) - Complete 4-step wizard UI (PatientImportWizardViewModel.cs) (L: 16-20h)
- **#516**: [验方编辑对话框优化](516.md) - EditFormulaDialogViewModel enhancements (5 TODOs) (L: 18-22h)
- **#517**: [处方编辑器完善](517.md) - PrescriptionEditorDialogViewModel improvements (4 TODOs) (L: 20-24h)
- **#518**: [医案管理界面优化](518.md) - MedicalCaseManagementViewModel enhancements (3 TODOs) (M: 14-18h)
- **#519**: [UI细节完善](519.md) - Remaining 6 minor UI TODOs across various ViewModels (M: 12-16h)

### Phase 3: Quality Assurance
- **#520**: [测试验证和质量保证](520.md) - Comprehensive testing and QA (L: 20-24h)

**Total Effort**: 144-178 hours (6-8 weeks with proper resource allocation)

### Task Dependencies
- Tasks #515-#519 can execute in parallel after tasks #512-#514 complete
- Task #520 requires all development tasks (#512-#519) to be completed
- Phase 1 completion enables 90% of core medical workflows
- Phase 2 optimizes user experience and completes remaining functionality
## Dependencies

### External Dependencies
- **Backend API availability**: 95% complete APIs must remain functional
- **Database integrity**: Medical data structure consistency
- **Authentication system**: Existing JWT and RBAC systems must continue working

### Internal Dependencies  
- **UltraThink architecture**: Maintain compatibility with existing dual-layer architecture
- **AutoMapper configurations**: Ensure DTO mappings remain valid
- **Prism container**: Dependency injection configurations must support restored functions

### Prerequisite Work
- **Phase 3 validation completion**: ✅ Already completed
- **TODO analysis**: ✅ Already completed
- **Architecture stability**: ✅ System currently compiles with zero errors/warnings

## Success Criteria (Technical)

### P0 Critical Success Criteria
- **Medical workflow completion rate**: 0% → 90%
- **Medical case management functionality**: 15% → 95%
- **TCM four-diagnosis capability**: 5% → 85%
- **Zero "简单诊所版本暂不支持" error messages**
- **System compilation**: Zero errors/warnings maintained

### P1 Enhancement Success Criteria
- **Excel import wizard**: 4-step process completion rate >95%
- **Dialog responsiveness**: All interactions <2 seconds
- **User satisfaction**: >90% satisfaction in acceptance testing
- **TODO elimination**: All 27 identified TODOs resolved

### Technical Quality Gates
- **Code coverage**: Maintain existing coverage levels
- **Performance**: No regression in system performance  
- **Security**: All restored functions maintain existing security controls
- **Compatibility**: Full compatibility with existing data and workflows

## Estimated Effort

### Overall Timeline: 6-8 weeks
**Resource Requirements**:
- 1 Senior Frontend Developer (full-time)
- 0.5 QA Engineer (testing and validation)
- 0.25 Technical Writer (documentation updates)

### Critical Path Items
1. **P0 MedicalCase restoration** (Week 1-2) - Blocks entire medical workflow
2. **P0 Consultation restoration** (Week 2-3) - Enables TCM professional features  
3. **P1 Excel wizard** (Week 4) - Core data entry functionality
4. **Testing and validation** (Week 7-8) - Production readiness

### Risk Buffers
- **20% contingency** built into timeline estimates
- **Parallel development** where possible to accelerate delivery
- **MVP approach** for P1/P2 features to ensure P0 completion

---

## Implementation Notes

This epic represents a unique situation where significant value can be unlocked through minimal technical changes. The primary challenge is not building new functionality, but rather removing artificial constraints that prevent existing functionality from being used.

**Key Success Factor**: Focus on systematic removal of limitations while maintaining system stability and data integrity.

**Expected Outcome**: Transform the system from "technically complete but unusable" to "production-ready professional medical system" that fully realizes the investment made in backend development.

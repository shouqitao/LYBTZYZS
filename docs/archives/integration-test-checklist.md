# Integration Test Checklist - Phase 3.2

**Project**: 凌隐宝堂中医诊所管理系统 (LYBTZYZS)  
**Focus**: Frontend UX Optimization - Integration Testing  
**Date**: 2026-04-18  
**Status**: Ready for Testing

---

## Test Environment Setup

### Prerequisites
- [ ] Application builds successfully without errors
- [ ] Database is accessible and properly seeded with test data
- [ ] Test patient data available (at least 3-5 patients)
- [ ] Test formulas available in system
- [ ] Printer configured (for print testing)

### Test Accounts
- [ ] Clinician account (Clinical mode access)
- [ ] Administrator account (Management mode access)

---

## Test Scenario 1: Full Clinical Workflow

### Objective
Verify end-to-end clinical workflow from patient selection to case completion.

### Steps

1. **Navigate to Patient Selection**
   - [ ] Launch application
   - [ ] Login as clinician
   - [ ] Navigate to Patient Selection screen
   - [ ] Verify: Patient list loads successfully

2. **Create New Medical Case**
   - [ ] Select a test patient
   - [ ] Click "开始看诊" (Start Consultation)
   - [ ] Verify: MedicalCaseWorkspaceView opens
   - [ ] Verify: WorkflowStepIndicator shows Step 1 (四诊采集) active
   - [ ] Verify: No step indicators are green (all pending)

3. **Step 1: 四诊采集 (Four Examinations)**
   - [ ] Fill in 现病史 (Present Illness) with at least 10 characters
   - [ ] Verify: Green checkmark (✓) appears next to field
   - [ ] Verify: Step indicator advances to Step 2 (中医辨证)
   - [ ] Select 舌诊 (Tongue Diagnosis) from dropdown
   - [ ] Select 脉诊 (Pulse Diagnosis) from dropdown
   - [ ] Verify: Toast notification NOT triggered (field-level feedback only)

4. **Step 2: 中医辨证 (TCM Diagnosis)**
   - [ ] Fill in 中医诊断 (TcmDiagnosis) with valid diagnosis (e.g., "脾胃虚弱证")
   - [ ] Verify: Green checkmark (✓) appears next to field
   - [ ] Verify: Validation message disappears
   - [ ] Verify: Step indicator advances to Step 3 (处方决策)
   - [ ] Verify: CompletenessCheck shows "中医诊断: 已填写" in green

5. **Step 3: 处方决策 (Prescription Decision)**
   - [ ] Verify: Step indicator advances to Step 4 (处方编辑)
   - [ ] Select "需要处方" (Need Prescription) radio button
   - [ ] Verify: Toast notification shows "已决定需要处方" (if added)
   - [ ] Verify: Prescription section becomes enabled

6. **Step 4: 处方编辑 (Prescription Editing)**
   - [ ] Click "套验方" (Import Formula) button
   - [ ] Select a test formula from dialog
   - [ ] Click OK to import
   - [ ] Verify: Toast notification appears: "已导入验方「XXX」，共N味药材" for 5 seconds
   - [ ] Verify: Green checkmark (✓) appears in "共N味药材" text
   - [ ] Set 剂数 (Dosage Count) to 7
   - [ ] Select 用法 (Usage) from dropdown
   - [ ] Verify: Prescription calculations update (单剂价, 总价)
   - [ ] Verify: Step indicator advances to Step 5 (完成看诊)
   - [ ] Verify: CompletenessCheck shows all items green with "可以完成看诊"

7. **Suspend Case**
   - [ ] Click "暂存医案" (Suspend) button
   - [ ] Verify: Toast notification: "医案已暂存，可稍后继续" for 5 seconds
   - [ ] Verify: Loading indicator "正在暂存医案..." appears then disappears
   - [ ] Verify: Button state changes appropriately

8. **Resume Case**
   - [ ] Navigate back to patient list
   - [ ] Select same patient
   - [ ] Click "开始看诊"
   - [ ] Verify: All previously entered data is restored
   - [ ] Verify: WorkflowStepIndicator shows correct step
   - [ ] Verify: CompletenessCheck shows correct state

9. **Complete Case**
   - [ ] Click "完成看诊" (Complete Consultation) button
   - [ ] Verify: Toast notification: "看诊完成，医案已归档" for 5 seconds
   - [ ] Verify: Loading indicator "正在完成看诊并归档..." appears then disappears
   - [ ] Verify: Navigation back to Patient Selection
   - [ ] Verify: Case status shows as "Completed"

### Expected Results
- ✅ All steps progress logically
- ✅ Visual feedback (checkmarks, colors, toasts) appears at appropriate times
- ✅ Toast messages persist for 4-5 seconds
- ✅ Step indicator auto-advances based on field completion
- ✅ CompletenessCheck updates in real-time
- ✅ Data persists correctly through suspend/resume cycle

---

## Test Scenario 2: Management Mode Workflow

### Objective
Verify edit workflow for completed cases in Management mode.

### Steps

1. **Navigate to Management**
   - [ ] Launch application
   - [ ] Login as administrator
   - [ ] Navigate to 医案管理 (MedicalCase Management)
   - [ ] Verify: MasterDetailView loads with case list

2. **Open Completed Case**
   - [ ] Select a completed case from list
   - [ ] Click "查看详情" (View Details)
   - [ ] Verify: Read-only view displays correctly
   - [ ] Verify: All fields show entered data
   - [ ] Verify: No edit controls visible

3. **Enter Edit Mode**
   - [ ] Click "修改医案" (Edit Case) button
   - [ ] Verify: Toast notification: "进入编辑模式" (if added)
   - [ ] Verify: MedicalCaseEditControl appears in Compact mode
   - [ ] Verify: WorkflowStepIndicator shows correct step (should be Step 5)
   - [ ] Verify: CompletenessCheck shows all green with "可以完成看诊"

4. **Modify Diagnosis**
   - [ ] Update 中医诊断 (TcmDiagnosis) field
   - [ ] Verify: Green checkmark (✓) appears
   - [ ] Verify: Validation updates appropriately

5. **Modify Prescription**
   - [ ] Click "清空" (Clear) button
   - [ ] Verify: Confirmation dialog appears
   - [ ] Confirm clearing
   - [ ] Verify: Toast notification: "已清空所有药材（共N味）" for 4 seconds
   - [ ] Add new prescription items via formula import
   - [ ] Verify: Toast notification: "已导入验方「XXX」，共N味药材" for 5 seconds

6. **Save Changes**
   - [ ] Click "保存医案" (Save Case) button
   - [ ] Verify: Toast notification: "医案已保存" for 5 seconds
   - [ ] Verify: Loading indicator "正在保存医案..." appears then disappears
   - [ ] Verify: View returns to read-only mode
   - [ ] Verify: Changes are persisted

### Expected Results
- ✅ Edit mode works correctly in Compact layout
- ✅ Read-only view remains unchanged
- ✅ All toast notifications appear with proper duration
- ✅ Changes save successfully
- ✅ No data loss during edit cycle

---

## Test Scenario 3: Error Handling

### Objective
Verify application handles errors gracefully with clear user feedback.

### Steps

1. **Validation Errors**
   - [ ] Start new case without filling any fields
   - [ ] Try to click "完成看诊" (Complete)
   - [ ] Verify: Button is disabled (CanComplete = false)
   - [ ] Verify: CompletenessCheck shows "尚未完成所有必填项" in amber
   - [ ] Fill diagnosis only
   - [ ] Try to complete (with prescription enabled but no items)
   - [ ] Verify: Button remains disabled
   - [ ] Verify: CompletenessCheck shows prescription items incomplete

2. **Network Failure Simulation** (if test environment allows)
   - [ ] Start new case
   - [ ] Fill all required fields
   - [ ] Disconnect network (if possible) or mock service failure
   - [ ] Click "暂存医案" (Suspend)
   - [ ] Verify: Toast notification: "暂存失败：[error message]" for 4 seconds
   - [ ] Verify: Error message is descriptive and not technical
   - [ ] Verify: Application remains responsive
   - [ ] Verify: Data is not lost (can retry)

3. **Concurrent Edit Conflict** (if supported)
   - [ ] Open same case in two sessions (if possible)
   - [ ] Modify and save in Session A
   - [ ] Try to save in Session B
   - [ ] Verify: Appropriate conflict resolution message
   - [ ] Verify: Data integrity maintained

### Expected Results
- ✅ Validation errors prevent invalid actions
- ✅ Error messages are user-friendly (Chinese, descriptive)
- ✅ Toast notifications show errors in appropriate color (red)
- ✅ Application doesn't crash on errors
- ✅ User can recover from error states

---

## Test Scenario 4: Toast Notification Behavior

### Objective
Verify toast notifications work correctly across all scenarios.

### Steps

1. **Success Toasts**
   - [ ] Trigger save success
   - [ ] Verify: Toast appears at top of window
   - [ ] Verify: Green background with success icon
   - [ ] Verify: Smooth fade-in animation (0.3s)
   - [ ] Verify: Stays visible for 5 seconds
   - [ ] Verify: Smooth fade-out animation (0.2s)
   - [ ] Verify: No stacking (new toast replaces old)

2. **Info Toasts**
   - [ ] Trigger suspend success
   - [ ] Verify: Blue background with info icon
   - [ ] Verify: 5 second duration

3. **Warning Toasts**
   - [ ] Trigger clear herbs action
   - [ ] Verify: Yellow/amber background with warning icon
   - [ ] Verify: 4 second duration
   - [ ] Verify: Message includes item count

4. **Error Toasts**
   - [ ] Trigger an error (validation failure, network error, etc.)
   - [ ] Verify: Red background with error icon
   - [ ] Verify: 4 second duration
   - [ ] Verify: Error message is descriptive

### Expected Results
- ✅ All toast types appear correctly
- ✅ Animations are smooth and professional
- ✅ Durations match specification (4-5 seconds)
- ✅ Colors match message type
- ✅ No toast stacking issues

---

## Test Scenario 5: Step Indicator Behavior

### Objective
Verify 5-step workflow indicator functions correctly.

### Steps

1. **Initial State**
   - [ ] Open new case
   - [ ] Verify: Step 1 (四诊采集) is active (blue background)
   - [ ] Verify: Steps 2-5 are pending (gray background)
   - [ ] Verify: No checkmarks in completeness section

2. **Progression Through Steps**
   - [ ] Fill in PresentIllness
   - [ ] Verify: Step 1 becomes green (completed)
   - [ ] Verify: Step 2 becomes active (blue)
   - [ ] Verify: Smooth transition animation
   - [ ] Fill in TcmDiagnosis
   - [ ] Verify: Step 2 becomes green
   - [ ] Verify: Step 3 becomes active
   - [ ] Make prescription decision
   - [ ] Verify: Step 3 becomes green
   - [ ] Verify: Step 4 becomes active
   - [ ] Add prescription items
   - [ ] Verify: Step 4 becomes green
   - [ ] Verify: Step 5 becomes active

3. **Step Completion Indicators**
   - [ ] After completing all steps
   - [ ] Verify: All steps 1-4 show green checkmarks
   - [ ] Verify: Step 5 shows as current/active
   - [ ] Verify: Progress dots between steps are green

### Expected Results
- ✅ Auto-advance logic works correctly
- ✅ Visual feedback is clear
- ✅ Transitions are smooth
- ✅ No step is skipped incorrectly

---

## Test Scenario 6: Visual Feedback Validation

### Objective
Verify field-level validation feedback works as designed.

### Steps

1. **PresentIllness Field**
   - [ ] Field initially empty: no checkmark
   - [ ] Type 1-4 characters: no checkmark
   - [ ] Type 5+ characters: green checkmark appears
   - [ ] Delete content: checkmark disappears

2. **TcmDiagnosis Field**
   - [ ] Field initially empty: no checkmark
   - [ ] Type less than 2 characters: no checkmark, validation may show
   - [ ] Type valid diagnosis: green checkmark appears
   - [ ] Verify: Validation error message disappears

3. **Prescription Item Count**
   - [ ] Initially: no checkmark
   - [ ] Add first herb: checkmark appears
   - [ ] Delete all herbs: checkmark disappears

4. **CompletenessCheck Section**
   - [ ] Initially: All items amber (incomplete)
   - [ ] As fields fill: Items turn green with appropriate status text
   - [ ] When all complete: "可以完成看诊" appears in green
   - [ ] When incomplete: "尚未完成所有必填项" shows in amber

### Expected Results
- ✅ Checkmarks appear/disappear dynamically
- ✅ Colors indicate completion state (green/amber)
- ✅ Status text is accurate and helpful
- ✅ CompletenessCheck updates in real-time

---

## Regression Testing

### Verify Phase 1 Changes Don't Break Existing Functionality

1. **Print Functionality**
   - [ ] Open completed case with prescription
   - [ ] Click "打印处方单" (Print Prescription)
   - [ ] Verify: Print preview displays correctly
   - [ ] Verify: No errors in Compact mode layout

2. **Export PDF**
   - [ ] Open completed case with prescription
   - [ ] Click "导出PDF" (Export PDF)
   - [ ] Verify: Toast notification: "PDF导出成功，文件已保存" for 5 seconds
   - [ ] Verify: File is saved correctly

3. **History Copy**
   - [ ] In active case, click "历史处方" (History Prescription)
   - [ ] Select previous prescription
   - [ ] Verify: Toast notification: "已复制历史处方，共N味药材" for 5 seconds
   - [ ] Verify: Items are added correctly

4. **Formula Import**
   - [ ] In active case, click "套验方" (Import Formula)
   - [ ] Select formula
   - [ ] Verify: Toast notification with formula name and count
   - [ ] Verify: All herbs imported correctly

### Expected Results
- ✅ All existing features work in Compact mode
- ✅ No regressions from Full mode removal
- ✅ Toast messages provide better feedback than before
- ✅ Print/export functionality unchanged

---

## Performance Observations

### Monitor During Testing

- [ ] UI response time: All operations complete within 2 seconds
- [ ] Toast animations: Smooth, no lag or stuttering
- [ ] Step transitions: Instant, no delay
- [ ] Validation feedback: Immediate (< 100ms)
- [ ] Memory usage: No significant increase from Phase 1 changes
- [ ] CPU usage: No spikes during animations

---

## Bug Reporting Template

If any issues are found, document them using this template:

```
**Bug ID**: INT-001
**Scenario**: [Test scenario and step]
**Severity**: [Critical/High/Medium/Low]
**Description**: [What happened vs. what was expected]
**Steps to Reproduce**: 
1. [Step 1]
2. [Step 2]
**Expected**: [What should happen]
**Actual**: [What actually happened]
**Screenshot**: [If applicable]
**Environment**: [OS version, app version]
```

---

## Test Sign-Off

### Tester Information
- **Name**: ___________________
- **Date**: ___________________
- **Test Environment**: [Dev/Staging/Production]
- **Build Version**: ___________________

### Test Results Summary
- **Scenario 1 (Clinical Workflow)**: [ ] Pass [ ] Fail
- **Scenario 2 (Management Mode)**: [ ] Pass [ ] Fail
- **Scenario 3 (Error Handling)**: [ ] Pass [ ] Fail
- **Scenario 4 (Toast Behavior)**: [ ] Pass [ ] Fail
- **Scenario 5 (Step Indicator)**: [ ] Pass [ ] Fail
- **Scenario 6 (Visual Feedback)**: [ ] Pass [ ] Fail
- **Regression Tests**: [ ] Pass [ ] Fail

### Overall Assessment
- **Ready for UAT**: [ ] Yes [ ] No
- **Critical Issues**: _____
- **Recommendations**: _____

---

**Next Steps After Integration Testing**:
1. Fix any critical bugs found
2. Proceed to Phase 3.4: User Acceptance Testing (UAT)
3. Collect feedback from alpha testers (2 clinicians)
4. Iterate on critical issues
5. Prepare for beta rollout (5 clinicians)

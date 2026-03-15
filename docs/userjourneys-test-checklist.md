# UserJourneys 测试清单

> **生成日期**: 2026-03-13
> **测试项目**: LYBT.Tests.Server.UserJourneys
> **总计**: 146 个测试方法

---

## 1. AuthJourneyTests (4 tests)

- [ ] Auth_Full_Journey
- [ ] Auth_Login_NonExistentUser_Returns401
- [ ] Auth_Login_DisabledUser_Returns403
- [ ] Auth_Login_EmptyCredentials_Returns400

---

## 2. BootstrapJourneyTests (7 tests)

- [ ] US_BOOTSTRAP_001_Full_Journey
- [ ] US_USER_001_CreateUser_DuplicateUsername_ShouldFail
- [ ] US_USER_001_CreateUser_AdminCannotCreateAdmin_ShouldFail
- [ ] US_HERB_001_CreateHerb_DuplicateName_ShouldFail
- [ ] US_AUTH_001_SysAdmin_DefaultLogin_ShouldSucceed
- [ ] US_SYS_001_002_003_HealthEndpoint_AllChecksPass
- [ ] US_USER_001_CreateUser_ReservedUsername_ShouldFail

---

## 3. AdminSetupJourneyTests (9 tests)

- [ ] US_ADMIN_SETUP_001_Full_Journey
- [ ] US_USER_001_CreateUser_DuplicateUsername_ShouldFail
- [ ] US_USER_001_CreateUser_ReservedUsername_ShouldFail
- [ ] US_USER_001_CreateUser_AdminCannotCreateAdmin_ShouldFail
- [ ] US_USER_004_UpdateUser_ChangeDoctorRole_ShouldSucceed
- [ ] US_USER_005_DeleteUser_CannotDeleteSelf_ShouldFail
- [ ] US_USER_009_ChangePassword_OldPasswordIncorrect_ShouldFail

---

## 4. FirstVisitJourneyTests (5 tests)

- [ ] US_REG_001_NormalPath_CompleteFirstVisit
- [ ] US_MC_009_DuplicateActiveCase_ShouldFail
- [ ] US_MC_004_EmptyDiagnosis_BlocksCompletion
- [ ] US_MC_004_NoPrescriptionDecision_BlocksCompletion
- [ ] US_REG_004_CancelRegistration_Succeeds

---

## 5. ReturnVisitJourneyTests (6 tests)

- [ ] US_PAT_002_MC_009_ReturnVisit_Normal_Path
- [ ] US_MC_005_ReturnVisit_Exception_CompletedCase_RequiresEditReason
- [ ] US_REG_006_CancelMedicalCase_ReceptionistSource_RevertToWaiting
- [ ] US_REG_006_CancelMedicalCase_DoctorSource_AutoCancelled
- [ ] US_MC_018_CopyHistoricalPrescription_Succeeds
- [ ] US_MC_018_CopyPrescription_DisabledHerb_Skipped

---

## 6. MedicalCaseEditJourneyTests (5 tests)

- [ ] US_MC_013_DoctorCannotEditOtherDoctorCase_Returns403
- [ ] US_MC_013_EditCompletedWithoutReason_Returns422
- [ ] US_MC_014_SameDayCompleted_DoctorCanEdit_NotLocked
- [ ] US_MC_013_AdminCanEditLockedCase_WithEditReason
- [ ] MedicalCaseEdit_Full_Journey

---

## 7. PatientManagementJourneyTests (16 tests)

- [ ] US_PAT_001_CreatePatient_WithValidData_ReturnsCreatedPatientWithPinYin
- [ ] US_PAT_001_CreatePatient_DuplicatePhoneNumber_Returns400
- [ ] US_PAT_001_CreatePatient_DuplicateIdNumber_Returns400
- [ ] US_PAT_001_CreatePatient_FutureBirthDate_Returns400
- [ ] US_PAT_002_SearchPatient_ByKeyword_ReturnsMatchingResults
- [ ] US_PAT_003_GetPatientDetail_ByValidId_ReturnsPatientWithAge
- [ ] US_PAT_003_GetPatientDetail_InvalidId_Returns400
- [ ] US_PAT_004_UpdatePatient_NameChanged_PinYinRegenerated
- [ ] US_PAT_004_UpdatePatient_DuplicatePhone_Returns400
- [ ] US_PAT_005_DeletePatient_NoReferences_ReturnsSuccess
- [ ] US_PAT_005_DeletePatient_HasMedicalCases_Returns422
- [ ] US_PAT_013_DisablePatient_WithActiveMedicalCase_Returns422
- [ ] US_PAT_013_DisablePatient_Success_StatusChanged
- [ ] US_PAT_013_ToggleStatus_EnableDisabledPatient_ReturnsSuccess
- [ ] US_PAT_013_DisabledPatient_CannotCreateMedicalCase_Returns422
- [ ] US_PAT_002_Receptionist_CannotSeeDisabledPatients

---

## 8. HerbFormulaManagementJourneyTests (26 tests)

### 药材管理 (Herb)
- [ ] US_HERB_001_CreateHerb_WithValidData_ReturnsCreatedHerbWithPinYin
- [ ] US_HERB_001_CreateHerb_WithoutName_Returns400
- [ ] US_HERB_001_CreateHerb_WithZeroPrice_Returns400
- [ ] US_HERB_002_SearchHerb_ByKeyword_ReturnsMatchingResults
- [ ] US_HERB_003_GetHerbDetail_ById_ReturnsCompleteInfo
- [ ] US_HERB_003_GetHerbDetail_NonexistentId_Returns404
- [ ] US_HERB_004_UpdateHerb_ModifiesPriceAndRegeneratesPinYin
- [ ] US_HERB_004_UpdateHerb_NonexistentId_Returns404
- [ ] US_HERB_005_DeleteHerb_WithoutReferences_Succeeds
- [ ] US_HERB_005_CheckReference_ReturnsReferenceStatus
- [ ] US_HERB_005_DeleteHerb_WithPrescriptionReference_Blocked
- [ ] US_HERB_006_ToggleHerbStatus_DisabledHerb_NotInList

### 验方管理 (Formula)
- [ ] US_FORM_001_CreateFormula_WithHerbs_ReturnsCreatedFormula
- [ ] US_FORM_001_CreateFormula_WithoutHerbs_Returns400
- [ ] US_FORM_001_CreateFormula_WithDeferredBinding_Succeeds
- [ ] US_FORM_002_ListFormulas_ReturnsPaginatedResults
- [ ] US_FORM_003_GetFormulaDetail_ReturnsCompleteInfo
- [ ] US_FORM_003_GetFormulaDetail_NonexistentId_Returns404
- [ ] US_FORM_004_UpdateFormula_ModifiesFields
- [ ] US_FORM_004_UpdateFormula_OtherDoctorsFormula_Returns403
- [ ] US_FORM_005_DeleteFormula_Succeeds
- [ ] US_FORM_005_DeleteFormula_NonexistentId_Returns404
- [ ] US_FORM_006_ToggleFormulaStatus_DisabledFormula_NotInList
- [ ] US_FORM_008_SharedFormula_VisibleToOtherDoctors

### 集成测试
- [ ] US_HERB_FORMULA_Full_Journey_AdminDoctorPrescriptionIntegration

---

## 9. BatchOperationsJourneyTests (1 test)

- [ ] BatchOperations_Full_Journey

---

## 10. CrossNarrativeValidationTests (8 tests)

- [ ] US_MC_001_PatientDisable_BlocksCaseCreation
- [ ] US_HERB_005_ReferenceProtection_BlocksDeletion
- [ ] US_AUTH_003_TokenRefresh_LongSession
- [ ] US_SYS_001_HealthCheck_Endpoint
- [ ] US_REG_001_PatientDisable_BlocksRegistration
- [ ] US_MC_004_DisabledHerb_AcceptedInPrescription
- [ ] US_MC_001_CaseNumber_MappedAndUnique
- [ ] US_MC_015_PrintCompleted_WorksAfterClose

---

## 测试统计

| 测试文件 | 测试数量 |
|----------|----------|
| AuthJourneyTests | 4 |
| BootstrapJourneyTests | 7 |
| AdminSetupJourneyTests | 9 |
| FirstVisitJourneyTests | 5 |
| ReturnVisitJourneyTests | 6 |
| MedicalCaseEditJourneyTests | 5 |
| PatientManagementJourneyTests | 16 |
| HerbFormulaManagementJourneyTests | 26 |
| BatchOperationsJourneyTests | 1 |
| CrossNarrativeValidationTests | 8 |
| **总计** | **146** |

---

## 备注

- 清单用于校对测试覆盖范围
- 每个测试方法对应一个 User Story 或场景
- 测试命名规范: `US_XXX_描述` 或 `功能_Full_Journey`

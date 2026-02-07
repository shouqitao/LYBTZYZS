# 测试设计方案 - P3 模块综合文档

## 1. 模块概述

P3 模块现有测试覆盖率相对较好，主要需要补充边界条件和特殊场景测试。

| 模块 | 现有测试 | 目标测试 | 新增 |
|------|----------|----------|------|
| LYBT.Module.MedicalCase.Tests | 32 | 45 | +13 |
| LYBT.Desktop.Shell.Tests | 136 | 150 | +14 |
| LYBT.Desktop.MedicalCase.Tests | 117 | 130 | +13 |
| LYBT.Desktop.Formula.Tests | 24 | 35 | +11 |
| LYBT.Desktop.Herbs.Tests | 18 | 30 | +12 |
| LYBT.Desktop.Models.Tests | 6 | 15 | +9 |
| LYBT.Shared.Utilities.Tests | 184 | 200 | +16 |
| LYBT.Shared.ExceptionHandling.Tests | 53 | 60 | +7 |
| LYBT.Shared.Configuration.Tests | 43 | 50 | +7 |
| LYBT.WebAPI.Tests | - | 20 | +20 |
| **总计** | **613** | **735** | **+122** |

---

## 2. LYBT.Module.MedicalCase.Tests (+13)

### 补充测试清单

```
MedicalCaseService 补充:
- CreateAsync_WithConsultation_ShouldCreateBoth
- UpdateAsync_WithPrescription_ShouldUpdateBoth
- CompleteAsync_WithIncompletePrescription_ShouldFail
- CancelAsync_ShouldSetCancelledStatus
- GetByPatientIdAsync_ShouldReturnAllCases

MedicalCaseRepository 补充:
- GetWithDetailsAsync_ShouldIncludeAllRelations
- QueryAsync_WithDateRange_ShouldFilter
- GetPendingCasesAsync_ShouldReturnDraftAndActive

审计日志:
- LogAuditEventAsync_ShouldRecordAllFields
- GetAuditLogsAsync_ShouldReturnPagedResult
```

---

## 3. LYBT.Desktop.Shell.Tests (+14)

### 补充测试清单

```
ShellViewModel 补充:
- NavigateToModule_ShouldLoadModule
- NavigateToModule_WithInvalidModule_ShouldShowError
- Logout_ShouldClearStateAndNavigate
- SessionTimeout_ShouldTriggerLogout

模块加载:
- LoadModulesAsync_ShouldLoadByRole
- LoadModulesAsync_AdminRole_ShouldLoadAllModules
- LoadModulesAsync_DoctorRole_ShouldLoadDoctorModules

状态管理:
- CurrentUser_ShouldUpdateOnLogin
- ApiStatus_ShouldReflectConnectionState
- Notifications_ShouldShowAndHide
```

---

## 4. LYBT.Desktop.MedicalCase.Tests (+13)

### 补充测试清单

```
MedicalCaseListViewModel 补充:
- LoadListAsync_WithFilters_ShouldFilter
- SearchAsync_WithKeyword_ShouldSearch
- DeleteAsync_WithReferences_ShouldShowWarning

MedicalCaseDetailViewModel 补充:
- SaveAsync_WithValidation_ShouldValidate
- SaveAsync_WithInvalidData_ShouldShowErrors
- AddPrescription_ShouldCreatePrescription
- CompleteCaseAsync_ShouldSetStatus

处方管理:
- AddPrescriptionItem_ShouldAddToList
- RemovePrescriptionItem_ShouldRemove
- CalculateTotal_ShouldSumItems
```

---

## 5. LYBT.Desktop.Formula.Tests (+11)

### 补充测试清单

```
FormulaListViewModel 补充:
- LoadListAsync_WithTypeFilter_ShouldFilter
- SearchAsync_ByHerbName_ShouldSearch
- CloneFormula_ShouldCreateCopy

FormulaDetailViewModel 补充:
- AddHerbItem_ShouldAddToList
- RemoveHerbItem_ShouldRemove
- ValidateHerbs_ShouldCheckAllItems
- SaveAsync_WithUnmatchedHerbs_ShouldSetDraft
```

---

## 6. LYBT.Desktop.Herbs.Tests (+12)

### 补充测试清单

```
HerbListViewModel 补充:
- LoadListAsync_WithCategoryFilter_ShouldFilter
- SearchAsync_ByPinyin_ShouldSearch
- ToggleStatus_ShouldToggle

HerbDetailViewModel 补充:
- SaveAsync_WithDuplicateName_ShouldShowError
- PriceCalculation_ShouldCalculateCorrectly

批量操作:
- BatchImport_ShouldImportAll
- BatchDelete_ShouldDeleteAll
- ExportToExcel_ShouldExport
```

---

## 7. LYBT.Desktop.Models.Tests (+9)

### 补充测试清单

```
DisplayModel 测试:
- PatientDisplayModel_AgeCalculation_ShouldBeCorrect
- MedicalCaseDisplayModel_StatusDisplay_ShouldBeCorrect
- FormulaDisplayModel_HerbsCount_ShouldBeCorrect

InputModel 测试:
- PatientInputModel_Validation_ShouldValidate
- HerbInputModel_PriceValidation_ShouldValidate
- FormulaInputModel_HerbsValidation_ShouldValidate
```

---

## 8. LYBT.Shared.Utilities.Tests (+16)

### 补充测试清单

```
PinYinHelper 补充:
- GetPinYin_WithSpecialCharacters_ShouldHandle
- GetPinYin_WithNumbers_ShouldPreserve
- GetFirstLetter_ShouldReturnFirstLetters

DateTimeHelper 补充:
- CalculateAge_WithFutureBirthDate_ShouldReturnZero
- CalculateAge_WithToday_ShouldReturnZero
- FormatDateTime_WithNull_ShouldReturnEmpty

StringHelper 补充:
- Truncate_WithNullString_ShouldReturnEmpty
- MaskPhoneNumber_ShouldMaskMiddle
- MaskIdNumber_ShouldMaskMiddle
```

---

## 9. LYBT.Shared.ExceptionHandling.Tests (+7)

### 补充测试清单

```
ExceptionHandler 补充:
- HandleAsync_WithNestedExceptions_ShouldUnwrap
- HandleAsync_ShouldLogException

ValidationException 补充:
- Create_WithMultipleErrors_ShouldContainAll
- ToValidationResult_ShouldConvert

BusinessException 补充:
- Create_WithErrorCode_ShouldSetCode
```

---

## 10. LYBT.Shared.Configuration.Tests (+7)

### 补充测试清单

```
AppSettings 补充:
- Load_WithMissingFile_ShouldUseDefaults
- Load_WithInvalidJson_ShouldThrow

JwtOptions 补充:
- Validate_WithWeakSecret_ShouldFail
- Validate_WithValidOptions_ShouldPass

ConnectionStrings 补充:
- GetConnectionString_WithEnvironment_ShouldResolve
```

---

## 11. LYBT.WebAPI.Tests (+20)

### 新增测试清单

```
Controller 基础测试:
- HealthController_ShouldReturnHealthy
- AuthController_Login_ShouldReturnToken
- AuthController_Logout_ShouldRevokeToken

中间件测试:
- ExceptionMiddleware_ShouldHandleExceptions
- AuthorizationMiddleware_ShouldValidateToken

API 响应格式:
- ApiResponse_Success_ShouldHaveCorrectFormat
- ApiResponse_Failure_ShouldHaveErrorDetails
```

---

## 12. 执行优先级

| 优先级 | 模块 | 预估时间 |
|--------|------|----------|
| 1 | LYBT.Shared.Utilities.Tests | 1.5h |
| 2 | LYBT.Module.MedicalCase.Tests | 1.5h |
| 3 | LYBT.Desktop.MedicalCase.Tests | 1.5h |
| 4 | LYBT.Desktop.Shell.Tests | 1.5h |
| 5 | LYBT.Desktop.Formula.Tests | 1h |
| 6 | LYBT.Desktop.Herbs.Tests | 1h |
| 7 | LYBT.Shared.ExceptionHandling.Tests | 0.5h |
| 8 | LYBT.Shared.Configuration.Tests | 0.5h |
| 9 | LYBT.Desktop.Models.Tests | 0.5h |
| 10 | LYBT.WebAPI.Tests | 2h |
| **总计** | | **~12h** |

---

*文档版本: v1.0*
*创建日期: 2026-02-05*
*待代码实现*

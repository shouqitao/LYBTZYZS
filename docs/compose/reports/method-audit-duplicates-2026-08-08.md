# A-30-S0 方法级统计基线 — 重复聚类初筛

> 生成：Mimo Code（S0 只读审查）｜任务书：`task-a30-s0-method-baseline-2026-08-08.md`
> 基线 commit：`7edf020f5`｜生成时间：2026-08-09 02:10
> 性质：**机器初筛**——仅按方法名 group by 聚类；**真重复/同名不同职/接口实现 的判定留给 S1-S4 符号级复核**

## 0. 聚类口径

| 项 | 说明 |
|----|------|
| 聚类键 | 方法名（全仓业务代码 `proj` 非 Tests）|
| 范围 | 同名方法定义数 ≥2 的组；**排除 Tests 项目**（测试方法名重复是正常模式）|
| 初判口径 | 同名同参数类型列表（`paramList` 相同）→ 标注"同签名候选"；同名不同参数 → "同名不同职候选"；`override/virtual/abstract` → "接口/继承实现候选"（需符号级确认）|
| 红线 | 本报告**不判定**应合并/应删除（S1-S4 工作）；初判仅降低人工复核搜索范围 |

## 1. 总览

- 同名方法组（≥2 定义）：**693** 组
- 涉及方法定义总数：**2842**
- 跨项目同名组（任务书点名类，如 GetByIdAsync/MapToDto）：**408** 组

## 2. 高重复度组 TOP 60（按定义数降序）

| 方法名 | 定义数 | 跨项目数 | 涉及项目 | 初判 |
|--------|-------|---------|---------|------|
| `Handle` | 60 | 8 | LYBT.Infrastructure, LYBT.LocalWebAPI, LYBT.Module.Auth, LYBT.Module.Formula, LY… | 跨项目同名 |
| `GetByIdAsync` | 46 | 14 | LYBT.Desktop.Contracts, LYBT.Desktop.Formula, LYBT.Desktop.Foundation, LYBT.Desk… | 跨项目同名 + 同签名组 + override×10 |
| `Dispose` | 45 | 10 | LYBT.Desktop.Clinical, LYBT.Desktop.Formula, LYBT.Desktop.Foundation, LYBT.Deskt… | 跨项目同名 + 同签名组 + override×14 |
| `GetPagedAsync` | 43 | 13 | LYBT.Desktop.Contracts, LYBT.Desktop.Formula, LYBT.Desktop.Foundation, LYBT.Desk… | 跨项目同名 + 同签名组 + override×3 |
| `BatchDeleteAsync` | 38 | 8 | LYBT.Desktop.Contracts, LYBT.Desktop.Formula, LYBT.Desktop.Foundation, LYBT.Desk… | 跨项目同名 + 同签名组 |
| `ToggleStatusAsync` | 33 | 6 | LYBT.Desktop.Contracts, LYBT.Desktop.Formula, LYBT.Desktop.Foundation, LYBT.Desk… | 跨项目同名 + 同签名组 + override×1 |
| `RestoreAsync` | 26 | 7 | LYBT.Desktop.Contracts, LYBT.Desktop.Formula, LYBT.Desktop.Foundation, LYBT.Desk… | 跨项目同名 + 同签名组 |
| `ExportTemplateAsync` | 24 | 5 | LYBT.Desktop.Contracts, LYBT.Desktop.Formula, LYBT.Desktop.Foundation, LYBT.Desk… | 跨项目同名 + 同签名组 |
| `BatchImportAsync` | 24 | 5 | LYBT.Desktop.Contracts, LYBT.Desktop.Formula, LYBT.Desktop.Foundation, LYBT.Desk… | 跨项目同名 + 同签名组 |
| `UpdateAsync` | 24 | 11 | LYBT.Desktop.Contracts, LYBT.Desktop.Foundation, LYBT.Desktop.MedicalCase, LYBT.… | 跨项目同名 + 同签名组 + override×10 |
| `Validate` | 19 | 9 | LYBT.Desktop.Controls, LYBT.Desktop.Formula, LYBT.Desktop.Herbs, LYBT.Desktop.Me… | 跨项目同名 + 同签名组 |
| `SearchAsync` | 18 | 8 | LYBT.Desktop.Clinical, LYBT.Desktop.Contracts, LYBT.Desktop.Formula, LYBT.Deskto… | 跨项目同名 + 同签名组 |
| `Create` | 17 | 4 | LYBT.Entities, LYBT.LocalWebAPI, LYBT.Module.Users, LYBT.WebAPI | 跨项目同名 + 同签名组 |
| `Reset` | 17 | 8 | LYBT.Desktop.Contracts, LYBT.Desktop.Formula, LYBT.Desktop.Foundation, LYBT.Desk… | 跨项目同名 + 同签名组 |
| `DeleteAsync` | 16 | 6 | LYBT.Desktop.Contracts, LYBT.Desktop.Foundation, LYBT.Desktop.Infrastructure, LY… | 跨项目同名 + 同签名组 + override×4 |
| `CreateAsync` | 16 | 4 | LYBT.Desktop.Contracts, LYBT.Desktop.Foundation, LYBT.Desktop.MedicalCase, LYBT.… | 跨项目同名 + 同签名组 + override×1 |
| `Configure` | 15 | 1 | LYBT.Infrastructure | 同项目同名 + override×8 |
| `RegisterTypes` | 14 | 12 | LYBT.Desktop.Admin, LYBT.Desktop.Auth, LYBT.Desktop.Clinical, LYBT.Desktop.Formu… | 跨项目同名 + 同签名组 + override×1 |
| `OnInitialized` | 14 | 12 | LYBT.Desktop.Admin, LYBT.Desktop.Auth, LYBT.Desktop.Clinical, LYBT.Desktop.Formu… | 跨项目同名 + 同签名组 + override×1 |
| `BatchEnableAsync` | 13 | 3 | LYBT.Desktop.Contracts, LYBT.Desktop.Foundation, LYBT.Desktop.Infrastructure | 跨项目同名 + 同签名组 |
| `BatchDisableAsync` | 13 | 3 | LYBT.Desktop.Contracts, LYBT.Desktop.Foundation, LYBT.Desktop.Infrastructure | 跨项目同名 + 同签名组 |
| `InitializeAsync` | 13 | 6 | LYBT.Desktop.Admin, LYBT.Desktop.Clinical, LYBT.Desktop.Contracts, LYBT.Desktop.… | 跨项目同名 + 同签名组 + override×7 |
| `GetPendingCasesAsync` | 13 | 4 | LYBT.Desktop.Contracts, LYBT.Desktop.Foundation, LYBT.Desktop.MedicalCase, LYBT.… | 跨项目同名 + 同签名组 + override×2 |
| `SaveAsync` | 13 | 6 | LYBT.Desktop.Admin, LYBT.Desktop.Contracts, LYBT.Desktop.Foundation, LYBT.Deskto… | 跨项目同名 + 同签名组 + override×2 |
| `OnNavigatedTo` | 13 | 6 | LYBT.Desktop.Admin, LYBT.Desktop.Auth, LYBT.Desktop.Clinical, LYBT.Desktop.Infra… | 跨项目同名 + 同签名组 + override×12 |
| `ConvertBack` | 12 | 1 | LYBT.Desktop.Controls | 同项目同名 + 同签名组 |
| `LogoutAsync` | 12 | 4 | LYBT.Desktop.Contracts, LYBT.Desktop.Foundation, LYBT.Desktop.Shell, LYBT.WebAPI | 跨项目同名 + 同签名组 |
| `Convert` | 12 | 1 | LYBT.Desktop.Controls | 同项目同名 + 同签名组 |
| `Failure` | 11 | 2 | LYBT.Desktop.Infrastructure, LYBT.Shared.Models | 跨项目同名 + 同签名组 |
| `GetById` | 11 | 5 | LYBT.Infrastructure, LYBT.LocalWebAPI, LYBT.Module.Registration, LYBT.Module.Use… | 跨项目同名 + 同签名组 + override×11 |
| `Clear` | 11 | 5 | LYBT.Desktop.Contracts, LYBT.Desktop.Controls, LYBT.Desktop.Infrastructure, LYBT… | 跨项目同名 + 同签名组 |
| `CancelAsync` | 11 | 5 | LYBT.Desktop.Contracts, LYBT.Desktop.Foundation, LYBT.Desktop.Infrastructure, LY… | 跨项目同名 + 同签名组 |
| `ExecuteAsync` | 11 | 4 | LYBT.Desktop.Contracts, LYBT.Desktop.Foundation, LYBT.Desktop.Shell, LYBT.Infras… | 跨项目同名 + 同签名组 + override×1 |
| `BatchDelete` | 11 | 5 | LYBT.Infrastructure, LYBT.LocalWebAPI, LYBT.Module.Registration, LYBT.Module.Use… | 跨项目同名 + 同签名组 + override×11 |
| `Restore` | 11 | 5 | LYBT.Entities, LYBT.Infrastructure, LYBT.LocalWebAPI, LYBT.Module.Users, LYBT.We… | 跨项目同名 + 同签名组 + override×8 |
| `Delete` | 11 | 5 | LYBT.Infrastructure, LYBT.LocalWebAPI, LYBT.Module.Registration, LYBT.Module.Use… | 跨项目同名 + 同签名组 + override×11 |
| `GetList` | 11 | 5 | LYBT.Infrastructure, LYBT.LocalWebAPI, LYBT.Module.Registration, LYBT.Module.Use… | 跨项目同名 + 同签名组 + override×11 |
| `ResetPasswordAsync` | 11 | 3 | LYBT.Desktop.Contracts, LYBT.Desktop.Foundation, LYBT.Desktop.Users | 跨项目同名 + 同签名组 |
| `Success` | 10 | 4 | LYBT.Desktop.Infrastructure, LYBT.Desktop.MedicalCase, LYBT.Infrastructure, LYBT… | 跨项目同名 + 同签名组 |
| `CreateNew` | 10 | 5 | LYBT.Desktop.Formula, LYBT.Desktop.Herbs, LYBT.Desktop.Infrastructure, LYBT.Desk… | 跨项目同名 + 同签名组 |
| `SetPrescriptionFlagAsync` | 10 | 4 | LYBT.Desktop.Contracts, LYBT.Desktop.Foundation, LYBT.Desktop.MedicalCase, LYBT.… | 跨项目同名 + 同签名组 + override×1 |
| `ShowErrorAsync` | 10 | 3 | LYBT.Desktop.Contracts, LYBT.Desktop.Infrastructure, LYBT.Desktop.MedicalCase | 跨项目同名 + 同签名组 |
| `Update` | 10 | 4 | LYBT.LocalWebAPI, LYBT.Module.Registration, LYBT.Module.Users, LYBT.WebAPI | 跨项目同名 + 同签名组 |
| `Clone` | 10 | 6 | LYBT.Desktop.Formula, LYBT.Desktop.Herbs, LYBT.Desktop.MedicalCase, LYBT.Desktop… | 跨项目同名 + 同签名组 |
| `OnNavigatedFrom` | 10 | 5 | LYBT.Desktop.Admin, LYBT.Desktop.Clinical, LYBT.Desktop.Infrastructure, LYBT.Des… | 跨项目同名 + 同签名组 + override×9 |
| `GetAuditLogsAsync` | 10 | 4 | LYBT.Desktop.Contracts, LYBT.Desktop.Foundation, LYBT.Desktop.MedicalCase, LYBT.… | 跨项目同名 + 同签名组 |
| `ShowConfirmAsync` | 10 | 3 | LYBT.Desktop.Contracts, LYBT.Desktop.Infrastructure, LYBT.Desktop.MedicalCase | 跨项目同名 + 同签名组 |
| `Confirm` | 9 | 5 | LYBT.Desktop.Auth, LYBT.Desktop.Infrastructure, LYBT.Desktop.MedicalCase, LYBT.D… | 跨项目同名 + 同签名组 + override×9 |
| `CloseCaseAsync` | 9 | 3 | LYBT.Desktop.Contracts, LYBT.Desktop.Foundation, LYBT.Desktop.MedicalCase | 跨项目同名 + 同签名组 + override×2 |
| `OnDialogOpenedCore` | 9 | 5 | LYBT.Desktop.Auth, LYBT.Desktop.Infrastructure, LYBT.Desktop.MedicalCase, LYBT.D… | 跨项目同名 + 同签名组 + override×9 |
| `ChangePasswordAsync` | 9 | 4 | LYBT.Desktop.Contracts, LYBT.Desktop.Foundation, LYBT.Desktop.Shell, LYBT.Deskto… | 跨项目同名 + 同签名组 |
| `IsNavigationTarget` | 9 | 5 | LYBT.Desktop.Admin, LYBT.Desktop.Clinical, LYBT.Desktop.Infrastructure, LYBT.Des… | 跨项目同名 + 同签名组 + override×8 |
| `StartVisitAsync` | 9 | 3 | LYBT.Desktop.Contracts, LYBT.Desktop.Foundation, LYBT.Desktop.Registrations | 跨项目同名 + 同签名组 |
| `RefreshTokenAsync` | 9 | 4 | LYBT.Desktop.Contracts, LYBT.Desktop.Foundation, LYBT.Desktop.Shell, LYBT.WebAPI | 跨项目同名 + 同签名组 |
| `LoginAsync` | 9 | 4 | LYBT.Desktop.Contracts, LYBT.Desktop.Foundation, LYBT.Desktop.Shell, LYBT.WebAPI | 跨项目同名 + 同签名组 |
| `UpdateStatusAsync` | 9 | 4 | LYBT.Desktop.Contracts, LYBT.Desktop.Foundation, LYBT.Desktop.MedicalCase, LYBT.… | 跨项目同名 + 同签名组 + override×1 |
| `OnModelCreating` | 8 | 8 | LYBT.Infrastructure, LYBT.Module.Auth, LYBT.Module.Formula, LYBT.Module.Herbs, L… | 跨项目同名 + 同签名组 + override×8 |
| `GetDailyIncomeAsync` | 8 | 4 | LYBT.Desktop.Contracts, LYBT.Desktop.Foundation, LYBT.Desktop.MedicalCase, LYBT.… | 跨项目同名 + 同签名组 |
| `GetDailyHerbUsageAsync` | 8 | 4 | LYBT.Desktop.Contracts, LYBT.Desktop.Foundation, LYBT.Desktop.MedicalCase, LYBT.… | 跨项目同名 + 同签名组 |
| `GetDailyConsultationsAsync` | 8 | 4 | LYBT.Desktop.Contracts, LYBT.Desktop.Foundation, LYBT.Desktop.MedicalCase, LYBT.… | 跨项目同名 + 同签名组 |

## 3. 全部同名组清单（693 组，按定义数降序）

| 方法名 | 定义数 | 跨项目数 | 同签名子组 | override 数 | 初判 |
|--------|-------|---------|-----------|------------|------|
| `Handle` | 60 | 8 | 0 | 0 | 跨项目 |
| `GetByIdAsync` | 46 | 14 | 2 | 10 | 跨项目/同签名/override |
| `Dispose` | 45 | 10 | 2 | 14 | 跨项目/同签名/override |
| `GetPagedAsync` | 43 | 13 | 6 | 3 | 跨项目/同签名/override |
| `BatchDeleteAsync` | 38 | 8 | 3 | 0 | 跨项目/同签名 |
| `ToggleStatusAsync` | 33 | 6 | 3 | 1 | 跨项目/同签名/override |
| `RestoreAsync` | 26 | 7 | 2 | 0 | 跨项目/同签名 |
| `ExportTemplateAsync` | 24 | 5 | 2 | 0 | 跨项目/同签名 |
| `BatchImportAsync` | 24 | 5 | 6 | 0 | 跨项目/同签名 |
| `UpdateAsync` | 24 | 11 | 5 | 10 | 跨项目/同签名/override |
| `Validate` | 19 | 9 | 2 | 0 | 跨项目/同签名 |
| `SearchAsync` | 18 | 8 | 3 | 0 | 跨项目/同签名 |
| `Create` | 17 | 4 | 5 | 0 | 跨项目/同签名 |
| `Reset` | 17 | 8 | 1 | 0 | 跨项目/同签名 |
| `DeleteAsync` | 16 | 6 | 3 | 4 | 跨项目/同签名/override |
| `CreateAsync` | 16 | 4 | 3 | 1 | 跨项目/同签名/override |
| `Configure` | 15 | 1 | 0 | 8 | 同项目/override |
| `RegisterTypes` | 14 | 12 | 1 | 1 | 跨项目/同签名/override |
| `OnInitialized` | 14 | 12 | 1 | 1 | 跨项目/同签名/override |
| `BatchEnableAsync` | 13 | 3 | 1 | 0 | 跨项目/同签名 |
| `BatchDisableAsync` | 13 | 3 | 1 | 0 | 跨项目/同签名 |
| `InitializeAsync` | 13 | 6 | 4 | 7 | 跨项目/同签名/override |
| `GetPendingCasesAsync` | 13 | 4 | 3 | 2 | 跨项目/同签名/override |
| `SaveAsync` | 13 | 6 | 5 | 2 | 跨项目/同签名/override |
| `OnNavigatedTo` | 13 | 6 | 2 | 12 | 跨项目/同签名/override |
| `ConvertBack` | 12 | 1 | 2 | 0 | 同项目/同签名 |
| `LogoutAsync` | 12 | 4 | 2 | 0 | 跨项目/同签名 |
| `Convert` | 12 | 1 | 2 | 0 | 同项目/同签名 |
| `Failure` | 11 | 2 | 5 | 0 | 跨项目/同签名 |
| `GetById` | 11 | 5 | 1 | 11 | 跨项目/同签名/override |
| `Clear` | 11 | 5 | 1 | 0 | 跨项目/同签名 |
| `CancelAsync` | 11 | 5 | 3 | 0 | 跨项目/同签名 |
| `ExecuteAsync` | 11 | 4 | 1 | 1 | 跨项目/同签名/override |
| `BatchDelete` | 11 | 5 | 1 | 11 | 跨项目/同签名/override |
| `Restore` | 11 | 5 | 2 | 8 | 跨项目/同签名/override |
| `Delete` | 11 | 5 | 1 | 11 | 跨项目/同签名/override |
| `GetList` | 11 | 5 | 1 | 11 | 跨项目/同签名/override |
| `ResetPasswordAsync` | 11 | 3 | 4 | 0 | 跨项目/同签名 |
| `Success` | 10 | 4 | 3 | 0 | 跨项目/同签名 |
| `CreateNew` | 10 | 5 | 2 | 0 | 跨项目/同签名 |
| `SetPrescriptionFlagAsync` | 10 | 4 | 3 | 1 | 跨项目/同签名/override |
| `ShowErrorAsync` | 10 | 3 | 2 | 0 | 跨项目/同签名 |
| `Update` | 10 | 4 | 4 | 0 | 跨项目/同签名 |
| `Clone` | 10 | 6 | 1 | 0 | 跨项目/同签名 |
| `OnNavigatedFrom` | 10 | 5 | 1 | 9 | 跨项目/同签名/override |
| `GetAuditLogsAsync` | 10 | 4 | 2 | 0 | 跨项目/同签名 |
| `ShowConfirmAsync` | 10 | 3 | 2 | 0 | 跨项目/同签名 |
| `Confirm` | 9 | 5 | 1 | 9 | 跨项目/同签名/override |
| `CloseCaseAsync` | 9 | 3 | 2 | 2 | 跨项目/同签名/override |
| `OnDialogOpenedCore` | 9 | 5 | 1 | 9 | 跨项目/同签名/override |
| `ChangePasswordAsync` | 9 | 4 | 3 | 0 | 跨项目/同签名 |
| `IsNavigationTarget` | 9 | 5 | 1 | 8 | 跨项目/同签名/override |
| `StartVisitAsync` | 9 | 3 | 2 | 0 | 跨项目/同签名 |
| `RefreshTokenAsync` | 9 | 4 | 2 | 0 | 跨项目/同签名 |
| `LoginAsync` | 9 | 4 | 2 | 0 | 跨项目/同签名 |
| `UpdateStatusAsync` | 9 | 4 | 3 | 1 | 跨项目/同签名/override |
| `OnModelCreating` | 8 | 8 | 1 | 8 | 跨项目/同签名/override |
| `GetDailyIncomeAsync` | 8 | 4 | 3 | 0 | 跨项目/同签名 |
| `GetDailyHerbUsageAsync` | 8 | 4 | 3 | 0 | 跨项目/同签名 |
| `GetDailyConsultationsAsync` | 8 | 4 | 3 | 0 | 跨项目/同签名 |
| `ToggleStatus` | 8 | 4 | 1 | 8 | 跨项目/同签名/override |
| `SetValueAsync` | 8 | 3 | 2 | 0 | 跨项目/同签名 |
| `ShowInfoAsync` | 8 | 2 | 1 | 0 | 跨项目/同签名 |
| `SuspendAsync` | 8 | 4 | 3 | 0 | 跨项目/同签名 |
| `ExportPatientsAsync` | 8 | 3 | 2 | 0 | 跨项目/同签名 |
| `ExportHerbsAsync` | 8 | 3 | 2 | 0 | 跨项目/同签名 |
| `ExportFormulasAsync` | 8 | 3 | 2 | 0 | 跨项目/同签名 |
| `ShowWarningAsync` | 8 | 2 | 1 | 0 | 跨项目/同签名 |
| `GetByIdIncludingDeletedAsync` | 8 | 4 | 1 | 0 | 跨项目/同签名 |
| `ShowSuccessAsync` | 8 | 3 | 2 | 0 | 跨项目/同签名 |
| `AddAsync` | 8 | 3 | 3 | 1 | 跨项目/同签名/override |
| `ValidateTokenAsync` | 8 | 2 | 2 | 0 | 跨项目/同签名 |
| `ChangeProfileAsync` | 8 | 3 | 2 | 0 | 跨项目/同签名 |
| `GetCurrentUserAsync` | 7 | 3 | 2 | 0 | 跨项目/同签名 |
| `CreateNewDetail` | 7 | 6 | 1 | 6 | 跨项目/同签名/override |
| `ToDto` | 7 | 3 | 1 | 0 | 跨项目/同签名 |
| `DeleteItemAsync` | 7 | 6 | 1 | 6 | 跨项目/同签名/override |
| `SaveDetailAsync` | 7 | 6 | 1 | 6 | 跨项目/同签名/override |
| `GetHttpStatusCode` | 7 | 1 | 1 | 7 | 同项目/同签名/override |
| `InvokeAsync` | 7 | 3 | 3 | 0 | 跨项目/同签名 |
| `LoadListAsync` | 7 | 6 | 1 | 6 | 跨项目/同签名/override |
| `ApplyOperationAsync` | 7 | 5 | 1 | 7 | 跨项目/同签名/override |
| `GetUnfinishedCaseByPatientIdAsync` | 7 | 3 | 2 | 2 | 跨项目/同签名/override |
| `HandleException` | 7 | 2 | 2 | 0 | 跨项目/同签名 |
| `AppException` | 7 | 1 | 0 | 0 | 同项目 |
| `CanConfirm` | 7 | 5 | 2 | 7 | 跨项目/同签名/override |
| `ReadCardAsync` | 7 | 2 | 3 | 0 | 跨项目/同签名 |
| `QueryAsync` | 7 | 3 | 1 | 2 | 跨项目/同签名/override |
| `CreatePatientAsync` | 6 | 3 | 2 | 0 | 跨项目/同签名 |
| `GetByIdNumberAsync` | 6 | 3 | 1 | 0 | 跨项目/同签名 |
| `GetCategoriesAsync` | 6 | 2 | 1 | 0 | 跨项目/同签名 |
| `CreateFormulaAsync` | 6 | 3 | 2 | 0 | 跨项目/同签名 |
| `UpdatePatientAsync` | 6 | 3 | 2 | 0 | 跨项目/同签名 |
| `GetConfigurationAsync` | 6 | 3 | 2 | 0 | 跨项目/同签名 |
| `CreateHerbAsync` | 6 | 3 | 2 | 0 | 跨项目/同签名 |
| `Failed` | 6 | 3 | 1 | 0 | 跨项目/同签名 |
| `CancelMedicalCaseAsync` | 6 | 3 | 2 | 0 | 跨项目/同签名 |
| `LoadDetailAsync` | 6 | 6 | 0 | 6 | 跨项目/override |
| `Cancel` | 6 | 6 | 2 | 5 | 跨项目/同签名/override |
| `UpdateHerbAsync` | 6 | 3 | 2 | 0 | 跨项目/同签名 |
| `UpdateUserAsync` | 6 | 3 | 2 | 0 | 跨项目/同签名 |
| `DeleteFormulaAsync` | 6 | 3 | 2 | 0 | 跨项目/同签名 |
| `ExistsByNameAsync` | 6 | 3 | 1 | 0 | 跨项目/同签名 |
| `DeleteHerbAsync` | 6 | 3 | 2 | 0 | 跨项目/同签名 |
| `SoftDelete` | 6 | 1 | 1 | 0 | 同项目/同签名 |
| `DeletePatientAsync` | 6 | 3 | 2 | 0 | 跨项目/同签名 |
| `LoginWithAutoTokenAsync` | 6 | 2 | 1 | 0 | 跨项目/同签名 |
| `DeleteUserAsync` | 6 | 3 | 2 | 0 | 跨项目/同签名 |
| `CreateUserAsync` | 6 | 3 | 2 | 0 | 跨项目/同签名 |
| `RefreshAsync` | 6 | 3 | 2 | 0 | 跨项目/同签名 |
| `GetDetailDisplayName` | 6 | 6 | 1 | 6 | 跨项目/同签名/override |
| `CloneFormulaAsync` | 6 | 3 | 2 | 0 | 跨项目/同签名 |
| `GetValueAsync` | 6 | 3 | 2 | 0 | 跨项目/同签名 |
| `RecordPrintAsync` | 6 | 3 | 2 | 0 | 跨项目/同签名 |
| `GetQueueAsync` | 6 | 3 | 2 | 0 | 跨项目/同签名 |
| `GetPermissionsAsync` | 6 | 3 | 2 | 0 | 跨项目/同签名 |
| `NavigateTo` | 6 | 3 | 2 | 1 | 跨项目/同签名/override |
| `RestoreItemAsync` | 6 | 5 | 1 | 5 | 跨项目/同签名/override |
| `HealthCheckAsync` | 6 | 3 | 2 | 0 | 跨项目/同签名 |
| `ToListDto` | 6 | 6 | 0 | 0 | 跨项目 |
| `SaveChangesAsync` | 6 | 3 | 1 | 2 | 跨项目/同签名/override |
| `ToInputDto` | 6 | 3 | 1 | 0 | 跨项目/同签名 |
| `InitializeForNewCase` | 6 | 5 | 1 | 0 | 跨项目/同签名 |
| `UpdateConfigurationAsync` | 6 | 3 | 2 | 0 | 跨项目/同签名 |
| `GetListAsync` | 6 | 3 | 2 | 0 | 跨项目/同签名 |
| `DisconnectAsync` | 6 | 2 | 1 | 0 | 跨项目/同签名 |
| `SearchMedicalCasesAsync` | 6 | 3 | 2 | 0 | 跨项目/同签名 |
| `GetFormulaByIdAsync` | 6 | 3 | 1 | 0 | 跨项目/同签名 |
| `UpdateFormulaAsync` | 6 | 3 | 2 | 0 | 跨项目/同签名 |
| `InvalidateCachesAsync` | 6 | 5 | 1 | 5 | 跨项目/同签名/override |
| `GetDiagnostics` | 6 | 2 | 1 | 0 | 跨项目/同签名 |
| `ToDetailDto` | 6 | 6 | 0 | 0 | 跨项目 |
| `InitializeFromDto` | 6 | 5 | 0 | 0 | 跨项目 |
| `BusinessException` | 5 | 1 | 0 | 0 | 同项目 |
| `ValidationException` | 5 | 1 | 0 | 0 | 同项目 |
| `BatchEnable` | 5 | 3 | 1 | 1 | 跨项目/同签名/override |
| `BatchImport` | 5 | 2 | 2 | 0 | 跨项目/同签名 |
| `ExecuteRestoreAsync` | 5 | 5 | 1 | 5 | 跨项目/同签名/override |
| `StartAsync` | 5 | 4 | 2 | 0 | 跨项目/同签名 |
| `GetPrescriptionData` | 5 | 1 | 1 | 0 | 同项目/同签名 |
| `GetCorrelationId` | 5 | 3 | 2 | 0 | 跨项目/同签名 |
| `GetConsultationData` | 5 | 1 | 2 | 0 | 同项目/同签名 |
| `RestartAsync` | 5 | 3 | 1 | 0 | 跨项目/同签名 |
| `GetEntityId` | 5 | 5 | 0 | 5 | 跨项目/override |
| `ValidateAsync` | 5 | 3 | 1 | 5 | 跨项目/同签名/override |
| `Initialize` | 5 | 4 | 1 | 0 | 跨项目/同签名 |
| `StopAsync` | 5 | 4 | 2 | 0 | 跨项目/同签名 |
| `GenerateToken` | 5 | 2 | 2 | 0 | 跨项目/同签名 |
| `ApiException` | 5 | 1 | 0 | 0 | 同项目 |
| `GetEntityDisplayName` | 5 | 5 | 0 | 5 | 跨项目/override |
| `BatchDisable` | 5 | 3 | 1 | 1 | 跨项目/同签名/override |
| `UploadAsync` | 5 | 3 | 1 | 0 | 跨项目/同签名 |
| `Subscribe` | 5 | 1 | 0 | 0 | 同项目 |
| `DeleteMedicalCaseAsync` | 5 | 3 | 1 | 1 | 跨项目/同签名/override |
| `NavigateToHome` | 5 | 2 | 2 | 1 | 跨项目/同签名/override |
| `Succeeded` | 5 | 2 | 1 | 0 | 跨项目/同签名 |
| `OnDisposing` | 5 | 3 | 1 | 5 | 跨项目/同签名/override |
| `MaskIdNumber` | 5 | 3 | 1 | 0 | 跨项目/同签名 |
| `GetDisabledHerbIdsAsync` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `GetEntityName` | 4 | 2 | 1 | 4 | 跨项目/同签名/override |
| `ConflictException` | 4 | 1 | 0 | 0 | 同项目 |
| `GetDoctorsAsync` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `GetDoctorPerformanceAsync` | 4 | 1 | 1 | 0 | 同项目/同签名 |
| `ChangeStatus` | 4 | 1 | 1 | 0 | 同项目/同签名 |
| `CanRestore` | 4 | 2 | 2 | 0 | 跨项目/同签名 |
| `SendAsync` | 4 | 1 | 1 | 3 | 同项目/同签名/override |
| `DisableDebugModeAsync` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `SetLoggingLevelAsync` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `MarkAsChanged` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `UpdateLoginFailureAsync` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `GetByUsernameAsync` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `UnauthorizedException` | 4 | 1 | 0 | 0 | 同项目 |
| `GetToken` | 4 | 1 | 1 | 0 | 同项目/同签名 |
| `HandleExceptionAsync` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `CreateMedicalCaseAsync` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `UpdateProfile` | 4 | 1 | 0 | 0 | 同项目 |
| `OnAllHerbsChanged` | 4 | 1 | 2 | 0 | 同项目/同签名 |
| `CheckReference` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `GetUserBasicInfoAsync` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `GetUserByIdAsync` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `GetUserByUsernameAsync` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `HandleResult` | 4 | 1 | 0 | 0 | 同项目 |
| `ComputeTokenHash` | 4 | 1 | 1 | 0 | 同项目/同签名 |
| `InvalidateAsync` | 4 | 1 | 2 | 0 | 同项目/同签名 |
| `ShowError` | 4 | 2 | 2 | 0 | 跨项目/同签名 |
| `GetPendingValidationAsync` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `ValidateProductionAsync` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `HasPermission` | 4 | 2 | 2 | 0 | 跨项目/同签名 |
| `GetPatientsAsync` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `ValidateHerbAsync` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `RevokeAllUserSessionsAsync` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `SafeExecuteAsync` | 4 | 1 | 2 | 0 | 同项目/同签名 |
| `GetPatientByIdAsync` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `GetPatientBasicInfoAsync` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `ClearHistory` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `ToItemCore` | 4 | 2 | 0 | 0 | 跨项目 |
| `QueryMedicalCasesAsync` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `ToItem` | 4 | 2 | 0 | 0 | 跨项目 |
| `ResetLoginStateAsync` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `GetMedicalCasesAsync` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `GetMedicalCaseByIdAsync` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `GetLoggingStatusAsync` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `ResetAll` | 4 | 1 | 1 | 0 | 同项目/同签名 |
| `BatchCheckReference` | 4 | 2 | 2 | 0 | 跨项目/同签名 |
| `OnDialogClosedCore` | 4 | 2 | 1 | 4 | 跨项目/同签名/override |
| `GetHerbsAsync` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `GetHerbRankingAsync` | 4 | 1 | 1 | 0 | 同项目/同签名 |
| `GetHerbPricesAsync` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `GetHerbByIdAsync` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `GetHerbBasicInfoAsync` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `GetFormulasAsync` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `BuildMessage` | 4 | 4 | 1 | 4 | 跨项目/同签名/override |
| `Invoke` | 4 | 2 | 2 | 0 | 跨项目/同签名 |
| `GetPermittedEvents` | 4 | 3 | 1 | 0 | 跨项目/同签名 |
| `StopMonitoring` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `ForceCheckAsync` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `OnIsLoadingChanged` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `ExecuteWithLoadingAsync` | 4 | 1 | 2 | 0 | 同项目/同签名 |
| `GetWaitingQueueAsync` | 4 | 3 | 1 | 0 | 跨项目/同签名 |
| `AddPrintLogAsync` | 4 | 1 | 2 | 0 | 同项目/同签名 |
| `StartMonitoring` | 4 | 2 | 2 | 0 | 跨项目/同签名 |
| `NotFoundException` | 4 | 1 | 0 | 0 | 同项目 |
| `Fire` | 4 | 3 | 2 | 0 | 跨项目/同签名 |
| `OnNavigatedToAsync` | 4 | 4 | 1 | 3 | 跨项目/同签名/override |
| `ExecuteSafelyAsync` | 4 | 1 | 2 | 0 | 同项目/同签名 |
| `FinalizeResult` | 4 | 4 | 1 | 4 | 跨项目/同签名/override |
| `ShowWarning` | 4 | 2 | 2 | 0 | 跨项目/同签名 |
| `VerifyPasswordAsync` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `CanFire` | 4 | 3 | 2 | 0 | 跨项目/同签名 |
| `CancelEdit` | 4 | 2 | 1 | 1 | 跨项目/同签名/override |
| `LoadFromDto` | 4 | 1 | 2 | 0 | 同项目/同签名 |
| `StartVisit` | 4 | 4 | 1 | 3 | 跨项目/同签名/override |
| `ShowSuccess` | 4 | 2 | 2 | 0 | 跨项目/同签名 |
| `ExecuteWithRetryAsync` | 4 | 1 | 2 | 0 | 同项目/同签名 |
| `EnableDebugModeAsync` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `Register` | 4 | 2 | 2 | 0 | 跨项目/同签名 |
| `GetAllAsync` | 4 | 3 | 1 | 0 | 跨项目/同签名 |
| `GetAllActiveHerbsAsync` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `GenerateSecurePassword` | 4 | 1 | 0 | 0 | 同项目 |
| `UpdateState` | 4 | 3 | 0 | 0 | 跨项目 |
| `ShowInfo` | 4 | 2 | 2 | 0 | 跨项目/同签名 |
| `ShowInputAsync` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `GetAllPendingCasesAsync` | 4 | 1 | 1 | 0 | 同项目/同签名 |
| `GetUsersAsync` | 4 | 2 | 1 | 0 | 跨项目/同签名 |
| `GetHomeViewName` | 3 | 2 | 1 | 1 | 跨项目/同签名/override |
| `ExecuteToggleStatusAsync` | 3 | 3 | 1 | 3 | 跨项目/同签名/override |
| `DeleteRegistrationAsync` | 3 | 2 | 1 | 0 | 跨项目/同签名 |
| `ResetActivity` | 3 | 2 | 1 | 0 | 跨项目/同签名 |
| `QuickVisitAsync` | 3 | 2 | 1 | 0 | 跨项目/同签名 |
| `ValidateProperty` | 3 | 1 | 1 | 1 | 同项目/同签名/override |
| `CanToggleUserStatus` | 3 | 1 | 1 | 0 | 同项目/同签名 |
| `StopAutoRead` | 3 | 2 | 1 | 0 | 跨项目/同签名 |
| `ToEntity` | 3 | 3 | 0 | 0 | 跨项目 |
| `Result` | 3 | 1 | 0 | 0 | 同项目 |
| `EnableDebugMode` | 3 | 3 | 1 | 0 | 跨项目/同签名 |
| `StartAutoRead` | 3 | 2 | 1 | 0 | 跨项目/同签名 |
| `DisableDebugMode` | 3 | 3 | 1 | 0 | 跨项目/同签名 |
| `NavigateToPatientManagement` | 3 | 2 | 1 | 0 | 跨项目/同签名 |
| `DetectCardAsync` | 3 | 1 | 1 | 0 | 同项目/同签名 |
| `ToggleUserStatusAsync` | 3 | 1 | 1 | 0 | 同项目/同签名 |
| `ToInputDtoCore` | 3 | 2 | 0 | 0 | 跨项目 |
| `OnConnectionStateChanged` | 3 | 2 | 1 | 0 | 跨项目/同签名 |
| `ValidateToken` | 3 | 2 | 1 | 0 | 跨项目/同签名 |
| `QuickVisit` | 3 | 3 | 1 | 3 | 跨项目/同签名/override |
| `ClearError` | 3 | 1 | 1 | 0 | 同项目/同签名 |
| `Save` | 3 | 2 | 1 | 0 | 跨项目/同签名 |
| `EnterEditMode` | 3 | 2 | 1 | 0 | 跨项目/同签名 |
| `GoBack` | 3 | 3 | 1 | 0 | 跨项目/同签名 |
| `InitializeDatabaseAsync` | 3 | 3 | 0 | 0 | 跨项目 |
| `StartSessionAsync` | 3 | 1 | 1 | 0 | 同项目/同签名 |
| `SearchPatientsAsync` | 3 | 3 | 1 | 0 | 跨项目/同签名 |
| `ShowWarningMessageAsync` | 3 | 2 | 1 | 2 | 跨项目/同签名/override |
| `SetError` | 3 | 1 | 1 | 0 | 同项目/同签名 |
| `GetCurrentUserId` | 3 | 2 | 1 | 0 | 跨项目/同签名 |
| `GetCurrentUser` | 3 | 2 | 1 | 1 | 跨项目/同签名/override |
| `AddHerb` | 3 | 2 | 0 | 0 | 跨项目 |
| `ReloadAsync` | 3 | 2 | 1 | 1 | 跨项目/同签名/override |
| `OnNavigatedToCore` | 3 | 2 | 1 | 3 | 跨项目/同签名/override |
| `GetRegistrationsAsync` | 3 | 2 | 1 | 0 | 跨项目/同签名 |
| `ClearCache` | 3 | 1 | 1 | 0 | 同项目/同签名 |
| `CheckHealthAsync` | 3 | 2 | 1 | 0 | 跨项目/同签名 |
| `ValidateAll` | 3 | 1 | 1 | 1 | 同项目/同签名/override |
| `Show` | 3 | 3 | 1 | 0 | 跨项目/同签名 |
| `OnSelectedPatientChanged` | 3 | 2 | 1 | 0 | 跨项目/同签名 |
| `RefreshToken` | 3 | 2 | 1 | 0 | 跨项目/同签名 |
| `GetUserFriendlyMessage` | 3 | 2 | 1 | 0 | 跨项目/同签名 |
| `CreateClient` | 3 | 2 | 1 | 0 | 跨项目/同签名 |
| `ShowSuccessMessageAsync` | 3 | 2 | 1 | 2 | 跨项目/同签名/override |
| `SetDefaultPrinter` | 3 | 1 | 1 | 0 | 同项目/同签名 |
| `CopyFormulaAsync` | 3 | 2 | 1 | 0 | 跨项目/同签名 |
| `OnIsBusyChangedCore` | 3 | 2 | 1 | 3 | 跨项目/同签名/override |
| `UpdateStatus` | 3 | 3 | 1 | 0 | 跨项目/同签名 |
| `ToDtoCore` | 3 | 2 | 0 | 0 | 跨项目 |
| `GetAvailablePrinters` | 3 | 1 | 1 | 0 | 同项目/同签名 |
| `GetEntityStatus` | 3 | 3 | 0 | 3 | 跨项目/override |
| `CreateSuccess` | 3 | 2 | 0 | 0 | 跨项目 |
| `CanAddHerb` | 3 | 2 | 1 | 0 | 跨项目/同签名 |
| `SetBusy` | 3 | 3 | 1 | 0 | 跨项目/同签名 |
| `FindOrCreatePatientAsync` | 3 | 2 | 1 | 0 | 跨项目/同签名 |
| `ConnectAsync` | 3 | 1 | 1 | 0 | 同项目/同签名 |
| `FindPatientByIdNumberAsync` | 3 | 2 | 1 | 0 | 跨项目/同签名 |
| `CanResetPassword` | 3 | 1 | 1 | 0 | 同项目/同签名 |
| `GetDefaultPrinter` | 3 | 1 | 1 | 0 | 同项目/同签名 |
| `ShowErrorMessageAsync` | 3 | 2 | 1 | 2 | 跨项目/同签名/override |
| `RecordPrint` | 2 | 2 | 1 | 2 | 跨项目/同签名/override |
| `OnCardReadError` | 2 | 2 | 0 | 0 | 跨项目 |
| `Read` | 2 | 2 | 1 | 2 | 跨项目/同签名/override |
| `OnConnectionModeChanged` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `RecordSecurityAuditAsync` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `OnBatchCompletedAsync` | 2 | 2 | 1 | 2 | 跨项目/同签名/override |
| `RecordEventAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `RecordNavigation` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `RecordMemoryBaseline` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `PreloadModulesAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `QueryPagedAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `OnItemChanged` | 2 | 1 | 0 | 0 | 同项目 |
| `OnItemDeletedAsync` | 2 | 1 | 1 | 1 | 同项目/同签名/override |
| `ValidationFail` | 2 | 1 | 0 | 0 | 同项目 |
| `OnNavigatedFromCore` | 2 | 2 | 1 | 2 | 跨项目/同签名/override |
| `VerifyIntegrityAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `OnPasswordChanged` | 2 | 2 | 0 | 0 | 跨项目 |
| `OnSearchTextChanged` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `OnSelectedHerbChanged` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `OnSessionExpired` | 2 | 2 | 0 | 0 | 跨项目 |
| `OnTestStatusChanged` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `OnTick` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `OnTokenLifecycleStateChanged` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `PagedResult` | 2 | 1 | 0 | 0 | 同项目 |
| `PatientRepository` | 2 | 2 | 0 | 0 | 跨项目 |
| `PatientsController` | 2 | 2 | 0 | 0 | 跨项目 |
| `PatientSelectionControl_PatientDoubleClicked` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `PatientService` | 2 | 2 | 0 | 0 | 跨项目 |
| `Query` | 2 | 2 | 1 | 2 | 跨项目/同签名/override |
| `Publish` | 2 | 1 | 0 | 0 | 同项目 |
| `OnDataContextChanged` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `OnDetailCreatedAsync` | 2 | 1 | 1 | 1 | 同项目/同签名/override |
| `OnDetailSavedAsync` | 2 | 1 | 1 | 1 | 同项目/同签名/override |
| `ProcessPendingServerLogoutsAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `QuickCreatePatientAsync` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `PrintAsync` | 2 | 1 | 0 | 0 | 同项目 |
| `PreviewAsync` | 2 | 1 | 0 | 0 | 同项目 |
| `Ping` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `OnHerbNameChanged` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `PhotoExists` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `PerformLogoutAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `OnIsBusyChanged` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `WpfUiThreadDispatcher` | 2 | 1 | 0 | 0 | 同项目 |
| `OnIsLoadingChangedCore` | 2 | 2 | 1 | 2 | 跨项目/同签名/override |
| `UnsubscribeFromRegionCollection` | 2 | 2 | 0 | 0 | 跨项目 |
| `RegisterGlobalExceptionHandlers` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `StopMonitoringAsync` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `UpdateTime` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `Stop` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `UpdateTokenExpiration` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `StartTracking` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `StartTiming` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `StartPolling` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `StartMonitoringFromStorageAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `StopPolling` | 2 | 2 | 0 | 0 | 跨项目 |
| `StartMonitoringAsync` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `UpdateUserPasswordHashAsync` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `ShowTripleChoiceAsync` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `Upload` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `ShowSaveFileDialogAsync` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `ShowOpenFileDialogAsync` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `ShowLoginDialog` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `ShowLoading` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `ShowDialogAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `Start` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `StopTiming` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `StopTracking` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `SubscribeToRegionCollection` | 2 | 2 | 0 | 0 | 跨项目 |
| `UnregisterGlobalExceptionHandlers` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `Unregister` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `TryRefreshTokenAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `TryHandleAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `TryGetDoctorId` | 2 | 1 | 0 | 0 | 同项目 |
| `TransitionTo` | 2 | 2 | 0 | 0 | 跨项目 |
| `ToPrescriptionItemDtos` | 2 | 1 | 0 | 0 | 同项目 |
| `ToListDtos` | 2 | 2 | 0 | 0 | 跨项目 |
| `ToggleTheme` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `UpdateBreadcrumbs` | 2 | 2 | 0 | 0 | 跨项目 |
| `UpdateExpiration` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `ToggleSelection` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `UpdateFromDto` | 2 | 2 | 0 | 0 | 跨项目 |
| `TestRemoteConnectionAsync` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `TestLocalConnectionAsync` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `TestConnectionAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `SuspendViaApiAsync` | 2 | 1 | 0 | 1 | 同项目/override |
| `Suspend` | 2 | 2 | 0 | 0 | 跨项目 |
| `SuccessPaged` | 2 | 1 | 0 | 0 | 同项目 |
| `ShowConfirmationAsync` | 2 | 1 | 1 | 1 | 同项目/同签名/override |
| `UserDisabled` | 2 | 2 | 0 | 0 | 跨项目 |
| `UserExistsAsync` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `UserRepository` | 2 | 2 | 0 | 0 | 跨项目 |
| `SaveAutoLoginTokenAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `SaveAuthenticationAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `ValidateAutoLoginToken` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `SafeFireAndForget` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `Revoke` | 2 | 1 | 0 | 0 | 同项目 |
| `ValidateHerb` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `Restart` | 2 | 2 | 0 | 0 | 跨项目 |
| `ResetToDefaults` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `ValidateProductionConfigAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `ResetCircuitBreaker` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `RequestLeaveAsync` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `RequestEnterEditMode` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `ReportService` | 2 | 2 | 0 | 0 | 跨项目 |
| `ReportsController` | 2 | 2 | 0 | 0 | 跨项目 |
| `RemoveAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `RegistrationsController` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `RegistrationRepository` | 2 | 2 | 0 | 0 | 跨项目 |
| `RegisterStep` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `RegisterInfrastructureServices` | 2 | 2 | 0 | 0 | 跨项目 |
| `SavePasswordAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `RecordUserActivity` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `SavePhotoAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `SaveRemoteUrlAsync` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `UsersController` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `SetUrlAsync` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `SetTokens` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `SetSession` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `SetPrescriptionFlagWithDetailAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `SetPrescriptionFlag` | 2 | 2 | 1 | 2 | 跨项目/同签名/override |
| `SetMode` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `SetLoggingLevel` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `SetErrors` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `SetCorrelationId` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `SetAsLastPage` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `SelectMultiple` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `Select` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `SearchHerbsAsync` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `SearchByCategoryAsync` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `ValidateAndGetUserInfoAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `SaveWithDetailAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `SaveUsernameAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `SaveSettingsAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `SavePreferredModeAsync` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `NotifyStateChanged` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `GoToNextPage` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `NotifyNewRegistrationAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `ExecuteLocalLogoutAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `ExecuteBatchDeleteAsync` | 2 | 2 | 0 | 0 | 跨项目 |
| `ExecuteBackAsync` | 2 | 1 | 0 | 0 | 同项目 |
| `Error` | 2 | 1 | 0 | 0 | 同项目 |
| `EnsureModuleLoadedAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `EndSessionAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `EndLoading` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `EndEdit` | 2 | 2 | 1 | 1 | 跨项目/同签名/override |
| `EnableBatchAsync` | 2 | 1 | 1 | 1 | 同项目/同签名/override |
| `EditModeStateMachine` | 2 | 1 | 0 | 0 | 同项目 |
| `DisableBatchAsync` | 2 | 1 | 1 | 1 | 同项目/同签名/override |
| `DiagnosticsController` | 2 | 2 | 0 | 0 | 跨项目 |
| `DeployController` | 2 | 2 | 0 | 0 | 跨项目 |
| `DeletePhotoAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `DeleteHerb` | 2 | 1 | 0 | 0 | 同项目 |
| `DeleteBatchAsync` | 2 | 1 | 1 | 1 | 同项目/同签名/override |
| `DatabaseHealthCheckResult` | 2 | 1 | 0 | 0 | 同项目 |
| `CreateReader` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `CreateMedicalCaseForRegistrationAsync` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `CountUnfinishedMedicalCasesAsync` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `CountUnfinishedAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `CountPrescriptionsByPrefixAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `CountMedicalCasesAsync` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `CountByPrefixAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `CountAuditLogsAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `CountAllAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `ConfirmSaved` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `ExecuteOnUIThread` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `ExecuteOnUIThreadAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `ExecuteSearchAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `ExecuteSearchImmediateAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetByPatientIdPagedAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetByPatientIdAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetByNameAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetByMedicalCaseIdAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetByIdWithDetailsFreshAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetByIdWithDetailsAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetByIdNumber` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `GetBatchWithDetailsAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetBatchPrescriptionReferenceCountsAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetBatchFormulaReferenceCountsAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetBatchDetailDtosAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetAutoLoginTokenAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetAllModules` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `ConfigurationController` | 2 | 2 | 0 | 0 | 跨项目 |
| `GetAllMetrics` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `GetAllDefinitions` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `GenerateReport` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `FromException` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `FormulaService` | 2 | 2 | 0 | 0 | 跨项目 |
| `FormulasController` | 2 | 2 | 0 | 0 | 跨项目 |
| `FormulaRepository` | 2 | 2 | 0 | 0 | 跨项目 |
| `FireAsync` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `FindWithHerbsAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `FilterHerbs` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `ExportAsync` | 2 | 1 | 0 | 0 | 同项目 |
| `ExecutionUnit` | 2 | 1 | 0 | 0 | 同项目 |
| `ExecuteWithTimeoutAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `ExecuteWithErrorHandlingAsync` | 2 | 1 | 0 | 0 | 同项目 |
| `GetAllHerbsAsync` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `CompleteByMedicalCaseAsync` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `CompleteAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `Complete` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `CanDelete` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `CanCreateNew` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `CanConvert` | 2 | 2 | 1 | 2 | 跨项目/同签名/override |
| `CancelSearch` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `CanCancel` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `BusinessFail` | 2 | 1 | 0 | 0 | 同项目 |
| `BuildNavigationItems` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `bool` | 2 | 1 | 0 | 0 | 同项目 |
| `BeginTransactionAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `BeginLoading` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `BeginInvoke` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `BeginEdit` | 2 | 2 | 1 | 1 | 跨项目/同签名/override |
| `BatchPrintAsync` | 2 | 1 | 0 | 0 | 同项目 |
| `CanEdit` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `BatchDeletePatientsAsync` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `AutoDetectReaderAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `AuthenticationStateMachine` | 2 | 1 | 0 | 0 | 同项目 |
| `AuthController` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `ApplyTheme` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `ApplyProfileUpdate` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `ApplyPasswordChanged` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `ApplyLoginSuccess` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `AppDbContext` | 2 | 1 | 0 | 0 | 同项目 |
| `AddLybtClientConfiguration` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `AddHttpClientApiClient` | 2 | 1 | 0 | 0 | 同项目 |
| `AddHerbsAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `AddHerbs` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `AddAuditLogAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `BaseService` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetByTokenHashAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `CanGoToFirstPage` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `CanGoToNextPage` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `CloseDialog` | 2 | 1 | 0 | 0 | 同项目 |
| `ClearUsernameAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `ClearTokens` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `ClearSession` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `ClearSelection` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `ClearSearchAsync` | 2 | 1 | 0 | 0 | 同项目 |
| `ClearSearch` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `ClearPasswordAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `ClearLoginRegion` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `ClearCredentialsAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `ClearContentRegion` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `ClearAuthInfo` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `ClearAuthenticationAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `CanGoToLastPage` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `ClearAuthentication` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `CheckRemoteAvailableAsync` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `CheckHerbReferenceAsync` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `CheckDatabaseAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `CheckConnectionAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `CheckApiHealthAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `CheckAccess` | 2 | 2 | 0 | 0 | 跨项目 |
| `ChangePassword` | 2 | 2 | 0 | 1 | 跨项目/override |
| `CanToggleStatus` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `CanTestConnection` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `CanSave` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `CanRetry` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `CanReadCard` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `CanGoToPreviousPage` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `ClearAllErrors` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `NotifyRegistrationStatusChangedAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetConsultationCountAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetConsultationListAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `IsRememberMeEnabledAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `IsRegistered` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `IsModuleLoaded` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `IsLoggedInAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `IsAdmin` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `IsA4` | 2 | 1 | 0 | 0 | 同项目 |
| `InvalidateUserCaches` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `InvalidatePatientCaches` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `InvalidateMedicalCaseCaches` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `InvalidateHerbCaches` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `InvalidateFormulaCaches` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `InvalidateAll` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `InitializeViewModel` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `HideLoading` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `HerbsController` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `HerbRepository` | 2 | 2 | 0 | 0 | 跨项目 |
| `HealthController` | 2 | 2 | 0 | 0 | 跨项目 |
| `HasValidTokenAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `HasSavedPasswordAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `HasRole` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `HardDeleteAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `HandleTokenExpiredAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `HandleSuspendedCaseAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `HandleSessionExpiredAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `HandleNewPatientFromCardAsync` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `HandleMedicalCaseCancelledAsync` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `HandleLoginSuccessAsync` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `IsTokenExpiredAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `IsTokenExpiringSoon` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `IsTokenValid` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `IsValidUrl` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `NotificationService` | 2 | 2 | 0 | 0 | 跨项目 |
| `NotFound` | 2 | 2 | 0 | 0 | 跨项目 |
| `NewPatient` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `NavigateToRegistrationQueue` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `NavigateForward` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `NavigateBack` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `MigrateOldFormatAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `MedicalCasesController` | 2 | 2 | 0 | 0 | 跨项目 |
| `MedicalCaseRepository` | 2 | 2 | 0 | 0 | 跨项目 |
| `MedicalCaseQueryService` | 2 | 2 | 0 | 0 | 跨项目 |
| `MedicalCaseCommandService` | 2 | 2 | 0 | 0 | 跨项目 |
| `MatchPatientAsync` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `MarkAsSaved` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `HandleLeaveRequestAsync` | 2 | 1 | 0 | 0 | 同项目 |
| `Main` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `LogException` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `LocalWebApiHttpClientFactory` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `LoadPhotoAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `LoadPatientsAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `LoadPatientDetailAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `LoadModulesForRoleAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `LoadModulesAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `LoadModuleAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `LoadDetail` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `LoadCurrentUserAsync` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `LoadAllModulesAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `LoadAllAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `LinkRegistrationToMedicalCaseAsync` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `Logout` | 2 | 2 | 0 | 0 | 跨项目 |
| `HandleExceptionWithResult` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GoToPreviousPage` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GoToPage` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetPagedWithDetailsAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetOverallStatusAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetModulesForRole` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `GetMetric` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `GetMemorySnapshots` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `GetMedicineFeeTotalAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetMedicineFeeByDayAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetLoginResponseAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetLoginResponse` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetLoggingStatus` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `GetLoadedModules` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetListDtoAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetIncomeTrendAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetPasswordAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetHerbUsageAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetFormulasPagedAsync` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `GetFormulaReferenceCountAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetExactByNameAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetErrors` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetDialogParameter` | 2 | 1 | 0 | 0 | 同项目 |
| `GetDetailDtoAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetDefinition` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `GetDailyIncome` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `GetDailyHerbs` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `GetDailyConsultations` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `GetCurrentUserRoleDisplay` | 2 | 2 | 0 | 0 | 跨项目 |
| `GetConsultationTrendAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetConsultationsByDoctorAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetHerbByNameOrPinyinAsync` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `GetConsultationCountByDayAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetPatientConsultationsAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetPatientDetailByIdAsync` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `WrapSuccess` | 2 | 1 | 0 | 0 | 同项目 |
| `GoToLastPage` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GoToFirstPage` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetUserMessageFromStatusCode` | 2 | 1 | 0 | 0 | 同项目 |
| `GetUserMessageFromErrorCode` | 2 | 1 | 0 | 0 | 同项目 |
| `GetTokenAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetTodayMaxQueueNumberAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetSupportedReaders` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetSettings` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetSavedUsernameAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetSafeOperationFailureMessage` | 2 | 1 | 0 | 0 | 同项目 |
| `GetRequestId` | 2 | 1 | 0 | 0 | 同项目 |
| `GetRegistrationFeeTotalAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetPatientConsultationsPagedAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetRegistrationFeeByDayAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetRecentPrescriptionReferencesAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetRecentMedicalCasesAsync` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `GetRecentLogsAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetRecentAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetPrescriptionReferenceCountAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetPrescriptionListAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetPendingValidation` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `GetPatientsPagedAsync` | 2 | 2 | 1 | 0 | 跨项目/同签名 |
| `GetPatientRecentMedicalCasesAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetPatientPrescriptionsPagedAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetPatientPrescriptionsAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetPatientFlowByDayAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetPatientFlowAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `GetRefreshTokenAsync` | 2 | 1 | 1 | 0 | 同项目/同签名 |
| `Write` | 2 | 2 | 0 | 2 | 跨项目/override |

## 4. 任务书点名方法专项（GetByIdAsync / MapToDto / AddLogging / ValidateAsync）

### 4.1 `GetByIdAsync`（46 定义 / 14 项目）

| 项目 | 类 | 行号 | 参数 | 可见性 |
|------|----|------|------|--------|
| LYBT.Desktop.Contracts | IFormulaRepository | 20 | `Guid|CancellationToken` | public (interface 默认) |
| LYBT.Desktop.Contracts | IHerbRepository | 20 | `Guid|CancellationToken` | public (interface 默认) |
| LYBT.Desktop.Contracts | IFormulaService | 20 | `Guid|CancellationToken` | public (interface 默认) |
| LYBT.Desktop.Contracts | IPatientRepository | 21 | `Guid|CancellationToken` | public (interface 默认) |
| LYBT.Desktop.Contracts | IRegistrationRepository | 21 | `Guid|CancellationToken` | public (interface 默认) |
| LYBT.Desktop.Contracts | IUserRepository | 22 | `Guid|CancellationToken` | public (interface 默认) |
| LYBT.Desktop.Contracts | IRegistrationApi | 23 | `Guid` | public (interface 默认) |
| LYBT.Desktop.Contracts | IRegistrationService | 23 | `Guid|CancellationToken` | public (interface 默认) |
| LYBT.Desktop.Contracts | IApiClientRegistrations | 33 | `Guid` | public (interface 默认) |
| LYBT.Desktop.Contracts | IEntityApiSegment | 33 | `Guid` | public (interface 默认) |
| LYBT.Desktop.Contracts | IMedicalCaseRepository | 33 | `Guid|CancellationToken` | public (interface 默认) |
| LYBT.Desktop.Contracts | IUserService | 42 | `Guid|CancellationToken` | public (interface 默认) |
| LYBT.Desktop.Contracts | IHerbService | 42 | `Guid|CancellationToken` | public (interface 默认) |
| LYBT.Desktop.Contracts | IPatientService | 55 | `Guid|CancellationToken` | public (interface 默认) |
| LYBT.Desktop.Formula | FormulaService | 29 | `Guid|CancellationToken` | public |
| LYBT.Desktop.Foundation | RegistrationsHttpApiClient | 23 | `Guid` | public |
| LYBT.Desktop.Foundation | RegistrationApiClient | 36 | `Guid` | public |
| LYBT.Desktop.Foundation | EntityApiClientRepositoryBase | 69 | `Guid|CancellationToken` | public |
| LYBT.Desktop.Herbs | RemoteHerbService | 122 | `Guid|CancellationToken` | public |
| LYBT.Desktop.MedicalCase | MedicalCaseRepository | 52 | `Guid|CancellationToken` | public |
| LYBT.Desktop.Patients | PatientService | 182 | `Guid|CancellationToken` | public |
| LYBT.Desktop.Registrations | RegistrationRepository | 47 | `Guid|CancellationToken` | public |
| LYBT.Desktop.Registrations | RemoteRegistrationService | 53 | `Guid|CancellationToken` | public |
| LYBT.Desktop.Users | RemoteUserService | 123 | `Guid|CancellationToken` | public |
| LYBT.Infrastructure | BatchOperationHandlerBase | 16 | `Guid|CancellationToken` | protected |
| LYBT.Infrastructure | IRepository | 32 | `Guid|CancellationToken` | public (interface 默认) |
| LYBT.Infrastructure | BaseRepository | 34 | `Guid|CancellationToken` | public |
| LYBT.Module.Formula | IFormulaService | 12 | `Guid|CancellationToken` | public (interface 默认) |
| LYBT.Module.Formula | FormulaRepository | 24 | `Guid|CancellationToken` | public |
| LYBT.Module.Formula | BatchDeleteFormulasCommandHandler | 27 | `Guid|CancellationToken` | protected |
| LYBT.Module.Formula | FormulaService | 35 | `Guid|CancellationToken` | public |
| LYBT.Module.Herbs | IHerbService | 12 | `Guid|CancellationToken` | public (interface 默认) |
| LYBT.Module.Herbs | BatchDeleteHerbsCommandHandler | 32 | `Guid|CancellationToken` | protected |
| LYBT.Module.Herbs | HerbService | 35 | `Guid|CancellationToken` | public |
| LYBT.Module.Patients | IPatientService | 12 | `Guid|CancellationToken` | public (interface 默认) |
| LYBT.Module.Patients | BatchDeletePatientsCommandHandler | 32 | `Guid|CancellationToken` | protected |
| LYBT.Module.Patients | PatientService | 37 | `Guid|CancellationToken` | public |
| LYBT.Module.Registration | IRegistrationRepository | 20 | `Guid|CancellationToken` | public (interface 默认) |
| LYBT.Module.Registration | RegistrationRepository | 30 | `Guid|CancellationToken` | public |
| LYBT.Module.Users | IUserRepository | 9 | `Guid|CancellationToken` | public (interface 默认) |
| LYBT.Module.Users | IUserService | 12 | `Guid|CancellationToken` | public (interface 默认) |
| LYBT.Module.Users | UserRepository | 22 | `Guid|CancellationToken` | public |
| LYBT.Module.Users | BatchDisableUsersCommandHandler | 25 | `Guid|CancellationToken` | protected |
| LYBT.Module.Users | BatchEnableUsersCommandHandler | 25 | `Guid|CancellationToken` | protected |
| LYBT.Module.Users | BatchDeleteUsersCommandHandler | 28 | `Guid|CancellationToken` | protected |
| LYBT.Module.Users | UserService | 35 | `Guid|CancellationToken` | public |

> 初判：GetByIdAsync 在 Server Repository（`Guid` 参数）与 Desktop Contracts/Foundation（API 客户端）为**设计内镜像**（AGENTS.md 跨层镜像接口清单），语义不同不可直接合并；S2/S3 阶段按层内聚类复核。

### 4.2 `MapToDto`（全仓同名组）

（全仓无 `MapToDto` 同名方法——Mapperly 映射方法实际命名为 `ToDto`/`ToEntity` 等，见 §4.3）

### 4.3 Mapperly 映射方法族（`ToDto`/`ToEntity`/`MapTo` 等）

- `ToDto`：7 定义 / 3 项目
- `ToEntity`：3 定义 / 3 项目

### 4.4 `AddLogging` / `AddLogging` 族（日志配置扩展）


> 初判：日志配置扩展散落 Desktop.Infrastructure / WebAPI / Shell / Foundation（计划文档专项 A 证据），为 **S1 Shared 阶段集中定义候选**；本报告仅列出现状。

### 4.5 `ValidateAsync`（同名组）

| 项目 | 类 | 行号 | 参数 | 可见性 |
|------|----|------|------|--------|
| LYBT.Infrastructure | BatchOperationHandlerBase | 36 | `TEntity|Guid|Guid|CancellationToken` | protected |
| LYBT.Module.Patients | BatchDeletePatientsCommandHandler | 48 | `Patient|Guid|Guid|CancellationToken` | protected |
| LYBT.Module.Users | BatchDisableUsersCommandHandler | 44 | `ApplicationUser|Guid|Guid|CancellationToken` | protected |
| LYBT.Module.Users | BatchEnableUsersCommandHandler | 45 | `ApplicationUser|Guid|Guid|CancellationToken` | protected |
| LYBT.Module.Users | BatchDeleteUsersCommandHandler | 46 | `ApplicationUser|Guid|Guid|CancellationToken` | protected |

## 5. 结论与移交

- 本初筛覆盖全部 693 组同名方法；S1-S4 各阶段按层内聚类清单做符号级复核（serena `find_referencing_symbols` + 源码走查）
- 跨层镜像接口（Server Repository 接口 vs Desktop Contracts 接口）为**设计内同名**，不在合并范围（AGENTS.md 跨层镜像接口清单 7 组）
- 高频同名（Handle×60/GetByIdAsync×46/Dispose×45）多为 MediatR/接口实现模式，需按"接口实现 vs 独立实现"分流判定



# Tasks: optimize-batch-operations

## Phase 1: DTO命名规范化 [COMPLETED]

### 1.1 Patient模块
- [x] 1.1.1 重命名 `PatientBatchImportRequestDto` → `PatientBatchImportInputDto`
- [x] 1.1.2 重命名 `PatientImportDto` → `PatientImportItemDto`
- [x] 1.1.3 重命名 `PatientExportDto` → `PatientExportItemDto`
- [x] 1.1.4 重命名 `BatchImportResultDto` → `PatientBatchImportResultDto`

### 1.2 User模块
- [x] 1.2.1 重命名 `UserBatchImportRequestDto` → `UserBatchImportInputDto`

### 1.3 Herb模块
- [x] 1.3.1 重命名 `HerbBatchImportRequestDto` → `HerbBatchImportInputDto`
- [x] 1.3.2 重命名 `HerbImportDto` → `HerbImportItemDto`
- [x] 1.3.3 重命名 `HerbExportDto` → `HerbExportItemDto`
- [x] 1.3.4 重命名 `BatchCheckReferenceRequestDto` → `HerbBatchCheckReferenceInputDto`

### 1.4 Formula模块
- [x] 1.4.1 重命名 `ImportFormulasDataRequest` → `FormulaBatchImportInputDto`
- [x] 1.4.2 重命名 `FormulaImportDto` → `FormulaImportItemDto`
- [x] 1.4.3 重命名 `FormulaHerbImportDto` → `FormulaHerbImportItemDto`
- [x] 1.4.4 重命名 `FormulaExportDto` → `FormulaExportItemDto`
- [x] 1.4.5 重命名 `FormulaHerbExportDto` → `FormulaHerbExportItemDto`
- [x] 1.4.6 重命名 `FormulaImportResultDto` → `FormulaBatchImportResultDto`

### 1.5 验证
- [x] 1.5.1 全项目编译验证 (0 errors, 5 warnings - 均为预存警告)
- [x] 1.5.2 运行单元测试 (User: 31/31, Herb: 33/33, Patient/Formula: 预存问题)
- [x] 1.5.3 更新dto-refactoring-task.md

### 1.6 文件名与类名一致性整改 (git mv)
- [x] 1.6.1 `PatientBatchImportRequestDto.cs` → `PatientBatchImportInputDto.cs`
- [x] 1.6.2 `UserBatchImportRequestDto.cs` → `UserBatchImportInputDto.cs`
- [x] 1.6.3 `HerbBatchImportRequestDto.cs` → `HerbBatchImportInputDto.cs`
- [x] 1.6.4 `BatchCheckReferenceRequestDto.cs` → `HerbBatchCheckReferenceInputDto.cs`

### 1.7 删除重复定义
- [x] 1.7.1 删除 `BatchIdsDto.cs` (功能已由 `BatchDeleteInputDto` 覆盖)

### 1.8 统一失败详情DTO命名
- [x] 1.8.1 重命名 `ImportFailureDetailDto` → `PatientImportFailureDto`
- [x] 1.8.2 重命名 `UserImportFailureDetailDto` → `UserImportFailureDto`
- [x] 1.8.3 重命名 `HerbImportFailureDetailDto` → `HerbImportFailureDto`
- [x] 1.8.4 重命名 `FormulaImportErrorDto` → `FormulaImportFailureDto`
- [x] 1.8.5 更新Service层引用 (PatientService, HerbService, FormulaService)
- [x] 1.8.6 编译验证 (0 errors, 6 warnings - 均为预存警告)

### 1.9 BatchImportResultDto继承规范化
- [x] 1.9.1 `UserBatchImportResultDto` 继承 `ImportResultDto` (消除重复字段)
- [x] 1.9.2 `PatientBatchImportResultDto` 继承 `ImportResultDto` (消除重复字段)
- [x] 1.9.3 `HerbBatchImportResultDto` 继承 `ImportResultDto` (消除重复字段)
- [x] 1.9.4 `FormulaBatchImportResultDto.FailedItems` → `Failures` (统一命名)
- [x] 1.9.5 更新FormulaService使用`Failures`属性
- [x] 1.9.6 编译验证 (0 errors, 0 warnings)

---

## Phase 2: 批量操作API优化 [COMPLETED]

### 2.1 Server端批量删除端点
- [x] 2.1.1 添加 `POST /api/v1/users/batch-delete` 端点
- [x] 2.1.2 添加 `POST /api/v1/patients/batch-delete` 端点
- [x] 2.1.3 添加 `POST /api/v1/herbs/batch-delete` 端点
- [x] 2.1.4 添加 `POST /api/v1/formulas/batch-delete` 端点
- [x] 2.1.5 添加 `POST /api/v1/medicalcases/batch-delete` 端点

### 2.2 Server端批量启用/禁用端点
- [x] 2.2.1 添加 `POST /api/v1/users/batch-enable` 端点
- [x] 2.2.2 添加 `POST /api/v1/users/batch-disable` 端点
- [x] 2.2.3 添加 `POST /api/v1/herbs/batch-enable` 端点
- [x] 2.2.4 添加 `POST /api/v1/herbs/batch-disable` 端点
- [x] 2.2.5 添加 `POST /api/v1/formulas/batch-enable` 端点
- [x] 2.2.6 添加 `POST /api/v1/formulas/batch-disable` 端点

### 2.3 Service层批量删除实现
- [x] 2.3.1 实现 UserService.BatchDeleteAsync
- [x] 2.3.2 实现 PatientService.BatchDeleteAsync
- [x] 2.3.3 实现 HerbService.BatchDeleteAsync
- [x] 2.3.4 实现 FormulaService.BatchDeleteAsync
- [x] 2.3.5 实现 MedicalCaseCommandService.BatchDeleteAsync

### 2.4 Service层批量启用/禁用实现
- [x] 2.4.1 实现 UserService.BatchUpdateStatusAsync (统一处理启用/禁用)
- [x] 2.4.2 实现 HerbService.BatchUpdateStatusAsync (统一处理启用/禁用)
- [x] 2.4.3 实现 FormulaService.BatchUpdateStatusAsync (统一处理启用/禁用)

### 2.5 Desktop层API接口
- [x] 2.5.1 更新 IUserApi 添加 BatchDeleteAsync
- [x] 2.5.2 更新 IPatientApi 添加 BatchDeleteAsync
- [x] 2.5.3 更新 IHerbApi 添加 BatchDeleteAsync
- [x] 2.5.4 更新 IFormulaApi 添加 BatchDeleteAsync
- [x] 2.5.5 更新 IMedicalCaseApi 添加 BatchDeleteAsync

### 2.6 Desktop层Repository实现
- [x] 2.6.1 实现 UserRepository.BatchDeleteAsync
- [x] 2.6.2 实现 PatientRepository.BatchDeleteAsync

### 2.7 Desktop层ViewModel优化
- [x] 2.7.1 优化 UserMasterDetailViewModel.OnExecuteBatchDeleteAsync 为单次调用
- [x] 2.7.2 优化 PatientMasterDetailViewModel.OnExecuteBatchDeleteAsync 为单次调用
- [x] 2.7.3 优化 HerbMasterDetailViewModel.OnExecuteBatchDeleteAsync 为单次调用
- [x] 2.7.4 优化 FormulaMasterDetailViewModel.OnExecuteBatchDeleteAsync 为单次调用
- [x] 2.7.5 优化 MedicalCaseMasterDetailViewModel.OnExecuteBatchDeleteAsync 为单次调用

### 2.8 验证
- [x] 2.8.1 编译验证 (0 errors)
- [x] 2.8.2 集成测试 (BatchOperationsTests.cs - 覆盖Users/Herbs/Formulas/Patients/MedicalCases批量操作)
- [x] 2.8.3 性能测试 (BatchOperationsBenchmark.cs - N+1 vs ExecuteUpdate批量模式对比)

### 2.9 Desktop层批量启用/禁用集成
- [x] 2.9.1 更新 IUserApi 添加 BatchEnableAsync/BatchDisableAsync
- [x] 2.9.2 更新 IHerbApi 添加 BatchEnableAsync/BatchDisableAsync
- [x] 2.9.3 更新 IFormulaApi 添加 BatchEnableAsync/BatchDisableAsync
- [x] 2.9.4 更新 IUserRepository 添加 BatchEnableAsync/BatchDisableAsync
- [x] 2.9.5 更新 IHerbRepository 添加 BatchDeleteAsync/BatchEnableAsync/BatchDisableAsync
- [x] 2.9.6 更新 IFormulaRepository 添加 BatchDeleteAsync/BatchEnableAsync/BatchDisableAsync
- [x] 2.9.7 实现 UserRepository.BatchEnableAsync/BatchDisableAsync
- [x] 2.9.8 实现 HerbRepository.BatchDeleteAsync/BatchEnableAsync/BatchDisableAsync
- [x] 2.9.9 实现 FormulaRepository.BatchDeleteAsync/BatchEnableAsync/BatchDisableAsync
- [x] 2.9.10 编译验证 (0 errors, 4 warnings - 预存MedicalCase null检查警告)

---

## Phase 2 已完成

所有任务已完成:
- 2.8.2: 集成测试 ✓ (BatchOperationsTests.cs)
- 2.8.3: 性能测试 ✓ (BatchOperationsBenchmark.cs)

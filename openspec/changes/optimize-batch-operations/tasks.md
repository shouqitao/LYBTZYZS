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

---

## Phase 2: 批量操作API优化（后续PR）

### 2.1 Server端批量端点
- [ ] 2.1.1 添加 `POST /api/v1/users/batch-delete` 端点
- [ ] 2.1.2 添加 `POST /api/v1/patients/batch-delete` 端点
- [ ] 2.1.3 添加 `POST /api/v1/herbs/batch-delete` 端点
- [ ] 2.1.4 添加 `POST /api/v1/formulas/batch-delete` 端点
- [ ] 2.1.5 添加 `POST /api/v1/medicalcases/batch-delete` 端点
- [ ] 2.1.6 添加批量启用/禁用端点

### 2.2 Service层实现
- [ ] 2.2.1 实现 `BatchDeleteAsync(List<Guid> ids)` 使用 EF Core ExecuteDelete
- [ ] 2.2.2 实现 `BatchEnableAsync(List<Guid> ids)` 使用 EF Core ExecuteUpdate
- [ ] 2.2.3 实现 `BatchDisableAsync(List<Guid> ids)` 使用 EF Core ExecuteUpdate

### 2.3 Desktop层调用优化
- [ ] 2.3.1 更新 IUserApi 添加批量端点
- [ ] 2.3.2 更新 IPatientApi 添加批量端点
- [ ] 2.3.3 更新 IHerbApi 添加批量端点
- [ ] 2.3.4 更新 IFormulaApi 添加批量端点
- [ ] 2.3.5 更新 IMedicalCaseApi 添加批量端点
- [ ] 2.3.6 修改 *MasterDetailViewModel.OnExecuteBatchDeleteAsync 为单次调用

### 2.4 验证
- [ ] 2.4.1 编译验证
- [ ] 2.4.2 集成测试
- [ ] 2.4.3 性能测试（对比N+1 vs 单次调用）

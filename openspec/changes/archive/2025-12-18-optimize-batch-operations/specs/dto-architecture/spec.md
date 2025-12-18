# DTO Architecture Spec Delta: optimize-batch-operations

## ADDED Requirements

### Requirement: Batch Operation DTO Naming Convention
批量操作相关DTO SHALL遵循以下命名规范：

| 类型 | 命名格式 | 用途 |
|------|----------|------|
| 批量输入 | `{Entity}Batch{Operation}InputDto` | 批量操作请求参数 |
| 批量结果 | `{Entity}Batch{Operation}ResultDto` | 批量操作响应结果 |
| 导入行项 | `{Entity}ImportItemDto` | 批量导入中的单行数据 |
| 导出行项 | `{Entity}ExportItemDto` | 批量导出中的单行数据 |
| 通用批量ID | `BatchIdsDto` | 通用的ID列表输入 |
| 通用批量结果 | `BatchOperationResultDto` | 通用的批量操作结果 |

#### Scenario: Batch import input naming
- **WHEN** 定义批量导入请求DTO
- **THEN** 命名格式SHALL为 `{Entity}BatchImportInputDto`（如 `PatientBatchImportInputDto`）

#### Scenario: Import item naming
- **WHEN** 定义批量导入中的单行数据DTO
- **THEN** 命名格式SHALL为 `{Entity}ImportItemDto`（如 `PatientImportItemDto`）

#### Scenario: Export item naming
- **WHEN** 定义批量导出中的单行数据DTO
- **THEN** 命名格式SHALL为 `{Entity}ExportItemDto`（如 `PatientExportItemDto`）

---

### Requirement: Batch Delete/Enable/Disable API
系统SHALL提供服务端批量删除/启用/禁用API端点，避免客户端N+1调用模式。

#### Scenario: Single API call for batch delete
- **WHEN** 用户选择多个实体执行批量删除
- **THEN** Desktop客户端SHALL发送单次API请求到 `POST /api/v1/{entity}/batch-delete`
- **AND** 请求体SHALL使用 `BatchIdsDto` 包含所有待删除ID

#### Scenario: Batch operation result
- **WHEN** 服务端完成批量操作
- **THEN** 响应SHALL使用 `BatchOperationResultDto` 返回操作结果
- **AND** 结果SHALL包含 TotalCount, SuccessCount, FailureCount, FailedIds

---

## RENAMED Requirements

### Batch DTO Rename List

- FROM: `PatientBatchImportRequestDto`
- TO: `PatientBatchImportInputDto`

- FROM: `UserBatchImportRequestDto`
- TO: `UserBatchImportInputDto`

- FROM: `HerbBatchImportRequestDto`
- TO: `HerbBatchImportInputDto`

- FROM: `ImportFormulasDataRequest`
- TO: `FormulaBatchImportInputDto`

- FROM: `BatchCheckReferenceRequestDto`
- TO: `HerbBatchCheckReferenceInputDto`

- FROM: `PatientImportDto`
- TO: `PatientImportItemDto`

- FROM: `HerbImportDto`
- TO: `HerbImportItemDto`

- FROM: `FormulaImportDto`
- TO: `FormulaImportItemDto`

- FROM: `FormulaHerbImportDto`
- TO: `FormulaHerbImportItemDto`

- FROM: `PatientExportDto`
- TO: `PatientExportItemDto`

- FROM: `HerbExportDto`
- TO: `HerbExportItemDto`

- FROM: `FormulaExportDto`
- TO: `FormulaExportItemDto`

- FROM: `FormulaHerbExportDto`
- TO: `FormulaHerbExportItemDto`

- FROM: `BatchImportResultDto` (Patients namespace)
- TO: `PatientBatchImportResultDto`

- FROM: `FormulaImportResultDto`
- TO: `FormulaBatchImportResultDto`

# Change: 简化MedicalCase API端点

## Why

当前MedicalCase模块API设计存在严重问题:

1. **端点过多**: Server端28个方法 vs 其他模块10-15个
2. **命名不一致**: `/aggregate`, `/management`, `/list`等非直观命名
3. **查询变体冗余**: 6种查询变体(GetList, GetMedicalCasesList, GetMedicalCasesByPatientId, GetPatientRecentMedicalCases, GetPendingCases, GetUnfinishedCaseByPatientId)
4. **Ghost APIs**: Client定义但Server未实现的端点(ClearPrescription, ImportFormula)
5. **当前Bug**: `PUT /aggregate`返回400无法保存医案

## What Changes

### 端点简化 (28 → 13)

| 操作 | 原端点 | 新端点 | 说明 |
|------|--------|--------|------|
| **合并** | GetList, GetMedicalCasesList | GET `/` | 统一分页列表 |
| **合并** | GetById, GetMedicalCaseByIdWithDetails | GET `/{id}` | 用include参数控制详情级别 |
| **保留** | CreateMedicalCase | POST `/` | - |
| **重命名** | SaveAggregate | PUT `/{id}` | 移除/aggregate |
| **保留** | DeleteMedicalCase | DELETE `/{id}` | - |
| **保留** | BatchDelete | POST `/batch-delete` | - |
| **合并** | CancelMedicalCase, CloseMedicalCase, UpdateStatus | PATCH `/{id}/status` | 状态变更统一入口 |
| **保留** | SaveDraft | PUT `/{id}/draft` | - |
| **合并** | GetMedicalCasesByPatientId, GetPatientRecentMedicalCases, GetUnfinishedCaseByPatientId | GET `/patient/{patientId}` | 用filter参数区分 |
| **保留** | GetPendingCases | GET `/pending` | - |
| **保留** | SearchMedicalCases | GET `/search` | - |
| **保留** | GetPermissions | GET `/{id}/permissions` | - |
| **保留** | GetAuditLogs | GET `/{id}/audit-logs` | - |

### 删除的端点

- `ClearPrescriptionAsync` - Ghost API
- `ImportFormulaIntoPrescriptionAsync` - Ghost API
- `GetConsultationList` - 合并到GET `/{id}`
- `GetPrescriptionList` - 合并到GET `/{id}`
- `CreatePrescription/UpdatePrescription/DeletePrescription` - 通过PUT `/{id}`更新
- `CreatePrescriptionSimple/UpdatePrescriptionSimple` - 通过PUT `/{id}`更新
- `UpdateConsultation` - 通过PUT `/{id}`更新
- `SetPrescriptionFlag` - 通过PUT `/{id}`或PATCH `/{id}/status`

## Impact

- **Affected specs**: medicalcase
- **Affected code**:
  - `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs`
  - `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IMedicalCaseApi.cs`
  - `src/Server/Modules/LYBT.Module.MedicalCase/Services/`
  - `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/`
  - 相关测试文件
- **Breaking changes**: 是，但用户已确认不需要考虑兼容性

## Dependencies

- `unify-medicalcase-input-dto` (27/29) - DTO统一，与本提案互补
- `refactor-medicalcase-management` (32/41) - UI布局，与本提案独立

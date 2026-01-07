# unify-pending-query-api Tasks

## Overview

- **变更类型**: Refactor (API统一 + Bug修复)
- **风险等级**: Medium
- **预估工作量**: 2-3小时
- **实际执行时间**: 2026-01-07
- **状态**: 已完成

## Phase 1: 修复PatientSelectionViewModel Bug (优先)

### 1.1 分析当前调用逻辑
- [x] **文件**: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/PatientSelectionViewModel.cs`
- [x] **发现**: Server端GetPendingCases不接受doctorId参数，从JWT获取当前用户
- [x] **问题重定义**: Desktop端传递的参数被忽略，需改为patientId参数

### 1.2 修复Desktop端API接口
- [x] **文件**: `IMedicalCaseApi.cs`
- [x] **变更**: 移除无用的doctorId参数，改为可选的patientId参数
```csharp
Task<ApiResponse<List<PendingMedicalCaseDto>>> GetPendingCasesAsync([Refit.Query] Guid? patientId = null);
```
- [x] **验证**: 编译通过

### 1.3 修复PatientSelectionViewModel调用
- [x] **文件**: `PatientSelectionViewModel.cs`
- [x] **变更**: 传递patientId参数筛选该患者的暂存医案
```csharp
var pendingCases = await _medicalCaseApi.GetPendingCasesAsync(SelectedPatient.Id);
```
- [x] **验证**: 编译通过

## Phase 2: Server端添加patientId参数

### 2.1 修改IMedicalCaseRepository接口
- [x] **文件**: `src/Server/Modules/LYBT.Module.MedicalCase/Interfaces/IMedicalCaseRepository.cs`
- [x] **变更**: 添加可选`patientId`参数
```csharp
Task<List<PendingMedicalCaseDto>> GetPendingCasesAsync(Guid doctorId, Guid? patientId = null);
```

### 2.2 修改MedicalCaseRepository实现
- [x] **文件**: `src/Server/Modules/LYBT.Module.MedicalCase/Repositories/MedicalCaseRepository.cs`
- [x] **变更**: 实现按patientId筛选逻辑
- [x] **验证**: Where条件包含patientId筛选

### 2.3 修改IMedicalCaseQueryService接口
- [x] **文件**: `src/Server/Modules/LYBT.Module.MedicalCase/Interfaces/IMedicalCaseQueryService.cs`
- [x] **变更**: 添加可选`patientId`参数

### 2.4 修改MedicalCaseQueryService实现
- [x] **文件**: `src/Server/Modules/LYBT.Module.MedicalCase/Services/MedicalCaseQueryService.cs`
- [x] **变更**: 传递patientId参数到Repository

### 2.5 修改MedicalCaseController
- [x] **文件**: `src/Server/Services/LYBT.WebAPI/Controllers/MedicalCaseController.cs`
- [x] **变更**: GetPendingCases方法添加`[FromQuery] Guid? patientId = null`参数
- [x] **验证**: API端点支持新参数

## Phase 3: Desktop端其他调用点更新

### 3.1 更新PendingQueueManager
- [x] **文件**: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Services/PendingQueueManager.cs`
- [x] **变更**: 移除不再需要的doctorId参数，使用无参调用
```csharp
var response = await _medicalCaseApi.GetPendingCasesAsync();
```
- [x] **说明**: Server从JWT获取当前用户ID进行数据隔离

## Phase 4: 最终验证

### 4.1 更新单元测试
- [x] **文件**: `tests/UnitTests/Server/Modules/LYBT.Module.MedicalCase.Tests/Services/MedicalCaseQueryServiceTests.cs`
- [x] **变更**: 更新Mock配置，添加patientId参数测试用例

### 4.2 全量编译验证
- [x] **命令**: `dotnet build LYBT.All.sln -c Release --no-restore`
- [x] **验证**: 零编译错误，零警告

### 4.3 功能测试清单
- [ ] 待看诊队列正常显示（PendingQueueManager）
- [ ] 患者选择后能找到该患者的暂存医案（PatientSelectionViewModel）
- [ ] 无patientId参数时返回所有待看诊（向后兼容）
- [ ] 有patientId参数时仅返回该患者的待看诊

## Validation Checklist

- [x] PatientSelectionViewModel参数传递问题已修复
- [x] Server端GetPendingCasesAsync支持patientId参数
- [x] Desktop端IMedicalCaseApi接口已更新（改为patientId可选参数）
- [x] PendingQueueManager功能不受影响（向后兼容）
- [x] Server解决方案编译通过
- [x] Desktop解决方案编译通过
- [x] 全量编译通过
- [x] 相关测试已更新
- [ ] 待看诊队列功能正常（需手动测试）
- [ ] 暂存医案查找功能正常（需手动测试）

## Modified Files

| 文件 | 变更类型 | 说明 |
|------|---------|------|
| `IMedicalCaseApi.cs` | 修改 | doctorId→patientId可选参数 |
| `PatientSelectionViewModel.cs` | 修改 | 传递patientId参数 |
| `PendingQueueManager.cs` | 修改 | 移除不再需要的参数 |
| `IMedicalCaseRepository.cs` | 修改 | 添加patientId参数 |
| `MedicalCaseRepository.cs` | 修改 | 实现按患者筛选 |
| `IMedicalCaseQueryService.cs` | 修改 | 添加patientId参数 |
| `MedicalCaseQueryService.cs` | 修改 | 传递patientId参数 |
| `MedicalCaseController.cs` | 修改 | 添加patientId查询参数 |
| `MedicalCaseQueryServiceTests.cs` | 修改 | 更新Mock配置，添加新测试 |

## Notes

1. **架构发现**: Server端GetPendingCases从JWT获取当前用户ID，不接受doctorId查询参数。Desktop端原设计传递doctorId是错误的。

2. **向后兼容**: 所有变更保持向后兼容，patientId为可选参数

3. **管理员场景**: 管理员查询时如果传patientId，在内存中过滤结果

---

**生成时间**: 2026-01-07
**完成时间**: 2026-01-07
**状态**: 已完成，待手动测试

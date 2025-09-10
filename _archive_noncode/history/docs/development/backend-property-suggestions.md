# 后端属性添加建议表

## 概述
在清理前端Core项目中的DTO后，发现以下属性在前端使用但在Shared模块的DTO中缺失。建议后端在相应的DTO中添加这些属性。

## 1. UpdateHerbDto
**文件位置**: `LYBT.Shared.Models.Herbs.UpdateHerbDto`

**缺失属性**:
- `Id` (Guid) - 药材ID，用于标识要更新的药材

**建议**: 
- 添加 `public Guid Id { get; set; }` 属性
- 或者修改接口方法签名，将ID作为单独参数传递

## 2. RecordDto (前端使用但后端缺失的属性)
**文件位置**: `LYBT.Shared.Models.Records.RecordDto`

前端曾使用以下属性，但在后端RecordDto中不存在：
- `RegistrationId` (Guid) - 挂号ID
- `Diagnosis` (string) - 诊断（可能与TCMDiagnosis/WesternDiagnosis重复）
- `TreatmentAdvice` (string) - 治疗建议
- `DiagnosisResults` (List<string>) - 诊断结果列表
- `IsShared` (bool) - 是否共享
- `SharedToDoctorIds` (List<string>) - 共享给的医生ID列表
- `CreatedBy` (string) - 创建人
- `RecordTime` (DateTime) - 记录时间
- `IsSelected` (bool) - 是否选中（前端UI状态，可能不需要后端添加）

## 3. PatientDetailDto
**文件位置**: `LYBT.Shared.Models.Contracts.Patients.PatientDetailDto`

建议确认此DTO是否包含患者管理界面所需的所有属性。

## 4. HerbPagedQueryDto
**文件位置**: 需要创建或确认是否存在

前端需要分页查询药材的DTO，建议后端提供统一的查询参数DTO。

## 注意事项
1. 有些属性可能是前端特有的UI状态（如IsSelected），不需要后端添加
2. 建议后端团队评估哪些属性是业务必需的，哪些是前端特有的
3. 对于缺失的属性，可以考虑：
   - 直接添加到现有DTO
   - 创建新的专用DTO
   - 修改API接口设计
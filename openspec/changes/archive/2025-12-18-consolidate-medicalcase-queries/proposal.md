# Change: 整合跨医案查询到MedicalCase聚合根

## Why

当前跨医案查询(如患者历史处方查询)实现违反了DDD聚合根设计原则:
- PrescriptionService从Prescription实体出发，反向关联MedicalCase/Patient
- 存在独立的Consultation/Prescription WebApi端点，但Desktop实际调用率极低
- 查询路径不符合"从聚合根出发"的设计模式

## What Changes

### 1. 新增MedicalCase聚合根查询能力
- 在MedicalCaseController添加 `/search` 端点，支持跨医案搜索
- 在MedicalCaseController添加 `/patient/{patientId}/recent` 端点，获取患者最近医案

### 2. Desktop客户端迁移
- 将PrescriptionEditorService中的 `GetPatientRecentPrescriptionsAsync` 调用迁移到IMedicalCaseApi

### 3. WebApi清理 (保留项目结构)
- **删除**: ConsultationController (2个方法，0次实际调用)
- **删除**: PrescriptionsController (4个方法，仅1次实际调用，迁移后删除整个Controller)
- **删除**: IConsultationApi (3个方法，0次实际调用)
- **删除**: IPrescriptionApi (4个方法，仅1次实际调用，迁移后删除整个接口)

### 4. Server层清理
- 删除PrescriptionService中的 `SearchPrescriptionsAsync`
- 删除PrescriptionService中的 `GetPatientRecentPrescriptionsAsync`

### 5. DTO级联清理
- **删除**: `MedicalCaseBasicDto.cs` - 唯一调用者LoadMedicalCasesAsync随PrescriptionService方法删除而成为死代码
- **删除**: ICrossModuleQueryService中的 `GetMedicalCaseBasicInfoAsync` 和 `GetMedicalCasesBasicInfoAsync` 方法
- **删除**: PrescriptionService中的 `LoadMedicalCasesAsync` 私有方法

### 6. MedicalCase DTO统一为聚合模式

**业务规则确认**:
- 诊断（Consultation）：**必填**
- 处方（Prescription）：**可选**
- 系统不容许没有诊断的医案被保存

**设计问题**:
当前"先创建空壳医案再逐步填充"的流程违反业务规则，空壳医案是非法状态。

**6.1 Input DTO统一**:
- **删除**: `MedicalCaseInputDto.cs` - 16字段仅用5个
- **删除**: `MedicalCaseCreateInputDto.cs` - Server端点不存在
- **删除**: `CreateMedicalCaseRequest` - Controller内部类
- **重命名并扩展**: `MedicalCaseAggregateInputDto` → `MedicalCaseInputDto` - 添加PatientId/VisitDate支持创建场景

**6.2 Output DTO设计（CQRS读模型原则）**:

根据CQRS"每屏一投影"原则，读侧查询针对UI需求优化，保持List/Detail分离：
- **保留**: `MedicalCaseListDto` - 列表视图，扁平化设计，含Diagnosis字段支持hover
- **保留**: `MedicalCaseDetailDto` - 详情/编辑视图，聚合模式含嵌套子实体
- **保留**: `PendingMedicalCaseDto` - 特殊业务场景

**6.3 API契约统一**:
- **修改**: `POST /medicalcases` 使用 `MedicalCaseInputDto`
- **删除**: `CreateMedicalCaseWithDetailsAsync` (死代码)
- **删除**: `SoftDeleteMedicalCaseAsync` (Server端点不存在)
- **删除**: `DeletePrescriptionAsync` (Server端点不存在)

**结论**:
- Input: 统一为MedicalCaseInputDto（聚合模式），符合DDD写侧原则
- Output: 保持List/Detail分离，符合CQRS读侧原则

## Impact

### Affected Specs
- `medicalcase-lifecycle` - 新增LIFECYCLE-015/016跨医案查询需求

### Affected Code

**新增/修改:**
- `MedicalCaseController.cs` - 添加Search/GetPatientRecent端点
- `MedicalCaseQueryService.cs` - 添加SearchAsync/GetPatientRecentAsync方法
- `IMedicalCaseApi.cs` - 添加对应Refit方法
- `PrescriptionEditorService.cs` - 迁移到使用IMedicalCaseApi

**删除:**
- `ConsultationController.cs` - 整个Controller
- `PrescriptionsController.cs` - 整个Controller
- `IConsultationApi.cs` - 整个接口
- `IPrescriptionApi.cs` - 整个接口
- `PrescriptionService.cs` - SearchPrescriptionsAsync/GetPatientRecentPrescriptionsAsync/LoadMedicalCasesAsync方法
- `MedicalCaseBasicDto.cs` - 级联删除（死代码）
- `ICrossModuleQueryService.cs` - GetMedicalCaseBasicInfoAsync/GetMedicalCasesBasicInfoAsync方法
- `CrossModuleQueryService.cs` - 对应方法实现

**Input DTO清理:**
- `MedicalCaseInputDto.cs` - 删除11个死诊断字段(ChiefComplaint, PresentIllnessHistory等)
- `IMedicalCaseApi.cs` - 删除CreateMedicalCaseWithDetailsAsync方法
- `MedicalCaseCreateInputDto.cs` - 可选删除(确认无调用后)

### 不删除的项目 (显式保留)
- `LYBT.Desktop.Consultation` 项目
- `LYBT.Desktop.Prescriptions` 项目
- `LYBT.Module.Consultation` 项目
- `LYBT.Module.Prescriptions` 项目

## Design Rationale

### DDD聚合根查询模式
根据Microsoft DDD最佳实践:
1. **从聚合根出发**: MedicalCase作为聚合根，包含Consultation和Prescription子实体
2. **IReadOnlyCollection暴露**: 通过聚合根访问子实体集合
3. **查询完整聚合**: 返回MedicalCaseDetailDto，内含嵌套的ConsultationDetailDto和PrescriptionDetailDto

### 为何不删除项目结构
1. **扩展性**: 未来可能有Consultation/Prescription独立业务场景
2. **模块化**: 保持清晰的模块边界，即使当前代码较少
3. **渐进式清理**: 避免一次性大规模删除带来的风险

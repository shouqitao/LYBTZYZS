# DTO Contracts Specification: MedicalCase Input DTOs

## MODIFIED Requirements

### Requirement: REQ-DTO-MEDCASE-INPUT - MedicalCase Input DTO Structure

系统 **SHALL** 确保MedicalCaseInputDto仅包含创建/更新医案的核心字段(PatientId, DoctorId, VisitDate)，不包含诊断相关字段。

#### Scenario: 创建医案时使用简化的InputDto
- **Given** 用户选择患者并开始就诊
- **When** 系统调用CreateAsync创建医案
- **Then** 仅需提供PatientId和可选的VisitDate
- **And** 诊断信息通过单独的Consultation API保存

#### Scenario: Server端使用Shared层DTO
- **Given** Server模块MedicalCaseService
- **When** 处理创建医案请求
- **Then** 直接使用Shared层MedicalCaseInputDto
- **And** 不使用内部CreateMedicalCaseRequest类

---

### Requirement: REQ-DTO-MEDCASE-AGGREGATE - MedicalCase Aggregate Input DTO Structure

系统 **SHALL** 使用MedicalCaseAggregateInputDto用于聚合保存场景，包含嵌套的ConsultationInputDto和PrescriptionAggregateInputDto。

#### Scenario: 聚合保存医案数据
- **Given** 用户完成诊断和处方编辑
- **When** 点击保存按钮
- **Then** 使用MedicalCaseAggregateInputDto一次性提交
- **And** 包含嵌套的ConsultationInputDto和PrescriptionAggregateInputDto

---

## REMOVED Requirements

### Requirement: REQ-DTO-MEDCASE-INTERNAL - Server Internal CreateMedicalCaseRequest

系统 **SHALL** 删除Server内部的CreateMedicalCaseRequest类，统一使用Shared层DTO以保持Client-Server API契约一致。

#### Scenario: 消除内部类
- **Given** Server端MedicalCaseService
- **When** 定义CreateAsync方法参数
- **Then** 使用Shared.Models.Contracts.MedicalCase.MedicalCaseInputDto
- **And** 不存在CreateMedicalCaseRequest内部类

---

### Requirement: REQ-DTO-MEDCASE-DIAG-FIELDS - 诊断字段从InputDto移除

系统 **SHALL** 从MedicalCaseInputDto中移除未被使用的诊断字段(ChiefComplaint, TCMDiagnosis, WesternDiagnosis等)。

#### Scenario: InputDto不包含诊断字段
- **Given** MedicalCaseInputDto定义
- **When** 检查字段列表
- **Then** 不包含ChiefComplaint, TCMDiagnosis, WesternDiagnosis等诊断字段
- **And** 诊断字段仅存在于ConsultationInputDto

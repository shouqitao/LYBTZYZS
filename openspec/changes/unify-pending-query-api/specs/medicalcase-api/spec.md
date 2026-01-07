# MedicalCase API - Pending Query Enhancement

## MODIFIED Requirements

### Requirement: GetPendingCasesAsync支持按患者筛选

`GetPendingCasesAsync` API端点 **MUST** 支持可选的`patientId`参数，允许按患者筛选待看诊医案。当`patientId`参数未提供时，**SHALL** 返回所有待看诊医案（向后兼容）。

#### Scenario: 仅传doctorId获取所有待看诊
- **Given** 用户已登录且有待看诊医案
- **When** 调用 `GET /api/v1/medicalcases/pending?doctorId={id}`
- **Then** 返回该医生所有待看诊医案列表

#### Scenario: 传doctorId和patientId获取特定患者待看诊
- **Given** 用户已登录且特定患者有暂存医案
- **When** 调用 `GET /api/v1/medicalcases/pending?doctorId={id}&patientId={patientId}`
- **Then** 仅返回该患者的待看诊医案

#### Scenario: patientId不存在时返回空列表
- **Given** 用户已登录
- **When** 调用 `GET /api/v1/medicalcases/pending?doctorId={id}&patientId={不存在的ID}`
- **Then** 返回空列表，不报错

### Requirement: PatientSelectionViewModel正确传递参数

`PatientSelectionViewModel`调用`GetPendingCasesAsync`时 **MUST** 传递正确的`doctorId`参数（从SessionManager获取），**SHALL NOT** 使用`patientId`作为`doctorId`参数值。

#### Scenario: 开始诊疗时查找暂存医案
- **Given** 医生选择了一个患者
- **When** 点击"开始诊疗"按钮
- **Then** 系统使用当前医生ID和选中患者ID调用API
- **And** 如果该患者有暂存医案则提示恢复

---

**相关规范**: `specs/client-api-conventions/spec.md`

# MedicalCase API Specification Delta

## MODIFIED Requirements

### Requirement: MedicalCase Query API
系统SHALL提供简化的医案查询API，支持分页、过滤和详情级别控制。

#### Scenario: 获取医案列表
- **WHEN** 调用 GET `/api/v1/medicalcases`
- **THEN** 返回分页的医案列表
- **AND** 支持include参数控制返回字段级别

#### Scenario: 获取医案详情
- **WHEN** 调用 GET `/api/v1/medicalcases/{id}`
- **THEN** 返回医案详情
- **AND** 支持include=all参数返回完整聚合数据(含Consultation和Prescriptions)

#### Scenario: 按患者查询医案
- **WHEN** 调用 GET `/api/v1/medicalcases/patient/{patientId}`
- **THEN** 返回该患者的医案列表
- **AND** 支持filter参数(unfinished/recent/all)过滤结果

### Requirement: MedicalCase Command API
系统SHALL提供简化的医案命令API，支持创建、更新、删除和状态变更。

#### Scenario: 创建医案
- **WHEN** 调用 POST `/api/v1/medicalcases`
- **THEN** 创建新医案并返回创建结果

#### Scenario: 更新医案(聚合保存)
- **WHEN** 调用 PUT `/api/v1/medicalcases/{id}`
- **THEN** 更新医案及其关联的Consultation和Prescriptions
- **AND** 返回更新后的医案

#### Scenario: 删除医案
- **WHEN** 调用 DELETE `/api/v1/medicalcases/{id}`
- **THEN** 软删除医案

#### Scenario: 批量删除医案
- **WHEN** 调用 POST `/api/v1/medicalcases/batch-delete`
- **THEN** 批量软删除指定医案

#### Scenario: 更新医案状态
- **WHEN** 调用 PATCH `/api/v1/medicalcases/{id}/status`
- **THEN** 更新医案状态(Draft/Completed/Cancelled)
- **AND** 可选提供状态变更原因

#### Scenario: 保存草稿
- **WHEN** 调用 PUT `/api/v1/medicalcases/{id}/draft`
- **THEN** 保存医案草稿状态

## REMOVED Requirements

### Requirement: 独立Prescription CRUD端点
**Reason**: 处方作为医案聚合子实体，通过PUT `/{id}`统一更新
**Migration**: 使用PUT `/api/v1/medicalcases/{id}`更新处方

### Requirement: 独立Consultation更新端点
**Reason**: 诊断作为医案聚合子实体，通过PUT `/{id}`统一更新
**Migration**: 使用PUT `/api/v1/medicalcases/{id}`更新诊断

### Requirement: Ghost APIs
**Reason**: ClearPrescription和ImportFormula在Server端从未实现
**Migration**: 删除Client端未使用的接口定义

### Requirement: 重复查询端点
**Reason**: GetList/GetMedicalCasesList功能重复
**Migration**: 统一使用GET `/api/v1/medicalcases`

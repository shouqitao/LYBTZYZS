# medicalcase-lifecycle Spec Delta

## ADDED Requirements

### Requirement: LIFECYCLE-010 暂存医案API
系统 **SHALL** 提供暂存医案API端点，保存当前编辑状态而不完成医案。

#### Scenario: 暂存草稿成功
- **GIVEN** 医案状态为Draft或Active
- **WHEN** 调用 `PUT /api/v1/medicalcases/{id}/draft`
- **THEN** 系统保存当前Consultation和Prescription数据
- **AND** 医案状态设置为Draft
- **AND** 返回200 OK

#### Scenario: 暂存已完成医案失败
- **GIVEN** 医案状态为Completed
- **WHEN** 调用 `PUT /api/v1/medicalcases/{id}/draft`
- **THEN** 返回422 UnprocessableEntity
- **AND** 错误消息说明已完成医案不可暂存

---

### Requirement: LIFECYCLE-011 取消医案API
系统 **SHALL** 提供取消医案API端点，用于取消未完成的医案。

#### Scenario: 取消医案成功
- **GIVEN** 医案状态为Draft或Active
- **AND** 用户有取消权限
- **WHEN** 调用 `PUT /api/v1/medicalcases/{id}/cancel`
- **THEN** 医案状态设置为Cancelled
- **AND** 返回200 OK

#### Scenario: 取消需要审计理由
- **GIVEN** 医案需要审计（非当天本人操作）
- **WHEN** 调用取消API
- **THEN** 必须提供reason参数
- **AND** 记录审计日志

#### Scenario: 取消已完成医案
- **GIVEN** 医案状态为Completed
- **WHEN** 调用取消API
- **THEN** 返回422 UnprocessableEntity
- **AND** 错误消息说明已完成医案不可取消

---

### Requirement: LIFECYCLE-012 API端点统一入口
系统 **SHALL** 将所有医案写操作统一到MedicalCaseController。

#### Scenario: Consultation写操作入口
- **WHEN** 需要更新诊断信息
- **THEN** 必须使用 `PUT /api/v1/medicalcases/{id}/consultation`
- **AND** ConsultationController仅提供只读查询

#### Scenario: Prescription写操作入口
- **WHEN** 需要创建或更新处方
- **THEN** 必须使用 `POST/PUT /api/v1/medicalcases/{id}/prescriptions`
- **AND** PrescriptionsController仅提供只读查询

---

## MODIFIED Requirements

### Requirement: LIFECYCLE-005 关闭病案
系统 **SHALL** 提供关闭医案功能，支持权限检查和审计日志。

#### Scenario: 关闭医案成功
- **GIVEN** 医案状态为Draft或Active
- **AND** 用户有关闭权限
- **WHEN** 调用 `PUT /api/v1/medicalcases/{id}/close`
- **THEN** 医案状态设置为Completed
- **AND** 返回200 OK

#### Scenario: 关闭需要审计
- **GIVEN** 医案需要审计（非当天本人或已有内容）
- **WHEN** 调用关闭API
- **THEN** 记录审计日志
- **AND** 包含操作者、时间、操作类型

---

### Requirement: LIFECYCLE-006 删除病案
系统 **SHALL** 提供软删除医案功能，标记IsDeleted而非物理删除。

#### Scenario: 软删除成功
- **GIVEN** 医案存在
- **AND** 用户有删除权限
- **WHEN** 调用 `DELETE /api/v1/medicalcases/{id}`
- **THEN** 设置IsDeleted = true
- **AND** 返回204 NoContent

#### Scenario: 删除不存在的医案
- **GIVEN** 医案ID不存在
- **WHEN** 调用删除API
- **THEN** 返回404 NotFound

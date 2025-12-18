## ADDED Requirements

### Requirement: LIFECYCLE-015 跨医案搜索API
系统 **SHALL** 提供跨医案搜索功能，支持按患者名称、诊断关键词等条件查询。

#### Scenario: 按患者名称搜索
- **GIVEN** 存在多个医案，关联不同患者
- **WHEN** 调用 `GET /api/v1/medicalcases/search?patientName=张三`
- **THEN** 返回患者名称包含"张三"的所有医案
- **AND** 每个医案包含嵌套的Consultation和Prescription数据
- **AND** 结果按创建时间倒序排列

#### Scenario: 按诊断关键词搜索
- **GIVEN** 存在多个医案，包含不同诊断信息
- **WHEN** 调用 `GET /api/v1/medicalcases/search?diagnosisKeyword=感冒`
- **THEN** 返回诊断信息包含"感冒"的所有医案
- **AND** 结果支持分页（page, pageSize参数）

#### Scenario: 组合条件搜索
- **GIVEN** 存在多个医案
- **WHEN** 调用 `GET /api/v1/medicalcases/search?patientName=李&diagnosisKeyword=咳嗽&startDate=2024-01-01`
- **THEN** 返回同时满足所有条件的医案
- **AND** 条件为AND关系

---

### Requirement: LIFECYCLE-016 患者最近医案查询API
系统 **SHALL** 提供按患者ID查询最近医案的功能，用于处方编辑器历史处方参考。

#### Scenario: 获取患者最近医案
- **GIVEN** 患者存在多个历史医案
- **WHEN** 调用 `GET /api/v1/medicalcases/patient/{patientId}/recent?count=5`
- **THEN** 返回该患者最近5个医案
- **AND** 每个医案包含完整的Prescription数据（含Items）
- **AND** 结果按创建时间倒序排列

#### Scenario: 患者无历史医案
- **GIVEN** 患者无任何历史医案
- **WHEN** 调用 `GET /api/v1/medicalcases/patient/{patientId}/recent`
- **THEN** 返回空列表
- **AND** HTTP状态码为200 OK

#### Scenario: 患者不存在
- **GIVEN** 患者ID不存在
- **WHEN** 调用 `GET /api/v1/medicalcases/patient/{patientId}/recent`
- **THEN** 返回404 NotFound

---

## MODIFIED Requirements

### Requirement: LIFECYCLE-012 API端点统一入口
系统 **SHALL** 将所有医案写操作统一到MedicalCaseController，并将跨医案查询能力整合到MedicalCase端点。

#### Scenario: Consultation写操作入口
- **WHEN** 需要更新诊断信息
- **THEN** 必须使用 `PUT /api/v1/medicalcases/{id}/consultation`
- **AND** ConsultationController不再提供任何端点（已删除）

#### Scenario: Prescription写操作入口
- **WHEN** 需要创建或更新处方
- **THEN** 必须使用 `POST/PUT /api/v1/medicalcases/{id}/prescriptions`
- **AND** PrescriptionsController不再提供任何端点（已删除）

#### Scenario: 跨医案查询入口
- **WHEN** 需要跨医案搜索或查询患者历史处方
- **THEN** 必须使用MedicalCaseController的search/patient/{id}/recent端点
- **AND** 不再使用PrescriptionsController的Search/GetRecentByPatient端点

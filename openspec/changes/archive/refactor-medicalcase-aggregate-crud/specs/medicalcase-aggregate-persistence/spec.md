## ADDED Requirements

### Requirement: PERSIST-001 聚合根统一保存

系统 **SHALL** 提供医案聚合根的统一保存API，在单次事务中同时保存诊断(Consultation)和处方(Prescription)数据。

#### Scenario: 完整医案保存
- **Given** 用户在医案工作区填写了诊断和处方信息
- **When** 用户触发保存操作（暂存或完成看诊）
- **Then** 系统通过单一API端点`PUT /api/medicalcase/{id}/aggregate`提交所有数据
- **And** 诊断和处方在同一事务中保存
- **And** 任一部分保存失败时整体回滚

#### Scenario: 仅诊断无处方
- **Given** 用户选择"不开处方"
- **When** 用户触发保存操作
- **Then** 系统保存诊断数据
- **And** Prescription部分为空或NeedsPrescription=false
- **And** 不创建空的处方记录

---

### Requirement: PERSIST-002 聚合DTO结构

系统 **SHALL** 使用`MedicalCaseAggregateInputDto`作为聚合保存的输入结构，包含嵌套的诊断和处方信息。

#### Scenario: DTO结构定义
- **Given** 开发者需要调用聚合保存API
- **When** 构造请求体
- **Then** 使用以下嵌套结构：
  ```
  MedicalCaseAggregateInputDto
  ├── Id: Guid
  ├── Remark: string?
  ├── EditReason: string?  (审计原因)
  ├── Consultation: ConsultationInputDto?
  └── Prescription: PrescriptionAggregateDto?
      ├── NeedsPrescription: bool
      ├── DosageCount: int
      ├── Usage: string?
      └── Items: List<PrescriptionItemInputDto>
  ```

---

### Requirement: PERSIST-003 前端数据收集模式

系统 **SHALL** 采用数据收集模式，由工作区协调器统一收集各Panel数据后调用聚合保存API。

#### Scenario: Panel提供数据接口
- **Given** ConsultationPanel和PrescriptionPanel已加载
- **When** 工作区协调器需要保存数据
- **Then** 调用各Panel的数据收集方法（非API调用）
- **And** 组装MedicalCaseAggregateInputDto
- **And** 调用聚合保存API

#### Scenario: Panel不再独立调用API
- **Given** 用户触发保存操作
- **When** 保存流程执行
- **Then** ConsultationPanel不直接调用Consultation API
- **And** PrescriptionPanel不直接调用Prescription API
- **And** 仅通过聚合保存API完成持久化

---

### Requirement: PERSIST-004 完成看诊状态验证

系统 **SHALL** 基于数据完整性验证"完成看诊"按钮的可用状态，而非依赖事件驱动。

#### Scenario: 按钮启用条件
- **Given** 用户在医案工作区
- **When** 诊断必填字段已填写（主诉、中医诊断）
- **And** 处方条件满足（不开处方，或开处方且至少1个有效药材项）
- **Then** "完成看诊"按钮启用

#### Scenario: 按钮禁用条件
- **Given** 用户在医案工作区
- **When** 诊断必填字段未填写
- **Or** 选择开处方但无有效药材项
- **Then** "完成看诊"按钮禁用

---

### Requirement: PERSIST-005 暂存保存数据完整性

系统 **SHALL** 确保暂存操作保存完整的诊断和处方数据，下次打开时可恢复。

#### Scenario: 暂存包含处方
- **Given** 用户已填写诊断信息和处方药材
- **When** 用户选择"暂存医案"
- **Then** 诊断数据保存到数据库
- **And** 处方数据保存到数据库
- **And** 医案状态变为Draft

#### Scenario: 恢复暂存医案
- **Given** 存在一个Draft状态的医案，包含诊断和处方数据
- **When** 用户重新打开该医案
- **Then** 诊断Panel加载之前保存的数据
- **And** 处方Panel加载之前保存的药材项
- **And** 用户可继续编辑

---

## MODIFIED Requirements

### Requirement: LIFECYCLE-001 暂停看诊语义

系统 **SHALL** 将"暂停看诊"定义为保存当前进度并将医案状态设为Draft，用户可在后续继续编辑。保存操作通过聚合根统一保存API完成，确保诊断和处方数据一起持久化。

#### Scenario: 医生临时离开
- **Given** 医生正在进行看诊（医案状态为Active）
- **When** 医生点击"暂停看诊"按钮
- **Then** 系统通过聚合保存API保存所有已填写的诊断和处方数据
- **And** 医案状态变更为Draft
- **And** 用户可在患者待看诊列表中看到该患者
- **And** 重新选择该患者时可继续之前的看诊，诊断和处方数据完整恢复

#### Scenario: 急诊插队
- **Given** 医生正在为患者A看诊
- **When** 急诊患者B需要优先处理
- **Then** 医生可暂停患者A的看诊
- **And** 诊断和处方数据通过聚合保存API一起保存
- **And** 为患者B开始新的看诊
- **And** 完成患者B后可继续患者A的看诊，数据完整恢复

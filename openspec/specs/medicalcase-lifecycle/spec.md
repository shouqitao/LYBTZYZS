# medicalcase-lifecycle Specification

## Purpose
TBD - created by archiving change clarify-cancel-consultation-logic. Update Purpose after archive.
## Requirements
### Requirement: LIFECYCLE-001 暂停看诊语义
系统 **SHALL** 将"暂停看诊"定义为保存当前进度并将医案状态设为Draft，用户可在后续继续编辑。

#### Scenario: 医生临时离开
- **Given** 医生正在进行看诊（医案状态为Active）
- **When** 医生点击"暂停看诊"按钮
- **Then** 系统保存所有已填写的诊断和处方数据
- **And** 医案状态变更为Draft
- **And** 用户可在患者待看诊列表中看到该患者
- **And** 重新选择该患者时可继续之前的看诊

#### Scenario: 急诊插队
- **Given** 医生正在为患者A看诊
- **When** 急诊患者B需要优先处理
- **Then** 医生可暂停患者A的看诊
- **And** 为患者B开始新的看诊
- **And** 完成患者B后可继续患者A的看诊

---

### Requirement: LIFECYCLE-002 取消看诊语义
系统 **SHALL** 将"取消看诊"定义为作废本次就诊，通过软删除（IsDeleted=true）实现，数据保留供审计但无法直接继续编辑。

#### Scenario: 患者临时离开
- **Given** 医生正在进行看诊
- **When** 患者因故需要离开，本次就诊作废
- **And** 医生确认取消操作
- **Then** 系统先保存当前已填写的数据（供审计）
- **And** 将医案标记为软删除（IsDeleted=true）
- **And** 医案不再显示在正常列表中
- **And** 医案无法被重新打开编辑

#### Scenario: 取消确认
- **Given** 医生点击"取消看诊"按钮
- **When** 系统显示确认对话框
- **Then** 对话框明确说明取消后数据无法继续编辑
- **And** 建议用户如需临时离开应使用"暂停看诊"

---

### Requirement: LIFECYCLE-003 取消前自动保存
系统 **SHALL** 在执行取消操作前自动保存当前已填写的数据，确保审计数据完整性。

#### Scenario: 取消前保存诊断数据
- **Given** 医生已填写部分诊断信息但未手动保存
- **When** 医生确认取消看诊
- **Then** 系统先保存诊断数据
- **And** 然后执行软删除
- **And** 被取消的医案包含已填写的诊断数据

#### Scenario: 保存失败不阻止取消
- **Given** 医生确认取消看诊
- **When** 保存数据时发生错误（如网络问题）
- **Then** 系统记录警告日志
- **And** 继续执行软删除操作
- **And** 操作不被阻止

---

### Requirement: LIFECYCLE-004 UI提示明确性
系统 **SHALL** 在UI中明确区分"暂停看诊"和"取消看诊"的语义和后果。

#### Scenario: 暂停按钮提示
- **Given** 用户鼠标悬停在"暂停看诊"按钮上
- **When** 显示Tooltip
- **Then** Tooltip说明"保存当前进度并暂时离开，下次可继续"

#### Scenario: 取消按钮提示
- **Given** 用户鼠标悬停在"取消看诊"按钮上
- **When** 显示Tooltip
- **Then** Tooltip说明"作废本次就诊，数据保留供审计但无法继续编辑"

---


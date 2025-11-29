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

### Requirement: LIFECYCLE-005 医案创建入口约束

系统 **SHALL** 限制医案创建入口仅为临床工作流（PatientSelection → MedicalCaseWorkspace），管理界面不提供创建功能。

#### Scenario: 管理界面无新建入口
- **Given** 管理员进入医案管理界面(MedicalCaseManagementView)
- **When** 界面加载完成
- **Then** 界面不显示"新建案例"按钮
- **And** 界面提供查看、搜索、筛选、编辑和状态管理功能

#### Scenario: 临床工作流创建医案
- **Given** 医生通过PatientSelection选择患者
- **When** 医生选择"创建新医案"
- **Then** 系统导航到MedicalCaseWorkspaceView
- **And** 新医案自动关联所选患者
- **And** 医生可以开始诊疗流程

---

### Requirement: LIFECYCLE-006 管理界面职责边界

系统 **SHALL** 将医案管理界面(MedicalCaseManagementView)的职责限定为查看、编辑和管理已有医案，不包含创建功能。

#### Scenario: 管理界面核心功能
- **Given** 管理员进入医案管理界面
- **When** 界面加载完成
- **Then** 界面提供以下功能：
  - 医案列表展示（所有医案）
  - 搜索功能（按患者名/医生名/日期等）
  - 筛选功能（按状态/时间范围等）
  - 编辑功能（管理员可编辑所有医案）
  - 状态管理（完成/取消医案）
  - 审计日志查看

#### Scenario: 管理员编辑历史医案
- **Given** 管理员在医案管理界面选中一个已完成的医案
- **When** 管理员点击"编辑"按钮
- **Then** 系统导航到MedicalCaseWorkspaceView
- **And** 界面显示"历史修改模式"提示
- **And** 界面显示"修改原因"输入框
- **And** 管理员可以修改医案内容

---

### Requirement: LIFECYCLE-007 医案编辑权限控制

系统 **SHALL** 基于用户角色和医案状态控制编辑权限。

#### Scenario: 医生编辑自己未完成的医案
- **Given** 医生登录系统
- **And** 存在该医生创建的Draft或Active状态医案
- **When** 医生尝试编辑该医案
- **Then** 系统允许编辑操作

#### Scenario: 医生无法编辑已完成的医案
- **Given** 医生登录系统
- **And** 存在该医生创建的Completed状态医案
- **When** 医生尝试编辑该医案
- **Then** 系统拒绝编辑操作
- **And** 界面显示只读模式

#### Scenario: 医生无法编辑他人的医案
- **Given** 医生A登录系统
- **And** 存在医生B创建的医案
- **When** 医生A尝试编辑该医案
- **Then** 系统拒绝编辑操作
- **And** 界面不显示编辑按钮

#### Scenario: 管理员可编辑所有医案
- **Given** 管理员(Admin或SuperAdmin)登录系统
- **And** 存在任意状态的医案
- **When** 管理员尝试编辑该医案
- **Then** 系统允许编辑操作
- **And** 界面要求输入修改原因（已完成医案）

---

### Requirement: LIFECYCLE-008 医案修改审计日志

系统 **SHALL** 记录所有医案修改操作的审计日志，包括修改人、修改时间、修改内容和修改原因。

#### Scenario: 创建医案记录审计
- **Given** 用户创建新医案
- **When** 医案保存成功
- **Then** 系统记录审计日志
- **And** 日志包含操作类型(Create)、操作人、操作时间

#### Scenario: 更新医案记录审计
- **Given** 用户修改现有医案
- **When** 修改保存成功
- **Then** 系统记录审计日志
- **And** 日志包含操作类型(Update)、操作人、操作时间
- **And** 日志包含修改的字段列表
- **And** 日志包含修改前后的值

#### Scenario: 历史医案修改记录原因
- **Given** 管理员修改已完成的医案
- **When** 修改保存成功
- **Then** 系统记录审计日志
- **And** 日志包含修改原因字段
- **And** 修改原因为管理员输入的内容

#### Scenario: 查看审计日志
- **Given** 管理员在医案管理界面选中一个医案
- **When** 管理员点击"查看审计日志"
- **Then** 系统显示该医案的所有修改历史
- **And** 历史按时间倒序排列
- **And** 每条记录显示修改人、时间、内容摘要

---

### Requirement: LIFECYCLE-009 医生看诊界面保存和编辑按钮

系统 **SHALL** 在医生看诊界面(MedicalCaseWorkspaceView)底部操作栏提供保存和编辑按钮，支持模式切换。

#### Scenario: 编辑模式下显示保存按钮
- **Given** 医生正在编辑医案（新建或继续未完成医案）
- **When** 界面处于编辑模式
- **Then** 底部操作栏显示"保存"按钮
- **And** 点击保存后数据持久化但状态不变
- **And** 用户可继续编辑

#### Scenario: 只读模式下显示编辑按钮
- **Given** 用户查看已完成的医案
- **And** 用户有编辑权限（管理员或创建者+未完成）
- **When** 界面处于只读模式
- **Then** 底部操作栏显示"编辑"按钮
- **And** 点击编辑后进入编辑模式

#### Scenario: 无权限时不显示编辑按钮
- **Given** 医生查看他人创建的已完成医案
- **When** 界面加载完成
- **Then** 底部操作栏不显示"编辑"按钮
- **And** 界面保持只读模式

#### Scenario: 保留现有操作按钮
- **Given** 医生在编辑模式下
- **When** 界面显示底部操作栏
- **Then** 操作栏包含"保存"、"暂停看诊"、"完成看诊"按钮
- **And** "暂停看诊"保存数据并设状态为Draft
- **And** "完成看诊"保存数据并设状态为Completed

---

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

### Requirement: LIFECYCLE-013 关闭病案API
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

### Requirement: LIFECYCLE-014 删除病案API
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


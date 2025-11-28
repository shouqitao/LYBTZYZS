## ADDED Requirements

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

# medicalcase-edit-modes Specification

## Purpose
定义MedicalCaseWorkspaceView在临床看诊和管理编辑两个模块中的复用逻辑。

## ADDED Requirements

### Requirement: EDITMODE-001 工作区模式
系统 **SHALL** 支持MedicalCaseWorkspaceView在两种工作区模式下运行：Clinical(临床看诊)和Management(管理编辑)。

#### Scenario: 临床看诊模式
- **Given** 医生从PatientSelectionView选择患者并开始看诊
- **When** 系统导航到MedicalCaseWorkspaceView
- **Then** 工作区模式设置为Clinical
- **And** 界面标题显示"看诊中 | 患者：XXX"
- **And** 返回按钮显示"返回患者选择"

#### Scenario: 管理编辑模式
- **Given** 用户从MedicalCaseManagementView点击编辑或查看
- **When** 系统导航到MedicalCaseWorkspaceView
- **Then** 工作区模式设置为Management
- **And** 界面标题显示"编辑医案 | 患者：XXX"
- **And** 返回按钮显示"返回医案列表"

---

### Requirement: EDITMODE-002 编辑状态
系统 **SHALL** 支持MedicalCaseWorkspaceView在两种编辑状态下运行：Editing(编辑中)和ReadOnly(只读)。

#### Scenario: 编辑状态界面
- **Given** 用户进入编辑状态
- **When** 界面渲染
- **Then** 所有表单字段可编辑
- **And** 底部操作栏显示: [暂存医案] [打印处方笺] [完成看诊]

#### Scenario: 只读状态界面
- **Given** 用户进入只读状态
- **When** 界面渲染
- **Then** 所有表单字段只读
- **And** 底部操作栏显示: [修改医案](有权限时) [打印处方笺]

---

### Requirement: EDITMODE-003 暂存医案
系统 **SHALL** 提供"暂存医案"功能，保存当前数据并切换到只读状态。

#### Scenario: 点击暂存医案
- **Given** 用户正在编辑医案
- **When** 用户点击"暂存医案"按钮
- **Then** 系统保存当前诊断和处方数据
- **And** 医案状态设置为Draft
- **And** 界面切换到只读状态
- **And** 用户留在当前界面

#### Scenario: 暂存按钮Tooltip
- **Given** 用户鼠标悬停在"暂存医案"按钮上
- **When** 显示Tooltip
- **Then** Tooltip内容为"保存当前进度，可随时点击'修改医案'继续编辑"

---

### Requirement: EDITMODE-004 修改医案
系统 **SHALL** 提供"修改医案"功能，切换到编辑状态（需权限检查）。

#### Scenario: 点击修改医案
- **Given** 用户正在查看医案（只读状态）
- **And** 用户有编辑权限
- **When** 用户点击"修改医案"按钮
- **Then** 界面切换到编辑状态
- **And** 显示"暂存医案"和"完成看诊"按钮

#### Scenario: 修改按钮Tooltip
- **Given** 用户鼠标悬停在"修改医案"按钮上
- **When** 显示Tooltip
- **Then** Tooltip内容为"进入编辑模式，可修改诊断和处方内容"

#### Scenario: 无权限时隐藏修改按钮
- **Given** 用户查看他人创建的已完成医案
- **And** 用户不是管理员
- **When** 界面加载完成
- **Then** 不显示"修改医案"按钮

---

### Requirement: EDITMODE-005 动态返回导航
系统 **SHALL** 根据工作区模式返回到正确的来源页面。

#### Scenario: 临床模式返回
- **Given** 用户在Clinical模式下
- **When** 用户点击返回按钮
- **Then** 系统导航到PatientSelectionView

#### Scenario: 管理模式返回
- **Given** 用户在Management模式下
- **When** 用户点击返回按钮
- **Then** 系统导航到MedicalCaseManagementView

---

### Requirement: EDITMODE-006 管理界面入口
系统 **SHALL** 在医案管理界面提供明确区分的"查看详情"和"编辑"入口。

#### Scenario: 查看详情入口
- **Given** 用户在MedicalCaseManagementView
- **When** 用户点击"查看详情"按钮
- **Then** 系统导航到MedicalCaseWorkspaceView
- **And** 工作区模式为Management
- **And** 编辑状态为ReadOnly

#### Scenario: 编辑入口
- **Given** 用户在MedicalCaseManagementView
- **And** 用户有编辑权限
- **When** 用户点击"编辑"按钮
- **Then** 系统导航到MedicalCaseWorkspaceView
- **And** 工作区模式为Management
- **And** 编辑状态为Editing

---

### Requirement: EDITMODE-007 完成看诊后返回
系统 **SHALL** 在完成看诊后返回到来源页面。

#### Scenario: 临床模式完成看诊
- **Given** 用户在Clinical模式下完成看诊
- **When** 医案状态设置为Completed
- **Then** 系统自动导航到PatientSelectionView

#### Scenario: 管理模式完成看诊
- **Given** 用户在Management模式下完成看诊
- **When** 医案状态设置为Completed
- **Then** 系统自动导航到MedicalCaseManagementView

---

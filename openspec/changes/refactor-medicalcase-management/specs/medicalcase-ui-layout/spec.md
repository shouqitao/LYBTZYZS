# medicalcase-ui-layout Spec Delta

## MODIFIED Requirements

### Requirement: UI-LAYOUT-001 整体布局规范

系统 **SHALL** 在医案管理模块采用Master-Detail布局，左侧40%显示医案列表，右侧60%显示医案详情。

#### Scenario: 医案管理Master-Detail布局
- **Given** 用户进入医案管理界面
- **When** 界面加载完成
- **Then** 左侧显示医案列表（40%宽度）
- **And** 右侧显示医案详情或空状态（60%宽度）
- **And** 布局与FormulaMasterDetailView保持一致

---

### Requirement: UI-LAYOUT-004 诊断面板布局

系统 **SHALL** 采用4个核心诊断字段：现病史、舌诊、脉诊、中医诊断。

#### Scenario: 诊断字段布局（简化版）
- **Given** 用户在诊断面板
- **When** 填写诊断信息
- **Then** 字段按顺序显示：现病史 → 舌诊 → 脉诊 → 中医诊断
- **And** 仅中医诊断为必填字段（带红色*标记）
- **And** 主诉、四诊、治疗原则、医嘱、备注字段已移除

---

## ADDED Requirements

### Requirement: UI-LAYOUT-009 医案管理工具栏

系统 **SHALL** 在医案管理的Master区域提供仅包含刷新功能的工具栏，不包含新建功能。

#### Scenario: 工具栏无新建按钮
- **Given** 用户在医案管理界面
- **When** 查看Master区域工具栏
- **Then** 仅显示刷新按钮
- **And** 不显示新建按钮
- **And** 新建医案需通过看诊入口创建

---

### Requirement: UI-LAYOUT-010 医案详情区域

系统 **SHALL** 在Detail区域显示医案详情，包含患者信息（只读）、诊断摘要（只读）、处方摘要（只读）和备注（可编辑）。

#### Scenario: 详情区域显示
- **Given** 用户选择一个医案
- **When** 详情区域加载完成
- **Then** 显示患者姓名（只读）
- **And** 显示就诊日期
- **And** 显示诊断摘要（现病史、舌诊、脉诊、中医诊断）
- **And** 显示处方摘要（药材数、剂数）
- **And** 显示状态和备注

#### Scenario: 详情区域编辑
- **Given** 用户点击编辑按钮
- **When** 进入编辑模式
- **Then** 备注字段变为可编辑
- **And** 患者信息、诊断、处方信息保持只读

---

### Requirement: UI-LAYOUT-011 医案管理空状态

系统 **SHALL** 在未选择医案时显示空状态提示，引导用户选择列表项。

#### Scenario: 空状态显示
- **Given** 用户进入医案管理界面
- **When** 未选择任何医案
- **Then** Detail区域显示"请选择医案"提示
- **And** 显示引导文案"从左侧列表中选择一个医案查看详情"
- **And** 不显示新建按钮（与其他模块不同）

---

### Requirement: UI-LAYOUT-012 看诊工作区独立

系统 **SHALL** 保持看诊工作区（MedicalCaseWorkspaceView）独立运行，不受管理视图布局变更影响。

#### Scenario: 看诊工作区保持不变
- **Given** 用户通过看诊入口进入
- **When** 打开MedicalCaseWorkspaceView
- **Then** 界面布局保持原有5:5分栏（诊断+处方）
- **And** 诊断面板使用4个核心字段
- **And** 功能逻辑不变

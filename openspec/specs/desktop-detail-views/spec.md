# desktop-detail-views Specification

## Purpose
TBD - created by archiving change refactor-detail-view-container. Update Purpose after archive.
## Requirements
### Requirement: BaseDetailContainer容器组件

系统 SHALL 提供 BaseDetailContainer 容器组件，支持查看和编辑模式的独立内容定义。

#### Scenario: 容器基本结构
- **WHEN** 使用 BaseDetailContainer 组件
- **THEN** 组件包含 Header、Content、Footer 三个区域
- **AND** Header 显示标题和操作按钮
- **AND** Content 根据 IsEditMode 显示 ViewContent 或 EditContent
- **AND** Footer 在编辑模式显示保存/取消按钮

#### Scenario: 查看模式显示
- **WHEN** IsEditMode 为 False
- **THEN** 显示 ViewContent 内容
- **AND** 隐藏 EditContent 内容
- **AND** Header 显示"编辑"按钮

#### Scenario: 编辑模式显示
- **WHEN** IsEditMode 为 True
- **THEN** 显示 EditContent 内容
- **AND** 隐藏 ViewContent 内容
- **AND** Footer 显示保存/取消按钮

### Requirement: InfoCard信息卡片控件

系统 SHALL 提供 InfoCard 控件用于查看模式下的信息分组展示。

#### Scenario: 卡片基本显示
- **WHEN** 使用 InfoCard 控件
- **THEN** 显示带标题的卡片容器
- **AND** 内容区域支持自定义布局

#### Scenario: 多列布局支持
- **WHEN** InfoCard 内容需要多列显示
- **THEN** 支持 Grid 列定义
- **AND** 自动适应内容宽度

### Requirement: DetailView容器化架构

所有实体 DetailView 页面 SHALL 使用 BaseDetailContainer 容器模式实现查看/编辑分离。

#### Scenario: PatientDetailView容器化
- **WHEN** 打开患者详情页
- **THEN** 使用 BaseDetailContainer 容器
- **AND** ViewContent 使用 TextBlock 展示患者信息
- **AND** EditContent 使用 TextBox 编辑患者信息

#### Scenario: HerbDetailView容器化
- **WHEN** 打开药材详情页
- **THEN** 使用 BaseDetailContainer 容器
- **AND** ViewContent 使用纯展示控件
- **AND** EditContent 使用表单输入控件

#### Scenario: UserDetailView容器化
- **WHEN** 打开用户详情页
- **THEN** 使用 BaseDetailContainer 容器
- **AND** 查看/编辑模式完全分离

#### Scenario: FormulaDetailView容器化
- **WHEN** 打开验方详情页
- **THEN** 使用 BaseDetailContainer 容器
- **AND** 药材列表在两种模式下都可见

#### Scenario: MedicalCaseDetailView容器化
- **WHEN** 打开医案详情页
- **THEN** 使用 BaseDetailContainer 容器
- **AND** 诊疗记录在查看模式下格式化展示

### Requirement: DetailView模式切换

系统 SHALL 支持 DetailView 在查看模式和编辑模式之间切换，使用容器化架构替代 IsReadOnly 属性切换。

#### Scenario: 从查看切换到编辑
- **WHEN** 用户点击"编辑"按钮
- **THEN** IsEditMode 设为 True
- **AND** 显示 EditContent 面板
- **AND** 隐藏 ViewContent 面板

#### Scenario: 保存并返回查看
- **WHEN** 用户在编辑模式点击"保存"
- **THEN** 执行保存操作
- **AND** IsEditMode 设为 False
- **AND** 返回查看模式

#### Scenario: 取消编辑
- **WHEN** 用户在编辑模式点击"取消"
- **THEN** 放弃未保存的更改
- **AND** IsEditMode 设为 False
- **AND** 返回查看模式


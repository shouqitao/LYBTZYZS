## ADDED Requirements

### Requirement: DetailView预览控件组件化

系统 SHALL 为每个实体模块提供独立的预览控件（XxxViewControl），用于在查看模式下展示实体详情。

#### Scenario: FormulaViewControl预览控件
- **WHEN** 需要显示验方详情预览
- **THEN** 使用 FormulaViewControl 控件
- **AND** 控件通过 Formula DependencyProperty 接收 FormulaDetailDto 数据
- **AND** 显示名称、分类、药性、功效、用法、备注、审计信息和药材列表

#### Scenario: HerbViewControl预览控件
- **WHEN** 需要显示药材详情预览
- **THEN** 使用 HerbViewControl 控件
- **AND** 控件通过 Herb DependencyProperty 接收 HerbDto 数据
- **AND** 显示名称、分类、属性、功效等信息

#### Scenario: PatientViewControl预览控件
- **WHEN** 需要显示患者详情预览
- **THEN** 使用 PatientViewControl 控件
- **AND** 控件通过 Patient DependencyProperty 接收 PatientDto 数据
- **AND** 显示姓名、性别、年龄、联系方式等信息

#### Scenario: UserViewControl预览控件
- **WHEN** 需要显示用户详情预览
- **THEN** 使用 UserViewControl 控件
- **AND** 控件通过 User DependencyProperty 接收 UserDto 数据
- **AND** 显示用户名、角色、状态等信息

#### Scenario: MedicalCaseViewControl预览控件
- **WHEN** 需要显示医案详情预览
- **THEN** 使用 MedicalCaseViewControl 控件
- **AND** 控件通过 MedicalCase DependencyProperty 接收 MedicalCaseDto 数据
- **AND** 显示诊疗记录、处方信息等

### Requirement: DetailView编辑控件组件化

系统 SHALL 为每个实体模块提供独立的编辑控件（XxxEditControl），用于在编辑模式下修改实体信息。

#### Scenario: FormulaEditControl编辑控件
- **WHEN** 需要编辑验方信息
- **THEN** 使用 FormulaEditControl 控件
- **AND** 控件通过 Formula DependencyProperty 双向绑定数据
- **AND** 提供名称、分类、药性、功效等字段的编辑表单
- **AND** 包含药材列表编辑器（HerbListEditor）

#### Scenario: HerbEditControl编辑控件
- **WHEN** 需要编辑药材信息
- **THEN** 使用 HerbEditControl 控件
- **AND** 控件通过 Herb DependencyProperty 双向绑定数据
- **AND** 提供名称、分类、属性、功效等字段的编辑表单

#### Scenario: PatientEditControl编辑控件
- **WHEN** 需要编辑患者信息
- **THEN** 使用 PatientEditControl 控件
- **AND** 控件通过 Patient DependencyProperty 双向绑定数据
- **AND** 提供姓名、性别、年龄、联系方式等字段的编辑表单

#### Scenario: UserEditControl编辑控件
- **WHEN** 需要编辑用户信息
- **THEN** 使用 UserEditControl 控件
- **AND** 控件通过 User DependencyProperty 双向绑定数据
- **AND** 提供用户名、角色、状态等字段的编辑表单

#### Scenario: MedicalCaseEditControl编辑控件
- **WHEN** 需要编辑医案信息
- **THEN** 使用 MedicalCaseEditControl 控件
- **AND** 控件通过 MedicalCase DependencyProperty 双向绑定数据
- **AND** 提供诊疗记录、处方信息等字段的编辑表单

### Requirement: 预览控件跨场景复用

系统 SHALL 支持在不同场景复用预览控件。

#### Scenario: FormulaImportDialog复用FormulaViewControl
- **WHEN** FormulaImportDialog需要显示选中验方的详情预览
- **THEN** 右侧面板使用 FormulaViewControl 控件
- **AND** 绑定选中验方的详情数据
- **AND** 替代原有的自定义预览模板

## MODIFIED Requirements

### Requirement: DetailView容器化架构

所有实体 DetailView 页面 SHALL 使用 BaseDetailContainer 容器模式实现查看/编辑分离，ViewContent和EditContent通过独立控件组件实现。

#### Scenario: PatientDetailView容器化
- **WHEN** 打开患者详情页
- **THEN** 使用 BaseDetailContainer 容器
- **AND** ViewContent 使用 PatientViewControl 控件
- **AND** EditContent 使用 PatientEditControl 控件

#### Scenario: HerbDetailView容器化
- **WHEN** 打开药材详情页
- **THEN** 使用 BaseDetailContainer 容器
- **AND** ViewContent 使用 HerbViewControl 控件
- **AND** EditContent 使用 HerbEditControl 控件

#### Scenario: UserDetailView容器化
- **WHEN** 打开用户详情页
- **THEN** 使用 BaseDetailContainer 容器
- **AND** ViewContent 使用 UserViewControl 控件
- **AND** EditContent 使用 UserEditControl 控件

#### Scenario: FormulaDetailView容器化
- **WHEN** 打开验方详情页
- **THEN** 使用 BaseDetailContainer 容器
- **AND** ViewContent 使用 FormulaViewControl 控件
- **AND** EditContent 使用 FormulaEditControl 控件

#### Scenario: MedicalCaseDetailView容器化
- **WHEN** 打开医案详情页
- **THEN** 使用 BaseDetailContainer 容器
- **AND** ViewContent 使用 MedicalCaseViewControl 控件
- **AND** EditContent 使用 MedicalCaseEditControl 控件
